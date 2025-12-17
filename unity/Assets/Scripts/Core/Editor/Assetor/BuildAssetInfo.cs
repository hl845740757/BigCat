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
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;
using Wjybxx.BigCat.Assetor;

namespace Wjybxx.BigCat.Editor.Assetor
{
public sealed class BuildAssetInfo
{
    /// <summary>
    /// 资产路径，外部执行规格化
    /// </summary>
    public readonly string assetPath;
    /// <summary>
    /// 主资产类型
    /// </summary>
    public readonly Type assetType;
    /// <summary>
    /// 资产分类
    /// </summary>
    public readonly EAssetCategory category;

    /// <summary>
    /// 自定义索引
    /// </summary>
    public string address;
    /// <summary>
    /// 资产标签(不建议使用)
    /// </summary>
    public readonly List<string> assetTags = new List<string>();
    /// <summary>
    /// 归属的bundle(在构建时被剔除后为null)
    /// </summary>
    public BuildBundleInfo bundleInfo;
    /// <summary>
    /// 是否存在下游资产(是否被引用)
    /// </summary>
    public bool hasDownstreamAssets;

    public BuildAssetInfo(string assetPath, EAssetCategory category) {
        this.assetPath = assetPath;
        this.category = category;
        this.assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
    }

    /// <summary>
    /// 是否为图集资源
    /// </summary>
    /// <returns></returns>
    public bool IsSpriteAtlasAsset() => assetType == typeof(SpriteAtlas);

    /// <summary>
    /// 是否为Shader资源
    /// </summary>
    /// <returns></returns>
    public bool IsShaderAsset() => assetType == typeof(Shader)
                                   || assetType == typeof(ShaderVariantCollection);

    public AssetFileInfo Build() {
        return new AssetFileInfo()
        {
            assetPath = assetPath,
            address = address,
            assetTags = assetTags.Count > 0 ? assetTags.ToArray() : Array.Empty<string>()
        };
    }

    #region equals

    public override bool Equals(object obj) {
        return ReferenceEquals(this, obj);
    }

    public override int GetHashCode() {
        return (assetPath != null ? assetPath.GetHashCode() : 0);
    }

    public static bool operator ==(BuildAssetInfo left, BuildAssetInfo right) {
        return Equals(left, right);
    }

    public static bool operator !=(BuildAssetInfo left, BuildAssetInfo right) {
        return !Equals(left, right);
    }

    #endregion
}
}