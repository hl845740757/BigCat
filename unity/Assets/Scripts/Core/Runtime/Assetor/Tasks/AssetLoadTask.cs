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
using UnityEngine;
using Wjybxx.Commons;

namespace Wjybxx.BigCat.Assetor.Tasks
{
public class AssetLoadTask : ResourceTask
{
    private readonly AssetBundle _bundle;
    private readonly string _assetPath;
    private readonly Type _assetType;
    private readonly ELoadMethod _loadMethod;
    private AssetBundleRequest _request;

    public AssetLoadTask(AssetBundle bundle, string assetPath, Type assetType, ELoadMethod loadMethod) {
        _bundle = bundle;
        _assetPath = assetPath;
        _assetType = assetType;
        _loadMethod = loadMethod;
    }

    protected override void Execute() {
        // 异步任务进行期间，发起同步请求，前面的异步任务也会完成
        if (blackboard.isWaitForCompletion) {
            object result = _loadMethod switch
            {
                ELoadMethod.LoadAsset => _bundle.LoadAsset(_assetPath, _assetType),
                ELoadMethod.LoadAssetWithSubAssets => _bundle.LoadAssetWithSubAssets(_assetPath, _assetType),
                ELoadMethod.LoadAllAssets => _bundle.LoadAllAssets(_assetType),
                _ => throw new AssertionError()
            };
            promise.progress = 1f;
            promise.result = result;
            SetSuccess();
            return;
        }
        AssetBundleRequest request = _request;
        if (request == null) {
            promise.status = ELoadStatus.Loading;
            request = _request = _loadMethod switch
            {
                ELoadMethod.LoadAsset => _bundle.LoadAssetAsync(_assetPath, _assetType),
                ELoadMethod.LoadAssetWithSubAssets => _bundle.LoadAssetWithSubAssetsAsync(_assetPath, _assetType),
                ELoadMethod.LoadAllAssets => _bundle.LoadAllAssetsAsync(_assetType),
                _ => throw new AssertionError()
            };
        }
        promise.progress = request.progress;
        if (request.isDone) {
            promise.progress = 1f;
            promise.result = _loadMethod == ELoadMethod.LoadAsset
                ? request.asset
                : request.allAssets;
            SetSuccess();
        }
    }
}
}