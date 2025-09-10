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
using UnityEngine;

namespace Wjybxx.BigCat.UnityCore
{
/// <summary>
/// 资产引用属性（支持数组）
///
/// 注意：只可用于string和List[string]类型。
/// TODO 资产类型限制，感觉使用一个值类型封装更容易实现编辑器...
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class AssetReferenceAttribute : PropertyAttribute
{
    public readonly Type assetType;
    public readonly bool useGuid;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="assetType">资产对象类型</param>
    /// <param name="useGuid">是否保存为guid</param>
    public AssetReferenceAttribute(Type assetType, bool useGuid = false) {
        this.assetType = assetType;
        this.useGuid = useGuid;
    }
}
}