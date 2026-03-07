#region LICENSE

// Copyright 2026 wjybxx(845740757@qq.com)
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
using Wjybxx.Dson.Codec.Attributes;

namespace Wjybxx.BigCat.Editor.Assetor.Classifiers
{
/// <summary>
/// 配置表分类器
/// </summary>
[DsonSerializable]
public class ConfigClassifier : IAssetClassifier
{
    /// <summary>
    /// 原始文件扩展名
    /// </summary>
    public HashSet<string> rawFileExtensions = new HashSet<string>();
    /// <summary>
    /// 忽略文件扩展名
    /// </summary>
    public HashSet<string> ignoreFileExtensions = new HashSet<string>();

    public EAssetCategory GetCategory(string assetPath) {
        string extension = UnityEditorUtil.GetExtension(assetPath);
        if (rawFileExtensions.Contains(extension)) {
            return EAssetCategory.RawFile;
        }
        if (ignoreFileExtensions.Contains(extension)) {
            return EAssetCategory.None;
        }
        return BuildUtil.IsEditorAsset(assetPath)
            ? EAssetCategory.None // 通常为文件夹和代码等
            : EAssetCategory.MainAsset;
    }
}
}