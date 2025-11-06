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
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wjybxx.BigCat.CoreEditor.DataScript
{
/// <summary>
///
/// </summary>
public class NodeView : Node
{
    private readonly VisualElement leftAndRightOutputs;
    public readonly VisualElement leftOutputs;
    public readonly VisualElement rightOutputs;
    public readonly VisualElement bottomOutputs;

    /// <summary>
    /// 关联的数据模型
    /// </summary>
    public DataNode dataNode { get; set; }

    public NodeView() {
        title = "NodeView";
        style.flexShrink = 0;
        style.flexGrow = 1;

        leftAndRightOutputs = new VisualElement() { name = "leftAndRightOutputs" };
        leftAndRightOutputs.style.flexGrow = 1;
        leftAndRightOutputs.style.flexShrink = 1;
        //
        leftOutputs = new VisualElement() { name = "leftOutputs" };
        leftOutputs.style.flexGrow = 1;
        leftOutputs.style.flexShrink = 1;
        //
        rightOutputs = new VisualElement() { name = "rightOutputs" };
        rightOutputs.style.flexGrow = 1;
        rightOutputs.style.flexShrink = 1;
        //
        bottomOutputs = new VisualElement() { name = "bottomOutputs" };
        bottomOutputs.style.flexGrow = 1;
        bottomOutputs.style.flexShrink = 1;
        //
        leftAndRightOutputs.Add(leftOutputs);
        leftAndRightOutputs.Add(rightOutputs);
        outputContainer.Add(leftAndRightOutputs);
        outputContainer.Add(bottomOutputs);
        //
        {
            for (int i = 0; i < 3; i++) {
                Port inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, null);
                inputPort.portName = "H_Input" + i;
                inputContainer.Add(inputPort);

                Port outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, null);
                outputPort.portName = "L_Output" + i;
                leftOutputs.Add(outputPort);
            }
        }
        {
            for (int i = 0; i < 3; i++) {
                Port outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, null);
                outputPort.portName = "R_Output" + i;
                rightOutputs.Add(outputPort);
            }
        }
        {
            for (int i = 0; i < 3; i++) {
                Port outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, null);
                outputPort.portName = "B_Output" + i;
                bottomOutputs.Add(outputPort);
            }
        }
        SetTopContainerDirection(FlexDirection.Column);
    }

    /// <summary>
    ///
    /// 由于端口的<see cref="Orientation"/>不能动态变更，所以还是Column布局更自然；
    /// 由于<see cref="Orientation.Horizontal"/>不论是左右，还是上下侧表现都不错，因此Bottom也使用水平布线。
    /// </summary>
    /// <param name="direction"></param>
    public void SetTopContainerDirection(FlexDirection direction) {
        switch (direction) {
            case FlexDirection.Row:
            case FlexDirection.RowReverse: {
                // 左侧为Parents/inputs，下侧为lefts
                topContainer.style.flexDirection = FlexDirection.Row;
                inputContainer.style.flexDirection = FlexDirection.Column;
                outputContainer.style.flexDirection = FlexDirection.Row;
                //
                leftAndRightOutputs.style.flexDirection = FlexDirection.ColumnReverse;
                leftOutputs.style.alignItems = Align.FlexEnd;
                rightOutputs.style.alignItems = Align.FlexEnd;
                //
                leftOutputs.style.flexDirection = FlexDirection.Row;
                rightOutputs.style.flexDirection = FlexDirection.Row;
                bottomOutputs.style.flexDirection = FlexDirection.Column;
                //
                SetPortFlexDirection(inputContainer, FlexDirection.Row);
                SetPortFlexDirection(leftOutputs, FlexDirection.ColumnReverse);
                SetPortFlexDirection(rightOutputs, FlexDirection.Column);
                SetPortFlexDirection(bottomOutputs, FlexDirection.RowReverse);
                break;
            }
            case FlexDirection.Column:
            case FlexDirection.ColumnReverse: {
                // 顶部为Parents/inputs，左侧为lefts
                topContainer.style.flexDirection = FlexDirection.Column;
                inputContainer.style.flexDirection = FlexDirection.Row;
                outputContainer.style.flexDirection = FlexDirection.Column;
                // 
                leftAndRightOutputs.style.flexDirection = FlexDirection.Row;
                leftOutputs.style.alignItems = Align.FlexStart;
                rightOutputs.style.alignItems = Align.FlexStart;
                //
                leftOutputs.style.flexDirection = FlexDirection.Column;
                rightOutputs.style.flexDirection = FlexDirection.Column;
                bottomOutputs.style.flexDirection = FlexDirection.Row;
                //
                SetPortFlexDirection(inputContainer, FlexDirection.Column);
                SetPortFlexDirection(leftOutputs, FlexDirection.Row);
                SetPortFlexDirection(rightOutputs, FlexDirection.RowReverse);
                SetPortFlexDirection(bottomOutputs, FlexDirection.ColumnReverse);
                break;
            }
        }
    }

    private static void SetPortFlexDirection(VisualElement container, FlexDirection direction) {
        int childCount = container.childCount;
        for (int i = 0; i < childCount; i++) {
            Port port = (Port)container[i];
            port.style.flexDirection = direction;
        }
    }

    public override Port InstantiatePort(Orientation orientation, Direction direction, Port.Capacity capacity, Type type) {
        return PortView.Create<Edge>(orientation, direction, capacity, type);
    }

    protected override void OnPortRemoved(Port port) {
        base.OnPortRemoved(port);
    }

    public override void OnSelected() {
        base.OnSelected();
        GetFirstAncestorOfType<GraphView>().OnNodeSelected(this);
    }

    public override void OnUnselected() {
        base.OnUnselected();
        GetFirstAncestorOfType<GraphView>().OnNodeUnselected(this);
    }

    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt) {
        base.BuildContextualMenu(evt);
    }

    public override void SetPosition(Rect newPos) {
        base.SetPosition(newPos);
        if (dataNode != null) {
            dataNode.position = newPos.position;
            // dataNode.ApplyModifiedProperties();
        }
    }

    public new class UxmlFactory : UxmlFactory<NodeView, UxmlTraits>
    {
    }

    public new class UxmlTraits : UnityEditor.Experimental.GraphView.Node.UxmlTraits
    {
        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc) {
            base.Init(ve, bag, cc);
            var myView = (NodeView)ve;
        }
    }
}
}