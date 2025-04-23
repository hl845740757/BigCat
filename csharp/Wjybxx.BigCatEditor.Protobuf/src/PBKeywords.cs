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

using System.Collections.Generic;
using System.Collections.Immutable;
using Wjybxx.Commons.Collections;

namespace Wjybxx.BigCatEditor.Protobuf
{
/// <summary>
/// PB的关键字
/// </summary>
public static class PBKeywords
{
    /** 可选项 */
    public const string OPTION = "option";
    /** 导入文件 */
    public const string IMPORT = "import";
    /** 导入传递（依赖传递） */
    public const string PUBLIC = "public";

    /** 可选字段 */
    public const string OPTIONAL = "optional";
    /** 必要字段 */
    public const string REQUIRED = "required";
    /** pb3中<see cref="OPTIONAL"/>的替代物 */
    public const string SINGULAR = "singular";

    /** 数组字段 */
    public const string REPEATED = "repeated";
    /** 是否仅1个字段有效 */
    public const string ONE_OF = "oneof";


    /** rpc服务 */
    public const string SERVICE = "service";
    /** 结构体 */
    public const string MESSAGE = "message";
    /** 枚举 */
    public const string ENUM = "enum";
    /** rpc方法 */
    public const string RPC = "rpc";
    /** rpc方法返回值声明 */
    public const string RETURNS = "returns";

    #region file

    /** 语法 -- 字符串值 */
    public const string SYNTAX = "syntax";
    /** 生成代码优化项 */
    public const string OPTIMIZE_FOR = "optimize_for";

    /** 生成代码的包名 */
    public const string PACKAGE = "package";
    /**
     * 生成的java文件的包名
     * 如果未配置，由解析器赋予默认值 -- 通常是固定值
     */
    public const string JAVA_PACKAGE = "java_package";
    /**
     * 生成的java文件的外部类类名
     * 注意：
     * 1.如果未配置，由解析器赋予默认值 -- 通常建议根据文件名生成，eg：{@code bag.proto => MsgBag}
     * 2.Rpc服务生成类为顶层类，不使用该属性
     */
    public const string JAVA_OUTER_CLASSNAME = "java_outer_classname";
    /**
     * 是否将顶级消息、枚举、和服务定义在包级，而不是在以 .proto 文件命名的外部类中
     */
    public const string JAVA_MULTIPLE_FILES = "java_multiple_files";
    /**
     * 导出java时是否是导出rpc服务
     * 我们不使用protobuf的GRPC，因此不会导出 -- 我们在预处理文件时会关闭service的代码生成
     */
    public const string JAVA_GENERIC_SERVICES = "java_generic_services";

    /**
     * csharp命名空间
     */
    public const string CSHARP_NAMESPACE = "csharp_namespace";

    #endregion

    #region type

    /** 是否允许不同的枚举常量指向同一个值 */
    public const string ALLOW_ALIAS = "allow_alias";
    /** 保留字段编号 */
    public const string RESERVED = "reserved";

    #endregion

    private static readonly ISet<string> fieldModifiers = new[]
    {
        OPTIONAL, REQUIRED, SINGULAR, REPEATED, ONE_OF
    }.ToImmutableLinkedHashSet();

    private static readonly IDictionary<string, bool> stringValueOptions = ImmutableDictionary<string, bool>.Empty;

    /** 是否是字段修饰符 */
    public static bool IsFieldModifier(string word) {
        return fieldModifiers.Contains(word);
    }

    /** 是否是字符串可选项值 -- 输出时需要加引号 */
    public static bool IsStringValueOption(string name) {
        // 特殊值
        if (stringValueOptions.TryGetValue(name, out bool val)) {
            return val;
        }
        // 规则值
        return name.EndsWith("package")
               || name.EndsWith("namespace")
               || name.EndsWith("name") // 包含classname
               || name.EndsWith("prefix")
               || name.EndsWith("comments");
    }
}
}