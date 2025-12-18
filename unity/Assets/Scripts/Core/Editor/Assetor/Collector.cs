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
using UnityEditor;
using Wjybxx.BigCat.Assetor;
using Wjybxx.BTree;
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;
using Wjybxx.Dson.Codec.Attributes;
using Blackboard = Wjybxx.BigCat.Util.Blackboard;
using Object = UnityEngine.Object;

namespace Wjybxx.BigCat.Editor.Assetor
{
/// <summary>
/// 资产收集器
///
/// 注：
/// 1.收集器的职责是：扫描指定目录下的指定类别资产，然后将其分组为<see cref="BuildBundleInfo"/>。
/// 2.同一个资产目录，可以同时导出：主资产Bundle、依赖资产Bundle、原始文件Bundle三种类型的Bundle。
/// 3.收集器需要自动剔除同类子文件夹收集器，只区分Unity资产和非Unity资产，否则难以界定归属。
///
/// Q：为什么不支持指定某些扩展名类型的文件才需要建立索引？
/// A：指定需要索引的文件类型固然可以减少运行时的内存开销，但维护成本更高；在大型项目中，这类需要手动维护的配置越少越好。
/// </summary>
[DsonSerializable(NamespaceAliases = new string[]
{
    "BigCatUtil = Wjybxx.BigCat.Util",
})]
public class Collector : LeafTask<Blackboard>
{
    /// <summary>
    /// 收集器的起始目录
    /// 注：<see cref="CollectorPackage"/>会预处理该属性。
    /// </summary>
    public string collectPath;
    /// <summary>
    /// 扫描文件时是否递归
    /// </summary>
    public bool recursive = true;
    /// <summary>
    /// 收集器类型
    /// </summary>
    public ECollectorType collectorType = ECollectorType.MainAsset;

    /// <summary>
    /// 资产分组方式
    /// (通常可以固定按文件夹分组)
    /// </summary>
    public EGroupBy groupBy = EGroupBy.Directory;
    /// <summary>
    /// 分组深度
    /// </summary>
    public int groupDepth = 1;

    /// <summary>
    /// 所属的Bundle是否由外部提供(不参与最终打包)
    /// </summary>
    public bool provided;
    /// <summary>
    /// 为Bundle附加的标签
    /// </summary>
    public HashSet<string> bundleTags = new HashSet<string>();
    /// <summary>
    /// 需要为Bundle建立的索引类型
    /// </summary>
    public EAssetIndexes assetIndexes = EAssetIndexes.None;
    /// <summary>
    /// 需要为Bundle建立的唯一索引(打包期间唯一)
    /// </summary>
    public EAssetIndexes uniqueIndexes = EAssetIndexes.None;
    /// <summary>
    /// 资产索引深度(三级目录就应该实现唯一)
    /// </summary>
    public int indexDepth;

    /// <summary>
    /// 资产分类器
    ///
    /// 1.不同文件夹的逻辑不同；
    /// 2.如果没有设置，则使用Group绑定的分类器；
    /// 3.如果Group也没有绑定分类器，则根据收集器类型计算（即默认都有效）；
    /// </summary>
    [SerializeReference]
    public IAssetClassifier classifier;
    /// <summary>
    /// 收集到的所有资产
    /// </summary>
    [NonSerialized]
    public List<BuildAssetInfo> collectedAssets = new(100);
    /// <summary>
    /// bundle分组
    /// </summary>
    [NonSerialized]
    public LinkedDictionary<string, BuildBundleInfo> collectedBundles = new(10);

    protected override void BeforeEnter() {
        this.classifier ??= this.GetFirstAncestorOfType<CollectorGroup>().classifier;
    }

    protected override void Execute() {
        if (string.IsNullOrEmpty(collectPath)
            || AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(collectPath) == null) {
            SetSuccess();
            return;
        }
        CollectorPackage package = this.GetFirstAncestorOfType<CollectorPackage>();
        CollectorGroup group = this.GetFirstAncestorOfType<CollectorGroup>();
        PathCache pathCache = blackboard.Get(BuildKeys.pathCache);
        // 筛选资产
        foreach (string assetPath in blackboard.Get(BuildKeys.allAssetPaths)) {
            if (!UnityEditorUtil.IsSubPath(collectPath, assetPath)) {
                continue;
            }
            if (!recursive && assetPath.IndexOf('/', collectPath.Length) > 0) {
                continue;
            }
            // 测试Editor的效率较低，放在前面两个条件之后
            if (assetPath.Contains("/Editor/")) {
                continue;
            }
            // 剔除同类型子目录收集器的资源
            if (ContainsSubPathCollector(package, pathCache, assetPath)) {
                continue;
            }
            // 其实测试资产的类别一定程度上包含了Ignore规则
            if (package.ignoreService.IsIgnore(assetPath)) {
                continue;
            }
            EAssetCategory category = GetCategory(assetPath);
            if (!TestAssetCategory(category)) {
                continue;
            }
            BuildAssetInfo assetInfo = new BuildAssetInfo(assetPath, category);
            collectedAssets.Add(assetInfo);
        }
        // 分组
        foreach (var assetInfo in collectedAssets) {
            string bundlePath = GetBundlePath(assetInfo.assetPath);
            if (!collectedBundles.TryGetValue(bundlePath, out BuildBundleInfo bundleInfo)) {
                bundleInfo = new BuildBundleInfo(bundlePath)
                {
                    collectPath = collectPath,
                    collectorType = collectorType,
                    provided = provided
                };
                bundleInfo.InitBundleName();
                bundleInfo.bundleType = collectorType == ECollectorType.RawFile
                    ? EBundleType.RawFileBundle
                    : EBundleType.AssetBundle;

                bundleInfo.bundleId = bundleInfo.bundleName.GetHashCode();
                bundleInfo.bundleTags.AddRange(group.bundleTags);
                bundleInfo.bundleTags.AddRange(bundleTags);
                bundleInfo.assetIndexes = assetIndexes;
                bundleInfo.uniqueIndexes = uniqueIndexes;
                bundleInfo.indexDepth = indexDepth;
                collectedBundles.Add(bundlePath, bundleInfo);
            }
            assetInfo.bundleInfo = bundleInfo; // 即使当前尚未分配name和id也安全
            bundleInfo.assetList.Add(assetInfo);
        }
        // 发布到PackageInfo
        BuildPackageInfo packageInfo = package.packageInfo;
        foreach (BuildBundleInfo bundleInfo in collectedBundles.Values) {
            packageInfo.id2BundleDic.Add(bundleInfo.bundleId, bundleInfo);
            packageInfo.name2BundleDic.Add(bundleInfo.bundleName, bundleInfo);
            foreach (BuildAssetInfo assetInfo in bundleInfo.assetList) {
                packageInfo.assetDic.Add(assetInfo.assetPath, assetInfo);
            }
        }
        SetSuccess();
    }

    private string GetBundlePath(string assetPath) {
        if (groupBy == EGroupBy.Collector || !recursive) {
            return collectPath;
        }
        int depth = 0;
        int index = collectPath.Length;
        while ((index = assetPath.IndexOf('/', index)) > 0) {
            depth++;
            if (depth >= groupDepth) {
                return assetPath.Substring(0, index);
            }
        }
        return assetPath.Substring(assetPath.LastIndexOf('/'));
    }

    private EAssetCategory GetCategory(string assetPath) {
        if (classifier != null) {
            return classifier.GetCategory(assetPath);
        }
        Object asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
        if (asset == null || asset is DefaultAsset) {
            return EAssetCategory.None; // 通常是文件夹
        }
        return collectorType switch
        {
            ECollectorType.MainAsset => EAssetCategory.MainAsset,
            ECollectorType.RawFile => EAssetCategory.RawFile,
            _ => EAssetCategory.DependAsset
        };
    }

    private bool TestAssetCategory(EAssetCategory category) {
        if (category == EAssetCategory.None) {
            return false;
        }
        return collectorType switch
        {
            ECollectorType.MainAsset => category == EAssetCategory.MainAsset,
            ECollectorType.DependBundle
                or ECollectorType.DependAsset => category == EAssetCategory.DependAsset,
            ECollectorType.RawFile => category == EAssetCategory.RawFile,
            _ => false,
        };
    }

    private bool ContainsSubPathCollector(CollectorPackage package, PathCache pathCache, string assetPath) {
        string directoryName = pathCache.GetDirectoryName(assetPath);
        while (UnityEditorUtil.IsSubPath(collectPath, directoryName)) {
            bool contains;
            if (collectorType != ECollectorType.RawFile) {
                contains = package.ContainsCollector(directoryName, ECollectorType.MainAsset)
                           || package.ContainsCollector(directoryName, ECollectorType.DependBundle)
                           || package.ContainsCollector(directoryName, ECollectorType.DependAsset);
            } else {
                contains = package.ContainsCollector(directoryName, ECollectorType.RawFile);
            }
            if (contains) {
                return true;
            }
            directoryName = pathCache.GetParentPath(directoryName);
        }
        return false;
    }

    protected override void OnEventImpl(object eventObj) {
    }
}
}