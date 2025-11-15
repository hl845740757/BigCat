using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Wjybxx.BigCat.CoreEditor;
using Wjybxx.BigCat.CoreEditor.UIElements;
using Wjybxx.BigCatTool.DataScript;
using Wjybxx.Commons.Collections;
using Wjybxx.Dson.Text;

namespace Wjybxx.BigCat.CoreEditor.DataScript
{
/// <summary>
/// 该类是一个模板基类，其它编辑器可以基于此进行即可
///
/// TODO 提起NodeInspector 
/// </summary>
public class DataEditor : EditorWindow
{
    protected Toolbar toolbar;
    protected GraphView graphView;
    protected VisualElement inspectorView;
    protected VisualElement nodeHeaderView;
    protected VisualElement nodeValueView;
    // protected VisualElement portValueView; // port详细信息展示

    protected LongField localIdField; // 其实Node也可以通过一个Variable绑定UI
    protected TextField folderField;
    protected TextField nameField;
    protected TextField titleField;
    protected Vector2Field positionField;
    protected Toggle enablePortToggle; // Pair类型启用端口

    /// <summary>
    /// 需要主动构建<see cref="DSRepository"/>
    /// </summary>
    public DataGraph model { get; set; }
    /// <summary>
    /// 当前选中的节点
    /// </summary>
    public NodeView selectedNode { get; set; }

    /// <summary>
    /// 用户应该在自己的静态方法中初始化该Window的依赖，主要是初始化DataScript文件
    /// </summary>
    [MenuItem("Window/BigCat/DataEditor")]
    private static void OpenWindow() {
        DataEditor wnd = GetWindow<DataEditor>();
        wnd.titleContent = new GUIContent("DataGraphEditor");
    }

    /// <summary>
    /// 
    /// </summary>
    protected virtual void OnEnable() {
        model = new DataGraph(new DSRepository());
        model.undoPerformed += OnUndoRedoPerformed;
        model.redoPerformed += OnUndoRedoPerformed;
        model.onGraphChanged += OnDataGraphChanged;
        // TODO 
        string filePath = Application.dataPath + "/Resources/DataScript/data_script.ds";
        DSFile dsFile = DSFileParser.Parse(new FileInfo(filePath));
        model.repository.AddFile(dsFile);
        model.repository.Build();
    }

    /// <summary>
    /// 
    /// </summary>
    protected virtual void OnDisable() {
        model.undoPerformed -= OnUndoRedoPerformed;
        model.redoPerformed -= OnUndoRedoPerformed;
        model.onGraphChanged -= OnDataGraphChanged;
    }

    protected virtual void OnUndoRedoPerformed(DataGraphChange _) {
        graphView.Refresh();
        RefreshNodeInspector();
    }

    protected virtual void OnDataGraphChanged(DataGraphChange _) {
        // GraphView内部会监听，其实多数情况下都可以不刷新整个Inspector，只刷新基础信息部分
        RefreshNodeInspector();
    }

    protected void Update() {
        model.Update();
    }

    public void CreateGUI() {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;
        // Import UXML
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
            "Assets/Scripts/Core/Editor/DataScript/DataEditor.uxml");
        VisualElement labelFromUXML = visualTree.Instantiate();
        root.Add(labelFromUXML);
        //
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
        // 
        graphView.Bind(model);
    }

    /// <summary>
    /// 主要处理控制事件，不建议停止事件传播
    /// </summary>
    /// <param name="evt"></param>
    protected virtual void OnKeyDownEvent(KeyDownEvent evt) {
        if (!evt.ctrlKey || evt.shiftKey) return;
        if (evt.keyCode == KeyCode.Z) {
            model.Undo();
        } else if (evt.keyCode == KeyCode.Y) {
            model.Redo();
        } else if (evt.keyCode == KeyCode.S) {
            model.Save();
        }
    }

    private void OnClickCloseFile(ClickEvent evt) {
        evt.StopPropagation();
        model.Close();
        model.assetPath = null;
        //
        selectedNode = null;
        graphView.Bind(model);
        RefreshNodeInspector();
    }

    private void OnClickOpenFile(ClickEvent evt) {
        evt.StopPropagation();
        string filePath = EditorUtility.OpenFilePanel("选择资产文件", UnityEditorUtil.lastOpenFolder, "dson");
        if (string.IsNullOrEmpty(filePath)) {
            return;
        }
        string assetPath = UnityEditorUtil.ConvertToAssetPath(filePath);
        UnityEditorUtil.lastOpenFolder = UnityEditorUtil.GetAssetFolderPath(assetPath);
        toolbar.Q<MTextField>("asset-path").SetValueWithoutNotify(assetPath);
        //
        model.assetPath = assetPath;
        model.Load();
        //
        selectedNode = null;
        graphView.Bind(model);
        RefreshNodeInspector();
    }

    #region node-info

    private void OnPositionFiledChanged(ChangeEvent<Vector2> evt) {
        evt.StopPropagation();
        if (selectedNode == null) return;
        selectedNode.dataNode.position = evt.newValue;
        selectedNode.dataNode.ApplyModifiedProperties();
    }

    private void OnTitleFieldChanged(ChangeEvent<string> evt) {
        evt.StopPropagation();
        if (selectedNode == null) return;
        selectedNode.dataNode.title = evt.newValue;
        selectedNode.dataNode.ApplyModifiedProperties();
    }

    private void OnEnablePortChanged(ChangeEvent<bool> evt) {
        evt.StopPropagation();
        if (selectedNode == null) return;
        DataNode dataNode = selectedNode.dataNode;
        if ((dataNode.features & Features.EnablePort) != 0) return;
        if (!dataNode.value.isPariType) return; // 该功能为Pair类型设计的
        dataNode.features |= Features.EnablePort;
        model.InitOutputFields(dataNode);
        dataNode.ApplyModifiedProperties();
    }

    private void OnFolderFieldChanged(ChangeEvent<string> evt) {
        evt.StopPropagation();
        if (selectedNode == null) return;
        selectedNode.dataNode.folder = evt.newValue;
        selectedNode.dataNode.ApplyModifiedProperties();
        // TODO 刷新View
    }

    private void OnNameFieldChanged(ChangeEvent<string> evt) {
        evt.StopPropagation();
        if (selectedNode == null) return;
        selectedNode.dataNode.name = evt.newValue;
        selectedNode.dataNode.ApplyModifiedProperties();
    }

    private void OnLocalIdFieldChanged(ChangeEvent<long> evt) {
        if (selectedNode == null) return;
        if (model.nodeDic.ContainsKey(evt.newValue)) {
            Debug.LogWarning("localId is duplicated: " + evt.newValue);
            localIdField.SetValueWithoutNotify(evt.previousValue);
            return;
        }
        selectedNode.dataNode.localId = evt.newValue;
        selectedNode.dataNode.ApplyModifiedProperties();
    }

    #endregion

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
    /// 创建NodeView
    ///
    /// 注：主要要为不同类型的Node分配不同的主题样式。
    /// </summary>
    public virtual NodeView CreateNode(DataNode dataNode) {
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
        localIdField.SetValueWithoutNotify(dataNode.localId);
        nameField.SetValueWithoutNotify(dataNode.name);
        folderField.SetValueWithoutNotify(dataNode.folder);
        titleField.SetValueWithoutNotify(dataNode.title);
        positionField.SetValueWithoutNotify(dataNode.position);
        if (nodeValueView.childCount == 0) {
            nodeValueView.Add(DataEditorUtil.CreateField(dataNode.value, this));
        } else {
            DataEditorUtil.Bind(nodeValueView[0], dataNode.value, this);
        }
    }
}
}