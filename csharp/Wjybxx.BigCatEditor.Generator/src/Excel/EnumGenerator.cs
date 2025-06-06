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
using System.Linq;
using Wjybxx.BigCatEditor.Excel;
using static Wjybxx.BigCatEditor.Generator.Excel.ExcelConstants;
using Util = Wjybxx.BigCatEditor.Core.Util;

namespace Wjybxx.BigCatEditor.Generator.Excel
{
/// <summary>
/// 根据表格数据生成枚举类
///
/// 1.在新的表格设计下，我们不直接生成C#类，而是生成ds文件。
/// 2.所有的枚举生成到同一个ds文件。
/// 3.枚举生成的顺序由用户的List决定 -- 通常是根据配置来的，比较稳定
/// </summary>
public class EnumGenerator : ISheetProcessor
{
    private readonly SheetRepository _repository;
    private readonly FileInfo _templateFile;
    private readonly List<ConstCfg> _enumCfgs;
    private readonly string _outPath;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="repository"></param>
    /// <param name="templateFile">枚举的模板ds文件，主要处理文件头</param>
    /// <param name="enumCfgs">所有的枚举配置</param>
    /// <param name="outPath">输出路径</param>
    public EnumGenerator(SheetRepository repository, FileInfo templateFile, List<ConstCfg> enumCfgs, string outPath) {
        _repository = repository;
        _templateFile = templateFile;
        _enumCfgs = enumCfgs;
        _outPath = outPath;
    }

    public void Execute() {
        if (!_templateFile.Exists) {
            throw new IOException("Template file doesn't exist.");
        }
        string[] fileHeaders = File.ReadAllLines(_templateFile.FullName);
        List<string> lines = new List<string>(100);
        lines.AddRange(fileHeaders);

        bool firstSheet = true;
        foreach (ConstCfg enumCfg in _enumCfgs) {
            List<Sheet> sheets = _repository.SheetMap.Values
                .Where(e => GetFirstSheetName(e.sheetName) == enumCfg.sheetName)
                .ToList();
            try {
                if (sheets.Count > 0 && sheets[0].isParamSheet) {
                    // 参数表不应该生成枚举
                    throw new InvalidOperationException("param sheet can't generate enum");
                }
                // 这里对Sheet排序是不必要的，因为我们需要对枚举值排序
                List<EnumValue> values = CollectValues(sheets, enumCfg);
                if (firstSheet) {
                    firstSheet = false;
                } else {
                    lines.Add("");
                }
                values.Sort((a, b) => a.value.CompareTo(b.value));
                Append(lines, enumCfg, values);
            }
            catch (Exception ex) {
                throw new Exception($"sheetName: {enumCfg.sheetName}", ex);
            }
        }
        File.WriteAllLines(_outPath, lines, Util.ENCODING_UTF8);
    }

    private static List<EnumValue> CollectValues(List<Sheet> sheets, ConstCfg enumCfg) {
        List<EnumValue> values = new List<EnumValue>();
        foreach (Sheet sheet in sheets) {
            foreach (SheetRow sheetRow in sheet.valueRows) {
                string name = sheetRow.GetValue(enumCfg.nameCol);
                if (string.IsNullOrWhiteSpace(name)) {
                    continue;
                }
                string value = sheetRow.GetValue(enumCfg.valueCol)
                               ?? throw new InvalidOperationException("value is null, colName: " + enumCfg.valueCol);
                string comment = sheetRow.GetValue(enumCfg.commentCol);
                int number = int.Parse(value);
                values.Add(new EnumValue(name, number, comment));
            }
        }
        return values;
    }

    private static void Append(List<string> lines, ConstCfg enumCfg, List<EnumValue> enumValues) {
        // 标记为生成代码时需要添加Flags注解
        if (enumCfg.isFlags) {
            lines.Add("//@Flags{}");
        }
        lines.Add($"enum {enumCfg.clsName} {{");
        foreach (EnumValue enumValue in enumValues) {
            lines.Add($"    {enumValue.name} = {enumValue.value}; // {enumValue.comment ?? enumValue.name}");
        }
        // 增加最大和最小值
        int min = -1;
        int max = -1;
        if (enumValues.Count > 0) {
            min = enumValues.Min(e => e.value);
            max = enumValues.Max(e => e.value);
        }
        lines.Add($"    MinValue = {min}; // Generated");
        lines.Add($"    MaxValue = {max}; // Generated");
        lines.Add("}");
    }
}
}