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

namespace Wjybxx.BigCat.CoreEditor.DataScript
{
/// <summary>
/// 数据节点
///
/// 注：
/// 1.Node的主要作用是提供localId和localPath，以支持互相引用（对象图/数据图/数据集）。
/// 2.Node实际充当的是Value的对象头。
/// </summary>
public sealed class DataNode
{
    /// <summary>
    /// 文件内的唯一id，程序分配
    ///
    /// 注：当外部引用Node时，应当优先使用name进行引用，更具有稳定性。
    /// </summary>
    public long localId { get; }
    /// <summary>
    /// 节点的名字
    /// </summary>
    public string name { get; internal set; }
    /// <summary>
    /// 对象归属的文件夹（虚拟分组）
    ///
    /// 注：如果folder不为空，外部通过name引用node时，应当通过<code>folder/name</code>的方式引用。
    /// </summary>
    public string folder { get; internal set; }
    /// <summary>
    /// 节点注释
    ///
    /// 注：避免过长的注释，顶层节点的注释会写入输出数据。
    /// </summary>
    public string comment;
    /// <summary>
    /// 数据的值
    /// 
    /// 注：顶层Node在导出数据时都会写入类型信息；避免修改Value对象的引用，否则可能导致Redo后数据缓存数据丢失。
    /// </summary>
    public Variable value { get; internal set; }
    /// <summary>
    /// 数据节点特征值（会持久化，因此在初始化完成后避免再修改）
    /// </summary>
    public Features features = Features.Defaults;

    /// <summary>
    /// 关联的数据图
    /// </summary>
    public DataGraph graph { get; internal set; }
    /// <summary>
    /// 关联的视图
    ///
    /// 注：如果不在Graph视图展示，则可能为null。
    /// </summary>
    public NodeView nodeView;
    /// <summary>
    /// GraphView下的坐标
    /// </summary>
    public Vector2 position;
    /// <summary>
    /// Node上的output字段
    ///
    /// 注意：
    /// 1.如果是Node是List/Map类型，Undo以后Variable的引用可能产生变更，需要手动修复。
    /// 2.逻辑层不记录Input信息，目前来说并无必要 —— 因为无特殊数据需要保存，我们也不需要对Inputs排序。
    /// 3.逻辑层会提供获取Inputs的方法，实时查询代替缓存 —— 可以有效降低复杂度。
    /// </summary>
    public readonly List<Variable> outputFields = new List<Variable>();
    /// <summary>
    /// 用户自定义数据(缓存)
    /// </summary>
    public object userData { get; set; }

    /// <summary>
    /// 数据版本
    /// </summary>
    private int version = 1;
    /// <summary>
    /// 当前使用的数据备份
    /// </summary>
    internal NodeMemento currentMemento;

    internal DataNode(long localId) {
        this.localId = localId;
    }

    #region undo/redo

    /// <summary>
    /// 应用属性修改，即创建备份点
    /// 
    /// 注意：创建新的备份会清空Redo队列。
    /// <returns>是否创建了新的Undo记录</returns>
    /// </summary>
    public bool ApplyModifiedProperties() {
        if (graph == null || !IsDataChanged()) {
            return false;
        }
        version++;
        graph.CreateUpdateCommand(this);
        return true;
    }

    /// <summary>
    /// 检测数据是否发生改变
    ///
    /// 注：不含备份的情况总是返回false。
    /// </summary>
    /// <returns></returns>
    private bool IsDataChanged() {
        NodeMemento memento = this.currentMemento;
        if (memento == null) return false;
        if (memento.version != version) return true;
        if (memento.name != name) return true;
        if (memento.folder != folder) return true;
        if (memento.comment != comment) return true;
        if (memento.position != position) return true;
        if (memento.features != features) return true;
        return !Variable.BackupEquals(memento.value, value);
    }

    /// <summary>
    /// 创建数据备份
    /// </summary>
    internal void Backup(NodeMemento backup) {
        backup.localId = localId;
        backup.version = version;
        backup.name = name;
        backup.folder = folder;
        backup.comment = comment;
        backup.position = position;
        backup.features = features;
        if (value != null) {
            backup.value ??= new Variable();
            backup.value.Restore(value); // 反向恢复即备份
        } else {
            backup.value = null;
        }
    }

    /// <summary>
    /// 数据恢复
    /// </summary>
    internal void Restore(NodeMemento backup) {
        if (localId != backup.localId) {
            throw new InvalidOperationException("localId != memento.localId");
        }
        version = backup.version;
        name = backup.name;
        folder = backup.folder;
        comment = backup.comment;
        position = backup.position;
        features = backup.features;
        //
        if (backup.value != null) {
            value ??= new Variable();
            value.Restore(backup.value);
            value.SetDataNode(this);
        } else {
            value = null;
        }
    }

    /// <summary>
    /// 测试备份数据的相等性
    /// </summary>
    internal static bool BackupEquals(NodeMemento left, NodeMemento right) {
        if (left.localId != right.localId) return false;
        if (left.version != right.version) return false;
        if (left.name != right.name) return false;
        if (left.folder != right.folder) return false;
        if (left.comment != right.comment) return false;
        if (left.position != right.position) return false;
        if (left.features != right.features) return false;
        return Variable.BackupEquals(left.value, right.value);
    }

    #endregion

    /// <summary>
    /// 由于Node的冗余数据较多，因此我们使用额外的数据结构保存数据
    ///
    /// 注：该对象使用全局池化方案
    /// </summary>
    internal class NodeMemento
    {
        public long localId;
        public int version;
        public string name;
        public string folder;
        public string comment;
        public Variable value;
        public Vector2 position;
        public Features features;

        public void Reset() {
            // value不reset，restore的时候直接复用
            localId = 0;
            version = 0;
            name = null;
            folder = null;
            comment = null;
            position = default;
            features = default;
        }
    }
}
}