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

namespace Wjybxx.BigCat.Assetor
{
/// <summary>
/// 普通资产提供者基类
///
/// <h3>同步加载问题</h3>
/// 如果创建Provider的是同步加载请求，那么其Bundle加载会被强制为同步的；
/// 如果关联Bundle已处于异步加载状态，则抛出异常 —— 新版本Unity不再支持Bundle异步转同步；
/// 也就是说，普通资产对象同步加载的前提是：Bundle已加载。
///
/// <h3>引用计数</h3>
/// 资产Provider在构造函数中增加对Bundle的引用计数，在销毁时释放对Bundle的引用计数。
///
/// <h3>取消信号</h3>
/// 由于Bundle是共享的，因此AssetProvider的取消信号不能发布到BundleProvider，
/// 只能传播给自己的子任务。
/// </summary>
public abstract class AssetProviderBase : Provider
{
    private static readonly List<BundleProvider> emptyList = new List<BundleProvider>();

    /// <summary>
    /// 关联的资产文件信息
    /// </summary>
    public readonly AssetFileInfo assetInfo;
    /// <summary>
    /// 关联的Bundle
    /// 注：外部先创建BundleProvider，可确保优先级相同时排前面。
    /// </summary>
    public readonly BundleProvider bundleProvider;
    /// <summary>
    /// 依赖的上游Bundle
    /// 注：不可修改。
    /// </summary>
    public readonly List<BundleProvider> upstreamBundles;

    protected AssetProviderBase(ResourceManager resourceMgr, ProviderId pid,
                                AssetFileInfo assetInfo,
                                BundleProvider bundleProvider,
                                List<BundleProvider> upstreamBundles)
        : base(resourceMgr, pid) {
        this.assetInfo = assetInfo;
        this.bundleProvider = bundleProvider;
        this.upstreamBundles = upstreamBundles ?? emptyList;
        RetainBundles();
    }

    /// <summary>
    /// 资产全路径（规格化的路径）
    /// </summary>
    public string assetPath => pid.assetPath;
    /// <summary>
    /// 请求的资产类型
    /// </summary>
    public System.Type assetType => pid.assetType;
    /// <summary>
    /// 请求的加载方式
    /// </summary>
    public ELoadMethod loadMethod => pid.loadMethod;

    #region update

    /// <summary>
    /// Bundle加载是否已全部结束
    /// </summary>
    /// <returns></returns>
    public bool IsBundleLoadCompleted() {
        if ((flags & MASK_BUNDLE_LOADED) != 0) {
            return true;
        }
        if (!bundleProvider.IsCompleted) return false;
        for (int index = 0; index < upstreamBundles.Count; index++) {
            if (!upstreamBundles[index].IsCompleted) return false;
        }
        flags |= MASK_BUNDLE_LOADED;
        return true;
    }

    /// <summary>
    /// bundle加载是否已失败
    /// </summary>
    /// <returns></returns>
    public bool IsBundleLoadFailed() {
        if (bundleProvider.IsFailedOrCancelled) return true;
        for (int index = 0; index < upstreamBundles.Count; index++) {
            if (upstreamBundles[index].IsFailedOrCancelled) return true;
        }
        return false;
    }

    /// <summary>
    /// 阻塞等待Bundle加载完成
    /// </summary>
    public void WaitForBundleCompletion() {
        TaskScheduler scheduler = Scheduler;
        Blackboard blackboard = this.blackboard;
        scheduler.WaitForCompletion(bundleProvider, blackboard.stopwatch, blackboard.deadline);
        foreach (BundleProvider upstreamBundle in upstreamBundles) {
            scheduler.WaitForCompletion(upstreamBundle, blackboard.stopwatch, blackboard.deadline);
        }
    }

    protected override void OnPriorityChanged(int prevValue) {
        base.OnPriorityChanged(prevValue);
        EnsureBundleProviderPriority();
    }

    /// <summary>
    /// 确保Bundle加载器的任务处于较高的优先级
    /// </summary>
    private void EnsureBundleProviderPriority() {
        int priority = Priority;
        if (bundleProvider.Priority > priority) {
            bundleProvider.Priority = priority;
        }
        for (int index = 0; index < upstreamBundles.Count; index++) {
            BundleProvider upstreamBundle = upstreamBundles[index];
            if (upstreamBundle.Priority > priority) {
                upstreamBundle.Priority = priority;
            }
        }
    }

    #endregion

    #region 引用计数

    /// <summary>
    /// 尚未释放的资源句柄id
    /// </summary>
    private static readonly Dictionary<uint, int> retainHandles = new(1000);

    /// <summary>
    /// 注册资源句柄
    /// </summary>
    public void Retain(AssetHandle handle, int count = 1) {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        if (count == 0) return;
        if (retainHandles.TryGetValue(handle.HandleId, out int prevCount)) {
            retainHandles[handle.HandleId] = prevCount + count;
        } else {
            retainHandles[handle.HandleId] = count;
            Retain();
        }
    }

    /// <summary>
    /// 释放资源句柄
    /// </summary>
    public void Release(AssetHandle handle, int count = 1) {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        if (count == 0) return;
        if (!retainHandles.TryGetValue(handle.HandleId, out int prevCount) || count > prevCount) {
            throw new ArgumentOutOfRangeException(nameof(count), $"prev: {prevCount}, count: {count}");
        }
        if (count < prevCount) {
            retainHandles[handle.HandleId] = prevCount - count;
        } else {
            retainHandles.Remove(handle.HandleId);
            Release();
        }
    }

    private void RetainBundles() {
        foreach (BundleProvider upstreamBundle in upstreamBundles) {
            upstreamBundle.Retain();
        }
        bundleProvider.Retain();
    }

    private void ReleaseBundles() {
        bundleProvider.Release();
        foreach (BundleProvider upstreamBundle in upstreamBundles) {
            upstreamBundle.Release();
        }
    }

    public override void Destroy() {
        if (IsDestroyed) return;
        IsDestroyed = true;
        ReleaseBundles();
    }

    #endregion
}
}