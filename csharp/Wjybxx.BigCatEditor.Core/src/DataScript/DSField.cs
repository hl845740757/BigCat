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
using System.Text;

namespace Wjybxx.BigCatEditor.DataScript
{
/// <summary>
/// Class和Struct的字段
///
/// 最麻烦的就是解析字段中的泛型参数，可能类型定义的泛型变量
/// 1.需要考虑`T?`中的T是值类型的情况，需要转换为<see cref="Nullable{T}"/>
/// 2.需要考虑
/// </summary>
public class DSField : DSElement
{
#nullable disable
    /** 字段类型符号 -- 可能是 T? 类型 */
    private readonly string typeSymbol;
    /** 数字id */
    private readonly int number;
    /** 类型 -- 延迟解析 */
    private DSTypeElement type;

    /** 泛型字段的原始定义 */
    private readonly DSField _originDefine;
#nullable enable

    public DSField(string simpleName, string typeSymbol, int number)
        : base(simpleName) {
        this.typeSymbol = typeSymbol;
        this.number = number;
        this._originDefine = null;
    }

    public DSField(DSField originDefine, DSTypeElement type)
        : base(originDefine.SimpleName) {
        _originDefine = originDefine;
        this.number = originDefine.number;
        this.type = type;
    }

    public override DSElementKind Kind => DSElementKind.Field;
    public override DSField OriginDefine => _originDefine != null ? _originDefine : this;

#nullable disable

    #region props

    /// <summary>
    /// 字段的类型符号-只有文件定义的原始Element才有值 
    /// </summary>
    public string TypeSymbol => typeSymbol;

    /// <summary>
    /// 字段的数字
    /// </summary>
    public int Number => number;

    /// <summary>
    /// 字段类型缓存，延迟解析
    /// </summary>
    public DSTypeElement Type {
        get => type;
        set => type = value;
    }

    #endregion

    protected override void ToString(StringBuilder sb) {
        sb.Append(", typeSymbol='").Append(typeSymbol).Append('\'')
            .Append(", number=").Append(number);
    }
}
}