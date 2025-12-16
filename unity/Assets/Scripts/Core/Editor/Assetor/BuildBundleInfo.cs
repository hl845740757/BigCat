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

using System.Collections.Generic;
using Wjybxx.BigCat.Assetor;
using Wjybxx.Commons.Collections;

namespace Wjybxx.BigCat.Editor.Assetor
{
/// <summary>
/// 要构建的Bundle信息(资产分组结果)
/// </summary>
public sealed class BuildBundleInfo
{
    /// <summary>
    /// 关联文件夹
    /// </summary>
    public readonly string assetPath;
    /// <summary>
    /// 收集器绑定的地址（运行时计算索引）
    /// </summary>
    public string collectPath;
    /// <summary>
    /// 关联的收集器类型(影响BundleName和BundleId等计算)
    /// </summary>
    public ECollectorType collectorType;

    /// <summary>
    /// Bundle名
    /// </summary>
    public string bundleName;
    /// <summary>
    /// 要构建的Bundle类型
    /// </summary>
    public EBundleType bundleType;

    /// <summary>
    /// Unity生成的Hash(基于未压缩内容计算)
    /// </summary>
    public string unityHash;
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
    /// 加密方式(用户自行扩展)
    /// </summary>
    public int encrypted;

    /// <summary>
    /// 为Bundle附加的标签
    /// </summary>
    public List<string> bundleTags = new List<string>();
    /// <summary>
    /// 需要为Bundle建立的索引类型
    /// </summary>
    public EAssetIndexes assetIndexes;
    /// <summary>
    /// 索引深度
    /// </summary>
    public int indexDepth;
    /// <summary>
    /// Bundle内资源(需要支持快速删除)
    /// </summary>
    public List<BuildAssetInfo> assetList = new();

    /// <summary>
    /// 自身BundleId
    /// </summary>
    public int bundleId;
    /// <summary>
    /// 依赖的BundleId(只添加的情况下是有序的)
    /// </summary>
    public readonly HashSet<int> upstreamBundles = new();
    /// <summary>
    /// 依赖的Bundle名字
    /// </summary>
    public readonly HashSet<string> upstreamBundleNames = new();

    public BuildBundleInfo(string assetPath) {
        this.assetPath = assetPath;
    }

    /// <summary>
    /// 默认的BundleName计算规则
    /// </summary>
    public void InitBundleName() {
        bundleName = collectorType switch
        {
            ECollectorType.MainAsset => assetPath + "@main",
            ECollectorType.RawFile => assetPath + "@raw",
            _ => assetPath + "@dep"
        };
    }

    public AssetBundleInfo Build() {
        AssetBundleInfo bundleInfo = new AssetBundleInfo()
        {
            assetPath = assetPath,
            bundleName = bundleName,
            bundleType = bundleType,
            collectPath = collectPath,

            unityCRC = unityCRC,
            fileHash = fileHash,
            fileCRC = fileCRC,
            fileSize = fileSize,
            encrypted = encrypted,

            bundleTags = new List<string>(bundleTags),
            assetIndexes = assetIndexes,
            indexDepth = indexDepth,
            bundleId = bundleId,
            upstreamBundles = new List<int>(upstreamBundles),
        };
        if (collectorType == ECollectorType.MainAsset
            || collectorType == ECollectorType.RawFile) {
            //
            bundleInfo.mainAssets.Capacity = assetList.Count;
            foreach (BuildAssetInfo assetInfo in assetList) {
                bundleInfo.mainAssets.Add(assetInfo.Build());
            }
        }
        return bundleInfo;
    }
}
}