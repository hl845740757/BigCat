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
/// </summary>
public class DataEditor : EditorWindow
{
    protected GraphView graphView;
    protected VisualElement inspectorView;
    protected VisualElement nodeInfoView;
    protected VisualElement nodeValueView;
    // protected VisualElement portValueView; // port详细信息展示

    protected LongField localIdField;
    protected TextField nameField;
    protected TextField folderField;
    protected TextField commentField;

    public DataGraph model { get; private set; }
    public DsonTextWriterSettings writerSettings { get; set; }

    /// <summary>
    /// Key为菜单路径，Value为创建Node的模板元素
    /// TODO 增加NodeView绑定配置，从而实现不同样式。
    /// </summary>
    public readonly LinkedDictionary<string, DSNamedType> templates = new();
    /// <summary>
    /// 当前选中的节点 
    /// </summary>
    public NodeView selectedNode { get; set; }

    private DataNode _dataNode;

    [MenuItem("Window/UI Toolkit/DataEditor")]
    private static void OpenWindow() {
        DataEditor wnd = GetWindow<DataEditor>();
        wnd.titleContent = new GUIContent("DataEditor");
    }

    /// <summary>
    /// 
    /// </summary>
    protected virtual void OnEnable() {
        this.model = new DataGraph(new DSRepository());
        this.writerSettings = (DsonTextWriterSettings)new DsonTextWriterSettings.Builder()
        {
            NumberStyle = NumberStyle.Simple,
        }.Build();
        //
        model.undoPerformed += OnUndoRedoPerformed;
        model.redoPerformed += OnUndoRedoPerformed;

        string filePath = Application.dataPath + "/Resources/DataScript/data_script.ds";
        DSFile dsFile = DSFileParser.Parse(new FileInfo(filePath));
        model.repository.AddFile(dsFile);
        model.repository.Build();

        DSNamedType namedType = model.repository.GetType("OuterClass");
        _dataNode = model.CreateNode(namedType);
        model.AddNode(_dataNode);
    }

    /// <summary>
    /// 
    /// </summary>
    protected virtual void OnDisable() {
        model.undoPerformed -= OnUndoRedoPerformed;
        model.redoPerformed -= OnUndoRedoPerformed;
    }

    protected virtual void OnUndoRedoPerformed(List<DataNode> insertNodes,
                                               List<DataNode> deleteNodes,
                                               List<DataNode> updateNodes) {
        // Undo/Redo以后引用可能变更
        if (model.nodeDic.TryGetValue(_dataNode.localId, out DataNode existNode)) {
            _dataNode = existNode;
            if (nodeValueView.childCount <= 0) {
                return;
            }
            VisualElement element = nodeValueView[0];
            if (element is IVarField field) {
                field.Bind(this, _dataNode.value);
            }
            element.SetEnabled(true);
        } else {
            if (nodeValueView.childCount > 0) {
                nodeValueView[0].SetEnabled(false);
            }
        }
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
        graphView = root.Q<GraphView>();
        inspectorView = root.Q<VisualElement>("inspector-div");
        nodeInfoView = root.Q<VisualElement>("node-info");
        nodeValueView = root.Q<VisualElement>("node-value");
        graphView.editor = this;
        //
        localIdField = nodeInfoView.Q<LongField>("local-id");
        nameField = nodeInfoView.Q<TextField>("name");
        folderField = nodeInfoView.Q<TextField>("folder");
        commentField = nodeInfoView.Q<TextField>("comment");
        localIdField.isReadOnly = true;
        nameField.isDelayed = true;
        folderField.isDelayed = true;
        commentField.isDelayed = true;
        nameField.RegisterValueChangedCallback(OnNameFieldChanged);
        folderField.RegisterValueChangedCallback(OnFolderFieldChanged);
        commentField.RegisterValueChangedCallback(OnCommentFieldChanged);

        // 
        root.RegisterCallback<KeyDownEvent>(OnKeyDownEvent);

        // DEBUG
        nodeValueView.Add(DataEditorUtil.CreateField(_dataNode.value, this));
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
            // TODO 保存
        }
    }

    private void OnCommentFieldChanged(ChangeEvent<string> evt) {
        evt.StopPropagation();
        if (selectedNode == null) return;
        selectedNode.dataNode.comment = evt.newValue;
        selectedNode.dataNode.ApplyModifiedProperties();
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
        //
        selectedNode.title = evt.newValue;
    }

    /// <summary>
    /// 由于GraphView是支持框选的，但Inspector我们只展示其中一个
    /// </summary>
    /// <param name="nodeView"></param>
    public void OnNodeSelected(NodeView nodeView) {
        if (selectedNode != null) {
            return;
        }
        selectedNode = nodeView;
        nodeInfoView.SetEnabled(true);
        // 测试代码
        nodeView.dataNode = _dataNode;
        //
        DataNode dataNode = selectedNode.dataNode;
        localIdField.value = dataNode.localId;
        nameField.value = dataNode.name;
        folderField.value = dataNode.folder;
        commentField.value = dataNode.comment;
        //
        BuildNodeValueView();
    }

    private void BuildNodeValueView() {
        DataNode nodeData = selectedNode.dataNode;
        nodeValueView.SetEnabled(true);
        nodeValueView.Add(DataEditorUtil.CreateField(nodeData.value, this));
    }

    public void OnNodeUnselected(NodeView nodeView) {
        if (selectedNode != nodeView) {
            return;
        }
        selectedNode = null;
        nodeInfoView.SetEnabled(false);
        nodeValueView.SetEnabled(false);

        // 解除所有属性绑定
        nodeInfoView.Query<BindableElement>().ForEach(e => e.Unbind());
        nodeValueView.Clear();
    }
}
}