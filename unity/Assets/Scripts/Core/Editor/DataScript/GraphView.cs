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
using Wjybxx.Commons.Pool;

namespace Wjybxx.BigCat.CoreEditor.DataScript
{
public class GraphView : UnityEditor.Experimental.GraphView.GraphView
{
    public DataEditor editor { get; set; }
    public DataGraph dataGraph { get; set; }

    public GraphView() {
        this.Insert(0, new GridBackground()); // 网格背景
        this.AddManipulator(new ContentZoomer()); // 缩放
        this.AddManipulator(new ContentDragger()); // 画布拖拽
        this.AddManipulator(new SelectionDragger()); // 节点拖拽
        this.AddManipulator(new RectangleSelector()); // 框选
        //
        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/Core/Editor/DataScript/GraphView.uss");
        this.styleSheets.Add(styleSheet);

        // 视图和逻辑同步
        graphViewChanged = this.OnGraphViewChanged;
    }

    public void PopulateView() {
        graphViewChanged -= this.OnGraphViewChanged;
        DeleteElements(graphElements);
        //
        graphViewChanged += this.OnGraphViewChanged;
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
        // 节点删除会触发边删除吗？会，而且边的删除会在前面(可Debug查看顺序)
        if (viewChange.elementsToRemove != null) {
            List<Variable> disconnectedPorts = null;
            List<DataNode> deletedNodes = null;
            foreach (GraphElement graphElement in viewChange.elementsToRemove) {
                if (graphElement is Edge edge) {
                    // 动态端口在断开连接后主动删除
                    if (edge.input is PortView input) {
                        if (input.isDynamicPort) {
                            schedule.Execute(() => RemoveDynamicPort(input));
                        }
                    }
                    if (edge.output is PortView output) {
                        if (output.isDynamicPort) {
                            schedule.Execute(() => RemoveDynamicPort(output));
                        }
                        if (output.variable != null) {
                            disconnectedPorts ??= new List<Variable>();
                            disconnectedPorts.Add(output.variable);
                        }
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
                dataGraph.DeleteNodes(deletedNodes);
            }
        }
        // 如果连接的发起端是List类型，则创建一个动态Port，并将Edge的Output修改为动态Port -- 接入端处理相同
        if (viewChange.edgesToCreate != null) {
            for (int index = 0; index < viewChange.edgesToCreate.Count; index++) {
                Edge edge = viewChange.edgesToCreate[index];
                // 需要先在逻辑层建立连接
                LogicConnect(edge);
                if (edge.output is PortView output && output.isListPort && output.isExpanded) {
                    PortView dynamicPort = ReplaceWithDynamicPort(output);
                    dynamicPort.variable = output.variable?.TryPeekLast(); // Output需绑定变量
                    edge.output = dynamicPort;
                }
                if (edge.input is PortView input && input.isListPort && input.isExpanded) {
                    PortView dynamicPort = ReplaceWithDynamicPort(input);
                    edge.input = dynamicPort;
                }
            }
        }
        // 坐标更新，会先执行Node的SetPotion，再调用这里的方法 - 推荐在这里统一处理位置更新
        if (viewChange.movedElements != null) {
            foreach (GraphElement element in viewChange.movedElements) {
                if (element is NodeView nodeView && nodeView.dataNode != null) {
                    nodeView.dataNode.position = nodeView.transform.position;
                    nodeView.dataNode.ApplyModifiedProperties();
                }
            }
            editor.RefreshNodeInfo();
        }
        return viewChange;
    }

    private void LogicConnect(Edge edge) {
        if (edge.output is PortView output && output.variable != null
                                           && edge.input.node is NodeView inputNode
                                           && inputNode.dataNode != null) {
            dataGraph.Connect(output.variable, inputNode.dataNode);
        }
    }

    private PortView ReplaceWithDynamicPort(PortView port) {
        NodeView nodeView = (NodeView)port.node;
        Side side = nodeView.GetSide(port);
        PortView dynamicPort = nodeView.CreateDynamicPort(side);
        dynamicPort.listPort = port;
        nodeView.AddDynamicPort(dynamicPort, side);
        return dynamicPort;
    }

    private void RemoveDynamicPort(PortView port) {
        NodeView nodeView = (NodeView)port.node;
        Side side = nodeView.GetSide(port);
        port.RemoveFromHierarchy();
        nodeView.RefreshDynamicPorts(side); // 重新绑定数据
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
        // 允许连接到自身 - FSM状态切换合法
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
            NodeView nodeView = new NodeView();
            nodeView.transform.position = mousePos;
            AddElement(nodeView);
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