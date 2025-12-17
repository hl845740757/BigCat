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
using System.Linq;
using Wjybxx.BigCat.Util;
using Wjybxx.BTree;
using Wjybxx.Dson.Codec.Attributes;

namespace Wjybxx.BigCat.Editor.Assetor.Tasks
{
/// <summary>
/// 构建资产之间的依赖图
///
/// 1.根据文件之间的依赖计算Bundle之间的依赖
/// 2.如果文件关联的依赖没有在Package中，则抛出异常
/// 3.剔除未被引用的依赖资源 TODO
/// </summary>
[DsonSerializable(NamespaceAliases = new string[]
{
    "BigCatUtil = Wjybxx.BigCat.Util",
})]
public class BuildDependencyTask : LeafTask<Blackboard>
{
    protected override void Execute() {
        DependencyCache dependencyCache = blackboard.Get(BuildKeys.dependencyCache);
        BuildPackageInfo packageInfo = blackboard.Get(BuildKeys.packageInfo);
        foreach (BuildBundleInfo bundleInfo in packageInfo.name2BundleDic.Values) {
            for (int index = bundleInfo.assetList.Count - 1; index >= 0; index--) {
                BuildAssetInfo assetInfo = bundleInfo.assetList[index];
                if (assetInfo.category == EAssetCategory.DependAsset) {
                    // TODO 剔除未引用的资源
                }
                foreach (string dependPath in dependencyCache.GetDependencies(assetInfo.assetPath)) {
                    if (!packageInfo.assetDic.TryGetValue(dependPath, out BuildAssetInfo dependAssetInfo)) {
                        throw new Exception($"The dependent asset: {dependPath} is missing");
                    }
                    BuildBundleInfo dependBundle = dependAssetInfo.bundleInfo;
                    if (bundleInfo.upstreamBundles.Add(dependBundle.bundleId)) {
                        bundleInfo.upstreamBundleNames.Add(dependBundle.bundleName);
                    }
                }
            }
        }
        SetSuccess();
    }

    protected override void OnEventImpl(object eventObj) {
    }
}
}