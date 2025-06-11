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

namespace Wjybxx.BigCatEditor.Excel
{
/// <summary>
/// 表头
///
/// 如果是普通表，行号相同，列号不一定连续；
/// 如果是参数表，列号相同，行号不一定连续。
/// </summary>
public sealed class Header
{
    /** 字段选项 -- 格式自定义 */
    public readonly string options;
    /** 字段类型 eg：{@code int32} */
    public readonly string type;
    /** 字段名 eg: {@code  itemId} */
    public readonly string name;
    /** 字段注释 */
    public readonly string? comment;

    /** 定义name的行索引(0-based) */
    public readonly int rowIndex;
    /** 定义name的列索引(0-based) */
    public readonly int colIndex;

    public Header(string? options, string? type, string name, string? comment,
                  int rowIndex, int colIndex) {
        this.options = options ?? "";
        this.type = type ?? "";
        this.name = name ?? throw new ArgumentNullException(nameof(name));
        this.comment = comment;
        this.rowIndex = rowIndex;
        this.colIndex = colIndex;
    }

    public Header WithOptions(string options) => new Header(options, type, name, comment, rowIndex, colIndex);

    public Header WithType(string type) => new Header(options, type, name, comment, rowIndex, colIndex);

    public override string ToString() {
        return $"{nameof(options)}: {options}," +
               $" {nameof(name)}: {name}," +
               $" {nameof(type)}: {type}," +
               $" {nameof(comment)}: {comment}," +
               $" {nameof(rowIndex)}: {rowIndex}," +
               $" {nameof(colIndex)}: {colIndex}";
    }
}
}