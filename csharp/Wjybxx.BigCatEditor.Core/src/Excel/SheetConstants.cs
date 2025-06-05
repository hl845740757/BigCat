#region LICENSE

// Copyright 2023 wjybxx(845740757@qq.com)
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

using Wjybxx.Commons.Collections;

namespace Wjybxx.BigCatEditor.Excel
{
/// <summary>
/// 表单常量
/// </summary>
public static class SheetConstants
{
    public const string COL_OPTIONS = "options";
    public const string COL_TYPE = "type";
    public const string COL_NAME = "name";
    public const string COL_VALUE = "value";
    public const string COL_COMMENT = "comment";

    /// <summary>
    /// 参数表的列
    ///
    /// PS：虽然定义为普通表的转置看似更规范，但只配置单值的情况下，commit放在value前面的体验并不好。
    /// </summary>
    public static readonly ImmutableList<string> PARAM_SHEET_COLS = new[]
    {
        COL_OPTIONS,
        COL_TYPE,
        COL_NAME,
        COL_VALUE,
        COL_COMMENT,
    }.ToImmutableList2();

    /// <summary>
    /// 尝试通过参数表的内容行创建Header
    ///
    /// 注意：只要name存在即可创建header，因为在允许数组和字典元素拆分配置的情况下，数组元素列可以只有名字。
    /// </summary>
    /// <param name="valueRow">内容行</param>
    /// <param name="nameColIndex">name的列索引</param>
    /// <returns></returns>
    public static Header? TryCreateHeader(SheetRow valueRow, int nameColIndex = 2) {
        string? options = valueRow.GetValue(COL_OPTIONS);
        string? type = valueRow.GetValue(COL_TYPE);
        string name = valueRow.GetValue(COL_NAME);
        string? comment = valueRow.GetValue(COL_COMMENT);
        if (!string.IsNullOrWhiteSpace(name)) {
            return new Header(options, type, name, comment, valueRow.RowIndex, nameColIndex);
        }
        return null;
    }
}
}