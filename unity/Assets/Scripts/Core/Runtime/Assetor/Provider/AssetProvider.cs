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
using Wjybxx.Commons;

namespace Wjybxx.BigCat.Assetor
{
/// <summary>
/// 普通资产对象提供者
/// </summary>
public class AssetProvider : AssetProviderBase
{
    private ResourceTask _loadAssetTask;

    public AssetProvider(ResourceManager resourceMgr, ProviderId pid,
                         AssetFileInfo assetInfo, BundleProvider bundleProvider)
        : base(resourceMgr, pid, assetInfo, bundleProvider) {
    }

    public override bool CanDestroy() {
        return base.CanDestroy()
               && (_loadAssetTask == null || _loadAssetTask.IsCompleted);
    }

    protected override void Enter(int reentryId) {
        promise.phase = ELoadPhase.Loading; // 由于Bundle可能处于不同的状态，不能细化状态和进度
    }

    protected override void Execute() {
        if (blackboard.isWaitForCompletion) {
            Scheduler.WaitForCompletion(bundleProvider, blackboard.stopwatch, blackboard.deadline);
        } else {
            if (!bundleProvider.IsCompleted) {
                return;
            }
        }
        ResourceTask loadAssetTask = _loadAssetTask;
        if (loadAssetTask == null) {
            // 检测Bundle加载结果
            if (bundleProvider.IsFailedOrCancelled) {
                SetFailed((int)ResourceErrorCode.BundleLoadFailed);
                return;
            }
            // 发起加载请求
            IAssetBundle assetBundle = bundleProvider.assetBundle;
            loadAssetTask = _loadAssetTask = pid.loadMethod switch
            {
                ELoadMethod.LoadAsset => assetBundle.LoadAssetAsync(assetPath, assetType),
                ELoadMethod.LoadAssetWithSubAssets => assetBundle.LoadAssetWithSubAssetsAsync(assetPath, assetType),
                ELoadMethod.LoadAllAssets => assetBundle.LoadAllAssetsAsync(assetType),
                _ => throw new AssertionError(pid.ToString())
            };
            // 资源类型不匹配 -- LoadAll可能返回空数组
            if (loadAssetTask == null) {
                promise.result = null;
                SetSuccess();
                return;
            }
        }
        if (blackboard.isWaitForCompletion) {
            Scheduler.WaitForCompletion(loadAssetTask, blackboard.stopwatch, blackboard.deadline);
        } else {
            promise.SyncProgressFrom(loadAssetTask.promise);
        }
        //
        if (loadAssetTask.IsCompleted) {
            promise.result = _loadAssetTask.promise.result;
            SetCompleted(_loadAssetTask.Status, true);
        }
    }
}
}