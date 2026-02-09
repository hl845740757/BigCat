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
/// 资产文件信息
/// </summary>
[Serializable]
public sealed class AssetFileInfo
{
    /// <summary>
    /// 资产路径
    ///
    /// 注：由打包工具执行规格化。
    /// </summary>
    public string assetPath;
    /// <summary>
    /// 资产索引方式
    /// </summary>
    public EAssetIndexes assetIndexes;
    /// <summary>
    /// 自定义索引
    /// </summary>
    public string[] addresses = Array.Empty<string>();
    /// <summary>
    /// 资产标签(不建议使用)
    /// </summary>
    public string[] assetTags = Array.Empty<string>();

    /// <summary>
    /// 归属的bundle
    /// </summary>
    [NonSerialized]
    public AssetBundleInfo bundleInfo;

    #region 序列化

    private const int KEY_ASSET_PATH = 1;
    private const int KEY_ASSET_INDEXES = 2;
    private const int KEY_ADDRESSES = 3;
    private const int KEY_ADDRESSES_COUNT = 4;
    private const int KEY_TAGS = 5;
    private const int KEY_TAGS_COUNT = 6;

    public void Serialize(IDsonWriter<string> writer) {
        writer.WriteStartObject();
        writer.WriteString(nameof(assetPath), assetPath);
        writer.WriteInt32(nameof(assetIndexes), (int)assetIndexes, NumberStyle.Simple);
        // 文本格式不写入Count，且空数组也写入，提高可读性
        WriteStringArray(writer, nameof(addresses), addresses);
        WriteStringArray(writer, nameof(assetTags), assetTags);
        writer.WriteEndObject();
    }

    public void Deserialize(IDsonReader<string> reader) {
        reader.ReadStartObject();
        while (reader.ReadDsonType() != DsonType.EndOfObject) {
            string key = reader.ReadName();
            switch (key) {
                case nameof(assetPath): {
                    assetPath = reader.ReadString();
                    break;
                }
                case nameof(assetIndexes): {
                    assetIndexes = (EAssetIndexes)reader.ReadInt32();
                    break;
                }
                case nameof(addresses): {
                    this.addresses = ReadStringArray(reader);
                    break;
                }
                case nameof(assetTags): {
                    this.assetTags = ReadStringArray(reader);
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
        writer.WriteInt32(KEY_ASSET_INDEXES, (int)assetIndexes);
        // 将Count写在对象外在二进制下是最佳方案，可减少嵌套(Header)
        writer.WriteInt32(KEY_ADDRESSES_COUNT, addresses.Length);
        if (addresses.Length > 0) {
            WriteStringArray(writer, KEY_ADDRESSES, addresses);
        }
        writer.WriteInt32(KEY_TAGS_COUNT, assetTags.Length);
        if (assetTags.Length > 0) {
            WriteStringArray(writer, KEY_TAGS, assetTags);
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
                case KEY_ASSET_INDEXES: {
                    assetIndexes = (EAssetIndexes)reader.ReadInt32();
                    break;
                }
                case KEY_ADDRESSES_COUNT: {
                    int count = reader.ReadInt32();
                    addresses = ReadStringArray(reader, KEY_ADDRESSES, count);
                    break;
                }
                case KEY_TAGS_COUNT: {
                    int count = reader.ReadInt32();
                    assetTags = ReadStringArray(reader, KEY_TAGS, count);
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

    private static void WriteStringArray(IDsonWriter<string> writer, string name, string[] array) {
        writer.WriteStartArray(name, ObjectStyle.Flow);
        foreach (string element in array) {
            writer.WriteString(element);
        }
        writer.WriteEndArray();
    }

    private static string[] ReadStringArray(IDsonReader<string> reader) {
        List<string> result = new List<string>();
        reader.ReadStartArray();
        while (reader.ReadDsonType() != DsonType.EndOfObject) {
            result.Add(reader.ReadString());
        }
        reader.ReadEndArray();
        return result.ToArray();
    }

    private static void WriteStringArray(IDsonWriter<int> writer, int name, string[] array) {
        writer.WriteStartArray(name, ObjectStyle.Flow);
        foreach (string element in array) {
            writer.WriteString(element);
        }
        writer.WriteEndArray();
    }

    private static string[] ReadStringArray(IDsonReader<int> reader, int name, int count) {
        if (count == 0) {
            return Array.Empty<string>();
        }
        string[] result = new string[count];
        int idx = 0;
        reader.ReadStartArray(name); // 校验name
        while (reader.ReadDsonType() != DsonType.EndOfObject) {
            result[idx++] = reader.ReadString();
        }
        reader.ReadEndArray();
        return result;
    }

    #endregion
}
}