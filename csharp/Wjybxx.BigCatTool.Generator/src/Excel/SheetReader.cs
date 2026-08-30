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
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using ExcelDataReader;
using Wjybxx.BigCatTool.Excel;
using Wjybxx.Commons;
using static Wjybxx.BigCatTool.Excel.SheetConstants;

namespace Wjybxx.BigCatTool.Generator.Excel
{
/// <summary>
/// 将Excel中的表单读取为<see cref="Sheet"/>类型
///
/// 1.Reader处于开发期工具，不根据表格的C/S选择要读取的列，而是会将所有定义了Name的列都读取到内存。
/// 2.表格内容之间不可插入空白行，否则Reader将难以判断表格的结束行；根本问题在于Excel表向下和向右拉动便会创建大量的空白行。
/// 3.要想插入注释行（分隔行），第一列（id列）的值需要以'#'开头，表示当前行为注释行，还需要继续读取；
/// 4.读表仅仅是读表，不校验表格内容的正确性，校验需要额外的Validator实现。
///
/// 再吐槽一下，C#的系统库抽象做得真垃圾...
/// </summary>
internal class SheetReader
{
    /** 普通表的表头行数 */
    private const int HEADER_ROW_COUNT = 4;
    /** 字段名的正则表达式 - 数组和字典支持#index语法；不需要那么严格的匹配 */
    private static readonly Regex fieldNameRegex = new("^[a-zA-Z_][a-zA-Z0-9_#]*$", RegexOptions.Compiled);

    private readonly string fileName;
    private readonly string sheetName;
    private readonly int sheetIndex;
    private readonly ExcelReaderOptions options;

    private readonly int rowCount;
    private readonly int colCount;
    private readonly IExcelDataReader reader;

    public SheetReader(string fileName, string sheetName, int sheetIndex, ExcelReaderOptions options,
                       IExcelDataReader reader) {
        this.fileName = fileName;
        this.sheetName = sheetName;
        this.sheetIndex = sheetIndex;
        this.options = options;
        this.reader = reader;

        this.colCount = reader.FieldCount;
        this.rowCount = reader.RowCount;
    }

    /// <summary>
    /// 如果是非业务表，则返回null
    ///
    /// 依赖业务
    /// </summary>
    /// <returns></returns>
    public Sheet? Read() {
        SkipRowsUtil(reader, options.skipRows);
        if (reader.Depth != options.skipRows) {
            return null; // 行数不足或不符合规范
        }
        List<string> firstRowValues = GetRowValues(reader, true);
        SheetContent sheetContent;
        bool isParamSheet = IsParamSheet(firstRowValues);
        if (isParamSheet) {
            sheetContent = ReadParamSheet(firstRowValues);
        } else {
            if (rowCount < options.skipRows + HEADER_ROW_COUNT) {
                return null; // 非业务表格
            }
            List<string> typeRow = ReadRowValues(reader, trim: true)!;
            List<string> nameRow = ReadRowValues(reader, trim: true)!;
            List<string> commentRow = ReadRowValues(reader, trim: true)!;
            if (!IsNormalSheet(nameRow)) {
                return null;
            }
            sheetContent = ReadNormalSheet(firstRowValues, typeRow, nameRow, commentRow);
        }
        SheetType sheetType = isParamSheet ? SheetType.Param : SheetType.Normal;
        return new Sheet(fileName, sheetName, sheetIndex, sheetType,
            sheetContent.headers, sheetContent.valueRowList);
    }

    #region util

    private static List<string>? ReadRowValues(IExcelDataReader reader, bool trim = false) {
        if (!reader.Read()) {
            return null;
        }
        return GetRowValues(reader, trim);
    }

    private static List<string> GetRowValues(IExcelDataReader row, bool trim = false, List<string>? outList = null) {
        int fieldCount = row.FieldCount;
        if (outList == null) {
            outList = new List<string>(fieldCount);
        } else {
            outList.Clear();
            outList.EnsureCapacity(fieldCount);
        }
        for (int colIndex = 0; colIndex < fieldCount; colIndex++) {
            string cellValue = (string)row.GetValue(colIndex) ?? ""; // 定制ExcelDataReader，总是返回原始字符串
            if (trim) {
                cellValue = cellValue.Trim();
            }
            outList.Add(cellValue);
        }
        return outList;
    }

    /// <summary>
    /// 是否是参数表（纵表）
    /// 当第一行包含参数表需要的所有列名时有效
    /// </summary>
    /// <param name="firstRowValues"></param>
    /// <returns></returns>
    private static bool IsParamSheet(List<string> firstRowValues) {
        foreach (string value in PARAM_SHEET_COLS) {
            if (!firstRowValues.Contains(value)) {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// 是否是普通表（横表）
    /// 当第三行都是有效的字段名时有效
    /// </summary>
    /// <param name="thirdRowValues"></param>
    /// <returns></returns>
    private static bool IsNormalSheet(List<string> thirdRowValues) {
        if (thirdRowValues.Count == 0) {
            return false;
        }
        int nameCount = 0;
        foreach (string value in thirdRowValues) {
            if (string.IsNullOrWhiteSpace(value)) continue;
            if (!fieldNameRegex.IsMatch(value.Trim())) return false;
            nameCount++;
        }
        return nameCount > 0;
    }

    /// <summary>
    /// 跳转到第N行
    /// </summary>
    /// <param name="reader">reader</param>
    /// <param name="expected">期望的行索引(0-based)</param>
    /// <exception cref="InvalidOperationException"></exception>
    private static void SkipRowsUtil(IExcelDataReader reader, int expected) {
        while (reader.Read()) {
            int rowIndex = reader.Depth;
            if (rowIndex == expected) {
                return;
            }
            if (rowIndex > expected) {
                throw new InvalidOperationException(
                    $"The number of rows is not continuous, expected: {expected}, found: {rowIndex}");
            }
        }
    }

    #endregion

    private struct SheetContent
    {
        internal readonly List<Header> headers;
        internal readonly List<SheetRow> valueRowList;

        public SheetContent(List<Header> headers, List<SheetRow> valueRowList) {
            this.headers = headers;
            this.valueRowList = valueRowList;
        }
    }

    private static bool IsBlankLine(List<string> rowValues) {
        if (rowValues.Count == 0) return true;
        foreach (string value in rowValues) {
            if (!string.IsNullOrWhiteSpace(value)) return false;
        }
        return true;
    }

    /// <summary>
    /// 读取参数表
    /// </summary>
    /// <param name="firstRowValues"></param>
    /// <returns></returns>
    private SheetContent ReadParamSheet(List<string> firstRowValues) {
        int argsColIndex = firstRowValues.IndexOf(COL_OPTIONS);
        int nameColIndex = firstRowValues.IndexOf(COL_NAME);
        int typeColIndex = firstRowValues.IndexOf(COL_TYPE);
        int valueColIndex = firstRowValues.IndexOf(COL_VALUE);
        int commentColIndex = firstRowValues.IndexOf(COL_COMMENT);

        List<Header> headers = new List<Header>();
        List<SheetRow> valueRowList = new(rowCount - options.skipRows);

        HashSet<string> nameSet = new HashSet<string>();
        List<string> rowValues = new List<string>(colCount);
        while (reader.Read()) {
            GetRowValues(reader, trim: false, rowValues);
            string args = rowValues[argsColIndex].Trim();
            string name = rowValues[nameColIndex].Trim();
            string type = rowValues[typeColIndex].Trim();
            string value = rowValues[valueColIndex]; // value不可以trim
            string comment = rowValues[commentColIndex].Trim();

            if (string.IsNullOrWhiteSpace(name)) { // 注释行
                continue;
            }
            if (!nameSet.Add(name)) { // 参数名不可以重复
                throw new IOException($"the name is duplicate, name: {name}");
            }
            // 保留原始行数据
            SheetRow sheetRow = new SheetRow(reader.Depth);
            sheetRow.SetValue(COL_OPTIONS, args);
            sheetRow.SetValue(COL_TYPE, type);
            sheetRow.SetValue(COL_NAME, name);
            sheetRow.SetValue(COL_VALUE, value);
            sheetRow.SetValue(COL_COMMENT, comment);
            valueRowList.Add(sheetRow);

            // 保留原始的行列索引
            Header header = TryCreateHeader(sheetRow, nameColIndex);
            headers.Add(header);
        }
        return new SheetContent(headers, valueRowList);
    }

    /// <summary>
    /// 读取普通表（横表）
    /// </summary>
    /// <param name="argsRow">选项行</param>
    /// <param name="typeRow">type行</param>
    /// <param name="nameRow">name行</param>
    /// <param name="commentRow">注释行</param>
    /// <returns></returns>
    private SheetContent ReadNormalSheet(List<string> argsRow,
                                         List<string> typeRow,
                                         List<string> nameRow,
                                         List<string> commentRow) {
        // 读取Header
        List<Header> headers = new List<Header>(nameRow.Count);
        int nameRowIndex = options.skipRows + 2;

        HashSet<string> nameSet = new HashSet<string>();
        List<Header?> indexedHeaders = new List<Header?>(nameRow.Count);
        for (int colIndex = 0; colIndex < nameRow.Count; colIndex++) {
            indexedHeaders.Add(null);

            string args = argsRow[colIndex].Trim();
            string name = nameRow[colIndex].Trim();
            string type = typeRow[colIndex].Trim();
            string comment = commentRow[colIndex].Trim();
            if (string.IsNullOrWhiteSpace(name)) { // 注释行
                continue;
            }
            if (!nameSet.Add(name)) { // 字段名不可以重复
                throw new IOException($"the name is duplicate, name: {name}");
            }
            Header header = new Header(args, type, name, comment, nameRowIndex, colIndex);
            headers.Add(header);
            indexedHeaders[colIndex] = header;
        }
        // 读取内容行
        List<SheetRow> valueRowList = new(rowCount - options.skipRows);
        List<string> rowValues = new List<string>(colCount);
        HashSet<string> primaryKeySet = new HashSet<string>();
        while (reader.Read()) {
            GetRowValues(reader, trim: false, rowValues);
            if (string.IsNullOrWhiteSpace(rowValues[0]) // id为空表示注释行
                || rowValues[0].StartsWith(options.commentLinePrefix)) { // 注释行（分隔行）
                continue;
            }
            string primaryKey = rowValues[0]; // 主键不可以重复 -- 这对于工具来说十分重要
            if (!primaryKeySet.Add(primaryKey)) {
                throw new IOException("the value of first column cant be duplicate, lineNumber: " + (reader.Depth + 1));
            }

            SheetRow sheetRow = new SheetRow(reader.Depth);
            for (int colIndex = 0; colIndex < nameRow.Count; colIndex++) {
                Header? header = indexedHeaders[colIndex];
                if (header == null) continue;
                sheetRow.SetValue(header.name, rowValues[colIndex]);
            }
            valueRowList.Add(sheetRow);
        }
        return new SheetContent(headers, valueRowList);
    }
}
}