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
using Wjybxx.BigCatTool.Core;
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;
using Wjybxx.Dson;
using Wjybxx.Dson.Text;

namespace Wjybxx.BigCatTool.DataScript
{
/// <summary>
/// 数据脚本内置的注解类型
/// </summary>
public static class DSAnnotations
{
    /// <summary>
    /// 命名空间
    /// - 用于覆盖文件中指定的命名空间，用于配置特殊类型(第三方程序集）
    /// - 建议为每个第三方程序集定义一个ds文件
    /// 
    /// 语法如下：
    /// <code>// @Namespace{java : "xxx", cs: "xxx"} </code>
    /// - java 为java端的命名空间
    /// - cs 为csharp端的命名空间
    ///
    /// 1.由于命名空间可能包含点号，因此需要使用双引号
    /// 2.显式指定单个类型的命名空间时，需要使用全路径
    /// </summary>
    public const string NAMESPACE = "Namespace";

    /// <summary>
    /// 用于定义类型、字段、枚举值的可选项
    /// (主要服务于代码生成)
    ///
    /// 语法：<code>// @Options{isFlags: true, nonSerialized: true, immutable: true, ssti: true}</code>
    /// - isFlags 用于标识枚举类型是否是Flags类型
    /// - nonSerialized 用于字段时表示单个字段无需支持序列化；
    /// - immutable 用于标识集合字段需要转不可变集合
    /// - ssti 用于标识int字段或List{int}字段的值是共享字符串的索引
    ///
    /// 注：DS脚本中的类型默认都是可序列化的。
    /// </summary>
    public const string OPTIONS = "Options";

    /// <summary>
    /// 类型的序列化配置
    /// - 支持用于类型和字段
    ///
    /// 语法如下：
    /// <code>// @Codec{alias: [xxx, xxx, xxx], name: xyz, style: flow, elemStyle: flow} </code>
    /// - alias 表示类型序列化时的别名，别名用于简化Dson文本编写
    /// - name 表示字段序列化的名字，不推荐使用
    /// - style 表示该类型输出为Dson文本时的默认排版，其值可见<see cref="ObjectStyle"/>和<see cref="NumberStyle"/>和<see cref="StringStyle"/>；不区分大小写，不认识的值将被忽略。
    /// - elemStyle 用于指定数组元素或字典的Value的排版，可能不会生效。
    ///
    /// </summary>
    public const string CODEC = "Codec";

    /// <summary>
    /// 类型的Editor配置
    ///
    /// Editor相关的配置较复杂，所以可能不是很适合直接配置在类型数据上，而更适合通过额外的数据进行配置 -- 避免对类型元数据造成过多污染。
    /// (Editor需要对字段进行大量的配置，直接配置在类型元数据上，污染太严重)
    /// </summary>
    private const string Editor = "Editor";

    #region 注解属性的键

    public const string KEY_CS = "cs";
    public const string KEY_JAVA = "java";

    public const string KEY_ALIAS = "alias";
    public const string KEY_NAME = "name";
    public const string KEY_STYLE = "style";
    public const string KEY_ELEM_STYLE = "elemStyle";

    public const string KEY_IS_FLAGS = "isFlags";
    public const string KEY_NON_SERIALIZED = "nonSerialized";
    public const string KEY_IMMUTABLE = "immutable";
    public const string KEY_SSTI = "ssti";

    #endregion

    #region 注解处理

    private static readonly ImmutableDictionary<string, ObjectStyle>
        name2ObjectStyleDic = EnumUtil.GetValues<ObjectStyle>()
            .ToImmutableDictionary2(style => style.ToString().ToLower(), style => style);

    private static readonly ImmutableDictionary<string, NumberStyle>
        name2NumberStyleDic = EnumUtil.GetValues<NumberStyle>()
            .ToImmutableDictionary2(style => style.ToString().ToLower(), style => style);

    private static readonly ImmutableDictionary<string, StringStyle>
        name2StringStyleDic = EnumUtil.GetValues<StringStyle>()
            .ToImmutableDictionary2(style => style.ToString().ToLower(), style => style);

    /// <summary>
    /// 获取类型用于Dson编码时的别名
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    public static List<string> GetCodecAliases(DsonObject<string> options) {
        if (options.Count == 0 || !options.TryGetValue(DSAnnotations.KEY_ALIAS, out DsonValue value)) {
            return new List<string>();
        }
        DsonArray<string> dsonArray = value.AsArray();
        if (dsonArray.Count == 0) return new List<string>();
        //
        List<string> result = new List<string>(options.Count);
        foreach (DsonValue dsonValue in dsonArray) {
            result.Add(dsonValue.AsString().Trim());
        }
        return result;
    }

    public static ObjectStyle GetCodecStyle(DsonObject<string> options, ObjectStyle defaultStyle = ObjectStyle.Indent) {
        options.TryGetValue(DSAnnotations.KEY_STYLE, out DsonValue value);
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