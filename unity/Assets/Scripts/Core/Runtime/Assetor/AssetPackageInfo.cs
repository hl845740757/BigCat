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
using Wjybxx.Commons.Collections;
using Wjybxx.Dson;

namespace Wjybxx.BigCat.Assetor
{
/// <summary>
/// 资源包信息
///
/// 注：如果按照正儿八经的软件包方式管理，那么还应该定义包依赖...
/// </summary>
[Serializable]
public sealed class AssetPackageInfo
{
    /// <summary>
    /// 资源包名称
    /// </summary>
    public string packageName;
    /// <summary>
    /// 资源包版本
    /// </summary>
    public string packageVersion;
    /// <summary>
    /// 资源包展示名
    /// </summary>
    public string displayName;
    /// <summary>
    /// 资源包描述
    /// </summary>
    public string description;
    /// <summary>
    /// 构建时间
    /// 注：构建时间代替的是构建版本，不能用于判断是否需要更新。
    /// </summary>
    public string buildTime;

    /// <summary>
    /// 所有的Bundle信息
    /// </summary>
    public List<AssetBundleInfo> bundleList = new List<AssetBundleInfo>();
    /// <summary>
    /// BundleId到BundleInfo的映射(查询缓存)
    /// </summary>
    [NonSerialized]
    public readonly LinkedDictionary<int, AssetBundleInfo> id2BundleDic = new();
    /// <summary>
    /// BundleName到BundleInfo的映射(查询缓存)
    /// </summary>
    [NonSerialized]
    public readonly LinkedDictionary<string, AssetBundleInfo> name2BundleDic = new();
    /// <summary>
    /// 主资产文件数量
    /// </summary>
    [NonSerialized]
    public int mainAssetsCount;

    /// <summary>
    /// 清理缓存数据
    /// </summary>
    public void ClearCache() {
        mainAssetsCount = 0;
        id2BundleDic.Clear();
        name2BundleDic.Clear();
#if DEBUG
        foreach (AssetBundleInfo bundleInfo in bundleList) {
            bundleInfo.downstreamBundles.Clear();
        }
#endif
    }

    /// <summary>
    /// 构建缓存数据
    /// </summary>
    public void BuildCache() {
        ClearCache();
        id2BundleDic.EnsureCapacity(bundleList.Count);
        name2BundleDic.EnsureCapacity(bundleList.Count);
        // 先建立基础缓存 - 重复应当在打包就检测出来
        foreach (AssetBundleInfo bundleInfo in bundleList) {
            bundleInfo.packageInfo = this;
            id2BundleDic.Add(bundleInfo.bundleId, bundleInfo);
            name2BundleDic.Add(bundleInfo.bundleName, bundleInfo);
            //
            mainAssetsCount += bundleInfo.mainAssets.Count;
            foreach (AssetFileInfo fileInfo in bundleInfo.mainAssets) {
                fileInfo.bundleInfo = bundleInfo;
            }
        }
#if DEBUG
        // 构建依赖图缓存
        foreach (AssetBundleInfo bundleInfo in bundleList) {
            foreach (int upstreamBundle in bundleInfo.upstreamBundles) {
                if (!id2BundleDic.TryGetValue(upstreamBundle, out AssetBundleInfo upstreamBundleInfo)) {
                    throw new Exception($"Upstream bundle {upstreamBundle} not found");
                }
                upstreamBundleInfo.downstreamBundles.Add(bundleInfo.bundleId);
            }
        }
#endif
    }

    #region 序列化

    private const int KEY_PACKAGE_NAME = 1;
    private const int KEY_PACKAGE_VERSION = 2;
    private const int KEY_DISPLAY_NAME = 3;
    private const int KEY_DESCRIPTION = 4;
    private const int KEY_BUILD_TIME = 5;
    private const int KEY_BUILD_PIPELINE = 6;
    private const int KEY_BUNDLE_LIST = 7;
    private const int KEY_BUNDLE_LIST_COUNT = 8;

    /// <summary>
    /// 编码为key为string的Dson结构
    /// (用于查看打包结果)
    /// </summary>
    public void Serialize(IDsonWriter<string> writer) {
        writer.WriteStartObject();
        writer.WriteString(nameof(packageName), packageName);
        writer.WriteString(nameof(packageVersion), packageVersion);
        writer.WriteString(nameof(displayName), displayName ?? "");
        writer.WriteString(nameof(description), description ?? "");
        writer.WriteString(nameof(buildTime), buildTime ?? "");
        {
            writer.WriteStartArray(nameof(bundleList));
            foreach (AssetBundleInfo bundleInfo in bundleList) {
                bundleInfo.Serialize(writer);
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
                case nameof(packageName): {
                    packageName = reader.ReadString();
                    break;
                }
                case nameof(packageVersion): {
                    packageVersion = reader.ReadString();
                    break;
                }
                case nameof(displayName): {
                    displayName = reader.ReadString();
                    break;
                }
                case nameof(description): {
                    description = reader.ReadString();
                    break;
                }
                case nameof(buildTime): {
                    buildTime = reader.ReadString();
                    break;
                }
                case nameof(bundleList): {
                    bundleList.Clear();
                    reader.ReadStartArray();
                    while (reader.ReadDsonType() != DsonType.EndOfObject) {
                        AssetBundleInfo bundleInfo = new AssetBundleInfo();
                        bundleInfo.Deserialize(reader);
                        bundleList.Add(bundleInfo);
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

    /// <summary>
    /// 编码为key为int的Dson结构
    /// (用于运行时)
    /// </summary>
    public void Serialize(IDsonWriter<int> writer) {
        writer.WriteStartObject();
        writer.WriteString(KEY_PACKAGE_NAME, packageName);
        writer.WriteString(KEY_PACKAGE_VERSION, packageVersion);
        // 可选写入
        if (!string.IsNullOrEmpty(displayName)) {
            writer.WriteString(KEY_DISPLAY_NAME, displayName);
        }
        if (!string.IsNullOrEmpty(description)) {
            writer.WriteString(KEY_DESCRIPTION, description);
        }
        if (!string.IsNullOrEmpty(buildTime)) {
            writer.WriteString(KEY_BUILD_TIME, buildTime);
        }
        writer.WriteInt32(KEY_BUNDLE_LIST_COUNT, bundleList.Count);
        if (bundleList.Count > 0) {
            writer.WriteStartArray(KEY_BUNDLE_LIST);
            foreach (AssetBundleInfo bundleInfo in bundleList) {
                bundleInfo.Serialize(writer);
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
                case KEY_PACKAGE_NAME: {
                    packageName = reader.ReadString();
                    break;
                }
                case KEY_PACKAGE_VERSION: {
                    packageVersion = reader.ReadString();
                    break;
                }
                case KEY_DISPLAY_NAME: {
                    displayName = reader.ReadString();
                    break;
                }
                case KEY_DESCRIPTION: {
                    description = reader.ReadString();
                    break;
                }
                case KEY_BUILD_TIME: {
                    buildTime = reader.ReadString();
                    break;
                }
                case KEY_BUNDLE_LIST_COUNT: {
                    bundleList.Clear();
                    int count = reader.ReadInt32();
                    if (count == 0) {
                        break;
                    }
                    bundleList.Capacity = count;
                    reader.ReadStartArray(KEY_BUNDLE_LIST);
                    while (reader.ReadDsonType() != DsonType.EndOfObject) {
                        AssetBundleInfo bundleInfo = new AssetBundleInfo();
                        bundleInfo.Deserialize(reader);
                        bundleList.Add(bundleInfo);
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
        // 尽量触发引用相等
        if (!string.IsNullOrEmpty(packageName)) {
            packageName = string.Intern(packageName);
        }
    }

    #endregion
}
}