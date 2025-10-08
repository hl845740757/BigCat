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

namespace Wjybxx.BigCatTool.DataScript
{
/// <summary>
/// Class和Struct的字段
///
/// 1.字段可以使用类型的泛型参数。
/// 2.字段如果是值类型，且加了'?'修饰，会被转换为<see cref="Nullable{T}"/> -- 与C#语法一致。
/// 3.访问内部类时，如果不是直接内部类，需要使用A.B.C相对路径格式 -- 确保精确解析。
/// 4.字段可以使用readonly修饰，工具在生成csharp或其它语言代码时，应当响应字段的readonly诉求，在构造函数中解码字段。
/// </summary>
public class DSField : DSElement
{
#nullable disable
    /** 字段类型符号 -- 可能是'T?'类型 */
    private readonly string typeSymbol;
    /** 数字id */
    private readonly int number;
    /** 字段是否是只读的 -- 对字段来说，还是修饰符好用 */
    private readonly bool isReadonly;
    /** 是否是可重复字段 - 即数组 */
    private bool isRepeated;
    /** 类型 -- build时解析 */
    private DSTypeElement type;

    /** 泛型字段的原始定义 */
    private readonly DSField _originDefine;
#nullable restore

    public DSField(string simpleName, string typeSymbol, int number, bool isReadonly = false)
        : base(simpleName) {
        this.typeSymbol = typeSymbol;
        this.number = number;
        this.isReadonly = isReadonly;
        this._originDefine = null;
    }

    public DSField(DSField originDefine, DSTypeElement type)
        : base(originDefine.SimpleName) {
        _originDefine = originDefine;
        this.number = originDefine.number;
        this.isReadonly = originDefine.isReadonly;
        this.isRepeated = originDefine.IsRepeated;
        this.type = type;
    }

    public override DSElementKind Kind => DSElementKind.Field;
    public override DSElement OriginDefine => _originDefine ?? this;
    public DSField OriginField => _originDefine ?? this;
    public new DSNamedType EnclosingElement => (DSNamedType)base.EnclosingElement;
#nullable disable

    #region props

    /// <summary>
    /// 字段的类型符号-只有文件定义的原始Element才有值 
    /// </summary>
    public string TypeSymbol => typeSymbol;

    /// <summary>
    /// 字段的数字，推荐1开始
    /// </summary>
    public int Number => number;

    /// <summary>
    /// 字段是否是只读的
    /// </summary>
    public bool IsReadonly => isReadonly;

    /// <summary>
    /// 字段类型缓存，延迟解析
    /// </summary>
    public DSTypeElement Type {
        get => type;
        set => type = value;
    }

    /// <summary>
    /// 是否是可重复字段(数组字段)
    /// 
    /// 注：该属性服务于List和Map结构，一般业务不应该使用该属性。
    /// </summary>
    public bool IsRepeated {
        get => false;
        internal set => isRepeated = value;
    }

    #endregion

    protected override void ToString(StringBuilder sb) {
        sb.Append(", typeSymbol='").Append(typeSymbol).Append('\'')
            .Append(", number=").Append(number);
    }
}
}