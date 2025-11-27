#region LICENSE

// Copyright 2023 wjybxx(845740757@qq.com)
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
using Wjybxx.BigCatTool.Core;
using Wjybxx.BigCatTool.DataScript;
using Wjybxx.BigCatTool.Excel;
using Wjybxx.Commons;
using Wjybxx.Dson;

namespace Wjybxx.BigCatTool.Generator.Excel
{
/// <summary>
/// 表单常量
/// </summary>
public static class ExcelConstants
{
    #region options

    /// <summary>
    /// 表格的导出模式
    /// 格式为<code>C/S</code>
    /// 
    /// 注意：该属性不应该显式配置，而是我们在解析<code>options</code>的时候追加到<see cref="DsonObject{TK}"/>中的。
    /// </summary>
    public const string KEY_MODE = "mode";

    /// <summary>
    /// Cell文本需要翻译
    /// 1.支持String类型和List{String}类型。
    /// 2.String会被修正为int类型，但使用时无感。
    /// 3.List{string}会被修正为List{int}，但最终的代码会提供属性转换。
    /// 
    /// <code>i18n: true</code>
    /// </summary>
    public const string KEY_I18N = "i18n";
    /// <summary>
    /// 字符串值需要池化
    /// 1.支持String类型和List{String}类型。
    /// 2.String会被修正为int类型，但使用时无感。
    /// 3.List{string}会被修正为List{int}，但最终的代码会提供属性转换。
    /// 
    /// <code>intern: true</code>
    /// </summary>
    public const string KEY_INTERN = "intern";
    /// <summary>
    /// Cell数据保密：生成翻译表时应当忽略该列
    /// <code>secret: true</code>
    /// </summary>
    public const string KEY_SECRET = "secret";

    /// <summary>
    /// Cell为元组类型（定长数组）
    /// <code>isRecord: true</code>
    /// </summary>
    public const string KEY_IS_TUPLE = "isTuple";
    /// <summary>
    /// 标记int32和int64类型为Flags类型，允许采用‘|’表示进行或操作。
    /// 格式：<code>isFlags: true</code>
    /// 示例：<code>A | B | C</code>
    ///
    /// 对于BitArray类型，其实建议配置为数组类型；可以在导表时转换，也可以在解码时转换。
    /// </summary>
    public const string KEY_IS_FLAGS = "isFlags";
    /// <summary>
    /// 是否是资产路径
    /// </summary>
    public const string KEY_IS_ASSET_PATH = "isAssetPath";
    /// <summary>
    /// 标记number和string类型是否是常量值
    /// 
    /// 该属性用于Param表标记哪些参数需要导出额外的常量表，普通表直接指定列导出。
    /// (其实不建议为Param表生成常量类)
    /// </summary>
    public const string KEY_IS_CONST = "isConst";
    /// <summary>
    /// 用于标注字段是不可以热更新的，通常是指主键和索引键
    /// </summary>
    public const string KEY_IS_READONLY = "isReadonly";
    /// <summary>
    /// 用于标注字段不需要编解码(增加程序用的字段)
    /// 虽然在表格增加程序用的缓存字段不算优雅，但却是最简单的方式。
    /// </summary>
    public const string KEY_NON_SERIALIZED = "nonSerialized";
    /// <summary>
    /// 字符串数据在导出时执行Trim
    /// </summary>
    public const string KEY_TRIM = "trim";
    /// <summary>
    /// 字符串数据在导出时转小写
    /// </summary>
    public const string KEY_TO_LOWER = "toLower";

    /// <summary>
    /// value禁止重复
    /// 适用number类型和字符串类型
    /// <code>unique: true</code>
    /// </summary>
    public const string KEY_UNIQUE = "unique";
    /// <summary>
    /// number类型的最小值
    /// <code>min: 0</code>
    /// </summary>
    public const string KEY_MIN = "min";
    /// <summary>
    /// number类型的最大值
    /// <code>max: 999</code>
    /// </summary>
    public const string KEY_MAX = "max";
    /// <summary>
    /// 当前列数据不进行检查
    /// 比如期望在List和Map的元数据列配置注释时，可以禁用元数据列检查
    /// </summary>
    public const string KEY_NO_CHECK = "noCheck";

    /// <summary>
    /// 是否需要该单元格
    /// </summary>
    /// <param name="options"></param>
    /// <param name="requireMode"></param>
    /// <returns></returns>
    public static bool IsRequired(string? options, RequireMode requireMode) {
        return requireMode switch
        {
            RequireMode.Client => IsClientRequired(options),
            RequireMode.Server => IsServerRequired(options),
            RequireMode.All => IsClientRequired(options) || IsServerRequired(options),
            _ => false
        };
    }

    /// <summary>
    /// 是否客户端需要该单元格
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    public static bool IsClientRequired(string? options) {
        if (string.IsNullOrWhiteSpace(options)) {
            return false;
        }
        int spIndex = options.IndexOf('{');
        return spIndex < 0
            ? options.IndexOf('C') >= 0
            : options.IndexOf('C', 0, spIndex) >= 0;
    }

    /// <summary>
    /// 是否服务端需要该单元格
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    public static bool IsServerRequired(string? options) {
        if (string.IsNullOrWhiteSpace(options)) {
            return false;
        }
        int spIndex = options.LastIndexOf('{');
        return spIndex < 0
            ? options.IndexOf('S') >= 0
            : options.IndexOf('S', 0, spIndex) >= 0;
    }

    /// <summary>
    /// 解析表格选项
    /// 
    /// 格式:<code>C/S {}</code>，
    /// 1.C表示客户端需要该字段，S表示服务器需要该字段，'{}'中的内容为其它可选项。
    /// 2.默认不会将C/S拆为独立的C和S，由上层自行验证。
    /// 
    /// 注意：在解析options时，C/S信息默认会被追加到DsonObject中，且删除了其中的空白字符。
    /// </summary>
    /// <param name="options">要解析的字符串options</param>
    /// <param name="appendMode">是否将mode信息追加到返回的DsonObject中</param>
    /// <returns></returns>
    public static DsonObject<string> ParseOptions(string? options, bool appendMode = true) {
        if (string.IsNullOrWhiteSpace(options)) {
            DsonObject<string> result = new DsonObject<string>(1);
            if (appendMode) {
                result.Add(KEY_MODE, DsonString.EMPTY);
            }
            return result;
        }
        int spIndex = options.IndexOf('{');
        if (spIndex < 0) {
            DsonObject<string> result = new DsonObject<string>(1);
            string mode = ToolUtil.DeleteWhitespace(options);
            if (appendMode) {
                result.Add(KEY_MODE, new DsonString(mode));
            }
            return result;
        } else {
            string mode = ToolUtil.DeleteWhitespace(options.Substring(0, spIndex));
            DsonObject<string> result = Dsons.FromDson(options.Substring(spIndex)).AsObject();
            if (appendMode) {
                result.Add(KEY_MODE, new DsonString(mode));
            }
            return result;
        }
    }

    /// <summary>
    /// 获取bool类型属性值
    /// </summary>
    /// <param name="options">表头options</param>
    /// <param name="key">Key</param>
    /// <param name="defValue">key不存在时的默认值</param>
    /// <returns></returns>
    public static bool GetBool(DsonObject<string> options, string key, bool defValue = false) {
        return Annotation.GetBool(options, key, defValue);
    }

    /// <summary>
    /// 获取number类型属性值
    /// </summary>
    /// <param name="options">表头options</param>
    /// <param name="key">Key</param>
    /// <param name="defValue">key不存在时的默认值</param>
    /// <returns></returns>
    public static double GetNumber(DsonObject<string> options, string key, double defValue = 0) {
        return Annotation.GetNumber(options, key, defValue);
    }

    /// <summary>
    /// 获取int类型属性值
    /// </summary>
    /// <param name="options"></param>
    /// <param name="key"></param>
    /// <param name="defValue"></param>
    /// <returns></returns>
    public static int GetInt(DsonObject<string> options, string key, int defValue = 0) {
        return Annotation.GetInt(options, key, defValue);
    }

    #endregion

    #region cell拆分

    /// <summary>
    /// 拆分的最大单元格数量，通常不会超过10列，超过10列通常应该定义额外的表转换成行。
    /// </summary>
    public const int ELEMENT_LIMIT = 30;
    /// <summary>
    /// 数组元素的分隔符
    /// </summary>
    public const string ELEMENT_SEPARATOR = "#";
    /// <summary>
    /// 第一个元素的标识符
    /// </summary>
    public const string ELEMENT_FIRST = "#1";

    /// <summary>
    /// 获取关联的字段名字
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static string GetFieldName(string name) {
        int idx = name.IndexOf('#');
        return idx < 0 ? name : name.Substring2(0, idx);
    }

    /// <summary>
    /// 获取List/Map的元素名字
    /// </summary>
    /// <param name="name"></param>
    /// <param name="idx"></param>
    /// <returns></returns>
    public static string GetElementName(string name, int idx) {
        return name + "#" + idx;
    }

    /// <summary>
    /// 是否是List或Map的元素
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static bool IsListOrMapElement(string name) {
        return name.IndexOf('#') > 0;
    }

    /// <summary>
    /// 收集字段的所有元素
    /// </summary>
    internal static List<Header> CollectElementHeaders(Sheet sheet, Header fieldHeader, List<Header>? elemHeaders = null) {
        string fieldName = fieldHeader.name;
        bool isCollectionType = IsCollectionType(fieldHeader.type);
        bool isMapType = IsMapType(fieldHeader.type);
        if (!isCollectionType && !isMapType) {
            throw new Exception($"the field {fieldName} must be Collection or Map");
        }
        if (elemHeaders == null) {
            elemHeaders = new List<Header>(5);
        }
        // 收集所有列名 -- 配置表中索引1开始
        for (int index = 1; index <= ELEMENT_LIMIT; index++) {
            string elementName = GetElementName(fieldHeader.name, index);
            Header? elemHeader = sheet.GetHeader(elementName);
            if (elemHeader == null) {
                break;
            }
            if (isMapType && !string.IsNullOrWhiteSpace(elemHeader.type) && !IsPairType(elemHeader.type)) {
                throw new Exception($"the filed {fieldName} is map type, but the element {elemHeader.name} is not pair type");
            }
            elemHeaders.Add(elemHeader);
        }
        return elemHeaders;
    }

    #endregion

    #region 类型工具方法

    /// <summary>
    /// 表格应该不需要指针类型
    /// </summary>
    private const string TYPE_PTR = "Ptr";
    private const string TYPE_LPTR = "LPtr";
    /// <summary>
    /// BitArray在配置表配置为数组[A, B, C]格式，而不是 A | B | C 格式，
    /// 因为BitArray的数据可能较多，不太适合 A|B|C 格式，适合用Array格式。
    /// (其实不是很推荐大量使用Flags格式)
    /// </summary>
    private const string TYPE_BIT_ARRAY = "BitArray";

    public const string TYPE_LIST_INT32 = "List<" + DSKeywords.TYPE_INT32 + ">";
    public const string TYPE_LIST_STRING = "List<" + DSKeywords.TYPE_STRING + ">";

    /// <summary>
    /// 获取类型的默认值
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static string GetDefaultValue(string type) {
        if (IsNumberType(type)) return "0";
        if (IsStringType(type)) return "";
        if (IsBoolType(type)) return "false";
        if (IsCollectionType(type)) return "[]";
        if (IsMapType(type)) return "{}";
        // 指针、日期和时间戳...
        return "null";
    }

    /// <summary>
    /// 是否是数字类型
    ///
    /// 数字类型支持Dson文本支持的所有格式，此外还支持Flags格式<code>A|B|C</code>；
    /// 如果其它类型也期望使用支持Flags类型，需要自定义<see cref="DSTypeHandler"/>。
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsNumberType(string type) {
        return type == DSKeywords.TYPE_INT32
               || type == DSKeywords.TYPE_INT64
               || type == DSKeywords.TYPE_FLOAT
               || type == DSKeywords.TYPE_DOUBLE;
    }

    /// <summary>
    /// 是否是bool类型
    ///
    /// bool类型在表格支持4个值<code>true, false, 0, 1</code>
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsBoolType(string type) {
        return type == DSKeywords.TYPE_BOOL;
    }

    /// <summary>
    /// 是否是string类型
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsStringType(string type) {
        return type == DSKeywords.TYPE_STRING;
    }

    /// <summary>
    /// 是否是object类型
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsObjectType(string type) {
        return type == DSKeywords.TYPE_OBJECT;
    }

    /// <summary>
    /// 是否是可空值类型
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsNullableType(string type) {
        return type == DSKeywords.TYPE_NULLABLE || type.StartsWith(DSKeywords.TYPE_NULLABLE + "<");
    }

    /// <summary>
    /// 是否是List类型
    ///
    /// List类型格式<code>[V1, V2]</code>
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsListType(string type) {
        // 用户可能使用拆分后的符号进行测试
        return type == DSKeywords.TYPE_LIST || type.StartsWith(DSKeywords.TYPE_LIST + "<");
    }

    /// <summary>
    /// 是否是HashSet类型
    ///
    /// HashSet类型格式<code>[V1, V2]</code>
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsHashSetType(string type) {
        return type == DSKeywords.TYPE_HASHSET || type.StartsWith(DSKeywords.TYPE_HASHSET + "<");
    }

    /// <summary>
    /// 是否是集合类型(不包含字典)
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsCollectionType(string type) {
        return IsListType(type) || IsHashSetType(type);
    }

    /// <summary>
    /// 是否是字典类型
    ///
    /// 字典类型格式<code>{K1: V1, K2: V2}</code>
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsMapType(string type) {
        // 用户可能使用拆分后的符号进行测试
        return type == DSKeywords.TYPE_MAP || type.StartsWith(DSKeywords.TYPE_MAP + "<");
    }

    /// <summary>
    /// 是否是Pair类型
    ///
    /// Pair类型格式<code>{K: V}</code>，
    /// Excel中的Pair的Key限<code>int32, int64, string, enum</code>类型。
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsPairType(string type) {
        return type == DSKeywords.TYPE_PAIR || type.StartsWith(DSKeywords.TYPE_PAIR + "<");
    }

    /// <summary>
    /// 是否是List{string}类型
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsListStringType(string type) {
        return type == TYPE_LIST_STRING;
    }

    /// <summary>
    /// 是否是List{int32}类型
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsListInt32Type(string type) {
        return type == TYPE_LIST_INT32;
    }

    #endregion

    #region 分表

    /// <summary>
    /// 分表的表名分隔符
    /// </summary>
    private const string PARTITION_SEPARATOR = ".";
    /// <summary>
    /// 分表的基础表表名
    /// </summary>
    private const string STRING_BASE = "Base";

    /// <summary>
    /// 表格对应的Class名字
    /// </summary>
    public const string STRING_CLS_NAME = "clsName";
    /// <summary>
    /// 序列化版本
    /// </summary>
    public const string STRING_SERIAL_VERSION = "serialVersion";
    /// <summary>
    /// 序列化的内容行数(限普通表)
    /// </summary>
    public const string STRING_ROW_COUNT = "rowCount";

    /// <summary>
    /// 是否是分区表
    ///
    /// 命名规则：<code>Item.Base.0 Item.Base.1</code>
    /// </summary>
    /// <param name="sheetName"></param>
    /// <returns></returns>
    public static bool IsPartitionSheet(string sheetName) {
        int idx = sheetName.LastIndexOf('.');
        if (idx <= 0) return false;

        string suffix = sheetName.Substring(idx + 1);
        return int.TryParse(suffix, out _); // 末尾是普通数字
    }

    /// <summary>
    /// 获取分区合并后的表名
    /// </summary>
    /// <param name="sheetName"></param>
    /// <returns></returns>
    public static string GetMergedSheetName(string sheetName) {
        if (IsPartitionSheet(sheetName)) {
            int idx = sheetName.LastIndexOf('.');
            return sheetName.Substring2(0, idx);
        }
        return sheetName;
    }

    /// <summary>
    /// 是否是子类型表
    /// （注意：不是子表，是子类型表）
    ///
    /// 命名规则：<code>Item.Base Item.Equip</code>
    /// </summary>
    /// <param name="sheetName"></param>
    /// <returns></returns>
    public static bool IsSubTypeSheet(string sheetName) {
        string mergedSheetName = GetMergedSheetName(sheetName);
        return mergedSheetName.Contains('.') && !sheetName.EndsWith(".Base");
    }

    /// <summary>
    /// 是否是基类表
    /// </summary>
    /// <param name="sheetName"></param>
    /// <returns></returns>
    public static bool IsBaseTypeSheet(string sheetName) {
        return sheetName.EndsWith(".Base");
    }

    /// <summary>
    /// 获取基类表的名字
    /// </summary>
    /// <param name="sheetName"></param>
    /// <returns></returns>
    public static string GetBaseTypeSheetName(string sheetName) {
        return GetFirstSheetName(sheetName) + ".Base";
    }

    /// <summary>
    /// 获取子类型表的名字
    /// </summary>
    public static string GetSubTypeSheetName(string sheetName, string subTypeName) {
        return GetFirstSheetName(sheetName) + "." + subTypeName;
    }

    /// <summary>
    /// 获取第一级表名
    ///
    /// <code>Item => Item</code>
    /// <code>Item.Base => Item</code>
    /// <code>Item.Base.0 => Item</code>
    /// </summary>
    public static string GetFirstSheetName(string sheetName) {
        int idx = sheetName.IndexOf('.');
        return idx < 0 ? sheetName : sheetName.Substring2(0, idx);
    }

    /// <summary>
    /// 获取第二级表名
    ///
    /// <code>Item.Base => Base</code>
    /// <code>Item.Base.0 => Base</code>
    /// </summary>
    public static string GetSecondSheetName(string sheetName) {
        int start = sheetName.IndexOf('.');
        int end = sheetName.LastIndexOf('.');
        if (start < 0) {
            throw new ArgumentException("Sheet name is invalid: " + sheetName);
        }
        if (start < end) { // Item.Base.0
            return sheetName.Substring2(start + 1, end);
        }
        string r = sheetName.Substring2(start + 1);
        if (int.TryParse(r, out _)) { // Item.0
            throw new ArgumentException("Sheet name is invalid: " + sheetName);
        }
        return r;
    }

    #endregion
}
}