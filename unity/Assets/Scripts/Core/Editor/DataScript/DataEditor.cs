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
    private GraphView _graphView;
    private VisualElement _inspectorView;
    private VisualElement _nodeInfoView;
    private VisualElement _nodeValueView;
    private VisualElement _portValueView; // port详细信息展示，用于排序
    
    public DataGraph model { get; private set; }
    public DsonTextWriterSettings writerSettings { get; set; }
    /// <summary>
    /// Key为菜单路径，Value为创建Node的模板元素
    /// TODO 增加NodeView绑定配置，从而实现不同样式。
    /// </summary>
    public readonly LinkedDictionary<string, DSNamedType> templates = new();

    public NodeView selectedNode { get; private set; }
    private DataNode _dataNode;

    [MenuItem("Window/UI Toolkit/DataGraphEditor")]
    private static void OpenWindow() {
        DataEditor wnd = GetWindow<DataEditor>();
        wnd.titleContent = new GUIContent("DataGraphEditor");
    }

    /// <summary>
    /// 
    /// </summary>
    protected virtual void OnEnable() {
        this.model = new DataGraph(new DSRepository());
        this.writerSettings = (DsonTextWriterSettings)new DsonTextWriterSettings.Builder()
        {
            NumberStyle = NumberStyles.Simple,
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

    protected void Update() {
        model.Update();
    }

    protected virtual void OnUndoRedoPerformed(List<DataNode> insertNodes, List<DataNode> deleteNodes,
                                               List<DataNode> updateNodes) {
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
        _graphView = root.Q<GraphView>();
        _inspectorView = root.Q<VisualElement>("inspector-div");
        _nodeInfoView = root.Q<VisualElement>("node-info");
        _nodeValueView = root.Q<VisualElement>("node-value");

        //
        _nodeValueView.Add(DataEditorUtil.CreateField(_dataNode.value, this));

        // 长生命周期字段初始化
        TextField nameField = _nodeInfoView.Q<MTextField>("name");
        nameField.RegisterValueChangedCallback(OnNameFieldChanged);
        //
        TextField folderField = _nodeInfoView.Q<MTextField>("folder");
        folderField.RegisterValueChangedCallback(OnFolderFieldChanged);
    }

    private void OnFolderFieldChanged(ChangeEvent<string> evt) {
        evt.StopPropagation();
        // TODO 刷新View
    }

    private void OnNameFieldChanged(ChangeEvent<string> evt) {
        evt.StopImmediatePropagation();
        if (selectedNode == null) return;
        selectedNode.dataNode.name = evt.newValue;
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

        DataNode nodeData = nodeView.dataNode;
        // _nodeInfoView.Q<TextField>("folder")
        //     .BindProperty(nodeData.serializedObject.FindProperty("_folder"));
        // _nodeInfoView.Q<TextField>("local-id")
        //     .BindProperty(nodeData.serializedObject.FindProperty("_localId"));
        // _nodeInfoView.Q<TextField>("comment")
        //     .BindProperty(nodeData.serializedObject.FindProperty("_comment"));
        // _nodeInfoView.Q<MVector2Field>("position")
        //     .BindProperty(nodeData.positionProperty);

        // name和folder有特殊逻辑
        _nodeInfoView.Q<MTextField>("name").value = nodeData.name;
        _nodeInfoView.Q<TextField>("folder").value = nodeData.folder;
        //
        BuildNodeValueView();
    }

    private void BuildNodeValueView() {
        DataNode nodeData = selectedNode.dataNode;
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