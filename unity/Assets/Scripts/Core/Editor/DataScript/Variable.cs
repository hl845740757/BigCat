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
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using Wjybxx.BigCat.Core;
using Wjybxx.BigCatTool.DataScript;
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;
using Wjybxx.Dson;
using Wjybxx.Dson.Types;

namespace Wjybxx.BigCat.Editor.DataScript
{
/// <summary>
/// 变量（值）
///
/// Q：为什么不再使用Unity的<see cref="SerializedProperty"/>处理Undo和Redo？
/// A：代码维护成本高，性能也不好。
/// 
/// 注：
/// 1.REDO无法保证List/Map元素引用的稳定性，只能保证可序列化数据的相等性；
/// 因此在执行Undo以后需要自顶向下修正和数组元素的缓存字段，而部分缓存将无法恢复。
///
/// 2.由于我们已修改Undo/Redo实现，因此中字段的类型修复不依赖TypeSymbol；
/// 而从文件中读取数据时，需要根据TypeSymbol修正类型。
/// </summary>
public sealed class Variable : IDisposable
{
    /// <summary>
    /// 变量元数据信息
    ///
    /// 1.<see cref="DSNamedType"/>或<see cref="DSField"/>类型，避免过多假设。
    /// 2.如果是泛型类，必须是已构造具体泛型，即泛型参数也是<see cref="DSNamedType"/>。
    /// 3.普通业务避免使用该属性。
    /// </summary>
    public DSElement defineInfo { get; internal set; }
    /// <summary>
    /// 编辑器相关配置
    ///
    /// 注：如果是普通字段，通常由<see cref="defineInfo"/>上的注解信息解析得到。
    /// </summary>
    public VariableCfg cfg { get; internal set; }
    /// <summary>
    /// 变量的类型
    ///
    /// 注：对于多态字段，该属性会变更；内存Undo/Redo会备份该数据。
    /// </summary>
    public DSNamedType type { get; internal set; }
    /// <summary>
    /// 展示名(用于覆盖配置)
    /// </summary>
    public string displayName { get; internal set; }

    /// <summary>
    /// 当前是否是null值
    ///
    /// 注：值类型应当通过Nullable实现null，引用类型可直接使用该属性实现null。
    /// </summary>
    public bool isNull;
    /// <summary>
    /// 整数类型值(int32、int64、bool)
    /// </summary>
    public long longValue;
    /// <summary>
    /// 浮点数类型值(float、double)
    /// </summary>
    public double doubleValue;
    /// <summary>
    /// 字符串值(string, bytes)
    /// </summary>
    public string stringValue;
    /// <summary>
    /// 如果不是原子类型，则Value按字段存储在List中。
    /// 
    /// 1.对于字典类型，KV会封装一个Pair变量 -- 更容易维护。
    /// 2.对于Nullable类型，value也会存储在这里，但仍然通过IsNull属性标识是否为null（依赖注入）。
    /// </summary>
    public List<Variable> values { get; internal set; }

    /// <summary>
    /// 关联的数据节点
    /// </summary>
    public DataNode dataNode { get; internal set; }
    /// <summary>
    /// 用户自定义数据(缓存)
    /// </summary>
    public object userData { get; set; }

    #region util

    public int intValue {
        get => (int)longValue;
        set => longValue = value;
    }

    public float floatValue {
        get => (float)doubleValue;
        set => doubleValue = value;
    }

    public bool boolValue {
        get => longValue != 0;
        set => longValue = value ? 1 : 0;
    }

    #region struct

    // Debug窗口默认不展示，以避免异常
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public Vector2 vector2Value {
        get {
            float x = values[0].floatValue;
            float y = values[1].floatValue;
            return new Vector2(x, y);
        }
        set {
            values[0].floatValue = value.x;
            values[1].floatValue = value.y;
        }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public Vector3 vector3Value {
        get {
            float x = values[0].floatValue;
            float y = values[1].floatValue;
            float z = values[2].floatValue;
            return new Vector3(x, y, z);
        }
        set {
            values[0].floatValue = value.x;
            values[1].floatValue = value.y;
            values[2].floatValue = value.z;
        }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public Vector4 vector4Value {
        get {
            float x = values[0].floatValue;
            float y = values[1].floatValue;
            float z = values[2].floatValue;
            float w = values[3].floatValue;
            return new Vector4(x, y, z, w);
        }
        set {
            values[0].floatValue = value.x;
            values[1].floatValue = value.y;
            values[2].floatValue = value.z;
            values[3].floatValue = value.w;
        }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public Quaternion quaternionValue {
        get {
            float x = values[0].floatValue;
            float y = values[1].floatValue;
            float z = values[2].floatValue;
            float w = values[3].floatValue;
            return new Quaternion(x, y, z, w);
        }
        set {
            values[0].floatValue = value.x;
            values[1].floatValue = value.y;
            values[2].floatValue = value.z;
            values[3].floatValue = value.w;
        }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public Vector2Int vector2IntValue {
        get {
            int x = values[0].intValue;
            int y = values[1].intValue;
            return new Vector2Int(x, y);
        }
        set {
            values[0].intValue = value.x;
            values[1].intValue = value.y;
        }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public Vector3Int vector3IntValue {
        get {
            int x = values[0].intValue;
            int y = values[1].intValue;
            int z = values[2].intValue;
            return new Vector3Int(x, y, z);
        }
        set {
            values[0].intValue = value.x;
            values[1].intValue = value.y;
            values[2].intValue = value.z;
        }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public Color colorValue {
        get {
            float r = values[0].floatValue;
            float g = values[1].floatValue;
            float b = values[2].floatValue;
            float a = values[3].floatValue;
            return new Color(r, g, b, a);
        }
        set {
            values[0].floatValue = value.r;
            values[1].floatValue = value.g;
            values[2].floatValue = value.b;
            values[3].floatValue = value.a;
        }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public DateTime dateTimeValue { // 底层结构：seconds + nanos
        get {
            long seconds = values[0].longValue;
            int nanos = values[1].intValue;
            ExtDateTime extDateTime = new ExtDateTime(seconds, nanos);
            return extDateTime.ToDateTime();
        }
        set {
            ExtDateTime extDateTime = ExtDateTime.OfDateTime(value);
            values[0].longValue = extDateTime.Seconds;
            values[1].intValue = extDateTime.Nanos;
        }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public Timestamp timestampValue { // 底层结构：seconds + nanos
        get {
            long seconds = values[0].longValue;
            int nanos = values[1].intValue;
            return new Timestamp(seconds, nanos);
        }
        set {
            values[0].longValue = value.Seconds;
            values[1].intValue = value.Nanos;
        }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public ObjectPath objectPathValue {
        get {
            string collection = values[0].stringValue;
            string localPath = values[1].stringValue;
            long localId = values[2].longValue;
            int type = values[3].intValue;
            return new ObjectPath(collection, localPath, localId, type);
        }
        set {
            values[0].stringValue = value.collection;
            values[1].stringValue = value.localPath;
            values[2].longValue = value.localId;
            values[3].intValue = value.type;
        }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public MinMaxAABB aabbValue {
        get {
            Vector3 min = values[0].vector3Value;
            Vector3 max = values[1].vector3Value;
            return new MinMaxAABB(min, max);
        }
        set {
            values[0].vector3Value = value.min;
            values[1].vector3Value = value.max;
        }
    }

    #endregion

    #region array

    /// <summary>
    /// 字段数量
    /// </summary>
    public int Count => values == null ? 0 : values.Count;

    /// <summary>
    /// 逻辑层自行确定values的有效性
    /// </summary>
    /// <param name="index"></param>
    public Variable this[int index] {
        get => values[index];
        set {
            if (value != null && dataNode != null) {
                value.SetDataNode(dataNode);
            }
            values[index] = value;
        }
    }

    /// <summary>
    /// 获取最后一个变量
    /// </summary>
    public Variable TryPeekLast() {
        if (values != null && values.Count > 0) {
            return values[values.Count - 1];
        }
        return null;
    }

    /// <summary>
    /// 查询嵌套变量的索引
    /// </summary>
    public int IndexOf(Variable nestedVar) {
        return values == null ? -1 : values.IndexOf(nestedVar);
    }

    /// <summary>
    /// 添加元素
    /// </summary>
    /// <param name="nestedVar"></param>
    public void Add(Variable nestedVar) {
        Insert(values.Count, nestedVar);
    }

    /// <summary>
    /// 插入元素
    /// </summary>
    /// <param name="index">元素索引</param>
    /// <param name="nestedVar">要添加的元素</param>
    public void Insert(int index, Variable nestedVar) {
        if (nestedVar != null && dataNode != null) {
            nestedVar.SetDataNode(dataNode);
        }
        values.Insert(index, nestedVar);
    }

    /// <summary>
    /// 删除指定位置的元素
    /// </summary>
    /// <param name="index"></param>
    /// <param name="detach">是否解除对dataNode的引用</param>
    /// <returns></returns>
    public Variable RemoveAt(int index, bool detach = false) {
        Variable nestedVar = values[index];
        if (nestedVar != null && detach) {
            nestedVar.SetDataNode(null);
        }
        values.RemoveAt(index);
        return nestedVar;
    }

    /// <summary>
    /// 移动元素
    /// </summary>
    /// <param name="index"></param>
    /// <param name="newIndex"></param>
    public void MoveTo(int index, int newIndex) {
        Variable variable = values[index];
        values.RemoveAt(index);
        values.Insert(newIndex, variable);
    }

    /// <summary>
    /// 清空数组
    /// </summary>
    public void ClearArray() {
        values?.Clear();
    }

    #endregion

    #region data-script

    /// <summary>
    /// 是否是<see cref="Nullable{T}"/>类型
    /// </summary>
    public bool isNullableType => DSUtil.IsNullableType(type);
    /// <summary>
    /// 是否的Pair类型
    /// </summary>
    public bool isPariType => DSUtil.IsPairType(type);
    /// <summary>
    /// 是否是集合类型(List/HashSet)
    /// </summary>
    public bool isCollectionType => DSUtil.IsCollectionType(type);
    /// <summary>
    /// 是否是字典类型(Map)
    /// </summary>
    public bool isMapType => DSUtil.IsMapType(type);

    #endregion

    public bool isRoot => dataNode != null && dataNode.value == this;

    /// <summary>
    /// 是否已销毁(方便它处判断Variable的有效性)
    /// </summary>
    public bool isDisposed => defineInfo == null;

    /// <summary>
    /// 销毁对象
    /// </summary>
    public void Dispose() {
        defineInfo = null;
        cfg = null;
        type = null;
        dataNode = null;
        userData = null;
        if (values != null) {
            foreach (Variable nestedVar in values) {
                nestedVar?.Dispose();
            }
            values = null;
        }
    }

    /// <summary>
    /// 查找指定变量
    /// 
    /// 注：List/HastSet/Map 应当使用索引符号定位元素；普通对象虽然也可以通过下标取值，但要记住字段的下标较为困难。
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public Variable FindValue(string path) {
        if (values == null) return null;
        if (string.IsNullOrWhiteSpace(path)) {
            return null;
        }
        if (!path.Contains('.')) {
            return FindValueHelper(path.Trim());
        }
        Variable current = this;
        foreach (string part in ObjectUtil.SplitAndTrim(path, '.')) {
            if (string.IsNullOrWhiteSpace(part)) {
                throw new ArgumentException("invalid path: " + path);
            }
            current = current.FindValueHelper(part);
            if (current == null) {
                return null;
            }
        }
        return current;
    }

    private Variable FindValueHelper(string path) {
        if (values == null) {
            return null;
        }
        int length = path.Length;
        if (path[0] == '{' && path[length - 1] == '}') {
            string indexString = path.Substring(0, length - 1).Trim();
            int index = int.Parse(indexString);
            return values[index];
        }
        foreach (Variable nestedVar in values) {
            if (nestedVar.defineInfo.SimpleName == path) {
                return nestedVar;
            }
        }
        return null;
    }

    #endregion

    #region undo/redo

    /// <summary>
    /// 应用修改（创建undo记录）
    ///
    /// <returns>是否创建了新的Undo记录</returns>
    /// </summary>
    public bool ApplyModifiedProperties() {
        return dataNode != null && dataNode.ApplyModifiedProperties();
    }

    /// <summary>
    /// 变量被添加到Node时应当调用该方法
    /// </summary>
    /// <param name="dataNode"></param>
    internal void SetDataNode(DataNode dataNode) {
        this.dataNode = dataNode;
        if (values == null) {
            return;
        }
        foreach (Variable nestedVar in values) {
            nestedVar?.SetDataNode(dataNode);
        }
    }

    /// <summary>
    /// <h3>Backup</h3>
    /// 1.由于Variable整体还是比较轻量的，因此我们不定义额外的抽象来存储数据，这使得我们可以备份部分缓存数据。
    /// 2.通过持久化数据备份和恢复由<see cref="DsonValue"/>负责。
    /// 
    /// <h3>Restore</h3>
    /// 1.Restore只恢复需要保存的数据，默认不清理缓存字段。
    /// 2.当数组的长度发生变化时，自动创建的数组元素的缓存字段皆为null。
    /// 3.当数组的长度不发生改变，缓存也可能与实际的上下文不匹配 —— 无法保证动态路径下缓存数据的有效性。
    /// 4.在执行Restore以后应当自上而下修正缓存数据。
    /// </summary>
    internal void Restore(Variable backup) {
        defineInfo = backup.defineInfo;
        cfg = backup.cfg;
        type = backup.type;
        displayName = backup.displayName;
        //
        isNull = backup.isNull;
        longValue = backup.longValue;
        doubleValue = backup.doubleValue;
        stringValue = backup.stringValue;
        //
        List<Variable> backupValues = backup.values;
        if (backupValues == null) {
            values?.Clear();
            values = null;
            return;
        }
        int count = backupValues.Count;
        values ??= new List<Variable>(count);
        values.EnsureCapacity(count);
        // 修正交叠元素
        for (int index = 0; index < count; index++) {
            if (index >= values.Count) {
                values.Add(null);
            }
            Variable nestedVar = backupValues[index];
            if (nestedVar == null) {
                values[index] = null;
                continue;
            }
            values[index] ??= new Variable();
            values[index].Restore(nestedVar);
        }
        // 删除多于元素
        if (values.Count > count) {
            values.RemoveRange(count, values.Count - count);
        }
    }

    internal static bool BackupEquals(Variable left, Variable right) {
        if (left == null && right == null) return true;
        if (left == null || right == null) return false;
        //
        if (!ReferenceEquals(left.type, right.type)) return false;
        if (left.isNull != right.isNull) return true;
        if (left.longValue != right.longValue) return false;
        if (!left.doubleValue.Equals(right.doubleValue)) return false;
        if (left.stringValue != right.stringValue) return false;
        //
        List<Variable> leftValues = left.values;
        List<Variable> rightValues = right.values;
        if (leftValues == null && rightValues == null) return true;
        if (leftValues == null || rightValues == null) return false;
        if (leftValues.Count != rightValues.Count) return false;
        //
        for (int index = 0; index < leftValues.Count; index++) {
            if (!BackupEquals(leftValues[index], rightValues[index]))
                return false;
        }
        return true;
    }

    #endregion
}
}