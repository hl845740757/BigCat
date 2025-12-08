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
using Wjybxx.Commons.Collections;

namespace Wjybxx.BigCat.Assetor
{
/// <summary>
/// 资产清单
/// </summary>
[Serializable]
public sealed class AssetManifest
{
    /// <summary>
    /// 资源包名称
    /// </summary>
    public string packageName;
    /// <summary>
    /// 资源包版本
    /// </summary>
    public string packageVersion;
    /// <summary>
    /// 构建时间
    /// 注：构建时间代替的是构建版本，不能用于判断是否需要更新。
    /// </summary>
    public string buildTime;

    /// <summary>
    /// 所有的Bundle信息
    /// </summary>
    public List<AssetBundleInfo> bundleList = new List<AssetBundleInfo>();
    /// <summary>
    /// BundleId到BundleInfo的映射(查询缓存)
    /// </summary>
    [NonSerialized]
    public readonly LinkedDictionary<int, AssetBundleInfo> id2BundleDic = new();
    /// <summary>
    /// BundleName到BundleInfo的映射(查询缓存)
    /// </summary>
    [NonSerialized]
    public readonly LinkedDictionary<string, AssetBundleInfo> name2BundleDic = new();
    /// <summary>
    /// 主资产文件数量
    /// </summary>
    [NonSerialized]
    public int mainAssetsCount;

    /// <summary>
    /// 清理缓存数据
    /// </summary>
    public void ClearCache() {
        mainAssetsCount = 0;
        id2BundleDic.Clear();
        name2BundleDic.Clear();
        foreach (AssetBundleInfo bundleInfo in bundleList) {
            bundleInfo.downstreamBundles.Clear();
        }
    }

    /// <summary>
    /// 构建缓存数据
    /// </summary>
    public void BuildCache() {
        ClearCache();
        id2BundleDic.EnsureCapacity(bundleList.Count);
        name2BundleDic.EnsureCapacity(bundleList.Count);
        // 先建立基础缓存 - 重复应当在打包就检测出来
        foreach (AssetBundleInfo bundleInfo in bundleList) {
            bundleInfo.packageName = packageName;
            id2BundleDic.Add(bundleInfo.bundleId, bundleInfo);
            name2BundleDic.Add(bundleInfo.bundleName, bundleInfo);
            //
            mainAssetsCount += bundleInfo.mainAssets.Count;
            foreach (AssetFileInfo fileInfo in bundleInfo.mainAssets) {
                fileInfo.packageName = packageName;
                fileInfo.bundleId = bundleInfo.bundleId;
            }
        }
        // 构建依赖图缓存
        foreach (AssetBundleInfo bundleInfo in bundleList) {
            foreach (int upstreamBundle in bundleInfo.upstreamBundles) {
                id2BundleDic[upstreamBundle].downstreamBundles.Add(bundleInfo.bundleId);
            }
        }
    }
}
}