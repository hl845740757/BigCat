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
using UnityEngine;
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Pool;
using Wjybxx.Dson;
using Wjybxx.Dson.IO;
using Wjybxx.Dson.Text;

namespace Wjybxx.BigCat.Editor.Assetor
{
/// <summary>
/// 
/// 注：如果不调用<see cref="Load"/>，则表示实时计算并缓存。
/// </summary>
public sealed class DependencyCache
{
    /// <summary>
    /// 缓存文件路径
    /// </summary>
    private string _cacheFilePath = "Library/AssetDependency.db";
    /// <summary>
    /// 共享字符串表,index是1开始连续增长
    /// </summary>
    private readonly LinkedDictionary<string, int> str2IndexMap = new(10000);
    private readonly LinkedDictionary<int, string> index2StrMap = new(10000);
    /// <summary>
    /// 资产路径到缓存信息的映射
    /// </summary>
    private readonly LinkedDictionary<string, Item> path2ItemMap = new(10000);

    /// <summary>
    /// 缓存文件路径
    /// </summary>
    public string CacheFilePath {
        get => _cacheFilePath;
        set => _cacheFilePath = value;
    }

    /// <summary>
    /// 获取资产关联的依赖资产（上游资产）
    /// </summary>
    /// <param name="assetPath"></param>
    /// <returns></returns>
    public IReadOnlyList<string> GetDependencies(string assetPath) {
        if (path2ItemMap.TryGetValue(assetPath, out Item item)) {
            List<string> dependPaths = new(item.dependGuids.Count);
            foreach (int guidIdx in item.dependGuids) {
                string dependGuid = index2StrMap[guidIdx];
                string dependPath = AssetDatabase.GUIDToAssetPath(dependGuid);
                dependPaths.Add(dependPath);
            }
            return dependPaths;
        } else {
            string guid = AssetDatabase.AssetPathToGUID(assetPath, AssetPathToGUIDOptions.OnlyExistingAssets);
            if (string.IsNullOrEmpty(guid)) {
                throw new Exception($"Asset {assetPath} not found");
            }
            string[] dependPaths = AssetDatabase.GetDependencies(assetPath, true);
            List<int> dependGuids = new List<int>(dependPaths.Length);
            foreach (string dependPath in dependPaths) {
                string dependGuid = AssetDatabase.AssetPathToGUID(dependPath, AssetPathToGUIDOptions.OnlyExistingAssets);
                dependGuids.Add(AddSharedString(dependGuid));
            }
            Hash128 hash128 = AssetDatabase.GetAssetDependencyHash(assetPath);
            item = new Item(AddSharedString(guid), hash128.ToString(), dependGuids);
            item.guid = guid;
            item.assetPath = assetPath;
            item.hash128 = hash128;
            path2ItemMap.Add(assetPath, item);
            return dependPaths;
        }
    }

    private int AddSharedString(string value) {
        if (string.IsNullOrEmpty(value)) {
            throw new Exception("Shared string is null or empty");
        }
        if (!str2IndexMap.TryGetValue(value, out int index)) {
            index = str2IndexMap.Count > 0 ? str2IndexMap.PeekLast().Value + 1 : 1;
            str2IndexMap.AddLast(value, index);
            index2StrMap.AddLast(index, value);
        }
        return index;
    }

    #region 序列化

    private const int KEY_GUID_INDEX = 1;
    private const int KEY_DEPEND_HASH = 2;
    private const int KEY_DEPEND_GUIDS = 3;

    /// <summary>
    /// 保存到数据库
    /// </summary>
    public void Save() {
        string filePath = Path.GetDirectoryName(Application.dataPath) + "/" + _cacheFilePath;
        if (File.Exists(filePath)) {
            File.Delete(filePath);
        }
        using DsonOutputs.ArrayOutput output = DsonOutputs.NewInstance(IArrayPool<byte>.Shared, 64 * 1024, 1024 * 1024);
        using DsonBinaryWriter<int> writer = new DsonBinaryWriter<int>(DsonWriterSettings.Default, output, false);
        // index2str
        writer.WriteStartObject(ObjectStyle.Indent);
        foreach (var pair in index2StrMap) {
            writer.WriteString(pair.Key, pair.Value);
        }
        writer.WriteEndObject();
        // ItemArray
        writer.WriteStartArray(ObjectStyle.Indent);
        foreach (Item item in path2ItemMap.Values) {
            writer.WriteStartObject(ObjectStyle.Flow);
            writer.WriteInt32(KEY_GUID_INDEX, item.guidIndex);
            writer.WriteString(KEY_DEPEND_HASH, item.dependHash);
            writer.WriteStartArray(KEY_DEPEND_GUIDS);
            foreach (int dependGuid in item.dependGuids) {
                writer.WriteInt32(dependGuid);
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        writer.WriteEndArray();

        byte[] data = ArrayUtil.CopyOf(output.Buffer, 0, output.Buffer.Length);
        File.WriteAllBytes(filePath, data);
    }

    /// <summary>
    /// 加载缓存数据
    /// </summary>
    public void Load() {
        str2IndexMap.Clear();
        index2StrMap.Clear();
        path2ItemMap.Clear();

        string filePath = Path.GetDirectoryName(Application.dataPath) + "/" + _cacheFilePath;
        if (!File.Exists(filePath)) {
            return;
        }
        IDsonInput dsonInput = DsonInputs.NewInstance(File.ReadAllBytes(filePath));
        using DsonBinaryReader<int> reader = new DsonBinaryReader<int>(DsonReaderSettings.Default, dsonInput);
        if (reader.ReadDsonType() == DsonType.EndOfObject) {
            return; // 空文件
        }
        // index2str
        reader.ReadStartObject();
        while (reader.CurrentDsonType != DsonType.EndOfObject) {
            int index = reader.ReadName();
            string str = reader.ReadString();
            index2StrMap.Add(index, str);
            str2IndexMap.Add(str, index);
        }
        reader.ReadEndObject();
        // itemArray
        reader.ReadStartArray();
        List<Item> itemList = new List<Item>(10000);
        while (reader.CurrentDsonType != DsonType.EndOfObject) {
            reader.ReadStartObject();
            int guidIndex = reader.ReadInt32(KEY_GUID_INDEX);
            string dependHash = reader.ReadString(KEY_DEPEND_HASH);
            List<int> dependGuids = ReadIntArray(reader, KEY_DEPEND_GUIDS);
            reader.ReadEndObject();
            //
            Item item = new Item(guidIndex, dependHash, dependGuids);
            item.guid = index2StrMap[guidIndex];
            item.assetPath = AssetDatabase.GUIDToAssetPath(item.guid);
            item.hash128 = Hash128.Parse(dependHash);
            itemList.Add(item);
        }
        reader.ReadEndArray();
        // 构建缓存
        foreach (Item item in itemList) {
            if (string.IsNullOrEmpty(item.assetPath)) {
                continue;
            }
            if (AssetDatabase.GetAssetDependencyHash(item.assetPath) != item.hash128) {
                continue; // 缓存失效
            }
            path2ItemMap.Add(item.assetPath, item);
        }
    }

    private static List<int> ReadIntArray(DsonBinaryReader<int> reader, int name) {
        List<int> result = new List<int>();
        reader.ReadStartArray(name);
        while (reader.ReadDsonType() != DsonType.EndOfObject) {
            result.Add(reader.ReadInt32());
        }
        reader.ReadEndArray();
        return result;
    }

    #endregion

    /// <summary>
    /// 缓存数据是基于guid的
    /// </summary>
    private sealed class Item
    {
        public int guidIndex;
        public string dependHash;
        public List<int> dependGuids; // 所有依赖，因为我们保存的是字符串索引，所以保存所有依赖会大幅降低复杂度

        [NonSerialized] public string guid;
        [NonSerialized] public string assetPath;
        [NonSerialized] public Hash128 hash128;

        public Item(int guidIndex, string dependHash, List<int> dependGuids) {
            this.guidIndex = guidIndex;
            this.dependHash = dependHash;
            this.dependGuids = dependGuids;
        }
    }
}
}