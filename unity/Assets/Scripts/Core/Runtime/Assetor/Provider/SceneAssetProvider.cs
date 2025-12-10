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

namespace Wjybxx.BigCat.Assetor
{
/// <summary>
/// Scene资产提供者
/// 
/// 注：
/// 1.该Provider只用于确保Scene关联的Bundle已加载，和维护关联Bundle的引用计数。
/// 2.该Provider的引用计数由ResourceMgr负责增减。
/// </summary>
public class SceneAssetProvider : AssetProviderBase
{
    public SceneAssetProvider(ResourceManager resourceMgr, ProviderId pid,
                              AssetFileInfo assetInfo, BundleProvider bundleProvider)
        : base(resourceMgr, pid, assetInfo, bundleProvider) {
    }

    protected override void Enter(int reentryId) {
        promise.phase = ELoadPhase.Loading;
    }

    protected override void Execute() {
        if (blackboard.isWaitForCompletion) {
            Scheduler.WaitForCompletion(bundleProvider, blackboard.stopwatch, blackboard.deadline);
        } else {
            if (!bundleProvider.IsCompleted) {
                return;
            }
        }
        if (bundleProvider.IsFailedOrCancelled) {
            SetFailed((int)ResourceErrorCode.BundleLoadFailed);
        } else {
            SetSuccess();
        }
    }
}
}