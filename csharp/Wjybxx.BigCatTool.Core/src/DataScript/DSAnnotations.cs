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
using Wjybxx.Commons;
using Wjybxx.Dson.Codec;

namespace Wjybxx.BigCatTool.DataScript
{
/// <summary>
/// 数据脚本内置的注解类型
///
/// 注意：
/// 1.如果要扩展注解的Key，建议采用特殊的命名前缀，以避免和内置的Key冲突。
/// 2.如何支持超长内容注解？两种方式：将一个注解拆为多个注解；通过id指向<see cref="DSInst"/>。
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
    ///
    /// <h3>用于类型时</h3>
    /// <code>// @Options{isFlags: true, dataClass: true, nonGenerate: true, encodeFeatures: [ObjectIndent]}</code>
    /// - isFlags 标识枚举类型是否是Flags类型
    /// - style 序列化样式，如果只想配置文本样式，可以通过style代替序列化特征值
    /// - dataClass 标识class或struct是否是纯粹的数据类，如果为true，则会生成equals和hashcode方法
    /// - nonGenerate 表示生成代码时跳过，即类型是外部库类型的镜像
    /// -
    /// - alias 表示类型序列化时的别名，别名用于简化Dson文本编写；单值模式可不声明为数组；
    /// - encodeFeatures 序列化特征值，使用枚举名配置，忽略大小写 - <see cref="SerializeFeatures"/>
    /// - decodeFeatures 反序列化特征值，使用枚举名配置，忽略大小写 - <see cref="DeserializeFeatures"/>
    ///
    /// <h3>用于字段时</h3>
    /// <code>// @Options{ nonSerialized: true, nonEqual: true, ssti: true, encodeFeatures: [NumberHex] }</code>
    /// - nonSerialized 标识字段无需支持序列化
    /// - nonEqual 是否不执行equals测试(不参与equals测试也就不会参与hash计算)
    /// - ssti 标识int字段或List{int}字段的值是共享字符串的索引
    /// -
    /// - encodeFeatures 序列化特征值
    /// - decodeFeatures 反序列化特征值
    ///
    /// 注：
    /// 1.DS脚本中的类型默认都是可序列化的。
    /// 2.用于服务和方法时，由用户自行约定。
    /// 3.特征值支持字符串和数组两种模式，字符串格式使用竖线分割；忽略大小写。
    /// </summary>
    public const string OPTIONS = "Options";

    /// <summary>
    /// 类型或字段的编辑器基础选项
    /// 
    /// 语法：<code>// @Editor{ displayType: Vector3, displayName: Vector3, tooltip: "Tip", dsonType: Pointer }</code>
    /// - displayType 展示类型，枚举名见代码
    /// - displayName 字段展示别名；默认为类型名或字段名
    /// - tooltip 类型和字段tip
    ///
    /// - min 数字类型的最小值
    /// - max 数字类型的最大值
    /// - initNull bool类型，是否将字段初始化为null值(延迟初始化)
    /// - isDelayed 是否延迟响应输入
    /// - isMultiline 是否是多行文本
    /// - isInteger 是否是整数类型AABB
    /// 
    /// - dsonType dson类型投影，可将自定义数据结构导出为Dson内建结构，如ObjectPtr，Pointer。
    /// - nodeFeatures node的特征值，是否启用Port端口等
    /// </summary>
    public const string EDITOR = "Editor";
    /// <summary>
    /// 字段编辑器风格(Inspector视图)
    /// - expanded 是否默认展开
    /// - maxWidth 最大宽度
    /// - maxHeight 最大高度
    /// - labelMargin label和value的边距
    /// </summary>
    public const string FIELD_STYLE = "FieldStyle";
    /// <summary>
    /// 节点编辑器风格(GraphView视图)
    /// 
    /// </summary>
    public const string NODE_STYLE = "NodeStyle";

    /// <summary>
    /// 端口字段
    ///
    /// 语法：<code>// @PortField{ side: Right }</code>
    /// - side 端口的显示位置；Left、Right、Bottom，未指定的情况下默认Right。
    /// - distinct 端口去重：禁止List/Map类型端口连接至同一对象
    /// - expanded 是否默认展开；同侧只能出现一个默认展开端口
    /// 
    /// 注：当List/Map内的元素也需要定义数据接口时，必须将List字段自身标记为PortField。
    /// </summary>
    public const string PORT_FIELD = "PortField";

    /// <summary>
    /// Pop字段(支持多个)
    /// 
    /// 语法：<code>// @PopField{ value: 1, displayName: AABB }</code>
    /// - value 字段对应的值
    /// - displayName 字段的展示名；如果是string字段，暂无需配置
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
    public const string KEY_ENCODE_FEATURES = "encodeFeatures";
    public const string KEY_DECODE_FEATURES = "decodeFeatures";
    public const string KEY_NAME = "name";

    // Editor
    public const string KEY_DISPLAY_TYPE = "displayType";
    public const string KEY_DISPLAY_NAME = "displayName";
    public const string KEY_TOOLTIP = "tooltip";

    public const string KEY_DSON_TYPE = "dsonType";
    public const string KEY_NODE_FEATURES = "nodeFeatures";

    public const string KEY_MIN = "min";
    public const string KEY_MAX = "max";
    public const string KEY_INIT_NULL = "initNull";
    public const string KEY_IS_DELAYED = "isDelayed";
    public const string KEY_IS_MULTILINE = "isMultiline";
    public const string KEY_IS_INTEGER = "isInteger";

    public const string KEY_CTRL = "ctrl";
    public const string KEY_VALUE = "value";
    public const string KEY_SIDE = "side";
    public const string KEY_DISTINCT = "distinct";
    public const string KEY_EXPANDED = "expanded";

    public const string KEY_MAX_WIDTH = "maxWidth";
    public const string KEY_MAX_HEIGHT = "maxHeight";
    public const string KEY_LABEL_MARGIN = "labelMargin";
    public const string KEY_X_LABEL_MARGIN = "xLabelMargin";
    public const string KEY_Y_LABEL_MARGIN = "yLabelMargin";
    public const string KEY_Z_LABEL_MARGIN = "zLabelMargin";
    public const string KEY_W_LABEL_MARGIN = "wLabelMargin";

    #endregion
}
}