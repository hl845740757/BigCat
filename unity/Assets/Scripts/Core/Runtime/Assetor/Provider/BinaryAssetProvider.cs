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

using System.Collections.Generic;
using Wjybxx.Commons;

namespace Wjybxx.BigCat.Assetor
{
/// <summary>
/// 原始文件内容提供者
/// </summary>
public class BinaryAssetProvider : AssetProviderBase
{
    public BinaryAssetProvider(ResourceManager resourceMgr, ProviderId pid,
                               AssetFileInfo assetInfo,
                               BundleProvider bundleProvider,
                               List<BundleProvider> upstreamBundles)
        : base(resourceMgr, pid, assetInfo, bundleProvider, upstreamBundles) {
    }

    protected override void Enter(int reentryId) {
        promise.phase = ELoadPhase.Loading;
    }

    protected override void Execute() {
        if (blackboard.isWaitForCompletion) {
            WaitForBundleCompletion();
        } else {
            if (!IsBundleLoadCompleted()) {
                return;
            }
        }
        if (IsBundleLoadFailed()) {
            SetFailed((int)ResourceErrorCode.BundleLoadFailed);
            return;
        }
        IAssetBundle assetBundle = bundleProvider.assetBundle;
        object result = pid.loadMethod switch
        {
            ELoadMethod.LoadBinaryAsset => assetBundle.LoadBinaryAsset(assetPath),
            ELoadMethod.LoadAllBinaryAssets => assetBundle.LoadAllBinaryAssets(),
            _ => throw new AssertionError(pid.ToString())
        };
        promise.result = result;
        SetSuccess();
    }
}
}