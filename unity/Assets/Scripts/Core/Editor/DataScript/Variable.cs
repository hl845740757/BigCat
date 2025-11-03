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
using UnityEditor;
using UnityEngine;
using Wjybxx.BigCat.Core;
using Wjybxx.BigCatTool.DataScript;
using Wjybxx.Commons;
using Wjybxx.Dson.Types;

namespace Wjybxx.BigCat.CoreEditor.DataScript
{
/// <summary>
/// 变量（值）
///
/// 注：REDO无法保证List元素引用的稳定性，只能保证可序列化数据的相等性；
/// 因此在执行Undo以后需要自顶向下修正和数组元素的缓存字段。
/// </summary>
[Serializable]
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
    /// 注：对于多态字段，该属性会变更；因此需要在Undo之后通过typeSymbol进行恢复。
    /// </summary>
    public DSNamedType type { get; internal set; }

    /// <summary>
    /// 变量的类型，类型需要和值一起进行undo和redo
    /// </summary>
    [SerializeField] private string _typeSymbol;
    /// <summary>
    /// 当前是否是null值
    ///
    /// 注：值类型需通过Nullable实现null，引用类型可直接使用该属性实现null。
    /// </summary>
    [SerializeField] private bool _isNull;

    /// <summary>
    /// 整数类型值(int32、int64、bool)
    /// </summary>
    [SerializeField] private long _longValue;
    /// <summary>
    /// 浮点数类型值(float、double)
    /// </summary>
    [SerializeField] private double _doubleValue;
    /// <summary>
    /// 字符串值(string, bytes)
    /// </summary>
    [SerializeField] private string _stringValue;
    /// <summary>
    /// 如果不是原子类型，则Value按字段存储在List中。
    /// 
    /// 1.对于字典类型，KV会封装一个Pair变量 -- 更容易维护。
    /// 2.对于Nullable类型，value也会存储在这里，但仍然通过IsNull属性标识是否为null（依赖注入）。
    /// 3.由框架创建数据结构实例时初始化，可能为null
    /// </summary>
    [SerializeReference]
    public List<Variable> values;

    /// <summary>
    /// 关联的Port
    ///
    /// 注：引用信息存储在<see cref="objectPathValue"/>中；如果是List类型，则每个Value都是一个ObjectPath。
    /// </summary>
    public PortView portView { get; internal set; }
    /// <summary>
    /// 用户自定义数据
    ///
    /// 注意：在undo和redo的时候，如果数组的长度发生变更，数组元素的引用可能发生变化导致缓存数据丢失。
    /// </summary>
    public object userData { get; set; }

    /// <summary>
    /// 序列化对象，用于支持Redo和Undo
    /// </summary>
    public SerializedProperty serializedProperty { get; set; }
    private SerializedProperty _longProperty;
    private SerializedProperty _doubleProperty;
    private SerializedProperty _stringProperty;
    private SerializedProperty _valuesProperty;

    #region util

    public SerializedProperty longValueProperty => _longProperty ??= serializedProperty?.FindPropertyRelative("_longValue");
    public SerializedProperty doubleValueProperty => _doubleProperty ??= serializedProperty?.FindPropertyRelative("_doubleValue");
    public SerializedProperty stringValueProperty => _stringProperty ??= serializedProperty?.FindPropertyRelative("_stringValue");
    public SerializedProperty valuesProperty => _valuesProperty ??= serializedProperty?.FindPropertyRelative("values");

    public bool isNull {
        get => _isNull;
        set {
            if (serializedProperty == null) {
                _isNull = value;
            } else {
                using SerializedProperty isNullProperty = serializedProperty.FindPropertyRelative("_isNull");
                isNullProperty.boolValue = value;
            }
        }
    }

    public string typeSymbol {
        get => _typeSymbol;
        set {
            if (serializedProperty == null) {
                _typeSymbol = value;
            } else {
                using SerializedProperty isNullProperty = serializedProperty.FindPropertyRelative("_typeSymbol");
                isNullProperty.stringValue = value;
            }
        }
    }

    public int intValue {
        get => (int)_longValue;
        set {
            if (serializedProperty == null) {
                _longValue = value;
            } else {
                longValueProperty.longValue = value;
            }
        }
    }

    public long longValue {
        get => _longValue;
        set {
            if (serializedProperty == null) {
                _longValue = value;
            } else {
                longValueProperty.longValue = value;
            }
        }
    }

    public float floatValue {
        get => (float)_doubleValue;
        set {
            if (serializedProperty == null) {
                _doubleValue = value;
            } else {
                doubleValueProperty.doubleValue = value;
            }
        }
    }

    public double doubleValue {
        get => _doubleValue;
        set {
            if (serializedProperty == null) {
                _doubleValue = value;
            } else {
                doubleValueProperty.doubleValue = value;
            }
        }
    }

    public bool boolValue {
        get => _longValue != 0;
        set {
            if (serializedProperty == null) {
                _longValue = value ? 1 : 0;
            } else {
                longValueProperty.longValue = value ? 1 : 0;
            }
        }
    }

    public string stringValue {
        get => _stringValue;
        set {
            if (serializedProperty == null) {
                _stringValue = value;
            } else {
                stringValueProperty.stringValue = value;
            }
        }
    }

    #region struct

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
    /// 注意：执行set前需确保已和序列化层同步数组长度，最好是通过<see cref="Insert"/>添加元素。
    /// </summary>
    /// <param name="index"></param>
    public Variable this[int index] {
        get => values[index];
        set {
            if (serializedProperty == null) {
                values[index] = value;
            } else {
                values[index]?.UnbindValuesProperty();
                var property = valuesProperty.GetArrayElementAtIndex(index);
                property.managedReferenceValue = value;
                value?.BindProperty(property);
            }
        }
    }

    public void Add(Variable nestedVar) {
        Insert(values.Count, nestedVar);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="index">元素索引</param>
    /// <param name="nestedVar">要添加的元素</param>
    /// <param name="applyModifiers">用于合批Apply</param>
    public void Insert(int index, Variable nestedVar, bool applyModifiers = true) {
        if (serializedProperty == null) {
            values.Insert(index, nestedVar);
        } else {
            valuesProperty.InsertArrayElementAtIndex(index);
            using (var property = valuesProperty.GetArrayElementAtIndex(index)) {
                property.managedReferenceValue = nestedVar;
            }
            if (applyModifiers) {
                ApplyModifiedProperties();
                RebindValuesProperty(index);
            }
        }
    }

    public Variable RemoveAt(int index, bool applyModifiers = true) {
        Variable nestedVar = values[index];
        if (serializedProperty == null) {
            values.RemoveAt(index);
        } else {
            valuesProperty.DeleteArrayElementAtIndex(index);
            if (applyModifiers) {
                ApplyModifiedProperties();
                RebindValuesProperty(index);
            }
        }
        return nestedVar;
    }

    public void MoveTo(int index, int newIndex, bool applyModifiers = true) {
        if (index == newIndex) return;
        if (serializedProperty == null) {
            Variable variable = values[index];
            values.RemoveAt(index);
            values.Insert(newIndex, variable);
        } else {
            valuesProperty.MoveArrayElement(index, newIndex);
            if (applyModifiers) {
                ApplyModifiedProperties();
                MathCommon.MinMax(index, newIndex, out int min, out int max);
                RebindValuesProperty(min, max);
            }
        }
    }

    public void ClearArray() {
        if (values == null) return;
        foreach (Variable nestedVar in values) {
            nestedVar.UnbindProperty();
        }
        if (serializedProperty == null) {
            values.Clear();
        } else {
            valuesProperty.ClearArray();
            ApplyModifiedProperties();
        }
    }

    #endregion

    public void ApplyModifiedProperties() {
        serializedProperty.serializedObject.ApplyModifiedProperties();
    }

    public void UpdateProperties() {
        serializedProperty.serializedObject.Update();
        EditorUtility.SetDirty(serializedProperty.serializedObject.targetObject);
    }

    /// <summary>
    /// 绑定属性
    /// </summary>
    /// <param name="property"></param>
    public void BindProperty(SerializedProperty property) {
        if (serializedProperty != property) { // 只有切换引用时才能销毁
            UnbindPropertySelf();
            serializedProperty = property;
        }
        RebindValuesProperty();
    }

    /// <summary>
    /// 解除属性绑定
    /// </summary>
    public void UnbindProperty() {
        if (serializedProperty != null) {
            UnbindPropertySelf();
        }
        UnbindValuesProperty();
    }

    /// <summary>
    /// 解除自身属性绑定
    /// </summary>
    private void UnbindPropertySelf() {
        serializedProperty?.Dispose();
        _longProperty?.Dispose();
        _doubleProperty?.Dispose();
        _stringProperty?.Dispose();
        _valuesProperty?.Dispose();
        //
        serializedProperty = null;
        _longProperty = null;
        _doubleProperty = null;
        _stringProperty = null;
        _valuesProperty = null;
    }

    /// <summary>
    /// 重新绑定子节点的属性
    /// </summary>
    public void RebindValuesProperty(int startIndex, int endIndex = -1) {
        if (values == null || serializedProperty == null) return;
        if (endIndex == -1) {
            endIndex = values.Count - 1;
        }
        SerializedProperty arrayProperty = valuesProperty;
        for (int index = startIndex; index <= endIndex; index++) {
            Variable nestedVar = values[index];
            nestedVar?.BindProperty(arrayProperty.GetArrayElementAtIndex(index));
        }
    }

    /// <summary>
    /// 重新绑定子节点的属性
    /// </summary>
    public void RebindValuesProperty() {
        if (values == null || serializedProperty == null) return;
        SerializedProperty arrayProperty = valuesProperty;
        for (int index = 0; index < values.Count; index++) {
            Variable nestedVar = values[index];
            nestedVar?.BindProperty(arrayProperty.GetArrayElementAtIndex(index));
        }
    }

    /// <summary>
    /// 解绑子节点属性
    /// </summary>
    public void UnbindValuesProperty() {
        if (values == null || serializedProperty == null) return;
        foreach (Variable nestedVar in values) {
            nestedVar?.UnbindProperty();
        }
    }

    /// <summary>
    /// 销毁对象
    /// </summary>
    public void Dispose() {
        if (values != null) {
            foreach (Variable nestedVar in values) {
                nestedVar?.Dispose();
            }
        }
        UnbindPropertySelf();
        defineInfo = null;
        cfg = null;
        type = null;
        portView = null;
    }

    /// <summary>
    /// 是否已销毁(方便它处判断Variable的有效性)
    /// </summary>
    public bool isDisposed => defineInfo == null;

    public Variable FindValue(string name) {
        if (values == null) return null;
        // TODO 支持路径表达式
        foreach (Variable nestedVar in values) {
            if (nestedVar == null) continue;
            if (nestedVar.defineInfo.SimpleName == name) {
                return nestedVar;
            }
        }
        return null;
    }

    /// <summary>
    /// 是否处于展开状态
    /// </summary>
    public bool isExpanded { get; set; }
    /// <summary>
    /// 编辑器使用的缓存数据，通过该字段可以让Drawer总是保持为无（可变）状态的。
    /// </summary>
    public object editorState { get; set; }

    public T GetEditorState<T>() where T : new() {
        if (editorState == null) {
            editorState = new T();
        }
        return (T)editorState;
    }

    #endregion
}
}