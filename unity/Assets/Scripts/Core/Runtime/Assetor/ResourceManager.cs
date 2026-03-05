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
using System.Globalization;
using System.Text;
using UnityEngine;
using Wjybxx.BTree;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Inject.Attributes;
using Wjybxx.Commons.Logger;
using Wjybxx.Commons.Pool;
using ILogger = Wjybxx.Commons.Logger.ILogger;
using Object = UnityEngine.Object;

namespace Wjybxx.BigCat.Assetor
{
/// <summary>
/// 资源管理器
///
/// 注：
/// 1.该管理器是<see cref="IPackageManager"/>和<see cref="IBundleManager"/>的集成门面。
/// 2.该管理器也为资源加载相关组件提供Update支持<see cref="TaskScheduler"/>。
/// 3.由于游戏的启动逻辑和停止逻辑可能不同，因此启动和停止逻辑由用户负责，但需要在准备就绪后调用<see cref="BuildQuery"/>。
/// </summary>
public class ResourceManager
{
    private static readonly ILogger logger = LoggerFactory.GetLogger<ResourceManager>();
    public static ResourceManager Inst { get; set; }

    private readonly TaskScheduler _scheduler;
    private readonly List<IPackageManager> _packageManagers = new List<IPackageManager>();
    private readonly List<IBundleManager> _bundleManagers = new List<IBundleManager>();
    //
    private readonly AssetQuery _query = new AssetQuery();
    private readonly Dictionary<ProviderId, Provider> _providers = new(1000);
    private readonly LinkedHashSet<Provider> _idleProviders = new(200);
    private readonly HashSet<string> _loadStack = new HashSet<string>(16);
    private readonly List<Provider> _removeList = new List<Provider>(10);
    //
    private long _assetMaxIdleTime = 5 * 1000;
    private long _bundleMaxIdleTime = 10 * 1000;
    private long _lastCheckTime;

    [Inject]
    public ResourceManager() {
        _scheduler = new TaskScheduler();
        TaskEntry<Blackboard> taskEntry = new TaskEntry<Blackboard>()
        {
            RootTask = _scheduler,
            Blackboard = new Blackboard()
        };
        taskEntry.Update(); // 启动
    }

    /// <summary>
    /// 资产查询支持
    /// </summary>
    public AssetQuery Query => _query;
    /// <summary>
    /// 任务调度器
    /// </summary>
    public TaskScheduler Scheduler => _scheduler;
    /// <summary>
    /// 所有的资源包管理器
    /// </summary>
    public List<IPackageManager> PackageManagers => _packageManagers;
    /// <summary>
    /// 所有的Bundle加载器
    /// </summary>
    public List<IBundleManager> BundleManagers => _bundleManagers;

    /// <summary>
    /// 普通资产的最大空闲时间
    /// </summary>
    public long AssetMaxIdleTime {
        get => _assetMaxIdleTime;
        set => _assetMaxIdleTime = Math.Max(value, 0);
    }
    /// <summary>
    /// Bundle资产的最大空闲时间
    /// </summary>
    public long BundleMaxIdleTime {
        get => _bundleMaxIdleTime;
        set => _bundleMaxIdleTime = Math.Max(value, 0);
    }

    /// <summary>
    /// 添加资源包管理器
    /// </summary>
    public void AddPackageManager(IPackageManager packageManager) {
        if (packageManager == null) throw new ArgumentNullException(nameof(packageManager));
        if (GetPackageManager(packageManager.PackageName) != null) {
            throw new ArgumentException($"PackageManager with name {packageManager.PackageName} already exists");
        }
        _packageManagers.Add(packageManager);
    }

    /// <summary>
    /// 添加Bundle管理器
    /// </summary>
    public void AddBundleManager(IBundleManager bundleManager) {
        if (bundleManager == null) throw new ArgumentNullException(nameof(bundleManager));
        if (_bundleManagers.Contains(bundleManager)) {
            throw new ArgumentException($"BundleManager {bundleManager} already exists.");
        }
        _bundleManagers.Add(bundleManager);
    }

    /// <summary>
    /// 查找关联的包管理器
    /// </summary>
    public IPackageManager GetPackageManager(string packageName) {
        if (string.IsNullOrEmpty(packageName)) throw new ArgumentNullException(nameof(packageName));
        for (int index = 0; index < _packageManagers.Count; index++) {
            IPackageManager manager = _packageManagers[index];
            if (manager.PackageName == packageName) return manager;
        }
        return null;
    }

    /// <summary>
    /// 构建查询缓存
    ///
    /// 注：可重复调用，支持动态增加资源包。
    /// </summary>
    public void BuildQuery() {
        foreach (IPackageManager packageManager in _packageManagers) {
            _query.AddPackage(packageManager.PackageInfo);
        }
        _query.BuildCache();
    }

    #region update

    /// <summary>
    /// 心跳方法
    /// </summary>
    public void Update() {
        _scheduler.Template_Execute(false); // false不影响正确性
        // 自动释放资源 - 每秒1次即可，避免不必要的开销
        long frameTime = _scheduler.FrameTime;
        if (frameTime - _lastCheckTime >= 1000) {
            _lastCheckTime = frameTime;
            UnloadIdleTimeoutProviders();
        }
    }

    #endregion

    #region 资源加载

    /// <summary>
    /// 加载主资源
    /// </summary>
    public AssetHandle LoadAssetAsync<T>(string location, int priority = 0) where T : Object {
        return LoadAssetAsync(location, typeof(T), priority, ELoadMethod.LoadAsset);
    }

    /// <summary>
    /// 加载主资源
    /// </summary>    
    public AssetHandle LoadAssetAsync(string location, Type assetType, int priority = 0) {
        return LoadAssetAsync(location, assetType, priority, ELoadMethod.LoadAsset);
    }

    /// <summary>
    /// 加载主资源和子资源
    /// </summary>
    public AssetHandle LoadAssetWithSubAssetsAsync<T>(string location, int priority = 0) where T : Object {
        return LoadAssetAsync(location, typeof(T), priority, ELoadMethod.LoadAssetWithSubAssets);
    }

    /// <summary>
    /// 加载主资源和子资源
    /// </summary>
    public AssetHandle LoadAssetWithSubAssetsAsync(string location, Type assetType, int priority = 0) {
        return LoadAssetAsync(location, assetType, priority, ELoadMethod.LoadAssetWithSubAssets);
    }

    /// <summary>
    /// 加载Location所属Bundle的所有指定类型资产
    /// 注：通常用于加载自定义资产，
    /// </summary>
    /// <param name="location">资产坐标，建议使用全路径</param>
    /// <param name="priority">优先级</param>
    public AssetHandle LoadAllAssetsAsync<T>(string location, int priority = 0) where T : Object {
        return LoadAssetAsync(location, typeof(T), priority, ELoadMethod.LoadAllAssets);
    }

    /// <summary>
    /// 加载Location所属Bundle的所有指定类型资产
    /// 注：通常用于加载自定义资产，
    /// </summary>
    public AssetHandle LoadAllAssetsAsync(string location, Type assetType, int priority = 0) {
        return LoadAssetAsync(location, assetType, priority, ELoadMethod.LoadAllAssets);
    }

    /// <summary>
    /// 加载原始二进制资产
    /// </summary>
    /// <param name="location">资产坐标，建议使用全路径</param>
    /// <param name="assetType">资产类型，用于解压缩</param>
    /// <param name="priority">优先级</param>
    public AssetHandle LoadBinaryAssetAsync(string location, Type assetType = null, int priority = 0) {
        return LoadBinaryAssetAsync(location, assetType, priority, ELoadMethod.LoadBinaryAsset);
    }

    /// <summary>
    /// 加载原始二进制资产
    /// </summary>
    public AssetHandle LoadBinaryAssetAsync<T>(string location, int priority = 0)
        where T : ScriptableObject, IBinaryAssetReceiver {
        return LoadBinaryAssetAsync(location, typeof(T), priority, ELoadMethod.LoadBinaryAsset);
    }

    /// <summary>
    /// 加载Location所属Bundle的所有指定类型资产
    /// 注：通常用于加载自定义资产，
    /// </summary>
    /// <returns></returns>
    /// <param name="location">资产坐标，建议使用全路径</param>
    /// <param name="assetType">资产类型，用于解压缩</param>
    /// <param name="priority">优先级</param>
    public AssetHandle LoadAllBinaryAssetAsync(string location, Type assetType = null, int priority = 0) {
        return LoadBinaryAssetAsync(location, assetType, priority, ELoadMethod.LoadAllBinaryAssets);
    }

    public AssetHandle LoadAllBinaryAssetAsync<T>(string location, int priority = 0)
        where T : ScriptableObject, IBinaryAssetReceiver {
        return LoadBinaryAssetAsync(location, typeof(T), priority, ELoadMethod.LoadAllBinaryAssets);
    }

    /// <summary>
    /// 加载Scene关联的资源
    ///
    /// 注：这只是确保Scene关联的Bundle加载到内存，不会直接加载Scene！
    /// </summary>
    /// <param name="sceneName">场景名</param>
    /// <param name="priority">任务优先级</param>
    /// <returns></returns>
    public AssetHandle LoadSceneAssetAsync(string sceneName, int priority = 0) {
        return LoadSceneAssetAsync(sceneName, priority, ELoadMethod.LoadSceneAsset);
    }

    #endregion

    #region internal

    private AssetHandle LoadAssetAsync(string location, Type assetType, int priority, ELoadMethod loadMethod) {
        assetType ??= typeof(Object);
        if (string.IsNullOrEmpty(location)) {
            throw new ArgumentNullException(nameof(location));
        }
        bool so = loadMethod == ELoadMethod.LoadAsset && assetType.IsSubclassOf(typeof(ScriptableObject));
        AssetFileInfo assetInfo = GetAssetInfo(location, so ? assetType.Name : null);
        Provider provider;
        if (assetInfo == null) {
            provider = GetErrorProvider(assetType, loadMethod);
            logger.LogWarn($"ObjectAsset not found, location: {location}, assetType: {assetType.Name}");
        } else {
            ProviderId providerId = new ProviderId(assetInfo.assetPath, assetType, loadMethod);
            if (!_providers.TryGetValue(providerId, out provider)) {
                provider = CreateAssetProvider(assetInfo, providerId, priority);
                provider.TimeReleased = _scheduler.FrameTime;
                _scheduler.AddChild(provider);
                _providers[providerId] = provider;
            }
        }
        AssetHandle handle = new AssetHandle(location, provider);
        handle.Retain();
        return handle;
    }

    private AssetProvider CreateAssetProvider(AssetFileInfo assetInfo, ProviderId providerId, int priority) {
        BundleProvider bundleProvider = LoadBundleAsync(assetInfo.bundleInfo, priority);
        AssetProvider provider = new AssetProvider(this, providerId, assetInfo, bundleProvider);
        provider.Priority = priority;
        return provider;
    }

    private AssetHandle LoadBinaryAssetAsync(string location, Type assetType, int priority, ELoadMethod loadMethod) {
        assetType ??= typeof(BinaryAsset);
        if (string.IsNullOrEmpty(location)) {
            throw new ArgumentNullException(nameof(location));
        }
        bool so = loadMethod == ELoadMethod.LoadBinaryAsset && assetType.IsSubclassOf(typeof(ScriptableObject));
        AssetFileInfo assetInfo = GetAssetInfo(location, so ? assetType.Name : null);
        Provider provider;
        if (assetInfo == null) {
            provider = GetErrorProvider(assetType, loadMethod);
            logger.LogWarn($"BinaryAsset not found, location: {location}");
        } else {
            ProviderId providerId = new ProviderId(assetInfo.assetPath, assetType, loadMethod);
            if (!_providers.TryGetValue(providerId, out provider)) {
                provider = CreateBinaryAssetProvider(assetInfo, providerId, priority);
                provider.TimeReleased = _scheduler.FrameTime;
                _scheduler.AddChild(provider);
                _providers[providerId] = provider;
            }
        }
        AssetHandle handle = new AssetHandle(location, provider);
        handle.Retain();
        return handle;
    }

    private BinaryAssetProvider CreateBinaryAssetProvider(AssetFileInfo assetInfo, ProviderId providerId, int priority) {
        BundleProvider bundleProvider = LoadBundleAsync(assetInfo.bundleInfo, priority);
        BinaryAssetProvider provider = new BinaryAssetProvider(this, providerId, assetInfo, bundleProvider);
        provider.Priority = priority;
        return provider;
    }

    private AssetHandle LoadSceneAssetAsync(string sceneName, int priority, ELoadMethod loadMethod) {
        if (string.IsNullOrEmpty(sceneName)) {
            throw new ArgumentNullException(nameof(sceneName));
        }
        AssetFileInfo assetInfo = GetSceneAssetInfo(sceneName);
        Provider provider;
        if (assetInfo == null) {
            provider = GetErrorProvider(null, loadMethod);
        } else {
            ProviderId providerId = new ProviderId(assetInfo.assetPath, null, loadMethod);
            if (!_providers.TryGetValue(providerId, out provider)) {
                provider = CreateSceneAssetProvider(assetInfo, providerId, priority);
                provider.TimeReleased = _scheduler.FrameTime;
                _scheduler.AddChild(provider);
                _providers[providerId] = provider;
            }
        }
        AssetHandle handle = new AssetHandle(sceneName, provider);
        handle.Retain();
        return handle;
    }

    private SceneAssetProvider CreateSceneAssetProvider(AssetFileInfo assetInfo, ProviderId providerId, int priority) {
        BundleProvider bundleProvider = LoadBundleAsync(assetInfo.bundleInfo, priority);
        SceneAssetProvider provider = new SceneAssetProvider(this, providerId, assetInfo, bundleProvider);
        provider.Priority = priority;
        return provider;
    }

    /// <summary>
    /// 创建异步Bundle加载任务
    /// 
    /// 注：Bundle任务不立即启动，以允许外部调用阻塞接口转同步。
    /// </summary>
    private BundleProvider LoadBundleAsync(AssetBundleInfo bundleInfo, int priority) {
        ProviderId providerId = new ProviderId(bundleInfo.assetPath, null, ELoadMethod.LoadBundle);
        if (!_providers.TryGetValue(providerId, out Provider provider)) {
            provider = CreateBundleProvider(bundleInfo, providerId, priority);
            provider.TimeReleased = _scheduler.FrameTime;
            provider.Priority = priority; // 修正上游Provider的优先级
            _scheduler.AddChild(provider);
            _providers[providerId] = provider;
        } else {
            if (provider.Priority > priority) {
                provider.Priority = priority;
            }
        }
        return (BundleProvider)provider;
    }

    private BundleProvider CreateBundleProvider(AssetBundleInfo bundleInfo, ProviderId providerId, int priority) {
        // 为什么禁止循环依赖？循环依赖会导致同步加载死锁
        if (!_loadStack.Add(bundleInfo.bundleName)) {
            throw new InvalidOperationException($"Circular dependency detected: {CollectionUtil.ToString(_loadStack)}");
        }
        try {
            List<BundleProvider> upstreamBundles = new List<BundleProvider>(bundleInfo.upstreamBundles.Count);
            foreach (int bundleId in bundleInfo.upstreamBundles) {
                AssetBundleInfo upstreamBundleInfo = bundleInfo.packageInfo.id2BundleDic[bundleId];
                upstreamBundles.Add(LoadBundleAsync(upstreamBundleInfo, priority));
            }
            return new BundleProvider(this, providerId, bundleInfo, upstreamBundles);
        }
        finally {
            _loadStack.Remove(bundleInfo.bundleName);
        }
    }

    private Provider GetErrorProvider(Type assetType, ELoadMethod loadMethod) {
        ProviderId providerId = new ProviderId("Error", assetType, loadMethod);
        if (!_providers.TryGetValue(providerId, out Provider provider)) {
            provider = new ErrorProvider(this, providerId);
            _scheduler.WaitForCompletion(provider, 0); // 立即完成且不需要添加为子节点
            _providers[providerId] = provider;
        }
        return provider;
    }

    #endregion

    #region 引用计数

    /// <summary>
    /// 卸载未使用的资源
    /// </summary>
    public void UnloadUnusedAssets() {
        UnloadIdleTimeoutProviders(true);
    }

    /// <summary>
    /// 卸载空闲超时的资源
    /// </summary>
    /// <param name="force"></param>
    private void UnloadIdleTimeoutProviders(bool force = false) {
        // 这里删除元素时不能直接调用Destroy，因为释放资源的时候，可能会导致Bundle添加到集合
        List<Provider> removeList = _removeList.ClearAndReturn();
        var enumerator = _idleProviders.GetEnumerator();
        while (enumerator.MoveNext()) {
            Provider provider = enumerator.Current!;
            if (!provider.CanDestroy()) {
                continue;
            }
            if (force || IsIdleTimeout(provider)) {
                enumerator.Remove();
                _providers.Remove(provider.pid);
                removeList.Add(provider);
            }
        }
        foreach (Provider provider in removeList) {
            provider.Destroy();
        }
        removeList.Clear();
    }

    private bool IsIdleTimeout(Provider provider) {
        long maxIdleTime = provider is BundleProvider ? _bundleMaxIdleTime : _assetMaxIdleTime;
        return _scheduler.FrameTime - provider.TimeReleased >= maxIdleTime;
    }

    internal void RemoveFromIdles(Provider provider) {
        _idleProviders.Remove(provider);
        provider.TimeReleased = 0;
    }

    internal void AddToIdles(Provider provider) { // 实例Provider也自动销毁
        provider.TimeReleased = _scheduler.FrameTime;
        _idleProviders.Add(provider);
    }

    #endregion

    #region 查询资产信息

    /// <summary>
    /// 根据location查询AssetInfo
    /// </summary>
    /// <param name="location">资产坐标</param>
    public AssetFileInfo GetAssetInfo<T>(string location) where T : Object {
        bool so = typeof(T).IsSubclassOf(typeof(ScriptableObject));
        return GetAssetInfo(location, so ? typeof(T).Name : null);
    }

    /// <summary>
    /// 根据location查询AssetInfo
    /// </summary>
    /// <param name="location">资产坐标</param>
    /// <param name="assetType">资产类型</param>
    public AssetFileInfo GetAssetInfo(string location, string assetType = null) {
        if (string.IsNullOrEmpty(location)) {
            return null;
        }
        if (!Query.assetIndex2AssetDic.TryGetValue(location, out AssetFileInfo assetInfo)
            && !string.IsNullOrEmpty(assetType)
            && !location.StartsWith("Assets/")
            && location.LastIndexOf('/') < 0) {
            // Fallback - 尝试按照资产类型索引查询
            int idx = location.LastIndexOf('.');
            if (idx > 0) {
                location = location.Substring(0, idx);
            }
            location = $"{assetType}:{location}";
            Query.assetIndex2AssetDic.TryGetValue(location, out assetInfo);
        }
        return assetInfo;
    }

    /// <summary>
    /// 根据SceneName查询资产信息
    /// </summary>
    /// <param name="sceneName">场景名，不包含文件扩展名</param>
    /// <returns></returns>
    public AssetFileInfo GetSceneAssetInfo(string sceneName) {
        return Query.sceneName2AssetDic.TryGetValue(sceneName, out AssetFileInfo assetInfo) ? assetInfo : null;
    }

    /// <summary>
    /// 根据相对资产路径，查询资产信息
    /// </summary>
    /// <param name="assetPath"></param>
    /// <param name="relativePath"></param>
    /// <returns></returns>
    public AssetFileInfo GetRelativeAssetInfo(string assetPath, string relativePath) {
        string path = CombinePath(assetPath, relativePath);
        return GetAssetInfo(path);
    }

    /// <summary>
    /// 根据当前路径和相对路径，计算最终资产路径
    /// 
    /// 注：这里并非通用规则，我们只支持极简规则(允许回退n级目录)
    /// </summary>
    /// <param name="currentPath">当前资产路径</param>
    /// <param name="relativePath">相对资产路径</param>
    /// <returns></returns>
    public static string CombinePath(string currentPath, string relativePath) {
        StringBuilder sb = _sb.Clear();
        sb.EnsureCapacity(currentPath.Length + relativePath.Length);
        sb.Append(currentPath);
        sb.Length = LastIndexOf(sb, '/'); // 获得当前目录
        // 目录回退
        int idx = 0;
        while (relativePath[idx] == '.') {
            if (idx + 2 >= relativePath.Length
                || relativePath[idx + 1] != '.'
                || relativePath[idx + 2] != '/') {
                throw new InvalidOperationException($"Invalid relative path: {relativePath}");
            }
            idx += 3;
            sb.Length = LastIndexOf(sb, '/');
        }
        // 其它字符转小写
        sb.Append('/');
        for (; idx < relativePath.Length; idx++) {
            char c = _culture.ToLower(relativePath[idx]);
            sb.Append(c);
        }
        return sb.ToString();
    }

    private static int LastIndexOf(StringBuilder sb, char c) {
        for (int idx = sb.Length - 1; idx >= 0; idx--) {
            if (sb[idx] == c) return idx;
        }
        return -1;
    }

    /// <summary>
    /// 资产路径规格化
    /// </summary>
    private static string NormalizePath(string path) {
        // 避免构建额外在字符串
        int upIndex = -1;
        for (int index = 0; index < path.Length; index++) {
            char c = path[index];
            if (c == '\\') {
                throw new ArgumentException(path);
            }
            if (char.IsUpper(c)) {
                upIndex = index;
                break;
            }
        }
        if (upIndex < 0) {
            return path;
        }
        StringBuilder sb = _sb.Clear();
        sb.Append(path, 0, upIndex);
        sb.Append(_culture.ToLower(path[upIndex]));
        for (int index = upIndex + 1; index < path.Length; index++) {
            char c = path[index];
            if (c == '\\') {
                throw new ArgumentException(path);
            }
            sb.Append(_culture.ToLower(c));
        }
        return sb.ToString();
    }

    private static readonly StringBuilder _sb = new StringBuilder(64);
    private static readonly TextInfo _culture = CultureInfo.InvariantCulture.TextInfo;

    #endregion

    #region util

    internal static bool IsCompleted(List<ResourceTask> tasks) {
        foreach (ResourceTask resourceTask in tasks) {
            if (!resourceTask.IsCompleted) return false;
        }
        return true;
    }

    internal static bool IsFailedOrCancelled(List<ResourceTask> tasks) {
        foreach (ResourceTask resourceTask in tasks) {
            if (!resourceTask.IsFailedOrCancelled) return false;
        }
        return true;
    }

    #endregion
}
}