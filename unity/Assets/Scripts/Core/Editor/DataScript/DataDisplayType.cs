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

namespace Wjybxx.BigCat.CoreEditor
{
/// <summary>
/// 数据的展示类型
///
/// 注：
/// 1.该枚举仅用于指示如何展示数据，不用于运行时。
/// 2.编辑器直接按数组方式操作结构，不检测字段名和类型 - 可提高灵活性。
/// 3.枚举值需要保持稳定，DS脚本中通过字符串映射。
/// 4.HashSet和字典在编辑器中不执行去重操作，因为无法检测重复 - 无法执行元素的equals，以及无法确定输入结束。
/// </summary>
public enum DataDisplayType
{
    Default, // 默认类型：普通Object类型，Value按字段顺序存储，固定长度
    Int32,
    Int64,
    Float,
    Double,
    Bool,

    String, // 二进制在编辑器中也是字符串
    TextArea, // 文本块
    AssetPath, // Unity资产路径，string值为资产路径

    Enum, // 枚举类型(pop + mask)
    DateTime, // 日期时间类型 [seconds + nanos]
    Timestamp, // 时间戳类型 [seconds + nanos]
    ObjectPath, // Unity资产对象引用 [ assetPath, localName, localId, type ]

    List, // 所有的集合类型都使用List类型展示；Map也可以指定为List，解码兼容
    Map, // 字典类型，KV连续存储，提供的特殊支持是：删除KV其中的一方，也将删除另一方
    Nullable, // 可空值类型，value存储在values中，空数组表示null；导出时会内联到上层
    Custom, // 自定义容器结构，需要绑定编辑器（暂未实现）

    Vector2,
    Vector3,
    Vector4, // 兼容四元数
    Vector2Int,
    Vector3Int,
    Color, // 需要4个浮点数变量
    Color32, // 需要4个整数变量(Unity没有开放单int32的构造函数)
}
}