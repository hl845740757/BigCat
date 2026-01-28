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
    /// region支持
    /// <code>// @region[cmd, p1, p2]</code>
    /// <code>// @region{cmd: cmd, k1: v1, k2: v2}</code>
    ///
    /// 1.region注解强制为其所属容器的注解
    /// 2.region的值格式由用户自行约定，可以是数组，也可以是Object
    /// </summary>
    public const string REGION = "region";
    public const string ENDREGION = "endregion";

    /// <summary>
    /// 用于定义类型、字段、枚举值的可选项
    ///
    /// <h3>用于类型时</h3>
    /// <code>// @Options{isFlags: true, isIndexes: true, dataClass: true, nonGenerate: true, encodeFeatures: [ObjectIndent]}</code>
    /// - isFlags 标识枚举类型是否是Flags类型(枚举值为掩码)
    /// - isIndexes 标识枚举类型是否是Index类型(枚举值可充当数组下标，可存储在BitSet等集合中)
    /// - baseType 代码生成时的特殊超类符号，主要用于指定枚举的类型
    /// 
    /// - dataClass 标识class或struct是否是纯粹的数据类，如果为true，则会生成equals和hashcode方法
    /// - nonGenerate 表示生成代码时跳过，即类型是外部库类型的镜像
    /// -
    /// - style 序列化样式，如果只想配置文本样式，可以通过style代替序列化特征值
    /// - alias 表示类型序列化时的别名，别名用于简化Dson文本编写；单值模式可不声明为数组；
    /// - encodeFeatures 序列化特征值，使用枚举名配置，忽略大小写 - <see cref="SerializeFeatures"/>
    /// - decodeFeatures 反序列化特征值，使用枚举名配置，忽略大小写 - <see cref="DeserializeFeatures"/>
    /// - projection 类型投影，将自定义数据结构投影到其它数据结构，以覆盖数据结构的编辑器属性；投影类型在生成代码时自动跳过
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
    /// - initNull bool类型，是否将字段初始化为null值(延迟初始化)；Port字段自动初始化null
    /// - pathType 用于初始化ObjectPath字段，枚举值见ObjectPathType
    /// - isDelayed 是否延迟响应输入
    /// - isMultiline 是否是多行文本
    /// - isInteger 是否是整数类型AABB
    /// - isFolder 是否是文件夹路径
    ///
    /// - minWidth 最小宽度
    /// - maxWidth 最大宽度
    /// - maxHeight 最大高度
    /// - labelMargin label和value的边距
    /// - labelMargins 原子结构内嵌字段的label和value的边距；数组类型，允许null值
    /// 
    /// - dsonType dson类型投影，可将自定义数据结构导出为Dson内建结构，如ObjectPtr、Pointer、Double4。
    /// - nodeFeatures node的特征值，是否启用Port端口等
    /// </summary>
    public const string EDITOR = "Editor";
    /// <summary>
    /// 节点编辑器风格(GraphView视图)
    /// </summary>
    private const string NODE_STYLE = "NodeStyle";

    /// <summary>
    /// 端口名重映射
    ///
    /// 语法：<code>// @PortNameRemap{ path: displayName }</code>
    /// - path 为数据路劲，采用"a.b.c"格式
    ///
    /// 注：
    /// 1.仅适用于静态路径字段，不适用于List/Map内的元素。
    /// 2.每一个键值对为一个映射，用于解决复用数据结构导致的端口名重复问题。
    /// </summary>
    public const string PORT_NAME_REMAP = "PortNameRemap";

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
    /// Pop字段(支持多个，自动合并)
    /// 
    /// 语法：<code>// @PopField{ value: 1, displayName: AABB }</code>
    /// - value 字段对应的值
    /// - displayName 字段的展示名；如果是string字段，暂无需配置
    ///
    /// 注：Pop字段通常和分支字段配套使用，实现标签类；也用于IntMask字段。
    /// </summary>
    public const string POP_FIELD = "PopField";
    /// <summary>
    /// 分支字段（标签类字段）(支持多个，自动合并)
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
    /// 多态字段(支持多个，自动合并)
    /// 
    /// 语法：<code>// @PloyField[ Vector2, Vector3 ]</code>
    ///
    /// 注：
    /// 1.注解为数组类型，Value为支持的类型。
    /// 2.如果字段是集合或Map类型，则表示集合内元素支持的多态类型。
    /// </summary>
    public const string PLOY_FIELD = "PloyField";

    /// <summary>
    /// Mask字段(支持多个，自动合并)
    /// 
    /// 语法1：<code>// @MaskField[ Left, Right, Bottom ]</code>
    /// 语法2：<code>// @MaskField[ @{clsName} ]</code> - 将关联枚举放在对象头
    ///
    /// 注：
    /// 1.注解为数组类型，Value为每个bit对应的名字，无特殊字符时可不加双引号。
    /// 2.如果字段是集合或Map类型，则表示集合内元素支持的Mask配置。
    /// 3.语法2表示通过Indexes类型枚举初始化MaskName
    /// </summary>
    public const string MASK_FIELD = "MaskField";

    /// <summary>
    /// 候选值(支持多个，自动合并)
    /// 
    /// 语法：<code>// @Candidates[ Vector2, Vector3 ]</code>
    ///
    /// 1.注解为数组类型，Value为候选值；TODO 如果注解是Object类型，则key为displayName，value为数字值。
    /// 2.目前仅支持string字段
    /// </summary>
    public const string CANDIDATES = "Candidates";

    #region 注解属性的键

    public const string KEY_CS = "cs";
    public const string KEY_JAVA = "java";
    // 类型
    public const string KEY_IS_FLAGS = "isFlags";
    public const string KEY_IS_INDEXES = "isIndexes";
    public const string KEY_BASE_TYPE = "baseType";
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
    public const string KEY_PROJECTION = "projection";
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
    public const string KEY_PATH_TYPE = "pathType";
    public const string KEY_IS_DELAYED = "isDelayed";
    public const string KEY_IS_MULTILINE = "isMultiline";
    public const string KEY_IS_INTEGER = "isInteger";
    public const string KEY_IS_FOLDER = "isFolder";

    public const string KEY_CTRL = "ctrl";
    public const string KEY_VALUE = "value";
    public const string KEY_PATH = "path";
    public const string KEY_SIDE = "side";
    public const string KEY_DISTINCT = "distinct";
    public const string KEY_EXPANDED = "expanded";

    public const string KEY_MIN_WIDTH = "minWidth";
    public const string KEY_MAX_WIDTH = "maxWidth";
    public const string KEY_MAX_HEIGHT = "maxHeight";
    public const string KEY_LABEL_MARGIN = "labelMargin";
    public const string KEY_LABEL_MARGINS = "labelMargins";

    #endregion
}
}