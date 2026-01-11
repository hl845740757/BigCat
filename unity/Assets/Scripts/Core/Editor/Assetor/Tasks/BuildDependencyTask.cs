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
using System.Linq;
using UnityEditor;
using Wjybxx.BigCat.Util;
using Wjybxx.BTree;
using Wjybxx.Commons.Collections;
using Wjybxx.Dson.Codec.Attributes;
using Object = UnityEngine.Object;

namespace Wjybxx.BigCat.Editor.Assetor.Tasks
{
/// <summary>
/// 构建资产之间的依赖图
///
/// 1.根据文件之间的依赖计算Bundle之间的依赖
/// 2.如果文件关联的依赖没有在Package中，则抛出异常
/// 3.剔除未被引用的依赖资源
///
/// 注意：该任务通常应当为构建管线的第一个任务，后续的任务都依赖已构建好的依赖图。
/// </summary>
[DsonSerializable(NamespaceAliases = new string[]
{
    "BigCatUtil = Wjybxx.BigCat.Util",
})]
public class BuildDependencyTask : LeafTask<Blackboard>
{
    /// <summary>
    /// 是否自动忽略第三方程序集
    /// </summary>
    public bool autoIgnoreLibrary = true;

    protected override void Execute() {
        DependencyCache dependencyCache = blackboard.Get(BuildKeys.dependencyCache);
        BuildPackageInfo packageInfo = blackboard.Get(BuildKeys.packageInfo);
        // 可被剔除的资源，其依赖项不在打包范围内是安全的，因此需要先计算依赖
        foreach (BuildBundleInfo bundleInfo in packageInfo.name2BundleDic.Values) {
            if (bundleInfo.collectorType != ECollectorType.MainAsset
                && bundleInfo.collectorType != ECollectorType.DependBundle) {
                continue;
            }
            foreach (BuildAssetInfo assetInfo in bundleInfo.assetList) {
                foreach (string dependPath in dependencyCache.GetDependencies(assetInfo.assetPath)) {
                    if (packageInfo.assetDic.TryGetValue(dependPath, out BuildAssetInfo dependAssetInfo)) {
                        dependAssetInfo.hasDownstreamAssets = true;
                    }
                }
            }
        }
        // 剔除未被引用的资产 - 空Bundle在最终打包时跳过
        foreach (BuildBundleInfo bundleInfo in packageInfo.name2BundleDic.Values) {
            if (bundleInfo.collectorType != ECollectorType.DependAsset) {
                continue;
            }
            for (int index = bundleInfo.assetList.Count - 1; index >= 0; index--) {
                BuildAssetInfo assetInfo = bundleInfo.assetList[index];
                if (!assetInfo.hasDownstreamAssets) {
                    assetInfo.bundleInfo = null;
                    bundleInfo.assetList.RemoveAt(index);
                }
            }
        }
        // 构建Bundle之间依赖 - 理应和Unity构建管线计算的结果一致
        foreach (BuildBundleInfo bundleInfo in packageInfo.name2BundleDic.Values) {
            foreach (string dependPath in bundleInfo.assetList
                         .SelectMany(assetInfo => dependencyCache.GetDependencies(assetInfo.assetPath))) {
                //
                if (!packageInfo.assetDic.TryGetValue(dependPath, out BuildAssetInfo dependAssetInfo)) {
                    if (autoIgnoreLibrary && dependPath.StartsWith("Packages/")) {
                        continue; // 自动忽略第三方程序集
                    }
                    Object dependAsset = AssetDatabase.LoadMainAssetAtPath(dependPath);
                    if (dependAsset && dependAsset.GetType().Namespace == "UnityEditor") {
                        continue; // 自动跳过编辑器资产(如代码脚本)
                    }
                    throw new Exception($"The dependent asset: {dependPath} is missing");
                }
                BuildBundleInfo dependBundle = dependAssetInfo.bundleInfo;
                if (dependBundle == bundleInfo || dependBundle.provided) {
                    continue;
                }
                if (bundleInfo.upstreamBundles.Add(dependBundle.bundleId)) {
                    bundleInfo.upstreamBundleNames.Add(dependBundle.bundleName);
                }
            }
        }
        SetSuccess();
    }

    protected override void OnEventImpl(object eventObj) {
    }
}
}