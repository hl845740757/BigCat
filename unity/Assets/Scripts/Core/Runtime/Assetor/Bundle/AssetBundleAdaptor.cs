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
using UnityEngine;
using Wjybxx.BigCat.Assetor.Tasks;

namespace Wjybxx.BigCat.Assetor
{
/// <summary>
/// UnityBundle适配器
/// </summary>
public sealed class AssetBundleAdaptor : IAssetBundle
{
    private readonly AssetBundleInfo _bundleInfo;
    private readonly AssetBundle _bundle;
    private readonly TaskScheduler _scheduler;
    private Action<AssetBundleAdaptor, bool> _unloadCallback;

    public AssetBundleAdaptor(AssetBundleInfo bundleInfo, AssetBundle bundle, TaskScheduler scheduler) {
        _bundle = bundle;
        _scheduler = scheduler;
        _bundleInfo = bundleInfo;
    }

    public AssetBundleInfo BundleInfo => _bundleInfo;
    public AssetBundle AssetBundle => _bundle;
    /// <summary>
    /// 卸载回调(用于解除对BundleManager的依赖)
    /// </summary>
    public Action<AssetBundleAdaptor, bool> UnloadCallback {
        get => _unloadCallback;
        set => _unloadCallback = value;
    }

    public void UnloadBundle(bool unloadAllLoadedObjects) {
        _bundle.Unload(unloadAllLoadedObjects);
        _unloadCallback?.Invoke(this, unloadAllLoadedObjects);
    }

    public ResourceTask LoadAssetAsync(string assetPath, Type assetType) {
        AssetLoadTask task = new AssetLoadTask(_bundle, assetPath, assetType, ELoadMethod.LoadAsset);
        _scheduler.AddChild(task);
        return task;
    }

    public ResourceTask LoadAssetWithSubAssetsAsync(string assetPath, Type assetType) {
        AssetLoadTask task = new AssetLoadTask(_bundle, assetPath, assetType, ELoadMethod.LoadAssetWithSubAssets);
        _scheduler.AddChild(task);
        return task;
    }

    public ResourceTask LoadAllAssetsAsync(Type assetType) {
        AssetLoadTask task = new AssetLoadTask(_bundle, "", assetType, ELoadMethod.LoadAllAssets);
        _scheduler.AddChild(task);
        return task;
    }

    public BinaryAsset LoadBinaryAsset(string assetPath) {
        return null;
    }

    public IReadOnlyList<BinaryAsset> LoadAllBinaryAssets() {
        return Array.Empty<BinaryAsset>();
    }
}
}