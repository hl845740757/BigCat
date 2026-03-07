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

namespace Wjybxx.BigCat.Editor.Assetor
{
/// <summary>
/// 资产分类器
/// </summary>
public interface IAssetClassifier
{
    /// <summary>
    /// 计算资产的类别
    ///
    /// 注意：尽量使用<see cref="AssetDatabase.GetMainAssetTypeAtPath"/>以避免将资产加载到内存。
    /// </summary>
    /// <returns></returns>
    EAssetCategory GetCategory(string assetPath);
}
}