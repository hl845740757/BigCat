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
using Wjybxx.BigCatEditor.Core;
using Wjybxx.BigCatEditor.DataScript;
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;
using Wjybxx.Dson;

namespace Wjybxx.BigCatEditor.Generator.Excel
{
/// <summary>
/// 表单常量
/// </summary>
public static class SheetConstants
{
    #region 参数表表头

    public const string COL_OPTIONS = "options";
    public const string COL_TYPE = "type";
    public const string COL_NAME = "name";
    public const string COL_VALUE = "value";
    public const string COL_COMMENT = "comment";

    /// <summary>
    /// 参数表的列
    ///
    /// PS：虽然定义为普通表的转置看似更规范，但只配置单值的情况下，commit放在value前面的体验并不好。
    /// </summary>
    public static readonly ImmutableList<string> PARAM_SHEET_COLS = new[]
    {
        COL_OPTIONS,
        COL_TYPE,
        COL_NAME,
        COL_VALUE,
        COL_COMMENT,
    }.ToImmutableList2();

    #endregion

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
    /// 1.支持String类型和List{String}类型，但List{String}类型的元素必须拆分配置。
    /// 2.String会被修正为int类型，但使用时无感。
    /// 3.List{string}会被修正为List{int}，且使用时需要显式处理。
    /// 
    /// <code>i18n: true</code>
    /// </summary>
    public const string KEY_I18N = "i18n";
    /// <summary>
    /// 字符串值需要池化
    /// 1.支持String类型和List{String}类型，但List{String}类型的元素必须拆分配置。
    /// 2.String会被修正为int类型，但使用时无感。
    /// 3.List{string}会被修正为List{int}，且使用时需要显式处理。
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
    /// Cell为record类型（定长数组）
    /// <code>isRecord: true</code>
    /// </summary>
    public const string KEY_IS_RECORD = "isRecord";
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

    #endregion

    #region options工具方法

    /// <summary>
    /// 是否需要该单元格
    /// </summary>
    /// <param name="options"></param>
    /// <param name="mode"></param>
    /// <returns></returns>
    public static bool IsRequired(string? options, Mode mode) {
        return mode switch
        {
            Mode.Client => IsClientRequired(options),
            Mode.Server => IsServerRequired(options),
            Mode.All => IsClientRequired(options) || IsServerRequired(options),
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
        int spIndex = options.LastIndexOf('{');
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

    private static readonly DsonString EMPTY = new DsonString(string.Empty);

    /// <summary>
    /// 解析表格选项
    /// 
    /// 
    /// 格式:<code>C/S {}</code>，
    /// 1.C表示客户端需要该字段，S表示服务器需要该字段，'{}'中的内容为其它可选项。
    /// 2.默认不会拆为C/S为独立的C和S，由上层自行验证。
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
                result.Add(KEY_MODE, EMPTY);
            }
            return result;
        }
        int spIndex = options.LastIndexOf('{');
        if (spIndex < 0) {
            DsonObject<string> result = new DsonObject<string>(1);
            string mode = Util.DeleteWhitespace(options);
            if (appendMode) {
                result.Add(KEY_MODE, new DsonString(mode));
            }
            return result;
        } else {
            string mode = Util.DeleteWhitespace(options.Substring(0, spIndex));
            DsonObject<string> result = Dsons.FromDson(options.Substring(spIndex)).AsObject();
            if (appendMode) {
                result.Add(KEY_MODE, new DsonString(mode));
            }
            return result;
        }
    }

    /// <summary>
    /// 获取number类型属性值
    /// </summary>
    /// <param name="options">表头options</param>
    /// <param name="key">Key</param>
    /// <param name="defValue">key不存在时的默认值</param>
    /// <returns></returns>
    public static double GetNumber(DsonObject<string> options, string key, double defValue = 0) {
        if (!options.TryGetValue(key, out DsonValue value)) {
            return defValue;
        }
        return value.IsNumber ? value.AsDsonNumber().DoubleValue : defValue;
    }

    /// <summary>
    /// 获取bool类型属性值
    /// </summary>
    /// <param name="options">表头options</param>
    /// <param name="key">Key</param>
    /// <param name="defValue">key不存在时的默认值</param>
    /// <returns></returns>
    public static bool GetBool(DsonObject<string> options, string key, bool defValue = false) {
        if (!options.TryGetValue(key, out DsonValue value)) {
            return defValue;
        }
        if (value.DsonType == DsonType.Bool) return value.AsBool();
        if (value.IsNumber) {
            int number = value.AsDsonNumber().IntValue;
            if (number == 0) return false;
            if (number == 1) return true;
            throw new Exception("invalid number: " + number);
        }
        return defValue;
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
    /// 是否是List或Map的元素
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static bool IsListOrMapElement(string name) {
        return name.IndexOf('#') > 0;
    }

    #endregion

    #region 类型工具方法

    /// <summary>
    /// 获取类型的默认值
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static string GetDefaultValue(string type) {
        if (IsNumberType(type)) return "0";
        if (IsStringType(type)) return "";
        if (IsBoolType(type)) return "false";
        return "null";
    }

    /// <summary>
    /// 是否是数字类型
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
    /// </summary>
    /// <param name="typed"></param>
    /// <returns></returns>
    public static bool IsBoolType(string typed) {
        return typed == DSKeywords.TYPE_BOOL;
    }

    /// <summary>
    /// 是否是string类型
    /// </summary>
    /// <param name="typed"></param>
    /// <returns></returns>
    public static bool IsStringType(string typed) {
        return typed == DSKeywords.TYPE_STRING;
    }

    /// <summary>
    /// 是否是List类型
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsListType(string type) {
        return type.StartsWith(DSKeywords.TYPE_LIST + "<");
    }

    /// <summary>
    /// 是否是字典类型
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsMapType(string type) {
        return type.StartsWith(DSKeywords.TYPE_MAP + "<");
    }

    /// <summary>
    /// 是否是Pair类型
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsPairType(string type) {
        return type.StartsWith(DSKeywords.TYPE_PAIR + "<");
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
    /// 是否是分区表
    ///
    /// 命名规则：<code>Item.Base.0 Item.Base.1</code>
    /// </summary>
    /// <param name="sheetName"></param>
    /// <returns></returns>
    public static bool IsPartitionSheet(string sheetName) {
        int idx = sheetName.LastIndexOf('.'); // Item.Base.0 => Item.Base
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
        return GetMergedSheetName(sheetName).Contains('.');
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
        return sheetName + ".Base";
    }

    /// <summary>
    /// 获取顶层表名
    ///
    /// <code>Item.Base => Item</code>
    /// <code>Item.Base.0 => Item</code>
    ///
    /// PS：实在不知道取啥名了...
    /// </summary>
    public static string GetRootSheetName(string sheetName) {
        int idx = sheetName.IndexOf('.');
        return idx < 0 ? sheetName : sheetName.Substring2(0, idx);
    }

    #endregion

    #region 注解

    /// <summary>
    /// 表示表格是生成的
    /// </summary>
    public const string ANNOTATION_GENERATED = "Generated";

    #endregion
}
}