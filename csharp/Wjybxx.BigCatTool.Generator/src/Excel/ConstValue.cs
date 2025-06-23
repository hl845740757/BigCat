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

namespace Wjybxx.BigCatTool.Generator.Excel
{
/// <summary>
/// 常量值的类型
/// </summary>
public enum ConstKind
{
    Int32 = 0,
    Int64 = 1,
    Float = 2,
    Double = 3,
    Bool = 4,
    String = 5
}

/// <summary>
/// 常量值
///
/// 约定：如果字符串值不为null，则表示字符串值；否则表示数字值；
/// </summary>
public readonly struct ConstValue
{
    public readonly ConstKind kind;
    public readonly string name;
    public readonly string value; // 我们尽可能保留表格中的原始字符串，这可以避免大量的问题
    public readonly string? comment;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="kind">常量值的类型</param>
    /// <param name="name">常量名</param>
    /// <param name="value">常量值</param>
    /// <param name="comment">注释</param>
    public ConstValue(ConstKind kind, string name, string value, string? comment) {
        this.kind = kind;
        this.name = name ?? throw new ArgumentNullException(nameof(name));
        this.value = value;
        this.comment = comment;
    }

    public override string ToString() {
        return $"{nameof(kind)}: {kind}, {nameof(name)}: {name}, {nameof(value)}: {value}, {nameof(comment)}: {comment}";
    }
}
}