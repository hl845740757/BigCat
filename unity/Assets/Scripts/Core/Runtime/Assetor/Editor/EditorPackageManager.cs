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
using System.IO;
using Wjybxx.BigCat.Assetor.Tasks;
using Wjybxx.Dson;
using Wjybxx.Dson.IO;

namespace Wjybxx.BigCat.Assetor
{
public class EditorPackageManager : IPackageManager
{
    private readonly TaskScheduler _scheduler;
    private readonly string _packageName;
    private readonly string _packagePath;

    public EditorPackageManager(TaskScheduler scheduler,
                                string packageName,
                                string packagePath) {
        _scheduler = scheduler;
        _packageName = packageName;
        _packagePath = packagePath;
    }

    public string PackageName => _packageName;
    public string LocalVersion => "0.0.0";
    public string RemoteVersion => "0.0.0";
    public AssetPackageInfo PackageInfo { get; private set; }

    public ResourceTask Start() {
        throw new System.NotImplementedException();
    }

    public ResourceTask Stop() {
        throw new System.NotImplementedException();
    }

    public ResourceTask DownloadVersionFileAsync() {
        throw new System.NotImplementedException();
    }

    public ResourceTask DownloadPackageInfoAsync() {
        throw new NotImplementedException();
    }

    public ResourceTask LoadPackageInfoAsync() {
        // 编辑器下直接同步加载
        byte[] bytes = File.ReadAllBytes(_packagePath);
        using DsonBinaryReader<int> reader = new DsonBinaryReader<int>(DsonReaderSettings.Default, DsonInputs.NewInstance(bytes));
        PackageInfo = new AssetPackageInfo();
        PackageInfo.Deserialize(reader);
        //
        CompletedTask task = new CompletedTask();
        _scheduler.AddChild(task);
        return task;
    }

    public ResourceTask BuildCacheInfoAsync() {
        CompletedTask task = new CompletedTask();
        _scheduler.AddChild(task);
        return task;
    }

    public List<AssetBundleInfo> GetNeedDownloadBundles(List<AssetBundleInfo> result = null) {
        return result ?? new List<AssetBundleInfo>();
    }

    public ResourceTask ClearCacheFilesAsync() {
        CompletedTask task = new CompletedTask();
        _scheduler.AddChild(task);
        return task;
    }

    public bool HasImported(AssetBundleInfo bundleInfo) {
        return true;
    }

    public bool NeedDownload(AssetBundleInfo bundleInfo) {
        return false;
    }

    public DownloadTask DownloadBundleAsync(AssetBundleInfo bundleInfo) {
        throw new System.NotImplementedException();
    }

    public IReadOnlyList<DownloadTask> GetDownloadTasks() {
        return Array.Empty<DownloadTask>();
    }

    public bool NeedImport(AssetBundleInfo bundleInfo) {
        return false;
    }

    public ResourceTask ImportBundleAsync(AssetBundleInfo bundleInfo) {
        throw new System.NotImplementedException();
    }

    public IReadOnlyList<ResourceTask> GetImportTasks() {
        return Array.Empty<ResourceTask>();
    }
}
}