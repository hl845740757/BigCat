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
using Wjybxx.Commons;
using Wjybxx.Commons.Pool;

namespace Wjybxx.BigCat.CoreEditor.DataScript
{
/// <summary>
///
/// 注：为减少复杂度，我们最终只保留<see cref="FlexDirection.Column"/>的端口布局，即GraphView整体自上而下布局。
/// </summary>
public class NodeView : Node
{
    private readonly VisualElement leftAndRightOutputs;
    public readonly VisualElement topInputs;
    public readonly VisualElement leftOutputs;
    public readonly VisualElement rightOutputs;
    public readonly VisualElement bottomOutputs;
    //
    private VisualElement topDynamicInputs;
    private VisualElement leftDynamicOutputs;
    private VisualElement rightDynamicOutputs;
    private VisualElement bottomDynamicOutputs;

    /// <summary>
    /// 关联的数据模型
    /// </summary>
    public DataNode dataNode { get; set; }

    public NodeView() {
        //
        topInputs = new VisualElement() { name = "topInputs" };
        topInputs.style.flexShrink = 1;
        topInputs.style.flexGrow = 1;
        inputContainer.Add(topInputs);
        //
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
        InitPortContainerStyle();
        //
        {
            for (int i = 0; i < 3; i++) {
                PortView inputPort = CreateInputPort();
                inputPort.portName = "H_Input" + i;
                AddPort(inputPort, Side.Top);

                PortView outputPort = CreateOutputPort(Side.Left, true);
                outputPort.portName = "L_Output" + i;
                AddPort(outputPort, Side.Left);
            }
        }
        {
            for (int i = 0; i < 3; i++) {
                PortView outputPort = CreateOutputPort(Side.Right, true);
                outputPort.portName = "R_Output" + i;
                AddPort(outputPort, Side.Right);
            }
        }
        {
            for (int i = 0; i < 3; i++) {
                PortView outputPort = CreateOutputPort(Side.Bottom, true);
                outputPort.portName = "B_Output" + i;
                AddPort(outputPort, Side.Bottom);
            }
        }
        title = "NodeView";
    }

    #region port增删

    /// <summary>
    /// 创建一个输入端口
    /// </summary>
    /// <returns></returns>
    public PortView CreateInputPort() {
        return PortView.Create<Edge>(Orientation.Vertical, Direction.Input, Port.Capacity.Multi);
    }

    /// <summary>
    /// 创建一个输出端口
    /// </summary>
    public PortView CreateOutputPort(Side side, bool isListPort = false) {
        Port.Capacity capacity = isListPort ? Port.Capacity.Multi : Port.Capacity.Single;
        return PortView.Create<Edge>(GetPortOrientation(side), Direction.Output, capacity);
    }

    /// <summary>
    /// 创建一个动态端口
    /// </summary>
    public PortView CreateDynamicPort(Side side) {
        Port.Capacity capacity = Port.Capacity.Single;
        PortView port = PortView.Create<Edge>(GetPortOrientation(side), GetPortType(side), capacity);
        port.style.flexDirection = GetPortFlexDirection(side);
        port.style.width = 33; // 错开身位
        port.isDynamicPort = true;
        port.portName = "";
        return port;
    }

    /// <summary>
    /// 添加一个普通端口
    /// </summary>
    public void AddPort(PortView port, Side side) {
        CheckPortType(port, side);
        if (port.isDynamicPort) {
            throw new ArgumentException("port.isDynamicPort == true");
        }
        VisualElement container = GetPortContainer(side);
        port.style.flexDirection = GetPortFlexDirection(side);
        container.Add(port);
    }

    /// <summary>
    /// 添加一个动态端口
    /// </summary>
    public void AddDynamicPort(PortView port, Side side) {
        CheckPortType(port, side);
        if (!port.isDynamicPort) {
            throw new ArgumentException("port.isDynamicPort != true");
        }
        VisualElement container = EnsureDynamicPortContainer(side);
        port.style.flexDirection = GetPortFlexDirection(side);
        container.Add(port);
    }

    /// <summary>
    /// 获取某侧动态端口数量
    /// </summary>
    /// <param name="side"></param>
    /// <returns></returns>
    public int GetDynamicPortCount(Side side) {
        VisualElement container = GetDynamicPortContainer(side);
        return container == null ? 0 : container.childCount;
    }

    private VisualElement EnsureDynamicPortContainer(Side side) {
        switch (side) {
            case Side.Top: {
                VisualElement container = topDynamicInputs;
                if (container == null) {
                    container = topDynamicInputs = CreateDynamicPortContainer("topDynamicInputs", topInputs);
                    inputContainer.Insert(0, container); // 添加到顶部
                    // container.style.marginLeft = 7; // 错开以防连线重叠
                } else if (!inputContainer.Contains(container)) {
                    inputContainer.Insert(0, container);
                }
                return container;
            }
            case Side.Left: {
                VisualElement container = leftDynamicOutputs;
                if (container == null) {
                    container = leftDynamicOutputs = CreateDynamicPortContainer("leftDynamicOutputs", leftOutputs);
                    leftAndRightOutputs.Insert(0, container); // 添加到左侧
                    // container.style.marginTop = 7; // 错开以防连线重叠
                } else if (!leftAndRightOutputs.Contains(container)) {
                    leftAndRightOutputs.Insert(0, container);
                }
                return container;
            }
            case Side.Right: {
                VisualElement container = rightDynamicOutputs;
                if (container == null) {
                    container = rightDynamicOutputs = CreateDynamicPortContainer("rightDynamicOutputs", rightOutputs);
                    leftAndRightOutputs.Add(container); // 添加到右侧
                    // container.style.marginTop = 7; // 错开以防连线重叠
                } else if (!leftAndRightOutputs.Contains(container)) {
                    leftAndRightOutputs.Add(container);
                }
                return container;
            }
            case Side.Bottom: {
                VisualElement container = bottomDynamicOutputs;
                if (container == null) {
                    container = bottomDynamicOutputs = CreateDynamicPortContainer("bottomDynamicOutputs", bottomOutputs);
                    outputContainer.Add(container); // 添加到底部
                    // container.style.marginLeft = 7; // 错开以防连线重叠
                } else if (!outputContainer.Contains(container)) {
                    outputContainer.Add(container);
                }
                return container;
            }
            default: throw new AssertionError();
        }
    }

    private static VisualElement CreateDynamicPortContainer(string name, VisualElement basic) {
        VisualElement container = new VisualElement();
        container.name = name;
        container.style.flexDirection = basic.style.flexDirection;
        container.style.flexGrow = basic.style.flexGrow;
        container.style.flexShrink = basic.style.flexShrink;
        container.style.alignItems = basic.style.alignItems;
        return container;
    }

    #endregion

    #region util

    private static void CheckPortType(PortView port, Side side) {
        if (side == Side.Top) {
            if (port.direction != Direction.Input) {
                throw new ArgumentException("port.direction != Direction.Input");
            }
        } else {
            if (port.direction != Direction.Output) {
                throw new ArgumentException("port.direction != Direction.Output");
            }
        }
    }

    /// <summary>
    /// 获取端口所属的方位
    /// </summary>
    public Side GetSide(PortView portView) {
        VisualElement container = portView.hierarchy.parent;
        if (portView.isDynamicPort) {
            if (container == topDynamicInputs) return Side.Top;
            if (container == rightDynamicOutputs) return Side.Right;
            if (container == leftDynamicOutputs) return Side.Left;
            if (container == bottomDynamicOutputs) return Side.Bottom;
        } else {
            if (container == topInputs) return Side.Top;
            if (container == rightOutputs) return Side.Right;
            if (container == leftOutputs) return Side.Left;
            if (container == bottomOutputs) return Side.Bottom;
        }
        throw new ArgumentException("invalid port");
    }

    internal VisualElement GetDynamicPortContainer(Side side) {
        return side switch
        {
            Side.Top => topDynamicInputs,
            Side.Left => leftDynamicOutputs,
            Side.Right => rightDynamicOutputs,
            Side.Bottom => bottomDynamicOutputs,
            _ => throw new AssertionError()
        };
    }

    private VisualElement GetPortContainer(Side side) {
        return side switch
        {
            Side.Top => topInputs,
            Side.Left => leftOutputs,
            Side.Right => rightOutputs,
            Side.Bottom => bottomOutputs,
            _ => throw new AssertionError()
        };
    }

    #endregion

    #region list端口展开

    private static readonly ObjectPool<List<Edge>> _connectListPool = ObjectPoolUtil.NewListPool<Edge>(4);

    /// <summary>
    /// 刷新动态端口(重新绑定数据)
    /// </summary>
    /// <param name="side"></param>
    public void RefreshDynamicPorts(Side side) {
        RefreshDynamicPorts(side, 0);
    }

    /// <summary>
    /// 刷新动态端口(重新绑定数据)
    /// </summary>
    /// <param name="side"></param>
    /// <param name="startIndex">inclusive</param>
    /// <param name="endIndex">inclusive</param>
    public void RefreshDynamicPorts(Side side, int startIndex, int endIndex = -1) {
        VisualElement container = GetDynamicPortContainer(side);
        if (container == null) return;
        if (endIndex == -1) {
            endIndex = container.childCount - 1;
        } else {
            endIndex = Math.Min(endIndex, container.childCount - 1);
        }
        for (int index = startIndex; index <= endIndex; index++) {
            PortView dynamicPort = (PortView)container[index];
            PortView listPort = dynamicPort.listPort;
            // 是否分配一个数字name？
            if (listPort.direction == Direction.Output) {
                dynamicPort.variable = listPort.variable?.TryGet(index);
            }
        }
    }

    /// <summary>
    /// 展开动态端口
    /// </summary>
    public void ExpandListPort(PortView port) {
        if (!port.isListPort || port.isExpanded) return;
        Side side = GetSide(port);
        CollapseListPort(side, false); // 折叠当前展开窗口
        //
        port.isExpanded = true;
        port.SetBorderWidth(1);
        port.SetBorderColor(Color.yellow);
        //
        VisualElement dynamicPortContainer = EnsureDynamicPortContainer(side);
        List<Edge> connectList = _connectListPool.Acquire();
        connectList.AddRange(port.connectionList);
        for (int index = 0; index < connectList.Count; index++) {
            Edge edge = connectList[index];
            PortView dynamicPort = CreateDynamicPort(side);
            dynamicPort.listPort = port;
            dynamicPortContainer.Add(dynamicPort);
            //
            port.Disconnect(edge);
            dynamicPort.Connect(edge);
            if (port.direction == Direction.Input) {
                edge.input = dynamicPort;
            } else {
                edge.output = dynamicPort;
                dynamicPort.variable = port.variable?.TryGet(index);
            }
        }
        _connectListPool.Release(connectList);
    }

    /// <summary>
    /// 收起List端口
    /// </summary>
    public void CollapseListPort(PortView port) {
        CollapseListPort(port, true);
    }

    /// <summary>
    /// 收起指定侧List端口
    /// </summary>
    private void CollapseListPort(Side side, bool removeFromHierarchy) {
        VisualElement container = GetPortContainer(side);
        for (int index = 0, count = container.childCount; index < count; index++) {
            PortView portView = (PortView)container[index];
            if (portView.isListPort && portView.isExpanded) {
                CollapseListPort(portView, removeFromHierarchy);
                break;
            }
        }
    }

    private void CollapseListPort(PortView port, bool removeFromHierarchy) {
        if (!port.isListPort || !port.isExpanded) return;
        port.isExpanded = false;
        port.SetBorderWidth(0);
        //
        Side side = GetSide(port);
        VisualElement container = GetDynamicPortContainer(side);
        if (container == null) {
            return;
        }
        // 修正Edge的输出端口为当前Port
        for (int index = 0, count = container.childCount; index < count; index++) {
            PortView dynamicPort = (PortView)container[index];
            Edge edge = dynamicPort.connectionList[0];
            //
            dynamicPort.Disconnect(edge);
            port.Connect(edge);
            if (port.direction == Direction.Input) {
                edge.input = port;
            } else {
                edge.output = port;
            }
        }
        container.Clear();
        if (removeFromHierarchy) {
            container.RemoveFromHierarchy();
        }
    }

    #endregion

    #region port布局

    private void InitPortContainerStyle() {
        topContainer.style.flexDirection = FlexDirection.Column;
        inputContainer.style.flexDirection = FlexDirection.Column;
        outputContainer.style.flexDirection = FlexDirection.Column;
        //
        leftAndRightOutputs.style.flexDirection = FlexDirection.Row;
        bottomOutputs.style.marginTop = 5;
        bottomOutputs.style.marginLeft = 0;
        //
        SetTopInputsStyle(FlexDirection.Row, Align.Center);
        SetLeftOutputsStyle(FlexDirection.Column, Align.FlexStart);
        SetRightOutputsStyle(FlexDirection.Column, Align.FlexStart);
        SetBottomOutputsStyle(FlexDirection.Row, Align.Center);
    }

    private void SetTopInputsStyle(FlexDirection flexDirection, Align alignItems) {
        SetFlexDirection(topInputs, flexDirection, alignItems);
        if (topDynamicInputs != null) {
            SetFlexDirection(topDynamicInputs, flexDirection, alignItems);
        }
    }

    private void SetLeftOutputsStyle(FlexDirection flexDirection, Align alignItems) {
        SetFlexDirection(leftOutputs, flexDirection, alignItems);
        if (leftDynamicOutputs != null) {
            SetFlexDirection(leftDynamicOutputs, flexDirection, alignItems);
        }
    }

    private void SetRightOutputsStyle(FlexDirection flexDirection, Align alignItems) {
        SetFlexDirection(rightOutputs, flexDirection, alignItems);
        if (rightDynamicOutputs != null) {
            SetFlexDirection(rightDynamicOutputs, flexDirection, alignItems);
        }
    }

    private void SetBottomOutputsStyle(FlexDirection flexDirection, Align alignItems) {
        SetFlexDirection(bottomOutputs, flexDirection, alignItems);
        if (bottomDynamicOutputs != null) {
            SetFlexDirection(bottomDynamicOutputs, flexDirection, alignItems);
        }
    }

    private static void SetFlexDirection(VisualElement container,
                                         FlexDirection direction, Align alignItems) {
        container.style.flexDirection = direction;
        container.style.alignItems = alignItems;
    }

    private static FlexDirection GetPortFlexDirection(Side side) {
        return side switch
        {
            Side.Top => FlexDirection.Column,
            Side.Left => FlexDirection.Row,
            Side.Right => FlexDirection.RowReverse,
            Side.Bottom => FlexDirection.ColumnReverse,
            _ => throw new AssertionError()
        };
    }

    private static Orientation GetPortOrientation(Side side) {
        return side switch
        {
            Side.Top => Orientation.Vertical,
            Side.Left => Orientation.Horizontal,
            Side.Right => Orientation.Horizontal,
            Side.Bottom => Orientation.Vertical,
            _ => throw new AssertionError()
        };
    }

    private static Direction GetPortType(Side side) {
        return side == Side.Top ? Direction.Input : Direction.Output;
    }

    #endregion

    #region overrides

    public override Port InstantiatePort(Orientation orientation, Direction direction, Port.Capacity capacity, Type type) {
        return PortView.Create<Edge>(orientation, direction, capacity, type);
    }

    public override void OnSelected() {
        base.OnSelected();
        GetFirstAncestorOfType<GraphView>()?.OnNodeSelected(this);
    }

    public override void OnUnselected() {
        base.OnUnselected();
        // Delete触发时会先解除GraphView的引用，因此要判空
        GetFirstAncestorOfType<GraphView>()?.OnNodeUnselected(this);
    }

    protected override void OnPortRemoved(Port port) {
        //
    }

    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt) {
        base.BuildContextualMenu(evt);
    }

    #endregion

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