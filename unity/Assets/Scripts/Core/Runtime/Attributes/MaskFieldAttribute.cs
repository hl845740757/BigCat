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
using Wjybxx.Commons;

namespace Wjybxx.BigCat.Core
{
/// <summary>
/// 将int字段标记可为Mask字段
/// </summary>
public class MaskFieldAttribute : PropertyAttribute
{
    public readonly string[] displayNames;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="bitCount">有效Bit数量</param>
    public MaskFieldAttribute(int bitCount) {
        bitCount = Mathf.Clamp(bitCount, 0, 32);
        this.displayNames = bitCount == 32 ? intDisplayNames : ArrayUtil.CopyOf(intDisplayNames, 0, bitCount);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="displayNames">每个Bit的展示名</param>
    public MaskFieldAttribute(string[] displayNames) {
        this.displayNames = displayNames ?? throw new ArgumentNullException(nameof(displayNames));
    }

    private static readonly string[] intDisplayNames = new string[32];

    static MaskFieldAttribute() {
        for (int i = 0; i < intDisplayNames.Length; i++) {
            intDisplayNames[i] = i.ToString();
        }
    }
}
}