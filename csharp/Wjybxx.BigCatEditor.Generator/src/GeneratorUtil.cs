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
using System.Reflection;
using System.Runtime.CompilerServices;
using Wjybxx.Commons.Poet;

namespace Wjybxx.BigCatEditor.Generator
{
/// <summary>
/// 生成器工具类--非编译时
/// </summary>
public static class GeneratorUtil
{
    private static readonly ClassName clsName_GeneratedAttribute = ClassName.Get("Wjybxx.Commons.Attributes", "GeneratedAttribute");
    private static readonly ClassName clsName_SourceFileRef = ClassName.Get("Wjybxx.Commons.Attributes", "SourceFileRefAttribute");

    /// <summary>
    /// 为生成代码的注解处理器创建一个通用注解
    /// </summary>
    /// <param name="type">生成器的类型信息</param>
    /// <param name="version">生成器的版本</param>
    /// <param name="dateTime">执行时间</param>
    /// <returns></returns>
    public static AttributeSpec NewProcessorInfoAnnotation(Type type, string? version = null, DateTime? dateTime = null) {
        var builder = AttributeSpec.NewBuilder(clsName_GeneratedAttribute)
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
        return AttributeSpec.NewBuilder(clsName_SourceFileRef)
            .Constructor(CodeBlock.Of("typeof($T)", sourceFileTypeName))
            .Build();
    }

    /** @param cname 类的标准名，import语句格式 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ClassName ClassNameOfCanonicalName(string cname) {
        int index = cname.LastIndexOf('.');
        return ClassName.Get(cname.Substring2(0, index), cname.Substring2(index + 1));
    }

    /**
     * 将继承体系展开，不包含实现的接口。
     * （超类在后，包含object）
     */
    public static List<Type> FlatInherit(Type type) {
        List<Type> result = new List<Type>(4);
        result.Add(type);
        while ((type = type.BaseType) != null) {
            result.Add(type);
        }
        return result;
    }

    /**
     * 将继承体系展开，并逆序返回，不包含实现的接口。
     * （超类在前，包含object）
     */
    public static List<Type> FlatInheritAndReverse(Type type) {
        List<Type> result = FlatInherit(type);
        result.Reverse();
        return result;
    }

    /// <summary>
    /// 获取类的所有字段和方法，包含继承得到的字段和方法和属性。
    /// (查询的开销较大，用户应当缓存结果)
    /// </summary>
    /// <param name="type"></param>
    /// <param name="memberTypes"></param>
    /// <returns></returns>
    public static List<MemberInfo> GetAllMembersWithInherit(Type type, MemberTypes memberTypes = MemberTypes.Field
                                                                                                 | MemberTypes.Property
                                                                                                 | MemberTypes.Method) {
        // FlattenHierarchy 不能拉取到超类的private字段
        return FlatInheritAndReverse(type)
            .SelectMany(e => e.GetMembers(BindingFlags.DeclaredOnly
                                          | BindingFlags.Public | BindingFlags.NonPublic
                                          | BindingFlags.Static | BindingFlags.Instance))
            .Where(e => (e.MemberType & memberTypes) != 0)
            .ToList();
    }
}
}