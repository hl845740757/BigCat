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

using UnityEditor;
using UnityEngine;
using Wjybxx.BigCat.Core;
using Wjybxx.Dson.Codec.Attributes;

namespace Wjybxx.BigCat.Editor.Assetor.Classifiers
{
/// <summary>
/// 将<see cref="SpriteGroup"/>定义为主资源，<see cref="Sprite"/>定义为依赖资源。
///
/// 注：目前为测试用。
/// </summary>
[DsonSerializable]
public class SpriteClassifier : IAssetClassifier
{
    public EAssetCategory GetCategory(string assetPath) {
        Object asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
        return asset switch
        {
            SpriteGroup => EAssetCategory.MainAsset,
            Texture or Sprite => EAssetCategory.DependAsset,
            _ => EAssetCategory.None
        };
    }
}
}