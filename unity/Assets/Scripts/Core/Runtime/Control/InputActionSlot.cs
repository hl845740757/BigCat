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
using UnityEngine;
using UnityEngine.InputSystem;

namespace Wjybxx.BigCat.Control
{
/// <summary>
/// Action的帧锁存槽
///
/// 1.InputSystem的<see cref="InputAction.WasPressedThisFrame"/>是<b>渲染帧</b>语义 —— 它只在按键发生的那一个
///   更新步内返回true。而我们的逻辑帧由<see cref="InputManager.BeginOfFrame"/>驱动，且并非每个渲染帧都会推进
///   （比如帧率高于FixedUpdate频率时，某些渲染帧内逻辑帧不会推进），直接查询会<b>丢按键</b>。
/// 2.因此该槽在<see cref="InputSystem.onAfterUpdate"/>时机把按下/释放<b>累积</b>起来
///   （<see cref="Accumulate"/>），到下一个逻辑帧开始时再一次性交付（<see cref="Snapshot"/>），
///   逻辑帧结束时清理（<see cref="ClearFrame"/>）。按键因此永不丢失，最多延迟一个逻辑帧。
/// 3.边沿信号（按下/释放/触发）走锁存；连续量（<see cref="ReadValue{TValue}"/>）是实时读取的，
///   因为连续量不存在"丢事件"问题，实时值反而更准确。
/// 4.<see cref="Consume"/>用于在同一个Action的多个消费者之间实现优先级；跨上下文的屏蔽请用
///   <see cref="InputContext.Exclusive"/>。
///
/// 注：一个更新步内发生的多次按下只会计为1次，这在实践中不会发生（人手速远低于输入采样率）。
/// </summary>
public sealed class InputActionSlot
{
    /// <summary>
    /// 关联的Action
    /// </summary>
    private readonly InputAction _action;
    /// <summary>
    /// 所属上下文
    /// </summary>
    private readonly InputContext _context;

    // region 累积区：由InputSystem的更新回调写入，逻辑层不可见
    private int _pressAccum;
    private int _releaseAccum;
    private int _performAccum;
    // endregion

    // region 帧区：BeginOfFrame从累积区快照而来，逻辑层可见
    private int _pressCount;
    private int _releaseCount;
    private int _performCount;
    private bool _isPressed;
    // endregion

    internal InputActionSlot(InputAction action, InputContext context) {
        _action = action ?? throw new ArgumentNullException(nameof(action));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// 关联的Action
    /// </summary>
    public InputAction Action => _action;
    /// <summary>
    /// Action名
    /// </summary>
    public string Name => _action.name;
    /// <summary>
    /// 所属的输入上下文
    /// </summary>
    public InputContext Context => _context;
    /// <summary>
    /// Action当前是否处于启用状态
    ///
    /// 注：上下文被屏蔽时，其下的Action会被禁用。
    /// </summary>
    public bool Enabled => _action.enabled;

    #region 边沿信号(锁存)

    /// <summary>
    /// 本逻辑帧内是否按下过
    ///
    /// 注：即使按下后立即释放（同一帧内完成点击），该属性和<see cref="WasReleased"/>也会同时为true。
    /// </summary>
    public bool WasPressed => _pressCount > 0;
    /// <summary>
    /// 本逻辑帧内是否释放过
    /// </summary>
    public bool WasReleased => _releaseCount > 0;
    /// <summary>
    /// 本逻辑帧内Action是否被触发过
    ///
    /// 注：与<see cref="WasPressed"/>的区别在于该属性取决于Action上的交互器(Interaction)，
    /// 例如配置了<c>Hold</c>交互器时，按下的瞬间并不会触发。
    /// </summary>
    public bool WasPerformed => _performCount > 0;

    /// <summary>
    /// 本逻辑帧内按下的次数
    ///
    /// 注：一个逻辑帧内可能跨越多个输入更新步，因此可能大于1（如双击）。
    /// </summary>
    public int PressCount => _pressCount;
    /// <summary>
    /// 本逻辑帧内释放的次数
    /// </summary>
    public int ReleaseCount => _releaseCount;
    /// <summary>
    /// 本逻辑帧内触发的次数
    /// </summary>
    public int PerformCount => _performCount;

    /// <summary>
    /// 是否处于按下状态(逻辑帧快照)
    ///
    /// 注：该值在<see cref="InputManager.BeginOfFrame"/>时采样，整个逻辑帧内保持稳定；
    /// 需要实时值请使用<see cref="IsPressedLive"/>。
    /// </summary>
    public bool IsPressed => _isPressed;
    /// <summary>
    /// 是否处于按下状态(实时)
    /// </summary>
    public bool IsPressedLive => _action.IsPressed();

    /// <summary>
    /// 消费掉本帧的边沿信号
    ///
    /// 注：
    /// 1.用于在<b>同一个Action</b>的多个消费者之间实现优先级 —— 先处理的模块调用该方法后，
    ///   后续模块便观察不到该输入。
    /// 2.跨上下文的屏蔽<b>不是</b>靠该方法，而是靠<see cref="InputContext.Exclusive"/> ——
    ///   不同上下文中的同名Action是两个独立的<see cref="InputAction"/>，互不影响。
    /// 3.只清理边沿信号，不影响<see cref="IsPressed"/>和<see cref="ReadValue{TValue}"/>。
    /// </summary>
    public void Consume() {
        _pressCount = 0;
        _releaseCount = 0;
        _performCount = 0;
    }

    #endregion

    #region 连续量(实时)

    /// <summary>
    /// 读取Action的当前值
    ///
    /// 注：
    /// 1.该方法是实时读取的，未经过帧锁存 —— 连续量不存在丢事件问题。
    /// 2.<typeparamref name="TValue"/>必须与Action绑定的控件值类型一致，否则抛出异常。
    /// 3.Action未处于活动状态时返回的是<c>default</c>经过processor处理后的结果，
    ///   配置了invert/scale等processor时该结果可能<b>不是</b><c>default</c>。
    /// </summary>
    /// <exception cref="InvalidOperationException">值类型不匹配</exception>
    public TValue ReadValue<TValue>() where TValue : struct {
        return _action.ReadValue<TValue>();
    }

    /// <summary>
    /// 读取二维向量值(移动、视角等)
    /// </summary>
    public Vector2 ReadVector2() {
        return _action.ReadValue<Vector2>();
    }

    /// <summary>
    /// 读取浮点值(扳机键、滚轮等)
    /// </summary>
    public float ReadFloat() {
        return _action.ReadValue<float>();
    }

    #endregion

    #region 内部调度

    /// <summary>
    /// 累积输入边沿
    ///
    /// 注：该方法在<see cref="InputSystem.onAfterUpdate"/>时机被调用，即每个输入更新步结束时。
    /// </summary>
    internal void Accumulate() {
        // WasXxxThisFrame是"当前更新步"语义，此处正处于该更新步内，因此可以准确取到边沿
        if (_action.WasPressedThisFrame()) _pressAccum++;
        if (_action.WasReleasedThisFrame()) _releaseAccum++;
        if (_action.WasPerformedThisFrame()) _performAccum++;
    }

    /// <summary>
    /// 将累积区交付到帧区(逻辑帧开始)
    /// </summary>
    internal void Snapshot() {
        _pressCount = _pressAccum;
        _releaseCount = _releaseAccum;
        _performCount = _performAccum;
        _pressAccum = 0;
        _releaseAccum = 0;
        _performAccum = 0;
        _isPressed = _action.IsPressed();
    }

    /// <summary>
    /// 清理帧区(逻辑帧结束)
    ///
    /// 注：不清理累积区，否则会丢掉本帧内新到达的输入。
    /// </summary>
    internal void ClearFrame() {
        _pressCount = 0;
        _releaseCount = 0;
        _performCount = 0;
    }

    /// <summary>
    /// 重置所有状态
    ///
    /// 注：上下文被屏蔽/禁用时调用，避免恢复后收到陈旧输入。
    /// </summary>
    internal void Reset() {
        _pressAccum = 0;
        _releaseAccum = 0;
        _performAccum = 0;
        _pressCount = 0;
        _releaseCount = 0;
        _performCount = 0;
        _isPressed = false;
    }

    #endregion

    public override string ToString() {
        return $"InputActionSlot({_context.Name}/{Name})";
    }
}
}
