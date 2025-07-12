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
using System.Text;

namespace Wjybxx.BigCatTool.Protobuf
{
/// <summary>
/// Message的字段
/// </summary>
public class PBField : PBElement
{
#nullable disable
    /** 字段类型 -- 包含泛型参数 */
    private string type;
    /** 数字id */
    private int number;
    /** 修饰符 */
    private readonly List<string> modifiers = new();
#nullable restore

    public override PBElementKind Kind => PBElementKind.Field;

    /// <summary>
    /// 是否是数组字段
    ///
    /// 吐槽：repeat是个糟糕的设计，List类型是更好的选择
    /// </summary>
    public bool IsRepeated => modifiers.Contains(PBKeywords.REPEATED);

    /// <summary>
    /// 是否是字典
    ///
    /// Map是另一种形式的repeated结构
    /// </summary>
    public bool IsMap => type.StartsWith("map");

    /// <summary>
    /// 添加修饰符
    /// </summary>
    public PBField AddModifier(string modifier) {
        this.modifiers.Add(modifier);
        return this;
    }

    /// <summary>
    /// 添加修饰符
    /// </summary>
    public PBField AddModifiers(List<string> modifiers) {
        this.modifiers.AddRange(modifiers);
        return this;
    }

    #region props

    public string Type {
        get => type;
        set => type = value;
    }
    public int Number {
        get => number;
        set => number = value;
    }

    public List<string> Modifiers => modifiers;

    #endregion

    protected override void ToString(StringBuilder sb) {
        if (modifiers.Count > 0) {
            sb.Append(", modifiers=");
            for (var idx = 0; idx < modifiers.Count; idx++) {
                if (idx > 0) sb.Append(' ');
                sb.Append(modifiers[idx]);
            }
        }
        sb.Append(", type='").Append(type).Append('\'')
            .Append(", number=").Append(number);
    }
}
}