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
using System.Text;
using Wjybxx.BigCatTool.DataScript;
using Wjybxx.BigCatTool.Excel;
using Wjybxx.Commons;
using Wjybxx.Dson;
using static Wjybxx.BigCatTool.Generator.Excel.ExcelConstants;

namespace Wjybxx.BigCatTool.Generator.Excel
{
/// <summary>
/// 根据Excel生成对应的DataScript文件
/// (将表格数据追加到文本模板尾部即可)
/// </summary>
public class DataScriptGenerator : ISheetProcessor
{
    private readonly SheetRepository _repository;
    private readonly DataScriptGeneratorCfg _cfg;
    private readonly RequireMode _requireMode;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="repository">文件仓库</param>
    /// <param name="cfg">生成器配置文件</param>
    /// <param name="requireMode">导出模式</param>
    public DataScriptGenerator(SheetRepository repository, DataScriptGeneratorCfg cfg, RequireMode requireMode) {
        _repository = repository;
        _cfg = cfg;
        _requireMode = requireMode;
    }

    public void Execute() {
        string[] fileHeaders = File.ReadAllLines(_cfg.templateFile);
        List<string> lines = new List<string>(100);
        HashSet<string> generatedSheets = new HashSet<string>();
        // 初始化文件头
        bool singleFileMode = _cfg.outPath.EndsWith(".ds");
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
            if (sheets[0].IsParamSheet) {
                // 参数表需要拼接到一起...
                string mergedSheetName = grouping.Key;
                if (!firstSheet) {
                    lines.Add("");
                } else {
                    firstSheet = false;
                }
                try {
                    if (sheets.Count == 1) {
                        Append(mergedSheetName, sheets[0].headers.Values, lines, isParamSheet: true);
                    } else {
                        List<Header> headers = new List<Header>();
                        foreach (Sheet sheet in sheets) {
                            headers.AddRange(sheet.headers.Values);
                        }
                        Append(mergedSheetName, headers, lines, isParamSheet: true);
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
                string path = Path.Combine(_cfg.outPath, grouping.Key + ".ds");
                File.WriteAllLines(path, lines.ToArray(), ToolUtil.ENCODING_UTF8);
            }
        }
        if (singleFileMode) {
            File.WriteAllLines(_cfg.outPath, lines.ToArray(), ToolUtil.ENCODING_UTF8);
        }
    }

    private void Append(string mergedSheetName, ICollection<Header> headers, List<string> lines, bool isParamSheet = false) {
        // 追加表单信息 -- 以生成ToString
        SheetType sheetType = isParamSheet ? SheetType.Param : SheetType.Normal;
        lines.Add($"//@{ExcelAnnotations.SHEET_INFO} "
                  + "{"
                  + $"{ExcelAnnotations.KEY_NAME}: \"{mergedSheetName}\","
                  + $" {ExcelAnnotations.KEY_TYPE}: {(int)sheetType}"
                  + "}");
        // 类型声明
        string clsName = GetClassName(mergedSheetName);
        string baseTypeName = null;
        Sheet? baseSheet = null;
        if (IsSubTypeSheet(mergedSheetName)) {
            baseSheet = GetBaseSheet(mergedSheetName);
            baseTypeName = GetClassName(baseSheet.sheetName);
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
                if (number != 1) {
                    throw new Exception("Base class fields must be declared before subclass fields, field: " + header.name);
                }
                continue;
            }
            if (!IsRequired(header.options, _requireMode)) {
                continue;
            }
            if (IsListOrMapElement(header.name)) {
                continue;
            }
            GetOptions(header, out bool isIntern, out bool isReadonly, out bool nonSerialized);
            if (number == 1 && baseTypeName == null && !isParamSheet) {
                isReadonly = true; // 普通表的第一列强制readonly
            }
            if (isIntern || nonSerialized) {
                lines.Add($"    //@{DSAnnotations.OPTIONS}"
                          + "{"
                          + $"{DSAnnotations.KEY_SSTI}: {isIntern.ToString2()},"
                          + $" {DSAnnotations.KEY_NON_SERIALIZED}: {nonSerialized.ToString2()}"
                          + "}");
            }
            if (isReadonly) {
                lines.Add($"    readonly {header.type} {header.name} = {number++}; // {header.comment ?? header.name}");
            } else {
                lines.Add($"    {header.type} {header.name} = {number++}; // {header.comment ?? header.name}");
            }
        }
        // 扩展字段 -- 扩展字段都是缓存字段，不参与equals和hashcode测试
        DataScriptGeneratorCfg.ClassCfg classCfg = _cfg.items.FirstOrDefault(e => e.name == clsName);
        if (classCfg != null && classCfg.extraFields.Count > 0) {
            foreach (DataScriptGeneratorCfg.FieldCfg fieldCfg in classCfg.extraFields) {
                lines.Add($"    //@{DSAnnotations.OPTIONS}"
                          + "{"
                          + $"{DSAnnotations.KEY_NON_EQUAL}: true,"
                          + $" {DSAnnotations.KEY_NON_SERIALIZED}: true"
                          + "}");
                lines.Add($"    {fieldCfg.type} {fieldCfg.name} = {number++}; // {fieldCfg.comment ?? fieldCfg.name}");
            }
        }
        lines.Add("}");
    }

    private static void GetOptions(Header header, out bool isIntern, out bool isReadonly, out bool nonSerialized) {
        DsonObject<string> options = ParseOptions(header.options);
        isIntern = GetBool(options, KEY_I18N) || GetBool(options, KEY_INTERN);
        isReadonly = GetBool(options, KEY_IS_READONLY);
        nonSerialized = GetBool(options, KEY_NON_SERIALIZED);
    }

    private Sheet GetBaseSheet(string mergedSheetName) {
        string baseSheetName = GetBaseTypeSheetName(mergedSheetName);
        Sheet sheet = _repository.GetSheet(baseSheetName) ?? _repository.GetSheet(GetFirstSheetName(mergedSheetName));
        if (sheet == null) {
            throw new Exception($"sheet {baseSheetName} not found.");
        }
        return sheet;
    }

    /// <summary>
    /// 获取表格对应的Class名字
    /// </summary>
    /// <param name="mergedSheetName"></param>
    /// <returns></returns>
    public static string GetClassName(string mergedSheetName) {
        int spIndex = mergedSheetName.IndexOf('.');
        if (spIndex < 0) {
            return mergedSheetName + "Cfg";
        }
        if (IsBaseTypeSheet(mergedSheetName)) {
            return GetFirstSheetName(mergedSheetName) + "Cfg";
        }
        // ItemEquipCfg 拼接以避免命名冲突
        return new StringBuilder(mergedSheetName.Length + 3)
            .Append(mergedSheetName).Remove(spIndex, 1).Append("Cfg")
            .ToString();
    }

    /// <summary>
    /// 字段元数据的Hash
    /// 
    /// 我们需要将其添加到生成的Class类型信息中，或是注解-或是静态字段
    /// </summary>
    /// <param name="fields"></param>
    /// <returns></returns>
    public static int GetHashCode(List<DSField> fields) {
        int hash = 1;
        foreach (DSField field in fields) {
            hash = hash * 31 + field.TypeSymbol.GetHashCode();
            hash = hash * 31 + field.Name.GetHashCode();
        }
        return hash;
    }
}
}