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
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Wjybxx.BigCatEditor.Core;
using Wjybxx.BigCatEditor.DataScript;
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Poet;
using Wjybxx.Commons.Pool;
using Wjybxx.Dson;
using Wjybxx.Dson.Text;
using Wjybxx.Dson.Types;
using TypeName = Wjybxx.Commons.Poet.TypeName;

namespace Wjybxx.BigCatEditor.Generator
{
/// <summary>
/// 生成器工具类--非编译时
/// </summary>
public static class GeneratorUtil
{
    private static readonly ClassName TYPE_NAME_GENERATED = ClassName.Get("Wjybxx.Commons.Attributes", "GeneratedAttribute");
    private static readonly ClassName TYPE_NAME_SOURCE_FILE_REF = ClassName.Get("Wjybxx.Commons.Attributes", "SourceFileRefAttribute");
    public static readonly ClassName TYPE_NAME_FLAGS = ClassName.Get(typeof(FlagsAttribute));

    private static readonly Encoding ENCODING_UTF8 = new UTF8Encoding(false);
    public static readonly ConcurrentObjectPool<CodeWriter> codeWriterPool = new ConcurrentObjectPool<CodeWriter>(
        () => new CodeWriter("    ", 150), e => e.Reset());

    /// <summary>
    /// 为生成代码的注解处理器创建一个通用注解
    /// </summary>
    /// <param name="type">生成器的类型信息</param>
    /// <param name="version">生成器的版本</param>
    /// <param name="dateTime">执行时间</param>
    /// <returns></returns>
    public static AttributeSpec NewProcessorInfoAnnotation(Type type, string? version = null, DateTime? dateTime = null) {
        var builder = AttributeSpec.NewBuilder(TYPE_NAME_GENERATED)
            .Constructor(CodeBlock.Of("$S", type.ToString()));
        if (version != null) {
            builder.AddMember("Version", "$S", version);
        }
        if (dateTime != null) {
            builder.AddMember("DateTime", "$S", dateTime.Value.ToString("s"));
        }
        return builder.Build();
    }

    /// <summary>
    /// 添加指向源代码文件的引用，方便查看文件依赖
    /// </summary>
    /// <param name="sourceFileTypeName"></param>
    /// <returns></returns>
    public static AttributeSpec NewSourceFileRefAnnotation(TypeName sourceFileTypeName) {
        return AttributeSpec.NewBuilder(TYPE_NAME_SOURCE_FILE_REF)
            .Constructor(CodeBlock.Of("typeof($T)", sourceFileTypeName))
            .Build();
    }

    /** @param cname 类的标准名，import语句格式 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ClassName ClassNameOfCanonicalName(string cname) {
        int index = cname.LastIndexOf('.');
        return ClassName.Get(cname.Substring2(0, index), cname.Substring2(index + 1));
    }

    /// <summary>
    /// 将继承体系展开，不包含实现的接口。
    /// （超类在后，包含object）
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static List<Type> FlatInherit(Type type) {
        List<Type> result = new List<Type>(4);
        result.Add(type);
        while ((type = type.BaseType) != null) {
            result.Add(type);
        }
        return result;
    }

    /// <summary>
    /// 将继承体系展开，并逆序返回，不包含实现的接口。
    /// （超类在前，包含object）
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static List<Type> FlatInheritAndReverse(Type type) {
        List<Type> result = FlatInherit(type);
        result.Reverse();
        return result;
    }

    /// <summary>
    /// 将类型写入文件
    /// 适用简单情况，我们多数情况下一个类型一个文件
    /// </summary>
    public static void WriteToFile(string outDir, string ns, TypeSpec typeSpec) {
        CsharpFile csharpFile = CsharpFile.NewBuilder(typeSpec.name)
            .AddSpec(NamespaceSpec.Of(ns, typeSpec))
            .Build();
        CodeWriter codeWriter = codeWriterPool.Acquire();
        codeWriter.IndentInsideNamespace = false;
        try {
            string path = outDir + "/" + typeSpec.name + ".cs";
            File.WriteAllText(path, codeWriter.Write(csharpFile), ENCODING_UTF8);
        }
        finally {
            codeWriterPool.Release(codeWriter);
        }
    }

    /// <summary>
    /// 适用复杂情况
    /// </summary>
    /// <param name="outDir"></param>
    /// <param name="csharpFile"></param>
    public static void WriteToFile(string outDir, CsharpFile csharpFile) {
        CodeWriter codeWriter = codeWriterPool.Acquire();
        codeWriter.IndentInsideNamespace = false;
        try {
            string path = outDir + "/" + csharpFile.name + ".cs";
            File.WriteAllText(path, codeWriter.Write(csharpFile), ENCODING_UTF8);
        }
        finally {
            codeWriterPool.Release(codeWriter);
        }
    }

    #region Dson-Cdoec

    public static readonly ClassName TYPE_NAME_DATETIME = ClassName.DATETIME;
    public static readonly ArrayTypeName TYPE_NAME_BYTES = ArrayTypeName.BYTE_ARRAY;
    public static readonly ClassName TYPE_NAME_PAIR = ClassName.Get(typeof(KeyValuePair<,>));

    public static readonly ClassName TYPE_NAME_BINARY = ClassName.Get(typeof(Binary));
    public static readonly ClassName TYPE_NAME_PTR = ClassName.Get(typeof(ObjectPtr));
    public static readonly ClassName TYPE_NAME_LPTR = ClassName.Get(typeof(ObjectLitePtr));
    public static readonly ClassName TYPE_NAME_TIMESTAMP = ClassName.Get(typeof(Timestamp));
    // 集合接口
    public static readonly ClassName TYPE_NAME_I_COLLECTION = ClassName.Get(typeof(ICollection<>));
    public static readonly ClassName TYPE_NAME_I_LIST = ClassName.Get(typeof(IList<>));
    public static readonly ClassName TYPE_NAME_I_SET = ClassName.Get(typeof(ISet<>));
    public static readonly ClassName TYPE_NAME_I_DICTIONARY = ClassName.Get(typeof(IDictionary<,>));
    // 常用集合
    public static readonly ClassName TYPE_NAME_LIST = ClassName.Get(typeof(List<>));
    public static readonly ClassName TYPE_NAME_HASHSET = ClassName.Get(typeof(HashSet<>));
    public static readonly ClassName TYPE_NAME_DICTIONARY = ClassName.Get(typeof(Dictionary<,>));

    public static readonly ClassName TYPE_NAME_LINKED_DICTIONARY = ClassName.Get(typeof(LinkedDictionary<,>));
    public static readonly ClassName TYPE_NAME_LINKED_HASHSET = ClassName.Get(typeof(LinkedHashSet<>));
    // 不可变集合
    public static readonly ClassName TYPE_NAME_IMMUTABLE_LIST = ClassName.Get(typeof(ImmutableList<>));
    public static readonly ClassName TYPE_NAME_IMMUTABLE_SET = ClassName.Get(typeof(ImmutableSet<>));
    public static readonly ClassName TYPE_NAME_IMMUTABLE_DICTIONARY = ClassName.Get(typeof(ImmutableDictionary<,>));

    private static readonly ImmutableDictionary<string, ObjectStyle>
        name2ObjectStyleDic = EnumUtil.GetValues<ObjectStyle>()
            .ToDictionary(e => e.ToString().ToLower(), e => e)
            .ToImmutableDictionary2();

    private static readonly ImmutableDictionary<string, NumberStyle>
        name2NumberStyleDic = EnumUtil.GetValues<NumberStyle>()
            .ToDictionary(e => e.ToString().ToLower(), e => e)
            .ToImmutableDictionary2();

    private static readonly ImmutableDictionary<string, StringStyle>
        name2StringStyleDic = EnumUtil.GetValues<StringStyle>()
            .ToDictionary(e => e.ToString().ToLower(), e => e)
            .ToImmutableDictionary2();

    // 这两个方法名有特殊类逻辑
    public const string METHOD_NAME_READ_OBJECT = "ReadObject";
    public const string METHOD_NAME_WRITE_OBJECT = "WriteObject";

    /** 获取read字段的方法名 */
    public static string GetReadMethodName(TypeName typeName) {
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

        if (typeName == TYPE_NAME_BYTES) return "ReadBytes";
        if (typeName == TYPE_NAME_PTR) return "ReadPtr";
        if (typeName == TYPE_NAME_LPTR) return "ReadLitePtr";
        if (typeName == TYPE_NAME_DATETIME) return "ReadDateTime";
        if (typeName == TYPE_NAME_TIMESTAMP) return "ReadTimestamp";
        return "ReadObject";
    }

    /** 获取write字段的方法名 */
    public static string GetWriteMethodName(TypeName typeName) {
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

        if (typeName == TYPE_NAME_BYTES) return "WriteBytes";
        if (typeName == TYPE_NAME_PTR) return "WritePtr";
        if (typeName == TYPE_NAME_LPTR) return "WriteLitePtr";
        if (typeName == TYPE_NAME_DATETIME) return "WriteDateTime";
        if (typeName == TYPE_NAME_TIMESTAMP) return "WriteTimestamp";
        return "WriteObject";
    }

    /// <summary>
    /// 获取类型用于Dson编码时的别名
    /// </summary>
    /// <param name="namedType"></param>
    /// <returns></returns>
    public static List<string> GetCodecAliases(DSNamedType namedType) {
        Annotation? annotation = namedType.GetAnnotation(DSAnnotations.CODEC);
        if (annotation == null) return new List<string>();

        DsonObject<string> dsonObject = annotation.DsonValue.AsObject();
        if (dsonObject.Count == 0 || !dsonObject.TryGetValue(DSAnnotations.KEY_ALIAS, out DsonValue value)) {
            return new List<string>();
        }
        DsonArray<string> dsonArray = value.AsArray();
        if (dsonArray.Count == 0) return new List<string>();
        //
        List<string> result = new List<string>(dsonObject.Count);
        foreach (DsonValue dsonValue in dsonArray) {
            result.Add(dsonValue.AsString().Trim());
        }
        return result;
    }

    public static ObjectStyle GetCodecStyle(DSNamedType namedType, ObjectStyle defaultStyle = ObjectStyle.Indent) {
        Annotation? annotation = namedType.GetAnnotation(DSAnnotations.CODEC);
        if (annotation == null) {
            return defaultStyle;
        }
        annotation.DsonValue.AsObject().TryGetValue(DSAnnotations.KEY_STYLE, out DsonValue value);
        if (value == null) {
            return defaultStyle;
        }
        if (value.IsNumber) {
            return (ObjectStyle)value.AsDsonNumber().IntValue;
        }
        string style = value.AsString().ToLower();
        return name2ObjectStyleDic.TryGetValue(style, out ObjectStyle result) ? result : defaultStyle;
    }

    #endregion
}
}