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
using System.Linq;
using System.Text;
using UnityEngine;
using Wjybxx.BigCat.Core;
using Wjybxx.Commons.Collections;

namespace Wjybxx.BigCat.Assetor
{
/// <summary>
/// 资产查询支持(索引)
/// </summary>
public sealed class AssetQuery
{
    /// <summary>
    /// 所有资源包
    /// </summary>
    public readonly List<AssetPackageInfo> packages = new List<AssetPackageInfo>();
    /// <summary>
    /// 支持无扩展名索引的文件类型
    ///
    /// 1.当一类资产支持多种文件类型时，才需要添加；json/xml这类非Unity对象资产无需添加。
    /// 2.大小写严格，文件扩展名不会被规格化。
    /// </summary>
    public readonly HashSet<string> supportExtensions = new HashSet<string>(16)
    {
        "fbx", "unity", "prefab", "asset",
        "png", "jpg", "tif",
        "ogg", "wav", "mp3"
    };

    /// <summary>
    /// 资产索引到资产的映射
    /// 注：
    /// 1.<code>folderName/fileName</code>一定不和fileName索引重复，因此一个字典即可。
    /// 2.资产路径到资产的映射也在这里，这样查询资产时只需要查询一次。
    /// </summary>
    public readonly LinkedDictionary<string, AssetFileInfo> assetIndex2AssetDic = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>
    /// 场景名到资产文件的映射
    /// 注：由于我们需要根据Scene的名字精准查询关联的资产，因此使用额外的索引以避免冲突。
    /// </summary>
    public readonly LinkedDictionary<string, AssetFileInfo> sceneName2AssetDic = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 添加一个包
    /// </summary>
    public void AddPackage(AssetPackageInfo packageInfo) {
        if (packageInfo == null) throw new ArgumentNullException(nameof(packageInfo));
        if (!packages.Contains(packageInfo)) {
            packages.Add(packageInfo);
        }
    }

    /// <summary>
    /// 清理缓存
    /// </summary>
    /// <returns></returns>
    public void ClearCache() {
        assetIndex2AssetDic.Clear();
        sceneName2AssetDic.Clear();
    }

    /// <summary>
    /// 构建缓存内容
    /// 
    /// 1.该方法假设打包工具已对资产路径执行了规格化。
    /// 2.只有特定类型的文件才会添加无扩展名索引。
    /// </summary>
    public void BuildCache() {
        ClearCache();
        // 确保Package已构建缓存
        foreach (AssetPackageInfo package in packages) {
            if (package.id2BundleDic.IsEmpty) {
                package.BuildCache();
            }
        }
        // 减少字符串切割
        HashSet<FileExtension> supportExtensions2 = new HashSet<FileExtension>();
        foreach (string extension in supportExtensions) {
            supportExtensions2.Add(new FileExtension(extension));
        }
        //
        List<AssetBundleInfo> bundleList = GetSortedBundles(); // 保证索引稳定性
        int fileCount = GetMainAssetsCount();
        assetIndex2AssetDic.EnsureCapacity(fileCount * 3);
        sceneName2AssetDic.EnsureCapacity(50);
        foreach (AssetBundleInfo bundleInfo in bundleList) {
            foreach (AssetFileInfo fileInfo in bundleInfo.mainAssets) {
                string assetPath = fileInfo.assetPath;
                assetIndex2AssetDic.Add(assetPath, fileInfo);
                // 全路径索引无扩展名索引
                FileExtension extension = GetExtension(assetPath);
                if (supportExtensions2.Contains(extension)) {
                    string assetPathNoExt = RemoveExtension(assetPath, in extension);
                    assetIndex2AssetDic.Add(assetPathNoExt, fileInfo);
                }
                // 场景文件需要独立的索引
                if (assetPath.EndsWith(".unity")) {
                    string sceneName = Path.GetFileNameWithoutExtension(assetPath);
                    sceneName2AssetDic.Add(sceneName, fileInfo);
                }
                // 自定义索引禁止重复，程序生成的索引可以重复
                if (!string.IsNullOrEmpty(fileInfo.address)) {
                    assetIndex2AssetDic.Add(assetPath, fileInfo);
                }
                if (bundleInfo.assetIndexes == EAssetIndexes.None) {
                    continue;
                }
                string fileName = GetSubAssetPath(assetPath, 0);
                // 文件名索引：剔除无意义name索引，主要针对图片资源：1.png
                if ((bundleInfo.assetIndexes & EAssetIndexes.FileName) != 0 && !IsNumber(fileName)) {
                    assetIndex2AssetDic[fileName] = fileInfo;
                    //
                    if (supportExtensions2.Contains(extension)) {
                        string fileNameNoExt = RemoveExtension(fileName, in extension);
                        assetIndex2AssetDic[fileNameNoExt] = fileInfo;
                    }
                }
                // 文件夹索引：允许图片资源同文件夹建立索引：sm_8001/1.png
                if ((bundleInfo.assetIndexes & EAssetIndexes.FolderAndFileName) != 0) {
                    string subAssetPath = GetSubAssetPath(assetPath, 1);
                    assetIndex2AssetDic[subAssetPath] = fileInfo;
                    //
                    if (supportExtensions2.Contains(extension)) {
                        string subAssetPathNoExt = RemoveExtension(subAssetPath, in extension);
                        assetIndex2AssetDic[subAssetPathNoExt] = fileInfo;
                    }
                }
                // 自定义深度索引：需要唯一性（打包时）
                if ((bundleInfo.assetIndexes & EAssetIndexes.FolderAndFileNamePlus) != 0) {
                    string subAssetPath = GetSubAssetPath(assetPath, bundleInfo.indexDepth);
                    assetIndex2AssetDic[subAssetPath] = fileInfo;
                    //
                    if (supportExtensions2.Contains(extension)) {
                        string subAssetPathNoExt = RemoveExtension(subAssetPath, in extension);
                        assetIndex2AssetDic[subAssetPathNoExt] = fileInfo;
                    }
                }
                // 相对收集器的路径索引：需要唯一性（打包时）
                if ((bundleInfo.assetIndexes & EAssetIndexes.RelativeToCollector) != 0
                    && !string.IsNullOrEmpty(bundleInfo.collectPath)) {
                    string relativePath = fileInfo.assetPath.Substring(bundleInfo.collectPath.Length + 1);
                    assetIndex2AssetDic[relativePath] = fileInfo;
                    //
                    if (supportExtensions2.Contains(extension)) {
                        string relativePathNoExt = RemoveExtension(relativePath, in extension);
                        assetIndex2AssetDic[relativePathNoExt] = fileInfo;
                    }
                }
            }
        }
    }

    private static bool IsNumber(string fileName) {
        int index = fileName.LastIndexOf('.');
        if (index < 0) {
            return int.TryParse(fileName, out _);
        }
        return int.TryParse(fileName.AsSpan(0, index), out _);
    }

    private static string GetSubAssetPath(string assetPath, int depth) {
        int index = assetPath.LastIndexOf('/');
        int count = 0;
        while (count < depth) {
            index = assetPath.LastIndexOf('/', index - 1);
            if (index < 0) {
                throw new InvalidOperationException($"assetPath: {assetPath}, depth: {depth}");
            }
            count++;
        }
        return assetPath.Substring(index + 1);
    }

    private static string RemoveExtension(string path, in FileExtension extension) {
        return extension.IsEmpty ? path : path.Substring(0, path.Length - extension.Length - 1);
    }

    private static FileExtension GetExtension(string path) {
        int index = path.LastIndexOf('.');
        if (index >= 0 && index > path.LastIndexOf('/')) {
            return new FileExtension(path.AsSpan(index + 1));
        }
        return default;
    }

    /// <summary>
    /// 将Bundle按照打包路径排序
    /// </summary>
    /// <returns></returns>
    private List<AssetBundleInfo> GetSortedBundles() {
        List<AssetBundleInfo> result = new(GetBundleCount());
        foreach (AssetBundleInfo bundleInfo in packages.SelectMany(e => e.bundleList)) {
            result.Add(bundleInfo);
        }
        result.Sort((a, b) => string.Compare(a.assetPath, b.assetPath, StringComparison.Ordinal));
        return result;
    }

    internal bool HasCache => assetIndex2AssetDic.Count > 0;

    /// <summary>
    /// 获取Package数量
    /// </summary>
    /// <returns></returns>
    public int GetPackageCount() => packages.Count;

    /// <summary>
    /// 获取总Bundle数量
    /// </summary>
    /// <returns></returns>
    public int GetBundleCount() {
        int count = 0;
        foreach (AssetPackageInfo package in packages) {
            count += package.bundleList.Count;
        }
        return count;
    }

    /// <summary>
    /// 获取主资产文件数量
    /// </summary>
    /// <returns></returns>
    public int GetMainAssetsCount() {
        int count = 0;
        foreach (AssetPackageInfo package in packages) {
            count += package.mainAssetsCount;
        }
        return count;
    }

    /// <summary>
    /// 查找指定包裹
    /// </summary>
    public AssetPackageInfo FindPackage(string packageName) {
        // Package通常数量较少，迭代查询效率足够
        for (int index = 0; index < packages.Count; index++) {
            AssetPackageInfo package = packages[index];
            if (package.packageName == packageName) return package;
        }
        return null;
    }

    /// <summary>
    /// 查找指定Bundle
    /// </summary>
    public AssetBundleInfo FindBundle(string packageName, int bundleId) {
        AssetPackageInfo package = FindPackage(packageName);
        package.id2BundleDic.TryGetValue(bundleId, out AssetBundleInfo bundleInfo);
        return bundleInfo;
    }
}
}