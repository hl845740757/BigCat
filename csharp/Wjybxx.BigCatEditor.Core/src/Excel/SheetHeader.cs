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

namespace Wjybxx.BigCatEditor.Excel
{
/// <summary>
/// 表头
/// </summary>
public sealed class SheetHeader
{
    /** 命令和参数，格式：{@code cs -x -y} */
    public readonly string? cmd;
    /** 字段类型 eg：{@code int32} */
    public readonly string type;
    /** 字段名 eg: {@code  itemId} */
    public readonly string name;
    /** 字段注释 */
    public readonly string? comment;

    /** 定义name的行索引 */
    public readonly int rowIndex;
    /** 定义name的列索引 */
    public readonly int colIndex;

    public SheetHeader(string? cmd, string type, string name, string? comment,
                       int rowIndex, int colIndex) {
        this.cmd = cmd;
        this.name = name;
        this.type = type;
        this.comment = comment;
        this.rowIndex = rowIndex;
        this.colIndex = colIndex;
    }

    public override string ToString() {
        return $"{nameof(cmd)}: {cmd}," +
               $" {nameof(name)}: {name}," +
               $" {nameof(type)}: {type}," +
               $" {nameof(comment)}: {comment}," +
               $" {nameof(rowIndex)}: {rowIndex}," +
               $" {nameof(colIndex)}: {colIndex}";
    }
}
}