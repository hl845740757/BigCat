using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Wjybxx.BigCat.CoreEditor;
using Wjybxx.BigCat.CoreEditor.UIElements;
using Wjybxx.BigCatTool.DataScript;

namespace Wjybxx.BigCat.CoreEditor.DataScript
{
public class DataGraphEditor : EditorWindow
{
    private NodeGraphView _graphView;
    private VisualElement _inspectorView;
    private VisualElement _nodeInfoView;
    private VisualElement _nodeValueView;
    private VisualElement _portValueView; // port详细信息展示，用于排序

    public NodeView selectedNode { get; private set; }
    public DataEditorModel model { get; private set; }

    private NodeData _nodeData;

    [MenuItem("Window/UI Toolkit/DataGraphEditor")]
    public static void ShowExample() {
        DataGraphEditor wnd = GetWindow<DataGraphEditor>();
        wnd.titleContent = new GUIContent("DataGraphEditor");
    }

    private void OnEnable() {
        Undo.undoRedoPerformed += OnUndoExecuted;
        model = CreateInstance<DataEditorModel>(); // 不可在构造函数中直接构建其它脚本对象

        string filePath = Application.dataPath + "/Resources/DataScript/data_script.ds";
        DSFile dsFile = DSFileParser.Parse(new FileInfo(filePath));
        model.repository.AddFile(dsFile);
        model.repository.Build();

        _nodeData = CreateInstance<NodeData>();
        _nodeData.value = model.CreateVariable(model.repository.GetType("OuterClass"));
        _nodeData.serializedObject.Update();
        _nodeData.RebindValueProperty();
    }

    private void OnDisable() {
        Undo.undoRedoPerformed -= OnUndoExecuted;
        DestroyImmediate(model);
        DestroyImmediate(_nodeData);
    }

    private void OnUndoExecuted() {
        model.RepairNode(_nodeData);
        if (_nodeValueView.childCount > 0) {
            DataEditorUtil.Refresh(_nodeValueView[0], true);
        }
    }

    public void CreateGUI() {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        // Import UXML
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
            "Assets/Scripts/Core/Editor/DataScript/DataGraphEditor.uxml");
        VisualElement labelFromUXML = visualTree.Instantiate();
        root.Add(labelFromUXML);

        //
        _graphView = root.Q<NodeGraphView>();
        _inspectorView = root.Q<VisualElement>("inspector-div");
        _nodeInfoView = root.Q<VisualElement>("node-info");
        _nodeValueView = root.Q<VisualElement>("node-value");

        //
        _nodeValueView.Add(DataEditorUtil.CreateField(_nodeData.value, this));

        // 长生命周期字段初始化
        TextField nameField = _nodeInfoView.Q<MTextField>("name");
        nameField.RegisterValueChangedCallback(OnNameFieldChanged);
        //
        TextField folderField = _nodeInfoView.Q<MTextField>("folder");
        folderField.RegisterValueChangedCallback(OnFolderFieldChanged);
    }

    private void OnFolderFieldChanged(ChangeEvent<string> evt) {
        evt.StopPropagation();
        // TODO 将Node从当前Folder剔除，移入另一个文件夹
    }

    private void OnNameFieldChanged(ChangeEvent<string> evt) {
        evt.StopImmediatePropagation();
        if (selectedNode == null) return;
        selectedNode.nodeData.name = evt.newValue;
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
        _nodeInfoView.SetEnabled(true);

        NodeData nodeData = nodeView.nodeData;
        _nodeInfoView.Q<TextField>("folder")
            .BindProperty(nodeData.serializedObject.FindProperty("_folder"));
        _nodeInfoView.Q<TextField>("local-id")
            .BindProperty(nodeData.serializedObject.FindProperty("_localId"));
        _nodeInfoView.Q<TextField>("comment")
            .BindProperty(nodeData.serializedObject.FindProperty("_comment"));
        _nodeInfoView.Q<MVector2Field>("position")
            .BindProperty(nodeData.positionProperty);

        // name和folder有特殊逻辑
        _nodeInfoView.Q<MTextField>("name").value = nodeData.name;
        _nodeInfoView.Q<TextField>("folder").value = nodeData.folder;
        //
        BuildNodeValueView();
    }

    private void BuildNodeValueView() {
        NodeData nodeData = selectedNode.nodeData;
        if (nodeData.value == null) {
            _nodeValueView.SetEnabled(false);
            return;
        }
        _nodeValueView.SetEnabled(true);
        _nodeValueView.Add(DataEditorUtil.CreateField(nodeData.value, this));
    }

    public void OnNodeUnselected(NodeView nodeView) {
        if (selectedNode != nodeView) {
            return;
        }
        selectedNode = null;
        _nodeInfoView.SetEnabled(false);
        _nodeValueView.SetEnabled(false);

        // 解除所有属性绑定
        _nodeInfoView.Query<BindableElement>().ForEach(e => e.Unbind());
        _nodeValueView.Clear();
    }
}
}