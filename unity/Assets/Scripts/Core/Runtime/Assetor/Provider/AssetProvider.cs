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
                         AssetFileInfo assetInfo,
                         BundleProvider bundleProvider,
                         List<BundleProvider> upstreamBundles)
        : base(resourceMgr, pid, assetInfo, bundleProvider, upstreamBundles) {
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
            WaitForBundleCompletion();
        } else {
            if (!IsBundleLoadCompleted()) {
                return;
            }
        }
        ResourceTask loadAssetOp = _loadAssetTask;
        if (loadAssetOp == null) {
            // 检测Bundle加载结果
            if (IsBundleLoadFailed()) {
                SetFailed((int)ResourceErrorCode.BundleLoadFailed);
                return;
            }
            // 发起加载请求
            IAssetBundle assetBundle = bundleProvider.assetBundle;
            loadAssetOp = _loadAssetTask = pid.loadMethod switch
            {
                ELoadMethod.LoadAsset => assetBundle.LoadAssetAsync(assetPath, assetType),
                ELoadMethod.LoadAssetWithSubAssets => assetBundle.LoadAssetWithSubAssetsAsync(assetPath, assetType),
                ELoadMethod.LoadAllAssets => assetBundle.LoadAllAssetsAsync(assetType),
                _ => throw new AssertionError(pid.ToString())
            };
            // 资源类型不匹配 -- LoadAll可能返回空数组
            if (loadAssetOp == null) {
                promise.result = null;
                SetSuccess();
                return;
            }
        }
        if (blackboard.isWaitForCompletion) {
            Scheduler.WaitForCompletion(loadAssetOp, blackboard.stopwatch, blackboard.deadline);
        } else {
            promise.SyncProgressFrom(loadAssetOp.promise);
        }
        //
        if (loadAssetOp.IsCompleted) {
            promise.result = _loadAssetTask.promise.result;
            SetCompleted(_loadAssetTask.Status, true);
        }
    }
}
}