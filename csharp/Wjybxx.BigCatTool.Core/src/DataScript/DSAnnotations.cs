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
using Wjybxx.Dson.Text;

namespace Wjybxx.BigCatTool.DataScript
{
/// <summary>
/// 数据脚本内置的注解类型
///
/// 注意：如果要扩展注解的Key，建议采用特殊的命名前缀，以避免和内置的Key冲突。
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
    /// <h3>用于类型时</h3>
    /// <code>// @Options{isFlags: true, dataClass: true, nonGenerate: true}</code>
    /// - isFlags 用于标识枚举类型是否是Flags类型
    /// - dataClass 用于标识class或struct是否是纯粹的数据类，如果为true，则会生成equals和hashcode方法
    /// - nonGenerate 非生成类，表示生成代码时跳过
    ///
    /// <h3>用于字段时</h3>
    /// <code>// @Options{nonSerialized: true, nonEqual: true, ssti: true}</code>
    /// - nonSerialized 用于标识字段无需支持序列化
    /// - nonEqual 是否不执行equals测试(不参与equals测试也就不会参与hash计算)
    /// - ssti 用于标识int字段或List{int}字段的值是共享字符串的索引
    ///
    /// 注：
    /// 1.DS脚本中的类型默认都是可序列化的。
    /// 2.用于服务和方法时，由用户自行约定。
    /// </summary>
    public const string OPTIONS = "Options";

    /// <summary>
    /// 类型的序列化配置
    ///
    /// <h3>用于类型时</h3>
    /// <code>// @Codec{alias: [xxx, xxx, xxx], style: flow, elemStyle: flow} </code>
    /// - alias 表示类型序列化时的别名，别名用于简化Dson文本编写
    /// - style 表示该类型输出为Dson文本时的默认排版
    /// - elemStyle 用于指定数组元素或字典的Value的排版；非必需功能，可能不会生效。
    ///
    /// <h3>用于字段时</h3>
    /// <code>// @Codec{name: xyz, style: flow} </code>
    /// - name 表示字段序列化的名字，不推荐使用
    /// - style 表示该类型输出为Dson文本时的默认排版；非必需功能，可能不会生效。
    ///
    /// 注：style的值见<see cref="ObjectStyle"/>和<see cref="NumberStyle"/>和<see cref="StringStyle"/>，不区分大小写，不认识的值将被忽略。
    /// </summary>
    public const string CODEC = "Codec";

    /// <summary>
    /// 服务和方法的Rpc选项
    ///
    /// <h3>用于类型时</h3>
    /// <code>//@Rpc {id: 1, async: true, ctx: true, manual: true}</code>
    /// - id表示为服务分配的id
    /// - async 表示服务端接口是否为异步模式；默认值为false
    /// - ctx 表示是否需要RpcContext参数；默认值为false
    /// - manual 表示是否手动管理返回时机，默认值为false; 如果为true，应当声明tx。
    ///
    /// 注：服务上的async等参数为参数的模式值，避免每个方法重复配置。
    /// 
    ///
    /// <h3>用于方法时</h3>
    /// <code>//@Rpc {async: true, ctx: true, manual: true}</code>
    /// - async 表示服务端接口是否为异步模式；默认值为false
    /// - ctx 表示是否需要RpcContext参数；默认值为false
    /// - manual 表示是否手动管理返回时机，默认值为false; 如果为true，应当声明tx。
    ///
    /// 注：方法上的async等属性用于覆盖service上的默认值。
    /// </summary>
    public const string RPC = "Rpc";
    /// <summary>
    /// RPC切面数据，与具体的应用相关
    /// </summary>
    public const string RPC_CUSTOM = "RpcCustom";

    /// <summary>
    /// 类型的Editor配置
    /// 语法：<code>// @Editor{displayType: List, tooltip: "半径" }</code>
    /// - displayType 展示类型
    /// - tooltip 编辑器中的tip
    ///
    /// 注：Editor相关的数据直接配置在类型上，如果数据量较多，可能影响阅读代码。
    /// </summary>
    public const string EDITOR = "Editor";
    /// <summary>
    /// 分支字段（标签类字段）
    ///
    /// 语法：<code>// @Branch{ ctrl: type, value: 1, alias: radius, tooltip: "半径" }</code>
    /// - ctrl 控制字段的名字
    /// - value 控制字段的值，支持数字和字符串
    /// - alias 在该类型下的展示别名
    /// - tooltip 编辑器下的tip
    /// 
    /// 示例解析：示例表示当type字段的值为1时字段有效，并展示为别名radius，tips为半径。
    /// </summary>
    public const string BRANCH = "Branch";
    /// <summary>
    /// 多态字段支持的类型
    /// 
    /// 语法：<code>// @SupportedTypes[Vector2, Vector3]</code>
    /// 如果存在该属性，则只可以切换到限定的类型，如果没有该属性，则可以切换到任意类型。
    /// (如非必要，不要限制 - 宽松的限制更不容易出Bug)
    /// </summary>
    public const string SUPPORTED_TYPES = "SupportedTypes";

    #region 注解属性的键

    public const string KEY_CS = "cs";
    public const string KEY_JAVA = "java";
    // 类型
    public const string KEY_IS_FLAGS = "isFlags";
    public const string KEY_DATA_CLASS = "dataClass";
    public const string KEY_NON_GENERATE = "nonGenerate";
    // 字段
    public const string KEY_NON_SERIALIZED = "nonSerialized";
    public const string KEY_NON_EQUAL = "nonEqual";
    public const string KEY_SSTI = "ssti";
    // Codec
    public const string KEY_ALIAS = "alias";
    public const string KEY_STYLE = "style";
    public const string KEY_ELEM_STYLE = "elemStyle";
    public const string KEY_NAME = "name";
    // Rpc
    private const string KEY_ID = "id";
    private const string KEY_ASYNC = "async";
    private const string KEY_MANUAL = "manual";
    private const string KEY_CTX = "ctx";
    // Editor
    public const string KEY_CTRL = "ctrl";
    public const string KEY_VALUE = "value";
    // public const string KEY_ALIAS = "alias"; // 重复
    public const string KEY_TOOLTIP = "tooltip";

    #endregion
}
}