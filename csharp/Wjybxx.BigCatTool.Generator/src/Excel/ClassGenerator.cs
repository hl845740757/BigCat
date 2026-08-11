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
using Wjybxx.BigCatTool.Core;
using Wjybxx.BigCatTool.DataScript;
using Wjybxx.BigCatTool.Excel;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Poet;
using Wjybxx.Commons.Pool;
using Wjybxx.Dson;
using static Wjybxx.BigCatTool.DataScript.DSUtil;

namespace Wjybxx.BigCatTool.Generator.Excel
{
/// <summary>
/// 根据表格DS文件生成对应的csharp类
/// （仅适用为表格生成的DS文件生成csharp类，不是为任意的DS文件生成csharp类的）
///
/// 生成内容：
/// 1.根据tables.ds乘除对应的配置类。
/// 2.表格ds文件中的所有List和字典都将转换为不可变集合 -- 配置类要严格不可变。
/// 3.如果是ssti字段，则会生成辅助属性，将字符串缓存到辅助字段。
/// 4.如果配置了字段的解码代理，则会调度到对应的代码上。
///
/// <h3>非partial</h3>
/// 生成的class并不是partial的，因为我们要支持字段读写代理，而字段的读写代理只能通过配置实现；
/// 因此我们统一都通过配置增加字段和启用AfterDecode方法。
/// 此外，我们统一管理缓存字段，这样缓存字段的集合也将被转换为不可变集合。
/// 
/// 注意事项：
/// 1.该生成器不处理其它的文件，即不会生成tables.ds引入的文件。
/// 2.如果表格要使用Unity的内置类型的话，只需要配置一个ds文件，然后导入即可。
/// 3.仅支持<see cref="DataScriptGenerator"/>生成的文件。
/// 4.生成的Class的属性将不按照大驼峰命名，而是直接保持为表格的小驼峰名字 -- 我们期望点出来的就当做字段。
/// </summary>
public class ClassGenerator : ISheetProcessor
{
    private static readonly ClassName TYPE_NAME_SERIAL_VERSION = GeneratorUtil.ClassNameOfCanonicalName("Wjybxx.BigCat.Util.SerialVersionAttribute");
    private static readonly AttributeSpec processorInfo = GeneratorUtil.NewProcessorInfoAnnotation(typeof(ClassGenerator));

    private readonly DSRepository _dsRepository;
    private readonly CodeGeneratorCfg _cfg;
    private readonly LinkedHashSet<string> _fileNames;
    private readonly Helper _helper;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="dsRepository">ds文件仓库</param>
    /// <param name="fileNames">需要处理的文件</param>
    /// <param name="cfg">详细的配置</param>
    public ClassGenerator(DSRepository dsRepository, CodeGeneratorCfg cfg, ICollection<string> fileNames) {
        _dsRepository = dsRepository;
        _cfg = cfg;
        _helper = new Helper(dsRepository, cfg, processorInfo);
        _fileNames = new LinkedHashSet<string>(fileNames);
    }

    public void Execute() {
        if (!Directory.Exists(_cfg.outPath)) {
            Directory.CreateDirectory(_cfg.outPath);
        }
        foreach (string fileName in _fileNames) {
            string fileSimpleName = fileName;
            if (fileSimpleName.EndsWith(".ds")) {
                fileSimpleName = fileSimpleName.Substring(0, fileSimpleName.Length - 3);
            }
            DSFile? dsFile = _dsRepository.GetFile(fileSimpleName);
            if (dsFile == null) {
                throw new InvalidOperationException("ds file not found: " + fileSimpleName);
            }

            string? csharpNamespace = Annotation.GetString(dsFile.GetOptions(), DSKeywords.CSHARP_NAMESPACE);
            if (string.IsNullOrEmpty(csharpNamespace)) {
                throw new InvalidOperationException("csharpNamespace is absent" + dsFile.FileName);
            }
            foreach (var element in dsFile.EnclosedElements) {
                if (!element.Kind.IsNamedType()) {
                    continue;
                }
                DSNamedType namedType = (DSNamedType)element;
                try {
                    TypeSpec.Builder builder = _helper.Generate(namedType);
                    GeneratorUtil.WriteToFile(_cfg.outPath, csharpNamespace, builder.Build());
                }
                catch (Exception ex) {
                    throw new Exception($"file: {fileName}, type: {namedType.FullName}", ex);
                }
            }
        }
    }

    private class Helper : CodeGeneratorHelper
    {
        private readonly List<DSField> _fieldListCache = new(20);

        public Helper(DSRepository dsRepository, CodeGeneratorCfg generatorCfg, AttributeSpec processorInfo)
            : base(dsRepository, generatorCfg, processorInfo) {
        }

        protected override void InitAttributes(DSNamedType namedType, DsonObject<string> options, TypeSpec.Builder typeBuilder) {
            base.InitAttributes(namedType, options, typeBuilder);
            // 增加序列化版本注解
            int serialVersion = DataScriptGenerator.GetHashCode(namedType.GetFields(true, _fieldListCache.ClearAndReturn()));
            typeBuilder.AddAttribute(AttributeSpec.NewBuilder(TYPE_NAME_SERIAL_VERSION)
                .Constructor(CodeBlock.Of(serialVersion.ToString())).Build());
        }

        protected override CodeBlock? GetFieldInitializer(DSField field, DsonObject<string> fieldOptions, TypeName fieldTypeName) {
            if (field.IsReadonly) {
                return null;
            }
            if (IsListType(field.Type) || IsSetType(field.Type) || IsMapType(field.Type)) {
                return CodeBlock.Of("$T.Empty", fieldTypeName);
            }
            return null;
        }

        protected override Modifiers GetSetterModifiers(DSField field, DsonObject<string> fieldOptions) {
            return Modifiers.Internal; // 允许表格模块读表时赋值和增加缓存字段
        }

        protected override bool IsDataClass(DSNamedType namedType, DsonObject<string> options) {
            return false;
        }

        protected override bool NeedClearSstiMethod(DSNamedType namedType, DsonObject<string> options) {
            return true;
        }

        protected override bool NeedCodecMethod(DSNamedType namedType, DsonObject<string> options) {
            return true;
        }

        protected override bool NeedCopyMethod(DSNamedType namedType, DsonObject<string> options) {
            return true;
        }

        protected override bool NeedToStringMethod(DSNamedType namedType, DsonObject<string> options) {
            Annotation annotation = namedType.GetAnnotation(ExcelAnnotations.SHEET_INFO);
            if (annotation == null) return false;
            if (!annotation.AsObject().TryGetValue(ExcelAnnotations.KEY_TYPE, out DsonValue value)) return false;
            return value.AsNumber().IntValue == (int)SheetType.Normal;
        }

        protected override void BuildToStringMethod(DSNamedType namedType, TypeSpec.Builder typeBuilder) {
            MethodSpec.Builder methodBuilder = MethodSpec.NewMethodBuilder("ToString")
                .AddModifiers(Modifiers.Public | Modifiers.Override)
                .Returns(TypeName.STRING);

            namedType.GetFields(true, _fieldListCache.ClearAndReturn());
            DSField primaryKeyField = _fieldListCache[0];
            // 打印主键id即可 -- 主键是数字或字符串 -- ItemCfg{id: 1001}
            methodBuilder.codeBuilder.AddStatement("return \"$L{$L: \" + $L + \"}\"",
                namedType.Name, primaryKeyField.Name, primaryKeyField.Name);
            typeBuilder.AddMethod(methodBuilder.Build());
        }

        protected override ClassName? GetBuiltinMetaTypeName(DSNamedType originDefine) {
            return originDefine.Name switch
            {
                // 集合全部转不可变
                DSKeywords.TYPE_LIST => TYPE_NAME_IMMUTABLE_LIST,
                DSKeywords.TYPE_HASHSET => TYPE_NAME_IMMUTABLE_SET,
                DSKeywords.TYPE_MAP => TYPE_NAME_IMMUTABLE_DICTIONARY,
                TYPE_LINKED_MAP => TYPE_NAME_IMMUTABLE_DICTIONARY,
                TYPE_ARRAY_MAP => TYPE_NAME_IMMUTABLE_DICTIONARY,
                //
                TYPE_IMMUTABLE_LIST => TYPE_NAME_IMMUTABLE_LIST,
                TYPE_IMMUTABLE_SET => TYPE_NAME_IMMUTABLE_SET,
                TYPE_IMMUTABLE_MAP => TYPE_NAME_IMMUTABLE_DICTIONARY,
                _ => base.GetBuiltinMetaTypeName(originDefine)
            };
        }
    }
}
}