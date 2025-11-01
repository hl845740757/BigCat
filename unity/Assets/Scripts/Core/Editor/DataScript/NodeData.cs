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
using UnityEngine.UIElements;
using Wjybxx.Commons;
using Wjybxx.Dson;

namespace Wjybxx.BigCat.CoreEditor.DataScript
{
/// <summary>
/// 数据节点
///
/// 注：每个节点一个UnityObject，可以避免增删节点导致大量的属性绑定失效。
/// </summary>
public sealed class NodeData : ScriptableObject
{
    /// <summary>
    /// 对象归属的文件（分组）（可选）
    ///
    /// 注：如果folder不为空.外部通过name引用node时，应当通过<code>folder/name</code>的方式引用。
    /// </summary>
    [SerializeField] private string _folder;
    /// <summary>
    /// 文件内的唯一id，程序分配
    ///
    /// 注：当外部引用Node时，应当优先使用name进行引用，更具有稳定性。
    /// </summary>
    [SerializeField] private long _localId;
    /// <summary>
    /// 节点注释
    ///
    /// 注：避免过长的注释，顶层节点的注释会写入输出数据。
    /// </summary>
    [SerializeField] private string _comment;
    /// <summary>
    /// 数据的值
    /// 
    /// 注：顶层Node在导出数据时都会写入类型信息。
    /// </summary>
    public Variable value;

    /// <summary>
    /// 关联的视图
    ///
    /// 注：如果不在Graph视图展示，则可能为null
    /// </summary>
    [NonSerialized] public NodeView nodeView;
    /// <summary>
    /// graphView的坐标，需要支持undo
    /// </summary>
    [SerializeField] private Vector2 _position;

    /// <summary>
    /// Node上的input信息(保存的是发起端的变量)
    /// </summary>
    [NonSerialized] public readonly List<Variable> inputFields = new List<Variable>();
    /// <summary>
    /// Node上的output字段
    /// </summary>
    [NonSerialized] public readonly List<Variable> outputFields = new List<Variable>();

    /// <summary>
    /// 是否是纯粹的内存节点（不需要序列化保存）
    ///
    /// 注：该属性在初始化完成以后不应该再变更，否则可能导致错误。
    /// </summary>
    public bool isMemoryNode { get; set; }
    /// <summary>
    /// 是否启用端口（对象图）（场景中数据通常不启用）
    ///
    /// 注：该属性在初始化完成以后不应该再变更，否则可能导致错误。
    /// </summary>
    public bool enablePort { get; set; }
    /// <summary>
    /// 在Update队列中的索引
    /// </summary>
    [NonSerialized] internal int qIndex = -1;

    /// <summary>
    /// 关联的序列化对象
    ///
    /// 注：在首次完成反序列化时，应当执行<see cref="SerializedObject.ApplyModifiedPropertiesWithoutUndo"/>
    /// </summary>
    public SerializedObject serializedObject { get; private set; }
    public SerializedProperty positionProperty { get; private set; }
    public SerializedProperty valueProperty { get; private set; }

    #region util

    private void Awake() {
        serializedObject = new SerializedObject(this);
        positionProperty = this.serializedObject.FindProperty("_position");
        valueProperty = this.serializedObject.FindProperty("value");
    }

    public string folder {
        get => _folder;
        internal set {
            using SerializedProperty property = serializedObject.FindProperty("_folder");
            property.stringValue = value;
        }
    }

    public long localId {
        get => _localId;
        internal set {
            using SerializedProperty property = serializedObject.FindProperty("_localId");
            property.longValue = value;
        }
    }

    public string comment {
        get => _comment;
        set {
            using SerializedProperty property = serializedObject.FindProperty("_comment");
            property.stringValue = value;
        }
    }

    public Vector2 position {
        get => _position;
        set => positionProperty.vector2Value = value;
    }

    public void ApplyModifiedProperties() {
        serializedObject.ApplyModifiedProperties();
    }

    public new void SetDirty() {
        // serializedObject.Update();
        EditorUtility.SetDirty(serializedObject.targetObject);
    }

    public void RebindValueProperty() {
        value?.BindProperty(valueProperty);
    }

    #endregion
}
}