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
/// 数据节点的特征值
/// </summary>
[Flags]
public enum Features : uint
{
    /// <summary>
    /// 是否启用数据端口（场景中数据通常不启用）
    ///
    /// 注：
    /// 1.该属性在添加到对象图以后不应该再变更，否则可能导致错误。
    /// 2.启用该属性的Node在添加到Graph后，所有的Port字段将转换为ObjectPath类型。
    /// 3.该属性需要持久化，才能正确从文件中恢复数据。
    /// </summary>
    EnablePort = 0x01,
    /// <summary>
    /// 输出节点的桥接Node
    ///
    /// 1.代替边为输出端口和输入端口提供额外的配置数据。
    /// 2.桥接Node应当给予特殊的展示。
    /// </summary>
    OutputBridge = 0x02,
    /// <summary>
    /// 输入节点的桥接Node(预留)
    /// </summary>
    InputBridge = 0x04,
    /// <summary>
    /// 是否是纯粹的内存节点（不需要序列化保存）
    /// </summary>
    MemoryOnly = 0x08,

    /// <summary>
    /// 编辑器数据(更建议folder命名区分)
    /// </summary>
    EditorData = 0x10,
    /// <summary>
    /// 场景内数据(更建议folder命名区分)
    /// </summary>
    SceneData = 0x20,

    /// <summary>
    /// 没有<see cref="NodeView"/>，即不显示在GraphView中
    ///
    /// 注：不显示在GraphView中，但仍可以显示Inspector视图，由程序打开。
    /// </summary>
    WithoutNodeView = 0x100,

    /// <summary>
    /// 默认值集合
    /// </summary>
    Defaults = EnablePort,
}
}