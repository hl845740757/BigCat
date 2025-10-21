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

namespace Wjybxx.BigCat.Core
{
/// <summary>
/// 资产引用属性（支持数组）
///
/// 注意：
/// 1.只可用于string和List[string]类型。
/// 2.默认情况下，对数据对象的引用是guid，资产对象的引用是path。
/// 3.资产的guid是Unity分配的，并且运行时非用户管理的，因此使用Path。
/// 4.数据的guid是用户分配的，并且运行时是用户管理的，因此使用guid。
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class AssetReferenceAttribute : PropertyAttribute
{
    public readonly Type assetType;
    public readonly AssetReferenceMode mode;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="assetType">资产对象类型</param>
    /// <param name="mode">引用模式</param>
    public AssetReferenceAttribute(Type assetType, AssetReferenceMode mode = AssetReferenceMode.Path) {
        this.assetType = assetType;
        this.mode = mode;
    }
}

/// <summary>
/// 资产引用模式
/// </summary>
public enum AssetReferenceMode
{
    Path = 0, // 按资产路径引用；如果目标资产包含preferName属性，则根据preferName属性决定
    Name = 1, // 按资产文件名引用，当某类资产具有规则唯一命名时可使用
    Guid = 2, // 按Unity资产guid引用，适用于静态资源引用
}
}