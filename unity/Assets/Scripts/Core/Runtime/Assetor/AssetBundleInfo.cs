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

namespace Wjybxx.BigCat.Assetor
{
/// <summary>
/// Bundle文件信息
/// </summary>
[Serializable]
public sealed class AssetBundleInfo
{
    /// <summary>
    /// bundle文件名
    ///
    /// 注：至少保证Manifest级别唯一。
    /// </summary>
    public string bundleName;
    /// <summary>
    /// Bundle类型
    /// </summary>
    public EBundleType bundleType;
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
    public long fileSize;
    /// <summary>
    /// 加密方式
    /// </summary>
    public int encryptType;

    /// <summary>
    /// bundle打包的起始文件夹路径
    ///
    /// 注：
    /// 1.同一个资产目录可以打出多个Bundle（Unity资产Bundle + 原始文件Bundle）。
    /// 2.由打包工具执行规格化。
    /// </summary>
    public string assetPath;
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
    /// 2.存储在这里和存储在Manifest中区别不大，但存储在这里会更直观。
    /// </summary>
    public List<AssetFileInfo> mainAssets = new List<AssetFileInfo>();
    /// <summary>
    /// bundle内资产索引方式
    /// </summary>
    public EAssetIndexes assetIndexes;

    /// <summary>
    /// 所属的资源包（用于运行时反向查询）
    /// </summary>
    [NonSerialized]
    public string packageName;
    /// <summary>
    /// bundleId
    ///
    /// 注：至少保证Manifest级别唯一。
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
}
}