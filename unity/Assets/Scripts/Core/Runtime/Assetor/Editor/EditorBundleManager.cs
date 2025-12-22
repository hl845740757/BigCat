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
using Wjybxx.BigCat.Assetor.Tasks;

namespace Wjybxx.BigCat.Assetor
{
/// <summary>
///
/// 注：编辑器下不验证Bundle文件的CRC等信息，总是可以加载；因此如果混合其它Bundle管理器使用，
/// 则应当将其它管理器放在加载顺序的前面。
/// </summary>
public class EditorBundleManager : IBundleManager
{
    private readonly TaskScheduler _scheduler;
    private readonly Dictionary<string, EditorAssetBundle> _loadedBundles = new(100);

    public EditorBundleManager(TaskScheduler scheduler) {
        _scheduler = scheduler;
    }

    public ResourceTask Start() {
        CompletedTask task = new CompletedTask();
        _scheduler.AddChild(task);
        return task;
    }

    public ResourceTask Stop() {
        CompletedTask task = new CompletedTask();
        _scheduler.AddChild(task);
        return task;
    }

    public bool Exists(AssetBundleInfo bundleInfo) {
        return true;
    }

    public IAssetBundle LoadBundle(AssetBundleInfo bundleInfo) {
        if (!_loadedBundles.TryGetValue(bundleInfo.bundleName, out EditorAssetBundle bundle)) {
            bundle = new EditorAssetBundle(bundleInfo, _scheduler);
            bundle.UnloadCallback = e => _loadedBundles.Remove(e.BundleInfo.bundleName);
            _loadedBundles.Add(bundleInfo.bundleName, bundle);
        }
        return bundle;
    }

    public ResourceTask LoadBundleAsync(AssetBundleInfo bundleInfo) {
        IAssetBundle assetBundle = LoadBundle(bundleInfo);
        // 返回的异步任务是延迟成功的
        CompletedTask task = new CompletedTask(assetBundle);
        _scheduler.AddChild(task);
        return task;
    }
}
}