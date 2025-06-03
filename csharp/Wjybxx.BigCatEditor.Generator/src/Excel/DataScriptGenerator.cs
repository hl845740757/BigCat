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
using System.Diagnostics;
using System.IO;
using Wjybxx.BigCatEditor.Core;
using Wjybxx.BigCatEditor.Excel;
using static Wjybxx.BigCatEditor.Generator.Excel.SheetConstants;

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
    private readonly string _outDir;
    private readonly Mode _mode;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="repository">文件仓库</param>
    /// <param name="templateFile">模板文件</param>
    /// <param name="outDir">输出文件夹</param>
    /// <param name="mode">导出模式</param>
    public DataScriptGenerator(SheetRepository repository, FileInfo templateFile, string outDir, Mode mode) {
        _templateFile = templateFile;
        _repository = repository;
        _outDir = outDir;
        _mode = mode;
    }

    public void Execute() {
        if (!_templateFile.Exists) {
            throw new IOException("Template file doesn't exist.");
        }

        string[] fileHeaders = File.ReadAllLines(_templateFile.FullName);
        List<string> lines = new List<string>(100);
        HashSet<string> generatedSheets = new HashSet<string>();
        foreach (Sheet sheet in _repository.SheetMap.Values) {
            string mergedSheetName = GetMergedSheetName(sheet.sheetName);
            if (!generatedSheets.Add(mergedSheetName)) {
                continue;
            }
            string clsName;
            string baseTypeName = null;
            Sheet? baseSheet = null;
            if (IsBaseTypeSheet(mergedSheetName)) {
                // Item.Base => Item
                clsName = GetRootSheetName(mergedSheetName);
            } else if (IsSubTypeSheet(mergedSheetName)) {
                // Item.Equip => ItemEquip
                clsName = Util.DeleteChar(mergedSheetName, '.');
                baseTypeName = GetRootSheetName(mergedSheetName);
                // 找到Item.Base
                string baseTypeSheetName = GetBaseTypeSheetName(baseTypeName);
                baseSheet = _repository.GetSheet(baseTypeSheetName);
                if (baseSheet == null) {
                    throw new Exception($"baseSheet {baseTypeSheetName} not found.");
                }
            } else {
                clsName = mergedSheetName;
            }
            // 初始化文件头
            lines.Clear();
            lines.AddRange(fileHeaders);

            // 处理继承
            if (baseTypeName != null) {
                lines.Add($"class {clsName}Cfg : {baseTypeName}Cfg {{");
            } else {
                lines.Add($"class {clsName}Cfg {{");
            }

            // 普通表和参数都是基于Header生成--但普通表存在继承问题
            int number = 1;
            foreach (Header header in sheet.headers.Values) {
                if (baseSheet != null && baseSheet.GetHeader(header.name) != null) {
                    continue; // 超类字段
                }
                if (!IsRequired(header.options, _mode)) {
                    continue; // 不需要的列
                }
                if (IsListOrMapElement(header.name)) {
                    continue; // 数组元素列
                }
                lines.Add($"    {header.type} {header.name} = {number++}; // {header.comment ?? header.name}");
            }
            lines.Add("}");

            string path = Path.Combine(_outDir, mergedSheetName + ".ds");
            File.WriteAllLines(path, lines.ToArray());
        }
    }
}
}