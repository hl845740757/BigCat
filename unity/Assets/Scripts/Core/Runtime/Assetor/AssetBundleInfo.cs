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
using Wjybxx.Dson;
using Wjybxx.Dson.Text;

namespace Wjybxx.BigCat.Assetor
{
/// <summary>
/// Bundle文件信息
///
/// 注：运行时可通过<see cref="AssetFileInfo"/>获取该实例的引用。
/// </summary>
[Serializable]
public sealed class AssetBundleInfo
{
    /// <summary>
    /// bundle打包的起始文件夹路径
    /// </summary>
    public string assetPath;
    /// <summary>
    /// bundle文件名
    ///
    /// 注：至少保证包级别唯一。
    /// </summary>
    public string bundleName;
    /// <summary>
    /// Bundle类型
    /// </summary>
    public EBundleType bundleType;
    /// <summary>
    /// 收集器路径的长度(用于计算索引)
    /// </summary>
    public int collectPathLength;

    /// <summary>
    /// Unity生成的CRC(基于未压缩内容计算)
    /// </summary>
    public uint unityCRC;
    /// <summary>
    /// 文件哈希值(sha1)
    /// </summary>
    public string fileHash;
    /// <summary>
    /// 文件校验码
    /// </summary>
    public uint fileCRC;
    /// <summary>
    /// 文件大小（字节数，计算下载量）
    /// </summary>
    public int fileSize;
    /// <summary>
    /// 加密方式
    /// </summary>
    public int encrypted;

    /// <summary>
    /// Bundle标签
    ///
    /// 注：主要用于支持预下载（或预加载）特定标签的Bundle;
    /// 比如登录游戏前，进入特定场景前，必须完成特定标签的Bundle下载或加载。
    /// </summary>
    public List<string> bundleTags = new List<string>();
    /// <summary>
    /// 该Bundle的所有主资产文件（可代码加载的资产）
    /// 
    /// 1.我们只索引主资产和原始文件资产，以避免不必要的开销 -- 手动加载的资源为少数。
    /// 2.存储在这里和存储在Package中区别不大，但存储在这里会更直观。
    /// </summary>
    public List<AssetFileInfo> mainAssets = new List<AssetFileInfo>();
    /// <summary>
    /// bundle内资产索引方式
    /// </summary>
    public EAssetIndexes assetIndexes;
    /// <summary>
    /// 索引祖先节点长度
    /// </summary>
    public int ancestorPathLength;
    /// <summary>
    /// bundle内文件总数(主资产+依赖资产)
    /// </summary>
    public int assetCount;

    /// <summary>
    /// 所属的资源包（用于运行时反向查询）
    /// </summary>
    [NonSerialized]
    public AssetPackageInfo packageInfo;
    /// <summary>
    /// bundleId
    ///
    /// 注：至少保证包级别唯一。
    /// </summary>
    public int bundleId;
    /// <summary>
    /// 该bundle依赖的上游bundle
    ///
    /// 注：其实可以根据内部文件数据计算出来。
    /// </summary>
    public List<int> upstreamBundles = new List<int>();
    /// <summary>
    /// 依赖该bundle的下游bundle（缓存值）
    /// </summary>
    [NonSerialized]
    public HashSet<int> downstreamBundles = new HashSet<int>();
    /// <summary>
    /// 卸载Bundle时是否卸载加载的对象
    /// </summary>
    [NonSerialized]
    public bool unloadAllLoadedObjects;

    #region 序列化

    private const int KEY_ASSET_PATH = 1;
    private const int KEY_BUNDLE_NAME = 2;
    private const int KEY_BUNDLE_TYPE = 3;
    private const int KEY_COLLECT_PATH_LENGTH = 4;

    private const int KEY_BUNDLE_UNITY_CRC = 5;
    private const int KEY_FILE_HASH = 6;
    private const int KEY_FILE_CRC = 7;
    private const int KEY_FILE_SIZE = 8;
    private const int KEY_ENCRYPTED = 9;

    private const int KEY_BUNDLE_TAGS = 10;
    private const int KEY_BUNDLE_TAGS_COUNT = 11;
    private const int KEY_MAIN_ASSETS = 12;
    private const int KEY_MAIN_ASSETS_COUNT = 13;
    private const int KEY_INDEXES = 14;
    private const int KEY_ANCESTOR_PATH_LENGTH = 15;
    private const int KEY_ASSET_COUNT = 16;
    private const int KEY_BUNDLE_ID = 17;
    private const int KEY_UPSTREAMS = 18;
    private const int KEY_UPSTREAMS_COUNT = 19;

    public void Serialize(IDsonWriter<string> writer) {
        writer.WriteStartObject();
        writer.WriteString(nameof(assetPath), assetPath);
        writer.WriteString(nameof(bundleName), bundleName);
        writer.WriteInt32(nameof(bundleType), (int)bundleType, NumberStyle.Simple);
        writer.WriteInt32(nameof(collectPathLength), collectPathLength, NumberStyle.Simple);
        //
        writer.WriteInt32(nameof(unityCRC), (int)unityCRC, NumberStyle.Hex);
        writer.WriteString(nameof(fileHash), fileHash);
        writer.WriteInt32(nameof(fileCRC), (int)fileCRC, NumberStyle.Hex);
        writer.WriteInt32(nameof(fileSize), fileSize, NumberStyle.Simple);
        writer.WriteInt32(nameof(encrypted), encrypted, NumberStyle.Simple);
        //
        {
            writer.WriteStartArray(nameof(bundleTags), ObjectStyle.Flow);
            foreach (string bundleTag in bundleTags) {
                writer.WriteString(bundleTag);
            }
            writer.WriteEndArray();
        }
        {
            writer.WriteStartArray(nameof(mainAssets));
            foreach (AssetFileInfo fileInfo in mainAssets) {
                fileInfo.Serialize(writer);
            }
            writer.WriteEndArray();
        }
        writer.WriteInt32(nameof(assetIndexes), (int)assetIndexes, NumberStyle.Hex);
        writer.WriteInt32(nameof(ancestorPathLength), ancestorPathLength, NumberStyle.Simple);
        writer.WriteInt32(nameof(assetCount), assetCount, NumberStyle.Simple);
        writer.WriteInt32(nameof(bundleId), bundleId, NumberStyle.Simple);
        {
            writer.WriteStartArray(nameof(upstreamBundles), ObjectStyle.Flow);
            foreach (int upstreamBundle in upstreamBundles) {
                writer.WriteInt32(upstreamBundle, NumberStyle.Simple);
            }
            writer.WriteEndArray();
        }
        writer.WriteEndObject();
    }

    public void Deserialize(IDsonReader<string> reader) {
        reader.ReadStartObject();
        while (reader.ReadDsonType() != DsonType.EndOfObject) {
            string name = reader.ReadName();
            switch (name) {
                case nameof(assetPath): {
                    assetPath = reader.ReadString();
                    break;
                }
                case nameof(bundleName): {
                    bundleName = reader.ReadString();
                    break;
                }
                case nameof(bundleType): {
                    bundleType = (EBundleType)reader.ReadInt32();
                    break;
                }
                case nameof(collectPathLength): {
                    collectPathLength = reader.ReadInt32();
                    break;
                }
                case nameof(unityCRC): {
                    unityCRC = (uint)reader.ReadInt32();
                    break;
                }
                case nameof(fileHash): {
                    fileHash = reader.ReadString();
                    break;
                }
                case nameof(fileCRC): {
                    fileCRC = (uint)reader.ReadInt32();
                    break;
                }
                case nameof(fileSize): {
                    fileSize = reader.ReadInt32();
                    break;
                }
                case nameof(encrypted): {
                    encrypted = reader.ReadInt32();
                    break;
                }
                case nameof(bundleTags): {
                    bundleTags.Clear();
                    reader.ReadStartArray();
                    while (reader.ReadDsonType() != DsonType.EndOfObject) {
                        bundleTags.Add(reader.ReadString());
                    }
                    reader.ReadEndArray();
                    break;
                }
                case nameof(mainAssets): {
                    mainAssets.Clear();
                    reader.ReadStartArray();
                    while (reader.ReadDsonType() != DsonType.EndOfObject) {
                        AssetFileInfo fileInfo = new AssetFileInfo();
                        fileInfo.Deserialize(reader);
                        mainAssets.Add(fileInfo);
                    }
                    reader.ReadEndArray();
                    break;
                }
                case nameof(assetIndexes): {
                    assetIndexes = (EAssetIndexes)reader.ReadInt32();
                    break;
                }
                case nameof(ancestorPathLength): {
                    ancestorPathLength = reader.ReadInt32();
                    break;
                }
                case nameof(assetCount): {
                    assetCount = reader.ReadInt32();
                    break;
                }
                case nameof(bundleId): {
                    bundleId = reader.ReadInt32();
                    break;
                }
                case nameof(upstreamBundles): {
                    upstreamBundles.Clear();
                    reader.ReadStartArray();
                    while (reader.ReadDsonType() != DsonType.EndOfObject) {
                        upstreamBundles.Add(reader.ReadInt32());
                    }
                    reader.ReadEndArray();
                    break;
                }
                default: {
                    reader.SkipValue();
                    break;
                }
            }
        }
        reader.ReadEndObject();
    }

    public void Serialize(IDsonWriter<int> writer) {
        writer.WriteStartObject();
        writer.WriteString(KEY_ASSET_PATH, assetPath);
        writer.WriteString(KEY_BUNDLE_NAME, bundleName);
        writer.WriteInt32(KEY_BUNDLE_TYPE, (int)bundleType);
        writer.WriteInt32(KEY_COLLECT_PATH_LENGTH, collectPathLength);
        //
        writer.WriteInt32(KEY_BUNDLE_UNITY_CRC, (int)unityCRC);
        writer.WriteString(KEY_FILE_HASH, fileHash);
        writer.WriteInt32(KEY_FILE_CRC, (int)fileCRC);
        writer.WriteInt32(KEY_FILE_SIZE, fileSize);
        writer.WriteInt32(KEY_ENCRYPTED, encrypted);
        //
        writer.WriteInt32(KEY_BUNDLE_TAGS_COUNT, bundleTags.Count);
        if (bundleTags.Count > 0) {
            writer.WriteStartArray(KEY_BUNDLE_TAGS);
            foreach (string bundleTag in bundleTags) {
                writer.WriteString(bundleTag);
            }
            writer.WriteEndArray();
        }
        writer.WriteInt32(KEY_MAIN_ASSETS_COUNT, mainAssets.Count);
        if (mainAssets.Count > 0) {
            writer.WriteStartArray(KEY_MAIN_ASSETS);
            foreach (AssetFileInfo fileInfo in mainAssets) {
                fileInfo.Serialize(writer);
            }
            writer.WriteEndArray();
        }
        writer.WriteInt32(KEY_INDEXES, (int)assetIndexes);
        writer.WriteInt32(KEY_ANCESTOR_PATH_LENGTH, ancestorPathLength);
        writer.WriteInt32(KEY_ASSET_COUNT, assetCount);
        writer.WriteInt32(KEY_BUNDLE_ID, bundleId);
        writer.WriteInt32(KEY_UPSTREAMS_COUNT, upstreamBundles.Count);
        if (upstreamBundles.Count > 0) {
            writer.WriteStartArray(KEY_UPSTREAMS);
            foreach (int upstreamBundle in upstreamBundles) {
                writer.WriteInt32(upstreamBundle);
            }
            writer.WriteEndArray();
        }
        writer.WriteEndObject();
    }

    public void Deserialize(IDsonReader<int> reader) {
        reader.ReadStartObject();
        while (reader.ReadDsonType() != DsonType.EndOfObject) {
            int name = reader.ReadName();
            switch (name) {
                case KEY_ASSET_PATH: {
                    assetPath = reader.ReadString();
                    break;
                }
                case KEY_BUNDLE_NAME: {
                    bundleName = reader.ReadString();
                    break;
                }
                case KEY_BUNDLE_TYPE: {
                    bundleType = (EBundleType)reader.ReadInt32();
                    break;
                }
                case KEY_COLLECT_PATH_LENGTH: {
                    collectPathLength = reader.ReadInt32();
                    break;
                }
                case KEY_BUNDLE_UNITY_CRC: {
                    unityCRC = (uint)reader.ReadInt32();
                    break;
                }
                case KEY_FILE_HASH: {
                    fileHash = reader.ReadString();
                    break;
                }
                case KEY_FILE_CRC: {
                    fileCRC = (uint)reader.ReadInt32();
                    break;
                }
                case KEY_FILE_SIZE: {
                    fileSize = reader.ReadInt32();
                    break;
                }
                case KEY_ENCRYPTED: {
                    encrypted = reader.ReadInt32();
                    break;
                }
                case KEY_BUNDLE_TAGS_COUNT: {
                    bundleTags.Clear();
                    int count = reader.ReadInt32();
                    if (count == 0) {
                        break;
                    }
                    bundleTags.Capacity = count;
                    reader.ReadStartArray(KEY_BUNDLE_TAGS);
                    while (reader.ReadDsonType() != DsonType.EndOfObject) {
                        bundleTags.Add(reader.ReadString());
                    }
                    reader.ReadEndArray();
                    break;
                }
                case KEY_MAIN_ASSETS_COUNT: {
                    mainAssets.Clear();
                    int count = reader.ReadInt32();
                    if (count == 0) {
                        break;
                    }
                    mainAssets.Capacity = count;
                    reader.ReadStartArray(KEY_MAIN_ASSETS);
                    while (reader.ReadDsonType() != DsonType.EndOfObject) {
                        AssetFileInfo fileInfo = new AssetFileInfo();
                        fileInfo.Deserialize(reader);
                        mainAssets.Add(fileInfo);
                    }
                    reader.ReadEndArray();
                    break;
                }
                case KEY_INDEXES: {
                    assetIndexes = (EAssetIndexes)reader.ReadInt32();
                    break;
                }
                case KEY_ANCESTOR_PATH_LENGTH: {
                    ancestorPathLength = reader.ReadInt32();
                    break;
                }
                case KEY_ASSET_COUNT: {
                    assetCount = reader.ReadInt32();
                    break;
                }
                case KEY_BUNDLE_ID: {
                    bundleId = reader.ReadInt32();
                    break;
                }
                case KEY_UPSTREAMS_COUNT: {
                    upstreamBundles.Clear();
                    int count = reader.ReadInt32();
                    if (count == 0) {
                        break;
                    }
                    upstreamBundles.Capacity = count;
                    reader.ReadStartArray(KEY_UPSTREAMS);
                    while (reader.ReadDsonType() != DsonType.EndOfObject) {
                        upstreamBundles.Add(reader.ReadInt32());
                    }
                    reader.ReadEndArray();
                    break;
                }
            }
        }
        reader.ReadEndObject();
    }

    #endregion
}
}