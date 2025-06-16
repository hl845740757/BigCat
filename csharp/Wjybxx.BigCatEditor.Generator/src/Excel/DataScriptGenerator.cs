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
using System.IO;
using System.Linq;
using Wjybxx.BigCatEditor.Core;
using Wjybxx.BigCatEditor.DataScript;
using Wjybxx.BigCatEditor.Excel;
using Wjybxx.Dson;
using static Wjybxx.BigCatEditor.Generator.Excel.ExcelConstants;

namespace Wjybxx.BigCatEditor.Generator.Excel
{
/// <summary>
/// 根据Excel生成对应的DataScript文件
///
/// 将表格数据追加到文本模板尾部即可
/// </summary>
public class DataScriptGenerator : ISheetProcessor
{
    private readonly SheetRepository _repository;
    private readonly FileInfo _templateFile;
    private readonly string _outPath;
    private readonly RequireMode _requireMode;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="repository">文件仓库</param>
    /// <param name="templateFile">模板文件</param>
    /// <param name="outPath">输出路径；如果'.ds'结尾，表示输出为单个文件；单文件有利于import</param>
    /// <param name="requireMode">导出模式</param>
    public DataScriptGenerator(SheetRepository repository, FileInfo templateFile, string outPath, RequireMode requireMode) {
        _templateFile = templateFile;
        _repository = repository;
        _outPath = outPath;
        _requireMode = requireMode;
    }

    public void Execute() {
        if (!_templateFile.Exists) {
            throw new IOException("Template file doesn't exist.");
        }
        string[] fileHeaders = File.ReadAllLines(_templateFile.FullName);
        List<string> lines = new List<string>(100);
        HashSet<string> generatedSheets = new HashSet<string>();
        // 初始化文件头
        bool singleFileMode = _outPath.EndsWith(".ds");
        if (singleFileMode) {
            lines.AddRange(fileHeaders);
        }
        // 将子表的文件合并到超类会有更好的稳定性(避免突然增加表格时文件名变动)
        bool firstSheet = true;
        foreach (IGrouping<string, Sheet> grouping in _repository.GetSortedSheets().GroupBy(e => GetFirstSheetName(e.sheetName))) {
            if (!singleFileMode) {
                lines.Clear();
                lines.AddRange(fileHeaders);
                firstSheet = true;
            }
            List<Sheet> sheets = grouping.ToList();
            if (sheets[0].isParamSheet) {
                // 参数表需要拼接到一起...
                string mergedSheetName = grouping.Key;
                if (!firstSheet) {
                    lines.Add("");
                } else {
                    firstSheet = false;
                }
                try {
                    if (sheets.Count == 1) {
                        Append(mergedSheetName, sheets[0].headers.Values, lines);
                    } else {
                        List<Header> headers = new List<Header>();
                        foreach (Sheet sheet in sheets) {
                            headers.AddRange(sheet.headers.Values);
                        }
                        Append(mergedSheetName, headers, lines);
                    }
                }
                catch (Exception ex) {
                    throw new Exception($"sheetName: {mergedSheetName}", ex);
                }
            } else {
                // 普通表的分区表的表头必须一致，否则会导致遗漏
                foreach (Sheet sheet in sheets) {
                    string mergedSheetName = GetMergedSheetName(sheet.sheetName);
                    if (!generatedSheets.Add(mergedSheetName)) {
                        continue;
                    }
                    if (!firstSheet) {
                        lines.Add("");
                    } else {
                        firstSheet = false;
                    }
                    try {
                        Append(mergedSheetName, sheet.headers.Values, lines);
                    }
                    catch (Exception ex) {
                        throw new Exception($"sheetName: {sheet.sheetName}", ex);
                    }
                }
            }
            if (!singleFileMode) {
                string path = Path.Combine(_outPath, grouping.Key + ".ds");
                File.WriteAllLines(path, lines.ToArray(), Util.ENCODING_UTF8);
            }
        }
        if (singleFileMode) {
            File.WriteAllLines(_outPath, lines.ToArray(), Util.ENCODING_UTF8);
        }
    }

    private void Append(string mergedSheetName, ICollection<Header> headers, List<string> lines) {
        string clsName;
        string baseTypeName = null;
        Sheet? baseSheet = null;
        if (IsBaseTypeSheet(mergedSheetName)) {
            // Item.Base => Item
            clsName = GetFirstSheetName(mergedSheetName) + "Cfg";
        } else if (IsSubTypeSheet(mergedSheetName)) {
            // Item.Equip => Equip -- 这允许用户自定义命名
            clsName = GetSecondSheetName(mergedSheetName) + "Cfg";
            baseTypeName = GetFirstSheetName(mergedSheetName) + "Cfg";
            baseSheet = GetBaseSheet(mergedSheetName);
        } else {
            clsName = mergedSheetName + "Cfg";
        }
        if (baseTypeName != null) {
            lines.Add($"class {clsName} : {baseTypeName} {{");
        } else {
            lines.Add($"class {clsName} {{");
        }
        // 普通表和参数都是基于Header生成
        int number = 1;
        foreach (Header header in headers) {
            if (baseSheet != null && baseSheet.GetHeader(header.name) != null) {
                continue;
            }
            if (!IsRequired(header.options, _requireMode)) {
                continue;
            }
            if (IsListOrMapElement(header.name)) {
                continue;
            }
            GetOptions(header, out bool isIntern, out bool isReadonly);
            if (isIntern || isReadonly) {
                lines.Add($"    //@{DSAnnotations.OPTIONS}"
                          + $"{{{DSAnnotations.KEY_SSTI}: {ToString(isIntern)},"
                          + $" {DSAnnotations.KEY_IS_READONLY}: {ToString(isReadonly)}}}");
            }
            lines.Add($"    {header.type} {header.name} = {number++}; // {header.comment ?? header.name}");
        }
        lines.Add("}");
    }

    private static string ToString(bool value) => value ? "true" : "false";

    private static void GetOptions(Header header, out bool isIntern, out bool isReadonly) {
        if (!header.options.Contains(KEY_I18N) && !header.options.Contains(KEY_INTERN)
                                               && !header.options.Contains(KEY_IS_READONLY)) {
            isIntern = false;
            isReadonly = false;
            return;
        }
        DsonObject<string> options = ParseOptions(header.options);
        isIntern = GetBool(options, KEY_I18N) || GetBool(options, KEY_INTERN);
        isReadonly = GetBool(options, KEY_IS_READONLY);
    }

    private Sheet GetBaseSheet(string mergedSheetName) {
        string baseSheetName = GetBaseTypeSheetName(mergedSheetName);
        Sheet sheet = _repository.GetSheet(baseSheetName) ?? _repository.GetSheet(GetFirstSheetName(mergedSheetName));
        if (sheet == null) {
            throw new Exception($"sheet {baseSheetName} not found.");
        }
        return sheet;
    }

    public static string GetClassName(string mergedSheetName) {
        if (IsBaseTypeSheet(mergedSheetName)) {
            return GetFirstSheetName(mergedSheetName) + "Cfg";
        }
        if (IsSubTypeSheet(mergedSheetName)) {
            return GetSecondSheetName(mergedSheetName) + "Cfg";
        }
        return mergedSheetName + "Cfg";
    }
}
}