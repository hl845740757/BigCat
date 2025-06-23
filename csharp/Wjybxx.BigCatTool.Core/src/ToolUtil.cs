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
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Poet;
using Wjybxx.Commons.Pool;
using Wjybxx.Dson.Types;

namespace Wjybxx.BigCatTool
{
/// <summary>
/// 
/// </summary>
public class ToolUtil
{
    public static readonly Encoding ENCODING_UTF8 = new UTF8Encoding(false);

    #region 字符串

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
            string str1;
            while ((str1 = stringReader.ReadLine()) != null)
                stringList.Add(str1);
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

    #region 文件

    /// <summary>
    /// 从工作目录向上查找指定目录
    /// </summary>
    /// <param name="dirName"></param>
    /// <returns></returns>
    public static string GetDirectory(string dirName) {
        DirectoryInfo directoryInfo = new DirectoryInfo(Environment.CurrentDirectory);
        while (true) {
            if (directoryInfo.Name == dirName) {
                return directoryInfo.FullName;
            }
            directoryInfo = directoryInfo.Parent;
            if (directoryInfo == null) {
                throw new IOException($"dic {dirName} not found");
            }
        }
    }

    /// <summary>
    /// 拷贝文件夹
    /// </summary>
    /// <param name="sourceDir">原目录</param>
    /// <param name="destinationDir">目标目录</param>
    /// <param name="overwrite">是否覆盖已存在的文件</param>
    /// <param name="recursive">是否递归</param>
    /// <exception cref="DirectoryNotFoundException">如果原文件夹不存在</exception>
    public static void CopyDirectory(string sourceDir, string destinationDir, bool overwrite, bool recursive = true) {
        DirectoryInfo? srcDirInfo = new DirectoryInfo(sourceDir);
        if (!srcDirInfo.Exists) {
            throw new DirectoryNotFoundException($"Source directory not found: {srcDirInfo.FullName}");
        }
        DirectoryInfo destDirInfo = new DirectoryInfo(destinationDir);
        if (!destDirInfo.Exists) {
            destDirInfo.Create();
        }
        // 先拷贝直接文件
        foreach (FileInfo file in srcDirInfo.GetFiles()) {
            string targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath, overwrite);
        }
        // 如果递归，则拷贝子目录
        if (recursive) {
            foreach (DirectoryInfo subDir in srcDirInfo.GetDirectories()) {
                string destinationSubDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, destinationSubDir, overwrite);
            }
        }
    }

    /// <summary>
    /// 清理文件夹（保留空文件夹）
    /// </summary>
    /// <param name="dirName">要清理的文件夹</param>
    /// <param name="retainSubDir">是否保留子文件夹</param>
    public static void CleanDirectory(string dirName, bool retainSubDir = false) {
        DirectoryInfo directoryInfo = new DirectoryInfo(dirName);
        if (directoryInfo.Exists) {
            foreach (FileInfo file in directoryInfo.GetFiles()) {
                file.Delete();
            }
            foreach (DirectoryInfo subDir in directoryInfo.GetDirectories()) {
                if (retainSubDir) {
                    CleanDirectory(subDir.FullName, true);
                } else {
                    subDir.Delete(true);
                }
            }
        }
    }

    /// <summary>
    /// 删除文件夹
    /// </summary>
    /// <param name="dirName"></param>
    public static void DelectDirectory(string dirName) {
        DirectoryInfo directoryInfo = new DirectoryInfo(dirName);
        if (directoryInfo.Exists) {
            directoryInfo.Delete(true);
        }
    }

    #endregion

    #region 代码生成

    private static readonly ClassName TYPE_NAME_GENERATED = ClassName.Get("Wjybxx.Commons.Attributes", "GeneratedAttribute");
    private static readonly ClassName TYPE_NAME_SOURCE_FILE_REF = ClassName.Get("Wjybxx.Commons.Attributes", "SourceFileRefAttribute");

    public static readonly ClassName TYPE_NAME_FLAGS = ClassName.Get(typeof(FlagsAttribute));
    /// <summary>
    /// CodeWriter池
    /// </summary>
    public static readonly ConcurrentObjectPool<CodeWriter> codeWriterPool = new ConcurrentObjectPool<CodeWriter>(
        () => new CodeWriter("    ", 150), e => e.Reset(), 8);

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

    #endregion

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

    // 这两个方法名有特殊类逻辑 -- 外部需要测试
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

    #endregion
}
}