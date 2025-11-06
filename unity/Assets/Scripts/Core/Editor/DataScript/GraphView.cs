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

namespace Wjybxx.BigCat.CoreEditor.DataScript
{
public class GraphView : UnityEditor.Experimental.GraphView.GraphView
{
    public DataEditor editor;

    private int count;

    public GraphView() {
        this.Insert(0, new GridBackground()); // 网格背景
        this.AddManipulator(new ContentZoomer()); // 缩放
        this.AddManipulator(new ContentDragger()); // 画布拖拽
        this.AddManipulator(new SelectionDragger()); // 节点拖拽
        this.AddManipulator(new RectangleSelector()); // 框选
        //
        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/Core/Editor/DataScript/NodeGraphView.uss");
        this.styleSheets.Add(styleSheet);

        // 视图和逻辑同步
        graphViewChanged = this.OnGraphViewChanged;
    }

    /// <summary>
    /// 可以在这里剔除不该删除的元素
    /// </summary>
    /// <param name="viewChange"></param>
    /// <returns></returns>
    private GraphViewChange OnGraphViewChanged(GraphViewChange viewChange) {
        // 节点删除会触发边删除吗？
        if (viewChange.elementsToRemove != null) {

        }
        if (viewChange.edgesToCreate != null) { // 端口连接

        }
        return viewChange;
    }

    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter) {
        return this.ports.ToList()
            .Where(endPort => endPort.direction != startPort.direction
                              && endPort.node != startPort.node
                              && endPort.portType == startPort.portType) // TODO 验证类型
            .ToList();
    }


    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt) {
        if (evt.target is not UnityEditor.Experimental.GraphView.Node) {
            base.BuildContextualMenu(evt);
        }
        // TODO 提供搜索栏
        Vector2 mousePos = evt.localMousePosition;
        evt.menu.AppendSeparator();
        evt.menu.AppendAction("CreateNode", muneAction => {
            NodeView nodeView = new NodeView();
            nodeView.transform.position = mousePos;
            if (++count % 2 == 1) {
                nodeView.SetTopContainerDirection(FlexDirection.Row);
            }
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