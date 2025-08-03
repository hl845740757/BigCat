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
using Wjybxx.BigCat.Util;
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Fx;
using Wjybxx.Commons.Inject;
using Wjybxx.Commons.Logger;
using Wjybxx.Dson.Codec;
using Wjybxx.Dson.Codec.Attributes;

#if UNITY_2021_3_OR_NEWER
using UnityEngine;
using UnityScene = UnityEngine.SceneManagement.Scene;
using ILogger = Wjybxx.Commons.Logger.ILogger;
#endif

namespace Wjybxx.BigCat.Gameplay
{
/// <summary>
/// 游戏世界（场景抽象）
///
/// 1.面向对象：Scene上同时包含数据组件和行为组件，组件在编辑器中配置。
/// 2.可反序列化创建实例：Scene组件在编辑器中配置。
/// 3.Scene在初始化完成后禁止增删组件，只有在New状态下可增删组件。
/// 4.Scene暂时限制最多128类组件。
/// 5.数据组件不支持重复，需要提前将元素转换为List类型。
/// </summary>
[DsonSerializable(SkipFields = new[] { "*" })]
public sealed class Scene
{
    internal static readonly ILogger logger = LoggerFactory.GetLogger<Scene>();
#nullable disable
    /// <summary>
    /// 配置表id
    /// </summary>
    private int _configId;
    /// <summary>
    ///实例id
    /// </summary>
    [NonSerialized] private long _instId;
    /// <summary>
    /// 对象的状态
    /// </summary>
    [NonSerialized] private ComponentStatus _status = ComponentStatus.New;
    /// <summary>
    /// 激活状态（前后台状态）
    /// </summary>
    [NonSerialized] private bool _active = true;

    /// <summary>
    /// 场景关联的管理器
    /// </summary>
    [NonSerialized] private SceneMgr _sceneMgr;
    /// <summary>
    /// 内部代理
    /// 注意：该代理由用户创建
    /// </summary>
    [NonSerialized] private SceneAgent _agent;
    /// <summary>
    /// Scene的依赖
    /// </summary>
    [NonSerialized] private IInjector _injector;
    /// <summary>
    /// 用户自定义数据
    /// </summary>
    [NonSerialized] private object? _userData;
#if UNITY_2021_3_OR_NEWER
    /// <summary>
    /// 关联的引擎对象
    /// </summary>
    [NonSerialized] public UnityScene unityScene;
#endif

    /// <summary>
    /// 绑定的组件
    /// </summary>
    private readonly List<SComponent> _components = new List<SComponent>();
    /// <summary>
    /// 索引后的组件 - 提高查询速度
    /// 
    /// PS：场景的数量较少，且其组件数也较少，因此无需在意这里的空间浪费。
    /// </summary>
    [NonSerialized] private readonly ComponentList<SComponent?> _indexedComponents = new(SComponentListHelper.Inst);
    /// <summary>
    /// 我们调度的单位是Scene
    /// (scene作为一个完全独立的环境进行调度)
    /// </summary>
    [NonSerialized] private readonly IndexedDynamicArray<SComponent> _fixedUpdateList = new(SComponentIndexHelper.GetInst(1), 10);
    [NonSerialized] private readonly IndexedDynamicArray<SComponent> _earlyUpdateList = new(SComponentIndexHelper.GetInst(2), 10);
    [NonSerialized] private readonly IndexedDynamicArray<SComponent> _updateList = new(SComponentIndexHelper.GetInst(3), 10);
    [NonSerialized] private readonly IndexedDynamicArray<SComponent> _lateUpdateList = new(SComponentIndexHelper.GetInst(4), 10);

    /// <summary>
    /// 在队列中的索引缓存
    /// </summary>
    [NonSerialized] internal SIndexes indexes = SIndexes.Create();
    /// <summary>
    /// 场景自己的时间轴
    /// </summary>
    [NonSerialized] private readonly GTime _time = new GTime();
    /// <summary>
    /// 场景中的所有游戏对象
    /// </summary>
    [NonSerialized] private readonly GameUnitMgr _gameUnitMgr;
#nullable restore

    public Scene() {
        _gameUnitMgr = new GameUnitMgr(this);
    }

    public Scene(IDsonObjectReader reader) : this() {
        _configId = reader.ReadInt("configId");
        //
        List<SComponent> components = reader.ReadObject<List<SComponent>>("components");
        _components.EnsureCapacity(components.Count);
        _indexedComponents.EnsureCapacity(components.Count);
        foreach (SComponent component in components) {
            _components.Add(component);
            _indexedComponents.Add(component);
        }
    }

    public void WriteObject(IDsonObjectWriter writer) {
        writer.WriteInt("configId", _configId);
        writer.WriteObject("components", _components);
    }

    #region prop

    public int ConfigId {
        get => _configId;
        set {
            CheckStatus();
            _configId = value;
        }
    }
    public long InstId {
        get => _instId;
        set {
            CheckStatus();
            _instId = value;
        }
    }

    public ComponentStatus Status => _status;

    public SceneMgr SceneMgr {
        get => _sceneMgr;
        set {
            CheckStatus();
            _sceneMgr = value;
        }
    }

    public SceneAgent Agent {
        get => _agent;
        set {
            CheckStatus();
            _agent = value;
        }
    }

    public IInjector Injector {
        get => _injector;
        set {
            CheckStatus();
            _injector = value;
        }
    }

    public object? UserData {
        get => _userData;
        set => _userData = value;
    }

    /// <summary>
    /// 是否处于激活状态
    /// </summary>
    public bool IsActive {
        get => _active;
        set => SetActive(value); // 可能反序列化调用
    }

    /// <summary>
    /// 设置对象的激活状态
    ///
    /// 注：
    /// 1.场景的Active状态用于控制前后台运行，而非暂停场景；暂停场景请执行<see cref="Pause"/>。
    /// 2.主要用于客户端禁用音效特效等逻辑，服务器通常不应该使用该逻辑。
    /// </summary>
    /// <param name="value">自身的状态</param>
    public void SetActive(bool value) {
        if (value == IsActive) {
            return;
        }
        _active = value;
        // 状态变化事件 - 运行状态下才处理
        if (_status == ComponentStatus.Running || _status == ComponentStatus.Suspended) {
            _agent?.OnActiveChanged();
        }
    }

    public GTime Time => _time;
    public GameUnitMgr GameUnitMgr => _gameUnitMgr;

    private void CheckStatus() {
        if (_status != ComponentStatus.New) {
            throw new InvalidOperationException();
        }
    }

    #endregion

#nullable disable

    #region 组件模式

    /// <summary>
    /// 
    /// </summary>
    /// <param name="comp">套添加的组件</param>
    /// <param name="addFirst">是否添加到首部，通常用于插入基础组件</param>
    public void AddComponent(SComponent comp, bool addFirst = false) {
        if (comp == null) throw new ArgumentNullException(nameof(comp));
        if (_status != ComponentStatus.New) throw new InvalidOperationException();
        if (addFirst) {
            _components.Insert(0, comp);
        } else {
            _components.Add(comp);
        }
        _indexedComponents.Add(comp, addFirst);
    }

    /// <summary>
    /// 是否包含目标组件
    /// </summary>
    public bool ContainsComponent(SComponent comp) {
        return _indexedComponents.Contains(comp);
    }

    /// <summary>
    /// 当前组件数量
    /// </summary>
    public int ComponentsCount => _components.Count;

    /// <summary>
    /// 组件的掩码，用户快速测试场景包含的组件
    /// </summary>
    public GBitSet ComponentsMask => _indexedComponents.Mask;

    /// <summary>
    /// 获取原始的组件List
    /// 注：不可直接修改List
    /// </summary>
    public List<SComponent> Components => _components;

    // 泛型
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetComponent<T>(ComponentId<T> cid) where T : class {
        return _indexedComponents.Get(cid);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetLastComponent<T>(ComponentId<T> cid) where T : class {
        return _indexedComponents.GetLast(cid);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public List<T> GetComponents<T>(ComponentId<T> cid, List<T>? outList = null) where T : class {
        outList ??= new List<T>();
        _indexedComponents.Get(cid, outList);
        return outList;
    }

    // 非泛型
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CountComponent(ComponentId cid) {
        return _indexedComponents.Count(cid);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SComponent? GetComponent(ComponentId cid) {
        return _indexedComponents.Get(cid);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SComponent? GetLastComponent(ComponentId cid) {
        return _indexedComponents.GetLast(cid);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public List<SComponent> GetComponents(ComponentId cid, List<SComponent>? outList = null) {
        outList ??= new List<SComponent>();
        _indexedComponents.Get(cid, outList);
        return outList;
    }

    #endregion

#nullable restore

    #region 生命周期

    /// <summary>
    /// 将场景标记为已完成初始化
    /// </summary>
    public void SetInitialized() {
        if (_status != ComponentStatus.New) {
            throw new InvalidOperationException();
        }
        _agent?.Inject(this); // 双向绑定
        _status = ComponentStatus.Initialized;
        // 初始化模块
        foreach (SComponent component in _components) {
            if (component.Cid.shared) {
                continue;
            }
            if (component.Status == ComponentStatus.New) {
                component.SetEntity(this);
            }
        }
        // 解决模块之间的依赖
        foreach (SComponent component in _components) {
            if (component.Cid.shared) {
                continue;
            }
            component.ResolveDependence();
        }
    }

    /// <summary>
    /// 启动场景
    /// </summary>
    public void Start() {
        _status = ComponentStatus.Running;
        _time.Restart();
        // Start -- 顺序启动，出现任何异常直接退出
        _agent?.OnStart();
        foreach (SComponent component in _components) {
            if (!component.Cid.IsPrivateScript) {
                continue;
            }
            component.InvokeStart();
        }
        // 初始化Update列表 -- 按照updateOrder排序
        List<SComponent> components = new List<SComponent>(_components);
        components.Sort(ComponentUtil.UpdateOrderComparer);
        foreach (SComponent component in components) {
            if (!component.Cid.IsPrivateScript) {
                continue;
            }
            AddToUpdateList(component);
        }
    }

    /// <summary>
    /// 终止场景运行
    /// 注：由场景的组件(System/Manager)销毁或回收所有的游戏对象。
    /// </summary>
    public void Stop() {
        if (_status < ComponentStatus.Running || _status >= ComponentStatus.Shutdown) {
            return;
        }
        _status = ComponentStatus.Shutdown;
        ClearUpdateList();
        // Stop - 逆序
        for (int index = _components.Count - 1; index >= 0; index--) {
            SComponent component = _components[index];
            if (!component.Cid.IsPrivateScript) {
                continue;
            }
            if (component.Status != ComponentStatus.Running
                && component.Status != ComponentStatus.Suspended) {
                continue;
            }
            try {
                component.InvokeStop();
            }
            catch (Exception ex) {
                logger.Warn(ex, "component stop caught exception");
            }
        }
        try {
            _agent?.OnStop();
        }
        catch (Exception ex) {
            logger.Warn(ex, "agent stop caught exception");
        }

        _status = ComponentStatus.Terminated;
        if (_sceneMgr != null) {
            _sceneMgr.OnTerminated(this);
        }
    }

    /// <summary>
    /// 重置数据
    /// 注：清理运行过程中产生的临时数据，以支持重新启动。
    /// </summary>
    public void Reset() {
        if (_status == ComponentStatus.Destroyed) {
            throw new InvalidOperationException("already destroyed");
        }
        Stop();
        foreach (SComponent component in _components) {
            if (component.Cid.shared) continue;
            if (component.Status <= ComponentStatus.Initialized) continue;
            component.Reset();
        }
        ClearUpdateList();

        if (_status > ComponentStatus.Initialized) {
            _status = ComponentStatus.Initialized;
        }
        _active = true;
        _agent?.Reset();
        _time.Restart();
        _gameUnitMgr.Destroy();
    }

    /// <summary>
    /// 销毁场景
    ///
    /// 注意：UnityScene需要手动销毁
    /// </summary>
    public void Destroy() {
        if (_status == ComponentStatus.Destroyed) {
            return;
        }
        Stop();
        _status = ComponentStatus.Destroyed;
        foreach (SComponent component in _components) {
            if (component.Cid.shared) continue;
            if (component.Status == ComponentStatus.New) continue;
            try {
                component.InvokeDestroy();
            }
            catch (Exception ex) {
                logger.Warn(ex, "component destroy caught exception");
            }
        }
        _components.Clear();
        _indexedComponents.Clear();
        ClearUpdateList();
        _gameUnitMgr.Destroy();
    }

    /// <summary>
    /// 暂停运行
    /// 注意：该方法应当在帧首或帧尾处理
    /// </summary>
    /// <param name="extraInfo">附加信息</param>
    public void Pause(object? extraInfo = null) {
        if (_status == ComponentStatus.Running) {
            _status = ComponentStatus.Suspended;
            _sceneMgr.OnPause(this);
            _agent?.OnPausing(extraInfo);
        }
    }

    /// <summary>
    /// 恢复运行
    /// 注意：该方法应当在帧首或帧尾处理
    /// </summary>
    /// <param name="extraInfo">附加信息</param>
    public void Resume(object? extraInfo = null) {
        if (_status == ComponentStatus.Suspended) {
            _status = ComponentStatus.Running;
            _sceneMgr.OnResume(this);
            _agent?.OnResume(extraInfo);
        }
    }

    #endregion

    #region update

    public void FixedUpdate(double unscaledDeltaTime) {
        _time.FixedUpdate(unscaledDeltaTime);
        IndexedDynamicArray<SComponent> list = _fixedUpdateList;
        if (list.Length == 0) {
            return; // 逻辑层也少有FixedUpdate
        }
        list.BeginItr();
        for (int index = 0, len = list.Length; index < len; index++) {
            SComponent component = list[index];
            if (component == null) {
                continue;
            }
            try {
                component.FixedUpdate();
            }
            catch (Exception e) {
                logger.Warn(e, "component.FixedUpdate caught exception, cid: " + component.Cid);
            }
        }
        list.EndItr();
    }

    /// <summary>
    /// 该方法主要是为协程服务的
    /// </summary>
    /// <param name="unscaledDeltaTime"></param>
    public void EarlyUpdate(double unscaledDeltaTime) {
        _time.Update(unscaledDeltaTime);
        IndexedDynamicArray<SComponent> list = _earlyUpdateList;
        if (list.Length == 0) {
            return; // 逻辑层也少有EarlyUpdate
        }
        list.BeginItr();
        for (int index = 0, len = list.Length; index < len; index++) {
            SComponent component = list[index];
            if (component == null) {
                continue;
            }
            try {
                component.EarlyUpdate();
            }
            catch (Exception e) {
                logger.Warn(e, "component.EarlyUpdate caught exception, cid: " + component.Cid);
            }
        }
        list.EndItr();
    }

    public void Update() {
        IndexedDynamicArray<SComponent> list = _updateList;
        list.BeginItr();
        for (int index = 0, len = list.Length; index < len; index++) {
            SComponent component = list[index];
            if (component == null) {
                continue;
            }
            try {
                component.Update();
            }
            catch (Exception e) {
                logger.Warn(e, "component.Update caught exception, cid: " + component.Cid);
            }
        }
        list.EndItr();
    }

    public void LateUpdate() {
        IndexedDynamicArray<SComponent> list = _lateUpdateList;
        if (list.Length == 0) {
            return; // 逻辑层也少有LateUpdate
        }
        list.BeginItr();
        for (int index = 0, len = list.Length; index < len; index++) {
            SComponent component = list[index];
            if (component == null) {
                continue;
            }
            try {
                component.LateUpdate();
            }
            catch (Exception e) {
                logger.Warn(e, "component.LateUpdate caught exception, cid: " + component.Cid);
            }
        }
        list.EndItr();
    }

    private void AddToUpdateList(SComponent component) {
        ScriptMethods overrideInfo = ComponentUtil.GetOverrideInfo(typeof(SComponent), component.GetType());
        if (overrideInfo.IsIntersect(ScriptMethods.FixedUpdate)) _fixedUpdateList.Add(component);
        if (overrideInfo.IsIntersect(ScriptMethods.EarlyUpdate)) _earlyUpdateList.Add(component);
        if (overrideInfo.IsIntersect(ScriptMethods.Update)) _updateList.Add(component);
        if (overrideInfo.IsIntersect(ScriptMethods.LateUpdate)) _lateUpdateList.Add(component);
    }

    private void ClearUpdateList() {
        _fixedUpdateList.Clear();
        _earlyUpdateList.Clear();
        _updateList.Clear();
        _lateUpdateList.Clear();
    }

    #endregion
}
}