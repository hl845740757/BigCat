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
using Wjybxx.BigCatTool.Core;
using Wjybxx.BigCatTool.DataScript;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Poet;
using Wjybxx.Dson;
using Wjybxx.Dson.Codec;
using static Wjybxx.BigCatTool.Generator.Excel.ExcelConstants;
using ClassCfg = Wjybxx.BigCatTool.Generator.Excel.ClassGeneratorCfg.ClassCfg;
using FieldCfg = Wjybxx.BigCatTool.Generator.Excel.ClassGeneratorCfg.FieldCfg;
using TypeName = Wjybxx.Commons.Poet.TypeName;

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
/// 注意事项：
/// 1.该生成器不处理其它的文件，即不会生成tables.ds引入的文件。
/// 2.如果表格要使用Unity的内置类型的话，只需要配置一个ds文件，然后导入即可。
/// 3.仅支持<see cref="DataScriptGenerator"/>生成的文件。
/// 4.生成的Class的属性将不按照大驼峰命名，而是直接保持为表格的小驼峰名字 -- 我们期望点出来的就当做字段。
/// </summary>
public class ClassGenerator : ISheetProcessor
{
    private static readonly ClassName TYPE_NAME_SST_MGR = ToolUtil.ClassNameOfCanonicalName("Wjybxx.BigCat.Fx.SstMgr");
    private static readonly ClassName TYPE_NAME_SERIAL_VERSION = ToolUtil.ClassNameOfCanonicalName("Wjybxx.BigCat.Util.SerialVersionAttribute");
    private static readonly AttributeSpec processorInfo = ToolUtil.NewProcessorInfoAnnotation(typeof(ClassGenerator));

    private readonly DSRepository _dsRepository;
    private readonly List<string> _fileNames;
    private readonly ClassGeneratorCfg _cfg;

    private readonly Dictionary<string, ClassName> _metaTypeNameCache = new();
#nullable disable
    // 避免传参
    private ClassName typeName;
    private TypeSpec.Builder typeBuilder;
    private MethodSpec.Builder constructorBuilder;
    private MethodSpec.Builder copyMethodBuilder;
#nullable enable

    /// <summary>
    /// 
    /// </summary>
    /// <param name="dsRepository">ds文件仓库</param>
    /// <param name="fileNames">需要处理的文件</param>
    /// <param name="cfg">详细的配置</param>
    public ClassGenerator(DSRepository dsRepository, List<string> fileNames, ClassGeneratorCfg cfg) {
        _dsRepository = dsRepository;
        _fileNames = fileNames;
        _cfg = cfg;
    }

    private void CheckCfg() {
        if (string.IsNullOrEmpty(_cfg.outPath)) {
            throw new InvalidOperationException("cfg is invalid");
        }
        _cfg.items ??= new List<ClassCfg>();
    }

    public void Execute() {
        CheckCfg();
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
                throw new InvalidOperationException("ds file not found: " + _fileNames);
            }
            string? csharpNamespace = dsFile.GetOption(DSKeywords.CSHARP_NAMESPACE);
            if (string.IsNullOrEmpty(csharpNamespace)) {
                throw new InvalidOperationException("csharpNamespace is absent" + dsFile.FileName);
            }
            foreach (var element in dsFile.EnclosedElements) {
                if (!element.Kind.IsNamedType()) {
                    continue;
                }
                DSNamedType namedType = (DSNamedType)element;
                typeName = ClassName.Get(csharpNamespace, namedType.SimpleName);
                typeBuilder = TypeSpec.NewClassBuilder(namedType.SimpleName)
                    .AddModifiers(Modifiers.Public)
                    .AddAttribute(processorInfo);
                try {
                    Generate(namedType);
                    ToolUtil.WriteToFile(_cfg.outPath, csharpNamespace, typeBuilder.Build());
                }
                catch (Exception ex) {
                    throw new Exception($"type: {namedType.FullName}", ex);
                }
            }
        }
    }

    private void Generate(DSNamedType namedType) {
        // 增加序列化版本注解
        int serialVersion = DataScriptGenerator.GetHashCode(namedType.GetFields());
        typeBuilder.AddAttribute(AttributeSpec.NewBuilder(TYPE_NAME_SERIAL_VERSION)
            .Constructor(CodeBlock.Of(serialVersion.ToString())).Build());

        typeBuilder.AddSpec(MacroSpec.Get("nullable", "disable"));
        typeBuilder.AddSpec(new CodeBlockSpec(CodeBlock.Of("ReSharper disable InconsistentNaming"), CodeBlockSpec.Kind.Comment));
        // 泛型参数 -- 表格文件没有泛型
        // foreach (DSTypeParameter typeParameter in namedType.DeclaredTypeParameters) {
        //     typeBuilder.AddTypeParameter(TypeParameterSpec.Get(typeParameter.SimpleName, typeParameter.Constraints));
        // }
        // 解码构造函数
        constructorBuilder = MethodSpec.NewConstructorBuilder()
            .AddModifiers(Modifiers.Public)
            .AddParameter(ClassName.Get(typeof(IDsonObjectReader)), "reader");

        // 处理继承问题
        if (namedType.BaseType != null) {
            ClassName baseTypeName = ClassName.Get(GetNamespace(namedType.BaseType), namedType.BaseType.SimpleName);
            typeBuilder.AddBaseClass(baseTypeName);
            constructorBuilder.ConstructorInvoker(CodeBlock.Of("base(reader)"));
            // 方法修饰符需要调整
            copyMethodBuilder = MethodSpec.NewMethodBuilder("CopyFrom")
                .AddModifiers(Modifiers.Public | Modifiers.Override)
                .AddParameter(baseTypeName, "_src");
            // 方法参数需要强转
            copyMethodBuilder.codeBuilder.AddStatement("base.CopyFrom(_src)");
            copyMethodBuilder.codeBuilder.AddStatement("$T src = ($T)_src", typeName, typeName);
        } else {
            copyMethodBuilder = MethodSpec.NewMethodBuilder("CopyFrom")
                .AddModifiers(Modifiers.Public | Modifiers.Virtual)
                .AddParameter(typeName, "src");
        }

        ClassCfg? classCfg = _cfg.items.FirstOrDefault(e => e.name == namedType.SimpleName);
        ClassName? codecProxy = (classCfg != null && !string.IsNullOrWhiteSpace(classCfg.codecProxy))
            ? ClassName.Get(_cfg.codecProxyNs, classCfg.codecProxy)
            : null;
        foreach (DSElement element in namedType.EnclosedElements) {
            if (element.Kind != DSElementKind.Field) {
                continue;
            }
            // 字段统一声明为属性，但属性名不调整（仍使用小驼峰）
            DSField field = (DSField)element;
            TypeName fieldTypeName = GetTypeName(field.Type);
            string readMethodName = ToolUtil.GetReadMethodName(fieldTypeName);
            if (IsSstiField(field)) {
                // ssti字段需要特殊处理，由两个私有字段+属性构成，一个私有字段保存原始的int值，一个String保存缓存值
                string fieldName = GetFieldName(field);
                string sstiFieldName = GetSstiFieldName(field);
                TypeName sstiFieldTypeName = IsListType(field.Type.SimpleName) ? TYPE_NAME_LIST_STRING : TypeName.STRING;
                // 属性需要将内容缓存到本地
                string mgrMethodName = IsListType(field.Type.SimpleName) ? "GetStringList" : "GetString";
                typeBuilder.AddProperty(PropertySpec.NewBuilder(sstiFieldTypeName, field.SimpleName, Modifiers.Public)
                    .RemoveSetter()
                    .AddDocument(ToDocument(field.Comments))
                    .Getter(CodeBlock.Of("$L ??= $T.$L($L)", sstiFieldName, TYPE_NAME_SST_MGR, mgrMethodName, fieldName).WithExpressionStyle())
                    .Build());
                // 字段放属性后面，阅读体验更好
                typeBuilder.AddField(FieldSpec.NewBuilder(fieldTypeName, fieldName, Modifiers.Private).Build());
                typeBuilder.AddField(FieldSpec.NewBuilder(sstiFieldTypeName, sstiFieldName, Modifiers.Private).Build());
                // 两个字段都要拷贝
                copyMethodBuilder.codeBuilder.AddStatement("this.$L = src.$L", fieldName, fieldName);
                copyMethodBuilder.codeBuilder.AddStatement("this.$L = src.$L", sstiFieldName, sstiFieldName);

                // List解码需要传入声明类型
                if (readMethodName == ToolUtil.METHOD_NAME_READ_OBJECT) {
                    constructorBuilder.codeBuilder.AddStatement("this.$L = reader.$L<$T>($S)", fieldName, readMethodName, fieldTypeName, field.SimpleName);
                } else {
                    constructorBuilder.codeBuilder.AddStatement("this.$L = reader.$L($S)", fieldName, readMethodName, field.SimpleName);
                }
            } else {
                typeBuilder.AddProperty(PropertySpec.NewBuilder(fieldTypeName, field.SimpleName, Modifiers.Public)
                    .AddSetterModifiers(field.IsReadonly ? Modifiers.Private : Modifiers.Internal) // 读表可能有特殊逻辑
                    .AddDocument(ToDocument(field.Comments))
                    .Build());
                if (!field.IsReadonly) {
                    copyMethodBuilder.codeBuilder.AddStatement("this.$L = src.$L", field.SimpleName, field.SimpleName);
                }
                if (classCfg != null && classCfg.fieldProxies.TryGetValue(field.SimpleName, out string? fieldDecodeProxy)) {
                    // 这里我们按照标准的DsonCodec代理格式来(inst, reader, name)
                    constructorBuilder.codeBuilder.AddStatement("$T.$L(this, reader, $S)", codecProxy, fieldDecodeProxy, field.SimpleName);
                } else if (readMethodName == ToolUtil.METHOD_NAME_READ_OBJECT) {
                    // ReadObject需要传声明类型
                    constructorBuilder.codeBuilder.AddStatement("this.$L = reader.$L<$T>($S)", field.SimpleName, readMethodName, fieldTypeName, field.SimpleName);
                } else {
                    constructorBuilder.codeBuilder.AddStatement("this.$L = reader.$L($S)", field.SimpleName, readMethodName, field.SimpleName);
                }
            }
        }
        // 处理afterDecode钩子方法 -- 如果超类也定义了AfterDecode，这里可能有点问题，我们暂不处理
        if (classCfg != null && classCfg.fieldProxies.TryGetValue("AfterDecode", out string? methodName)) {
            constructorBuilder.codeBuilder.AddStatement("$T.$L(this, reader.Options)", codecProxy, methodName);
        }

        // 处理扩展字段，扩展字段不包含
        if (classCfg != null && classCfg.extensionFields.Count > 0) {
            foreach (FieldCfg fieldCfg in classCfg.extensionFields) {
                DSTypeElement fieldType = _dsRepository.ResolveTypeSymbol(namedType, fieldCfg.type);
                TypeName fieldTypeName = GetTypeName(fieldType);
                typeBuilder.AddProperty(PropertySpec.NewBuilder(fieldTypeName, fieldCfg.name, Modifiers.Public)
                    .AddSetterModifiers(Modifiers.Internal) // 读表可能有特殊逻辑
                    .AddDocument(fieldCfg.comment ?? "")
                    .Build());
                copyMethodBuilder.codeBuilder.AddStatement("this.$L = src.$L", fieldCfg.name, fieldCfg.name);
            }
        }
        typeBuilder.AddSpec(constructorBuilder.Build(true));
        typeBuilder.AddSpec(copyMethodBuilder.Build(true));
        // TODO 增加ToString -- 需要知道是否是普通表
    }

    private static string GetFieldName(DSField field) {
        return "_" + field.SimpleName;
    }

    private static string GetSstiFieldName(DSField field) {
        return "_" + field.SimpleName + "_cache";
    }

    private static bool IsSstiField(DSField field) {
        Annotation? annotation = field.GetAnnotation(DSAnnotations.OPTIONS);
        if (annotation == null) {
            return false;
        }
        DsonObject<string> options = annotation.DsonValue.AsObject();
        return GetBool(options, DSAnnotations.KEY_SSTI);
    }

    private static CodeBlock ToDocument(List<string> comments) {
        if (comments.Count == 0) return CodeBlock.Empty;
        CodeBlock.Builder builder = CodeBlock.NewBuilder();
        foreach (string comment in comments) {
            if (!builder.IsEmpty) {
                builder.AddNewLine();
            }
            if (comment.StartsWith("// ")) {
                builder.Add(comment.Substring(3));
            } else if (comment.StartsWith("//@")) {
                builder.Add(comment.Substring(2));
            } else {
                builder.Add(comment);
            }
        }
        return builder.Build();
    }

    #region 类型名解析

    private static readonly ClassName TYPE_NAME_LIST_INT = ClassName.Get(typeof(ImmutableList<int>));
    private static readonly ClassName TYPE_NAME_LIST_STRING = ClassName.Get(typeof(ImmutableList<string>));

    /// <summary>
    /// 获取字段类型导出时的TypeName
    /// 这里不是最终数据，因此需要处理泛型变量
    /// </summary>
    /// <returns></returns>
    private TypeName GetTypeName(DSTypeElement typeElement) {
        if (typeElement is DSTypeParameter typeParameter) {
            return typeParameter.TypeName;
        }
        DSNamedType namedType = (DSNamedType)typeElement;
        ClassName metaTypeName = GetMetaTypeName(namedType.OriginDefine);
        if (!metaTypeName.IsGenericType) {
            return metaTypeName;
        }
        List<TypeName> typeArgumentNames = new(namedType.TypeArguments.Count);
        foreach (DSTypeElement typeArgument in namedType.TypeArguments) {
            TypeName typeArgumentName = GetTypeName(typeArgument);
            typeArgumentNames.Add(typeArgumentName);
        }
        return metaTypeName.WithTypeArguments(typeArgumentNames.ToArray());
    }

    private ClassName GetMetaTypeName(DSNamedType originDefine) {
        if (_metaTypeNameCache.TryGetValue(originDefine.FullName, out ClassName? r)) {
            return r;
        }
        // 处理内建类型转换
        r = originDefine.SimpleName switch
        {
            DSKeywords.TYPE_INT32 => TypeName.INT,
            DSKeywords.TYPE_INT64 => TypeName.LONG,
            DSKeywords.TYPE_FLOAT => TypeName.FLOAT,
            DSKeywords.TYPE_DOUBLE => TypeName.DOUBLE,
            DSKeywords.TYPE_BOOL => TypeName.BOOL,
            DSKeywords.TYPE_STRING => TypeName.STRING,
            //
            DSKeywords.TYPE_DATETIME => ToolUtil.TYPE_NAME_DATETIME,
            DSKeywords.TYPE_TIMESTAMP => ToolUtil.TYPE_NAME_TIMESTAMP,
            DSKeywords.TYPE_PAIR => ToolUtil.TYPE_NAME_PAIR,
            //
            DSKeywords.TYPE_LIST => ToolUtil.TYPE_NAME_IMMUTABLE_LIST,
            DSKeywords.TYPE_HASH_SET => ToolUtil.TYPE_NAME_IMMUTABLE_SET,
            DSKeywords.TYPE_MAP => ToolUtil.TYPE_NAME_IMMUTABLE_DICTIONARY,
            //
            DSKeywords.TYPE_NULLABLE => ClassName.NULLABLE,
            DSKeywords.TYPE_OBJECT => TypeName.OBJECT,
            _ => null
        };
        if (r != null) {
            return r;
        }
        string csharpNamespace = GetNamespace(originDefine);
        r = ClassName.Get(csharpNamespace, originDefine.SimpleName, originDefine.TypeName.typeArguments);
        _metaTypeNameCache.Add(originDefine.FullName, r);
        return r;
    }

    private static string GetNamespace(DSNamedType originDefine) {
        // 先查看type是否指定了命名空间 -- 可能引用其它命名空间的文件
        Annotation? annotation = originDefine.GetAnnotation(DSAnnotations.NAMESPACE);
        if (annotation != null) {
            DsonObject<string> dsonObject = annotation.DsonValue.AsObject();
            if (dsonObject.TryGetValue(DSAnnotations.KEY_CS, out DsonValue value)) {
                return value.AsString();
            }
        }
        // 再根据文件options查询
        DSFile enclosingFile = originDefine.GetEnclosingFile();
        return GetNamespace(enclosingFile);
    }

    private static string GetNamespace(DSFile dsFile) {
        string? csharpNamespace = dsFile.GetOption(DSKeywords.CSHARP_NAMESPACE);
        if (string.IsNullOrEmpty(csharpNamespace)) {
            throw new InvalidOperationException("csharpNamespace is absent" + dsFile.FileName);
        }
        return csharpNamespace;
    }

    #endregion
}
}