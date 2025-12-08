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
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UnityGraphView = UnityEditor.Experimental.GraphView.GraphView;

namespace Wjybxx.BigCat.Editor.DataScript
{
/// <summary>
/// 数据端口视图
///
/// 注意：Port不能尝试更新数据模型，因为建立连接和断开连接涉及多个对象，因此不属于它们任意一方的职责。
/// TODO :视图需要设计多个状态（颜色）：连接为空、目标对象在同Folder内，目标对象在其它Folder。
/// </summary>
public class PortView : Port
{
    /// <summary>
    /// 关联的字段
    ///
    /// 注；Input端口不绑定变量
    /// </summary>
    public Variable variable { get; private set; }
    /// <summary>
    /// 是否是List类型端口（缓存值）
    /// </summary>
    public bool isListPort => capacity == Capacity.Multi;
    /// <summary>
    /// List类型端口是否处于展开状态
    /// </summary>
    public bool isExpanded { get; set; }

    /// <summary>
    /// 是否是动态端口
    /// </summary>
    public bool isDynamicPort { get; internal set; }
    /// <summary>
    /// 动态端口关联的List端口
    /// </summary>
    public PortView listPort { get; set; }
    /// <summary>
    /// 用于保证顺序
    ///
    /// 注：虽然Unity不支持Output到Output，Input到Input的连接；
    /// 但Port的<see cref="Connect"/>并没有特殊逻辑，仅仅是维护Edge的集合；
    /// 因此可以多个Port对同一个Edge调用Connect，因此List端口展开的情况下和动态端口共享连接。
    /// </summary>
    public readonly List<Edge> connectionList = new List<Edge>();

    private PortView(Orientation portOrientation,
                     Direction portDirection,
                     Capacity portCapacity,
                     Type type)
        : base(portOrientation, portDirection, portCapacity, type) {
        RegisterCallback<MouseDownEvent>(ShowContextMenu, TrickleDown.TrickleDown);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Bind(Variable variable) {
        Unbind();
        this.variable = variable;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Unbind() {
        this.variable = null;
    }

    public int connectionCount => connectionList.Count;

    private void ShowContextMenu(MouseDownEvent evt) {
        // 同ListView的问题，GraphView拦截了ContextClickEvent...
        if (evt.button != (int)MouseButton.RightMouse) return;
        evt.StopPropagation();
        GenericMenu menu = new GenericMenu();
        menu.AddDisabledItem(new GUIContent("Port:" + portName));
        menu.AddSeparator("");
        //
        int connectionCount = connectionList.Count;
        if (connectionCount > 0) {
            menu.AddItem(new GUIContent("DisconnectAll: " + connectionCount), false, OnClickDisconnectAll);
        } else {
            menu.AddDisabledItem(new GUIContent("DisconnectAll"), false);
        }
        // List展开/折叠
        if (isListPort) {
            if (isExpanded) {
                menu.AddItem(new GUIContent("Collapse"), false, () => {
                    NodeView nodeView = (NodeView)node;
                    nodeView.CollapseListPort(this);
                });
                menu.AddDisabledItem(new GUIContent("Expand"), true);
            } else {
                menu.AddDisabledItem(new GUIContent("Collapse"), true);
                menu.AddItem(new GUIContent("Expand"), false, () => {
                    NodeView nodeView = (NodeView)node;
                    nodeView.ExpandListPort(this);
                });
            }
            // TODO 增加按照X/Y坐标排序功能
        }
        menu.ShowAsContext();
    }

    private void OnClickDisconnectAll() {
        GetFirstAncestorOfType<GraphView>().DeleteElements(connections);
    }

    /// <summary>
    /// 通过右键菜单移动实在麻烦，因此我们在连接的时候进行移动
    /// </summary>
    private void MoveTo(Port destPort) {
        if (destPort == this) {
            return;
        }
        VisualElement container = hierarchy.parent;
        int srcIndex = -1;
        int destIndex = -1;
        for (int index = 0; index < container.childCount; index++) {
            PortView portView = (PortView)container[index];
            if (destIndex < 0 && portView == destPort) {
                destIndex = index;
            }
            if (srcIndex < 0 && portView == this) {
                srcIndex = index;
            }
            if (srcIndex >= 0 && destIndex >= 0) {
                break;
            }
        }
        if (destIndex < 0) {
            return;
        }
        Variable listVariable = listPort.variable;
        if (listVariable != null) {
            listVariable.MoveTo(srcIndex, destIndex);
            listVariable.ApplyModifiedProperties(); // 回调时刷新
        } else {
            Edge edge = listPort.connectionList[srcIndex];
            listPort.connectionList.RemoveAt(srcIndex);
            listPort.connectionList.Insert(destIndex, edge);
            //
            container.RemoveAt(srcIndex);
            container.Insert(destIndex, this);
        }
    }

    #region internal

    public override void Connect(Edge edge) {
        base.Connect(edge);
        if (!connectionList.Contains(edge)) {
            connectionList.Add(edge);
        }
    }

    public override void Disconnect(Edge edge) {
        base.Disconnect(edge);
        connectionList.Remove(edge);
    }

    public override void DisconnectAll() {
        base.DisconnectAll();
        connectionList.Clear();
    }

    public override bool ContainsPoint(Vector2 localPoint) {
        Rect portLayout = this.layout;
        Rect conLayout = this.m_ConnectorBox.layout; // 连接盒在Port内的坐标
        Rect labelLayout = this.m_ConnectorText.layout; // Label在Port的坐标
        Rect rect = default;
        switch (style.flexDirection.value) {
            case FlexDirection.Row: {
                // ConnectorBox在左侧，Label在右侧
                float width = labelLayout.xMin - 1;
                float height = portLayout.height;
                rect = new Rect(0, 0, width, height);
                break;
            }
            case FlexDirection.RowReverse: {
                // ConnectorBox在右侧，Label在左侧
                float x = labelLayout.xMax + 1;
                float width = portLayout.width - x;
                float height = portLayout.height;
                rect = new Rect(x, 0, width, height);
                break;
            }
            case FlexDirection.Column: {
                // ConnectorBox在上方，Label在下方
                float width = portLayout.width;
                float height = conLayout.height;
                rect = new Rect(0, 0, width, height);
                break;
            }
            case FlexDirection.ColumnReverse: {
                // ConnectorBox在下方，Label在上方
                float y = labelLayout.yMax + 1;
                float width = portLayout.width;
                float height = portLayout.height - y;
                rect = new Rect(0, y, width, height);
                break;
            }
        }
        return rect.Contains(localPoint);
    }

    public new static PortView Create<TEdge>(Orientation orientation,
                                             Direction direction,
                                             Capacity capacity,
                                             Type type = null) where TEdge : Edge, new() {
        DefaultEdgeConnectorListener listener = new DefaultEdgeConnectorListener();
        PortView ele = new PortView(orientation, direction, capacity, type)
        {
            m_EdgeConnector = new EdgeConnector<TEdge>(listener)
        };
        ele.AddManipulator(ele.m_EdgeConnector);
        listener.m_PortView = ele;
        return ele;
    }

    private class DefaultEdgeConnectorListener : IEdgeConnectorListener
    {
        private GraphViewChange m_GraphViewChange;
        private List<Edge> m_EdgesToCreate;
        private List<GraphElement> m_EdgesToDelete;
        public PortView m_PortView;

        public DefaultEdgeConnectorListener() {
            this.m_EdgesToCreate = new List<Edge>();
            this.m_EdgesToDelete = new List<GraphElement>();
            this.m_GraphViewChange.edgesToCreate = this.m_EdgesToCreate;
        }

        public void OnDropOutsidePort(Edge edge, Vector2 position) {
        }

        public void OnDrop(UnityGraphView graphView, Edge edge) {
            // 动态端口连线已被修改为移动 - 在连线的过程中会不断切换Input/Output
            if (m_PortView.isDynamicPort) {
                if (m_PortView.direction == Direction.Input) {
                    if (edge.input is PortView input && input.isDynamicPort) {
                        m_PortView.MoveTo(input);
                    }
                } else {
                    if (edge.output is PortView output && output.isDynamicPort) {
                        m_PortView.MoveTo(output);
                    }
                }
                return;
            }
            // 原始代码
            this.m_EdgesToCreate.Clear();
            this.m_EdgesToCreate.Add(edge);
            this.m_EdgesToDelete.Clear();
            if (edge.input.capacity == Capacity.Single) {
                foreach (Edge connection in edge.input.connections) {
                    if (connection != edge) {
                        this.m_EdgesToDelete.Add(connection);
                    }
                }
            }
            if (edge.output.capacity == Capacity.Single) {
                foreach (Edge connection in edge.output.connections) {
                    if (connection != edge) {
                        this.m_EdgesToDelete.Add(connection);
                    }
                }
            }
            if (this.m_EdgesToDelete.Count > 0) {
                graphView.DeleteElements(this.m_EdgesToDelete);
            }
            List<Edge> edgesToCreate = this.m_EdgesToCreate;
            if (graphView.graphViewChanged != null) {
                edgesToCreate = graphView.graphViewChanged(this.m_GraphViewChange).edgesToCreate;
            }
            foreach (Edge edge1 in edgesToCreate) {
                graphView.AddElement(edge1);
                edge.input.Connect(edge1);
                edge.output.Connect(edge1);
            }
        }
    }

    #endregion
}
}