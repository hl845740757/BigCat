#region LICENSE

// Copyright 2025 wjybxx(845740757@qq.com)
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Pool;

namespace Wjybxx.BigCat.Editor.DataScript
{
public class GraphView : UnityEditor.Experimental.GraphView.GraphView
{
    public DataEditor editor { get; set; }
    /// <summary>
    /// 绑定的数据图
    /// </summary>
    public DataGraph dataGraph { get; set; }
    /// <summary>
    /// 当前虚拟文件夹
    /// </summary>
    public string currentFolder { get; set; }
    /// <summary>
    /// 当前所有Node
    ///
    /// TODO 是否为Folder维护额外的缓存？目前来说非必须
    /// </summary>
    private readonly LinkedDictionary<long, NodeView> _nodeViewDic = new LinkedDictionary<long, NodeView>();

    private IVisualElementScheduledItem refreshTask;
    private int _refreshStack;
    private readonly ObjectPool<HashSet<Edge>> _edgeSetPool = ObjectPoolUtil.NewHashSetPool<Edge>(4);

    public GraphView() {
        this.Insert(0, new GridBackground()); // 网格背景
        this.AddManipulator(new ContentZoomer()); // 缩放
        this.AddManipulator(new ContentDragger()); // 画布拖拽
        this.AddManipulator(new SelectionDragger()); // 节点拖拽
        this.AddManipulator(new RectangleSelector()); // 框选
        //
        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/Core/Editor/DataScript/GraphView.uss");
        this.styleSheets.Add(styleSheet);
    }

    public void Bind(DataGraph dataGraph) {
        graphViewChanged -= this.OnGraphViewChanged;
        DeleteElements(graphElements);
        graphViewChanged += this.OnGraphViewChanged;
        //
        this.dataGraph = dataGraph;
        this.currentFolder = null;
        dataGraph.onGraphChanged -= this.OnDataGraphChanged;
        dataGraph.onGraphChanged += this.OnDataGraphChanged;
        this._nodeViewDic.Clear();
        this.refreshTask ??= schedule.Execute(Refresh);
        this.refreshTask.Resume();
    }

    public void Unbind() {
        if (dataGraph == null) return;
        dataGraph.onGraphChanged -= this.OnDataGraphChanged;
    }

    private void OnDataGraphChanged(DataGraphChange _) {
        // 不能立即刷新，OnViewChanged修改数据层，此时View还有部分逻辑没有执行完...
        refreshTask.Resume();
    }

    /// <summary>
    /// 刷新视图
    /// </summary>
    public void Refresh() {
        refreshTask.Pause();
        _refreshStack++;
        try {
            RefreshNodes();
            RefreshEdges();
        }
        finally {
            _refreshStack--;
        }
    }

    private NodeView GetNodeView(DataNode node) {
        _nodeViewDic.TryGetValue(node.localId, out NodeView view);
        return view;
    }

    private void RefreshNodes() {
        // 删除遗留节点
        List<NodeView> deletedNodes = null;
        List<KeyValuePair<long, NodeView>> idChangedNodes = null;
        using var itr = _nodeViewDic.GetEnumerator();
        while (itr.MoveNext()) {
            long localId = itr.Current.Key;
            NodeView nodeView = itr.Current.Value;
            DataNode dataNode = nodeView.dataNode;
            if (dataGraph.Contains(dataNode)
                && dataNode.folder == currentFolder) {
                // id可能变更 
                if (dataNode.localId != localId) {
                    idChangedNodes ??= new List<KeyValuePair<long, NodeView>>();
                    idChangedNodes.Add(new KeyValuePair<long, NodeView>(localId, nodeView));
                }
                nodeView.Refresh();
            } else {
                deletedNodes ??= new List<NodeView>();
                deletedNodes.Add(nodeView);
                itr.Remove();
            }
        }
        if (idChangedNodes != null) {
            foreach (var pair in idChangedNodes) {
                NodeView nodeView = pair.Value;
                _nodeViewDic.Remove(pair.Key);
                _nodeViewDic[nodeView.dataNode.localId] = nodeView;
            }
        }
        if (deletedNodes != null) {
            DeleteElements(deletedNodes);
        }
        // 创建缺失的节点
        foreach (DataNode dataNode in dataGraph.nodeList) {
            if (dataNode.folder != currentFolder) {
                continue;
            }
            if (!_nodeViewDic.ContainsKey(dataNode.localId)) {
                NodeView nodeView = editor.CreateNodeView(dataNode);
                nodeView.Bind(dataNode);
                _nodeViewDic[dataNode.localId] = nodeView;
                AddElement(nodeView);
            }
        }
    }

    private void RefreshEdges() {
        // 通过Output统计无效边
        foreach (NodeView nodeView in _nodeViewDic.Values) {
            RefreshOutputPorts(nodeView, Side.Left);
            RefreshOutputPorts(nodeView, Side.Right);
            RefreshOutputPorts(nodeView, Side.Bottom);
        }
        // output为null为无效边
        HashSet<Edge> deleteEdges = _edgeSetPool.Acquire();
        foreach (Edge edge in edges) {
            if (edge.output == null) {
                edge.input = null;
                deleteEdges.Add(edge);
                continue;
            }
            NodeView inputNode = (NodeView)edge.input.node;
            NodeView outputNode = (NodeView)edge.output.node;
            if (inputNode.dataNode == null || outputNode.dataNode == null) {
                edge.output = null;
                edge.input = null;
                deleteEdges.Add(edge);
            }
        }
        if (deleteEdges.Count > 0) {
            DeleteElements(deleteEdges);
        }
        // 由于显示层可能提前解除了Input的引用，导致我们必须迭代输入端口
        foreach (NodeView nodeView in _nodeViewDic.Values) {
            RefreshInputPorts(nodeView);
        }
        _edgeSetPool.Release(deleteEdges);
    }

    private void RefreshInputPorts(NodeView nodeView) {
        PortView inputPort = (PortView)nodeView.topInputs[0];
        VisualElement dynamicPortContainer = nodeView.GetDynamicPortContainer(Side.Top);
        for (int index = inputPort.connectionCount - 1; index >= 0; index--) {
            Edge edge = inputPort.connectionList[index];
            // output为null，表示不可达无效边，或拖拽导致的无效边
            if (inputPort.isExpanded) {
                PortView dynamicPort = (PortView)dynamicPortContainer[index];
                if (edge.output != null && edge.input == dynamicPort) {
                    continue;
                }
                inputPort.Disconnect(edge);
                dynamicPort.DisconnectAll();
                dynamicPort.Unbind();
                dynamicPort.listPort = null;
                dynamicPortContainer.RemoveAt(index);
            } else {
                if (edge.output != null && edge.input == inputPort) {
                    continue;
                }
                inputPort.Disconnect(edge);
            }
        }
    }

    /// <summary>
    /// 刷新输出端口
    ///
    /// 注：将无效边的Output置为null（断开连接）
    /// </summary>
    private void RefreshOutputPorts(NodeView nodeView, Side side) {
        VisualElement container = nodeView.GetPortContainer(side);
        int portCount = container.childCount;
        for (int portIndex = 0; portIndex < portCount; portIndex++) {
            PortView portView = (PortView)container[portIndex];
            Variable outputField = portView.variable;
            if (!portView.isListPort) {
                ObjectPath objectPath = outputField.objectPathValue;
                Edge edge;
                if (!CheckConnection(objectPath, out DataNode inputNode)) {
                    if (portView.connectionCount > 0) {
                        edge = portView.connectionList[0];
                        FixOutputPort(edge, portView);
                        DisconnectOutput(edge);
                    }
                    continue;
                }
                NodeView inputNodeView = GetNodeView(inputNode);
                if (portView.connectionList.Count == 0) {
                    ConnectTo(portView, inputNodeView); // 创建连接
                    continue;
                }
                edge = portView.connectionList[0];
                FixOutputPort(edge, portView);
                if (edge.input == null || edge.input.node != inputNodeView) {
                    ConnectTo(portView, inputNodeView, edge); // 纠正连接
                }
                continue;
            }
            VisualElement dynamicPortContainer = nodeView.GetDynamicPortContainer(side);
            for (int index = 0; index < outputField.Count; index++) {
                Edge edge;
                if (index >= portView.connectionCount) {
                    edge = new Edge();
                    if (portView.isExpanded) {
                        PortView dynamicPort = NodeView.CreateDynamicPort(side);
                        edge.output = dynamicPort;
                        dynamicPort.Connect(edge);
                        portView.Connect(edge); // 共享连接
                        dynamicPort.listPort = portView;
                        dynamicPortContainer.Add(dynamicPort);
                    } else {
                        edge.output = portView;
                        portView.Connect(edge);
                    }
                    AddElement(edge);
                } else {
                    edge = portView.connectionList[index];
                }
                Variable nestedVar = outputField[index];
                if (portView.isExpanded) {
                    PortView dynamicPort = (PortView)dynamicPortContainer[index];
                    dynamicPort.Bind(nestedVar);
                    FixOutputPort(edge, dynamicPort);
                } else {
                    FixOutputPort(edge, portView);
                }
                ObjectPath objectPath = nestedVar.objectPathValue;
                if (!CheckConnection(objectPath, out DataNode inputNode)) {
                    DisconnectInput(edge); // 不能删除边，需要保持数组长度一致
                    continue;
                }
                NodeView inputNodeView = GetNodeView(inputNode);
                if (edge.input != null // 可能是刚创建的空连接
                    && edge.input.node == inputNodeView) {
                    continue;
                }
                if (portView.isExpanded) {
                    PortView dynamicPort = (PortView)dynamicPortContainer[index];
                    ConnectTo(dynamicPort, inputNodeView, edge);
                } else {
                    ConnectTo(portView, inputNodeView, edge);
                }
            }
            // 删除多于的连接和动态端口
            for (int index = portView.connectionCount - 1; index >= outputField.Count; index--) {
                Edge edge = portView.connectionList[index];
                if (portView.isExpanded) {
                    PortView dynamicPort = (PortView)dynamicPortContainer[index];
                    FixOutputPort(edge, dynamicPort);
                } else {
                    FixOutputPort(edge, portView);
                }
                DisconnectOutput(edge);
            }
        }
    }

    /// <summary>
    /// 检查连接有效性
    /// </summary>
    private bool CheckConnection(ObjectPath objectPath, out DataNode inputNode) {
        if (dataGraph.GetReferenceNode(objectPath, out inputNode)
            && inputNode.folder == currentFolder) { // 不绘制跨folder连接
            return true;
        }
        inputNode = null;
        return false;
    }

    private static void DisconnectInput(Edge edge) {
        if (edge.input is PortView portView) {
            Disconnect(edge, portView);
            edge.input = null;
        }
    }

    private static void DisconnectOutput(Edge edge) {
        if (edge.output is PortView portView) {
            Disconnect(edge, portView);
            edge.output = null;
        }
    }

    private static void Disconnect(Edge edge, PortView port) {
        if (port.isDynamicPort) {
            port.DisconnectAll();
            port.Unbind();
            port.RemoveFromHierarchy();
            //
            port.listPort.Disconnect(edge);
            port.listPort = null;
        } else {
            port.Disconnect(edge);
        }
    }

    private static void FixOutputPort(Edge edge, PortView outputPort) {
        // 端口拖拽删除情况下，显示层会将Output/Input置为null
        if (edge.output == null) {
            edge.output = outputPort;
        }
    }

    private void ConnectTo(PortView outputPort, NodeView inputNode, Edge edge) {
        Debug.Assert(edge.output == outputPort);
        DisconnectInput(edge);
        PortView inputPort = (PortView)inputNode.topInputs[0];
        if (inputPort.isExpanded) {
            PortView dynamicPort = NodeView.CreateDynamicPort(Side.Top);
            edge.input = dynamicPort;
            dynamicPort.Connect(edge);
            inputPort.Connect(edge); // 共享连接
            dynamicPort.listPort = inputPort;
            inputNode.AddDynamicPort(dynamicPort, Side.Top);
        } else {
            edge.input = inputPort;
            inputPort.Connect(edge);
        }
    }

    private Edge ConnectTo(PortView outputPort, NodeView inputNode) {
        PortView inputPort = (PortView)inputNode.topInputs[0];
        Edge edge;
        if (inputPort.isExpanded) {
            PortView dynamicPort = NodeView.CreateDynamicPort(Side.Top);
            edge = outputPort.ConnectTo<Edge>(dynamicPort);
            inputPort.Connect(edge); // 共享连接
            dynamicPort.listPort = inputPort;
            inputNode.AddDynamicPort(dynamicPort, Side.Top);
        } else {
            edge = outputPort.ConnectTo<Edge>(inputPort);
        }
        AddElement(edge);
        return edge;
    }

    /// <summary>
    /// 注：
    /// 1.此时尚未执行删除，可以在这里剔除不该删除的元素
    /// 2.此时Edge也尚未真正添加到Graph，因此可以在这里纠正数据
    /// 3.更改连接目标先触发旧连接断开，下一次回调再触发新连接建立
    /// 4.拖拽断开边的情况下，即使清理了要删除的元素，回调方法返回后，仍会被清理掉Output和Input的引用...
    /// </summary>
    /// <param name="viewChange"></param>
    /// <returns></returns>
    private GraphViewChange OnGraphViewChanged(GraphViewChange viewChange) {
        if (_refreshStack > 0) {
            if (viewChange.elementsToRemove != null) {
                UnbindRemovedElements(viewChange.elementsToRemove);
            }
            return viewChange;
        }
        // 节点删除会触发边删除吗？会，而且边的删除会在前面(可Debug查看顺序)
        dataGraph.BeginModify();
        try {
            if (viewChange.elementsToRemove == null) {
                goto checkCreate;
            }
            List<Variable> disconnectedPorts = null;
            List<DataNode> deletedNodes = null;
            foreach (GraphElement graphElement in viewChange.elementsToRemove) {
                if (graphElement is Edge edge && edge.output is PortView output) {
                    disconnectedPorts ??= new List<Variable>();
                    if (output.isListPort) {
                        int index = output.connectionList.IndexOf(edge);
                        disconnectedPorts.Add(output.variable[index]);
                    } else {
                        disconnectedPorts.Add(output.variable);
                    }
                    continue;
                }
                if (graphElement is NodeView nodeView && nodeView.dataNode != null) {
                    deletedNodes ??= new List<DataNode>();
                    deletedNodes.Add(nodeView.dataNode);
                }
            }
            // 先执行边的删除
            if (disconnectedPorts != null) {
                dataGraph.Disconnect(disconnectedPorts);
            }
            if (deletedNodes != null) {
                dataGraph.DeleteNodes(deletedNodes, true, true);
            }
            //
            checkCreate:
            if (viewChange.edgesToCreate != null) {
                foreach (Edge edge in viewChange.edgesToCreate) {
                    PortView output = (PortView)edge.output;
                    NodeView inputNode = (NodeView)edge.input.node;
                    if (output.variable != null && inputNode.dataNode != null) {
                        dataGraph.Connect(output.variable, inputNode.dataNode);
                    }
                }
            }
            //
            if (viewChange.movedElements != null) {
                foreach (GraphElement element in viewChange.movedElements) {
                    if (element is NodeView nodeView && nodeView.dataNode != null) {
                        Vector2 position = nodeView.dataNode.position + viewChange.moveDelta;
                        position.x = (int)position.x; // 避免超长的浮点数...
                        position.y = (int)position.y;
                        nodeView.dataNode.position = position;
                        nodeView.dataNode.ApplyModifiedProperties();
                    }
                }
            }
            viewChange.edgesToCreate?.Clear();
            viewChange.elementsToRemove?.Clear();
            viewChange.movedElements?.Clear();
            return viewChange;
        }
        finally {
            dataGraph.EndModify();
        }
    }

    private static void UnbindRemovedElements(List<GraphElement> elementsToRemove) {
        foreach (GraphElement graphElement in elementsToRemove) {
            if (graphElement is NodeView nodeView) {
                nodeView.dataNode = null;
            }
        }
    }

    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter) {
        // 动态端口可以连接到自身区域的动态端口 -- 用于交换顺序，右键移动太过繁琐
        // 动态端口无法简单更改连接目标，始终会先触发Delete事件
        if (startPort is PortView port && port.isDynamicPort) {
            return this.ports.ToList()
                .Where(endPort => endPort != startPort
                                  && endPort.hierarchy.parent == startPort.hierarchy.parent)
                .ToList();
        }
        // 允许连接到自身 - FSM常见
        return this.ports.ToList()
            .Where(endPort => {
                if (endPort is PortView portView && portView.isDynamicPort) {
                    return false;
                }
                return endPort.direction != startPort.direction;
            })
            .ToList();
    }

    protected override bool canCopySelection => SelectionContainsNode();
    protected override bool canCutSelection => SelectionContainsNode();
    protected override bool canDuplicateSelection => SelectionContainsNode();

    private bool SelectionContainsNode() {
        return selection.Any(e => e is NodeView);
    }

    public void RefreshSelection(IEnumerable<DataNode> dataNodes) {
        ClearSelection();
        foreach (DataNode dataNode in dataNodes) {
            NodeView nodeView = GetNodeView(dataNode);
            if (nodeView != null) AddToSelection(nodeView);
        }
    }

    #region node事件

    public void OnNodeSelected(NodeView nodeView) {
        if (editor) editor.OnNodeSelected(nodeView);
    }

    public void OnNodeUnselected(NodeView nodeView) {
        if (editor) editor.OnNodeUnselected(nodeView);
    }

    public void OnNodeExecuteRequest(NodeView nodeView) {
        if (editor) editor.OnNodeExecuteRequest(nodeView);
    }

    public DropdownMenuAction.Status GetExecuteActionStatus(NodeView nodeView) {
        return editor && editor.IsExecutable(nodeView)
            ? DropdownMenuAction.Status.Normal
            : DropdownMenuAction.Status.Disabled;
    }

    #endregion

    #region uxml

    public new class UxmlFactory : UxmlFactory<GraphView, UxmlTraits>
    {
    }

    public new class UxmlTraits : VisualElement.UxmlTraits
    {
        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc) {
            base.Init(ve, bag, cc);
            var myView = (GraphView)ve;
        }
    }

    #endregion
}
}