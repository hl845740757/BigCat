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
using Wjybxx.Commons.Collections;
using Object = UnityEngine.Object;

namespace Wjybxx.BigCat.Assetor
{
/// <summary>
/// 资源管理器
///
/// 注：
/// 1.该管理器是<see cref="IPackageManager"/>和<see cref="IBundleManager"/>的集成门面。
/// 2.由于游戏的启动逻辑和停止逻辑可能不同，因此启动和停止逻辑由用户负责，但需要在准备就绪后调用<see cref="BuildQuery"/>。
/// </summary>
public class ResourceManager
{
    private readonly TaskScheduler _scheduler;
    private readonly List<IPackageManager> _packageManagers = new List<IPackageManager>();
    private readonly List<IBundleManager> _bundleManagers = new List<IBundleManager>();
    //
    private readonly AssetQuery _query = new AssetQuery();
    private readonly Dictionary<ProviderId, Provider> _providers = new(1000);
    private readonly LinkedHashSet<Provider> _idleProviders = new(200);
    //
    private long _assetMaxIdleTime = 5 * 1000;
    private long _bundleMaxIdleTime = 15 * 1000;
    private long _lastCheckTime;

    public ResourceManager(TaskScheduler scheduler) {
        _scheduler = scheduler;
        _lastCheckTime = scheduler.FrameTime;
        scheduler.AddChild(new Updater(this));
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
        _query.BuildCache();
    }

    #region update

    private class Updater : ResourceTask
    {
        private readonly ResourceManager _resourceMgr;

        public Updater(ResourceManager resourceMgr) {
            _resourceMgr = resourceMgr;
        }

        protected override void Execute() {
            _resourceMgr.Update();
        }
    }

    /// <summary>
    /// 心跳方法
    /// </summary>
    private void Update() {
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
    public AssetHandle LoadBinaryAssetAsync(string location, int priority = 0) {
        return LoadBinaryAssetAsync(location, priority, ELoadMethod.LoadBinaryAsset);
    }

    /// <summary>
    /// 加载Location所属Bundle的所有指定类型资产
    /// 注：通常用于加载自定义资产，
    /// </summary>
    /// <returns></returns>
    /// <param name="location">资产坐标，建议使用全路径</param>
    /// <param name="priority">优先级</param>
    public AssetHandle LoadAllBinaryAssetAsync(string location, int priority = 0) {
        return LoadBinaryAssetAsync(location, priority, ELoadMethod.LoadAllBinaryAssets);
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
        AssetFileInfo assetInfo = GetAssetInfo(location);
        Provider provider;
        if (assetInfo == null) {
            provider = GetErrorProvider(assetType, loadMethod);
        } else {
            ProviderId providerId = new ProviderId(assetInfo.assetPath, assetType, loadMethod);
            if (!_providers.TryGetValue(providerId, out provider)) {
                provider = CreateAssetProvider(assetInfo, providerId, priority);
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

    private AssetHandle LoadBinaryAssetAsync(string location, int priority, ELoadMethod loadMethod) {
        if (string.IsNullOrEmpty(location)) {
            throw new ArgumentNullException(nameof(location));
        }
        AssetFileInfo assetInfo = GetAssetInfo(location);
        Provider provider;
        if (assetInfo == null) {
            provider = GetErrorProvider(typeof(BinaryAsset), loadMethod);
        } else {
            ProviderId providerId = new ProviderId(assetInfo.assetPath, typeof(BinaryAsset), loadMethod);
            if (!_providers.TryGetValue(providerId, out provider)) {
                provider = CreateBinaryAssetProvider(assetInfo, providerId, priority);
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
    /// 注：
    /// 1.Bundle任务不立即启动，以允许外部调用阻塞接口转同步。
    /// 2.Bundle之间不可存在编译时循环依赖，否则会导致死循环。
    /// </summary>
    private BundleProvider LoadBundleAsync(AssetBundleInfo bundleInfo, int priority) {
        ProviderId providerId = new ProviderId(bundleInfo.assetPath, null, ELoadMethod.LoadBundle);
        if (!_providers.TryGetValue(providerId, out Provider provider)) {
            provider = CreateBundleProvider(bundleInfo, providerId, priority);
            provider.Priority = priority;
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
        List<BundleProvider> upstreamBundles = new List<BundleProvider>(bundleInfo.upstreamBundles.Count);
        foreach (int bundleId in bundleInfo.upstreamBundles) {
            AssetBundleInfo upstreamBundle = bundleInfo.packageInfo.id2BundleDic[bundleId];
            upstreamBundles.Add(LoadBundleAsync(upstreamBundle, priority));
        }
        return new BundleProvider(this, providerId, bundleInfo, upstreamBundles);
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
        LinkedHashSet<Provider>.Enumerator enumerator = _idleProviders.GetEnumerator();
        while (enumerator.MoveNext()) {
            Provider provider = enumerator.Current!;
            if (!provider.CanDestroy()) {
                continue;
            }
            if (force || IsIdleTimeout(provider)) {
                enumerator.Remove();
                provider.Destroy();
            }
        }
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
    ///
    /// 注：运行时的location不可以包含反斜杠，运行时规格化只处理大小写问题。
    /// </summary>
    /// <param name="location"></param>
    /// <exception cref="ArgumentException">如果资源路径包含反斜杠</exception>
    public AssetFileInfo GetAssetInfo(string location) {
        if (string.IsNullOrEmpty(location)) {
            return null;
        }
        Query.assetIndex2AssetDic.TryGetValue(location, out var assetInfo);
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