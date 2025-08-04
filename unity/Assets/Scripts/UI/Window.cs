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
using UnityEngine;
using Wjybxx.BigCat.Gameplay;
using Wjybxx.BigCat.Util;
using Wjybxx.BigCat.MVC;
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Fx;
using Wjybxx.Commons.Logger;
using ILogger = Wjybxx.Commons.Logger.ILogger;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Wjybxx.BigCat.UI
{
/// <summary>
/// Window是UI的入口（是Root节点的持有者）
///
/// 1.Window只提供一些基础的控制交互逻辑，其它的逻辑由Node承担 —— 即Window不负责绘制用户数据。
/// 2.Window理论上可以不继承UINode，但继承Node可以简化编程模型。
/// 3.Window由框架创建，而非用户创建；通常创建的Window为同一类型。
/// 4.资源管理的单位是Window，当Window关闭时，会释放所有动态加载的资源。
/// 5.不建议在Window上设计复杂的程序化动画，建议使用<see cref="Animator"/>制作固定的动画。
/// 6.为避免和<see cref="MonoBehaviour"/>的方法签名冲突，我们的方法都使用<code>Win</code>开头。
/// 7.全屏遮罩建议也实现为窗口，但位于最高层级；窗口内的遮罩建议使用<see cref="UINode"/>实现，有更强的灵活性。
/// 
/// TODO
/// 1.Close、Pause公共按钮支持 -- 可以提一个公共组件
/// 2.UI音效、特效管理
/// 
/// </summary>
public sealed class Window
{
    private static readonly ILogger logger = LoggerFactory.GetLogger<Window>();

    public readonly WindowCfg windowCfg;
    public readonly string windowUri;
    public readonly WindowMgr windowMgr;
    private readonly int _instId;
    private int _parentInstId; // 非0表示有父窗口，重用时会变更

    private ComponentStatus _status = ComponentStatus.New;
    /// <summary>
    /// 控制标记，避免过多的bool字段
    /// </summary>
    [NonSerialized] private int _ctl;
    /// <summary>
    /// 重入Id，只增不减
    ///
    /// 1.enter和exit都增加；
    /// 2.事件处理脚本可以捕获该id，以判断UI是否进入到了新的生命周期；
    /// </summary>
    [NonSerialized] private int _reentryId;
    /// <summary>
    /// 场景应当销毁的时间
    /// </summary>
    internal double destroyTime;

    /// <summary>
    /// 窗口的代理
    /// 
    /// 1.通常是<see cref="MonoBehaviour"/>类型。
    /// 2.运行时不为null，如果未绑定Agent，则会创建默认的Agent实现。
    /// </summary>
    private WindowAgent _agent;
    /// <summary>
    /// 绑定的数据
    /// 
    /// 注：特殊UI可能不需要数据。
    /// </summary>
    private object _dataModel;

    /// <summary>
    /// 窗口自身的画布
    /// </summary>
    private Canvas _canvas;
    /// <summary>
    /// 窗口所在的桌面
    /// </summary>
    internal Desktop desktop;
    /// <summary>
    /// 窗口的打开顺序
    ///
    /// 注：用于同层级内排序，后打开的排上面。
    /// </summary>
    internal int openOrder;
#if UNITY_EDITOR
    /// <summary>
    /// Editor窗口的引用，用于Editor开发
    /// </summary>
    public EditorWindow editorWindow;
#endif
    /// <summary>
    /// 窗口打开参数
    /// </summary>
    [NonSerialized] private WindowOpenArgs _openArgs;
    /// <summary>
    /// 窗口的展示模式
    /// 
    /// 注：模式切换需要<see cref="WindowAgent"/>支持。
    /// </summary>
    [NonSerialized] private WindowDisplayCfg _curDisplayCfg;
    /// <summary>
    /// 当前使用的根节点
    /// </summary>
    [NonSerialized] private readonly List<UINode> _rootNodes = new List<UINode>();
    /// <summary>
    /// 需要Update的Node
    /// </summary>
    [NonSerialized] private readonly IndexedDynamicArray<UINode> _updateNodes = new(NodeIndexHelper.Inst, 10, 0);

    /// <summary>
    /// 绑定的组件
    /// </summary>
    private readonly List<WComponent> _components = new List<WComponent>();
    /// <summary>
    /// 索引后的组件 - 提高查询速度
    /// </summary>
    [NonSerialized] private readonly ComponentList<WComponent?> _indexedComponents = new(WComponentListHelper.Inst);
    [NonSerialized] private readonly IndexedDynamicArray<WComponent> _earlyUpdateList = new(WComponentIndexHelper.GetInst(1), 4);
    [NonSerialized] private readonly IndexedDynamicArray<WComponent> _updateList = new(WComponentIndexHelper.GetInst(2), 4);
    [NonSerialized] private readonly IndexedDynamicArray<WComponent> _lateUpdateList = new(WComponentIndexHelper.GetInst(3), 4);

    /// <summary>
    /// 窗口在各个队列的索引
    /// </summary>
    [NonSerialized] internal WIndexes indexes = WIndexes.Create();
    /// <summary>
    /// 窗口的运行时间
    /// </summary>
    [NonSerialized] private readonly GTime _time = new GTime();
    /// <summary>
    /// Window黑板
    /// </summary>
    [NonSerialized] private readonly Blackboard _blackboard = new Blackboard();

    internal Window(WindowCfg windowCfg, string windowUri, WindowMgr windowMgr) {
        this.windowCfg = windowCfg;
        this.windowUri = windowUri;
        this.windowMgr = windowMgr;
        this._instId = windowCfg.GetInstanceID();

        this._agent = windowCfg.GetComponent<WindowAgent>() ?? new UIInternal.SimpleWindowAgent();
        this._canvas = windowCfg.GetComponent<Canvas>();
        // 初始化逻辑组件
        foreach (WComponentCfg componentCfg in windowCfg.GetComponents<WComponentCfg>()) {
            AddComponent(componentCfg.GetComponent());
        }
    }

    #region 生命周期

    /// <summary>
    /// 将Window标记为已完成初始化
    /// </summary>
    internal void SetInitialized() {
        if (_status != ComponentStatus.New) {
            throw new InvalidOperationException();
        }
        _agent.Inject(this);
        _status = ComponentStatus.Initialized;
        // 初始化模块
        foreach (WComponent component in _components) {
            if (component.Cid.shared) {
                continue;
            }
            if (component.Status == ComponentStatus.New) {
                component.SetEntity(this);
            }
        }
        // 解决模块之间的依赖
        foreach (WComponent component in _components) {
            if (component.Cid.shared) {
                continue;
            }
            component.ResolveDependence();
        }
    }

    /// <summary>
    /// 启动窗口
    /// </summary>
    internal void Start() {
        _status = ComponentStatus.Running;
        _reentryId++;
        _time.Restart();
        Nohup = _openArgs.nohup;

        _dataModel = _openArgs.dataModel ?? windowMgr.ResolveDataModel(null, windowCfg.dataAddress);
        ResetDisplayElements(windowCfg.displayCfgs[0]);

        // Component是为Node服务的，因此先启动
        StartComponents();
        Show();
    }

    /// <summary>
    /// 停止窗口逻辑
    /// (即关闭窗口)
    /// </summary>
    internal void Stop() {
        if (_status < ComponentStatus.Running || _status >= ComponentStatus.Shutdown) {
            return;
        }
        _status = ComponentStatus.Shutdown;
        _reentryId++;
        try {
            Hide();
        }
        catch (Exception ex) {
            logger.Warn(ex);
        }
        ClearUpdateList();
        StopComponents();

        _status = ComponentStatus.Terminated;
        if (windowMgr != null) {
            windowMgr.OnTerminated(this);
        }
    }

    private void StartComponents() {
        // Start -- 顺序启动，出现任何异常直接退出
        _agent.OnStart();
        foreach (WComponent component in _components) {
            if (!component.Cid.IsPrivateScript) {
                continue;
            }
            component.InvokeStart();
        }
        // 初始化Update列表 -- 按照updateOrder排序
        List<WComponent> components = new List<WComponent>(_components);
        components.Sort(UIInternal.UpdateOrderComparer);
        foreach (WComponent component in components) {
            if (!component.Cid.IsPrivateScript) {
                continue;
            }
            AddToUpdateList(component);
        }
    }

    private void StopComponents() {
        // Stop - 逆序
        for (int index = _components.Count - 1; index >= 0; index--) {
            WComponent component = _components[index];
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
            _agent.OnStop();
        }
        catch (Exception ex) {
            logger.Warn(ex);
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
        foreach (WComponent component in _components) {
            if (component.Cid.shared) continue;
            if (component.Status <= ComponentStatus.Initialized) continue;
            component.Reset();
        }
        ClearUpdateList();

        if (_status > ComponentStatus.Initialized) {
            _status = ComponentStatus.Initialized;
        }
        _ctl = 0;
        _agent.Reset();
        // 这些数据启动时重新赋值
        _dataModel = null;
        _curDisplayCfg = null;
        _rootNodes.Clear();
        _updateNodes.Clear();

        _time.Restart();
        _blackboard.Clear();
    }

    /// <summary>
    /// 销毁窗口
    /// </summary>
    internal void Destroy() {
        if (_status == ComponentStatus.Destroyed) return;
        Stop();
        _status = ComponentStatus.Destroyed;
        desktop = null;
        _agent = null;
        _dataModel = null;

        _canvas = null;
        _rootNodes.Clear();
        UnityEngine.Object.Destroy(gameObject);
        if (editorWindow) {
            UnityEngine.Object.Destroy(editorWindow);
        }
        //
        foreach (WComponent component in _components) {
            if (component.Cid.shared) continue;
            if (component.Status == ComponentStatus.New) continue;
            try {
                component.InvokeDestroy();
            }
            catch (Exception ex) {
                logger.Warn(ex, "component destroy caught exception");
            }
        }
        indexes.Clear();
        _blackboard.Clear();
    }

    /// <summary>
    /// 暂停窗口更新
    /// </summary>
    /// <param name="extraInfo">用户自定义数据</param>
    public void Pause(object extraInfo = null) {
        if (_status == ComponentStatus.Running) {
            _status = ComponentStatus.Suspended;
            windowMgr.OnPause(this);
            _agent.OnPause(extraInfo);
            // 从Mgr调度队列中删除？影响不大
        }
    }

    /// <summary>
    /// 恢复窗口更新
    /// </summary>
    /// <param name="extraInfo">用户自定义数据</param>
    public void Resume(object extraInfo = null) {
        if (_status == ComponentStatus.Suspended) {
            _status = ComponentStatus.Running;
            windowMgr.OnResume(this);
            _agent.OnResume(extraInfo);
            Repaint();
        }
    }

    #endregion

    #region update

    /// <summary>
    /// 该方法主要是为协程服务的
    /// </summary>
    /// <param name="unscaledDeltaTime"></param>
    internal void EarlyUpdate(double unscaledDeltaTime) {
        _time.Update(unscaledDeltaTime);
        IndexedDynamicArray<WComponent> list = _earlyUpdateList;
        if (list.Length == 0) {
            return; // 逻辑层也少有EarlyUpdate
        }
        list.BeginItr();
        for (int index = 0, len = list.Length; index < len; index++) {
            WComponent component = list[index];
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

    /// <summary>
    /// 执行窗口Update
    ///
    /// 注：由Manager测试是否处于暂停状态。
    /// </summary>
    internal void Update() {
        if ((_ctl & UIInternal.MASK_DIRTY_REPAINT) != 0) {
            Repaint();
        }
        UpdateComponents();
        UpdateNodes();
    }

    private void UpdateComponents() {
        IndexedDynamicArray<WComponent> list = _updateList;
        list.BeginItr();
        for (int index = 0, len = list.Length; index < len; index++) {
            WComponent component = list[index];
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

    private void UpdateNodes() {
        IndexedDynamicArray<UINode> updateNodes = _updateNodes;
        if (updateNodes.Length == 0) {
            return;
        }
        // 更新Node
        updateNodes.BeginItr();
        for (int i = 0, len = updateNodes.Length; i < len; i++) {
            UINode node = updateNodes[i];
            if (node) {
                node.OnUpdate();
            }
        }
        updateNodes.EndItr();
    }

    internal void LateUpdate() {
        IndexedDynamicArray<WComponent> list = _lateUpdateList;
        if (list.Length == 0) {
            return; // 逻辑层也少有LateUpdate
        }
        list.BeginItr();
        for (int index = 0, len = list.Length; index < len; index++) {
            WComponent component = list[index];
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

    private void AddToUpdateList(WComponent component) {
        ScriptMethods overrideInfo = UIInternal.GetOverrideInfo(typeof(WComponent), component.GetType());
        if (overrideInfo.IsIntersect(ScriptMethods.EarlyUpdate)) _earlyUpdateList.Add(component);
        if (overrideInfo.IsIntersect(ScriptMethods.Update)) _updateList.Add(component);
        if (overrideInfo.IsIntersect(ScriptMethods.LateUpdate)) _lateUpdateList.Add(component);
    }

    private void ClearUpdateList() {
        _earlyUpdateList.Clear();
        _updateList.Clear();
        _lateUpdateList.Clear();
    }

    #endregion

    #region 窗口管理

    private void ResetDisplayElements(WindowDisplayCfg displayCfg) {
        _curDisplayCfg = displayCfg;
        _rootNodes.Clear();
        _rootNodes.AddRange(displayCfg.nodes);
    }

    /// <summary>
    /// 显示root节点
    /// </summary>
    public void Show() {
        gameObject.SetActive(true);
        foreach (UINode rootNode in _rootNodes) {
            if (!rootNode.IsShowing && rootNode.enabled) {
                object dataModel = windowMgr.ResolveDataModel(_dataModel, rootNode.nodeCfg.dataAddress);
                rootNode.Show(this, null, dataModel);
            }
        }
    }

    /// <summary>
    /// 重新绘制UI
    /// </summary>
    public void Repaint() {
        if (!gameObject.activeInHierarchy) {
            return;
        }
        _ctl &= ~UIInternal.MASK_DIRTY_REPAINT;
        foreach (UINode rootNode in _rootNodes) {
            if (rootNode.IsShowing) {
                rootNode.Repaint();
            }
        }
    }

    /// <summary>
    /// 标记为需要下一帧重新绘制
    /// </summary>
    public void MarkDirtyRepaint() {
        _ctl |= UIInternal.MASK_DIRTY_REPAINT;
    }

    /// <summary>
    /// 隐藏显示内容
    /// (正常情况下不应该直接隐藏窗口)
    /// </summary>
    private void Hide() {
        foreach (UINode rootNode in _rootNodes) {
            if (rootNode.IsShowing) {
                rootNode.Hide();
            }
        }
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 切换窗口的展示模式
    /// (该方法由Agent或Window里的Controller)
    /// </summary>
    /// <param name="mode"></param>
    public void ChangeDisplayMode(WindowDisplayMode mode) {
        WindowDisplayCfg displayCfg = windowCfg.FindDisplayCfg(mode);
        if (displayCfg == null) {
            return;
        }
        if (_curDisplayCfg != null && _curDisplayCfg.mode == mode) {
            return;
        }
        // 关闭
        foreach (UINode rootNode in _rootNodes) {
            rootNode.Hide();
        }
        WindowDisplayCfg prevDisplayCfg = _curDisplayCfg;
        ResetDisplayElements(displayCfg);
        // 刷新
        if (_status == ComponentStatus.Running) {
            _agent.OnDisplayModeChanged(prevDisplayCfg);
        }
    }

    /// <summary>
    /// 窗口拒绝
    /// </summary>
    /// <param name="hasFocus"></param>
    public void OnFocus(bool hasFocus) {
        // TODO 是不是应该设计一个FocusOn的背景图?然后Focus时控制显隐就行？
        _ctl = BitFlags.Set(_ctl, UIInternal.MASK_FOCUS_ON, hasFocus);
        _agent.OnFocus(hasFocus);
    }

    /// <summary>
    /// 窗口绑定的桌面切换
    /// </summary>
    internal void OnDesktopChanged() {
        _agent.OnDesktopChanged();
    }

    /// <summary>
    /// 修正画布的层级
    /// </summary>
    internal void SetCanvasLayer(int sortingLayerID, int sortingOrder) {
        _canvas.overrideSorting = true;
        _canvas.sortingLayerID = sortingLayerID;
        _canvas.sortingOrder = sortingOrder;
    }

    /// <summary>
    /// 添加需要Update的Node
    /// </summary>
    /// <param name="node"></param>
    public void AddUpdateNode(UINode node) {
        if (node.qIndex >= 0) {
            throw new InvalidOperationException("node already exist");
        }
        _updateNodes.Add(node);
    }

    /// <summary>
    /// 删除需要Update的Node
    /// </summary>
    /// <param name="node"></param>
    public void RemoveUpdateNode(UINode node) {
        _updateNodes.Remove(node);
    }

    #endregion

#nullable disable

    #region 组件模式

    /// <summary>
    /// 
    /// </summary>
    /// <param name="comp">套添加的组件</param>
    /// <param name="addFirst">是否添加到首部，通常用于插入基础组件</param>
    public void AddComponent(WComponent comp, bool addFirst = false) {
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
    public bool ContainsComponent(WComponent comp) {
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
    public List<WComponent> Components => _components;

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
    public WComponent? GetComponent(ComponentId cid) {
        return _indexedComponents.Get(cid);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public WComponent? GetLastComponent(ComponentId cid) {
        return _indexedComponents.GetLast(cid);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public List<WComponent> GetComponents(ComponentId cid, List<WComponent>? outList = null) {
        outList ??= new List<WComponent>();
        _indexedComponents.Get(cid, outList);
        return outList;
    }

    #endregion

#nullable restore

    #region props

    /// <summary>
    /// 窗口启动参数
    /// </summary>
    public WindowOpenArgs OpenArgs {
        get => _openArgs;
        set => _openArgs = value;
    }

    /// <summary>
    /// 父窗口id - 0表示无父窗口
    /// </summary>
    public int ParentInstId {
        get => _parentInstId;
        internal set => _parentInstId = value;
    }

    /// <summary>
    /// Window的根节点 -- 不可修改
    /// </summary>
    public List<UINode> RootNodes => _rootNodes;

    /// <summary>
    /// 是否忽略父窗口关闭信号
    /// </summary>
    public bool Nohup {
        get => (_ctl & UIInternal.MASK_NO_HANGUP) != 0;
        set => _ctl = BitFlags.Set(_ctl, UIInternal.MASK_NO_HANGUP, value);
    }

    /// <summary>
    /// 是否被焦点选中
    /// </summary>
    public bool HasFocus => (_ctl & UIInternal.MASK_FOCUS_ON) != 0;

    /// <summary>
    /// 是否是子窗口
    /// </summary>
    public bool IsSubWindow => _parentInstId != 0;

    /// <summary>
    /// 窗口绑定的GameObject
    ///
    /// 注：窗口应当独占该GameObject。
    /// </summary>
    public GameObject gameObject => windowCfg.gameObject;
    public RectTransform transform => (RectTransform)windowCfg.transform;
    public string name => windowCfg.gameObject.name;

    // ----------------------------
    public int InstId => _instId;
    public Desktop Desktop => desktop;
    public ComponentStatus Status => _status;
    public int ReentryId => _reentryId;
    public WindowAgent Agent => _agent;
    public object DataModel => _dataModel;
    public GTime Time => _time;
    public Blackboard Blackboard => _blackboard;
    public WindowDisplayCfg CurrentDisplayCfg => _curDisplayCfg;
    public WindowDisplayMode CurrentDisplayMode => _curDisplayCfg.mode;

    #endregion
}
}