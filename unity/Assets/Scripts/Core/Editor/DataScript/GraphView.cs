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
using Wjybxx.BigCatTool.DataScript;
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Pool;

namespace Wjybxx.BigCat.CoreEditor.DataScript
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
        graphViewChanged += this.OnGraphViewChanged;
        _refreshStack++;
        try {
            DeleteElements(graphElements); // 在回调中解除数据层绑定
        }
        finally {
            _refreshStack--;
        }
        this.dataGraph = dataGraph;
        dataGraph.onGraphChanged -= this.OnDataGraphChanged;
        dataGraph.onGraphChanged += this.OnDataGraphChanged;
        this._nodeViewDic.Clear();
        Refresh();
    }

    private void OnDataGraphChanged(DataGraphChange _) {
        Refresh();
    }

    /// <summary>
    /// 刷新视图
    /// </summary>
    public void Refresh() {
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
                NodeView nodeView = editor.CreateNode(dataNode);
                nodeView.Bind(dataNode);
                _nodeViewDic[dataNode.localId] = nodeView;
                AddElement(nodeView);
            }
        }
    }

    private void RefreshEdges() {
        HashSet<Edge> deleteEdges = _edgeSetPool.Acquire();
        foreach (Edge edge in edges) {
            if (edge.input == null || edge.output == null) {
                deleteEdges.Add(edge);
                continue;
            }
            NodeView inputNode = (NodeView)edge.input.node;
            NodeView outputNode = (NodeView)edge.output.node;
            if (inputNode.dataNode == null || outputNode.dataNode == null) {
                deleteEdges.Add(edge); // 已被删除的Node
            }
        }
        // 通过Output或Input都可统计出无效边，基于Output可保证与数据的一致性
        foreach (NodeView nodeView in _nodeViewDic.Values) {
            RefreshOutputPorts(nodeView, Side.Left, deleteEdges);
            RefreshOutputPorts(nodeView, Side.Right, deleteEdges);
            RefreshOutputPorts(nodeView, Side.Bottom, deleteEdges);
        }
        if (deleteEdges.Count > 0) {
            DeleteElements(deleteEdges);
        }
        // 删除无效边以后才能更新输入端口
        foreach (NodeView nodeView in _nodeViewDic.Values) {
            RefreshInputPorts(nodeView);
        }
        _edgeSetPool.Release(deleteEdges);
    }

    private void RefreshInputPorts(NodeView nodeView) {
        VisualElement container = nodeView.GetDynamicPortContainer(Side.Top);
        if (container == null) {
            return;
        }
        int count = container.childCount;
        for (int portIndex = count - 1; portIndex >= 0; portIndex--) {
            PortView portView = (PortView)container[portIndex];
            if (!portView.connected) {
                portView.RemoveFromHierarchy();
            }
        }
    }

    private void RefreshOutputPorts(NodeView nodeView, Side side, HashSet<Edge> deleteEdges) {
        VisualElement container = nodeView.GetPortContainer(side);
        int count = container.childCount;
        for (int portIndex = 0; portIndex < count; portIndex++) {
            PortView portView = (PortView)container[portIndex];
            Variable outputField = portView.variable;
            if (!portView.isListPort) {
                ObjectPath objectPath = outputField.objectPathValue;
                if (!CheckConnection(objectPath, out DataNode inputNode)) {
                    if (portView.connectionList.Count > 0) {
                        deleteEdges.Add(portView.connectionList[0]);
                    }
                    continue;
                }
                NodeView inputNodeView = GetNodeView(inputNode);
                if (portView.connectionList.Count == 0) {
                    ConnectTo(portView, inputNodeView); // 创建连接
                    continue;
                }
                Edge edge = portView.connectionList[0];
                if (edge.input.node != inputNodeView) {
                    ConnectTo(portView, inputNodeView, edge); // 纠正连接
                }
                continue;
            }
            // List端口 - 删除多出的连接和动态端口，需要按索引迭代
            int conCount = portView.connectionList.Count;
            for (int index = conCount - 1; index >= outputField.Count; index--) {
                Edge edge = portView.connectionList[index];
                portView.Disconnect(edge);
                if (portView.isExpanded) {
                    PortView dynamicPort = (PortView)edge.output;
                    dynamicPort.DisconnectAll();
                    dynamicPort.Unbind();
                    dynamicPort.RemoveFromHierarchy();
                    dynamicPort.listPort = null;
                }
                deleteEdges.Add(edge);
            }
            // 补全连接数 - 数组对齐，多一次for循环，代码更清晰点
            for (int index = conCount; index < outputField.Count; index++) {
                Edge edge = new Edge();
                portView.Connect(edge);
                if (portView.isExpanded) {
                    PortView dynamicPort = NodeView.CreateDynamicPort(side);
                    dynamicPort.Connect(edge);
                    dynamicPort.listPort = portView;
                    // dynamicPort.Bind(outputField[index]); // 后面统一绑定数据
                    edge.output = dynamicPort;
                    nodeView.AddDynamicPort(dynamicPort, side);
                } else {
                    edge.output = portView;
                }
                AddElement(edge);
            }
            // 纠正连接
            for (int index = 0; index < outputField.Count; index++) {
                Variable nestedVar = outputField[index];
                Edge edge = portView.connectionList[index];
                // 可能由于重排序导致端口顺序变更
                if (portView.isExpanded) {
                    PortView dynamicPort = nodeView.GetDynamicPort(side, index);
                    dynamicPort.Bind(nestedVar);
                    if (edge.output != dynamicPort) {
                        edge.output = dynamicPort;
                    }
                } else {
                    if (edge.output != portView) {
                        edge.output = portView;
                    }
                }
                ObjectPath objectPath = nestedVar.objectPathValue;
                if (!CheckConnection(objectPath, out DataNode inputNode)) {
                    DisconnectInput(edge); // 不删除边，需要保持数组对齐
                    continue;
                }
                NodeView inputNodeView = GetNodeView(inputNode);
                if (edge.input != null // 可能是刚创建的空连接
                    && edge.input.node == inputNodeView) {
                    continue;
                }
                if (portView.isExpanded) {
                    PortView dynamicPort = nodeView.GetDynamicPort(side, index);
                    ConnectTo(dynamicPort, inputNodeView, edge);
                } else {
                    ConnectTo(portView, inputNodeView, edge);
                }
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

    private void DisconnectInput(Edge edge) {
        if (edge.input is PortView inputPort) {
            if (inputPort.isDynamicPort) {
                inputPort.DisconnectAll();
                inputPort.Unbind();
                inputPort.RemoveFromHierarchy();
                //
                inputPort.listPort.Disconnect(edge);
                inputPort.listPort = null;
            } else {
                inputPort.Disconnect(edge);
            }
        }
    }

    private void ConnectTo(PortView outputPort, NodeView inputNode, Edge edge) {
        Debug.Assert(edge.output == outputPort);
        DisconnectInput(edge);
        PortView inputPort = (PortView)inputNode.topInputs[0];
        if (inputPort.isExpanded) {
            PortView dynamicPort = NodeView.CreateDynamicPort(Side.Top);
            edge.input = dynamicPort;

            dynamicPort.listPort = inputPort;
            dynamicPort.Connect(edge); // 共享连接
            inputPort.Connect(edge);
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
            dynamicPort.listPort = inputPort;
            edge = outputPort.ConnectTo<Edge>(dynamicPort);
            inputPort.Connect(edge); // 共享连接
            //
            inputNode.AddDynamicPort(dynamicPort, Side.Top);
        } else {
            edge = outputPort.ConnectTo<Edge>(inputPort);
        }
        AddElement(edge);
        return edge;
    }

    /// <summary>
    /// 注：
    /// 1.此时尚未执行删除，可以在这里剔除不该删除的元素。
    /// 2.此时Edge也尚未真正添加到Graph，因此可以在这里纠正数据
    /// 3.更改连接目标先触发旧连接断开，下一次回调再触发新连接建立
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
                if (graphElement is Edge edge && edge.output is PortView output
                                              && output.variable != null) {
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
            .Where(endPort => endPort.direction != startPort.direction)
            .ToList();
    }

    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt) {
        base.BuildContextualMenu(evt);
        // TODO 提供搜索栏
        Vector2 mousePos = evt.localMousePosition;
        evt.menu.AppendSeparator();
        evt.menu.AppendAction("CreateNode", _ => {
            DSNamedType namedType = dataGraph.repository.GetType("OuterClass")!;
            DataNode dataNode = dataGraph.CreateNode(namedType);
            dataNode.features |= Features.EnablePort;
            dataNode.position = mousePos;
            dataGraph.AddNode(dataNode); // 事件回来再创建Node
        });
    }

    #region node事件

    public void OnNodeSelected(NodeView nodeView) {
        if (editor) editor.OnNodeSelected(nodeView);
    }

    public void OnNodeUnselected(NodeView nodeView) {
        if (editor) editor.OnNodeUnselected(nodeView);
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