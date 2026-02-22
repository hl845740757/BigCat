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

using UnityEngine;
using Wjybxx.BTree;
using Wjybxx.Commons;

namespace Wjybxx.BigCat.Assetor
{
/// <summary>
/// 原始文件内容提供者
/// </summary>
public class BinaryAssetProvider : AssetProviderBase
{
    public BinaryAssetProvider(ResourceManager resourceMgr, ProviderId pid,
                               AssetFileInfo assetInfo, BundleProvider bundleProvider)
        : base(resourceMgr, pid, assetInfo, bundleProvider) {
    }

    protected override void Enter(int reentryId) {
        promise.status = ELoadStatus.Loading;
    }

    public override void Destroy() {
        base.Destroy();
        if (promise.result is ScriptableObject so) {
            Object.Destroy(so);
        }
    }

    protected override void Execute() {
        if (blackboard.isWaitForCompletion) {
            Scheduler.WaitForCompletion(bundleProvider, blackboard.deadline);
        } else {
            if (!bundleProvider.IsCompleted) {
                return;
            }
        }
        if (bundleProvider.IsFailedOrCancelled) {
            promise.errorCode = ResourceErrorCode.BundleLoadFailed;
            SetFailed(TaskStatus.ERROR);
            return;
        }
        IAssetBundle assetBundle = bundleProvider.assetBundle;
        object result = pid.loadMethod switch
        {
            ELoadMethod.LoadBinaryAsset => assetBundle.LoadBinaryAsset(assetPath),
            ELoadMethod.LoadAllBinaryAssets => assetBundle.LoadAllBinaryAssets(),
            _ => throw new AssertionError(pid.ToString())
        };
        // 资产文件不存在 - 通常不应该发生
        if (result == null) {
            promise.errorCode = ResourceErrorCode.AssetFileNotFound;
            SetFailed(TaskStatus.ERROR);
            return;
        }
        // 图片音频压缩包等在底层解压，并缓存在资源管理层
        if (result is BinaryAsset binAsset
            && assetType.IsSubclassOf(typeof(ScriptableObject))
            && assetType.IsSubclassOf(typeof(IBinaryAssetReceiver))) {
            // TODO 考虑支持异步解压？
            ScriptableObject so = ScriptableObject.CreateInstance(assetType);
            IBinaryAssetReceiver receiver = (IBinaryAssetReceiver)so;
            receiver.Unpack(binAsset);
            result = so;
        }
        promise.result = result;
        SetSuccess();
    }
}
}