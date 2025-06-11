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
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;
using static Wjybxx.BigCatEditor.Excel.SheetConstants;

namespace Wjybxx.BigCatEditor.Excel
{
/// <summary>
/// 表单
///
/// 1.该抽象仅用于表格导出等工具，而不适用表格编辑器，表格编辑器需要更质朴的二维表抽象。
/// 2.该抽象不再用于运行时，因为我们设计为可变的，以允许工具动态修改数据。
/// 3.读表工具需要把所有的有name的列都读取进来，部分注释列我们也需要导出给翻译。
///
/// PS：其实也可以用于表格编辑器，但编辑器里不能修改元数据。
/// </summary>
public sealed class Sheet : IValueProvider
{
    /** 文件名字 如: bag.xlsx */
    public readonly string fileName;
    /** 页签名  如：bag */
    public readonly string sheetName;
    /** 页索引，默认应该为0 */
    public readonly int sheetIndex;
    /** 是否是参数表(纵表) */
    public readonly bool isParamSheet;

    /** 所有的表头信息 -- 参数表由内容行构建，属于缓存数据 */
    public readonly LinkedDictionary<string, Header> headers = new();
    /** 只包含内容部分 -- 因此第一个内容行的起始行号通常不是1；由于内容行可能不连续，使用二分查找 */
    public readonly List<SheetRow> valueRows = new();

    /// <summary>
    /// 建立一张空表
    /// </summary>
    public Sheet(string fileName, string sheetName, int sheetIndex, bool isParamSheet) {
        this.fileName = fileName;
        this.sheetName = sheetName;
        this.sheetIndex = sheetIndex;
        this.isParamSheet = isParamSheet;
    }

    /// <summary>
    /// 根据Excel等文件初始化Sheet
    /// </summary>
    public Sheet(string fileName, string sheetName, int sheetIndex, bool isParamSheet,
                 IList<Header> headers, IList<SheetRow> valueRows) {
        this.fileName = fileName;
        this.sheetName = sheetName;
        this.sheetIndex = sheetIndex;
        this.isParamSheet = isParamSheet;
        //
        if (isParamSheet && headers.Count == 0) {
            this.headers.AdjustCapacity(valueRows.Count);
            RefreshHeaders();
        } else {
            this.headers.AdjustCapacity(headers.Count);
            foreach (Header header in headers) {
                this.headers[header.name] = header;
            }
        }
        // 
        this.valueRows.AddRange(valueRows);
        this.valueRows.Sort((a, b) => a.RowIndex.CompareTo(b.RowIndex));
    }

    #region header-row

    /// <summary>
    /// 查询Header
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public Header? GetHeader(string name) {
        headers.TryGetValue(name, out Header header);
        return header;
    }

    /// <summary>
    /// 添加header
    /// (会覆盖旧值)
    /// </summary>
    /// <param name="header"></param>
    public void AddHeader(Header header) {
        headers[header.name] = header;
    }

    /// <summary>
    /// 删除Header
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public Header? RemoveHeader(string name) {
        headers.Remove(name, out Header header);
        return header;
    }

    /// <summary>
    /// 查询对应的行
    /// </summary>
    /// <param name="rowIndex">行标</param>
    /// <returns></returns>
    public SheetRow GetRow(int rowIndex) {
        int idx = CollectionUtil.BinarySearch(valueRows, midRow => midRow.RowIndex.CompareTo(rowIndex));
        if (idx < 0) {
            throw new IndexOutOfRangeException($"rowIndex:{rowIndex}");
        }
        return valueRows[idx];
    }

    /// <summary>
    /// 查询对应的行
    /// </summary>
    /// <param name="rowIndex">行标</param>
    /// <param name="row">out参数</param>
    /// <returns></returns>
    public bool TryGetRow(int rowIndex, out SheetRow? row) {
        int idx = CollectionUtil.BinarySearch(valueRows, midRow => midRow.RowIndex.CompareTo(rowIndex));
        if (idx < 0) {
            row = null;
            return false;
        }
        row = valueRows[idx];
        return true;
    }

    /// <summary>
    /// 添加行
    /// (可以是Insert)
    /// </summary>
    /// <param name="row"></param>
    public void AddRow(SheetRow row) {
        if (row == null) throw new ArgumentNullException(nameof(row));
        int idx = CollectionUtil.BinarySearch(valueRows, midRow => midRow.RowIndex.CompareTo(row.RowIndex));
        if (idx < 0) {
            idx = (idx + 1) * -1;
            valueRows.Insert(idx, row);
            return;
        }
        for (int nextIdx = idx; nextIdx < valueRows.Count; nextIdx++) {
            valueRows[nextIdx].RowIndex++;
        }
        valueRows.Insert(idx, row);
        if (isParamSheet) {
            RefreshHeaders();
        }
    }

    /// <summary>
    /// 删除行
    /// </summary>
    /// <param name="rowIndex"></param>
    public void RemoveRow(int rowIndex) {
        int idx = CollectionUtil.BinarySearch(valueRows, midRow => midRow.RowIndex.CompareTo(rowIndex));
        if (idx < 0) {
            return;
        }
        for (int nextIdx = idx + 1; nextIdx < valueRows.Count; nextIdx++) {
            valueRows[nextIdx].RowIndex--;
        }
        valueRows.RemoveAt(idx);
        if (isParamSheet) {
            RefreshHeaders();
        }
    }

    /// <summary>
    /// 清空行内容
    /// </summary>
    /// <param name="rowIndex"></param>
    public void ClearRow(int rowIndex) {
        SheetRow row = GetRow(rowIndex);
        row.Clear();
        if (isParamSheet) {
            RefreshHeaders();
        }
    }


    /// <summary>
    /// 刷新参数表的表头
    /// </summary>
    public void RefreshHeaders() {
        if (!isParamSheet) {
            return;
        }
        headers.Clear();
        foreach (SheetRow valueRow in valueRows) {
            Header header = TryCreateHeader(valueRow);
            if (header != null) {
                this.headers[header.name] = header;
            }
        }
    }

    /// <summary>
    /// 刷新指定行关联的header
    /// </summary>
    /// <param name="rowIndex"></param>
    public void RefreshHeader(int rowIndex) {
        if (!isParamSheet) {
            return;
        }
        SheetRow valueRow = GetRow(rowIndex);
        Header header = TryCreateHeader(valueRow);
        if (header != null) {
            this.headers[header.name] = header;
        }
    }

    #endregion

    #region cell

    /// <summary>
    /// 获取参数表的value
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public string? GetValue(string name) {
        if (!isParamSheet) {
            throw new IllegalStateException();
        }
        if (!headers.TryGetValue(name, out Header header)) {
            throw new ArgumentException($"param: {name} is absent");
        }
        SheetRow row = GetRow(header.rowIndex);
        return row.GetValue(COL_VALUE);
    }

    /// <summary>
    /// 设置参数表的value
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <exception cref="IllegalStateException"></exception>
    public void SetValue(string name, string? value) {
        if (!isParamSheet) {
            throw new IllegalStateException();
        }
        if (!headers.TryGetValue(name, out Header header)) {
            throw new ArgumentException($"param: {name} is absent");
        }
        SheetRow row = GetRow(header.rowIndex);
        row.SetValue(COL_VALUE, value);
    }

    /// <summary>
    /// 获取指定行列的值
    /// </summary>
    /// <param name="rowIndex">行下标</param>
    /// <param name="name">字段名</param>
    /// <returns></returns>
    public string? GetValue(int rowIndex, string name) {
        SheetRow row = GetRow(rowIndex);
        return row.GetValue(name);
    }

    /// <summary>
    /// 设置指定行列的值
    /// </summary>
    /// <param name="rowIndex">行下标</param>
    /// <param name="name">字段名</param>
    /// <param name="value">字段值</param>
    public void SetValue(int rowIndex, string name, string? value) {
        SheetRow row = GetRow(rowIndex);
        row.SetValue(name, value);
    }

    #endregion

    #region props

    /// <summary>
    /// 最小行号，0表示空表
    /// </summary>
    /// <value></value>
    public int MinLineNumber {
        get {
            int count = valueRows.Count;
            return count == 0 ? 0 : valueRows[0].LineNumber;
        }
    }

    /// <summary>
    /// 最大行号，0表示空表
    /// </summary>
    /// <value></value>
    public int MaxLineNumber {
        get {
            int count = valueRows.Count;
            return count == 0 ? 0 : valueRows[count - 1].LineNumber;
        }
    }

    #endregion

    public override string ToString() {
        return $"{nameof(fileName)}: {fileName}, {nameof(sheetName)}: {sheetName}, {nameof(sheetIndex)}: {sheetIndex}";
    }
}
}