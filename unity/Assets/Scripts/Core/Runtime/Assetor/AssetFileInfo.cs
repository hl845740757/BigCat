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
    /// 资产标签
    /// </summary>
    public List<string> assetTags = new List<string>();

    /// <summary>
    /// 所属的资源包（用于运行时反向查询）
    /// </summary>
    [NonSerialized]
    public string packageName;
    /// <summary>
    /// 归属的bundle
    /// </summary>
    [NonSerialized]
    public int bundleId;
    /// <summary>
    /// 依赖的bundle
    ///
    /// 注：用于细化加载粒度，加载文件时只加载必要的bundle -- 收益待评估。
    /// </summary>
    public List<int> upstreamBundles = new List<int>();
}
}