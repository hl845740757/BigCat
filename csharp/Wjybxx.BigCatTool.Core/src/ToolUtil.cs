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
using System.Runtime.CompilerServices;
using System.Text;
using Wjybxx.Commons;
using Wjybxx.Commons.Attributes;
using Wjybxx.Commons.Poet;
using Wjybxx.Commons.Pool;

namespace Wjybxx.BigCatTool
{
/// <summary>
///
/// </summary>
public class ToolUtil
{
    #region 字符串

    public static string FirstCharToUpperCase(string str) {
        return ObjectUtil.FirstCharToUpperCase(str);
    }

    public static string FirstCharToLowerCase(string str) {
        return ObjectUtil.FirstCharToLowerCase(str);
    }

    /// <summary>
    /// 索引首个空白字符
    /// </summary>
    public static int IndexOfWhitespace(string cs, int startIndex = 0) {
        return ObjectUtil.IndexOfWhitespace(cs, startIndex);
    }

    /// <summary>
    /// 反向索引首个空白字符
    /// </summary>
    public static int LastIndexOfWhitespace(string cs, int startIndex = -1) {
        return ObjectUtil.LastIndexOfWhitespace(cs, startIndex);
    }

    /// <summary>
    /// 删除空白字符
    /// </summary>
    /// <param name="cs"></param>
    /// <returns></returns>
    public static string DeleteWhitespace(string cs) {
        return ObjectUtil.DeleteWhitespace(cs);
    }

    /// <summary>
    /// 索引首个非空白字符
    /// </summary>
    public static int IndexOfNonWhitespace(string cs, int startIndex = 0) {
        if (startIndex < 0) {
            throw new ArgumentException("startIndex " + startIndex);
        }
        int length = ObjectUtil.Length(cs);
        if (length == 0) {
            return -1;
        }
        for (int i = startIndex; i < length; i++) {
            if (!char.IsWhiteSpace(cs[i])) {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// 反向索引首个非空白字符
    /// </summary>
    public static int LastIndexOfNonWhitespace(string cs, int startIndex = -1) {
        if (startIndex < -1) {
            throw new ArgumentException("startIndex " + startIndex);
        }
        int length = ObjectUtil.Length(cs);
        if (length == 0) {
            return -1;
        }
        if (startIndex == -1 || startIndex >= length) {
            startIndex = length - 1;
        }
        for (int i = startIndex; i >= 0; i--) {
            if (!char.IsWhiteSpace(cs[i])) {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// 将字符串拆分为行
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static List<string> GetLines(string str) {
        List<string> stringList = new List<string>();
        using (StringReader stringReader = new StringReader(str)) {
            string line;
            while ((line = stringReader.ReadLine()) != null)
                stringList.Add(line);
        }
        return stringList;
    }

    /// <summary>
    /// 去除字符串的双引号
    /// </summary>
    /// <param name="str">要处理的字符串</param>
    /// <param name="trim">是否去掉两端空白</param>
    /// <returns></returns>
    public static string Unquote(string str, bool trim = false) {
        int length = ObjectUtil.Length(str);
        if (length < 2) {
            return str;
        }
        char firstChar = str[0];
        char lastChar = str[str.Length - 1];
        if (firstChar == '"' && lastChar == '"') {
            if (trim) {
                int start = IndexOfNonWhitespace(str, 0);
                int end = LastIndexOfNonWhitespace(str);
                if (start < 0) {
                    return "";
                }
                return str.Substring2(start, end);
            }
            return str.Substring2(1, str.Length - 1);
        }
        return str;
    }

    /// <summary>
    /// 蛇形字符串转大驼峰
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string ToUpperCamel(string str) {
        if (str.IndexOf('_') < 0) {
            return ObjectUtil.FirstCharToUpperCase(str);
        }
        StringBuilder sb = new StringBuilder(str.Length);
        bool nextUpperCase = true;
        foreach (char c in str) {
            if (c == '_' || c == ' ') {
                nextUpperCase = true;
                continue;
            }
            sb.Append(nextUpperCase ? char.ToUpper(c) : c);
            nextUpperCase = false;
        }
        return sb.ToString();
    }

    /// <summary>
    /// 删除特定字符
    /// </summary>
    /// <param name="str"></param>
    /// <param name="c"></param>
    /// <returns></returns>
    public static string DeleteChar(string str, char c) {
        if (str.IndexOf(c) < 0) {
            return str;
        }
        int len = str.Length;
        StringBuilder sb = new StringBuilder(len);
        for (int idx = 0; idx < len; idx++) {
            char c2 = str[idx];
            if (c2 == c) {
                continue;
            }
            sb.Append(c2);
        }
        return sb.ToString();
    }

    #endregion

    #region 代码生成

    public static readonly Encoding ENCODING_UTF8 = new UTF8Encoding(false);
    private static readonly ClassName TYPE_NAME_GENERATED = ClassName.Get(typeof(GeneratedAttribute));
    private static readonly ClassName TYPE_NAME_SOURCE_FILE_REF = ClassName.Get(typeof(SourceFileRefAttribute));
    //
    private static readonly ConcurrentObjectPool<CodeWriter> codeWriterPool = new ConcurrentObjectPool<CodeWriter>(
        () => new CodeWriter("    ", 150), writer => writer.Reset(), 8);

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
    private static AttributeSpec NewSourceFileRefAnnotation(TypeName sourceFileTypeName) {
        return AttributeSpec.NewBuilder(TYPE_NAME_SOURCE_FILE_REF)
            .Constructor(CodeBlock.Of("typeof($T)", sourceFileTypeName))
            .Build();
    }

    /// <summary>
    /// 通过类的标准名(import名)获取ClassName 
    /// </summary>
    /// <param name="cname"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ClassName ClassNameOfCanonicalName(string cname) {
        int index = cname.LastIndexOf('.');
        return ClassName.Get(cname.Substring2(0, index), cname.Substring2(index + 1));
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

    #endregion
}
}