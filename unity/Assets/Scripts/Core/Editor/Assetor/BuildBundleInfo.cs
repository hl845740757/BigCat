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
using System.Linq;
using UnityEditor;
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
    /// 是否由外部提供（是否只参与模拟编译，但参与最终打包）
    /// </summary>
    public bool provided;

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
    public string unityHash = "";
    /// <summary>
    /// Unity生成的CRC(基于未压缩内容计算)
    /// </summary>
    public uint unityCRC;
    /// <summary>
    /// 文件哈希值(sha1)
    /// </summary>
    public string fileHash = "";
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
    /// 唯一索引信息
    /// </summary>
    public EAssetIndexes uniqueIndexes;
    /// <summary>
    /// 索引深度
    /// </summary>
    public int indexDepth;
    /// <summary>
    /// Bundle内资源
    /// </summary>
    public readonly List<BuildAssetInfo> assetList = new();

    /// <summary>
    /// 自身BundleId(需要保持稳定且包内唯一)
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
    /// 注：转换为文件名的时候，需要处理斜杠。
    /// </summary>
    public void InitBundleName() {
        bundleName = collectorType switch
        {
            ECollectorType.MainAsset => assetPath + "@main",
            ECollectorType.RawFile => assetPath + "@raw",
            _ => assetPath + "@dep"
        };
        bundleName = bundleName.ToLowerInvariant();
    }

    public AssetBundleInfo Build() {
        AssetBundleInfo bundleInfo = new AssetBundleInfo()
        {
            assetPath = assetPath.ToLowerInvariant(), // Path规格化
            bundleName = bundleName,
            bundleType = bundleType,
            collectPathLength = collectPath.Length,

            unityCRC = unityCRC,
            fileHash = fileHash,
            fileCRC = fileCRC,
            fileSize = fileSize,
            encrypted = encrypted,

            bundleTags = new List<string>(bundleTags),
            assetIndexes = assetIndexes | uniqueIndexes,
            assetIndexDepth = indexDepth,
            assetCount = assetList.Count,
            bundleId = bundleId,
            upstreamBundles = new List<int>(upstreamBundles),
        };
        if (collectorType == ECollectorType.MainAsset
            || collectorType == ECollectorType.RawFile) {
            bundleInfo.mainAssets.Capacity = assetList.Count;
            foreach (BuildAssetInfo assetInfo in assetList) {
                if (assetInfo.disableIndexes) continue;
                bundleInfo.mainAssets.Add(assetInfo.Build());
            }
        }
        return bundleInfo;
    }

    public AssetBundleBuild GetPipelineBuild() {
        return new AssetBundleBuild()
        {
            assetBundleName = bundleName,
            assetBundleVariant = "",
            assetNames = assetList.Select(e => e.assetPath).ToArray()
        };
    }
}
}