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
using System.Text;
using Wjybxx.BigCatEditor.DataScript;
using Wjybxx.BigCatEditor.Excel;
using Wjybxx.Commons.Poet;
using Wjybxx.Dson;
using static Wjybxx.BigCatEditor.Generator.Excel.ExcelConstants;

namespace Wjybxx.BigCatEditor.Generator.Excel
{
/// <summary>
/// 根据表格数据生成常量类
///
/// 1.每个表格生成一个常量类。
/// 2.如果是为参数表生成额外的常量类，需要在对应的单元格启用<see cref="ExcelConstants.KEY_IS_CONST"/>属性。
/// 3.如果是为参数表生成额外的常量类，只需要配置类型名和表单名。
/// </summary>
public class ConstantGenerator : ISheetProcessor
{
    private static readonly AttributeSpec processorInfo = GeneratorUtil.NewProcessorInfoAnnotation(typeof(ConstantGenerator));

    private readonly SheetRepository _repository;
    private readonly string _namespace;
    private readonly List<ConstCfg> _constCfgs;
    private readonly string _outDir;
    private readonly CodeBlock? _fileHeader;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="repository"></param>
    /// <param name="ns">生成代码的命名空间</param>
    /// <param name="constCfgs">所有的常量配置</param>
    /// <param name="outDir">输出文件夹</param>
    /// <param name="fileHeader">文件头</param>
    public ConstantGenerator(SheetRepository repository, string ns, List<ConstCfg> constCfgs, string outDir,
                             CodeBlock? fileHeader = null) {
        _repository = repository;
        _namespace = ns;
        _constCfgs = constCfgs;
        _outDir = outDir;
        _fileHeader = fileHeader;
    }

    public void Execute() {
        foreach (ConstCfg enumCfg in _constCfgs) {
            List<Sheet> sheets = _repository.SheetMap.Values
                .Where(e => GetFirstSheetName(e.sheetName) == enumCfg.sheetName)
                .ToList();
            List<ConstValue> values;
            try {
                values = sheets[0].isParamSheet
                    ? CollectParamSheetValues(sheets, _constCfgs[0])
                    : CollectNormalSheetValues(sheets, _constCfgs[0]);
            }
            catch (Exception ex) {
                throw new Exception($"sheetName: {enumCfg.sheetName}", ex);
            }

            TypeSpec.Builder typeBuilder = TypeSpec.NewClassBuilder(enumCfg.clsName)
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

            CsharpFile.Builder fileBuilder = CsharpFile.NewBuilder(enumCfg.clsName);
            if (_fileHeader != null) {
                fileBuilder.AddSpec(new CodeBlockSpec(_fileHeader));
            }
            fileBuilder.AddSpec(NamespaceSpec.Of(_namespace, typeBuilder.Build()));
            GeneratorUtil.WriteToFile(_outDir, fileBuilder.Build());
        }
    }

    private static List<ConstValue> CollectNormalSheetValues(List<Sheet> sheets, ConstCfg enumCfg) {
        List<ConstValue> values = new List<ConstValue>();
        // 其实对于普通表，类型是确定的，可以提前绑定函数避免大量的Switch-Case，但这个工具的执行频次不高，简单优先
        Header header = sheets[0].GetHeader(enumCfg.valueCol) ?? throw new InvalidOperationException("valHeader is null");
        foreach (Sheet sheet in sheets) {
            foreach (SheetRow sheetRow in sheet.valueRows) {
                string name = sheetRow.GetValue(enumCfg.nameCol);
                if (string.IsNullOrWhiteSpace(name)) {
                    continue;
                }
                string value = sheetRow.GetValue(enumCfg.valueCol) ?? "";
                string comment = sheetRow.GetValue(enumCfg.commentCol);
                values.Add(ParseValue(header, name, value, comment));
            }
        }
        return values;
    }

    private static List<ConstValue> CollectParamSheetValues(List<Sheet> sheets, ConstCfg enumCfg) {
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
                values.Add(ParseValue(header, name, value, comment));
            }
        }
        return values;
    }

    private static ConstValue ParseValue(Header header, string name, string value, string? comment) {
        return header.type switch
        {
            DSKeywords.TYPE_INT32 => new ConstValue(ConstKind.Int32, name, value, comment),
            DSKeywords.TYPE_INT64 => new ConstValue(ConstKind.Int64, name, value, comment),
            DSKeywords.TYPE_FLOAT => new ConstValue(ConstKind.Float, name, value, comment),
            DSKeywords.TYPE_DOUBLE => new ConstValue(ConstKind.Double, name, value, comment),
            DSKeywords.TYPE_BOOL => new ConstValue(ConstKind.Bool, name, value, comment),
            DSKeywords.TYPE_STRING => new ConstValue(ConstKind.String, name, value, comment),
            _ => throw new InvalidOperationException($"invalid const type: {header.type}")
        };
    }

    //
    // int值常量提供额外信息 -- 真的需要时候再说
    // {
    //     StringBuilder sb = new StringBuilder();
    //     sb.Append("new int[] {");
    //     for (int index = 0; index < intValues.Count; index++) {
    //         if (index > 0) sb.Append(',');
    //         if (index > 0 && (index % 5) == 0) { // 每5个值换一次行
    //             sb.Append('\n');
    //         }
    //         sb.Append(intValues[index]);
    //     }
    //     sb.Append("}.ToImmutableList2()");
    //
    //     typeBuilder.AddField(FieldSpec.NewBuilder(typeof(ImmutableList<int>), "INT_VALUES")
    //         .AddModifiers(Modifiers.Public | Modifiers.Static | Modifiers.ReadOnly)
    //         .AddDocument("Generated")
    //         .Initializer(sb.ToString())
    //         .Build());
    // }
}
}