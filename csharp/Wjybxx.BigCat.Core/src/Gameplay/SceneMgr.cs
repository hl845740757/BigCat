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
using Wjybxx.BigCat.Co;
using Wjybxx.BigCat.Fx;
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Concurrent;
using Wjybxx.Commons.Fx;
using Wjybxx.Commons.Inject;
using Wjybxx.Commons.Inject.Attributes;
using Wjybxx.Commons.Logger;

namespace Wjybxx.BigCat.Gameplay
{
/// <summary>
/// Scene管理器
/// </summary>
public sealed class SceneMgr
{
    private static readonly ILogger logger = LoggerFactory.GetLogger<SceneMgr>();
#if UNITY_2021_3_OR_NEWER || CLIENT_PROJECT
    /// <summary>
    /// 
    /// 1.Unity下提供静态访问方法，方便客户端编码；
    /// 2.SceneMgr由容器管理，设置到这里。
    /// </summary>
    public static SceneMgr Inst { get; set; }

    /// <summary>
    /// 客户端栈结构解析
    /// (数据在这里提供，方法通过扩展方法实现)
    /// </summary>
    public readonly List<Scene> stack = new List<Scene>(4);
    /// <summary>
    /// List的初始空间
    /// </summary>
    private const int INIT_CAPACITY = 4;
#else
    private const int INIT_CAPACITY = 20;
#endif

#nullable disable
    /// <summary>
    /// Scene可能用到的外部依赖
    /// </summary>
    private IInjector _injector;
    /// <summary>
    /// 所有的游戏场景 -- 仅用于查询，不用于Update
    /// </summary>
    private readonly Dictionary<long, Scene> _sceneDic = new Dictionary<long, Scene>(INIT_CAPACITY);
    /// <summary>
    /// 按照场景配置id排序的列表 -- 用于查询指定cid的场景。
    /// 即使是服务器，场景的数量也不算很多，增删的频率也不高，因此使用简单的插入排序是足够的。
    /// </summary>
    private readonly SortedList<SceneSortKey, Scene> _sortedSceneList = new(INIT_CAPACITY, SceneSortKey.Comparer);

    /// <summary>
    /// 所有的游戏场景
    /// </summary>
    private readonly IndexedDynamicArray<Scene> _sceneList = new IndexedDynamicArray<Scene>(SIndexHelper.GetInst(0), INIT_CAPACITY);
    /// <summary>
    /// 所有活动中的Scene列表
    /// </summary>
    private readonly IndexedDynamicArray<Scene> _activeSceneList = new IndexedDynamicArray<Scene>(SIndexHelper.GetInst(1), 10);
    /// <summary>
    /// 待延迟销毁的Scene
    /// </summary>
    private readonly IndexedDynamicArray<Scene> _closedSceneList = new IndexedDynamicArray<Scene>(SIndexHelper.GetInst(2), 10);

    /// <summary>
    /// 时间系统
    /// </summary>
    private readonly GTime time = new GTime();
    /// <summary>
    /// 协程管理器
    /// </summary>
    private readonly CoroutineMgr coroutineMgr;
#nullable restore

    [Inject]
    public SceneMgr(WorkerHolder workerHolder, SceneMgrCfg cfg) {
        coroutineMgr = new CoroutineMgr(workerHolder.Worker, time,
            cfg.minPeriod, cfg.unscaledMinPeriod,
            enableFrameQueue: cfg.enableFrameQueue);
    }

    /// <summary>
    /// 所有场景的依赖
    /// </summary>
    public IInjector Injector {
        get => _injector;
        set => _injector = value;
    }

    /// <summary>
    /// 场景循环的时间轴
    /// </summary>
    public GTime Time => time;

    /// <summary>
    /// 场景循环关联的协程管理器
    /// </summary>
    public CoroutineMgr CoroutineMgr => coroutineMgr;

    /// <summary>
    /// 创建循环绑定的线程
    /// </summary>
    public Worker Worker => (Worker)coroutineMgr.EventLoop;

    #region 容器管理

    /// <summary>
    /// 场景List
    /// 注：应当避免修改返回的List
    /// </summary>
    public IndexedDynamicArray<Scene> SceneList => _sceneList;
    /// <summary>
    /// 场景字典
    /// 注：应当避免修改返回的字典
    /// </summary>
    public Dictionary<long, Scene> SceneDic => _sceneDic;

    /// <summary>
    /// 根据实例id查找场景
    /// </summary>
    /// <param name="instId"></param>
    /// <returns></returns>
    public Scene? GetScene(long instId) {
        _sceneDic.TryGetValue(instId, out Scene scene);
        return scene;
    }

    /// <summary>
    /// 根据配置id查找场景
    /// </summary>
    /// <param name="configId"></param>
    /// <returns></returns>
    public Scene? FindFirst(int configId) {
        IList<SceneSortKey> keys = _sortedSceneList.Keys;
        int index = CollectionUtil.BinarySearch(keys, new SceneSortKey(configId, 0), SceneSortKey.Comparer);
        if (index >= 0) { // 不应该存在instId为0的对象
            throw new AssertionError();
        }
        index = (index + 1) * -1; // insert point
        if (index < keys.Count && keys[index].configId == configId) {
            return _sortedSceneList.Values[index];
        }
        return null;
    }

    /// <summary>
    /// 查找指定cid的所有场景
    /// </summary>
    /// <param name="configId"></param>
    /// <returns></returns>
    public List<Scene> FindAll(int configId) {
        IList<SceneSortKey> keys = _sortedSceneList.Keys;
        int index = CollectionUtil.BinarySearch(keys, new SceneSortKey(configId, 0), SceneSortKey.Comparer);
        if (index >= 0) { // 不应该存在instId为0的对象
            throw new AssertionError();
        }
        index = (index + 1) * -1; // insert point
        List<Scene> result = new List<Scene>();
        while (index < keys.Count && keys[index].configId == configId) {
            result.Add(_sortedSceneList.Values[index]);
            index++;
        }
        return result;
    }

    /// <summary>
    /// 添加场景
    ///
    /// 注：场景在添加后会立即启动，必须在构造完成的情况下调用
    /// </summary>
    /// <param name="scene"></param>
    public void Add(Scene scene) {
        if (scene == null) throw new ArgumentNullException(nameof(scene));
        if (scene.InstId <= 0) throw new ArgumentException("scene.InstId <= 0");
        _sceneDic.Add(scene.InstId, scene);
        _sceneList.Add(scene);
        _sortedSceneList.Add(new SceneSortKey(scene.ConfigId, scene.InstId), scene);
        _activeSceneList.Add(scene);
        try {
            scene.SceneMgr = this;
            if (scene.Status == ComponentStatus.New) {
                scene.SetInitialized();
            }
            scene.Start();
        }
        catch (Exception ex) {
            logger.Error(ex, "scene.Start failed, configId: " + scene.ConfigId);
        }
    }

    /// <summary>
    /// 关闭Scene
    /// </summary>
    public void Close(Scene scene) {
        if (scene.Status == ComponentStatus.Destroyed) return;
        try {
            scene.Stop();
        }
        catch (Exception ex) {
            logger.Warn(ex, "scene.Stop caught exception, configId: " + scene.ConfigId);
        }
        if (!_closedSceneList.Contains(scene)) {
            _activeSceneList.Remove(scene);
            _closedSceneList.Add(scene);
        }
    }

    /// <summary>
    /// 销毁Scene
    /// </summary>
    /// <param name="scene"></param>
    private void Destroy(Scene scene) {
        if (scene.Status == ComponentStatus.Destroyed) return;
        try {
            scene.Stop();
        }
        catch (Exception ex) {
            logger.Warn(ex, "scene.Stop caught exception, configId: " + scene.ConfigId);
        }

        _sceneDic.Remove(scene.InstId);
        _sortedSceneList.Remove(new SceneSortKey(scene.ConfigId, scene.InstId));
        _sceneList.Remove(scene);
        _activeSceneList.Remove(scene);
        _closedSceneList.Remove(scene);

        try {
            scene.Destroy();
        }
        catch (Exception ex) {
            logger.Warn(ex, "scene.Destroy caught exception, configId: " + scene.ConfigId);
        }
    }

    #endregion

    #region 场景Update管理

    /// <summary>
    /// 帧循环开始
    ///
    /// 注：该方法主要用于调度协程。
    /// </summary>
    /// <param name="unscaledDeltaTime"></param>
    public void BeginOfFrame(double unscaledDeltaTime) {
        time.Update(unscaledDeltaTime);
        coroutineMgr.Update(GameLoopPhase.BeginOfFrame);
    }

    /// <summary>
    /// 执行场景的EarlyUpdate方法
    /// </summary>
    public void EarlyUpdate() {
        coroutineMgr.Update(GameLoopPhase.EarlyUpdate);

        double unscaledDeltaTime = time.UnscaledDeltaTime;
        IndexedDynamicArray<Scene> sceneList = _activeSceneList;
        sceneList.BeginItr();
        for (int index = 0, len = sceneList.Length; index < len; index++) {
            Scene scene = sceneList[index];
            if (scene == null || scene.Status != ComponentStatus.Running) {
                continue;
            }
            try {
                scene.EarlyUpdate(unscaledDeltaTime);
            }
            catch (Exception e) {
                logger.Warn(e, "scene.EarlyUpdate caught exception, configId: " + scene.ConfigId);
            }
        }
        sceneList.EndItr();

        coroutineMgr.Update(GameLoopPhase.PostEarlyUpdate);
    }

    /// <summary>
    /// 执行场景的FixedUpdate方法
    /// </summary>
    public void FixedUpdate(double unscaledDeltaTime) {
        time.FixedUpdate(unscaledDeltaTime);
        coroutineMgr.Update(GameLoopPhase.FixedUpdate);

        IndexedDynamicArray<Scene> sceneList = _activeSceneList;
        sceneList.BeginItr();
        for (int index = 0, len = sceneList.Length; index < len; index++) {
            Scene scene = sceneList[index];
            if (scene == null || scene.Status != ComponentStatus.Running) {
                continue;
            }
            try {
                scene.FixedUpdate(unscaledDeltaTime);
            }
            catch (Exception e) {
                logger.Warn(e, "scene.FixedUpdate caught exception, configId: " + scene.ConfigId);
            }
        }
        sceneList.EndItr();

        coroutineMgr.Update(GameLoopPhase.PostFixedUpdate);
    }

    /// <summary>
    /// 执行场景的Update方法
    /// </summary>
    public void Update() {
        coroutineMgr.Update(GameLoopPhase.Update);

        IndexedDynamicArray<Scene> sceneList = _activeSceneList;
        sceneList.BeginItr();
        for (int index = 0, len = sceneList.Length; index < len; index++) {
            Scene scene = sceneList[index];
            if (scene == null || scene.Status != ComponentStatus.Running) {
                continue;
            }
            try {
                scene.Update();
            }
            catch (Exception e) {
                logger.Warn(e, "scene.Update caught exception, configId: " + scene.ConfigId);
            }
        }
        sceneList.EndItr();

        coroutineMgr.Update(GameLoopPhase.PostUpdate);
    }

    /// <summary>
    /// 执行场景的LateUpdate方法
    /// </summary>
    public void LateUpdate() {
        coroutineMgr.Update(GameLoopPhase.LateUpdate);

        IndexedDynamicArray<Scene> sceneList = _activeSceneList;
        sceneList.BeginItr();
        for (int index = 0, len = sceneList.Length; index < len; index++) {
            Scene scene = sceneList[index];
            if (scene == null || scene.Status != ComponentStatus.Running) {
                continue;
            }
            try {
                scene.LateUpdate();
            }
            catch (Exception e) {
                logger.Warn(e, "scene.LateUpdate caught exception, configId: " + scene.ConfigId);
            }
        }
        sceneList.EndItr();

        coroutineMgr.Update(GameLoopPhase.PostLateUpdate);
    }

    /// <summary>
    /// 帧循环结束
    /// 
    /// 注：该方法主要用于调度协程。
    /// </summary>
    public void EndOfFrame() {
        // 处理延迟销毁
        IndexedDynamicArray<Scene> closedSceneList = _closedSceneList;
        closedSceneList.BeginItr();
        for (int index = 0, len = closedSceneList.Length; index < len; index++) {
            Scene scene = closedSceneList.Set(index, null);
            if (scene == null || scene.Status == ComponentStatus.Destroyed) {
                continue;
            }
            Destroy(scene);
        }
        closedSceneList.EndItr();

        coroutineMgr.Update(GameLoopPhase.EndOfFrame);
    }

    #endregion

    #region internal

    /// <summary>
    /// Scene被暂停时调用
    /// </summary>
    /// <param name="scene"></param>
    internal void OnPause(Scene scene) {
        _activeSceneList.Remove(scene);
    }

    /// <summary>
    /// Scene恢复运行时调用
    /// </summary>
    /// <param name="scene"></param>
    internal void OnResume(Scene scene) {
        _activeSceneList.Add(scene);
    }

    /// <summary>
    /// 场景执行结束时调用
    /// </summary>
    internal void OnTerminated(Scene scene) {
        _activeSceneList.Remove(scene);
        _closedSceneList.Add(scene);
    }

    #endregion
}
}