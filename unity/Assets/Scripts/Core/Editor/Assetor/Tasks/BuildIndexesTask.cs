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
using System.Reflection;
using Wjybxx.BigCat.Assetor;
using Wjybxx.BigCatTool;
using Wjybxx.BTree;
using Wjybxx.Dson.Codec.Attributes;
using Wjybxx.Dson.Text;
using Wjybxx.Dson.Types;
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
    /// <summary>
    /// 是否上报详细日志到独立文件
    /// </summary>
    public bool reportLog;
    /// <summary>
    /// 上报日志路径
    /// </summary>
    public string reportPath;
    /// <summary>
    /// 支持无扩展名索引的文件类型
    /// </summary>
    public HashSet<string> supportExtensions = new HashSet<string>();

    protected override void Execute() {
        BuildPackageInfo packageInfo = blackboard.Get(BuildKeys.packageInfo);
        Dictionary<string, Item> index2AssetDic = new(packageInfo.assetDic.Count);
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
                string extension = UnityEditorUtil.GetExtension(fileName);
                if (assetInfo.disableIndexes) {
                    continue;
                }
                // 单纯的数字文件名不参与索引
                if ((indexes & EAssetIndexes.FileName) != 0 && !IsNumber(fileName)) {
                    bool unique = (bundleInfo.uniqueIndexes & EAssetIndexes.FileName) != 0;
                    AddIndex(index2AssetDic, fileName, assetInfo, unique);
                    //
                    if (supportExtensions.Contains(extension)) {
                        string fileNameNoExt = RemoveExtension(fileName, extension);
                        AddIndex(index2AssetDic, fileNameNoExt, assetInfo, unique);
                    }
                }
                // 单层级目录索引可选唯一性
                if ((indexes & EAssetIndexes.FolderAndFileName) != 0) {
                    bool unique = (bundleInfo.uniqueIndexes & EAssetIndexes.FolderAndFileName) != 0;
                    string subAssetPath = GetSubAssetPath(assetInfo.assetPath, 1);
                    AddIndex(index2AssetDic, subAssetPath, assetInfo, unique);
                    //
                    if (supportExtensions.Contains(extension)) {
                        string subAssetPathNoExt = RemoveExtension(subAssetPath, extension);
                        AddIndex(index2AssetDic, subAssetPathNoExt, assetInfo, unique);
                    }
                }
                // 多层级目录索引
                if ((indexes & EAssetIndexes.FolderAndFileNamePlus) != 0) {
                    bool unique = (bundleInfo.uniqueIndexes & EAssetIndexes.FolderAndFileNamePlus) != 0;
                    string subAssetPath = GetSubAssetPath(assetInfo.assetPath, bundleInfo.indexDepth);
                    AddIndex(index2AssetDic, subAssetPath, assetInfo, unique);
                    //
                    if (supportExtensions.Contains(extension)) {
                        string subAssetPathNoExt = RemoveExtension(subAssetPath, extension);
                        AddIndex(index2AssetDic, subAssetPathNoExt, assetInfo, unique);
                    }
                }
                // 相对Collector的路径
                if ((indexes & EAssetIndexes.RelativeToCollector) != 0) {
                    bool unique = (bundleInfo.uniqueIndexes & EAssetIndexes.RelativeToCollector) != 0;
                    string subAssetPath = assetInfo.assetPath.Substring(bundleInfo.collectPath.Length + 1);
                    AddIndex(index2AssetDic, subAssetPath, assetInfo, unique);
                    //
                    if (supportExtensions.Contains(extension)) {
                        string subAssetPathNoExt = RemoveExtension(subAssetPath, extension);
                        AddIndex(index2AssetDic, subAssetPathNoExt, assetInfo, unique);
                    }
                }
                // 资产类型名+文件名索引
                if ((indexes & EAssetIndexes.TypeAndFileName) != 0) {
                    bool unique = (bundleInfo.uniqueIndexes & EAssetIndexes.TypeAndFileName) != 0;
                    // 允许替换原始类名
                    AssetTypeAliasAttribute aliasAttribute = assetInfo.assetType.GetCustomAttribute<AssetTypeAliasAttribute>();
                    if (aliasAttribute == null || !aliasAttribute.replace) {
                        string address = assetInfo.assetType.Name + ":" + RemoveExtension(fileName, extension);
                        assetInfo.addresses.Add(address);
                        AddIndex(index2AssetDic, address, assetInfo, unique);
                    }
                    if (aliasAttribute != null) {
                        string address = aliasAttribute.alias + ":" + RemoveExtension(fileName, extension);
                        assetInfo.addresses.Add(address);
                        AddIndex(index2AssetDic, address, assetInfo, unique);
                    }
                }
                // 资产类型名+文件夹+文件名索引(AudioClip:Music/Login)
                if ((indexes & EAssetIndexes.TypeAndFolderName) != 0) {
                    bool unique = (bundleInfo.uniqueIndexes & EAssetIndexes.TypeAndFolderName) != 0;
                    string subAssetPath = GetSubAssetPath(assetInfo.assetPath, 1);
                    // 允许替换原始类名
                    AssetTypeAliasAttribute aliasAttribute = assetInfo.assetType.GetCustomAttribute<AssetTypeAliasAttribute>();
                    if (aliasAttribute == null || !aliasAttribute.replace) {
                        string address = assetInfo.assetType.Name + ":" + RemoveExtension(subAssetPath, extension);
                        assetInfo.addresses.Add(address);
                        AddIndex(index2AssetDic, address, assetInfo, unique);
                    }
                    if (aliasAttribute != null) {
                        string address = aliasAttribute.alias + ":" + RemoveExtension(subAssetPath, extension);
                        assetInfo.addresses.Add(address);
                        AddIndex(index2AssetDic, address, assetInfo, unique);
                    }
                }
            }
        }
        if (index2AssetDic.Values.Any(e => e.conflict)) {
            ReportLog(index2AssetDic);
            SetFailed((int)BuildErrorCodec.IndexConflict);
        } else {
            SetSuccess();
        }
    }

    protected override void OnEventImpl(object eventObj) {
    }

    private void ReportLog(Dictionary<string, Item> index2AssetDic) {
        using StringWriter stringWriter = new StringWriter();
        using DsonTextWriter writer = new DsonTextWriter(DsonTextWriterSettings.Default, stringWriter);
        // 构建信息
        writer.WriteStartObject(ObjectStyle.Indent);
        writer.WriteDateTime("dateTime", ExtDateTime.OfDateTime(DateTime.Now));
        writer.WriteEndObject();
        // 冲突详情
        foreach (Item item in index2AssetDic.Values.Where(e => e.conflict)) {
            writer.WriteStartObject(ObjectStyle.Indent);
            writer.WriteString("index", item.index);
            writer.WriteInt32("count", item.assetPaths.Count, NumberStyle.Simple); // 方便检索
            writer.WriteStartArray("assetPaths", ObjectStyle.Indent);
            foreach (string assetPath in item.assetPaths) {
                writer.WriteString(assetPath);
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        string details = stringWriter.ToString();
        blackboard.Set(BuildKeys.indexReportLog, details);
        // 上报到独立文件
        if (reportLog && !string.IsNullOrEmpty(reportPath)) {
            string filePath = UnityEditorUtil.ConvertToFilePath(reportPath);
            if (File.Exists(filePath)) {
                File.Delete(filePath);
            }
            ToolUtil.CreateFileDirectory(filePath);
            File.WriteAllText(filePath, details);
        }
    }

    private void AddIndex(Dictionary<string, Item> dic, string key, BuildAssetInfo assetInfo, bool unique) {
        if (!dic.TryGetValue(key, out Item item)) {
            item = new Item(key);
            dic.Add(key, item);
        }
        item.unique |= unique;
        item.assetPaths.Add(assetInfo.assetPath);
        if (item.unique && item.assetPaths.Count > 1) {
            item.conflict = true;
        }
    }

    private static string RemoveExtension(string path, string extension) {
        if (string.IsNullOrEmpty(extension)) return path;
        return path.Substring(0, path.Length - extension.Length - 1);
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

    private class Item
    {
        public readonly string index;
        public readonly List<string> assetPaths = new();
        public bool unique;
        public bool conflict;

        public Item(string index) {
            this.index = index;
        }
    }
}
}