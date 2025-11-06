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
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using Wjybxx.BigCatTool.DataScript;
using Wjybxx.Commons;
using Wjybxx.Dson;

namespace Wjybxx.BigCat.CoreEditor.DataScript
{
/// <summary>
/// 数据端口视图
///
/// 视图需要设计多个状态（颜色）：连接为空、目标对象在同Folder内，目标对象在其它Folder。
/// </summary>
public class PortView : Port
{
    /// <summary>
    /// 关联的字段
    /// </summary>
    public Variable field { get; set; }

    private PortView(Orientation portOrientation,
                     Direction portDirection,
                     Capacity portCapacity,
                     Type type)
        : base(portOrientation, portDirection, portCapacity, type) {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="prevValue">旧值</param>
    /// <param name="newValue">新值</param>
    /// <param name="index">如果是List或Map，则需要传入index</param>
    public void OnValueChanged(ObjectPath prevValue, ObjectPath newValue, int index = -1) {

    }

    #region internal

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

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public new static PortView Create<TEdge>(Orientation orientation,
                                             Direction direction,
                                             Capacity capacity,
                                             Type type) where TEdge : Edge, new() {
        DefaultEdgeConnectorListener listener = new DefaultEdgeConnectorListener();
        PortView ele = new PortView(orientation, direction, capacity, type)
        {
            m_EdgeConnector = new EdgeConnector<TEdge>(listener)
        };
        ele.AddManipulator(ele.m_EdgeConnector);
        return ele;
    }

    private class DefaultEdgeConnectorListener : IEdgeConnectorListener
    {
        private GraphViewChange m_GraphViewChange;
        private List<Edge> m_EdgesToCreate;
        private List<GraphElement> m_EdgesToDelete;

        public DefaultEdgeConnectorListener() {
            this.m_EdgesToCreate = new List<Edge>();
            this.m_EdgesToDelete = new List<GraphElement>();
            this.m_GraphViewChange.edgesToCreate = this.m_EdgesToCreate;
        }

        public void OnDropOutsidePort(Edge edge, Vector2 position) {
        }

        public void OnDrop(UnityEditor.Experimental.GraphView.GraphView graphView, Edge edge) {
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