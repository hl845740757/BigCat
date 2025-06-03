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

using System;
using System.Collections.Generic;
using System.Text;
using Wjybxx.BigCatEditor.Excel;
using Wjybxx.Commons;
using Wjybxx.Dson;
using static Wjybxx.BigCatEditor.Generator.Excel.SheetConstants;

namespace Wjybxx.BigCatEditor.Generator.Excel
{
/// <summary>
/// 合并数组和字典单元格
///
/// 数组和字典可以拆分为多列进行配置，并通过'name#index'表示对应下标的元素；
/// 我们在导表时，需要将散落的列收集起来，合并到元数据列。
///
/// <h3>规则</h3>
/// 1.必须显式定义元数据列，避免隐式数据列。
/// 2.在存在辅助列的情况下，元数据列不配置数据，辅助列可以不配置类型 -- 通常可以自动根据元数据列推断。
/// 3.options中<code>isRecord</code>表达当前数组是否是Record类型（定长数组）。
/// 如果record类型，则会将所有列合并，否则只合并非空白列。
/// 4.record合并时，空单元格默认转换null(默认值) -- 建议显式填充默认值，避免奇怪语义。
/// </summary>
public class SheetCellMerger : ISheetProcessor
{
    private readonly SheetRepository _repository;
    private readonly bool _checkOriginalCell;

#nullable disable
    private readonly StringBuilder _sb = new StringBuilder(100);
    /// <summary>
    /// 当前正在处理的表单
    /// </summary>
    private Sheet _sheet;
#nullable enable

    /// <summary>
    /// 
    /// </summary>
    /// <param name="repository">要处理的文件</param>
    /// <param name="checkOriginalCell">是否检测原始单元格的值</param>
    public SheetCellMerger(SheetRepository repository, bool checkOriginalCell = true) {
        _repository = repository;
        _checkOriginalCell = checkOriginalCell;
    }

    public void Execute() {
        foreach (Sheet sheet in _repository.SheetMap.Values) {
            _sheet = sheet;
            try {
                foreach (Header header in _sheet.headers.Values) {
                    if (!header.name.EndsWith("#1")) {
                        continue;
                    }
                    // 主字段名--必须显式定义主字段
                    string fieldName = header.name.Substring(0, header.name.Length - 2);
                    Header? fieldHeader = _sheet.GetHeader(fieldName);
                    if (fieldHeader == null) {
                        throw new Exception($"the field {fieldName} of {header.name} is absent");
                    }
                    // 收集所有列名 -- 注意，配置表中索引1开始
                    bool isMapType = IsMapType(fieldHeader.type);
                    List<Header> elemHeaders = new List<Header>();
                    for (int index = 1; index <= ELEMENT_LIMIT; index++) {
                        Header? elemHeader = _sheet.GetHeader(fieldName + "#" + index);
                        if (elemHeader == null) {
                            break;
                        }
                        if (isMapType && !string.IsNullOrWhiteSpace(elemHeader.type) && !IsPairType(elemHeader.type)) {
                            throw new Exception($"the filed {fieldName} is map type, but the element {elemHeader.name} is not pair type");
                        }
                        elemHeaders.Add(elemHeader);
                    }
                    if (sheet.isParamSheet) {
                        MergeParamSheetField(fieldHeader, elemHeaders);
                    } else {
                        MergeNormalSheetField(fieldHeader, elemHeaders);
                    }
                }
            }
            catch (Exception ex) {
                throw new Exception($"sheetName: {sheet.sheetName}", ex);
            }
        }
    }

    private void MergeNormalSheetField(Header fieldHeader, List<Header> elemHeaders) {
        string fieldName = fieldHeader.name;
        DsonObject<string> options = ParseOptions(fieldHeader.options);
        List<string> values = new List<string>(elemHeaders.Count);
        if (GetBool(options, KEY_IS_RECORD)) {
            // 合并所有列的值
            foreach (SheetRow sheetRow in _sheet.valueRows) {
                CheckOriginalCell(sheetRow, fieldName);
                values.Clear();
                foreach (Header elemHeader in elemHeaders) {
                    string? value = sheetRow.GetValue(elemHeader.name);
                    MergeRecordValue(value, values, elemHeader);
                }
                string mergedValue = ToStringValue(fieldHeader, values);
                sheetRow.SetValue(fieldName, mergedValue);
            }
        } else {
            // 合并非空白列，遇见空白列中断
            foreach (SheetRow sheetRow in _sheet.valueRows) {
                CheckOriginalCell(sheetRow, fieldName);
                values.Clear();
                foreach (Header elemHeader in elemHeaders) {
                    string? value = sheetRow.GetValue(elemHeader.name);
                    if (string.IsNullOrWhiteSpace(value)) {
                        break;
                    }
                    values.Add(value);
                }
                string mergedValue = ToStringValue(fieldHeader, values);
                sheetRow.SetValue(fieldName, mergedValue);
            }
        }
    }

    private void CheckOriginalCell(SheetRow sheetRow, string fieldName) {
        if (_checkOriginalCell && !string.IsNullOrWhiteSpace(sheetRow.GetValue(fieldName))) {
            throw new Exception($"the original field value must be empty, fieldName: {fieldName}");
        }
    }

    private void MergeParamSheetField(Header fieldHeader, List<Header> elemHeaders) {
        string fieldName = fieldHeader.name;
        if (_checkOriginalCell && !string.IsNullOrWhiteSpace(_sheet.GetValue(fieldName))) {
            throw new Exception($"the original field value must be empty, fieldName: {fieldName}");
        }

        DsonObject<string> options = ParseOptions(fieldHeader.options);
        List<string> values = new List<string>(elemHeaders.Count);
        if (GetBool(options, KEY_IS_RECORD)) {
            // 合并所有行的值
            foreach (Header elemHeader in elemHeaders) {
                string? value = _sheet.GetValue(elemHeader.name);
                MergeRecordValue(value, values, elemHeader);
            }
        } else {
            // 合并非空白行，遇见空白行中断
            foreach (Header elemHeader in elemHeaders) {
                string? value = _sheet.GetValue(elemHeader.name);
                if (string.IsNullOrWhiteSpace(value)) {
                    break;
                }
                values.Add(value);
            }
        }
        string mergedValue = ToStringValue(fieldHeader, values);
        _sheet.SetValue(fieldName, mergedValue);
    }

    private static void MergeRecordValue(string? value, List<string> values, Header elemHeader) {
        if (!string.IsNullOrWhiteSpace(value)) {
            values.Add(value);
            return;
        }
        // 字符串类型保留原值
        if (IsStringType(elemHeader.type)) {
            values.Add(value ?? "");
        } else {
            values.Add(GetDefaultValue(elemHeader.type));
        }
    }

    private string ToStringValue(Header fieldHeader, List<string> values) {
        StringBuilder sb = _sb;
        sb.Clear();
        if (IsMapType(fieldHeader.type)) {
            // 由于配置表中字典的key都是简单类型（int32，int64, string），因此我们合并为Object类型，而不是Array类型
            // {k1: v1} {k2: v2}  => {k1: v1, k2: v2}
            sb.Append('{');
            for (var i = 0; i < values.Count; i++) {
                if (i > 0) {
                    sb.Append(',');
                }
                string value = values[i].Trim();
                if (value[0] != '{' || value[value.Length - 1] != '}') {
                    throw new Exception($"field: {fieldHeader.name}, invalid element: {value}");
                }
                sb.Append(value.Substring2(1, value.Length - 1));
            }
            sb.Append('}');
        } else {
            // List直接合并即可
            sb.Append('[');
            for (var i = 0; i < values.Count; i++) {
                if (i > 0) {
                    sb.Append(',');
                }
                string value = values[i].Trim();
                sb.Append(value);
            }
            sb.Append(']');
        }
        return sb.ToString();
    }
}
}