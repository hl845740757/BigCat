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

namespace Wjybxx.BigCatEditor.Generator.Excel
{
/// <summary>
/// 常量值的类型
/// </summary>
public enum ConstKind
{
    Unknown = 0,
    Int32 = 1,
    Int64 = 2,
    Float = 3,
    Double = 4,
    Bool = 5,
    String = 6
}

/// <summary>
/// 常量值
///
/// 约定：如果字符串值不为null，则表示字符串值；否则表示数字值；
/// </summary>
public readonly struct ConstValue
{
    public readonly string name;
    public readonly ConstKind kind;
    public readonly double numValue;
    public readonly string? strValue;
    public readonly string? comment;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name">常量名</param>
    /// <param name="numValue">数字值</param>
    /// <param name="comment">注释</param>
    /// <param name="kind">常量值的类型</param>
    public ConstValue(string name, double numValue, string? comment, ConstKind kind = ConstKind.Int32) : this() {
        this.name = name;
        this.numValue = numValue;
        this.comment = comment;
        this.kind = kind;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name">常量名</param>
    /// <param name="strValue">字符串值</param>
    /// <param name="comment">注释</param>
    public ConstValue(string name, string? strValue, string? comment) : this() {
        this.name = name;
        this.strValue = strValue;
        this.comment = comment;
        this.kind = ConstKind.String;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name">常量名</param>
    /// <param name="boolValue">bool值</param>
    /// <param name="comment">注释</param>
    public ConstValue(string name, bool boolValue, string? comment) : this() {
        this.name = name;
        this.numValue = boolValue ? 1 : 0;
        this.comment = comment;
        this.kind = ConstKind.Bool;
    }

    public int IntVal => (int)numValue;
    public long LongVal => (long)numValue;
    public float FloatVal => (float)numValue;
    public double DoubleVal => numValue;
    public bool BoolVal => numValue != 0;

    public override string ToString() {
        return $"{nameof(name)}: {name}, {nameof(kind)}: {kind}, {nameof(numValue)}: {numValue}, {nameof(strValue)}: {strValue}, {nameof(comment)}: {comment}";
    }
}
}