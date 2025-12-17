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
using Wjybxx.BigCat.Assetor;
using Wjybxx.BTree;
using Wjybxx.Dson.Codec.Attributes;
using Blackboard = Wjybxx.BigCat.Util.Blackboard;

namespace Wjybxx.BigCat.Editor.Assetor.Tasks
{
/// <summary>
/// 检查资产的索引唯一性
/// </summary>
[DsonSerializable(NamespaceAliases = new string[]
{
    "BigCatUtil = Wjybxx.BigCat.Util",
})]
public class BuildIndexesTask : LeafTask<Blackboard>
{
    protected override void Execute() {
        BuildPackageInfo packageInfo = blackboard.Get(BuildKeys.packageInfo);
        Dictionary<string, BuildAssetInfo> index2AssetDic = new(packageInfo.assetDic.Count);
        foreach (BuildBundleInfo bundleInfo in packageInfo.id2BundleDic.Values) {
            if (bundleInfo.collectorType != ECollectorType.MainAsset
                && bundleInfo.collectorType != ECollectorType.RawFile) {
                continue;
            }
            EAssetIndexes indexes = bundleInfo.assetIndexes | bundleInfo.uniqueIndexes;
            if (indexes == 0) {
                continue;
            }
            foreach (BuildAssetInfo assetInfo in bundleInfo.assetList) {
                string fileName = GetSubAssetPath(assetInfo.assetPath, 0);
                if ((bundleInfo.uniqueIndexes & EAssetIndexes.FileName) != 0 && !IsNumber(fileName)) {
                    if (!index2AssetDic.TryAdd(fileName, assetInfo)) {
                        throw new Exception($"Duplicate index: {fileName}, asset: {assetInfo.assetPath}");
                    }
                }
                // 单层级目录索引可选唯一性
                if ((bundleInfo.uniqueIndexes & EAssetIndexes.FolderAndFileName) != 0) {
                    string subAssetPath = GetSubAssetPath(assetInfo.assetPath, 1);
                    if (!index2AssetDic.TryAdd(subAssetPath, assetInfo)) {
                        throw new Exception($"Duplicate index: {subAssetPath}, asset: {assetInfo.assetPath}");
                    }
                }
                // 多层级目录索引强制检查唯一性
                if ((indexes & EAssetIndexes.FolderAndFileNamePlus) != 0) {
                    string subAssetPath = GetSubAssetPath(assetInfo.assetPath, bundleInfo.indexDepth);
                    if (!index2AssetDic.TryAdd(subAssetPath, assetInfo)) {
                        throw new Exception($"Duplicate index: {subAssetPath}, asset: {assetInfo.assetPath}");
                    }
                }
                // 相对Collector的路径，也强制检查唯一性
                if ((indexes & EAssetIndexes.RelativeToCollector) != 0) {
                    string subAssetPath = assetInfo.assetPath.Substring(bundleInfo.collectPath.Length + 1);
                    if (!index2AssetDic.TryAdd(subAssetPath, assetInfo)) {
                        throw new Exception($"Duplicate index: {subAssetPath}, asset: {assetInfo.assetPath}");
                    }
                }
            }
        }
        SetSuccess();
    }

    protected override void OnEventImpl(object eventObj) {
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
}
}