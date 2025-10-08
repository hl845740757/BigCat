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
using UnityEngine;
using Wjybxx.Commons;

namespace Wjybxx.BigCat.CoreEditor
{
/// <summary>
/// 数据节点
/// </summary>
[Serializable]
public sealed class DataNode
{
    /// <summary>
    /// 对象归属的文件（分组）（可选）
    ///
    /// 注：如果folder不为空.外部应当通过<code>folder/name</code>的方式引用。
    /// </summary>
    public string folder;
    /// <summary>
    /// 文件内的唯一id，程序分配
    /// </summary>
    public long localId;
    /// <summary>
    /// 节点的名字，用户分配
    ///
    /// 注：当外部引用Node时，应当优先使用name进行引用，更具有稳定性。
    /// </summary>
    public string name;
    /// <summary>
    /// 节点注释
    ///
    /// 注：避免过长的注释，顶层节点的注释会写入输出数据。
    /// </summary>
    [TextArea(0, 5)]
    public string comment;

    /// <summary>
    /// 数据的值
    /// 
    /// 注：顶层Node在导出数据时都会写入类型信息。
    /// </summary>
    public DataVariable value;
    /// <summary>
    /// 数据端口（连接其它Node）
    /// </summary>
    [NonSerialized] public List<NodePort> ports = new List<NodePort>();

    /// <summary>
    /// UI坐标
    /// </summary>
    [NonSerialized] public Rect position;
    /// <summary>
    /// 关联的绘制器
    /// </summary>
    [NonSerialized] public DataNodeDrawer drawer;
    /// <summary>
    /// 在Update队列中的索引
    /// </summary>
    [NonSerialized] internal int qIndex = -1;
    /// <summary>
    /// 是否是被动节点
    /// (非用户创建的节点；不可手动删除，不可切换绑定的数据类型，不序列化)
    /// </summary>
    [NonSerialized] internal bool isPassive;

    #region util

    /// <summary>
    /// 查找port
    ///
    /// 注：由于数据量通常较少，因此不做额外的缓存。
    /// </summary>
    /// <param name="portId"></param>
    /// <returns></returns>
    public NodePort FindPort(int portId) {
        foreach (NodePort nodePort in ports) {
            if (nodePort.id == portId) return nodePort;
        }
        return null;
    }

    #endregion
}

/// <summary>
/// 节点端口
/// 
/// 注：
/// 1.端口用于通过连线的方式为字段赋值；
/// 2.如果字段的类型为<see cref="ObjectPath"/>，则表示保存为指针（延迟加载），否则表示内联对象。
/// </summary>
public sealed class NodePort
{
    public int id; // 端口id
    public long targetNode; // 引用的节点id
    public int targetPort; // 目标端口Id

    [NonSerialized] public Rect position; // ui坐标 - 检测点击
    public DataNode node { get; internal set; } // Node对象缓存
    public DataVariable field { get; internal set; } // 字段缓存
    public int index { get; internal set; } = -1; // 端口排序
    public Side side { get; internal set; } = Side.Right; // 展示位置
}

/// <summary>
/// Port的显示位置
///
/// 注：不可手动指定为Top区，Top区固定为Parent连接点，有特殊逻辑。
/// </summary>
public enum Side
{
    Left,
    Right,
    Bottom,
    Top
}
}