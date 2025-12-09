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
/// 代码生成器工具类
/// </summary>
public static class GeneratorUtil
{
    private static readonly Encoding ENCODING_UTF8 = new UTF8Encoding(false);
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
}
}