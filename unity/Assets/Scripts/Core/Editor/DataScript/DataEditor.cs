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
using System.IO;
using UnityEngine;
using UnityEditor;
using Wjybxx.BigCatTool.DataScript;
using Wjybxx.Commons;
using Wjybxx.BigCat.Core;
using Wjybxx.Commons.Pool;

namespace Wjybxx.BigCat.CoreEditor
{
/// <summary>
/// 通用数据结构编辑器
///
/// 注：GUI应当通过Editor来操作Model，避免直接调用Model中的方法，Editor是View + Controller的结合体。
///
/// <h3>自定义绘制</h3>
/// 暂时没有做自定义变量绘制，因为如果支持<code>DataVariableDrawer</code>
/// 
/// </summary>
public class DataEditor : EditorWindow
{
    public DataEditorModel model { get; private set; }
    private EditorWindow window; // 外层壳编辑器
    private Vector2 fileListScrollPos; // 左侧文件列表滚动条
    private Vector2 nodeListScrollPos; // 左侧Node列表滚动条
    private Vector2 scrollPos; // 中部对象图滚动条
    private Vector2 propScrollPos; // 右侧属性滚动条

    public DataNode currentNode { get; private set; } // 当前绘制的Node
    public DataNode selectedNode { get; private set; } // 当前选中的节点
    private Vector2 dragStartPos; // 拖动的起始位置，Node左上角坐标
    private bool isMouseDown; // 鼠标是否处于按下状态
    private bool isDragging; // Node是否处于拖动状态
    private string tempFileName; // 文件名临时输入

    public readonly ObjectPool<GUIContent> labelPool = new ObjectPool<GUIContent>(
        () => new GUIContent(), content => content.Reset()); // label池

    private readonly Dictionary<DataDisplayType, DataVariableDrawer> drawerMap = new(32);
    private DataNode _node;

    [MenuItem("Window/BigCat/DataEditor")]
    private static void OpenWindow() {
        DataEditor win = GetWindow<DataEditor>("数据编辑器");
        win.minSize = new Vector2(400, 600);
        win.Show();
    }

    private void Awake() {
        drawerMap[DataDisplayType.Int32] = new DataEditorUtil.Int32VariableDrawer();
        drawerMap[DataDisplayType.Int64] = new DataEditorUtil.Int64VariableDrawer();
        drawerMap[DataDisplayType.Float] = new DataEditorUtil.FloatVariableDrawer();
        drawerMap[DataDisplayType.Double] = new DataEditorUtil.DoubleVariableDrawer();
        drawerMap[DataDisplayType.Bool] = new DataEditorUtil.BoolVariableDrawer();
        //
        drawerMap[DataDisplayType.String] = new DataEditorUtil.TextVariableDrawer();
        drawerMap[DataDisplayType.TextArea] = new DataEditorUtil.TextAreaVariableDrawer();
        drawerMap[DataDisplayType.AssetPath] = new DataEditorUtil.AssetPathVariableDrawer();
        //
        drawerMap[DataDisplayType.Enum] = new DataEditorUtil.EnumVariableDrawer();
        drawerMap[DataDisplayType.DateTime] = new DataEditorUtil.DateTimeVariableDrawer();
        drawerMap[DataDisplayType.Timestamp] = new DataEditorUtil.TimestampVariableDrawer();
        drawerMap[DataDisplayType.ObjectPath] = new DataEditorUtil.ObjectPtrVariableDrawer();
        // unity-struct
        drawerMap[DataDisplayType.Vector2] = new DataEditorUtil.Vector2VariableDrawer();
        drawerMap[DataDisplayType.Vector3] = new DataEditorUtil.Vector3VariableDrawer();
        drawerMap[DataDisplayType.Vector4] = new DataEditorUtil.Vector4VariableDrawer();
        drawerMap[DataDisplayType.Vector2Int] = new DataEditorUtil.Vector2IntVariableDrawer();
        drawerMap[DataDisplayType.Vector3Int] = new DataEditorUtil.Vector3IntVariableDrawer();
        drawerMap[DataDisplayType.Color] = new DataEditorUtil.ColorVariableDrawer();
        drawerMap[DataDisplayType.Color32] = new DataEditorUtil.Color32VariableDrawer();
        //
        drawerMap[DataDisplayType.List] = new ListVariableDrawer();
        drawerMap[DataDisplayType.Map] = new MapVariableDrawer();
        drawerMap[DataDisplayType.Default] = new ObjectVariableDrawer();
        drawerMap[DataDisplayType.Nullable] = new NullableVariableDrawer();
    }

    private void OnEnable() {
        // position
        model = CreateInstance<DataEditorModel>();
        string filePath = Application.dataPath + "/Resources/DataScript/data_script.ds";
        DSFile dsFile = DSFileParser.Parse(new FileInfo(filePath));
        model.repository.AddFile(dsFile);
        model.repository.Build();

        Rect rect = new Rect(20, 20, 100, 100);
        _node = model.CreateNode(rect, model.repository.GetType("OuterClass"));
    }

    private void OnDisable() {
        // DestroyImmediate(_node);
    }

    private void OnDestroy() {
        DestroyImmediate(model);
    }

    private void OnGUI() {
        // Rect position = new Rect(0, 0, 400, 1200);
        GUIContent label = new GUIContent();
        EditorGUILayout.BeginVertical();
        propScrollPos = EditorGUILayout.BeginScrollView(propScrollPos);
        _node.value.isExpanded = true;
        DrawVariable(_node.value, label);
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    public void DrawVariable(DataVariable variable, GUIContent label) {
        if (variable.drawer == null) {
            variable.drawer = GetVariableDrawer(variable);
        }
        variable.drawer.OnGUI(this, variable, label);
    }

    private DataVariableDrawer GetVariableDrawer(DataVariable variable) {
        DSNamedType varType = variable.type;
        // List、Map、Nullable字段的DisplayType表示为Value的展示类型；集合字段的注解会拷贝到元素上
        if (DSUtil.IsCollectionType(varType)) {
            return drawerMap[DataDisplayType.List];
        }
        if (DSUtil.IsMapType(varType)) {
            return drawerMap[DataDisplayType.Map];
        }
        if (DSUtil.IsNullableType(varType)) {
            return drawerMap[DataDisplayType.Nullable];
        }
        // 枚举字段固定为EnumPop
        if (varType.Kind == DSElementKind.Enum) {
            return drawerMap[DataDisplayType.Enum];
        }
        // 如果字段指定了展示类型，则使用指定的类型 - 集合元素的信息拷贝自容器
        DataDisplayCfg displayCfg = variable.displayCfg;
        if (displayCfg.HasDisplayType) {
            return drawerMap[displayCfg.displayType];
        }
        // 如果类型指定了展示类型，则使用指定的类型
        DataDisplayCfg typeDisplayCfg = model.GetDisplayCfg(varType);
        if (typeDisplayCfg.HasDisplayType) {
            return drawerMap[typeDisplayCfg.displayType];
        }
        // 根据类型信息推测
        switch (varType.SimpleName) {
            case DSKeywords.TYPE_INT32:
                return drawerMap[DataDisplayType.Int32];
            case DSKeywords.TYPE_INT64:
                return drawerMap[DataDisplayType.Int64];
            case DSKeywords.TYPE_FLOAT:
                return drawerMap[DataDisplayType.Float];
            case DSKeywords.TYPE_DOUBLE:
                return drawerMap[DataDisplayType.Double];
            case DSKeywords.TYPE_BOOL:
                return drawerMap[DataDisplayType.Bool];
            case DSKeywords.TYPE_STRING:
                return drawerMap[DataDisplayType.String];
            case DSKeywords.TYPE_BYTES:
                return drawerMap[DataDisplayType.TextArea];
            case DSKeywords.TYPE_DATETIME:
                return drawerMap[DataDisplayType.DateTime];
            case DSKeywords.TYPE_TIMESTAMP:
                return drawerMap[DataDisplayType.Timestamp];
            case DSKeywords.TYPE_POINTER:
                return drawerMap[DataDisplayType.ObjectPath];
            default:
                // TODO 查找自定义编辑器
                return drawerMap[DataDisplayType.Default];
        }
    }
}
}