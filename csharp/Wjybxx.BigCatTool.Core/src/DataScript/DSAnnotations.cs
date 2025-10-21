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
    /// - nonGenerate 表示生成代码时跳过
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
    /// <code>// @Codec{alias: [xxx, xxx, xxx], style: flow} </code>
    /// - alias 表示类型序列化时的别名，别名用于简化Dson文本编写；不会自动追加文件中约定的别名默认前缀
    /// - style 表示该类型输出为Dson文本时的默认排版
    ///
    /// <h3>用于字段时</h3>
    /// <code>// @Codec{name: xyz, style: flow} </code>
    /// - name 表示字段序列化的名字，不推荐使用 -- 尽量直接使用字段名
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
    /// 类型或字段的编辑器基础选项
    /// 
    /// 语法：<code>// @Editor{ displayName: Vector3, displayType: Vector3, tooltip: "Tip", dsonType: Pointer }</code>
    /// - displayName 展示名；如果不配置，默认为类型名或字段名
    /// - displayType 展示类型，枚举名见代码
    /// - tooltip 类型和字段tip
    /// - dsonType dson类型投影，可将自定义数据结构导出为Dson内建结构，如ObjectPtr。
    /// - min 数字类型的最小值
    /// - max 数字类型的最大值
    /// - initNull bool类型，是否将字段初始化为null值，不适用List和Map字段
    /// - menuPath 字符串类型，节点菜单路径，用于配置脚本
    /// - scrollView bool类型，表示List和Map是否启用滚动视图
    /// </summary>
    public const string EDITOR = "Editor";
    /// <summary>
    /// 端口字段
    ///
    /// 语法：<code>// @NodePort{ side: Right }</code>
    /// - side 端口的显示位置：Left、Right、Bottom，未指定的情况下默认Right
    ///
    /// 注：当List内的元素也需要定义数据接口时，必须将List字段自身标记为PortField。
    /// </summary>
    public const string PORT_FIELD = "PortField";

    /// <summary>
    /// Pop字段(支持多个)
    /// 
    /// 语法：<code>// @PopField{ value: 1, displayName: AABB }</code>
    /// - value 字段对应的值
    /// - displayName 字段的展示名；如果是string字段，无需配置
    ///
    /// 注：Pop字段通常和分支字段配套使用，实现标签类；也用于IntMask字段。
    /// </summary>
    public const string POP_FIELD = "PopField";
    /// <summary>
    /// 分支字段（标签类字段）(支持多个)
    ///
    /// 语法：<code>// @BranchField{ ctrl: type, value: 1, displayName: radius, tooltip: "半径" }</code>
    /// - ctrl 控制字段的名字，通常为PopField或枚举字段
    /// - value 控制字段的值，支持int32和string
    /// - displayName 在该类型下的展示别名
    /// - tooltip 编辑器下的tip，可选
    /// 
    /// 示例解析：示例表示当type字段的值为1时字段有效，并展示为别名radius，tips为半径。
    /// </summary>
    public const string BRANCH_FIELD = "BranchField";

    /// <summary>
    /// Mask字段
    ///
    /// 语法：<code>// @MaskField[ Left, Right, Bottom ]</code>
    ///
    /// 注：
    /// 1.注解为数组类型，Value为每个bit对应的名字，无特殊字符时可不加双引号。
    /// 2.如果字段是集合或Map类型，则表示集合内元素支持的多态类型。
    /// 3.如果是枚举字段，数组保持为空即可。
    /// </summary>
    public const string MASK_FIELD = "MaskField";
    /// <summary>
    /// 多态字段
    /// 语法：<code>// @PloyField[ Vector2, Vector3 ]</code>
    ///
    /// 注：
    /// 1.注解为数组类型，Value为为支持的类型。
    /// 2.如果字段是集合或Map类型，则表示集合内元素支持的多态类型。
    /// </summary>
    public const string PLOY_FIELD = "PloyField";

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
    public const string KEY_NAME = "name";

    // Rpc
    private const string KEY_ID = "id";
    private const string KEY_ASYNC = "async";
    private const string KEY_MANUAL = "manual";
    private const string KEY_CTX = "ctx";

    // Editor
    public const string KEY_DISPLAY_NAME = "displayName";
    public const string KEY_DISPLAY_TYPE = "displayType";
    public const string KEY_DSON_TYPE = "dsonType";
    public const string KEY_MIN = "min";
    public const string KEY_MAX = "max";
    public const string KEY_INIT_NULL = "initNull";
    public const string KEY_SKIP_NULL = "skipNull";

    public const string KEY_SIDE = "side";
    public const string KEY_CTRL = "ctrl";
    public const string KEY_VALUE = "value";
    public const string KEY_TOOLTIP = "tooltip";

    public const string KEY_MENU_PATH = "menuPath";
    public const string KEY_SCROLL_VIEW = "scrollView";

    #endregion
}
}