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
using UnityEngine.InputSystem;
using Wjybxx.Commons.Logger;
using ILogger = Wjybxx.Commons.Logger.ILogger;

namespace Wjybxx.BigCat.Control
{
/// <summary>
/// 输入上下文
///
/// 1.一个上下文是一组<see cref="InputActionMap"/>的集合，代表一种"操作模式"，比如Gameplay、UI、Cutscene。
/// 2.上下文由<see cref="InputManager"/>以栈的方式管理：<see cref="Priority"/>越大越靠栈顶；
///   若栈顶某个上下文声明了<see cref="Exclusive"/>，则它下方的所有上下文都会被屏蔽（Action被禁用）。
///   典型用法：打开模态窗口时压入一个Exclusive的UI上下文，Gameplay的输入自动失效。
/// 3.上下文内的Action通过<see cref="InputActionSlot"/>暴露，以获得帧锁存能力。
///
/// 注：请通过<see cref="InputManager.CreateContext"/>创建实例，以便注册到管理器。
/// </summary>
public sealed class InputContext
{
    private static readonly ILogger logger = LoggerFactory.GetLogger<InputContext>();

    /// <summary>
    /// 上下文名字(唯一)
    /// </summary>
    private readonly string _name;
    /// <summary>
    /// 优先级，越大越靠栈顶
    /// </summary>
    private readonly int _priority;
    /// <summary>
    /// 是否屏蔽下层上下文
    /// </summary>
    private readonly bool _exclusive;

    /// <summary>
    /// 该上下文包含的所有ActionMap
    /// </summary>
    private readonly List<InputActionMap> _maps = new List<InputActionMap>(2);
    /// <summary>
    /// Action名 -> 槽
    /// </summary>
    private readonly Dictionary<string, InputActionSlot> _slotMap = new Dictionary<string, InputActionSlot>(16);
    /// <summary>
    /// 所有槽(添加序，用于快速迭代)
    /// </summary>
    private readonly List<InputActionSlot> _slots = new List<InputActionSlot>(16);

    /// <summary>
    /// 是否已压入管理器
    /// </summary>
    private bool _pushed;
    /// <summary>
    /// 是否生效中(未被上层Exclusive上下文屏蔽)
    /// </summary>
    private bool _active;

    internal InputContext(string name, int priority, bool exclusive) {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        _priority = priority;
        _exclusive = exclusive;
    }

    /// <summary>
    /// 上下文名字
    /// </summary>
    public string Name => _name;
    /// <summary>
    /// 优先级，越大越靠栈顶
    /// </summary>
    public int Priority => _priority;
    /// <summary>
    /// 是否屏蔽下层上下文
    /// </summary>
    public bool Exclusive => _exclusive;
    /// <summary>
    /// 是否已压入管理器
    /// </summary>
    public bool Pushed => _pushed;
    /// <summary>
    /// 是否生效中
    ///
    /// 注：已压入(<see cref="Pushed"/>)但被上层Exclusive上下文屏蔽时，该值为false。
    /// </summary>
    public bool Active => _active;
    /// <summary>
    /// 该上下文包含的所有ActionMap(只读)
    /// </summary>
    public IReadOnlyList<InputActionMap> Maps => _maps;
    /// <summary>
    /// 该上下文包含的所有槽(只读)
    /// </summary>
    public IReadOnlyList<InputActionSlot> Slots => _slots;

    /// <summary>
    /// 槽列表的具体类型
    ///
    /// 注：供<see cref="InputManager"/>在每帧的热路径上迭代，避免接口分派开销。
    /// </summary>
    internal List<InputActionSlot> SlotList => _slots;

    #region 构建

    /// <summary>
    /// 添加一个ActionMap，并为其中的Action建立槽
    ///
    /// 注：
    /// 1.同一上下文内Action名必须唯一，重名时保留先添加的，并打印警告。
    /// 2.同一个Map不应被添加到多个上下文，否则两个上下文会争夺它的启用状态。
    /// 3.压栈后仍可添加Map，新Map会立即跟随当前的生效状态。
    /// </summary>
    /// <param name="map">要添加的ActionMap，可以来自资源也可以是代码构建的</param>
    /// <returns>this，便于链式调用</returns>
    public InputContext AddMap(InputActionMap map) {
        if (map == null) throw new ArgumentNullException(nameof(map));
        if (_maps.Contains(map)) {
            return this;
        }
        _maps.Add(map);
        // Map的启用状态由管理器统一控制，此处对齐到当前上下文的生效状态
        if (_active) {
            map.Enable();
        } else {
            map.Disable();
        }
        foreach (InputAction action in map.actions) {
            if (_slotMap.ContainsKey(action.name)) {
                logger.Warn("duplicate action name in context, context: " + _name + ", action: " + action.name);
                continue;
            }
            InputActionSlot slot = new InputActionSlot(action, this);
            _slotMap[action.name] = slot;
            _slots.Add(slot);
        }
        return this;
    }

    #endregion

    #region 查询

    /// <summary>
    /// 查询槽
    /// </summary>
    /// <param name="actionName">Action名</param>
    /// <returns>不存在时返回null</returns>
    public InputActionSlot GetSlot(string actionName) {
        return _slotMap.TryGetValue(actionName, out InputActionSlot slot) ? slot : null;
    }

    /// <summary>
    /// 查询槽，不存在则抛异常
    /// </summary>
    /// <param name="actionName">Action名</param>
    /// <exception cref="ArgumentException">Action不存在</exception>
    public InputActionSlot this[string actionName] {
        get {
            InputActionSlot slot = GetSlot(actionName);
            if (slot == null) {
                throw new ArgumentException($"action not found, context: {_name}, action: {actionName}");
            }
            return slot;
        }
    }

    /// <summary>
    /// 查询Action
    /// </summary>
    /// <param name="actionName">Action名</param>
    /// <returns>不存在时返回null</returns>
    public InputAction GetAction(string actionName) {
        return GetSlot(actionName)?.Action;
    }

    #endregion

    #region 内部调度

    /// <summary>
    /// 标记压栈状态
    /// </summary>
    internal void SetPushed(bool pushed) {
        _pushed = pushed;
    }

    /// <summary>
    /// 设置生效状态
    ///
    /// 注：失效时会重置所有槽的锁存状态，避免恢复后收到陈旧输入。
    /// </summary>
    internal void SetActive(bool active) {
        if (_active == active) {
            return;
        }
        _active = active;
        List<InputActionMap> maps = _maps;
        for (int i = 0; i < maps.Count; i++) {
            if (active) {
                maps[i].Enable();
            } else {
                maps[i].Disable();
            }
        }
        if (!active) {
            List<InputActionSlot> slots = _slots;
            for (int i = 0; i < slots.Count; i++) {
                slots[i].Reset();
            }
        }
    }

    #endregion

    public override string ToString() {
        return $"InputContext({_name}, priority: {_priority}, exclusive: {_exclusive}, active: {_active})";
    }
}
}
