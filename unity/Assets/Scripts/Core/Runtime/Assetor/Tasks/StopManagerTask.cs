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
using Wjybxx.BTree;

namespace Wjybxx.BigCat.Assetor.Tasks
{
public class StopManagerTask : ResourceTask
{
    private const ELoadPhase Pending = 0;
    private const ELoadPhase StopBundles = (ELoadPhase)1;
    private const ELoadPhase StopPackages = (ELoadPhase)2;

    private readonly IList<IPackageManager> packageManagers;
    private readonly IList<IBundleManager> bundleManagers;
    private List<ResourceTask> packageTasks;
    private List<ResourceTask> bundleTasks;

    public StopManagerTask(IList<IPackageManager> packageManagers,
                           IList<IBundleManager> bundleManagers) {
        this.packageManagers = packageManagers;
        this.bundleManagers = bundleManagers;
    }

    protected override void Execute() {
        if (promise.phase == Pending) {
            promise.phase = StopBundles;
            StopBundleManagers();
        }
        if (promise.phase == StopBundles) {
            if (!ResourceManager.IsCompleted(bundleTasks)) {
                return;
            }
            if (!ResourceManager.IsFailedOrCancelled(bundleTasks)) {
                SetFailed(TaskStatus.ERROR);
                return;
            }
            promise.phase = StopPackages;
            StopPackageManagers();
        }
        if (promise.phase == StopPackages) {
            if (!ResourceManager.IsCompleted(packageTasks)) {
                return;
            }
            if (!ResourceManager.IsFailedOrCancelled(packageTasks)) {
                SetFailed(TaskStatus.ERROR);
                return;
            }
            SetSuccess();
        }
    }

    private void StopPackageManagers() {
        packageTasks = new List<ResourceTask>(packageManagers.Count);
        foreach (IPackageManager packageManager in packageManagers) {
            packageTasks.Add(packageManager.Stop());
        }
    }

    private void StopBundleManagers() {
        bundleTasks = new List<ResourceTask>(bundleManagers.Count);
        foreach (IBundleManager bundleManager in bundleManagers) {
            packageTasks.Add(bundleManager.Stop());
        }
    }
}
}