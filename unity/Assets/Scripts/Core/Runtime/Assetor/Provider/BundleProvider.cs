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
using Wjybxx.Commons.Concurrent;

namespace Wjybxx.BigCat.Assetor
{
/// <summary>
/// Bundle提供者
///
/// 注：
/// 1.资产对象提供者都通过该Provider获取Bundle。
/// 2.由于只是简单的引用计数，互相引用的资源包无法卸载。
/// </summary>
public class BundleProvider : Provider
{
    public readonly AssetBundleInfo bundleInfo;
    public readonly List<BundleProvider> upstreamBundles;
    private DownloadTask _downloadTask;
    private ResourceTask _importTask;
    private ResourceTask _loadTask;

    public BundleProvider(ResourceManager resourceMgr, ProviderId pid,
                          AssetBundleInfo bundleInfo,
                          List<BundleProvider> upstreamBundles)
        : base(resourceMgr, pid) {
        this.bundleInfo = bundleInfo;
        this.upstreamBundles = upstreamBundles;
        RetainsUpstreamBundles();
    }

    /// <summary>
    /// 关联的Bundle
    /// </summary>
    public IAssetBundle assetBundle => (IAssetBundle)promise.result;
    private IPackageManager packageManager => resourceMgr.GetPackageManager(bundleInfo.packageInfo.packageName);

    protected override void OnPriorityChanged(int prevValue) {
        base.OnPriorityChanged(prevValue);
        EnsureUpstreamBundlesPriority();
    }

    private void EnsureUpstreamBundlesPriority() {
        for (int index = 0; index < upstreamBundles.Count; index++) {
            BundleProvider upstreamBundle = upstreamBundles[index];
            if (upstreamBundle.Priority > Priority) {
                upstreamBundle.Priority = Priority;
            }
        }
    }

    private void RetainsUpstreamBundles() {
        foreach (BundleProvider upstreamBundle in upstreamBundles) {
            upstreamBundle.Retain();
        }
    }

    private void ReleaseUpstreamBundles() {
        foreach (BundleProvider upstreamBundle in upstreamBundles) {
            upstreamBundle.Release();
        }
    }

    public override bool CanDestroy() {
        return base.CanDestroy()
               && (_loadTask == null || _loadTask.IsCompleted);
    }

    public override void Destroy() {
        if (IsDestroyed) return;
        IsDestroyed = true;
        ReleaseUpstreamBundles();
        assetBundle?.UnloadBundle(true);
        // 取消下载任务
        _downloadTask?.CancelToken.Cancel();
        _importTask?.CancelToken.Cancel();
    }

    protected override void Enter(int reentryId) {
        EnsureUpstreamBundlesPriority();
    }

    protected override void Execute() {
        if (blackboard.isWaitForCompletion) {
            ExecuteUtilComplete();
            return;
        }
        // 这里使用状态模式是不必要的，因为逻辑很简单，顺序代码更清楚
        if (promise.phase == ELoadPhase.Pending) {
            promise.phase = ELoadPhase.Downloading;
            if (packageManager.NeedDownload(bundleInfo)) {
                _downloadTask = packageManager.DownloadBundleAsync(bundleInfo);
            }
        }
        // 
        if (promise.phase == ELoadPhase.Downloading) {
            DownloadTask downloadTask = _downloadTask;
            if (downloadTask != null) {
                if (!downloadTask.IsCompleted) {
                    promise.SyncProgressFrom(downloadTask.promise);
                    return;
                }
                if (!downloadTask.IsSucceeded) {
                    SetCompleted(downloadTask.Status, true);
                    return;
                }
            }
            promise.phase = ELoadPhase.Importing;
            promise.ClearProgress();
            if (packageManager.NeedImport(bundleInfo)) {
                _importTask = packageManager.ImportBundleAsync(bundleInfo);
            }
        }
        //
        if (promise.phase == ELoadPhase.Importing) {
            ResourceTask importTask = _importTask;
            if (importTask != null) {
                if (!importTask.IsCompleted) {
                    promise.SyncProgressFrom(importTask.promise);
                    return;
                }
                if (!importTask.IsSucceeded) {
                    SetCompleted(importTask.Status, true);
                    return;
                }
            }
            promise.phase = ELoadPhase.Loading;
            promise.ClearProgress();
        }
        //
        ResourceTask loadTask = _loadTask;
        if (loadTask == null) {
            foreach (BundleProvider upstreamBundle in upstreamBundles) {
                if (!upstreamBundle.IsCompleted) {
                    return;
                }
            }
            foreach (IBundleManager bundleManager in resourceMgr.BundleManagers) {
                if ((loadTask = bundleManager.LoadBundleAsync(bundleInfo)) != null) {
                    break;
                }
            }
            if (loadTask == null) {
                SetFailed((int)ResourceErrorCode.BundleFileNotFound);
                return;
            }
            _loadTask = loadTask;
        }
        promise.SyncProgressFrom(loadTask.promise);
        if (loadTask.IsCompleted) {
            promise.result = loadTask.promise.result;
            SetCompleted(loadTask.Status, true);
        }
    }

    private void ExecuteUtilComplete() {
        // Bundle当前不可用
        if (!packageManager.HasImported(bundleInfo)) {
            throw new BlockingOperationException("Bundle has not been imported");
        }
        // 进行中的异步加载无法转同步
        ResourceTask loadTask = _loadTask;
        if (loadTask != null) {
            if (!loadTask.IsCompleted) {
                throw new BlockingOperationException("Bundle has started async loading");
            }
            promise.result = loadTask.promise.result;
            SetCompleted(loadTask.Status, true);
            return;
        }
        // 等待上游Bundle加载
        foreach (BundleProvider upstreamBundle in upstreamBundles) {
            Scheduler.WaitForCompletion(upstreamBundle, blackboard.deadline);
        }
        // 同步加载
        IAssetBundle assetBundle = null;
        foreach (IBundleManager bundleManager in resourceMgr.BundleManagers) {
            if ((assetBundle = bundleManager.LoadBundle(bundleInfo)) != null) {
                break;
            }
        }
        if (assetBundle != null) {
            promise.result = assetBundle;
            SetSuccess();
        } else {
            SetFailed((int)ResourceErrorCode.BundleFileNotFound);
        }
    }
}
}