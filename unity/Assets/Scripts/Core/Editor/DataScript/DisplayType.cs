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

namespace Wjybxx.BigCat.CoreEditor.DataScript
{
/// <summary>
/// 数据(字段)的展示类型
///
/// 注：
/// 1.该枚举仅用于指示如何展示数据，不用于运行时。
/// 2.编辑器直接按数组方式操作结构，不检测字段名和类型 - 投影可提高灵活性。
/// 3.这里的枚举只包含可特殊映射的样式，Map以及Nullable这类不支持映射的样式不在此枚举
/// </summary>
public enum DisplayType
{
    Default, // 根据字段类型和配置数据自动推测
    List, // 所有的集合类型默认都使用List类型展示；Map也可以指定为List，解码兼容

    AssetPath, // string类型特殊样式，string值为资产路径，编辑器会提供点击定位功能
    ObjectPath, // 资产对象引用 [collection, localPath, localId, type]
    DateTime, // 日期时间类型，底层结构[seconds + nanos]，但编辑器精确到秒
    Timestamp, // 时间戳类型，底层结构[seconds + nanos]，但编辑器精确到毫秒

    Vector2,
    Vector3,
    Vector4,
    Vector2Int,
    Vector3Int,
    Color, // float(r,g,b,a)
    Color32, // 单int值结构体
    Euler32, // 单int值结构体，xyz限制在[0, 360]
    MinMaxAABB, // 包围盒
}
}