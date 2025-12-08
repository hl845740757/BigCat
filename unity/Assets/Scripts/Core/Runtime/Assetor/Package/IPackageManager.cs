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

namespace Wjybxx.BigCat.Assetor
{
/// <summary>
/// 资源包管理器
///
/// 1.该管理器为独立服务，可以不依赖其它服务运行。
/// 2.更新程序只负责下载和解压等基础逻辑，如果Bundle是加密文件，需要由<see cref="IBundleManager"/>处理。
/// </summary>
public interface IPackageManager
{
    /// <summary>
    /// 资源包名
    /// </summary>
    string PackageName { get; }
    /// <summary>
    /// 本地资源包版本
    /// 注；启动时初始化，<see cref="LoadManifestAsync"/>后纠正。
    /// </summary>
    string LocalVersion { get; }
    /// <summary>
    /// 远程资源包版本
    /// 注：只保留最后一次的下载结果。
    /// </summary>
    string RemoteVersion { get; }
    /// <summary>
    /// 该资源包的清单
    /// 注：<see cref="LoadManifestAsync"/>成功执行以后可访问。
    /// </summary>
    AssetManifest Manifest { get; }

    /// <summary>
    /// 启用程序
    /// </summary>
    /// <returns></returns>
    ResourceTask Start();

    /// <summary>
    /// 停止程序
    /// </summary>
    ResourceTask Stop();

    /// <summary>
    /// 下载版本文件
    /// </summary>
    /// <returns></returns>
    ResourceTask DownloadVersionFileAsync();

    /// <summary>
    /// 下载资源清单文件
    /// </summary>
    /// <returns></returns>
    ResourceTask DownloadManifestAsync();

    /// <summary>
    /// 加载最新的Manifest文件
    ///
    /// 注：需要支持重复调用，重复调用时覆盖旧结果。
    /// </summary>
    /// <returns></returns>
    ResourceTask LoadManifestAsync();

    /// <summary>
    /// 根据最新的Manifest构建Bundle缓存信息
    ///
    /// 注：需要支持重复调用，重复调用时覆盖旧结果。
    /// </summary>
    /// <returns></returns>
    ResourceTask BuildCacheInfoAsync();

    /// <summary>
    /// 获取当前需要下载的Bundle信息
    ///
    /// 注：
    /// 1.下载中但尚未完成的Bundle也在这里。
    /// 2.该方法基于<see cref="BuildCacheInfoAsync"/>构建的信息查询。
    /// </summary>
    /// <param name="result">接收结果的List</param>
    /// <returns></returns>
    List<AssetBundleInfo> GetNeedDownloadBundles(List<AssetBundleInfo> result = null);

    /// <summary>
    /// 清理缓存文件
    ///
    /// 注：
    /// 1.主要清理不再使用的bundle文件；如果项目支持预下载，还需要小心处理预下载资源。
    /// 2.可能需要扫描多个Bundle目录。
    /// </summary>
    ResourceTask ClearCacheFilesAsync();

    /// <summary>
    /// Bundle是否已导入到<see cref="IBundleManager"/>的加载目录。
    ///
    /// 注：
    /// 1.只有Bundle可立即加载时返回true，已下载但未导入的Bundle返回false
    /// 2.需要校验文件名和crc校验码
    /// </summary>
    bool HasImported(AssetBundleInfo bundleInfo);

    /// <summary>
    /// 是否需要进行下载
    /// </summary>
    bool NeedDownload(AssetBundleInfo bundleInfo);

    /// <summary>
    /// 下载指定的Bundle
    ///
    /// 注：如果存在进行中的任务，则返回进行中的任务；如果任务已完成（含取消），则会创建新任务。
    /// </summary>
    DownloadTask DownloadBundleAsync(AssetBundleInfo bundleInfo);

    /// <summary> 
    /// 获取当前进行的下载任务
    ///
    /// 注：可通过Promise获取进度信息，避免通过事件同步 -- 越高频率的事件价值越低。
    /// </summary>
    /// <returns></returns>
    IReadOnlyList<DownloadTask> GetDownloadTasks();

    /// <summary>
    /// 是否已下载但尚未导入到加载目录
    /// </summary>
    bool NeedImport(AssetBundleInfo bundleInfo);

    /// <summary>
    /// 导入Bundle
    ///
    /// 注：如果存在进行中的任务，则返回进行中的任务；如果任务已完成（含取消），则会创建新任务。
    /// </summary>
    ResourceTask ImportBundleAsync(AssetBundleInfo bundleInfo);

    /// <summary>
    /// 获取当前进行的导入任务
    /// </summary>
    /// <returns></returns>
    IReadOnlyList<ResourceTask> GetImportTasks();
}
}