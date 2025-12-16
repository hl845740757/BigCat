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

namespace Wjybxx.BigCat.Editor.Assetor
{
public class BuildPackageInfo
{
    /// <summary>
    /// 要构建的资源包名
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
    /// </summary>
    public string buildTime;

    /// <summary>
    /// 资产路径到资产的映射
    /// </summary>
    public Dictionary<string, BuildAssetInfo> assetDic = new(10000);
    /// <summary>
    /// Name到Bundle的字典缓存
    /// </summary>
    public Dictionary<string, BuildBundleInfo> name2BundleDic = new(1000);
    /// <summary>
    /// Id到Bundle的字典缓存(id至少package内唯一)
    /// </summary>
    public Dictionary<int, BuildBundleInfo> id2BundleDic = new(1000);

    public AssetPackageInfo Build() {
        AssetPackageInfo packageInfo = new AssetPackageInfo
        {
            packageName = packageName,
            packageVersion = packageVersion,
            displayName = displayName,
            description = description,
            buildTime = buildTime
        };
        packageInfo.bundleList.Capacity = name2BundleDic.Count;
        foreach (BuildBundleInfo bundleInfo in name2BundleDic.Values) {
            packageInfo.bundleList.Add(bundleInfo.Build());
        }
        return packageInfo;
    }
}
}