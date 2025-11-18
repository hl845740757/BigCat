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
using Wjybxx.Commons;

namespace Wjybxx.BigCat.CoreEditor.DataScript
{
/// <summary>
///
/// 注：为减少复杂度，我们最终只保留<see cref="FlexDirection.Column"/>的端口布局，即GraphView整体自上而下布局。
/// </summary>
public class NodeView : Node
{
    private readonly VisualElement leftAndRightOutputs;
    public readonly VisualElement topInputs; // 正式版只有一个元素
    public readonly VisualElement leftOutputs;
    public readonly VisualElement rightOutputs;
    public readonly VisualElement bottomOutputs;
    //
    private VisualElement topDynamicInputs;
    private VisualElement leftDynamicOutputs;
    private VisualElement rightDynamicOutputs;
    private VisualElement bottomDynamicOutputs;

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
    }

    #region 数据绑定

    /// <summary>
    /// 绑定数据
    /// 
    /// 1.不支持重复绑定 —— 涉及逻辑太多，不易处理。
    /// 2.端口之间的连接需要由GraphView恢复，动态端口也由GraphView处理。
    /// 3.避免修改顶层Node的数据类型，会导致NodeView的关联的端口信息失效。
    /// </summary>
    /// <param name="dataNode"></param>
    public void Bind(DataNode dataNode) {
        if (this.dataNode != null) {
            throw new InvalidOperationException("already bound");
        }
        // this.name = dataNode.value.type.SimpleName; // 视图对象name设置为类型名，用于筛选
        this.dataNode = dataNode;
        RebuildPorts();
        Refresh();
    }

    /// <summary>
    /// 刷新显示，子类可以重写该方法扩展逻辑
    /// </summary>
    public virtual void Refresh() {
        title = !string.IsNullOrWhiteSpace(dataNode.title)
            ? dataNode.title
            : dataNode.value.type.SimpleName;
        // 见Node.SetPosition
        style.left = dataNode.position.x;
        style.top = dataNode.position.y;
    }

    /// <summary>
    /// 重建固定端口
    /// (运行时只应该Pair类型调用，且需要在建立连接前调用)
    /// </summary>
    public void RebuildPorts() {
        ClearFixedPorts();
        {
            PortView inputPort = CreateInputPort();
            inputPort.portName = "inputs";
            AddPort(inputPort, Side.Top);
        }
        foreach (Variable outputField in dataNode.outputFields) {
            Side side = GetSide(outputField.cfg);
            PortView portView = CreateOutputPort(side, outputField.isCollectionType);
            portView.portName = outputField.defineInfo.SimpleName;
            portView.Bind(outputField);
            AddPort(portView, side);
            // 默认展开
            FieldPortCfg portCfg = outputField.cfg.portCfg;
            if (portCfg != null && portCfg.expanded) {
                ExpandListPort(portView);
            }
        }
    }

    /// <summary>
    /// 清理所有固定端口
    /// </summary>
    public void ClearFixedPorts() {
        topInputs.Clear();
        leftOutputs.Clear();
        rightOutputs.Clear();
        bottomOutputs.Clear();
    }

    /// <summary>
    /// 清理所有动态端口
    /// </summary>
    public void ClearDynamicPorts() {
        topDynamicInputs?.Clear();
        leftDynamicOutputs?.Clear();
        rightDynamicOutputs?.Clear();
        bottomDynamicOutputs?.Clear();
    }

    #endregion

    #region port增删

    /// <summary>
    /// 获取变量所属的边
    /// </summary>
    public static Side GetSide(VariableCfg cfg) {
        // Pair类型的PortCfg可能为null
        return cfg.portCfg != null ? cfg.portCfg.side : Side.Right;
    }

    /// <summary>
    /// 创建一个输入端口
    /// </summary>
    /// <returns></returns>
    public static PortView CreateInputPort() {
        PortView port = PortView.Create<Edge>(Orientation.Vertical, Direction.Input, Port.Capacity.Multi);
        port.style.flexDirection = GetPortFlexDirection(Side.Top);
        return port;
    }

    /// <summary>
    /// 创建一个输出端口
    /// </summary>
    public static PortView CreateOutputPort(Side side, bool isListPort = false) {
        Port.Capacity capacity = isListPort ? Port.Capacity.Multi : Port.Capacity.Single;
        PortView port = PortView.Create<Edge>(GetPortOrientation(side), Direction.Output, capacity);
        port.style.flexDirection = GetPortFlexDirection(side);
        return port;
    }

    /// <summary>
    /// 创建一个动态端口
    /// </summary>
    public static PortView CreateDynamicPort(Side side) {
        Port.Capacity capacity = Port.Capacity.Single;
        PortView port = PortView.Create<Edge>(GetPortOrientation(side), GetPortType(side), capacity);
        port.style.flexDirection = GetPortFlexDirection(side); // 需要Connect之前绑定样式，否则Edge坐标异常
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
        GetPortContainer(side).Add(port);
    }

    /// <summary>
    /// 添加一个动态端口
    /// </summary>
    public void AddDynamicPort(PortView port, Side side) {
        CheckPortType(port, side);
        if (!port.isDynamicPort) {
            throw new ArgumentException("port.isDynamicPort != true");
        }
        EnsureDynamicPortContainer(side).Add(port);
    }

    /// <summary>
    /// 插入一个动态端口
    /// </summary>
    public void InsertDynamicPort(PortView port, Side side, int index) {
        CheckPortType(port, side);
        if (!port.isDynamicPort) {
            throw new ArgumentException("port.isDynamicPort != true");
        }
        EnsureDynamicPortContainer(side).Insert(index, port);
    }

    internal PortView GetPort(Side side, int index) {
        VisualElement container = GetPortContainer(side);
        return (PortView)container[index];
    }

    internal PortView GetDynamicPort(Side side, int index) {
        VisualElement container = GetDynamicPortContainer(side);
        return (PortView)container[index];
    }

    private VisualElement EnsureDynamicPortContainer(Side side) {
        switch (side) {
            case Side.Top: {
                VisualElement container = topDynamicInputs;
                if (container == null) {
                    container = CreateDynamicPortContainer("topDynamicInputs", topInputs);
                    topDynamicInputs = container;
                    inputContainer.Insert(0, container); // 添加到顶部
                } else if (!inputContainer.Contains(container)) {
                    inputContainer.Insert(0, container);
                }
                return container;
            }
            case Side.Left: {
                VisualElement container = leftDynamicOutputs;
                if (container == null) {
                    container = CreateDynamicPortContainer("leftDynamicOutputs", leftOutputs);
                    leftDynamicOutputs = container;
                    leftAndRightOutputs.Insert(0, container); // 添加到左侧
                } else if (!leftAndRightOutputs.Contains(container)) {
                    leftAndRightOutputs.Insert(0, container);
                }
                return container;
            }
            case Side.Right: {
                VisualElement container = rightDynamicOutputs;
                if (container == null) {
                    container = CreateDynamicPortContainer("rightDynamicOutputs", rightOutputs);
                    rightDynamicOutputs = container;
                    leftAndRightOutputs.Add(container); // 添加到右侧
                } else if (!leftAndRightOutputs.Contains(container)) {
                    leftAndRightOutputs.Add(container);
                }
                return container;
            }
            case Side.Bottom: {
                VisualElement container = bottomDynamicOutputs;
                if (container == null) {
                    container = CreateDynamicPortContainer("bottomDynamicOutputs", bottomOutputs);
                    bottomDynamicOutputs = container;
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

    internal VisualElement GetPortContainer(Side side) {
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

    /// <summary>
    /// 展开动态端口
    /// 注：force参数用于Refresh时。
    /// </summary>
    public void ExpandListPort(PortView port) {
        if (!port.isListPort || port.isExpanded) return;
        Side side = GetSide(port);
        CollapseListPort(side, false);
        //
        port.isExpanded = true;
        port.SetBorderWidth(1);
        port.SetBorderColor(Color.yellow);
        //
        int connectionCount = port.connectionList.Count;
        VisualElement dynamicPortContainer = EnsureDynamicPortContainer(side);
        for (int index = 0; index < connectionCount; index++) {
            Edge edge = port.connectionList[index];
            PortView dynamicPort = CreateDynamicPort(side);
            if (port.direction == Direction.Output) {
                dynamicPort.Bind(port.variable[index]);
                edge.output = dynamicPort;
            } else {
                edge.input = dynamicPort;
            }
            dynamicPort.listPort = port;
            dynamicPort.Connect(edge); // 共享连接
            dynamicPortContainer.Add(dynamicPort);
        }
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
        VisualElement container = GetDynamicPortContainer(side);
        if (container == null || container.childCount == 0) {
            return;
        }
        PortView dynamicPort = (PortView)container[0];
        CollapseListPort(dynamicPort.listPort, removeFromHierarchy);
    }

    private void CollapseListPort(PortView port, bool removeFromHierarchy) {
        if (!port.isListPort || !port.isExpanded) return;
        port.isExpanded = false;
        port.SetBorderWidth(0);
        //
        Side side = GetSide(port);
        VisualElement container = GetDynamicPortContainer(side);
        if (container == null || container.childCount == 0) {
            return;
        }
        for (int index = 0; index < container.childCount; index++) {
            PortView dynamicPort = (PortView)container[index];
            Edge edge = dynamicPort.connectionList[0];
            if (port.direction == Direction.Output) {
                dynamicPort.Unbind();
                edge.output = port;
            } else {
                edge.input = port;
            }
            dynamicPort.listPort = null; // 解除绑定
            dynamicPort.DisconnectAll(); // 效率更好
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
        if (evt.target is not Node) {
            return;
        }
        base.BuildContextualMenu(evt);
        evt.StopImmediatePropagation(); // 禁用全局菜单栏
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