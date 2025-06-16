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
using Wjybxx.BigCatEditor.DataScript;
using Wjybxx.BigCatEditor.Excel;
using Wjybxx.Commons.Poet;
using Wjybxx.Dson;
using static Wjybxx.BigCatEditor.Generator.Excel.ExcelConstants;
using ConstCfg = Wjybxx.BigCatEditor.Generator.Excel.ConstGeneratorCfg.ConstCfg;

namespace Wjybxx.BigCatEditor.Generator.Excel
{
/// <summary>
/// 根据表格数据生成简单常量类
///
/// 1.每个表格生成一个常量类。
/// 2.如果是为参数表生成额外的常量类，需要在对应的单元格启用<see cref="ExcelConstants.KEY_IS_CONST"/>属性。
/// 3.如果是为参数表生成额外的常量类，只需要配置类型名和表单名。
/// 4.不支持Value为特殊类型，比如集合等；该生成器主要解决的是代码中的魔数问题。
/// </summary>
public class ConstGenerator : ISheetProcessor
{
    private static readonly AttributeSpec processorInfo = GeneratorUtil.NewProcessorInfoAnnotation(typeof(ConstGenerator));

    private readonly SheetRepository _repository;
    private readonly ConstGeneratorCfg _cfg;
    private readonly CodeBlock? _fileHeader;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="repository"></param>
    /// <param name="cfg">生成代码的命名空间</param>
    /// <param name="fileHeader">文件头</param>
    public ConstGenerator(SheetRepository repository, ConstGeneratorCfg cfg,
                          CodeBlock? fileHeader = null) {
        _repository = repository;
        _cfg = cfg;
        _fileHeader = fileHeader;
    }

    private void CheckCfg() {
        if (string.IsNullOrEmpty(_cfg.ns) || string.IsNullOrEmpty(_cfg.outPath)) {
            throw new InvalidOperationException("cfg is invalid");
        }
        _cfg.items ??= new List<ConstCfg>();
    }

    public void Execute() {
        CheckCfg();
        if (!Directory.Exists(_cfg.outPath)) {
            Directory.CreateDirectory(_cfg.outPath);
        }
        foreach (ConstCfg constCfg in _cfg.items) {
            List<Sheet> sheets = _repository.SheetMap.Values
                .Where(e => GetFirstSheetName(e.sheetName) == constCfg.sheetName)
                .ToList();

            List<ConstValue> values;
            try {
                values = sheets[0].isParamSheet
                    ? CollectParamSheetValues(sheets, constCfg)
                    : CollectNormalSheetValues(sheets, constCfg);
            }
            catch (Exception ex) {
                throw new Exception($"sheetName: {constCfg.sheetName}", ex);
            }

            TypeSpec.Builder typeBuilder = TypeSpec.NewClassBuilder(constCfg.clsName)
                .AddModifiers(Modifiers.Public | Modifiers.Static)
                .AddAttribute(processorInfo);
            foreach (ConstValue constValue in values) {
                TypeName typeName = constValue.kind switch
                {
                    ConstKind.Int32 => TypeName.INT,
                    ConstKind.Int64 => TypeName.LONG,
                    ConstKind.Float => TypeName.FLOAT,
                    ConstKind.Double => TypeName.DOUBLE,
                    ConstKind.Bool => TypeName.BOOL,
                    ConstKind.String => TypeName.STRING,
                    _ => throw new InvalidOperationException(constValue.ToString())
                };
                var fieldBuilder = FieldSpec.NewBuilder(typeName, constValue.name)
                    .AddModifiers(Modifiers.Public | Modifiers.Const);
                if (!string.IsNullOrWhiteSpace(constValue.comment)) {
                    fieldBuilder.AddDocument(constValue.comment);
                }
                switch (constValue.kind) {
                    case ConstKind.Int32:
                    case ConstKind.Int64:
                    case ConstKind.Double:
                        // 使用原始字符串的情况下可以避免各种问题
                        fieldBuilder.Initializer(constValue.value);
                        break;
                    case ConstKind.Float:
                        // 小数默认是double类型，由于Excel的浮点数也是double，单纯加f可能不能解决，因此我们使用强转的方式
                        if (constValue.value.Contains('.')) {
                            fieldBuilder.Initializer("(float)" + constValue.value);
                        } else {
                            fieldBuilder.Initializer(constValue.value);
                        }
                        break;
                    case ConstKind.Bool:
                        bool isTrue = constValue.value == "1" || constValue.value.ToLower() == "true";
                        fieldBuilder.Initializer(isTrue ? "true" : "false");
                        break;
                    case ConstKind.String:
                        // 需要双引号
                        fieldBuilder.Initializer("$S", constValue.value);
                        break;
                }
                typeBuilder.AddField(fieldBuilder.Build());
            }
            CsharpFile.Builder fileBuilder = CsharpFile.NewBuilder(constCfg.clsName);
            if (_fileHeader != null) {
                fileBuilder.AddSpec(new CodeBlockSpec(_fileHeader));
            }
            fileBuilder.AddSpec(NamespaceSpec.Of(_cfg.ns, typeBuilder.Build()));
            GeneratorUtil.WriteToFile(_cfg.outPath, fileBuilder.Build());
        }
    }

    private static List<ConstValue> CollectNormalSheetValues(List<Sheet> sheets, ConstCfg constCfg) {
        List<ConstValue> values = new List<ConstValue>();
        Header header = sheets[0].GetHeader(constCfg.valueCol) ?? throw new InvalidOperationException("valHeader is null");
        ConstKind kind = GetConstKind(header);
        foreach (Sheet sheet in sheets) {
            foreach (SheetRow sheetRow in sheet.valueRows) {
                string name = sheetRow.GetValue(constCfg.nameCol);
                if (string.IsNullOrWhiteSpace(name)) {
                    continue;
                }
                string value = sheetRow.GetValue(constCfg.valueCol) ?? "";
                string comment = sheetRow.GetValue(constCfg.commentCol);
                values.Add(new ConstValue(kind, name, value, comment));
            }
        }
        return values;
    }

    private static List<ConstValue> CollectParamSheetValues(List<Sheet> sheets, ConstCfg constCfg) {
        List<ConstValue> values = new List<ConstValue>();
        foreach (Sheet sheet in sheets) {
            foreach (Header header in sheet.headers.Values) {
                if (!header.options.Contains(KEY_IS_CONST)) {
                    continue; // 避免不必要的解析
                }
                DsonObject<string> options = ParseOptions(header.options);
                if (!GetBool(options, KEY_IS_CONST)) {
                    continue;
                }
                string name = header.name;
                string value = sheet.GetValue(name) ?? "";
                string comment = header.comment;
                ConstKind kind = GetConstKind(header);
                values.Add(new ConstValue(kind, name, value, comment));
            }
        }
        return values;
    }

    private static ConstKind GetConstKind(Header header) {
        return header.type switch
        {
            DSKeywords.TYPE_INT32 => ConstKind.Int32,
            DSKeywords.TYPE_INT64 => ConstKind.Int64,
            DSKeywords.TYPE_FLOAT => ConstKind.Float,
            DSKeywords.TYPE_DOUBLE => ConstKind.Double,
            DSKeywords.TYPE_BOOL => ConstKind.Bool,
            DSKeywords.TYPE_STRING => ConstKind.String,
            _ => throw new InvalidOperationException($"invalid const type: {header.type}")
        };
    }
}
}