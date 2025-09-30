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
using System.Linq;
using System.Text;
using Wjybxx.BigCatTool.Core;
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Concurrent;
using Wjybxx.Commons.Poet;
using Wjybxx.Commons.Pool;
using Wjybxx.Dson;
using Wjybxx.Dson.Codec;
using Wjybxx.Dson.Codec.Attributes;
using Wjybxx.Dson.Text;
using Wjybxx.Dson.Types;
using TypeName = Wjybxx.Commons.Poet.TypeName;
using static Wjybxx.BigCatTool.DataScript.DSUtil;

namespace Wjybxx.BigCatTool.DataScript
{
/// <summary>
/// 用于负责代码的生成，用于支持继承扩展
///
/// 1.如果配置了字段的解码代理，则会调度到对应的代码上。
/// 2.如果是ssti字段，则会生成辅助属性，将字符串缓存到辅助字段。
/// 3.如果是数据类，则会生成Equals、GetHashCode、ToString三个方法。
/// 4.默认的CopyFrom是浅拷贝，且不拷贝readonly字段。
/// 5.Equals、GetHashCode、ToString、CopyFrom都不是递归的，因此慎用二维List和字典。
///
/// <h3>关于集合</h3>
/// 1.用户果需要使用不可变集合，请将不可变集合注册到<see cref="DSRepository"/>，默认为Commons库中的不可变集合。
/// 2.默认情况下List使用<code>CollectionUtil.SequenceEqual</code>方法，而Set和字典使用<code>CollectionUtil.DataEquals</code>方法。
/// 3.系统库的HashSet和Dictionary，如果反复增删数据，hashcode无法保证一致 -- 只增不减的情况下才能保证hashcode和equals一致。
/// </summary>
public class CodeGeneratorHelper
{
    protected readonly CodeGeneratorCfg generatorCfg;
    private readonly AttributeSpec processorInfo;
    // 缓存
    private readonly Dictionary<string, ClassName> _metaTypeNameCache = new(200);
    private readonly Dictionary<ClassName, ClassName> _genericTypeNameCache = new(200);

    private readonly List<FieldSpec> _fieldListCache = new(20);
    private readonly List<PropertySpec> _propertyListCache = new(20);
    private readonly List<FieldSpec> _copyFieldListCache = new(20);

    private readonly List<DSField> _dsFieldListCache = new(20);
    private readonly List<DSMethod> _dsMethodListCache = new(20);
    private readonly StringBuilder _sb = new StringBuilder(64);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="generatorCfg">生成器相关配置</param>
    /// <param name="processorInfo"></param>
    public CodeGeneratorHelper(CodeGeneratorCfg generatorCfg, AttributeSpec processorInfo) {
        this.generatorCfg = generatorCfg;
        this.processorInfo = processorInfo;
    }

    /// <summary>
    /// 该方法为虚方法，子类可以在构建完成之后追加实现接口等逻辑
    /// </summary>
    public virtual TypeSpec.Builder Generate(DSNamedType namedType) {
        TypeSpec.Builder typeBuilder = namedType.TypeKind switch
        {
            DSTypeKind.Class => TypeSpec.NewClassBuilder(namedType.SimpleName),
            DSTypeKind.Struct => TypeSpec.NewStructBuilder(namedType.SimpleName),
            DSTypeKind.Enum => TypeSpec.NewEnumBuilder(namedType.SimpleName),
            DSTypeKind.Service => TypeSpec.NewInterfaceBuilder(namedType.SimpleName),
            _ => throw new InvalidOperationException("unknown type kind: " + namedType.TypeKind)
        };
        typeBuilder.AddModifiers(Modifiers.Public);
        if (namedType.EnclosingElement.Kind == DSElementKind.File) {
            typeBuilder.AddAttribute(processorInfo); // 顶层类追加生成器信息
        }
        if (namedType.Kind == DSElementKind.Enum) {
            GenerateEnum(namedType, typeBuilder);
        } else if (namedType.Kind == DSElementKind.Service) {
            GenerateService(namedType, typeBuilder);
            GenerateNestedTypes(namedType, typeBuilder);
        } else {
            GenerateClass(namedType, typeBuilder);
        }
        typeBuilder.AddDocument(BuildDocument(namedType.Comments));
        return typeBuilder;
    }

    private void GenerateNestedTypes(DSNamedType namedType, TypeSpec.Builder typeBuilder) {
        foreach (DSElement enclosedElement in namedType.EnclosedElements) {
            if (!enclosedElement.Kind.IsNamedType()) continue;
            DSNamedType nestedType = (DSNamedType)enclosedElement;
            typeBuilder.AddSpec(Generate(nestedType).Build());
        }
    }

    public static CodeBlock BuildDocument(List<string> comments) {
        if (comments.Count == 0) return CodeBlock.Empty;
        CodeBlock.Builder builder = CodeBlock.NewBuilder();
        foreach (string comment in comments) {
            if (string.IsNullOrWhiteSpace(comment)) {
                continue;
            }
            if (!builder.IsEmpty) {
                builder.AddNewLine();
            }
            if (comment.StartsWith("//")) {
                int idx = ToolUtil.IndexOfNonWhitespace(comment, 2);
                builder.Add(comment.Substring(idx));
            } else {
                builder.Add(comment.TrimStart());
            }
        }
        return builder.Build();
    }

    private void GenerateEnum(DSNamedType namedType, TypeSpec.Builder typeBuilder) {
        DsonObject<string> options = DSUtil.GetOptions(namedType);
        // 增加Flags注解
        if (Annotation.GetBool(options, DSAnnotations.KEY_IS_FLAGS)) {
            typeBuilder.AddAttribute(ATTRIBUTE_FLAGS);
        }
        foreach (DSElement enclosedElement in namedType.EnclosedElements) {
            if (enclosedElement.Kind != DSElementKind.EnumValue) {
                continue;
            }
            DSEnumValue enumValue = (DSEnumValue)enclosedElement;
            typeBuilder.AddEnumValue(new EnumValueSpec(enumValue.SimpleName, enumValue.Number, BuildDocument(enumValue.Comments)));
        }
    }

    #region service

    private void GenerateNormalService(DSNamedType namedType, TypeSpec.Builder typeBuilder) {
        if (namedType.IsGenericType) {
            foreach (DSTypeParameter typeParameter in namedType.DeclaredTypeParameters) {
                typeBuilder.AddTypeParameter(TypeParameterSpec.Get(typeParameter.SimpleName, typeParameter.Constraints));
            }
        }
        foreach (DSMethod method in namedType.GetMethods(false, _dsMethodListCache.ClearAndReturn())) {
            MethodSpec.Builder methodBuilder = MethodSpec.NewMethodBuilder(method.SimpleName);
            if (method.ParameterType != null) {
                methodBuilder.AddParameter(GetTypeName(method.ParameterType), method.ParameterName!);
            }
            if (method.ResultType != null) {
                methodBuilder.Returns(GetTypeName(method.ResultType));
            }
            methodBuilder.AddDocument(BuildDocument(method.Comments));
            typeBuilder.AddMethod(methodBuilder.Build());
        }
    }

    protected virtual void GenerateService(DSNamedType namedType, TypeSpec.Builder typeBuilder) {
        Annotation annotation = namedType.GetAnnotation(DSAnnotations.RPC);
        if (annotation == null) {
            GenerateNormalService(namedType, typeBuilder);
            return;
        }
        // RPC服务
        foreach (string superinterface in generatorCfg.serviceBaseTypes) {
            ClassName className = ToolUtil.ClassNameOfCanonicalName(superinterface);
            typeBuilder.AddBaseClass(className);
        }
        // service注解
        DsonObject<string> serviceData = annotation.AsObject();
        {
            AttributeSpec.Builder annoBuilder = AttributeSpec.NewBuilder(TYPE_NAME_RPC_SERVICE)
                .AddMember(PNAME_SERVICE_ID, GetServiceId(serviceData).ToString());
            typeBuilder.AddAttribute(annoBuilder.Build());
        }
        //
        foreach (DSMethod method in namedType.GetMethods(false, _dsMethodListCache.ClearAndReturn())) {
            DsonObject<string> methodData = DSUtil.GetRpcOptions(method);
            MethodSpec.Builder methodBuilder = MethodSpec.NewMethodBuilder(method.SimpleName);
            // method注解 
            {
                AttributeSpec.Builder annoBuilder = AttributeSpec.NewBuilder(TYPE_NAME_RPC_METHOD)
                    .AddMember(PNAME_METHOD_ID, method.Number.ToString());
                // .addMember("ArgSharable", "false") // ds类型默认也不是不可变的
                // .addMember("ResultSharable", "false"); 
                // 是否手动返回结果
                if (IsManualReturn(methodData, serviceData)) {
                    annoBuilder.AddMember(PNAME_MANUAL_RETURN, "true");
                }
                // 自定义数据-字符串
                Annotation custom = method.GetAnnotation(DSAnnotations.RPC_CUSTOM);
                if (custom != null) {
                    annoBuilder.AddMember(PNAME_CUSTOM_DATA, "$S", custom.value);
                }
                methodBuilder.AddAttribute(annoBuilder.Build());
            }
            // 处理方法的模式
            if (IsAsyncMethod(methodData, serviceData)) {
                BuildWithAsyncMode(method, methodData, serviceData, methodBuilder);
            } else {
                BuildWithSyncMode(method, methodData, serviceData, methodBuilder);
            }
            // 方法注释
            methodBuilder.AddDocument(BuildDocument(method.Comments));
            typeBuilder.AddMethod(methodBuilder.Build());
        }
    }

    private void BuildWithSyncMode(DSMethod method, DsonObject<string> methodData, DsonObject<string> serviceData,
                                   MethodSpec.Builder methodBuilder) {
        // 仅处理void
        TypeName returnType;
        if (method.ResultType != null) {
            returnType = GetTypeName(method.ResultType);
        } else {
            returnType = TypeName.VOID;
        }
        methodBuilder.Returns(returnType);

        // 是否需要context参数
        if (IsRequireContext(methodData, serviceData)) {
            methodBuilder.AddParameter(ParseRpcContextType(method), "rpcContext");
        }
        // 正常参数
        if (method.ParameterType != null) {
            TypeName argType = GetTypeName(method.ParameterType);
            string argName = method.ParameterName ?? "request";
            methodBuilder.AddParameter(argType, argName);
        }
    }

    private void BuildWithAsyncMode(DSMethod method, DsonObject<string> methodData, DsonObject<string> serviceData,
                                    MethodSpec.Builder methodBuilder) {
        // 返回值类型封装为future
        TypeName returnType;
        if (method.ResultType != null) {
            TypeName resultType = GetTypeName(method.ResultType);
            returnType = TYPE_NAME_VALUE_FUTURE_T.WithTypeArguments(resultType);
        } else {
            returnType = TYPE_NAME_VALUE_FUTURE;
        }
        methodBuilder.Returns(returnType);

        // 是否需要context参数--插在首位
        if (IsRequireContext(methodData, serviceData)) {
            methodBuilder.AddParameter(ParseRpcContextType(method), "rpcCtx");
        }
        // 正常参数
        if (method.ParameterType != null) {
            TypeName argType = GetTypeName(method.ParameterType);
            string argName = method.ParameterName ?? "request";
            methodBuilder.AddParameter(argType, argName);
        }
    }

    private TypeName ParseRpcContextType(DSMethod method) {
        TypeName contextType;
        if (method.ResultType != null) {
            TypeName resultType = GetTypeName(method.ResultType);
            contextType = TYPE_NAME_RPC_CONTEXT_T.WithTypeArguments(resultType);
        } else {
            // void时使用object代替 -- 可临时返回结果
            contextType = TYPE_NAME_RPC_CONTEXT_T.WithTypeArguments(TypeName.OBJECT);
        }
        // c#需要传引用
        return contextType.MakeByRefType();
    }

    #endregion

    #region class-struct

    private void GenerateClass(DSNamedType namedType, TypeSpec.Builder typeBuilder) {
        DsonObject<string> options = DSUtil.GetOptions(namedType);
        // 继承
        if (namedType.BaseType != null) {
            typeBuilder.AddBaseClass(GetTypeName(namedType.BaseType));
        }
        // 泛型参数
        if (namedType.IsGenericType) {
            foreach (DSTypeParameter typeParameter in namedType.DeclaredTypeParameters) {
                typeBuilder.AddTypeParameter(TypeParameterSpec.Get(typeParameter.SimpleName, typeParameter.Constraints));
            }
        }
        // 注解
        InitAttributes(namedType, options, typeBuilder);
        if (NeedCodecMethod(namedType, options)) {
            typeBuilder.AddAttribute(BuildCodecAttribute(namedType, _sb.Clear()));
        }
        // 禁用nullable提示
        typeBuilder.AddSpec(MacroSpec.Get("nullable", "disable"));
        typeBuilder.AddSpec(new CodeBlockSpec(CodeBlock.Of("ReSharper disable All"), CodeBlockSpec.Kind.Comment));

        List<FieldSpec> fieldSpecs = _fieldListCache;
        List<PropertySpec> propertySpecs = _propertyListCache;
        List<FieldSpec> copyFieldList = _copyFieldListCache;
        // 原生字段
        foreach (DSField field in namedType.GetFields(false, _dsFieldListCache.ClearAndReturn())) {
            DsonObject<string> fieldOptions = DSUtil.GetOptions(field);
            BuildFieldAndProperty(field, fieldOptions, out FieldSpec fieldSpec, out PropertySpec propertySpec);
            fieldSpecs.Add(fieldSpec);
            propertySpecs.Add(propertySpec);
            // readonly字段不拷贝
            if (!field.IsReadonly) {
                copyFieldList.Add(fieldSpec);
            }
            // 如果是指向共享字符串表的索引，则增加缓存字段 + 属性
            if (Annotation.GetBool(fieldOptions, DSAnnotations.KEY_SSTI)) {
                BuildSstiFieldAndProperty(field, fieldSpec.name, propertySpec.name, out FieldSpec? sstiFiledSpec, out PropertySpec sstiPropertySpec);
                if (sstiFiledSpec != null) {
                    fieldSpecs.Add(sstiFiledSpec);
                    copyFieldList.Add(sstiFiledSpec);
                }
                // 如果ssti属性和原字段属性名相同，则覆盖原字段的属性
                if (sstiPropertySpec.name == propertySpec.name) {
                    propertySpecs.TryRemoveLast(out _);
                }
                propertySpecs.Add(sstiPropertySpec);
            }
        }
        typeBuilder.AddSpecs(fieldSpecs);
        // 构造函数在字段后 -- 没有readonly字段时生成空构造函数，因为存在reader构造器
        typeBuilder.AddSpec(BuildExplicitConstructor(namedType));
        // 属性在构造函数后面
        typeBuilder.AddSpecs(propertySpecs);

        // 编解码钩子在属性后
        // 所有字段都在reader构造函数中解码，可以降低生成代码的复杂度 -- 尤其是有字段读写代理的时候
        // 为保证代码的正确性，所有的类都生成BeforeEncode和AfterDecode方法，否则子类无法确定超类是否包含该方法
        if (NeedCodecMethod(namedType, options)) {
            typeBuilder.AddSpec(MacroSpec.Get("region", "codec"));
            CodeGeneratorCfg.ClassCodecCfg? classCfg = GetClassCfg(namedType);
            typeBuilder.AddSpec(BuildReaderConstructor(namedType, classCfg));
            typeBuilder.AddSpec(BuildReaderObjectMethod(namedType, classCfg));
            typeBuilder.AddSpec(BuildReadFieldMethod(namedType, classCfg));
            typeBuilder.AddSpec(BuildWriteObjectMethod(namedType, classCfg));
            BuildCodecHookMethods(namedType, classCfg, typeBuilder);
            typeBuilder.AddSpec(MacroSpec.Get("endregion"));
        }

        // 允许在Copy方法前插入代码
        BeforeGenerateCopyMethod(namedType, typeBuilder, options);
        // 生成CopyFrom
        if (NeedCopyMethod(namedType, options)) {
            typeBuilder.AddSpec(MacroSpec.Get("region", "copy"));
            typeBuilder.AddSpec(BuildCopyMethod(namedType, copyFieldList));
            typeBuilder.AddSpec(MacroSpec.Get("endregion"));
        }
        // 生成equals和hashcode
        bool isDataClass = IsDataClass(namedType, options);
        if (isDataClass) {
            typeBuilder.AddSpec(MacroSpec.Get("region", "equals"));
            BuildEqualsMethod(namedType, typeBuilder);
            BuildHashCodeMethod(namedType, typeBuilder);
            typeBuilder.AddSpec(MacroSpec.Get("endregion"));
        }
        // 生成ToString
        if (isDataClass || NeedToStringMethod(namedType, options)) {
            typeBuilder.AddSpec(MacroSpec.Get("region", "ToString"));
            BuildToStringMethod(namedType, typeBuilder);
            typeBuilder.AddSpec(MacroSpec.Get("endregion"));
        }
        _fieldListCache.Clear();
        _propertyListCache.Clear();
        _copyFieldListCache.Clear();
        // 允许用户在生成内部类之前插入方法
        BeforeGenerateNestedTypes(namedType, typeBuilder, options);
        GenerateNestedTypes(namedType, typeBuilder);
    }

    #endregion

    #region 扩展钩子

    protected virtual void InitAttributes(DSNamedType namedType, DsonObject<string> options, TypeSpec.Builder typeBuilder) {

    }

    /// <summary>
    /// 是否是数据类 -- 是否生成equals和hashcode
    /// </summary>
    protected virtual bool IsDataClass(DSNamedType namedType, DsonObject<string> options) {
        return options.ContainsKey(DSAnnotations.KEY_DATA_CLASS)
            ? Annotation.GetBool(options, DSAnnotations.KEY_DATA_CLASS)
            : namedType.GetEnclosingFile().GetOption(DSKeywords.DATA_CLASS) == "true";
    }

    /// <summary>
    /// 是否生成CopyFrom方法
    /// </summary>
    /// <returns></returns>
    protected virtual bool NeedCopyMethod(DSNamedType namedType, DsonObject<string> options) {
        return !namedType.IsValueType && namedType.GetMethod("CopyFrom") != null;
    }

    /// <summary>
    /// 是否需要生成DsonCodec相关支持
    /// </summary>
    protected virtual bool NeedCodecMethod(DSNamedType namedType, DsonObject<string> options) {
        return true;
    }

    /// <summary>
    /// 是否需要生成ToString方法 -- DataClass一定会生成ToString
    /// </summary>
    /// <returns></returns>
    protected virtual bool NeedToStringMethod(DSNamedType namedType, DsonObject<string> options) {
        return false;
    }

    /// <summary>
    /// 生成Copy和Equals方法前调用，用户可以在这之前插入方法
    /// （走到这不代表一定会生成Copy或Equals方法，只是代码位置到了）
    /// </summary>
    protected virtual void BeforeGenerateCopyMethod(DSNamedType namedType, TypeSpec.Builder typeBuilder, DsonObject<string> options) {

    }

    /// <summary>
    /// 生成子类型前的钩子方法，用户可以在这之前插入方法
    /// </summary>
    protected virtual void BeforeGenerateNestedTypes(DSNamedType namedType, TypeSpec.Builder typeBuilder, DsonObject<string> options) {
    }

    #endregion

    #region 字段和属性

    /// <summary>
    /// 构建普通字段和对应属性
    /// </summary>
    protected virtual void BuildFieldAndProperty(DSField field, DsonObject<string> fieldOptions,
                                                 out FieldSpec fieldSpec, out PropertySpec propertySpec) {
        Modifiers fieldModifiers = GetFieldModifiers(field, fieldOptions);
        if (field.IsReadonly) {
            // fieldModifiers |= Modifiers.ReadOnly; // 改为private set模拟
        }
        TypeName fieldTypeName = GetTypeName(field.Type);
        FieldSpec.Builder fieldBuilder = FieldSpec.NewBuilder(GetTypeName(field.Type), GetFieldName(field.SimpleName), fieldModifiers)
            .AddDocument(BuildDocument(field.Comments));
        if (Annotation.GetBool(fieldOptions, DSAnnotations.KEY_NON_SERIALIZED)) {
            fieldBuilder.AddAttribute(ATTRIBUTE_NON_SERIALIZED);
        }
        CodeBlock initializer = GetFieldInitializer(field, fieldOptions, fieldTypeName);
        if (initializer != null) {
            fieldBuilder.Initializer(initializer);
        }
        fieldSpec = fieldBuilder.Build();
        //
        PropertySpec.Builder propertyBuilder = PropertySpec.NewBuilder(fieldSpec.type, GetPropertyName(field.SimpleName), Modifiers.Public);
        propertyBuilder.Getter(CodeBlock.Of("$L", fieldSpec.name).WithExpressionStyle());
        if (field.IsReadonly) {
            propertyBuilder.RemoveSetter();
        } else {
            Modifiers setterModifiers = GetSetterModifiers(field, fieldOptions);
            if (setterModifiers != Modifiers.Public) {
                propertyBuilder.AddSetterModifiers(setterModifiers);
            }
            propertyBuilder.Setter(CodeBlock.Of("this.$L = value", fieldSpec.name).WithExpressionStyle());
        }
        propertySpec = propertyBuilder.Build();
    }

    /// <summary>
    /// 构建sst字符串字段和属性
    /// TODO ClearCache方法
    /// </summary>
    private void BuildSstiFieldAndProperty(DSField field, string fieldName, string propertyName,
                                           out FieldSpec? sstiFieldSpec, out PropertySpec sstiPropertySpec) {
        GetSstiFieldAndPropertyName(fieldName, propertyName, out string sstiFieldName, out string sstiPropertyName);
        if (IsListType(field.Type)) {
            // List类型增加缓存字段
            TypeName sstiFieldTypeName = TYPE_NAME_IMMUTABLE_LIST_STRING;
            sstiFieldSpec = FieldSpec.NewBuilder(sstiFieldTypeName, sstiFieldName, Modifiers.Private)
                .AddAttribute(ATTRIBUTE_NON_SERIALIZED) // 避免被其它框架序列化
                .Build();

            sstiPropertySpec = PropertySpec.NewBuilder(sstiFieldTypeName, sstiPropertyName, Modifiers.Public)
                .Getter(CodeBlock.Of("$L ??= $T.GetStringList($L)", sstiFieldName, TYPE_NAME_SST_MGR, fieldName).WithExpressionStyle())
                .RemoveSetter()
                .Build();
        } else {
            sstiFieldSpec = null;
            sstiPropertySpec = PropertySpec.NewBuilder(TypeName.STRING, sstiPropertyName, Modifiers.Public)
                .Getter(CodeBlock.Of("$T.GetString($L)", TYPE_NAME_SST_MGR, fieldName).WithExpressionStyle())
                .RemoveSetter()
                .Build();
        }
    }

    /// <summary>
    /// 获取字段的初始值
    /// </summary>
    protected virtual CodeBlock? GetFieldInitializer(DSField field, DsonObject<string> fieldOptions, TypeName fieldTypeName) {
        return null;
    }

    /// <summary>
    /// 获取生成字段的名字
    /// </summary>
    /// <returns></returns>
    protected virtual string GetFieldName(string fieldRawName) {
        return "_" + fieldRawName;
    }

    /// <summary>
    /// 获取生成字段对应的属性名
    /// </summary>
    protected virtual string GetPropertyName(string fieldRawName) {
        return fieldRawName;
    }

    /// <summary>
    /// 获取字段对应的修饰符
    /// 用户可以将值类型的字段修正为public的，以避免属性访问时拷贝
    /// </summary>
    protected virtual Modifiers GetFieldModifiers(DSField field, DsonObject<string> fieldOptions) {
        return Modifiers.Private;
    }

    /// <summary>
    /// 获取setter属性的修饰符
    /// </summary>
    protected virtual Modifiers GetSetterModifiers(DSField field, DsonObject<string> fieldOptions) {
        return Modifiers.Public;
    }

    /// <summary>
    /// 获取ssti字段的名字和关联的属性名
    ///
    /// 如果返回的属性名和原字段的属性名相同，则删除原字段的属性 -- 默认相同
    /// </summary>
    protected virtual void GetSstiFieldAndPropertyName(string fieldName, string propertyName,
                                                       out string sstiFieldName, out string sstiPropertyName) {
        sstiFieldName = fieldName + "Cache";
        sstiPropertyName = propertyName;
    }

    #endregion

    #region 编解码方法

    private CodeGeneratorCfg.ClassCodecCfg? GetClassCfg(DSNamedType namedType) {
        string fullName = DSUtil.RemoveFirstName(namedType.FullName);
        foreach (CodeGeneratorCfg.ClassCodecCfg codecCfg in generatorCfg.codecCfgs) {
            if (codecCfg.name == fullName) return codecCfg;
        }
        return null;
    }

    private static CodeGeneratorCfg.FieldCodecCfg? GetFieldCodecCfg(CodeGeneratorCfg.ClassCodecCfg? classCfg, string fieldName) {
        if (classCfg == null) return null;
        foreach (CodeGeneratorCfg.FieldCodecCfg fieldCodecCfg in classCfg.fieldProxies) {
            if (fieldCodecCfg.name == fieldName) return fieldCodecCfg;
        }
        return null;
    }

    private ClassName? GetCodecProxyTypeName(DSNamedType namedType, CodeGeneratorCfg.ClassCodecCfg? classCfg) {
        ClassName typeName = (ClassName)GetTypeName(namedType);
        // CodecProxy需要和原类型保持相同的泛型参数
        return classCfg != null && !string.IsNullOrWhiteSpace(classCfg.proxy)
            ? ClassName.Get(generatorCfg.codecProxyNs, classCfg.proxy, typeName.typeArguments)
            : null;
    }

    /// <summary>
    /// 1.为保证代码的正确性，所有的类都生成BeforeEncode和AfterDecode方法 -- 子类无法确定超类是否包含该方法
    /// 2.不调用基类的钩子方法，因为钩子方法是委托给外部静态类的；若有需要，可手动调用基类静态代理的代码；这可以减少大量的空方法调用
    /// </summary>
    private void BuildCodecHookMethods(DSNamedType namedType, CodeGeneratorCfg.ClassCodecCfg? classCodecCfg, TypeSpec.Builder typeBuilder) {
        if (namedType.IsValueType && (classCodecCfg == null || classCodecCfg.hooks.Count == 0)) {
            return;
        }
        ClassName codecProxy = GetCodecProxyTypeName(namedType, classCodecCfg);
        {
            MethodSpec.Builder methodBuilder = MethodSpec.NewMethodBuilder(METHOD_BEFORE_ENCODE)
                .AddModifiers(Modifiers.Public)
                .AddParameter(TYPE_NAME_CONVERTER_OPTIONS, "options");
            if (namedType.BaseType != null) {
                methodBuilder.AddModifiers(Modifiers.Override);
                // methodBuilder.codeBuilder.AddStatement("base.$L(options)", METHOD_BEFORE_ENCODE);
            } else if (!namedType.IsValueType) {
                methodBuilder.AddModifiers(Modifiers.Virtual);
            }
            if (classCodecCfg != null && classCodecCfg.hooks.TryGetValue(METHOD_BEFORE_ENCODE, out string methodName)) {
                methodBuilder.codeBuilder.AddStatement("$T.$L(this, options)", codecProxy, methodName);
            }
            typeBuilder.AddSpec(methodBuilder.Build(true));
        }
        {
            MethodSpec.Builder methodBuilder = MethodSpec.NewMethodBuilder(METHOD_AFTER_DECODE)
                .AddModifiers(Modifiers.Public)
                .AddParameter(TYPE_NAME_CONVERTER_OPTIONS, "options");
            if (namedType.BaseType != null) {
                methodBuilder.AddModifiers(Modifiers.Override);
                // methodBuilder.codeBuilder.AddStatement("base.$L(options)", METHOD_AFTER_DECODE);
            } else if (!namedType.IsValueType) {
                methodBuilder.AddModifiers(Modifiers.Virtual);
            }
            if (classCodecCfg != null && classCodecCfg.hooks.TryGetValue(METHOD_AFTER_DECODE, out string methodName)) {
                methodBuilder.codeBuilder.AddStatement("$T.$L(this, options)", codecProxy, methodName);
            }
            typeBuilder.AddSpec(methodBuilder.Build(true));
        }
    }

    /// <summary>
    /// 由于要支持读写代理，我们重写WriteObject方法
    ///
    /// 这里的Writer就不像APT那般还支持Style了，非必须功能，避免增加维护工作量...
    /// </summary>
    private MethodSpec BuildWriteObjectMethod(DSNamedType namedType, CodeGeneratorCfg.ClassCodecCfg? classCfg) {
        MethodSpec.Builder methodBuilder = MethodSpec.NewMethodBuilder(METHOD_NAME_WRITE_OBJECT)
            .AddModifiers(Modifiers.Public)
            .AddParameter(TYPE_NAME_WRITER, "writer");
        if (namedType.BaseType != null) {
            methodBuilder.AddModifiers(Modifiers.Override);
            methodBuilder.codeBuilder.AddStatement("base.WriteObject(writer)");
        } else if (!namedType.IsValueType) {
            methodBuilder.AddModifiers(Modifiers.Virtual);
        }
        //
        ClassName? codecProxy = GetCodecProxyTypeName(namedType, classCfg);
        foreach (DSField field in namedType.GetFields(false, _dsFieldListCache.ClearAndReturn())) {
            DsonObject<string> fieldOptions = DSUtil.GetOptions(field);
            if (Annotation.GetBool(fieldOptions, DSAnnotations.KEY_NON_SERIALIZED)) {
                continue;
            }
            CodeGeneratorCfg.FieldCodecCfg? fieldCodecCfg = GetFieldCodecCfg(classCfg, field.SimpleName);
            if (fieldCodecCfg != null && !string.IsNullOrWhiteSpace(fieldCodecCfg.writeProxy)) {
                // 由用户编码 ItemCodecProxy.WriteType(inst, writer)
                methodBuilder.codeBuilder.AddStatement("$T.$L(this, writer, $S)", codecProxy, fieldCodecCfg.writeProxy, field.SimpleName);
                continue;
            }
            string fieldName = GetFieldName(field.SimpleName);
            TypeName fieldTypeName = GetTypeName(field.Type);
            string writeMethodName = GetWriteMethodName(fieldTypeName);

            if (writeMethodName == METHOD_NAME_WRITE_OBJECT) {
                // 写Object时传入类型信息和Style -- 会自动匹配泛型方法，暂不处理Style
                methodBuilder.codeBuilder.AddStatement("writer.$L($S, this.$L)",
                    writeMethodName, field.SimpleName, fieldName);
            } else {
                methodBuilder.codeBuilder.AddStatement("writer.$L($S, this.$L)",
                    writeMethodName, field.SimpleName, fieldName);
            }
        }
        return methodBuilder.Build();
    }

    private MethodSpec BuildReaderConstructor(DSNamedType namedType, CodeGeneratorCfg.ClassCodecCfg? classCfg) {
        MethodSpec.Builder constructorBuilder = MethodSpec.NewConstructorBuilder()
            .AddModifiers(Modifiers.Public)
            .AddParameter(TYPE_NAME_READER, "reader");
        if (namedType.BaseType != null) {
            constructorBuilder.ConstructorInvoker(CodeBlock.Of("base(reader)"));
        } else if (namedType.IsValueType) {
            constructorBuilder.ConstructorInvoker(CodeBlock.Of("this()")); // 结构体在使用前必须完成基础的初始化
        }
        return constructorBuilder.Build();
    }

    private MethodSpec BuildReaderObjectMethod(DSNamedType namedType, CodeGeneratorCfg.ClassCodecCfg? classCfg) {
        MethodSpec.Builder methodBuilder = MethodSpec.NewMethodBuilder(METHOD_NAME_READ_OBJECT)
            .AddModifiers(Modifiers.Public)
            .AddParameter(TYPE_NAME_READER, "reader");
        if (namedType.BaseType != null) {
            methodBuilder.AddModifiers(Modifiers.Override);
            methodBuilder.codeBuilder.AddStatement("base.ReadObject(reader)");
        } else if (!namedType.IsValueType) {
            methodBuilder.AddModifiers(Modifiers.Virtual);
        }

        CodeBlock.Builder codeBuilder = methodBuilder.codeBuilder;
        // Array格式顺序解码 - 读取所有字段
        codeBuilder.BeginControlFlow("if (reader.ContextType == $T.Array)", TYPE_NAME_CONTEXT_TYPE);
        ClassName? codecProxy = GetCodecProxyTypeName(namedType, classCfg);
        foreach (DSField field in namedType.GetFields(false, _dsFieldListCache.ClearAndReturn())) {
            DsonObject<string> fieldOptions = DSUtil.GetOptions(field);
            if (Annotation.GetBool(fieldOptions, DSAnnotations.KEY_NON_SERIALIZED)) {
                continue;
            }
            string fieldName = GetFieldName(field.SimpleName);
            TypeName fieldTypeName = GetTypeName(field.Type);
            string readMethodName = GetReadMethodName(fieldTypeName);

            CodeGeneratorCfg.FieldCodecCfg? fieldCodecCfg = GetFieldCodecCfg(classCfg, field.SimpleName);
            if (fieldCodecCfg != null && !string.IsNullOrWhiteSpace(fieldCodecCfg.readProxy)) {
                codeBuilder.AddStatement("$T.$L(this, reader, $S)", codecProxy, fieldCodecCfg.readProxy, field.SimpleName);
                continue;
            }
            if (readMethodName == METHOD_NAME_READ_OBJECT) {
                // ReadObject需要传声明类型
                codeBuilder.AddStatement("this.$L = reader.$L<$T>(null)", fieldName, readMethodName, fieldTypeName);
            } else {
                codeBuilder.AddStatement("this.$L = reader.$L(null)", fieldName, readMethodName);
            }
        }
        if (namedType.BaseType == null) {
            codeBuilder.AddStatement("return"); // 减少缩进
        }
        codeBuilder.EndControlFlow();

        // Object格式 - 由基类读取整个输入流；构造函数调用虚方法虽然不好，但目前最合适
        if (namedType.BaseType == null) {
            codeBuilder.BeginControlFlow("while (reader.ReadDsonType() != DsonType.EndOfObject)");
            codeBuilder.AddStatement("ReadField(reader, reader.ReadName())");
            codeBuilder.EndControlFlow();
        }
        return methodBuilder.Build();
    }

    private MethodSpec BuildReadFieldMethod(DSNamedType namedType, CodeGeneratorCfg.ClassCodecCfg? classCfg) {
        MethodSpec.Builder methodBuilder = MethodSpec.NewMethodBuilder("ReadField")
            .Returns(TypeName.BOOL)
            .AddParameter(TYPE_NAME_READER, "reader")
            .AddParameter(TypeName.STRING, "name");

        CodeBlock.Builder codeBuilder = methodBuilder.codeBuilder;
        if (namedType.BaseType != null) {
            methodBuilder.AddModifiers(Modifiers.Protected | Modifiers.Override);
            codeBuilder.AddStatement("if (base.ReadField(reader, name)) return true");
        } else if (!namedType.IsValueType) {
            methodBuilder.AddModifiers(Modifiers.Protected | Modifiers.Virtual);
        } else {
            methodBuilder.AddModifiers(Modifiers.Private);
        }
        codeBuilder.BeginControlFlow("switch (name)");

        ClassName? codecProxy = GetCodecProxyTypeName(namedType, classCfg);
        foreach (DSField field in namedType.GetFields(false, _dsFieldListCache.ClearAndReturn())) {
            DsonObject<string> fieldOptions = DSUtil.GetOptions(field);
            if (Annotation.GetBool(fieldOptions, DSAnnotations.KEY_NON_SERIALIZED)) {
                continue;
            }
            string fieldName = GetFieldName(field.SimpleName);
            TypeName fieldTypeName = GetTypeName(field.Type);
            string readMethodName = GetReadMethodName(fieldTypeName);

            codeBuilder.Add("case $S: ", field.SimpleName);
            // 外部读写代理 -- 不能操作private字段（伪readonly字段）
            CodeGeneratorCfg.FieldCodecCfg? fieldCodecCfg = GetFieldCodecCfg(classCfg, field.SimpleName);
            if (fieldCodecCfg != null && !string.IsNullOrWhiteSpace(fieldCodecCfg.readProxy)) {
                codeBuilder.AddStatement("$T.$L(this, reader, $S); return true", codecProxy, fieldCodecCfg.readProxy, field.SimpleName);
                continue;
            }
            if (readMethodName == METHOD_NAME_READ_OBJECT) {
                // ReadObject需要传声明类型
                codeBuilder.AddStatement("this.$L = reader.$L<$T>(null); return true", fieldName, readMethodName, fieldTypeName);
            } else {
                codeBuilder.AddStatement("this.$L = reader.$L(null); return true", fieldName, readMethodName);
            }
        }
        codeBuilder.AddStatement("default: return false");
        codeBuilder.EndControlFlow();
        return methodBuilder.Build();
    }

    #endregion

    #region 构造函数

    private MethodSpec BuildExplicitConstructor(DSNamedType namedType) {
        MethodSpec.Builder constructorBuilder = MethodSpec.NewConstructorBuilder()
            .AddModifiers(Modifiers.Public);
        // 需要根据所有的readonly定义构造函数
        CodeBlock.Builder? cInvokerBuilder = null;
        foreach (DSField field in namedType.GetFields(true, _dsFieldListCache.ClearAndReturn()).Where(e => e.IsReadonly)) {
            TypeName fieldTypeName = GetTypeName(field.Type);
            constructorBuilder.AddParameter(ParameterSpec.NewBuilder(fieldTypeName, field.SimpleName).Build());
            // base(a, b, c)
            if (!ReferenceEquals(namedType, field.EnclosingElement)) {
                if (cInvokerBuilder == null) {
                    cInvokerBuilder = CodeBlock.NewBuilder().Add("base(");
                } else {
                    cInvokerBuilder.Add(", ");
                }
                cInvokerBuilder.Add("$L", field.SimpleName);
                continue;
            }
            // this._a = a;
            constructorBuilder.codeBuilder.AddStatement("this.$L = $L", GetFieldName(field.SimpleName), field.SimpleName);
        }
        if (cInvokerBuilder != null) {
            cInvokerBuilder.Add(")");
            constructorBuilder.ConstructorInvoker(cInvokerBuilder.Build());
        }
        return constructorBuilder.Build();
    }

    #endregion

    #region copy

    /// <summary>
    /// 默认实现为浅拷贝，子类可以重写该方法以实现深度拷贝
    /// </summary>
    protected virtual MethodSpec BuildCopyMethod(DSNamedType namedType, List<FieldSpec> copyFieldList) {
        TypeName typeName = GetTypeName(namedType);
        MethodSpec.Builder copyMethodBuilder;
        if (namedType.BaseType != null) {
            // 方法参数和修饰符需要调整 -- 表格最大只有一层继承，因此这里可以不递归
            TypeName rootTypeName = GetTypeName(DSUtil.GetRootType(namedType));
            copyMethodBuilder = MethodSpec.NewMethodBuilder("CopyFrom")
                .AddModifiers(Modifiers.Public | Modifiers.Override)
                .AddParameter(rootTypeName, "_src");
            // 方法参数需要强转
            copyMethodBuilder.codeBuilder.AddStatement("base.CopyFrom(_src)");
            copyMethodBuilder.codeBuilder.AddStatement("$T src = ($T)_src", typeName, typeName);
        } else {
            copyMethodBuilder = MethodSpec.NewMethodBuilder("CopyFrom")
                .AddModifiers(Modifiers.Public)
                .AddParameter(typeName, "src");
            if (!namedType.IsValueType) {
                copyMethodBuilder.AddModifiers(Modifiers.Virtual);
            }
        }
        // 逐字段拷贝 - ssti字段不在namedType定义中
        foreach (FieldSpec fieldSpec in copyFieldList) {
            copyMethodBuilder.codeBuilder.AddStatement("this.$L = src.$L", fieldSpec.name, fieldSpec.name);
        }
        return copyMethodBuilder.Build(true);
    }

    #endregion

    #region equals-hashcode

    private void BuildEqualsMethod(DSNamedType namedType, TypeSpec.Builder typeBuilder) {
        TypeName typeName = GetTypeName(namedType);
        MethodSpec.Builder equalsBuilder;
        if (namedType.Kind == DSElementKind.Strut) {
            // 值类型实现IEquatable接口
            typeBuilder.AddBaseClass(TYPE_NAME_IEQUATABLE.WithTypeArguments(typeName));
            // 重写object的Equals
            typeBuilder.AddMethod(MethodSpec.NewMethodBuilder("Equals")
                .AddModifiers(Modifiers.Public | Modifiers.Override)
                .Returns(TypeName.BOOL)
                .AddParameter(TypeName.NRT_OBJECT, "obj")
                .Code(CodeBlock.Of("return obj is $T other && Equals(other);", typeName))
                .Build());
            // 接口类型的Equals
            equalsBuilder = MethodSpec.NewMethodBuilder("Equals")
                .AddModifiers(Modifiers.Public)
                .Returns(TypeName.BOOL)
                .AddParameter(typeName, "other");
        } else {
            // 重写object的Equals -- 引用类型需要测试GetType，值类型不需要
            CodeBlock objEqualsBody = CodeBlock.NewBuilder()
                .AddStatement("if (null == obj) return false")
                .AddStatement("if (ReferenceEquals(this, obj)) return true")
                .AddStatement("if (this.GetType() != obj.GetType()) return false")
                .AddStatement("return EqualsHelper(($T)obj)", typeName)
                .Build();
            typeBuilder.AddMethod(MethodSpec.NewMethodBuilder("Equals")
                .AddModifiers(Modifiers.Public | Modifiers.Override)
                .Returns(TypeName.BOOL)
                .AddParameter(TypeName.NRT_OBJECT, "obj")
                .Code(objEqualsBody)
                .Build());

            // 生成比较值的EqualsHelper方法，protect virtual bool EqualsHelper(T other)
            // 由于可能存在继承，需要先调用超类方法 -- 而且参数也需要调整，一致
            equalsBuilder = MethodSpec.NewMethodBuilder("EqualsHelper").Returns(TypeName.BOOL);
            if (namedType.BaseType != null) {
                TypeName rootTypeName = GetTypeName(DSUtil.GetRootType(namedType));
                equalsBuilder.AddModifiers(Modifiers.Protected | Modifiers.Override)
                    .AddParameter(rootTypeName, "_other");
                // 先强转，如果类型错误直接抛出异常，避免超类无效代码
                equalsBuilder.codeBuilder.AddStatement("var other = ($T)_other", typeName);
                equalsBuilder.codeBuilder.AddStatement("if (!base.EqualsHelper(other)) return false");
            } else {
                equalsBuilder.AddModifiers(Modifiers.Protected | Modifiers.Virtual)
                    .AddParameter(typeName, "other");
            }
        }
        // 逐字段比较
        CodeBlock.Builder codeBuilder = equalsBuilder.codeBuilder;
        foreach (DSField field in namedType.GetFields(false, _dsFieldListCache.ClearAndReturn())) {
            DsonObject<string> fieldOptions = DSUtil.GetOptions(field);
            if (Annotation.GetBool(fieldOptions, DSAnnotations.KEY_NON_EQUAL)) {
                continue;
            }
            string fieldName = GetFieldName(field.SimpleName);
            if (UsingEqualsOperator(field.Type)) {
                // 基础类型 -- 直接使用 '==' 比较
                codeBuilder.AddStatement("if (this.$L != other.$L) return false", fieldName, fieldName);
            } else if (field.Type.IsValueType) {
                // 值类型 -- 值类型不为null，避免装箱；其实我们的值类型也重写了==操作符
                codeBuilder.AddStatement("if (!this.$L.Equals(other.$L)) return false", fieldName, fieldName);
            } else if (IsListType(field.Type)) {
                // List默认使用SequenceEqual -- Util类处理了null
                codeBuilder.AddStatement("if (!$T.SequenceEqual(this.$L, other.$L)) return false", TYPE_NAME_COLLECTION_UTIL, fieldName, fieldName);
            } else if (IsSetOrMapType(field.Type)) {
                // 集合和字典使用DataEquals
                codeBuilder.AddStatement("if (!$T.DataEquals(this.$L, other.$L)) return false", TYPE_NAME_COLLECTION_UTIL, fieldName, fieldName);
            } else {
                // 其它引用类型 -- 使用object下的equals，泛型参数会走到这
                codeBuilder.AddStatement("if (!Equals(this.$L, other.$L)) return false", fieldName, fieldName);
            }
        }
        codeBuilder.AddStatement("return true");
        typeBuilder.AddSpec(equalsBuilder.Build());

        // 如果是值类型，重写==操作符，这里用CodeBlock构建更简单些 -- 需要额外的缩进
        if (namedType.Kind == DSElementKind.Strut) {
            CodeBlock eqOperator = CodeBlock.NewBuilder()
                .BeginControlFlow("public static bool operator ==($T left, $T right)", typeName, typeName)
                .AddStatement("return left.Equals(right)")
                .EndControlFlow()
                .Build();
            CodeBlock neqOperator = CodeBlock.NewBuilder()
                .BeginControlFlow("public static bool operator !=($T left, $T right)", typeName, typeName)
                .AddStatement("return !left.Equals(right)")
                .EndControlFlow()
                .Build();
            typeBuilder
                .AddSpec(CODE_NEW_LINE)
                .AddSpec(new CodeBlockSpec(eqOperator))
                .AddSpec(CODE_NEW_LINE)
                .AddSpec(new CodeBlockSpec(neqOperator));
        }
    }

    private void BuildHashCodeMethod(DSNamedType namedType, TypeSpec.Builder typeBuilder) {
        // 重写object的GetHashCode
        MethodSpec.Builder methodBuilder = MethodSpec.NewMethodBuilder("GetHashCode")
            .AddModifiers(Modifiers.Public | Modifiers.Override)
            .Returns(TypeName.INT);
        // 变量名需要特殊一点，避免和正常字段冲突
        if (namedType.BaseType != null) {
            methodBuilder.codeBuilder.AddStatement("int hashCode = base.GetHashCode()");
        }
        // 逐字段计算
        CodeBlock.Builder codeBuilder = methodBuilder.codeBuilder;
        foreach (DSField field in namedType.GetFields(false, _dsFieldListCache.ClearAndReturn())) {
            DsonObject<string> fieldOptions = DSUtil.GetOptions(field);
            if (Annotation.GetBool(fieldOptions, DSAnnotations.KEY_NON_EQUAL)) {
                continue;
            }
            string fieldName = GetFieldName(field.SimpleName);
            // 在首个字段处声明变量
            codeBuilder.Add(codeBuilder.IsEmpty ? "int hashCode = " : "hashCode = (hashCode * 397) ^ ");
            if (field.Type.IsValueType) {
                // 值类型直接调用HashCode
                codeBuilder.AddStatement("this.$L.GetHashCode()", fieldName);
            } else if (IsListType(field.Type) || IsSetOrMapType(field.Type)) {
                // 集合类型调用Util的HashCode
                codeBuilder.AddStatement("$T.HashCode(this.$L)", TYPE_NAME_COLLECTION_UTIL, fieldName);
            } else {
                // 手动测试null
                codeBuilder.AddStatement("(this.$L != null ? this.$L.GetHashCode() :  0)", fieldName, fieldName);
            }
        }
        if (codeBuilder.IsEmpty) {
            codeBuilder.AddStatement("return 1");
        } else {
            codeBuilder.AddStatement("return hashCode");
        }
        typeBuilder.AddSpec(methodBuilder.Build());
    }

    /** 是否使用 '==' 操作符测试相等性，<see cref="Nullable{T}"/> */
    protected virtual bool UsingEqualsOperator(DSElement typeElement) {
        if (typeElement.Kind.IsNamedType()) {
            return typeElement.SimpleName switch
            {
                DSKeywords.TYPE_INT32 => true,
                DSKeywords.TYPE_INT64 => true,
                DSKeywords.TYPE_FLOAT => true,
                DSKeywords.TYPE_DOUBLE => true,
                DSKeywords.TYPE_BOOL => true,
                DSKeywords.TYPE_STRING => true,
                DSKeywords.TYPE_DATETIME => true,
                DSKeywords.TYPE_NULLABLE => UsingEqualsOperator(((DSNamedType)typeElement).TypeArguments[0]), // Nullable需要检测Value
                _ => false
            };
        }
        return false;
    }

    /** 是否是csharp基本类型 */
    private static bool IsPrimitiveType(DSElement typeElement) {
        if (typeElement.Kind.IsNamedType()) {
            return typeElement.SimpleName switch
            {
                DSKeywords.TYPE_INT32 => true,
                DSKeywords.TYPE_INT64 => true,
                DSKeywords.TYPE_FLOAT => true,
                DSKeywords.TYPE_DOUBLE => true,
                DSKeywords.TYPE_BOOL => true,
                _ => false
            };
        }
        return false;
    }

    private static bool IsSetOrMapType(DSElement typeElement) {
        return IsSetType(typeElement) || IsMapType(typeElement);
    }

    #endregion

    #region ToString

    /// <summary>
    /// 默认格式<code>K1: V1, K2: V2 ...</code>
    /// </summary>
    /// <param name="namedType"></param>
    /// <param name="typeBuilder"></param>
    protected virtual void BuildToStringMethod(DSNamedType namedType, TypeSpec.Builder typeBuilder) {
        MethodSpec.Builder methodBuilder = MethodSpec.NewMethodBuilder("ToString")
            .AddModifiers(Modifiers.Public | Modifiers.Override)
            .Returns(TypeName.STRING);

        CodeBlock.Builder codeBuilder = methodBuilder.codeBuilder;
        codeBuilder.AddStatement("var sb = new $T()", TYPE_NAME_STRING_BUILDER);
        int count = 0;
        if (namedType.BaseType != null) {
            count = 1;
            codeBuilder.AddStatement("sb.Append(base.ToString())");
        }
        // 逐字段追加
        foreach (DSField field in namedType.GetFields(false, _dsFieldListCache.ClearAndReturn())) {
            if (count++ > 0) {
                codeBuilder.AddStatement("sb.Append(\", \")");
            }
            string fieldName = GetFieldName(field.SimpleName);
            // 集合类型调用Util类的ToString，避免创建额外的StringBuilder
            if (IsListType(field.Type) || IsSetOrMapType(field.Type)) {
                codeBuilder.AddStatement("sb.Append($S).Append(':')", field.SimpleName);
                codeBuilder.AddStatement("$T.ToStringHelper(this.$L, sb)", TYPE_NAME_COLLECTION_UTIL, fieldName);
                continue;
            }
            codeBuilder.Add("sb.Append($S).Append(':').Append(", field.SimpleName);
            if (IsPrimitiveType(field.Type)) {
                // 基本类型直接调用StringBuilder的Append，避免额外的ToString调用
                codeBuilder.Add("this.$L", fieldName);
            } else if (DSUtil.IsDateTimeType(field.Type)) {
                // DateTime指定ISO8601格式
                codeBuilder.Add("this.$L.ToString(\"s\")", fieldName);
            } else if (DSUtil.IsNullableType(field.Type)) {
                // Nullable我们追加Null -- 默认的ToString返回的是空字符串
                codeBuilder.Add("this.$L != null ? this.$L.ToString() :  \"null\"", fieldName, fieldName);
            } else if (field.Type.IsValueType) {
                // 普通值类型直接调用ToString
                codeBuilder.Add("this.$L.ToString()", fieldName);
            } else {
                // 手动测试null
                codeBuilder.Add("this.$L != null ? this.$L.ToString() :  \"null\"", fieldName, fieldName);
            }
            codeBuilder.Add(");\n");
        }
        codeBuilder.AddStatement("return sb.ToString()");
        typeBuilder.AddSpec(methodBuilder.Build());
    }

    #endregion

    #region 类型名解析

    private static readonly ArrayTypeName TYPE_NAME_BYTE_ARRAY = ArrayTypeName.BYTE_ARRAY;
    public static readonly ClassName TYPE_NAME_DATETIME = ClassName.DATETIME;
    public static readonly ClassName TYPE_NAME_PAIR = ClassName.Get(typeof(KeyValuePair<,>));

    public static readonly ClassName TYPE_NAME_BINARY = ClassName.Get(typeof(Binary));
    public static readonly ClassName TYPE_NAME_PTR = ClassName.Get(typeof(ObjectPtr));
    public static readonly ClassName TYPE_NAME_LPTR = ClassName.Get(typeof(ObjectLitePtr));
    public static readonly ClassName TYPE_NAME_TIMESTAMP = ClassName.Get(typeof(Timestamp));
    // 集合接口
    public static readonly ClassName TYPE_NAME_ICOLLECTION = ClassName.Get(typeof(ICollection<>));
    public static readonly ClassName TYPE_NAME_ILIST = ClassName.Get(typeof(IList<>));
    public static readonly ClassName TYPE_NAME_ISET = ClassName.Get(typeof(ISet<>));
    public static readonly ClassName TYPE_NAME_IDICTIONARY = ClassName.Get(typeof(IDictionary<,>));
    // 常用集合
    public static readonly ClassName TYPE_NAME_LIST = ClassName.Get(typeof(List<>));
    public static readonly ClassName TYPE_NAME_HASHSET = ClassName.Get(typeof(HashSet<>));
    public static readonly ClassName TYPE_NAME_DICTIONARY = ClassName.Get(typeof(Dictionary<,>));
    public static readonly ClassName TYPE_NAME_LINKED_HASHSET = ClassName.Get(typeof(LinkedHashSet<>));
    public static readonly ClassName TYPE_NAME_LINKED_DICTIONARY = ClassName.Get(typeof(LinkedDictionary<,>));
    // 不可变集合
    public static readonly ClassName TYPE_NAME_IMMUTABLE_LIST = ClassName.Get(typeof(ImmutableList<>));
    public static readonly ClassName TYPE_NAME_IMMUTABLE_SET = ClassName.Get(typeof(ImmutableSet<>));
    public static readonly ClassName TYPE_NAME_IMMUTABLE_DICTIONARY = ClassName.Get(typeof(ImmutableDictionary<,>));

    /// <summary>
    /// 获取字段类型导出时的TypeName
    /// 这里不是最终数据，因此需要处理泛型变量
    /// </summary>
    /// <returns></returns>
    protected TypeName GetTypeName(DSTypeElement typeElement) {
        if (typeElement is DSTypeParameter typeParameter) {
            return typeParameter.TypeName;
        }
        DSNamedType namedType = (DSNamedType)typeElement;
        ClassName metaTypeName = GetMetaTypeName(namedType.OriginNamedType);
        if (!metaTypeName.IsGenericType) {
            return metaTypeName;
        }
        if (_genericTypeNameCache.TryGetValue(namedType.TypeName, out ClassName r)) {
            return r;
        }
        // 这里可能是泛型原型 -- 泛型原型我们也调用这里的方法构建ClassName
        List<TypeName> typeArgumentNames = new(metaTypeName.typeArguments.Count);
        if (namedType.IsGenericTypeDefinition) {
            foreach (var type in namedType.TypeParameters) {
                typeArgumentNames.Add(type.TypeName);
            }
        } else {
            foreach (DSTypeElement typeArgument in namedType.TypeArguments) {
                TypeName typeArgumentName = GetTypeName(typeArgument);
                typeArgumentNames.Add(typeArgumentName);
            }
        }
        r = metaTypeName.WithTypeArguments(typeArgumentNames.ToArray());
        _genericTypeNameCache.Add(namedType.TypeName, r);
        return r;
    }

    private ClassName GetMetaTypeName(DSNamedType originDefine) {
        if (_metaTypeNameCache.TryGetValue(originDefine.FullName, out ClassName r)) {
            return r;
        }
        // 处理内建类型转换
        r = GetBuiltinMetaTypeName(originDefine);
        if (r != null) {
            _metaTypeNameCache.Add(originDefine.FullName, r);
            return r;
        }
        // 内部类需要A.B.C格式访问
        if (originDefine.EnclosingElement is DSNamedType outerClass) {
            ClassName outerClassName = GetMetaTypeName(outerClass);
            r = outerClassName.NestedClass(originDefine.SimpleName, originDefine.TypeName.typeArguments, false);
        } else {
            string csharpNamespace = GetNamespace(originDefine);
            r = ClassName.Get(csharpNamespace, originDefine.SimpleName, originDefine.TypeName.typeArguments);
        }
        _metaTypeNameCache.Add(originDefine.FullName, r);
        return r;
    }

    /** 获取内建类型的元TypeName */
    protected virtual ClassName? GetBuiltinMetaTypeName(DSNamedType originDefine) {
        return originDefine.SimpleName switch
        {
            DSKeywords.TYPE_INT32 => TypeName.INT,
            DSKeywords.TYPE_INT64 => TypeName.LONG,
            DSKeywords.TYPE_FLOAT => TypeName.FLOAT,
            DSKeywords.TYPE_DOUBLE => TypeName.DOUBLE,
            DSKeywords.TYPE_BOOL => TypeName.BOOL,
            DSKeywords.TYPE_STRING => TypeName.STRING,
            DSKeywords.TYPE_BYTES => TYPE_NAME_BINARY,
            //
            DSKeywords.TYPE_DATETIME => TYPE_NAME_DATETIME,
            DSKeywords.TYPE_TIMESTAMP => TYPE_NAME_TIMESTAMP,
            DSKeywords.TYPE_PAIR => TYPE_NAME_PAIR,
            //
            DSKeywords.TYPE_NULLABLE => ClassName.NULLABLE,
            DSKeywords.TYPE_OBJECT => TypeName.OBJECT,
            //
            DSKeywords.TYPE_LIST => TYPE_NAME_LIST,
            DSKeywords.TYPE_HASHSET => TYPE_NAME_HASHSET,
            DSKeywords.TYPE_MAP => TYPE_NAME_DICTIONARY,
            // 扩展支持
            TYPE_LINKED_HASHSET => TYPE_NAME_LINKED_HASHSET,
            TYPE_LINKED_MAP => TYPE_NAME_LINKED_DICTIONARY,

            TYPE_IMMUTABLE_LIST => TYPE_NAME_IMMUTABLE_LIST,
            TYPE_IMMUTABLE_SET => TYPE_NAME_IMMUTABLE_SET,
            TYPE_IMMUTABLE_MAP => TYPE_NAME_IMMUTABLE_DICTIONARY,
            _ => null
        };
    }

    private static string GetNamespace(DSNamedType originDefine) {
        // 先查看type是否指定了命名空间 -- 可能是外部程序集的引用配置文件
        Annotation? annotation = originDefine.GetAnnotation(DSAnnotations.NAMESPACE);
        if (annotation != null && annotation.AsObject().TryGetValue(DSAnnotations.KEY_CS, out DsonValue value)) {
            return value.AsString();
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

    #region Dson-Codec

    public static readonly ClassName TYPE_NAME_SERIALIZABLE = ClassName.Get(typeof(DsonSerializableAttribute));
    public static readonly ClassName TYPE_NAME_OBJECT_STYLE = ClassName.Get(typeof(ObjectStyle));
    public static readonly ClassName TYPE_NAME_STRING_BUILDER = ClassName.Get(typeof(StringBuilder));

    public static readonly ClassName TYPE_NAME_IEQUATABLE = ClassName.Get(typeof(IEquatable<>));
    public static readonly ClassName TYPE_NAME_COLLECTION_UTIL = ClassName.Get(typeof(CollectionUtil));

    public static readonly ClassName TYPE_NAME_WRITER = ClassName.Get(typeof(IDsonObjectWriter));
    public static readonly ClassName TYPE_NAME_READER = ClassName.Get(typeof(IDsonObjectReader));
    public static readonly ClassName TYPE_NAME_CONVERTER_OPTIONS = ClassName.Get(typeof(ConverterOptions));
    public static readonly ClassName TYPE_NAME_CONTEXT_TYPE = ClassName.Get(typeof(DsonContextType));
    // ssti
    private static readonly ClassName TYPE_NAME_SST_MGR = ToolUtil.ClassNameOfCanonicalName("Wjybxx.BigCat.Fx.SstMgr");
    private static readonly ClassName TYPE_NAME_IMMUTABLE_LIST_STRING = ClassName.Get(typeof(ImmutableList<string>));

    /** 注解：Flags枚举 */
    public static readonly AttributeSpec ATTRIBUTE_FLAGS = AttributeSpec.NewBuilder(typeof(FlagsAttribute)).Build();
    /** 注解：可序列化 */
    public static readonly AttributeSpec ATTRIBUTE_SERIALIZABLE = AttributeSpec.NewBuilder(TYPE_NAME_SERIALIZABLE).Build();
    /** 注解：字段不需要序列化 */
    public static readonly AttributeSpec ATTRIBUTE_NON_SERIALIZED = AttributeSpec.NewBuilder(ClassName.NON_SERIALIZED).Build();
    /** 用于在文件中插入换行符 */
    private static readonly CodeBlockSpec CODE_NEW_LINE = new CodeBlockSpec(CodeBlock.Of("\n"));

    public static AttributeSpec BuildCodecAttribute(DSNamedType namedType, StringBuilder sb) {
        var attributeBuilder = AttributeSpec.NewBuilder(TYPE_NAME_SERIALIZABLE)
            .AddMember("SkipFields", "new[] { $S }", "*") // 跳过所有字段，由生成的代码编解码
            .AddMember("Style", "$T.$L", TYPE_NAME_OBJECT_STYLE, namedType.DsonStyle.ToString());
        if (namedType.DsonAliases.Count > 0) {
            sb.Append("new[] { ");
            for (int index = 0; index < namedType.DsonAliases.Count; index++) {
                string alias = namedType.DsonAliases[index];
                if (index > 0) sb.Append(", ");
                sb.Append('"');
                sb.Append(alias);
                sb.Append('"');
            }
            sb.Append(" }");
            attributeBuilder.AddMember("Names", sb.ToString());
        }
        return attributeBuilder.Build();
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////
    // 这些方法名有特殊类逻辑 -- 外部需要测试
    private const string METHOD_NEW_INSTANCE = "NewInstance";
    private const string METHOD_AFTER_DECODE = "AfterDecode";
    private const string METHOD_BEFORE_ENCODE = "BeforeEncode";
    private const string METHOD_NAME_WRITE_OBJECT = "WriteObject";
    private const string METHOD_NAME_READ_OBJECT = "ReadObject";

    /** 获取read字段的方法名 */
    private static string GetReadMethodName(TypeName typeName) {
        if (typeName == TypeName.INT) return "ReadInt";
        if (typeName == TypeName.LONG) return "ReadLong";
        if (typeName == TypeName.FLOAT) return "ReadFloat";
        if (typeName == TypeName.DOUBLE) return "ReadDouble";
        if (typeName == TypeName.BOOL) return "ReadBool";
        if (typeName == TypeName.STRING) return "ReadString";

        if (typeName == TypeName.UINT) return "ReadUInt";
        if (typeName == TypeName.ULONG) return "ReadULong";
        if (typeName == TypeName.BYTE) return "ReadByte";
        if (typeName == TypeName.SBYTE) return "ReadSByte";
        if (typeName == TypeName.SHORT) return "ReadShort";
        if (typeName == TypeName.USHORT) return "ReadUShort";
        if (typeName == TypeName.CHAR) return "ReadChar";

        if (typeName == TYPE_NAME_BYTE_ARRAY) return "ReadBytes";
        if (typeName == TYPE_NAME_BINARY) return "ReadBinary";
        if (typeName == TYPE_NAME_PTR) return "ReadPtr";
        if (typeName == TYPE_NAME_LPTR) return "ReadLitePtr";
        if (typeName == TYPE_NAME_DATETIME) return "ReadDateTime";
        if (typeName == TYPE_NAME_TIMESTAMP) return "ReadTimestamp";
        return "ReadObject";
    }

    /** 获取write字段的方法名 */
    private static string GetWriteMethodName(TypeName typeName) {
        if (typeName == TypeName.INT) return "WriteInt";
        if (typeName == TypeName.LONG) return "WriteLong";
        if (typeName == TypeName.FLOAT) return "WriteFloat";
        if (typeName == TypeName.DOUBLE) return "WriteDouble";
        if (typeName == TypeName.BOOL) return "WriteBool";
        if (typeName == TypeName.STRING) return "WriteString";

        if (typeName == TypeName.UINT) return "WriteUInt";
        if (typeName == TypeName.ULONG) return "WriteULong";
        if (typeName == TypeName.BYTE) return "WriteByte";
        if (typeName == TypeName.SBYTE) return "WriteSByte";
        if (typeName == TypeName.SHORT) return "WriteShort";
        if (typeName == TypeName.USHORT) return "WriteUShort";
        if (typeName == TypeName.CHAR) return "WriteChar";

        if (typeName == TYPE_NAME_BYTE_ARRAY) return "WriteBytes";
        if (typeName == TYPE_NAME_BINARY) return "WriteBinary";
        if (typeName == TYPE_NAME_PTR) return "WritePtr";
        if (typeName == TYPE_NAME_LPTR) return "WriteLitePtr";
        if (typeName == TYPE_NAME_DATETIME) return "WriteDateTime";
        if (typeName == TYPE_NAME_TIMESTAMP) return "WriteTimestamp";
        return "WriteObject";
    }

    #endregion

    #region RPC

    public static readonly ClassName TYPE_NAME_RPC_SERVICE = ToolUtil.ClassNameOfCanonicalName("Wjybxx.BigCat.Fx.RpcServiceAttribute");
    public static readonly ClassName TYPE_NAME_RPC_METHOD = ToolUtil.ClassNameOfCanonicalName("Wjybxx.BigCat.Fx.RpcMethodAttribute");
    //
    public static readonly ClassName TYPE_NAME_RPC_CONTEXT_T = ClassName.Get("Wjybxx.BigCat.Fx", "RpcContext",
        new List<TypeName> { TypeParameterName.Get("T") });
    //
    public static readonly ClassName TYPE_NAME_VALUE_FUTURE = ClassName.Get(typeof(ValueFuture));
    public static readonly ClassName TYPE_NAME_VALUE_FUTURE_T = ClassName.Get(typeof(ValueFuture<>));

    public const string PNAME_SERVICE_ID = "ServiceId";
    public const string PNAME_METHOD_ID = "MethodId";
    public const string PNAME_MANUAL_RETURN = "ManualReturn";
    public const string PNAME_ARG_SHARABLE = "ArgSharable";
    public const string PNAME_RESULT_SHARABLE = "ResultSharable";
    public const string PNAME_CUSTOM_DATA = "CustomData";

    // 服务上的async等用于配置默认值
    // @Rpc {id: 1, async: true, ctx: true, manual: true}
    public static int GetServiceId(DsonObject<string> methodData) {
        // 默认是double类型
        return methodData["id"].AsDsonNumber().IntValue;
    }

    // @Rpc {id: 1, async: true, ctx: true, manual: true}
    public static int GetMethodId(int? number, DsonObject<string> methodData) {
        // 默认是double类型
        return number ?? methodData["id"].AsDsonNumber().IntValue;
    }

    public static bool IsAsyncMethod(DsonObject<string> methodData, DsonObject<string> serviceData) {
        if (methodData.TryGetValue("async", out DsonValue value)
            || serviceData.TryGetValue("async", out value)) {
            return GetBool(value);
        }
        return false;
    }

    public static bool IsManualReturn(DsonObject<string> methodData, DsonObject<string> serviceData) {
        if (methodData.TryGetValue("manual", out DsonValue value)
            || serviceData.TryGetValue("manual", out value)) {
            return GetBool(value);
        }
        return false;
    }

    public static bool IsRequireContext(DsonObject<string> methodData, DsonObject<string> serviceData) {
        // 手动返回结果时也需要ctx -- 且方法注解的优先级高于服务的默认配置
        if (methodData.TryGetValue("ctx", out DsonValue value)
            || methodData.TryGetValue("manual", out value)) {
            return GetBool(value);
        }
        if (serviceData.TryGetValue("ctx", out value)
            || serviceData.TryGetValue("manual", out value)) {
            return GetBool(value);
        }
        return false;
    }

    private static bool GetBool(DsonValue value) {
        if (value.DsonType == DsonType.Bool) return value.AsBool();
        if (value.IsNumber) return value.AsDsonNumber().IntValue == 1;
        return false;
    }

    #endregion
}
}