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
using Wjybxx.Commons.Poet;
using Wjybxx.Dson.Types;

namespace Wjybxx.BigCatEditor.DataScript
{
/// <summary>
/// 数据脚本语法下的关键字
/// </summary>
public static class DSKeywords
{
    /** 可选项 */
    public const string OPTION = "option";
    /** 导入文件 */
    public const string IMPORT = "import";
    /** 导入传递（不传递依赖） */
    public const string PRIVATE = "private";
    /** 导入传递（依赖传递） */
    public const string PUBLIC = "public";

    /** 泛型约束使用的关键字 */
    public const string WHERE = "where";
    /** 泛型约束：包含默认构造函数 */
    public const string NEW = "new";

    /** class */
    public const string CLASS = "class";
    /** 结构体 */
    public const string STRUCT = "struct";
    /** 枚举 */
    public const string ENUM = "enum";
    /** 实例 -- 暂时要求严格的输入格式，以降低解析难度， */
    public const string INST = "inst";

    #region options-file

    /** 默认的包名 */
    public const string PACKAGE = "package";
    /** 生成的java文件的包名 */
    public const string JAVA_PACKAGE = "java_package";
    /** csharp命名空间 */
    public const string CSHARP_NAMESPACE = "csharp_namespace";

    #endregion

    #region options-type

    /** 是否允许不同的枚举常量指向同一个值 */
    public const string ALLOW_ALIAS = "allow_alias";
    /** 保留字段编号 */
    public const string RESERVED = "reserved";

    #endregion

    #region builtin-types

    // 原子类型
    public const string TYPE_INT32 = "int32";
    public const string TYPE_INT64 = "int64";
    public const string TYPE_FLOAT = "float";
    public const string TYPE_DOUBLE = "double";
    public const string TYPE_BOOL = "bool";
    public const string TYPE_STRING = "string";
    public const string TYPE_BYTES = "bytes";
    // 容器类型
    public const string TYPE_LIST = "List";
    public const string TYPE_MAP = "Map";
    // 装箱类型
    public const string TYPE_OBJECT = "Object";
    public const string TYPE_NULLABLE = "Nullable";

    // 原子类型
    public static readonly ClassName TYPE_NAME_INT32 = ClassName.Get("ds", "int32");
    public static readonly ClassName TYPE_NAME_INT64 = ClassName.Get("ds", "int64");
    public static readonly ClassName TYPE_NAME_FLOAT = ClassName.Get("ds", "float");
    public static readonly ClassName TYPE_NAME_DOUBLE = ClassName.Get("ds", "double");
    public static readonly ClassName TYPE_NAME_BOOL = ClassName.Get("ds", "bool");
    public static readonly ClassName TYPE_NAME_STRING = ClassName.Get("ds", "string");
    public static readonly ClassName TYPE_NAME_BYTES = ClassName.Get("ds", "bytes");
    // 容器类型
    public static readonly ClassName TYPE_NAME_LIST = ClassName.Get("ds", "List", new List<TypeName>()
    {
        TypeParameterName.Get("T")
    });
    public static readonly ClassName TYPE_NAME_MAP = ClassName.Get("ds", "Map", new List<TypeName>()
    {
        TypeParameterName.Get("K"),
        TypeParameterName.Get("V")
    });
    // 装箱类型
    public static readonly ClassName TYPE_NAME_OBJECT = ClassName.Get("ds", "Object");
    public static readonly ClassName TYPE_NAME_NULLABLE = ClassName.Get("ds", "Nullable", new List<TypeName>()
    {
        TypeParameterName.Get("T")
    });

    #endregion
}
}