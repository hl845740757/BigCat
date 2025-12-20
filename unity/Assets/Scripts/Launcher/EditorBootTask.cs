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

using Wjybxx.BigCat.Assetor.Tasks;
using Wjybxx.Commons.Concurrent;

namespace Wjybxx.BigCat.Assetor
{
/// <summary>
/// Editor模式下的自动启动任务
///
/// 1.可以支持自定打包
/// 2.
/// </summary>
public class EditorBootTask : ResourceTask
{
    private readonly ResourceManager resourceMgr;
    public string packageName = "Default";
    public string manifestPath = "Assets/Resources/manifest.bin";

    public EditorBootTask(ResourceManager resourceMgr) {
        this.resourceMgr = resourceMgr;
    }

    protected override void Enter(int reentryId) {
        ValueFuture future = StartManagers();
        future.GetAwaiter().OnCompleted(() => {
            if (future.Status == TaskStatus.Success) {
                SetSuccess();
            } else {
                SetFailed(BTree.TaskStatus.ERROR);
            }
        });
    }

    private async ValueFuture StartManagers() {
        TaskScheduler scheduler = Scheduler;
        EditorPackageManager packageManager = new EditorPackageManager(scheduler, packageName, manifestPath);
        EditorBundleManager bundleManager = new EditorBundleManager(scheduler);
        resourceMgr.AddBundleManager(bundleManager);
        resourceMgr.AddPackageManager(packageManager);

        StartManagerTask startManagerTask = new StartManagerTask(resourceMgr.PackageManagers, resourceMgr.BundleManagers);
        scheduler.AddChild(startManagerTask);
        await startManagerTask.Future;
        //
        await packageManager.LoadPackageInfoAsync().Future;
        await packageManager.BuildCacheInfoAsync().Future;
        // 初始化完毕以后，构建查询缓存
        resourceMgr.BuildQuery();
    }

    protected override void Execute() {
    }
}
}