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

namespace Wjybxx.BigCatTool.DataScript
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

    /** 服务 */
    public const string SERVICE = "service";
    /** 函数 */
    public const string FUNC = "func";

    /** 用于修饰字段，表示字段是只读的 */
    public const string READONLY = "readonly";

    #region options-file

    /** 生成的java文件的包名 -- value为字符串，需要加双引号 */
    public const string JAVA_PACKAGE = "java_package";
    /** csharp命名空间 -- value为字符串，需要加双引号 */
    public const string CSHARP_NAMESPACE = "csharp_namespace";
    /** 用于指示文件内的数据结构都是数据类 -- value为bool类型，true或false */
    public const string DATA_CLASS = "data_class";

    #endregion

    #region options-type

    // DS脚本推荐使用注解代替option
    /// <summary>
    /// 是否允许不同的枚举常量指向同一个值
    /// (语法同protobuf)
    ///
    /// <code>allow_alias = true;</code>
    /// </summary>
    public const string ALLOW_ALIAS = "allow_alias";
    /// <summary>
    /// 保留字段编号
    /// (语法同protobuf)
    /// 
    /// <code>reversed 1, 2, 3 to 10;</code>
    /// <code>reversed "age", "env";</code>
    /// </summary>
    public const string RESERVED = "reserved";

    #endregion

    #region builtin-types

    /// <summary>
    /// 全局命名空间(文件名)
    /// </summary>
    public const string GLOBAL = "global";
    // 框架只需要支持必要的内置类型即可 -- 可正确记录数据即可
    // 原子类型
    public const string TYPE_INT32 = "int32";
    public const string TYPE_INT64 = "int64";
    public const string TYPE_FLOAT = "float";
    public const string TYPE_DOUBLE = "double";
    public const string TYPE_BOOL = "bool";
    public const string TYPE_STRING = "string";
    public const string TYPE_BYTES = "bytes";
    // 内建结构
    public const string TYPE_DATETIME = "DateTime";
    public const string TYPE_TIMESTAMP = "Timestamp";
    public const string TYPE_PAIR = "Pair"; // 除了拆分配置Map时，其它时候避免使用Pair
    // 容器类型
    public const string TYPE_LIST = "List";
    public const string TYPE_HASHSET = "HashSet";
    public const string TYPE_MAP = "Map";
    // 装箱类型
    public const string TYPE_OBJECT = "Object";
    public const string TYPE_NULLABLE = "Nullable";

    // 原子类型
    public static readonly ClassName TYPE_NAME_INT32 = ClassName.Get(GLOBAL, TYPE_INT32);
    public static readonly ClassName TYPE_NAME_INT64 = ClassName.Get(GLOBAL, TYPE_INT64);
    public static readonly ClassName TYPE_NAME_FLOAT = ClassName.Get(GLOBAL, TYPE_FLOAT);
    public static readonly ClassName TYPE_NAME_DOUBLE = ClassName.Get(GLOBAL, TYPE_DOUBLE);
    public static readonly ClassName TYPE_NAME_BOOL = ClassName.Get(GLOBAL, TYPE_BOOL);
    public static readonly ClassName TYPE_NAME_STRING = ClassName.Get(GLOBAL, TYPE_STRING);
    public static readonly ClassName TYPE_NAME_BYTES = ClassName.Get(GLOBAL, TYPE_BYTES);
    // 内建结构
    public static readonly ClassName TYPE_NAME_DATETIME = ClassName.Get(GLOBAL, TYPE_DATETIME);
    public static readonly ClassName TYPE_NAME_TIMESTAMP = ClassName.Get(GLOBAL, TYPE_TIMESTAMP);
    public static readonly ClassName TYPE_NAME_PAIR = ClassName.Get(GLOBAL, TYPE_PAIR, new List<TypeName>()
    {
        TypeParameterName.Get("K"),
        TypeParameterName.Get("V")
    });
    // 容器类型
    public static readonly ClassName TYPE_NAME_LIST = ClassName.Get(GLOBAL, TYPE_LIST, new List<TypeName>()
    {
        TypeParameterName.Get("T")
    });
    public static readonly ClassName TYPE_NAME_HASH_SET = ClassName.Get(GLOBAL, TYPE_HASHSET, new List<TypeName>()
    {
        TypeParameterName.Get("T")
    });
    public static readonly ClassName TYPE_NAME_MAP = ClassName.Get(GLOBAL, TYPE_MAP, new List<TypeName>()
    {
        TypeParameterName.Get("K"),
        TypeParameterName.Get("V")
    });
    // 装箱类型
    public static readonly ClassName TYPE_NAME_OBJECT = ClassName.Get(GLOBAL, TYPE_OBJECT);
    public static readonly ClassName TYPE_NAME_NULLABLE = ClassName.Get(GLOBAL, TYPE_NULLABLE, new List<TypeName>()
    {
        TypeParameterName.Get("T")
    });

    #endregion
}
}