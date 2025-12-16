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
using UnityEngine;
using Wjybxx.BigCat.Editor.Assetor.Tasks;
using Wjybxx.BigCat.Util;
using Wjybxx.BTree;
using Wjybxx.BTree.Branch;
using Wjybxx.Commons;
using Wjybxx.Dson.Codec.Attributes;
using Object = UnityEngine.Object;

namespace Wjybxx.BigCat.Editor.Assetor
{
/// <summary>
/// 包收集器
/// </summary>
[DsonSerializable(NamespaceAliases = new string[]
{
    "BigCatUtil = Wjybxx.BigCat.Util",
})]
public class CollectorPackage : Sequence<Blackboard>
{
    /// <summary>
    /// 要构建的资源包名
    /// </summary>
    public string packageName;
    /// <summary>
    /// 要构建的资源包版本
    /// </summary>
    public string packageVersion;
    /// <summary>
    /// 资源包的显示名称
    /// </summary>
    public string displayName;
    /// <summary>
    /// 资源包描述
    /// </summary>
    public string description;

    /// <summary>
    /// 收集器信息
    /// 注：大型项目可以将Group配置创建在独立Folder，以避免过多节点刷新卡顿
    /// </summary>
    [Commons.SerializeReference]
    public List<CollectorGroup> collectorGroups = new List<CollectorGroup>();
    /// <summary>
    /// 忽略规则服务
    /// 注：忽略工具使用全局配置更易维护
    /// </summary>
    [Commons.SerializeReference]
    public IIgnoreService ignoreService;
    /// <summary>
    /// 是否启用资产依赖数据库
    /// </summary>
    public bool useAssetDependencyDB;

    /// <summary>
    /// 收集到的信息
    /// </summary>
    [NonSerialized]
    public BuildPackageInfo packageInfo;
    /// <summary>
    /// 当前所有的收集器的键，用于判断是否存在子目录收集器
    /// </summary>
    [NonSerialized]
    private readonly HashSet<(string, ECollectorType)> collectorKeys = new(100);

    protected override void BeforeEnter() {
        base.BeforeEnter();
        children.Clear();
        children.AddRange(collectorGroups);
        children.Add(new BuildDependencyTask());

        ignoreService ??= new NullIgnoreService();
        ignoreService.Start();
        // 初始化PackageInfo
        packageInfo = new BuildPackageInfo
        {
            packageName = packageName,
            packageVersion = packageVersion,
            displayName = displayName,
            description = description,
        };
        blackboard.Set(BuildKeys.packageInfo, packageInfo);
        // GetAllAssetPaths返回的是缓存值，因此比搜索文件更快 -- 排序以确保有序
        string[] allAssetPaths = AssetDatabase.GetAllAssetPaths();
        Array.Sort(allAssetPaths, (a, b) => string.Compare(a, b, StringComparison.Ordinal));
        blackboard.Set(BuildKeys.allAssetPaths, allAssetPaths);
        //
        PathCache pathCache = new PathCache(allAssetPaths.Length);
        blackboard.Set(BuildKeys.pathCache, pathCache);
        //
        DependencyCache dependencyCache = new DependencyCache();
        if (useAssetDependencyDB) {
            dependencyCache.Load();
        }
        blackboard.Set(BuildKeys.dependencyCache, dependencyCache);
        //
        InitCollectorKeys();
    }

    private void InitCollectorKeys() {
        foreach (Collector collector in collectorGroups.SelectMany(e => e.collectors)) {
            if (string.IsNullOrEmpty(collector.collectPath)) {
                continue;
            }
            Object assetObj = AssetDatabase.LoadAssetAtPath<Object>(collector.collectPath);
            if (assetObj == null) {
                Debug.LogWarning($"invalid collectPath: {collector.collectPath}");
                continue;
            }
            // 转换为文件系统的标准路径，这样我们就无需规格化
            collector.collectPath = AssetDatabase.GetAssetPath(assetObj);
            (string, ECollectorType) key = new(collector.collectPath, collector.collectorType);
            if (!collectorKeys.Add(key)) {
                throw new Exception($"Collector {collector.collectPath}:{collector.collectorType} already exists!");
            }
        }
    }

    protected override void Exit() {
        base.Exit();
        ignoreService.Stop();
        //
        DependencyCache dependencyCache;
        if (useAssetDependencyDB && (dependencyCache = blackboard.Get(BuildKeys.dependencyCache)) != null) {
            dependencyCache.Save();
        }
    }

    public bool ContainsCollector(string collectPath, ECollectorType collectorType) {
        return collectorKeys.Contains((collectPath, collectorType));
    }
}
}