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
using Wjybxx.BigCat.Gameplay;
using Wjybxx.BigCat.MVC;
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Concurrent;
using Wjybxx.Commons.Fx;
using Wjybxx.Commons.Logger;
using ILogger = Wjybxx.Commons.Logger.ILogger;

namespace Wjybxx.BigCat.UI
{
/// <summary>
/// 窗口管理器
///
/// 注：
/// 1.该管理器不是MonoBehavior，还需要一个组件来调度所有UI相关的管理器。
/// 2.该管理器不可直接实现<see cref="WindowCmdMgr"/>，而是为<see cref="WindowCmdMgr"/>提供服务。
/// 3.暂不支持同一个地址的窗口打开多个，游戏应当是不需要的。
/// </summary>
public sealed class WindowMgr
{
    private static readonly ILogger logger = LoggerFactory.GetLogger<WindowMgr>();
    /// <summary>
    /// 场景外窗口管理器的实例
    /// </summary>
    public static WindowMgr Inst { get; set; }

    /// <summary>
    /// 根画布
    /// </summary>
    private readonly Canvas canvas;
    /// <summary>
    /// 窗口加载器
    /// </summary>
    private readonly WindowLoader _windowLoader;
    /// <summary>
    /// 聚合模型（所有Window的依赖）
    /// </summary>
    private readonly IAggerateModel _aggerateModel;
    /// <summary>
    /// 数据模型解析器
    /// </summary>
    private readonly IDataModelResolver _dataModelResolver;

    /// <summary>
    /// 所有的窗口
    /// Key: <see cref="UnityEngine.Object.GetInstanceID()"/>
    /// </summary>
    private readonly Dictionary<int, Window> _windowMap = new();
    /// <summary>
    /// 所有的窗口
    /// key: 窗口的路径
    /// </summary>
    private readonly Dictionary<string, Window> _uri2WindowMap = new();
    /// <summary>
    /// 所有的窗口(添加序)
    /// </summary>
    private readonly IndexedDynamicArray<Window> _windowList = new IndexedDynamicArray<Window>(WIndexHelper.GetInst(0), 10);
    /// <summary>
    /// 所有活动中的窗口列表
    /// (包括处于暂停状态的窗口)
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
    /// 所有的桌面
    /// (桌面的简单信息会配置在当前Mgr下)
    /// </summary>
    private readonly Desktop[] _desktops = new Desktop[WindowCfg.MAX_DESKTOP];
    /// <summary>
    /// 当前桌面
    /// </summary>
    private Desktop _curDesktop;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="canvas">根画布</param>
    /// <param name="windowLoader">窗口加载器</param>
    /// <param name="aggerateModel">聚合数据模型</param>
    /// <param name="dataModelResolver">数据模型解析器</param>
    public WindowMgr(Canvas canvas, WindowLoader windowLoader, IAggerateModel aggerateModel, IDataModelResolver dataModelResolver = null) {
        this.canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
        this._windowLoader = windowLoader;
        this._aggerateModel = aggerateModel;
        this._dataModelResolver = dataModelResolver;
        // 初始化Desktop
        for (int desktopId = 1; desktopId <= WindowCfg.MAX_DESKTOP; desktopId++) {
            _desktops[desktopId - 1] = new Desktop(desktopId, canvas);
        }
        _curDesktop = _desktops[0];
    }

    #region 容器管理

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
    /// <param name="windowUri">窗口路径</param>
    /// <returns></returns>
    public Window GetWindow(string windowUri) {
        return _uri2WindowMap.TryGetValue(windowUri, out var window) ? window : null;
    }

    /// <summary>
    /// 添加窗口
    /// </summary>
    /// <param name="window">要添加的窗口</param>
    public void Add(Window window) {
        if (string.IsNullOrEmpty(window.windowUri)) {
            throw new Exception("window.WindowUri is empty");
        }
        _windowMap.Add(window.InstId, window); // 检测重复
        _uri2WindowMap.Add(window.windowUri, window);
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
    /// 销毁Window
    /// </summary>
    public void Destroy(Window window) {
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
    /// 立即销毁Window
    /// </summary>
    /// <param name="window"></param>
    public void DestroyImmediately(Window window) {
        if (window.Status == ComponentStatus.Destroyed) return;
        try {
            window.Stop();
        }
        catch (Exception ex) {
            logger.Warn(ex, "Window.Stop caught exception, name: " + window.name);
        }

        _windowMap.Remove(window.InstId);
        _uri2WindowMap.Remove(window.windowUri);
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
    public void Open(string windowUri, GameObject gameObject, WindowOpenArgs openArgs, int pInstId = 0) {
        WindowCfg windowCfg = gameObject.GetComponent<WindowCfg>();
        if (!windowCfg) {
            throw new Exception("invalid gameObject");
        }
        Window window = new Window(windowCfg, windowUri, this);
        windowCfg.SetWindow(window); // 双向绑定
        window.ParentInstId = pInstId;
        window.OpenArgs = openArgs ?? new WindowOpenArgs();
        Add(window);
    }

    /// <summary>
    /// 打开窗口
    /// </summary>
    /// <param name="windowUri">窗口路径</param>
    /// <param name="openArgs">打开参数</param>
    /// <param name="pInstId">父窗口ID</param>
    public void Open(string windowUri, WindowOpenArgs openArgs, int pInstId = 0) {
        openArgs ??= new WindowOpenArgs();
        if (_uri2WindowMap.TryGetValue(windowUri, out Window window)) {
            if (window.Status != ComponentStatus.Terminated && !openArgs.reopen) {
                return; // 拒绝请求
            }
            Reopen(window, openArgs, pInstId);
            return;
        }
        // 取消既有加载请求 - 新的加载重新计时，旧加载可能存在异常
        _loadingTaskMap.Remove(windowUri);

        double timeout = openArgs.timeout > 0 ? openArgs.timeout : LoadingTimeout;
        if (openArgs.asyncLoad) {
            ValueFuture<GameObject> future = _windowLoader.LoadAsync(windowUri, timeout);
            LoadingTask loadingTask = new LoadingTask(windowUri, openArgs, pInstId, time.UnscaledTime + timeout);
            _loadingTaskMap[windowUri] = loadingTask;
            // 必须添加await回调，过时返回的对象需要被销毁
            AwaitTask(future, loadingTask).Forget();
        } else {
            try {
                GameObject gameObject = _windowLoader.Load(windowUri, timeout);
                Open(windowUri, gameObject, openArgs, pInstId);
            }
            catch (Exception ex) {
                logger.Warn(ex, "load window failed, windowUri: " + windowUri);
            }
        }
    }

    private async ValueFuture AwaitTask(ValueFuture<GameObject> future, LoadingTask loadingTask) {
        TaskResult<GameObject> taskResult = await future.GetAwaitable(SuppressedTypes.All);

        string windowUri = loadingTask.windowUri;
        if (_loadingTaskMap.TryGetValue(windowUri, out LoadingTask exist)
            && ReferenceEquals(exist, loadingTask)) {
            // 任务未被取消
            _loadingTaskMap.Remove(windowUri);
            if (taskResult.IsSucceeded) {
                Open(windowUri, taskResult.Result, loadingTask.openArgs, loadingTask.pInstId);
            } else {
                logger.Warn(taskResult.Exception, "load window failed, windowUri: " + windowUri);
            }
        } else {
            // 任务被取消 - 需要销毁被加载的对象
            if (taskResult.IsSucceeded) {
                UnityEngine.Object.Destroy(taskResult.Result);
            } else {
                logger.Warn(taskResult.Exception, "load window failed, windowUri: " + windowUri);
            }
        }
    }

    private void Reopen(Window window, WindowOpenArgs openArgs, int pInstId) {
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
        window.ParentInstId = pInstId;
        window.OpenArgs = openArgs;
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
    /// <param name="windowUri">窗口路径</param>
    /// <param name="force">是否强制关闭常驻窗口</param>
    public void Close(string windowUri, bool force = false) {
        if (_loadingTaskMap.Remove(windowUri)) { // 取消加载
            return;
        }
        if (!_uri2WindowMap.TryGetValue(windowUri, out Window window)) {
            return;
        }
        if (window.windowCfg.unclosable && !force) { // 常驻UI需要显式强制关闭
            return;
        }
        Destroy(window);
    }

    /// <summary>
    /// 关闭多个窗口
    /// </summary>
    /// <param name="windowUris">窗口路径</param>
    /// <param name="force">是否强制关闭常驻窗口</param>
    public void Close(List<string> windowUris, bool force = false) {
        foreach (string windowUri in windowUris) {
            Close(windowUri, force);
        }
    }

    /// <summary>
    /// 关闭当前桌面的具有任一指定Tag的窗口
    /// </summary>
    /// <param name="tags">需要关闭的窗口类型</param>
    /// <param name="force">是否强制关闭常驻窗口</param>
    public void CloseTagged(HashSet<int> tags, bool force = false) {
        foreach (Window window in new List<Window>(_curDesktop.Stack)) {
            if (UIInternal.IsIntersect(window.windowCfg.tags, tags)) {
                Close(window.windowUri, force);
            }
        }
    }

    /// <summary>
    /// 关闭当前桌面所有普通窗口(非常驻窗口)
    /// </summary>
    /// <param name="force">是否强制关闭常驻窗口</param>
    public void CloseAll(bool force = false) {
        foreach (Window window in new List<Window>(_curDesktop.Stack)) {
            Close(window.windowUri, force);
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
    /// <param name="windowUri">窗口路径</param>
    /// <param name="desktopId">桌面id</param>
    public void MoveToDesktop(string windowUri, int desktopId) {
        if (!_uri2WindowMap.TryGetValue(windowUri, out Window window)) {
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
    /// <param name="windowUri"></param>
    /// <returns></returns>
    public bool HasLoadingTask(string windowUri) {
        return _loadingTaskMap.ContainsKey(windowUri);
    }
    
    private void CheckLoadTimeout() {
        var enumerator = _loadingTaskMap.GetEnumerator();
        while (enumerator.MoveNext()) {
            LoadingTask loadingTask = enumerator.Current.Value;
            if (loadingTask.expireTime <= time.UnscaledTime) {
                enumerator.Remove();
            }
        }
        enumerator.Dispose();
    }

    #endregion

    #region update管理

    /// <summary>
    /// 执行窗口的EarlyUpdate方法
    /// 注：该方法其实主要是为协程服务的
    /// </summary>
    public void EarlyUpdate(double unscaledDeltaTime) {
        time.Update(unscaledDeltaTime);
        CheckLoadTimeout();

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
    }

    /// <summary>
    /// 执行窗口的Update方法
    /// </summary>
    public void Update() {
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
    }

    /// <summary>
    /// 执行窗口的LateUpdate方法
    /// </summary>
    public void LateUpdate() {
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

        // 处理延迟销毁
        BetterIndexedPriorityQueue<Window> closedWindowList = _closedWindowList;
        while (closedWindowList.TryDequeue(out Window window)) {
            if (window.destroyTime < time.UnscaledTime) {
                break;
            }
            closedWindowList.Dequeue();
            DestroyImmediately(window);
        }
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
        window.destroyTime = maxIdleTime switch
        {
            < 0 => time.UnscaledTime + 365 * DatetimeUtil.SecondsPerDay,
            0 => time.UnscaledTime,
            _ => time.UnscaledTime + maxIdleTime
        };
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
            Destroy(tempWindow);
        }
        windowList.EndItr();
    }

    /// <summary>
    /// 结束数据模型
    /// </summary>
    public object ResolveDataModel(object parentDataModel, string dataAddress) {
        if (string.IsNullOrWhiteSpace(dataAddress)) {
            return parentDataModel;
        }
        if (_dataModelResolver == null) {
            throw new InvalidOperationException("dataModelResolver is null");
        }
        return _dataModelResolver.Resolve(_aggerateModel, parentDataModel, dataAddress);
    }

    #region PROPS

    public Canvas Canvas => canvas;
    public IAggerateModel AggerateModel => _aggerateModel;
    public WindowLoader WindowLoader => _windowLoader;
    public IDataModelResolver DataModelResolver => _dataModelResolver;
    public Desktop CurrentDesktop => _curDesktop;

    public double LoadingTimeout {
        get => loadingTimeout;
        set => loadingTimeout = value;
    }

    // 不可修改
    public Dictionary<int, Window> WindowMap => _windowMap;
    public Dictionary<string, Window> Uri2WindowMap => _uri2WindowMap;
    public IndexedDynamicArray<Window> WindowList => _windowList;

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
    private sealed class LoadingTask
    {
        public readonly string windowUri;
        public readonly WindowOpenArgs openArgs;
        public readonly int pInstId;
        public readonly double expireTime;

        public LoadingTask(string windowUri, WindowOpenArgs openArgs, int pInstId, double expireTime) {
            this.windowUri = windowUri;
            this.openArgs = openArgs;
            this.pInstId = pInstId;
            this.expireTime = expireTime;
        }
    }
}
}