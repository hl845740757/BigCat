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
using UnityEngine;
using Wjybxx.BigCat.Assetor;
using Wjybxx.BigCat.Co;
using Wjybxx.BigCat.Fx;
using Wjybxx.BigCat.Gameplay;
using Wjybxx.BigCat.MVC;
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Fx;
using Wjybxx.Commons.Inject;
using Wjybxx.Commons.Inject.Attributes;
using Wjybxx.Commons.Logger;
using ILogger = Wjybxx.Commons.Logger.ILogger;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Wjybxx.BigCat.UI
{
/// <summary>
/// 窗口管理器
///
/// 注：
/// 1.该管理器不是MonoBehavior，还需要一个组件来调度所有UI相关的管理器。
/// 2.该管理器不可直接实现<see cref="WindowCmdMgr"/>，而是为<see cref="WindowCmdMgr"/>提供服务。
/// 3.暂不支持同一个地址的窗口打开多个，游戏应当是不需要的；可通过addr曲线救国实现。
/// </summary>
public class WindowMgr
{
    private static readonly ILogger logger = LoggerFactory.GetLogger<WindowMgr>();
    /// <summary>
    /// 场景外窗口管理器的实例
    /// </summary>
    public static WindowMgr Inst { get; set; }

    /// <summary>
    /// 根画布
    /// </summary>
    private readonly Canvas _canvas;
    /// <summary>
    /// 窗口加载器
    /// </summary>
    private WindowLoader _windowLoader = new DefaultWindowLoader();
    /// <summary>
    /// 聚合模型（所有Window的依赖）
    /// </summary>
    private IAggregationModel _aggregationModel;
    /// <summary>
    /// 数据模型解析器
    /// </summary>
    private IDataModelResolver _dataModelResolver;
    /// <summary>
    /// UI可能用到的外部依赖
    /// </summary>
    private IInjector _injector;

    /// <summary>
    /// 所有的窗口
    /// Key: <see cref="UnityEngine.Object.GetInstanceID()"/>
    /// </summary>
    private readonly Dictionary<int, Window> _windowMap = new();
    /// <summary>
    /// 所有的窗口
    /// key: 窗口的路径
    /// </summary>
    private readonly Dictionary<string, Window> _addr2WindowMap = new();
    /// <summary>
    /// 所有的窗口(添加序)
    /// </summary>
    private readonly IndexedDynamicArray<Window> _windowList = new IndexedDynamicArray<Window>(WIndexHelper.GetInst(0), 10);
    /// <summary>
    /// 所有活动中的窗口列表
    /// </summary>
    private readonly IndexedDynamicArray<Window> _activeWindowList = new IndexedDynamicArray<Window>(WIndexHelper.GetInst(1), 10, 0);
    /// <summary>
    /// 已关闭的窗口列表
    /// (用于自动延迟销毁)
    /// </summary>
    private readonly BetterIndexedPriorityQueue<Window> _closedWindowList = new(DestroyTimeComparer.Inst, WIndexHelper.GetInst(2));

    /// <summary>
    /// 加载中的窗口
    /// (支持迭代时删除)
    /// </summary>
    private readonly LinkedDictionary<string, LoadingTask> _loadingTaskMap = new();
    /// <summary>
    /// 窗口加载的超时时间(秒)
    /// </summary>
    private double loadingTimeout = 15;

    /// <summary>
    /// 时间系统
    /// </summary>
    private readonly GTime time = new GTime();
    /// <summary>
    /// 协程管理器
    /// (由于管理器存在自定义设置，因此延迟构造)
    /// </summary>
    private readonly CoroutineMgr coroutineMgr;
    /// <summary>
    /// 所有的桌面
    /// (桌面的简单信息会配置在当前Mgr下)
    /// </summary>
    private readonly Desktop[] _desktops = new Desktop[WindowCfg.MAX_DESKTOP];
    /// <summary>
    /// 当前桌面
    /// </summary>
    private Desktop _curDesktop;

    /// <summary>
    /// 如果不通过容器构造该对象，亦可手动创建
    /// </summary>
    /// <param name="workerHolder">事件循环线程</param>
    /// <param name="cfg">管理器配置</param>
    [Inject]
    public WindowMgr(WorkerHolder workerHolder, WindowMgrCfg cfg) {
        this._injector = workerHolder.Worker.Injector;
        this.coroutineMgr = new CoroutineMgr(workerHolder.Worker, time,
            cfg.minPeriod, cfg.unscaledMinPeriod,
            cfg.enableUnscaledQueue, cfg.enableFrameQueue);
        // 初始化Desktop
        this._canvas = cfg.canvas ?? throw new Exception("Canvas not found");
        for (int desktopId = 1; desktopId <= WindowCfg.MAX_DESKTOP; desktopId++) {
            _desktops[desktopId - 1] = new Desktop(desktopId, _canvas);
        }
        _curDesktop = _desktops[0];
        _curDesktop.Show();
    }

    /// <summary>
    /// UI循环的时间轴
    /// </summary>
    public GTime Time => time;

    /// <summary>
    /// UI系统的协程
    /// (不支持帧定时器)
    /// </summary>
    public CoroutineMgr CoroutineMgr => coroutineMgr;

    /// <summary>
    /// UI系统绑定的线程
    /// </summary>
    public Worker Worker => (Worker)coroutineMgr.EventLoop;

    /// <summary>
    /// 所有UI的依赖
    /// </summary>
    public IInjector Injector {
        get => _injector;
        set => _injector = value;
    }

    /// <summary>
    /// 窗口加载的超时时间
    /// </summary>
    public double LoadingTimeout {
        get => loadingTimeout;
        set => loadingTimeout = Math.Max(0, value);
    }

    #region 容器管理

    public Dictionary<int, Window> WindowMap => _windowMap;
    public Dictionary<string, Window> Addr2WindowMap => _addr2WindowMap;
    public IndexedDynamicArray<Window> WindowList => _windowList;

    /// <summary>
    /// 获取桌面
    /// </summary>
    /// <param name="desktopId">桌面id，1开始</param>
    /// <returns></returns>
    public Desktop GetDesktop(int desktopId) {
        return _desktops[desktopId - 1];
    }

    /// <summary>
    /// 根据窗口Id查找窗口
    /// </summary>
    /// <param name="instId">窗口实例id</param>
    /// <returns></returns>
    public Window GetWindow(int instId) {
        return _windowMap.TryGetValue(instId, out var window) ? window : null;
    }

    /// <summary>
    /// 根据窗口路径查找窗口
    /// </summary>
    /// <param name="windowAddr">窗口路径</param>
    /// <returns></returns>
    public Window GetWindow(string windowAddr) {
        return _addr2WindowMap.TryGetValue(windowAddr, out var window) ? window : null;
    }

    /// <summary>
    /// 添加窗口
    /// </summary>
    /// <param name="window">要添加的窗口</param>
    public void Add(Window window) {
        if (string.IsNullOrEmpty(window.windowAddr)) {
            throw new Exception("window.WindowAddr is empty");
        }
        _windowMap.Add(window.InstId, window); // 检测重复
        _addr2WindowMap.Add(window.windowAddr, window);
        _windowList.Add(window);
        _activeWindowList.Add(window);
        // 如果指定了桌面，则添加到指定桌面，否则添加到当前桌面
        int desktopId = window.windowCfg.desktopId;
        if (desktopId > 0) {
            Desktop desktop = GetDesktop(desktopId);
            desktop.Add(window);
        } else {
            _curDesktop.Add(window);
        }
        try {
            if (window.Status == ComponentStatus.New) {
                window.SetInitialized();
            }
            window.Start();
        }
        catch (Exception ex) {
            logger.Warn(ex, "Window.Start caught exception");
        }
    }

    /// <summary>
    /// 关闭Window
    ///
    /// 注：窗口关闭后，在销毁之前可以重新打开。
    /// </summary>
    public void Close(Window window) {
        if (window.Status == ComponentStatus.Destroyed) return;
        try {
            window.Stop();
        }
        catch (Exception ex) {
            logger.Warn(ex);
        }
        if (!_closedWindowList.Contains(window)) {
            _activeWindowList.Remove(window);
            _closedWindowList.Add(window);
        }
    }

    /// <summary>
    /// 销毁Window
    /// </summary>
    /// <param name="window"></param>
    private void Destroy(Window window) {
        if (window.Status == ComponentStatus.Destroyed) return;
        try {
            window.Stop();
        }
        catch (Exception ex) {
            logger.Warn(ex, "Window.Stop caught exception, name: " + window.name);
        }

        _windowMap.Remove(window.InstId);
        _addr2WindowMap.Remove(window.windowAddr);
        _windowList.Remove(window);
        _activeWindowList.Remove(window);
        _closedWindowList.Remove(window);
        window.desktop.Remove(window);

        try {
            window.Destroy();
        }
        catch (Exception ex) {
            logger.Warn(ex, "Window.Destroy caught exception, name: " + window.name);
        }
    }

    #endregion

    #region WindowCmd

    /// <summary>
    /// 打开窗口
    /// 
    /// 注：适用用户自己加载Window的场景。
    /// </summary>
    public Window Open(string windowAddr, GameObject gameObject, WindowOpenArgs openArgs) {
        WindowCfg windowCfg = gameObject.GetComponent<WindowCfg>();
#if UNITY_EDITOR
        if (!windowCfg) {
            throw new Exception("Invalid gameObject");
        }
        if (PrefabUtility.IsPartOfPrefabAsset(gameObject)) {
            throw new Exception("Instantiate is required");
        }
#endif
        openArgs ??= new WindowOpenArgs();
        Window window = new Window(windowCfg, windowAddr, this);
        windowCfg.SetWindow(window); // 双向绑定
        window.OpenArgs = openArgs;
        window.ParentInstId = openArgs.pInstId;
        window.prefabHandle = openArgs.assetHandle;
        Add(window);
        return window;
    }

    /// <summary>
    /// 打开窗口
    /// </summary>
    /// <param name="windowAddr">窗口路径</param>
    /// <param name="openArgs">打开参数</param>
    public void Open(string windowAddr, WindowOpenArgs openArgs) {
        openArgs ??= new WindowOpenArgs();
        if (_addr2WindowMap.TryGetValue(windowAddr, out Window window)) {
            if (window.Status != ComponentStatus.Terminated && !openArgs.reopen) {
                return; // 拒绝请求
            }
            Reopen(window, openArgs);
            return;
        }
        // 取消既有加载请求 - 新的加载重新计时，旧加载可能存在异常
        if (_loadingTaskMap.Remove(windowAddr, out LoadingTask prevTask)) {
            prevTask.Dispose();
        }
        double timeout = openArgs.timeout > 0 ? openArgs.timeout : LoadingTimeout;
        openArgs.assetHandle = _windowLoader.LoadAsync(windowAddr, timeout);
        LoadingTask loadingTask = new LoadingTask(windowAddr, openArgs, time.UnscaledTime + timeout);
        _loadingTaskMap[windowAddr] = loadingTask;
        // 先注册再监听
        openArgs.assetHandle.Completed += _ => OnLoadCompleted(loadingTask);
    }

    private void OnLoadCompleted(LoadingTask loadingTask) {
        if (!_loadingTaskMap.TryGetValue(loadingTask.windowAddr, out LoadingTask exist)
            || !ReferenceEquals(exist, loadingTask)) {
            // 任务被取消
            loadingTask.Dispose();
            return;
        }
        _loadingTaskMap.Remove(loadingTask.windowAddr);
        AssetHandle assetHandle = loadingTask.openArgs.assetHandle;
        GameObject prefab = assetHandle.GetAsset<GameObject>();
        if (!prefab) {
            loadingTask.Dispose();
            logger.Warn("Load window failed, windowAddr: " + loadingTask.windowAddr);
            return;
        }
        try {
            GameObject go = _windowLoader.Instantiate(loadingTask.windowAddr, prefab, _canvas.transform);
            Open(loadingTask.windowAddr, go, loadingTask.openArgs);
        }
        catch (Exception ex) {
            logger.Warn(ex, "Open window caught exception, windowAddr: " + loadingTask.windowAddr);
        }
    }

    private void Reopen(Window window, WindowOpenArgs openArgs) {
        // Reset触发Stop可能添加到关闭列表
        window.Reset();
        _closedWindowList.Remove(window);
        _activeWindowList.Add(window);
        // 切换到当前桌面
        if (window.windowCfg.desktopId == 0
            && window.desktop.DesktopId != _curDesktop.DesktopId) {
            window.desktop.Remove(window);
            _curDesktop.Add(window);
        }
        // 重新启动
        window.OpenArgs = openArgs;
        window.ParentInstId = openArgs.pInstId;
        try {
            window.Start();
        }
        catch (Exception ex) {
            logger.Warn(ex, "Window.Restart caught exception, name:" + window.name);
        }
    }

    /// <summary>
    /// 关闭窗口
    /// </summary>
    /// <param name="windowAddr">窗口路径</param>
    /// <param name="force">是否强制关闭常驻窗口</param>
    public void Close(string windowAddr, bool force = false) {
        if (_loadingTaskMap.Remove(windowAddr, out LoadingTask loadingTask)) { // 取消加载
            loadingTask.Dispose();
            return;
        }
        if (!_addr2WindowMap.TryGetValue(windowAddr, out Window window)) {
            return;
        }
        if (window.windowCfg.unclosable && !force) { // 常驻UI需要显式强制关闭
            return;
        }
        Close(window);
    }

    /// <summary>
    /// 关闭多个窗口
    /// </summary>
    /// <param name="windowAddrList">窗口路径</param>
    /// <param name="force">是否强制关闭常驻窗口</param>
    public void Close(List<string> windowAddrList, bool force = false) {
        foreach (string windowAddr in windowAddrList) {
            Close(windowAddr, force);
        }
    }

    /// <summary>
    /// 关闭当前桌面的具有任一指定Tag的窗口
    /// </summary>
    /// <param name="tags">需要关闭的窗口类型</param>
    /// <param name="force">是否强制关闭常驻窗口</param>
    public void CloseTagged(List<string> tags, bool force = false) {
        foreach (Window window in new List<Window>(_curDesktop.Stack)) {
            if (UIInternal.IsIntersect(window.windowCfg.tags, tags)) {
                Close(window.windowAddr, force);
            }
        }
    }

    /// <summary>
    /// 关闭当前桌面所有普通窗口(非常驻窗口)
    /// </summary>
    /// <param name="force">是否强制关闭常驻窗口</param>
    public void CloseAll(bool force = false) {
        foreach (Window window in new List<Window>(_curDesktop.Stack)) {
            Close(window.windowAddr, force);
        }
    }

    /// <summary>
    /// 切换桌面
    /// </summary>
    /// <param name="desktopId"></param>
    public void SwitchDesktop(int desktopId) {
        if (desktopId == _curDesktop.DesktopId) {
            return;
        }
        Desktop nextDesktop = GetDesktop(desktopId);
        List<Window> tempList = new List<Window>();
        for (int index = 0; index < _curDesktop.Stack.Count; index++) {
            Window window = _curDesktop.Stack[index];
            if (!window.windowCfg.isCrossDesktop) {
                continue;
            }
            _curDesktop.RemoveAt(index, false);
            index--;
            nextDesktop.Add(window, false);
            tempList.Add(window);
        }
        _curDesktop.Hide();
        _curDesktop = nextDesktop;
        _curDesktop.Show();
        // 跨桌面的窗口可能需要切换展示的数据
        foreach (Window window in tempList) {
            window.OnDesktopChanged();
        }
    }

    /// <summary>
    /// 将指定窗口移动到目标桌面
    /// </summary>
    /// <param name="windowAddr">窗口路径</param>
    /// <param name="desktopId">桌面id</param>
    public void MoveToDesktop(string windowAddr, int desktopId) {
        if (!_addr2WindowMap.TryGetValue(windowAddr, out Window window)) {
            return;
        }
        if (window.desktop.DesktopId == desktopId) {
            return;
        }
        Desktop desktop = GetDesktop(desktopId);
        window.desktop.Remove(window);
        desktop.Add(window);
        //
        window.Repaint();
        window.OnDesktopChanged();
    }

    /// <summary>
    /// 是否包含正在加载的窗口
    /// </summary>
    /// <param name="windowAddr"></param>
    /// <returns></returns>
    public bool HasLoadingTask(string windowAddr) {
        return _loadingTaskMap.ContainsKey(windowAddr);
    }

    private void CheckLoadTimeout() {
        var enumerator = _loadingTaskMap.GetEnumerator();
        while (enumerator.MoveNext()) {
            LoadingTask loadingTask = enumerator.Current.Value;
            if (loadingTask.expireTime <= time.UnscaledTime) {
                enumerator.Remove();
                loadingTask.Dispose();
            }
        }
        enumerator.Dispose();
    }

    /// <summary>
    /// 强制销毁已经关闭的窗口(主动释放资源)
    /// </summary>
    public void DestroyClosedWindows() {
        while (_closedWindowList.TryDequeue(out Window window)) {
            Destroy(window);
        }
    }

    #endregion

    #region update管理

    /// <summary>
    /// 帧循环开始
    ///
    /// 注：该方法主要用于调度协程。
    /// </summary>
    /// <param name="unscaledDeltaTime"></param>
    public void BeginOfFrame(double unscaledDeltaTime) {
        time.Update(unscaledDeltaTime);
        coroutineMgr.Update(GameLoopPhase.BeginOfFrame);
        CheckLoadTimeout();
    }

    /// <summary>
    /// 执行窗口的EarlyUpdate方法
    /// 注：该方法其实主要是为协程服务的
    /// </summary>
    public void EarlyUpdate() {
        coroutineMgr.Update(GameLoopPhase.EarlyUpdate);

        double unscaledDeltaTime = time.UnscaledDeltaTime;
        IndexedDynamicArray<Window> windowList = _activeWindowList;
        windowList.BeginItr();
        for (int index = 0, len = windowList.Length; index < len; index++) {
            Window window = windowList[index];
            if (window == null || window.Status != ComponentStatus.Running) {
                continue;
            }
            try {
                window.EarlyUpdate(unscaledDeltaTime);
            }
            catch (Exception e) {
                logger.Warn(e, "Window.EarlyUpdate caught exception, name: " + window.name);
            }
        }
        windowList.EndItr();

        coroutineMgr.Update(GameLoopPhase.PostEarlyUpdate);
    }

    /// <summary>
    /// 执行窗口的Update方法
    /// </summary>
    public void Update() {
        coroutineMgr.Update(GameLoopPhase.Update);

        IndexedDynamicArray<Window> windowList = _activeWindowList;
        windowList.BeginItr();
        for (int index = 0, len = windowList.Length; index < len; index++) {
            Window window = windowList[index];
            if (window == null || window.Status != ComponentStatus.Running) {
                continue;
            }
            try {
                window.Update();
            }
            catch (Exception e) {
                logger.Warn(e, "Window.Update caught exception, name: " + window.name);
            }
        }
        windowList.EndItr();

        coroutineMgr.Update(GameLoopPhase.PostUpdate);
    }

    /// <summary>
    /// 执行窗口的LateUpdate方法
    /// </summary>
    public void LateUpdate() {
        coroutineMgr.Update(GameLoopPhase.LateUpdate);

        IndexedDynamicArray<Window> windowList = _activeWindowList;
        windowList.BeginItr();
        for (int index = 0, len = windowList.Length; index < len; index++) {
            Window window = windowList[index];
            if (window == null || window.Status != ComponentStatus.Running) {
                continue;
            }
            try {
                window.LateUpdate();
            }
            catch (Exception e) {
                logger.Warn(e, "Window.LateUpdate caught exception, name: " + window.name);
            }
        }
        windowList.EndItr();

        coroutineMgr.Update(GameLoopPhase.PostLateUpdate);
    }

    /// <summary>
    /// 帧循环结束
    /// 
    ///  注：该方法主要用于调度协程。
    /// </summary>
    public void EndOfFrame() {
        // 处理延迟销毁
        BetterIndexedPriorityQueue<Window> closedWindowList = _closedWindowList;
        while (closedWindowList.TryPeekHead(out Window window)) {
            if (window.destroyTime < time.UnscaledTime) {
                break;
            }
            closedWindowList.Dequeue();
            Destroy(window);
        }
        coroutineMgr.Update(GameLoopPhase.EndOfFrame);
    }

    #endregion

    /// <summary>
    /// 窗口被暂停时调用
    /// </summary>
    /// <param name="window"></param>
    internal void OnPause(Window window) {
        _activeWindowList.Remove(window);
    }

    /// <summary>
    /// 窗口恢复运行时调用
    /// </summary>
    /// <param name="window"></param>
    internal void OnResume(Window window) {
        _activeWindowList.Add(window);
    }

    /// <summary>
    /// 窗口关闭时调用
    /// </summary>
    internal void OnTerminated(Window window) {
        float maxIdleTime = window.windowCfg.maxIdleTime;
        window.destroyTime = maxIdleTime > 0
            ? time.UnscaledTime + maxIdleTime
            : time.UnscaledTime + 365 * DatetimeUtil.SecondsPerDay;
        _activeWindowList.Remove(window);
        _closedWindowList.Add(window);

        // 关闭子窗口，List不长，直接迭代全部
        IndexedDynamicArray<Window> windowList = _windowList;
        windowList.BeginItr();
        for (int i = 0, len = windowList.Length; i < len; i++) {
            Window tempWindow = windowList[i];
            if (tempWindow == null
                || tempWindow.ParentInstId != window.InstId
                || tempWindow.Nohup) {
                continue;
            }
            Close(tempWindow);
        }
        windowList.EndItr();
    }

    /// <summary>
    /// 结束数据模型
    /// </summary>
    public object ResolveDataModel(object parentDataModel, string dataAddress, int uiIndex = -1) {
        if (string.IsNullOrWhiteSpace(dataAddress)) {
            return parentDataModel;
        }
        if (_dataModelResolver == null) {
            throw new InvalidOperationException("dataModelResolver is null");
        }
        return _dataModelResolver.Resolve(_aggregationModel, parentDataModel, dataAddress, uiIndex);
    }

    #region PROPS

    public Canvas Canvas => _canvas;
    public IAggregationModel AggregationModel {
        get => _aggregationModel;
        set => _aggregationModel = value;
    }
    public WindowLoader WindowLoader {
        get => _windowLoader;
        set => _windowLoader = value;
    }
    public IDataModelResolver DataModelResolver {
        get => _dataModelResolver;
        set => _dataModelResolver = value;
    }
    public Desktop CurrentDesktop => _curDesktop;

    #endregion

    private class DestroyTimeComparer : IComparer<Window>
    {
        public static DestroyTimeComparer Inst { get; } = new DestroyTimeComparer();

        public int Compare(Window x, Window y) {
            // ReSharper disable PossibleNullReferenceException
            return x.destroyTime.CompareTo(y.destroyTime);
        }
    }

    /// <summary>
    /// 注：不保留Future的引用，因为我们不能操作Future
    /// </summary>
    private sealed class LoadingTask : IDisposable
    {
        public readonly string windowAddr;
        public readonly WindowOpenArgs openArgs;
        public readonly double expireTime;

        public LoadingTask(string windowAddr, WindowOpenArgs openArgs,
                           double expireTime) {
            this.windowAddr = windowAddr;
            this.openArgs = openArgs;
            this.expireTime = expireTime;
        }

        public void Dispose() {
            openArgs.assetHandle.Release();
        }
    }
}
}