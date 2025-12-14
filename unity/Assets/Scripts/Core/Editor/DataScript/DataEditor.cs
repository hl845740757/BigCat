using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Wjybxx.BigCat.Editor.UIElements;
using Wjybxx.BigCatTool.DataScript;
using Wjybxx.Commons;
using Wjybxx.Commons.Pool;

namespace Wjybxx.BigCat.Editor.DataScript
{
/// <summary>
/// 该类是一个模板基类，其它编辑器可以基于此进行即可
/// </summary>
public class DataEditor : EditorWindow
{
    protected Toolbar toolbar;
    protected GraphView graphView;
    protected VisualElement inspectorView;
    protected VisualElement nodeHeaderView;
    protected VisualElement nodeValueView;

    protected LongField localIdField; // 其实Node也可以通过一个Variable绑定UI
    protected TextField folderField;
    protected TextField nameField;
    protected TextField titleField;
    protected Vector2Field positionField;
    protected TextField typeSymbolField;
    protected Toggle enablePortToggle; // Pair类型启用端口

    /// <summary>
    /// 由外部构建<see cref="DSRepository"/>
    /// </summary>
    public DSRepository repository { get; private set; }
    /// <summary>
    /// 对象图，所有的数据都保存在这里，包括场景中的数据
    /// </summary>
    public DataGraph dataGraph { get; private set; }
    /// <summary>
    /// 当前选中的节点
    /// </summary>
    public NodeView selectedNode { get; set; }
    /// <summary>
    /// 类型搜索实现
    /// </summary>
    private TypeSearchWindowProvider typeSearchWindowProvider;

    private readonly List<DSField> _filedListCache = new List<DSField>();
    private readonly Dictionary<DSNamedType, string> displayNameCache = new Dictionary<DSNamedType, string>();
    private readonly Dictionary<DSNamedType, VarObjectField> objectFieldCache = new Dictionary<DSNamedType, VarObjectField>();

    /// <summary>
    /// 用户应该在自己的静态方法中初始化该Window的依赖，主要是初始化DataScript文件
    /// </summary>
    [MenuItem("Window/BigCat/DataEditor")]
    private static void OpenWindow() {
        DataEditor wnd = GetWindow<DataEditor>();
        wnd.titleContent = new GUIContent("DataGraphEditor");
        DSRepository repository = wnd.repository;
        // TODO 通过配置文件加载关联的ds文件
        string scriptDir = Application.dataPath + "/Resources/DataScript";
        foreach (string filePath in Directory.GetFiles(scriptDir, "*.ds", SearchOption.AllDirectories)) {
            DSFile dsFile = DSFileParser.Parse(new FileInfo(filePath));
            repository.AddFile(dsFile);
        }
        repository.Build();
    }

    protected virtual void OnEnable() {
        if (repository == null) {
            repository = new DSRepository();
            dataGraph = new DataGraph(repository);
            dataGraph.undoPerformed += OnUndoRedoPerformed;
            dataGraph.redoPerformed += OnUndoRedoPerformed;
            dataGraph.onGraphChanged += OnDataGraphChanged;
        }
        typeSearchWindowProvider = CreateInstance<TypeSearchWindowProvider>();
        typeSearchWindowProvider.editor = this;
    }

    protected virtual void OnDisable() {
        dataGraph.undoPerformed -= OnUndoRedoPerformed;
        dataGraph.redoPerformed -= OnUndoRedoPerformed;
        dataGraph.onGraphChanged -= OnDataGraphChanged;
    }

    protected virtual void OnUndoRedoPerformed(DataGraphChange _) {
        graphView.Refresh();
        RefreshNodeInspector();
    }

    protected virtual void OnDataGraphChanged(DataGraphChange _) {
        // GraphView内部会监听，其实多数情况下都可以不刷新整个Inspector，只刷新基础信息部分
        RefreshNodeInspector();
    }

    protected virtual void Update() {
        dataGraph.Update();
    }

    public void CreateGUI() {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;
        // Import UXML
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
            "Assets/Scripts/Core/Editor/DataScript/DataEditor.uxml");
        root.Add(visualTree.Instantiate());

        toolbar = root.Q<Toolbar>();
        graphView = root.Q<GraphView>();
        inspectorView = root.Q<VisualElement>("inspector-div");
        nodeHeaderView = root.Q<VisualElement>("node-header");
        nodeValueView = root.Q<VisualElement>("node-value");
        graphView.editor = this;
        //
        localIdField = nodeHeaderView.Q<LongField>("local-id");
        nameField = nodeHeaderView.Q<TextField>("name");
        folderField = nodeHeaderView.Q<TextField>("folder");
        titleField = nodeHeaderView.Q<TextField>("title");
        positionField = nodeHeaderView.Q<Vector2Field>("position");
        typeSymbolField = nodeHeaderView.Q<TextField>("type-symbol");
        enablePortToggle = nodeHeaderView.Q<Toggle>("enable-port");
        localIdField.RegisterValueChangedCallback(OnLocalIdFieldChanged);
        nameField.RegisterValueChangedCallback(OnNameFieldChanged);
        folderField.RegisterValueChangedCallback(OnFolderFieldChanged);
        titleField.RegisterValueChangedCallback(OnTitleFieldChanged);
        positionField.RegisterValueChangedCallback(OnPositionFiledChanged);
        enablePortToggle.RegisterValueChangedCallback(OnEnablePortChanged);
        //
        root.RegisterCallback<KeyDownEvent>(OnKeyDownEvent);
        toolbar.Q<Button>("open-file").RegisterCallback<ClickEvent>(OnClickOpenFile);
        toolbar.Q<Button>("close-file").RegisterCallback<ClickEvent>(OnClickCloseFile);
        toolbar.Q<Button>("save-file-as").RegisterCallback<ClickEvent>(OnClickSaveFileAs);
        toolbar.Q<Button>("change-folder").RegisterCallback<ClickEvent>(OnClickChangeFolder);
        toolbar.Q<ToolbarMenu>("folder-menu").RegisterCallback<ClickEvent>(OnClickFolderMenu);
        // 
        graphView.Bind(dataGraph);
        graphView.nodeCreationRequest = OnNodeCreationRequest;
        graphView.serializeGraphElements = SerializeNodes;
        graphView.unserializeAndPaste = UnserializeAndPasteNodes;
    }

    private string SerializeNodes(IEnumerable<GraphElement> elements) {
        List<DataNode> dataNodes = new List<DataNode>();
        foreach (NodeView nodeView in elements.Where(e => e is NodeView).Cast<NodeView>()) {
            dataNodes.Add(nodeView.dataNode);
        }
        if (dataNodes.Count == 0) return "";
        return dataGraph.SerializeNodes(dataNodes);
    }

    private void UnserializeAndPasteNodes(string operation, string data) {
        // operation有两个值：Paste Duplicate 目前行为一致
        List<DataNode> dataNodes = dataGraph.UnserializeAndPasteNodes(data);
        graphView.Refresh(); // 立即刷新并更新选中区域
        graphView.RefreshSelection(dataNodes);
    }

    /// <summary>
    /// 主要处理控制事件，不建议停止事件传播
    /// </summary>
    /// <param name="evt"></param>
    protected virtual void OnKeyDownEvent(KeyDownEvent evt) {
        if (!evt.ctrlKey || evt.shiftKey) return;
        if (evt.keyCode == KeyCode.Z) {
            dataGraph.Undo();
        } else if (evt.keyCode == KeyCode.Y) {
            dataGraph.Redo();
        } else if (evt.keyCode == KeyCode.S) {
            dataGraph.Save();
        }
    }

    private void OnClickChangeFolder(ClickEvent evt) {
        evt.StopPropagation();
        string folder = toolbar.Q<MTextField>("folder").value?.Trim();
        ChangeFolder(folder);
    }

    private void ChangeFolder(string folder) {
        if (string.IsNullOrEmpty(folder) || folder == "root") {
            folder = null;
        }
        graphView.currentFolder = folder;
        graphView.Refresh();
        Debug.Log("切换成功，CurrentFolder: " + folder);
    }

    private void OnClickFolderMenu(ClickEvent evt) {
        evt.StopPropagation();
        ToolbarMenu toolbarMenu = (ToolbarMenu)evt.currentTarget;
        toolbarMenu.menu.MenuItems().Clear();
        //
        foreach (string folder in dataGraph.nodeList.Select(e => e.folder).Distinct()) {
            string folderName = folder == null ? "root" : folder;
            toolbarMenu.menu.AppendAction(folderName, menuAction => ChangeFolder(menuAction.name));
        }
    }

    private void OnClickCloseFile(ClickEvent evt) {
        evt.StopPropagation();
        dataGraph.Close();
        dataGraph.assetPath = null;
        graphView.currentFolder = null;
        toolbar.Q<MTextField>("asset-path").SetValueWithoutNotify("");
        toolbar.Q<MTextField>("folder").SetValueWithoutNotify("");
        //
        selectedNode = null;
        graphView.Bind(dataGraph);
        RefreshNodeInspector();
    }

    private void OnClickSaveFileAs(ClickEvent evt) {
        evt.StopPropagation();
        string filePath = EditorUtility.SaveFilePanel("选择文件路径", UnityEditorUtil.lastOpenFolder,
            "NewDataGraph", "dson");
        if (string.IsNullOrEmpty(filePath)) {
            return;
        }
        string assetPath = UnityEditorUtil.ConvertToAssetPath(filePath);
        UnityEditorUtil.lastOpenFolder = UnityEditorUtil.GetAssetFolderPath(assetPath);
        toolbar.Q<MTextField>("asset-path").SetValueWithoutNotify(assetPath);
        //
        dataGraph.assetPath = assetPath;
        dataGraph.Save();
    }

    private void OnClickOpenFile(ClickEvent evt) {
        evt.StopPropagation();
        string filePath = UnityEditorUtil.OpenFilePanel("选择资产文件", UnityEditorUtil.lastOpenFolder, "dson");
        if (string.IsNullOrEmpty(filePath)) {
            return;
        }
        string assetPath = UnityEditorUtil.ConvertToAssetPath(filePath);
        UnityEditorUtil.lastOpenFolder = UnityEditorUtil.GetAssetFolderPath(assetPath);
        toolbar.Q<MTextField>("asset-path").SetValueWithoutNotify(assetPath);
        toolbar.Q<MTextField>("folder").SetValueWithoutNotify("");
        //
        dataGraph.assetPath = assetPath;
        dataGraph.Load();
        //
        selectedNode = null;
        graphView.Bind(dataGraph);
        RefreshNodeInspector();
    }

    #region node-info

    private void OnEnablePortChanged(ChangeEvent<bool> evt) {
        evt.StopPropagation();
        if (selectedNode == null) return;
        DataNode dataNode = selectedNode.dataNode;
        if ((dataNode.features & Features.EnablePort) != 0) return;
        dataNode.features |= Features.EnablePort; // 该功能其实是为Pair类型设计的
        dataGraph.InitOutputFields(dataNode);
        selectedNode.RebuildPorts();
        dataNode.ApplyModifiedProperties();
    }

    private void OnPositionFiledChanged(ChangeEvent<Vector2> evt) {
        evt.StopPropagation();
        if (selectedNode == null) return;
        selectedNode.dataNode.position = evt.newValue;
        selectedNode.dataNode.ApplyModifiedProperties();
    }

    private void OnTitleFieldChanged(ChangeEvent<string> evt) {
        evt.StopPropagation();
        if (selectedNode == null) return;
        selectedNode.dataNode.title = ObjectUtil.EmptyToDef(evt.newValue, null);
        selectedNode.dataNode.ApplyModifiedProperties();
    }

    private void OnFolderFieldChanged(ChangeEvent<string> evt) {
        evt.StopPropagation();
        if (selectedNode == null) return;
        selectedNode.dataNode.folder = ObjectUtil.EmptyToDef(evt.newValue, null);
        selectedNode.dataNode.ApplyModifiedProperties();
    }

    private void OnNameFieldChanged(ChangeEvent<string> evt) {
        evt.StopPropagation();
        if (selectedNode == null) return;
        selectedNode.dataNode.name = ObjectUtil.EmptyToDef(evt.newValue, null);
        selectedNode.dataNode.ApplyModifiedProperties();
    }

    private void OnLocalIdFieldChanged(ChangeEvent<long> evt) {
        if (selectedNode == null) return;
        if (evt.newValue <= 0) {
            Debug.LogWarning("localId is invalid: " + evt.newValue);
            localIdField.SetValueWithoutNotify(evt.previousValue);
            return;
        }
        if (dataGraph.nodeDic.ContainsKey(evt.newValue)) {
            Debug.LogWarning("localId is duplicated: " + evt.newValue);
            localIdField.SetValueWithoutNotify(evt.previousValue);
            return;
        }
        selectedNode.dataNode.localId = evt.newValue;
        selectedNode.dataNode.ApplyModifiedProperties();
    }

    #endregion

    #region view事件

    /// <summary>
    /// 由于GraphView是支持框选的，但Inspector我们只展示其中一个
    /// </summary>
    /// <param name="nodeView"></param>
    public virtual void OnNodeSelected(NodeView nodeView) {
        selectedNode = nodeView;
        RefreshNodeInspector();
    }

    public virtual void OnNodeUnselected(NodeView nodeView) {
        if (selectedNode != nodeView) {
            return;
        }
        selectedNode = null;
        nodeHeaderView.SetEnabled(false);
        nodeValueView.SetEnabled(false);

        // 解除所有属性绑定
        nodeHeaderView.Query<BindableElement>().ForEach(e => e.Unbind());
        nodeValueView.Clear();
    }

    /// <summary>
    /// 请求执行选中的Node
    /// </summary>
    /// <param name="nodeView"></param>
    public virtual void OnNodeExecuteRequest(NodeView nodeView) {

    }

    /// <summary>
    /// 目标Node是否可执行
    /// </summary>
    public virtual bool IsExecutable(NodeView nodeView) {
        return (nodeView.dataNode.features & Features.Executable) != 0;
    }

    internal string GetDisplayName(DSNamedType namedType) {
        if (!displayNameCache.TryGetValue(namedType, out string displayName)) {
            displayName = DSUtil.ToDisplayString(namedType.TypeName);
            displayNameCache[namedType] = displayName;
        }
        return displayName;
    }

    /// <summary>
    /// 请求创建Node
    /// </summary>
    public virtual void OnNodeCreationRequest(NodeCreationContext nodeCreationContext) {
        SearchWindowContext context = new SearchWindowContext(nodeCreationContext.screenMousePosition, 400f);
        SearchWindow.Open(context, typeSearchWindowProvider);
    }

    internal List<SearchTreeEntry> CreateTypeSearchTree(SearchWindowContext context) {
        List<SearchTreeEntry> entries = new List<SearchTreeEntry>();
        // 按文件创建搜索栏
        foreach (DSFile file in dataGraph.repository.GetSortedFiles()) {
            foreach (DSElement element in file.EnclosedElements) {
                if (element is not DSNamedType groupType || groupType.GetAnnotation("SearchTreeGroupEntry") == null) {
                    continue;
                }
                entries.Add(new SearchTreeGroupEntry(new GUIContent(groupType.SimpleName)));
                foreach (DSField field in groupType.GetFields(true, _filedListCache.ClearAndReturn())) {
                    DSNamedType filedType = (DSNamedType)field.Type;
                    VariableCfg variableCfg = dataGraph.GetVariableCfg(field);
                    GUIContent content = new GUIContent(GetDisplayName(filedType) + " : " + variableCfg.nodeFeatures);
                    entries.Add(new SearchTreeEntry(content) { level = 1, userData = field });
                }
            }
        }
        return entries;
    }

    internal bool OnSelectTypeEntry(SearchTreeEntry entry, SearchWindowContext context) {
        if (entry.userData is DSField field) {
            DSNamedType namedType = (DSNamedType)field.Type;
            DataNode dataNode = dataGraph.CreateNode(namedType);
            // 特征值在字段上
            VariableCfg variableCfg = dataGraph.GetVariableCfg(field);
            dataNode.folder = graphView.currentFolder;
            dataNode.features = variableCfg.nodeFeatures;
            dataNode.position = graphView.contentViewContainer.WorldToLocal(context.screenMousePosition);
            dataGraph.AddNode(dataNode);
            return true;
        }
        return false;
    }

    /// <summary>
    /// 创建NodeView
    ///
    /// 注：主要要为不同类型的Node分配不同的主题样式。
    /// </summary>
    public virtual NodeView CreateNodeView(DataNode dataNode) {
        return new NodeView();
    }

    /// <summary>
    /// 刷新Inspector
    /// </summary>
    public void RefreshNodeInspector() {
        if (selectedNode?.dataNode == null) {
            nodeHeaderView.SetEnabled(false);
            nodeValueView.SetEnabled(false);
            return;
        }
        nodeHeaderView.SetEnabled(true);
        nodeValueView.SetEnabled(true);

        DataNode dataNode = selectedNode.dataNode;
        DSNamedType valueType = dataNode.value.type;
        //
        localIdField.SetValueWithoutNotify(dataNode.localId);
        nameField.SetValueWithoutNotify(dataNode.name);
        folderField.SetValueWithoutNotify(dataNode.folder);
        titleField.SetValueWithoutNotify(dataNode.title);
        positionField.SetValueWithoutNotify(dataNode.position);
        typeSymbolField.SetValueWithoutNotify(GetDisplayName(valueType));
        enablePortToggle.SetValueWithoutNotify((dataNode.features & Features.EnablePort) != 0);
        //
        if (nodeValueView.childCount == 0) {
            VarObjectField objectField = GetObjectField(valueType);
            objectField.Bind(this, dataNode.value);
            nodeValueView.Add(objectField);
            objectField.label = "Node Value";
        } else {
            VarObjectField objectField = (VarObjectField)nodeValueView[0];
            if (objectField.buildType != valueType) {
                nodeValueView.RemoveAt(0);
                objectField.Unbind();
                //
                objectField = GetObjectField(valueType);
                objectField.Bind(this, dataNode.value);
                nodeValueView.Add(objectField);
            } else {
                objectField.Bind(this, dataNode.value);
            }
            objectField.label = "Node Value";
        }
    }

    private VarObjectField GetObjectField(DSNamedType valueType) {
        if (!objectFieldCache.TryGetValue(valueType, out VarObjectField objectField)) {
            objectField = new VarObjectField();
            objectFieldCache.Add(valueType, objectField);
        }
        return objectField;
    }

    #endregion
}
}