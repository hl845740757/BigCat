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
using UnityEngine.InputSystem.LowLevel;
using Wjybxx.Commons.Inject.Attributes;
using Wjybxx.Commons.Logger;
using ILogger = Wjybxx.Commons.Logger.ILogger;
using RebindingOperation = UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation;

namespace Wjybxx.BigCat.Control
{
/// <summary>
/// 输入管理器
///
/// 1.该管理器是新版InputSystem的门面，提供四类能力：
///   帧锁存（<see cref="InputActionSlot"/>）、输入上下文栈（<see cref="InputContext"/>）、
///   设备归类（<see cref="CurDevice"/>）、按键重绑定与持久化（<see cref="StartRebind"/>）。
/// 2.Action的来源不限：既可以来自<see cref="InputActionAsset"/>资源（<see cref="RegisterAsset"/>），
///   也可以用代码构建（<see cref="CreateMap"/>），两者可混用。框架自身不预设任何Action名。
/// 3.该管理器是纯C#类，不是MonoBehaviour；心跳由外部驱动，需要在帧循环中调用
///   <see cref="BeginOfFrame"/>和<see cref="EndOfFrame"/>，参考<c>GameLauncher</c>。
/// 4.使用前必须调用<see cref="Start"/>挂接InputSystem的回调，销毁前调用<see cref="Stop"/>。
///
/// 典型初始化流程：
/// <code>
/// InputManager inputMgr = new InputManager();
/// InputManager.Inst = inputMgr;
/// inputMgr.Start();
///
/// // 代码构建Action
/// InputActionMap map = inputMgr.CreateMap("Gameplay");
/// InputAction move = map.AddAction("Move", InputActionType.Value);
/// move.AddCompositeBinding("2DVector")
///     .With("Up", "&lt;Keyboard&gt;/w").With("Down", "&lt;Keyboard&gt;/s")
///     .With("Left", "&lt;Keyboard&gt;/a").With("Right", "&lt;Keyboard&gt;/d");
/// map.AddAction("Jump", InputActionType.Button, "&lt;Keyboard&gt;/space");
///
/// // 建立上下文并压栈
/// inputMgr.CreateContext("Gameplay").AddMap(map);
/// inputMgr.PushContext("Gameplay");
/// inputMgr.LoadBindings(); // 应用玩家的改键
///
/// // 逻辑层读取
/// Vector2 dir = inputMgr["Move"].ReadVector2();
/// if (inputMgr["Jump"].WasPressed) { ... }
/// </code>
/// </summary>
public class InputManager
{
    private static readonly ILogger logger = LoggerFactory.GetLogger<InputManager>();

    /// <summary>
    /// 默认的取消重绑定的按键
    /// </summary>
    public const string DefaultRebindCancelPath = "<Keyboard>/escape";

    /// <summary>
    /// 全局实例
    /// </summary>
    public static InputManager Inst { get; set; }

    /// <summary>
    /// 名字 -> 上下文
    /// </summary>
    private readonly Dictionary<string, InputContext> _contextMap = new Dictionary<string, InputContext>(8);
    /// <summary>
    /// 上下文栈，下标0是栈顶（优先级最高）
    ///
    /// 注：使用List而非Stack，因为压栈位置由优先级决定，且需要按序迭代。
    /// </summary>
    private readonly List<InputContext> _contextStack = new List<InputContext>(8);
    /// <summary>
    /// 参与重绑定持久化的Action集合
    /// </summary>
    private readonly List<BindingOwner> _bindingOwners = new List<BindingOwner>(4);
    /// <summary>
    /// 代码构建的ActionMap（不属于任何资源，需要我们自己持有以避免被GC）
    /// </summary>
    private readonly List<InputActionMap> _standaloneMaps = new List<InputActionMap>(4);

    /// <summary>
    /// 重绑定数据的存储
    /// </summary>
    private IInputBindingStore _bindingStore = new PlayerPrefsBindingStore();
    /// <summary>
    /// 是否已启动
    /// </summary>
    private bool _started;

    /// <summary>
    /// 当前设备(粗粒度)
    /// </summary>
    private EInputDevice _curDevice = EInputDevice.None;
    /// <summary>
    /// 当前设备(原始)
    /// </summary>
    private InputDevice _curRawDevice;

    /// <summary>
    /// 进行中的重绑定操作
    /// </summary>
    private RebindingOperation _rebindOp;
    /// <summary>
    /// 重绑定的目标Action
    /// </summary>
    private InputAction _rebindAction;
    /// <summary>
    /// 重绑定的目标binding下标，-1表示未指定
    /// </summary>
    private int _rebindBindingIndex = -1;
    /// <summary>
    /// 重绑定开始前Action是否处于启用状态
    /// </summary>
    private bool _rebindActionEnabled;
    /// <summary>
    /// 重绑定完成的回调
    /// </summary>
    private Action<InputRebindResult> _rebindCallback;
    /// <summary>
    /// 待释放的重绑定操作
    ///
    /// 注：InputSystem在回调返回后仍会访问该对象（<c>ResetAfterMatchCompleted</c>），
    /// 因此绝不能在回调中Dispose，只能延迟到下一个安全点释放。
    /// </summary>
    private RebindingOperation _pendingDisposeOp;
    /// <summary>
    /// 是否正处于重绑定回调中
    /// </summary>
    private bool _inRebindCallback;

    /// <summary>
    /// 逻辑帧累计时间(不受timeScale影响)
    /// </summary>
    private double _unscaledTime;
    /// <summary>
    /// 逻辑帧计数
    /// </summary>
    private long _frameCount;
    /// <summary>
    /// 最近一次采集输入的更新步计数
    ///
    /// 注：用于过滤不推进更新步的<see cref="InputUpdateType.BeforeRender"/>回调，避免边沿被重复累积。
    /// </summary>
    private uint _lastUpdateStep;

    [Inject]
    public InputManager() {
    }

    #region 属性

    /// <summary>
    /// 是否已启动
    /// </summary>
    public bool Started => _started;

    /// <summary>
    /// 当前使用的设备(粗粒度)
    ///
    /// 注：指玩家<b>最近一次</b>产生输入的设备，用于切换UI上的按键提示图标。
    /// </summary>
    public EInputDevice CurDevice => _curDevice;

    /// <summary>
    /// 当前使用的设备(原始)
    ///
    /// 注：尚无任何输入时为null。
    /// </summary>
    public InputDevice CurRawDevice => _curRawDevice;

    /// <summary>
    /// 上下文栈，下标0是栈顶(只读)
    ///
    /// 注：请通过<see cref="PushContext"/>/<see cref="PopContext"/>修改。
    /// </summary>
    public IReadOnlyList<InputContext> ContextStack => _contextStack;

    /// <summary>
    /// 逻辑帧累计时间
    /// </summary>
    public double UnscaledTime => _unscaledTime;

    /// <summary>
    /// 逻辑帧计数
    /// </summary>
    public long FrameCount => _frameCount;

    /// <summary>
    /// 重绑定数据的存储
    ///
    /// 注：默认是<see cref="PlayerPrefsBindingStore"/>，可替换为项目自己的存档系统。
    /// </summary>
    public IInputBindingStore BindingStore {
        get => _bindingStore;
        set => _bindingStore = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// 是否正在进行按键重绑定
    /// </summary>
    public bool Rebinding => _rebindOp != null;

    /// <summary>
    /// 当前使用的设备发生变化
    ///
    /// 注：用于驱动UI按键提示图标的切换。
    /// </summary>
    public event Action<EInputDevice> OnDeviceChanged;

    /// <summary>
    /// 设备接入或移除
    ///
    /// 注：参数分别是设备和变更类型，可用于提示"手柄已断开"。
    /// </summary>
    public event Action<InputDevice, InputDeviceChange> OnDeviceStateChanged;

    #endregion

    #region 生命周期

    /// <summary>
    /// 启动管理器，挂接InputSystem的回调
    /// </summary>
    public void Start() {
        if (_started) {
            return;
        }
        _started = true;
        // onAfterUpdate在每个输入更新步结束时触发，是采集输入边沿的唯一可靠时机
        InputSystem.onAfterUpdate += OnAfterInputUpdate;
        InputSystem.onActionChange += OnActionChange;
        InputSystem.onDeviceChange += OnDeviceChange;
    }

    /// <summary>
    /// 停止管理器并释放资源
    ///
    /// 注：
    /// 1.该方法是<b>终态</b>操作 —— 会取消重绑定、禁用并清空所有上下文、释放代码构建的Map
    ///   （<see cref="InputActionMap"/>持有非托管的InputActionState）。停止后不应再使用该实例。
    /// 2.必须在销毁时调用，否则挂在<see cref="InputSystem"/>上的回调会让死实例活到下一次域重载。
    /// </summary>
    public void Stop() {
        if (!_started) {
            return;
        }
        _started = false;
        InputSystem.onAfterUpdate -= OnAfterInputUpdate;
        InputSystem.onActionChange -= OnActionChange;
        InputSystem.onDeviceChange -= OnDeviceChange;
        //
        CancelRebind();
        DisposePendingRebindOp();
        for (int i = 0; i < _contextStack.Count; i++) {
            InputContext context = _contextStack[i];
            context.SetActive(false);
            context.SetPushed(false);
        }
        _contextStack.Clear();
        _contextMap.Clear();
        _bindingOwners.Clear();
        // 释放代码构建的Map；来自资源的Map由资源系统负责，不能在此释放
        for (int i = 0; i < _standaloneMaps.Count; i++) {
            try {
                _standaloneMaps[i].Dispose();
            }
            catch (Exception e) {
                logger.Warn(e, "dispose action map caught exception, map: " + _standaloneMaps[i].name);
            }
        }
        _standaloneMaps.Clear();
        //
        _lastUpdateStep = 0;
        _curRawDevice = null;
        _curDevice = EInputDevice.None;
    }

    /// <summary>
    /// 逻辑帧开始 —— 将累积的输入边沿交付给逻辑层
    ///
    /// 注：
    /// 1.必须在所有读取输入的逻辑之前调用。
    /// 2.该方法可以不是每个渲染帧都调用（比如只在FixedUpdate推进逻辑帧时调用），
    ///   输入不会因此丢失，参见<see cref="InputActionSlot"/>的说明。
    /// </summary>
    /// <param name="unscaledDeltaTime">不受timeScale影响的帧间隔</param>
    public void BeginOfFrame(double unscaledDeltaTime) {
        _unscaledTime += unscaledDeltaTime;
        _frameCount++;
        // 逻辑帧开始时必然不在InputSystem的回调中，是释放重绑定操作的安全点
        DisposePendingRebindOp();
        //
        List<InputContext> contextStack = _contextStack;
        for (int i = 0; i < contextStack.Count; i++) {
            InputContext context = contextStack[i];
            if (!context.Active) {
                continue;
            }
            List<InputActionSlot> slots = context.SlotList;
            for (int j = 0; j < slots.Count; j++) {
                slots[j].Snapshot();
            }
        }
    }

    /// <summary>
    /// 逻辑帧结束 —— 清理本帧的输入边沿
    ///
    /// 注：只清理已交付的边沿，本帧内新到达的输入会保留到下一个逻辑帧。
    /// </summary>
    public void EndOfFrame() {
        List<InputContext> contextStack = _contextStack;
        for (int i = 0; i < contextStack.Count; i++) {
            InputContext context = contextStack[i];
            if (!context.Active) {
                continue;
            }
            List<InputActionSlot> slots = context.SlotList;
            for (int j = 0; j < slots.Count; j++) {
                slots[j].ClearFrame();
            }
        }
    }

    #endregion

    #region Action来源

    /// <summary>
    /// 注册一个ActionAsset，使其参与重绑定的持久化
    ///
    /// 注：
    /// 1.该方法不会启用其中的Action，你仍需建立上下文并压栈。
    /// 2.持久化的键为<c>"asset." + asset.name</c>，因此多个资源不应重名。
    /// </summary>
    /// <param name="asset">Action资源</param>
    public void RegisterAsset(InputActionAsset asset) {
        if (asset == null) throw new ArgumentNullException(nameof(asset));
        if (string.IsNullOrEmpty(asset.name)) {
            throw new ArgumentException("asset name is empty");
        }
        RegisterBindingOwner("asset." + asset.name, asset);
    }

    /// <summary>
    /// 创建一个由代码构建的ActionMap
    ///
    /// 注：
    /// 1.返回的Map由管理器持有（避免被GC），并自动参与重绑定的持久化。
    /// 2.请使用<see cref="InputActionSetupExtensions"/>中的扩展方法向其添加Action和Binding。
    /// </summary>
    /// <param name="name">Map名，同时作为持久化的键</param>
    public InputActionMap CreateMap(string name) {
        if (string.IsNullOrEmpty(name)) throw new ArgumentException("name is empty");
        InputActionMap map = new InputActionMap(name);
        _standaloneMaps.Add(map);
        RegisterBindingOwner("map." + name, map);
        return map;
    }

    /// <summary>
    /// 注册一个参与重绑定持久化的Action集合
    ///
    /// 注：<see cref="InputActionAsset"/>和<see cref="InputActionMap"/>都实现了
    /// <see cref="IInputActionCollection2"/>，因此都可以直接传入。
    /// </summary>
    /// <param name="key">持久化的键，必须唯一</param>
    /// <param name="collection">Action集合</param>
    public void RegisterBindingOwner(string key, IInputActionCollection2 collection) {
        if (string.IsNullOrEmpty(key)) throw new ArgumentException("key is empty");
        if (collection == null) throw new ArgumentNullException(nameof(collection));
        for (int i = 0; i < _bindingOwners.Count; i++) {
            if (_bindingOwners[i].key == key) {
                throw new ArgumentException("duplicate binding owner key: " + key);
            }
        }
        _bindingOwners.Add(new BindingOwner(key, collection));
    }

    #endregion

    #region 上下文管理

    /// <summary>
    /// 创建输入上下文
    /// </summary>
    /// <param name="name">上下文名，必须唯一</param>
    /// <param name="priority">优先级，越大越靠栈顶</param>
    /// <param name="exclusive">是否屏蔽下层上下文（模态语义）</param>
    /// <exception cref="ArgumentException">名字重复</exception>
    public InputContext CreateContext(string name, int priority = 0, bool exclusive = false) {
        if (string.IsNullOrEmpty(name)) throw new ArgumentException("name is empty");
        if (_contextMap.ContainsKey(name)) {
            throw new ArgumentException("duplicate context name: " + name);
        }
        InputContext context = new InputContext(name, priority, exclusive);
        _contextMap[name] = context;
        return context;
    }

    /// <summary>
    /// 查询上下文
    /// </summary>
    /// <param name="name">上下文名</param>
    /// <returns>不存在时返回null</returns>
    public InputContext GetContext(string name) {
        return _contextMap.TryGetValue(name, out InputContext context) ? context : null;
    }

    /// <summary>
    /// 压入上下文
    ///
    /// 注：重复压入同一上下文不会有任何效果。
    /// </summary>
    /// <param name="name">上下文名</param>
    /// <exception cref="ArgumentException">上下文不存在</exception>
    public void PushContext(string name) {
        InputContext context = GetContext(name);
        if (context == null) {
            throw new ArgumentException("context not found: " + name);
        }
        PushContext(context);
    }

    /// <summary>
    /// 压入上下文
    /// </summary>
    /// <param name="context">上下文</param>
    public void PushContext(InputContext context) {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (context.Pushed) {
            return;
        }
        context.SetPushed(true);
        // 按优先级插入；相同优先级时后压入的更靠栈顶
        int index = 0;
        while (index < _contextStack.Count && _contextStack[index].Priority > context.Priority) {
            index++;
        }
        _contextStack.Insert(index, context);
        RefreshActiveContexts();
    }

    /// <summary>
    /// 弹出上下文
    /// </summary>
    /// <param name="name">上下文名</param>
    /// <returns>是否发生了弹出</returns>
    public bool PopContext(string name) {
        InputContext context = GetContext(name);
        return context != null && PopContext(context);
    }

    /// <summary>
    /// 弹出上下文
    /// </summary>
    /// <param name="context">上下文</param>
    /// <returns>是否发生了弹出</returns>
    public bool PopContext(InputContext context) {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (!context.Pushed) {
            return false;
        }
        context.SetPushed(false);
        context.SetActive(false);
        _contextStack.Remove(context);
        RefreshActiveContexts();
        return true;
    }

    /// <summary>
    /// 重算上下文的生效状态
    ///
    /// 注：从栈顶向下遍历，遇到<see cref="InputContext.Exclusive"/>的上下文后，其下方全部失效。
    /// </summary>
    private void RefreshActiveContexts() {
        bool blocked = false;
        List<InputContext> contextStack = _contextStack;
        for (int i = 0; i < contextStack.Count; i++) {
            InputContext context = contextStack[i];
            context.SetActive(!blocked);
            if (!blocked && context.Exclusive) {
                blocked = true;
            }
        }
    }

    #endregion

    #region Action查询

    /// <summary>
    /// 查询槽 —— 从栈顶向下查找第一个生效的同名Action
    /// </summary>
    /// <param name="actionName">Action名</param>
    /// <returns>不存在时返回null</returns>
    public InputActionSlot GetSlot(string actionName) {
        List<InputContext> contextStack = _contextStack;
        for (int i = 0; i < contextStack.Count; i++) {
            InputContext context = contextStack[i];
            if (!context.Active) {
                continue;
            }
            InputActionSlot slot = context.GetSlot(actionName);
            if (slot != null) {
                return slot;
            }
        }
        return null;
    }

    /// <summary>
    /// 查询槽，不存在则抛异常
    ///
    /// 注：Action名写错是编程错误，因此默认抛异常而非静默返回null；
    /// 需要容错请使用<see cref="GetSlot"/>。
    /// </summary>
    /// <param name="actionName">Action名</param>
    /// <exception cref="ArgumentException">Action不存在或所在上下文未生效</exception>
    public InputActionSlot this[string actionName] {
        get {
            InputActionSlot slot = GetSlot(actionName);
            if (slot == null) {
                throw new ArgumentException("action not found or inactive: " + actionName);
            }
            return slot;
        }
    }

    #endregion

    #region 按键重绑定

    /// <summary>
    /// 开始交互式按键重绑定 —— 等待玩家按下新的按键
    ///
    /// 注：
    /// 1.同一时刻只能有一个重绑定操作，开始新的操作会取消旧的。
    /// 2.重绑定期间会临时禁用目标Action，避免玩家的按键被当作正常输入处理。
    /// 3.InputSystem默认已排除指针的position/delta（否则鼠标一动就完成绑定），
    ///   但只在非Button类型的Action上自动加Esc取消，因此这里统一补上。
    /// 4.<paramref name="cancelPath"/>传null可禁用取消键（此时只能由UI调用<see cref="CancelRebind"/>），
    ///   若要把Esc本身绑定给某个Action，也需要传null。
    /// 5.<paramref name="configurator"/>中<b>不要</b>调用<c>OnComplete</c>/<c>OnCancel</c> ——
    ///   它们是赋值语义而非事件订阅，会覆盖掉框架的清理逻辑；请改用<paramref name="callback"/>。
    /// 6.重绑定结果不会自动落盘，需要调用<see cref="SaveBindings"/>。
    /// </summary>
    /// <param name="action">目标Action</param>
    /// <param name="bindingIndex">目标binding下标；-1表示不指定，此时override会应用到该Action的<b>所有</b>binding</param>
    /// <param name="callback">完成或取消时的回调</param>
    /// <param name="cancelPath">取消重绑定的控件路径，null表示不设置取消键</param>
    /// <param name="configurator">用于定制重绑定操作，在框架安装回调之前执行</param>
    /// <returns>是否成功开始</returns>
    public bool StartRebind(InputAction action, int bindingIndex = -1,
                            Action<InputRebindResult> callback = null,
                            string cancelPath = DefaultRebindCancelPath,
                            Action<RebindingOperation> configurator = null) {
        if (action == null) throw new ArgumentNullException(nameof(action));
        CancelRebind();
        //
        _rebindAction = action;
        _rebindBindingIndex = bindingIndex;
        _rebindActionEnabled = action.enabled;
        _rebindCallback = callback;
        // 必须先禁用，否则玩家按下的键会同时触发该Action(且WithAction会因Action启用而抛异常)
        if (_rebindActionEnabled) {
            action.Disable();
        }
        RebindingOperation op = null;
        try {
            op = action.PerformInteractiveRebinding(bindingIndex);
            if (!string.IsNullOrEmpty(cancelPath)) {
                op.WithCancelingThrough(cancelPath);
            }
            // 用户定制先执行，框架回调最后安装 —— OnComplete/OnCancel是赋值语义，
            // 反序会让用户的回调覆盖掉ClearRebind，导致Action被永久禁用
            configurator?.Invoke(op);
            op.OnComplete(OnRebindComplete).OnCancel(OnRebindCancel);
            _rebindOp = op;
            op.Start();
            return true;
        }
        catch (Exception e) {
            logger.Warn(e, "start rebind caught exception, action: " + action.name);
            // 尚未接管的op需要自己释放，否则非托管内存要等终结器
            if (!ReferenceEquals(_rebindOp, op)) {
                op?.Dispose();
            }
            // 恢复现场，避免Action被永久禁用；此处不在回调中，可直接释放
            ClearRebind(false);
            return false;
        }
    }

    /// <summary>
    /// 取消进行中的重绑定
    /// </summary>
    /// <returns>是否发生了取消</returns>
    public bool CancelRebind() {
        RebindingOperation op = _rebindOp;
        if (op == null) {
            DisposePendingRebindOp();
            return false;
        }
        // Cancel会同步回调OnRebindCancel，由后者负责清理
        op.Cancel();
        // 兜底：若op尚未Start导致Cancel直接返回，此处强制清理。
        // 注：必须比对引用 —— 用户可能在取消回调里又发起了新的重绑定，不能把新op拆掉
        if (ReferenceEquals(_rebindOp, op)) {
            ClearRebind(true);
        }
        // Cancel返回后InputSystem已不再访问op，可以安全释放
        DisposePendingRebindOp();
        return true;
    }

    private void OnRebindComplete(RebindingOperation op) {
        InputAction action = _rebindAction;
        int bindingIndex = _rebindBindingIndex;
        // 优先取binding上已生效的(泛化)路径，它才是真正被持久化的值；
        // 未指定下标时退化为玩家按下的具体控件路径
        string newPath = null;
        if (action != null && bindingIndex >= 0 && bindingIndex < action.bindings.Count) {
            newPath = action.bindings[bindingIndex].effectivePath;
        }
        if (newPath == null) {
            newPath = op.selectedControl?.path;
        }
        Action<InputRebindResult> callback = _rebindCallback;
        //
        ClearRebind(true);
        InvokeRebindCallback(callback, new InputRebindResult(true, action, bindingIndex, newPath));
    }

    private void OnRebindCancel(RebindingOperation op) {
        InputAction action = _rebindAction;
        int bindingIndex = _rebindBindingIndex;
        Action<InputRebindResult> callback = _rebindCallback;
        //
        ClearRebind(true);
        InvokeRebindCallback(callback, new InputRebindResult(false, action, bindingIndex, null));
    }

    /// <summary>
    /// 清理重绑定现场，并恢复Action的启用状态
    ///
    /// 注：若重绑定期间目标Action所在的上下文被压栈/弹栈，这里恢复的启用状态可能与上下文不一致；
    /// 调用方应避免在改键界面打开时切换输入上下文。
    /// </summary>
    /// <param name="deferDispose">是否延迟释放操作对象，在InputSystem的回调中必须为true</param>
    private void ClearRebind(bool deferDispose) {
        RebindingOperation op = _rebindOp;
        _rebindOp = null;
        _rebindCallback = null;
        _rebindBindingIndex = -1;
        InputAction action = _rebindAction;
        _rebindAction = null;
        //
        if (action != null && _rebindActionEnabled) {
            action.Enable();
        }
        _rebindActionEnabled = false;
        //
        if (op == null) {
            return;
        }
        if (deferDispose) {
            DisposePendingRebindOp(); // 先释放上一个，避免堆积
            _pendingDisposeOp = op;
        } else {
            op.Dispose();
        }
    }

    /// <summary>
    /// 释放延迟到现在的重绑定操作
    ///
    /// 注：处于重绑定回调中时必须跳过 —— InputSystem在回调返回后还要访问该对象，
    /// 而用户可能在回调里调用<see cref="StartRebind"/>/<see cref="CancelRebind"/>间接走到这里。
    /// </summary>
    private void DisposePendingRebindOp() {
        if (_inRebindCallback) {
            return;
        }
        RebindingOperation op = _pendingDisposeOp;
        if (op == null) {
            return;
        }
        _pendingDisposeOp = null;
        try {
            op.Dispose();
        }
        catch (Exception e) {
            logger.Warn(e, "dispose rebind operation caught exception");
        }
    }

    /// <summary>
    /// 调用重绑定回调
    ///
    /// 注：调用期间置位<see cref="_inRebindCallback"/>，防止用户回调中的重入操作
    /// 提前释放InputSystem仍在使用的操作对象。
    /// </summary>
    private void InvokeRebindCallback(Action<InputRebindResult> callback, InputRebindResult result) {
        bool prev = _inRebindCallback;
        _inRebindCallback = true;
        try {
            callback?.Invoke(result);
        }
        catch (Exception e) {
            logger.Warn(e, "rebind callback caught exception");
        }
        finally {
            _inRebindCallback = prev;
        }
    }

    #endregion

    #region 重绑定持久化

    /// <summary>
    /// 为代码构建的Map分配确定性的binding id
    ///
    /// 1.<c>LoadBindingOverridesFromJson</c><b>只按binding id匹配</b>，匹配不上就直接丢弃并打警告。
    ///   而<see cref="InputActionSetupExtensions"/>为新建的binding分配的是<c>Guid.NewGuid()</c>，
    ///   每次运行都不同 —— 若不封存，代码构建的Map的改键永远无法跨会话恢复。
    ///   （来自资源的Map不受影响，它们的id是序列化在资源里的。）
    /// 2.id由"Map名 + Action名 + binding下标"派生，因此要求每次运行的构建顺序一致（代码构建天然满足）。
    /// 3.该方法幂等，且保留已有的override。<see cref="SaveBindings"/>/<see cref="LoadBindings"/>
    ///   会自动对<see cref="CreateMap"/>创建的Map调用它，通常无需手动调用。
    /// </summary>
    /// <param name="map">代码构建的ActionMap</param>
    public void SealMap(InputActionMap map) {
        if (map == null) throw new ArgumentNullException(nameof(map));
        // 每轮重新取bindings —— 循环体内的ChangeBinding会触发OnBindingModified，不缓存更稳妥
        for (int i = 0, count = map.bindings.Count; i < count; i++) {
            InputBinding binding = map.bindings[i];
            Guid id = MakeStableGuid(map.name, binding.action, i);
            if (binding.id == id) {
                continue; // 已封存
            }
            // 只改id，overridePath等其它字段原样写回
            binding.id = id;
            map.ChangeBinding(i).To(binding);
        }
    }

    /// <summary>
    /// 封存所有代码构建的Map
    /// </summary>
    private void SealStandaloneMaps() {
        List<InputActionMap> maps = _standaloneMaps;
        for (int i = 0; i < maps.Count; i++) {
            try {
                SealMap(maps[i]);
            }
            catch (Exception e) {
                logger.Warn(e, "seal map caught exception, map: " + maps[i].name);
            }
        }
    }

    /// <summary>
    /// 由字符串派生确定性的Guid
    ///
    /// 注：用FNV-1a而非MD5，一是避免IL2CPP裁剪<c>System.Security.Cryptography</c>的风险，
    /// 二是保证跨平台、跨运行结果完全一致（<c>string.GetHashCode</c>不保证这一点）。
    /// </summary>
    private static Guid MakeStableGuid(string mapName, string actionName, int bindingIndex) {
        // 长度前缀拼接，彻底排除撞键（否则 "a"+"bc" 与 "ab"+"c" 会得到同一个key）。
        // 注：复合binding的分部(part)其action字段为空，因此需要容忍null/空串。
        string map = mapName ?? "";
        string action = actionName ?? "";
        string key = map.Length + ":" + map + action.Length + ":" + action + bindingIndex;
        const ulong basis = 14695981039346656037UL;
        ulong h1 = Fnv1a64(key, basis);
        ulong h2 = Fnv1a64(key, basis ^ 0x9E3779B97F4A7C15UL);
        byte[] bytes = new byte[16];
        for (int i = 0; i < 8; i++) {
            bytes[i] = (byte)(h1 >> (i * 8));
            bytes[8 + i] = (byte)(h2 >> (i * 8));
        }
        return new Guid(bytes);
    }

    private static ulong Fnv1a64(string key, ulong hash) {
        const ulong prime = 1099511628211UL;
        for (int i = 0; i < key.Length; i++) {
            char c = key[i];
            hash = (hash ^ (byte)c) * prime;
            hash = (hash ^ (byte)(c >> 8)) * prime;
        }
        return hash;
    }

    /// <summary>
    /// 保存所有重绑定数据
    ///
    /// 注：没有任何改键的集合会从存储中删除，避免残留脏数据。
    /// </summary>
    public void SaveBindings() {
        SealStandaloneMaps();
        List<BindingOwner> owners = _bindingOwners;
        for (int i = 0; i < owners.Count; i++) {
            BindingOwner owner = owners[i];
            try {
                string json = owner.collection.SaveBindingOverridesAsJson();
                if (string.IsNullOrEmpty(json)) {
                    _bindingStore.Delete(owner.key);
                } else {
                    _bindingStore.Save(owner.key, json);
                }
            }
            catch (Exception e) {
                logger.Warn(e, "save bindings caught exception, key: " + owner.key);
            }
        }
        FlushBindingStore();
    }

    /// <summary>
    /// 加载所有重绑定数据
    ///
    /// 注：需要在Action构建完毕之后调用（压栈前后都可以）。
    /// </summary>
    public void LoadBindings() {
        SealStandaloneMaps();
        List<BindingOwner> owners = _bindingOwners;
        for (int i = 0; i < owners.Count; i++) {
            BindingOwner owner = owners[i];
            try {
                string json = _bindingStore.Load(owner.key);
                if (!string.IsNullOrEmpty(json)) {
                    owner.collection.LoadBindingOverridesFromJson(json);
                }
            }
            catch (Exception e) {
                logger.Warn(e, "load bindings caught exception, key: " + owner.key);
            }
        }
    }

    private void FlushBindingStore() {
        try {
            _bindingStore.Flush();
        }
        catch (Exception e) {
            logger.Warn(e, "flush binding store caught exception");
        }
    }

    /// <summary>
    /// 重置所有按键为默认值，并清空存储
    /// </summary>
    public void ResetBindings() {
        List<BindingOwner> owners = _bindingOwners;
        for (int i = 0; i < owners.Count; i++) {
            BindingOwner owner = owners[i];
            try {
                owner.collection.RemoveAllBindingOverrides();
                _bindingStore.Delete(owner.key);
            }
            catch (Exception e) {
                logger.Warn(e, "reset bindings caught exception, key: " + owner.key);
            }
        }
        FlushBindingStore();
    }

    #endregion

    #region InputSystem回调

    /// <summary>
    /// 每个输入更新步结束时采集输入边沿
    ///
    /// 注：
    /// 1.这是帧锁存的核心 —— 逻辑帧可能跳过某些渲染帧，但该回调不会跳过任何更新步。
    /// 2.<c>onAfterUpdate</c>对<see cref="InputUpdateType.BeforeRender"/>也会触发（存在XR等
    ///   updateBeforeRender设备时），但<c>BeforeRender</c><b>不推进</b>更新步计数，
    ///   于是同一次按下会被计两次。因此这里按更新步去重。
    /// </summary>
    private void OnAfterInputUpdate() {
        uint updateStep = InputState.updateCount;
        if (updateStep == _lastUpdateStep) {
            return;
        }
        _lastUpdateStep = updateStep;
        //
        List<InputContext> contextStack = _contextStack;
        for (int i = 0; i < contextStack.Count; i++) {
            InputContext context = contextStack[i];
            if (!context.Active) {
                continue;
            }
            List<InputActionSlot> slots = context.SlotList;
            for (int j = 0; j < slots.Count; j++) {
                slots[j].Accumulate();
            }
        }
    }

    /// <summary>
    /// 跟踪玩家最近使用的设备
    /// </summary>
    private void OnActionChange(object obj, InputActionChange change) {
        // Started比Performed更早，且不受交互器影响，用于设备探测更及时
        if (change != InputActionChange.ActionStarted && change != InputActionChange.ActionPerformed) {
            return;
        }
        if (!(obj is InputAction action)) {
            return;
        }
        InputDevice device = action.activeControl?.device;
        if (device == null) {
            return;
        }
        SetCurDevice(device);
    }

    private void SetCurDevice(InputDevice device) {
        if (ReferenceEquals(_curRawDevice, device)) {
            return;
        }
        _curRawDevice = device;
        EInputDevice kind = device.ToDeviceKind();
        if (_curDevice == kind) {
            return;
        }
        _curDevice = kind;
        try {
            OnDeviceChanged?.Invoke(kind);
        }
        catch (Exception e) {
            logger.Warn(e, "OnDeviceChanged caught exception");
        }
    }

    private void OnDeviceChange(InputDevice device, InputDeviceChange change) {
        // 当前设备被移除时清空，以便下次输入重新探测
        if (ReferenceEquals(_curRawDevice, device)
            && (change == InputDeviceChange.Removed || change == InputDeviceChange.Disconnected)) {
            _curRawDevice = null;
            _curDevice = EInputDevice.None;
            try {
                OnDeviceChanged?.Invoke(EInputDevice.None);
            }
            catch (Exception e) {
                logger.Warn(e, "OnDeviceChanged caught exception");
            }
        }
        try {
            OnDeviceStateChanged?.Invoke(device, change);
        }
        catch (Exception e) {
            logger.Warn(e, "OnDeviceStateChanged caught exception");
        }
    }

    #endregion

    /// <summary>
    /// 参与重绑定持久化的Action集合
    /// </summary>
    private readonly struct BindingOwner
    {
        internal readonly string key;
        internal readonly IInputActionCollection2 collection;

        internal BindingOwner(string key, IInputActionCollection2 collection) {
            this.key = key;
            this.collection = collection;
        }
    }
}
}
