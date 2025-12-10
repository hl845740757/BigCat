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
    /// 资产标签(不建议使用)
    /// </summary>
    public string[] assetTags = Array.Empty<string>();

    /// <summary>
    /// 归属的bundle
    /// </summary>
    [NonSerialized]
    public AssetBundleInfo bundleInfo;
    /// <summary>
    /// 依赖的bundle
    ///
    /// 注：用于细化加载粒度，加载文件时只加载必要的bundle -- 收益待评估。
    /// </summary>
    public int[] upstreamBundles = Array.Empty<int>();

    #region 序列化

    private const int KEY_ASSET_PATH = 1;
    private const int KEY_TAGS = 2;
    private const int KEY_TAGS_COUNT = 3;
    private const int KEY_UPSTREAMS = 4;
    private const int KEY_UPSTREAMS_COUNT = 5;

    public void Serialize(IDsonWriter<string> writer) {
        writer.WriteStartObject(ObjectStyle.Flow);
        writer.WriteString(nameof(assetPath), assetPath);
        // 文本格式不写入Count，且空数组也写入，提高可读性
        {
            writer.WriteStartArray(nameof(assetTags), ObjectStyle.Flow);
            foreach (string assetTag in assetTags) {
                writer.WriteString(assetTag);
            }
            writer.WriteEndArray();
        }
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
            string key = reader.ReadName();
            switch (key) {
                case nameof(assetPath): {
                    assetPath = reader.ReadString();
                    break;
                }
                case nameof(assetTags): {
                    List<string> assetTags = new List<string>();
                    reader.ReadStartArray();
                    while (reader.ReadDsonType() != DsonType.EndOfObject) {
                        assetTags.Add(reader.ReadString());
                    }
                    reader.ReadEndArray();
                    this.assetTags = assetTags.ToArray();
                    break;
                }
                case nameof(upstreamBundles): {
                    List<int> upstreamBundles = new List<int>();
                    reader.ReadStartArray();
                    while (reader.ReadDsonType() != DsonType.EndOfObject) {
                        upstreamBundles.Add(reader.ReadInt32());
                    }
                    reader.ReadEndArray();
                    this.upstreamBundles = upstreamBundles.ToArray();
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
        // 将Count写在对象外在二进制下是最佳方案，可减少嵌套(Header)
        writer.WriteInt32(KEY_TAGS_COUNT, assetTags.Length);
        if (assetTags.Length > 0) {
            writer.WriteStartArray(KEY_TAGS);
            foreach (string assetTag in assetTags) {
                writer.WriteString(assetTag);
            }
            writer.WriteEndArray();
        }
        writer.WriteInt32(KEY_UPSTREAMS_COUNT, upstreamBundles.Length);
        if (upstreamBundles.Length > 0) {
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
                case KEY_TAGS_COUNT: {
                    assetTags = Array.Empty<string>();
                    int count = reader.ReadInt32();
                    if (count == 0) {
                        break;
                    }
                    assetTags = new string[count];
                    reader.ReadStartArray(KEY_TAGS); // 需校验name
                    for (int idx = 0; idx < count; idx++) {
                        assetTags[idx] = reader.ReadString();
                    }
                    reader.ReadEndArray();
                    break;
                }
                case KEY_UPSTREAMS_COUNT: {
                    upstreamBundles = Array.Empty<int>();
                    int count = reader.ReadInt32();
                    if (count == 0) {
                        break;
                    }
                    upstreamBundles = new int[count];
                    reader.ReadStartArray(KEY_UPSTREAMS);
                    for (int idx = 0; idx < count; idx++) {
                        upstreamBundles[idx] = reader.ReadInt32();
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

    #endregion
}
}