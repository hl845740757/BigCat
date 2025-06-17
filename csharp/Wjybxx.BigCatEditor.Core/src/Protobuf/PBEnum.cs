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
using System.Linq;

namespace Wjybxx.BigCatEditor.Protobuf
{
/// <summary>
/// 枚举类
/// </summary>
public class PBEnum : PBTypeElement
{
    /** 是否允许字段指向相同的数字 */
    private bool allowAlias = false;

    public override PBElementKind Kind => PBElementKind.Enum;

    /// <summary>
    /// 获取所有的枚举值
    /// </summary>
    /// <returns></returns>
    public List<PBEnumValue> GetEnumValues() {
        return EnclosedElements.Where(e => e.Kind == PBElementKind.EnumValue)
            .Cast<PBEnumValue>()
            .ToList();
    }

    public bool AllowAlias {
        get => allowAlias;
        set => allowAlias = value;
    }
}
}