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
using Wjybxx.BigCatTool.DataScript;
using Wjybxx.Commons;
using Wjybxx.BigCat.Core;
using Wjybxx.Commons.Collections;
using static Wjybxx.BigCat.CoreEditor.DataScript.DataEditorUtil;

namespace Wjybxx.BigCat.CoreEditor.DataScript
{
/// <summary>
///
/// </summary>
internal class MapVariableDrawer : DataVariableDrawer
{
    private readonly GUILayoutOption[] _width20 = new[] { GUILayout.Width(20) };
    private readonly GUILayoutOption[] _noExpand = new[] { GUILayout.ExpandHeight(false) };
    private readonly GUIContent _emptyLabel = new GUIContent("Map Is Empty");

    public override void OnGUI(DataEditor editor, Variable variable, GUIContent label) {
        EditorGUILayout.BeginHorizontal();
        variable.isExpanded = EditorGUILayout.Foldout(variable.isExpanded, label);

        const string controlName = "_count";
        GUI.SetNextControlName(controlName);
        // 字典长度 - 需要除以2
        int mapCount = variable.values.Count / 2;
        int count = Math.Max(0, EditorGUILayout.DelayedIntField(mapCount));
        if (count != mapCount && GUI.GetNameOfFocusedControl() == controlName) {
            variable.isExpanded = true;
            OnClickEnter(editor, variable, count);
        }
        if (GUILayout.Button("+", _width20)) {
            OnClickAdd(editor, variable);
        }
        if (GUILayout.Button("-", _width20)) {
            OnClickDelete(editor, variable);
        }
        EditorGUILayout.EndHorizontal();
        if (!variable.isExpanded) {
            return;
        }
        EditorState editorState = variable.GetEditorState<EditorState>();
        VariableCfg displayCfg = variable.cfg;
        // DrawElements
        Rect rect = EditorGUILayout.BeginVertical();
        int indentLevel = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 1;
        if (variable.values.Count == 0) {
            EditorGUILayout.HelpBox(_emptyLabel);
        } else {
            for (int index = 0; index < variable.values.Count; index += 2) {
                if (index > 0) EditorGUILayout.Space(2);
                DrawElement(editor, variable, index);
            }
        }
        EditorGUI.indentLevel = indentLevel;
        EditorGUILayout.EndVertical();

        // 清理移动数据
        if (IsPrimaryClickEvent(Event.current) && !rect.Contains(Event.current.mousePosition)) {
            editorState.moveIndex = -1;
        }
    }

    private void DrawElement(DataEditor editor, Variable variable, int index) {
        Variable varKey = variable.values[index];
        Variable varValue = variable.values[index + 1];
        GUIContent label = editor.labelPool.Acquire();

        int pairIndex = index / 2;
        Rect pairRect = EditorGUILayout.BeginVertical();
        editor.DrawVariable(varKey, label.WithText(GetKeyName(pairIndex)));
        editor.DrawVariable(varValue, label.Reset().WithText(GetValueName(pairIndex)));
        EditorGUILayout.EndVertical();
        editor.labelPool.Release(label); // label已回收

        Event evt = Event.current; // 这里无法监听到左键事件 - 被内部控件消耗
        if (IsContextClickEvent(evt, pairRect)) {
            ShowContextMenu(editor, variable, pairIndex);
            evt.Use();
        }
    }

    private static void OnClickEnter(DataEditor editor, Variable variable, int count) {
        int listCount = variable.values.Count;
        int mapCount = listCount / 2;
        if (count == mapCount) {
            return;
        }
        if (count < mapCount) {
            int deleteCount = mapCount - count;
            variable.values.RemoveRange(count * 2, deleteCount * 2);
            return;
        }
        int addCount = count - mapCount;
        if (addCount > 200) {
            Debug.LogError("Adding too many elements at once");
            return;
        }
        variable.values.EnsureCapacity(count);
        if (mapCount > 0) {
            // 最后一个拷贝N次 - 批量Duplicate以减少中间对象
            Variable lastKey = variable.values[listCount - 2];
            Variable lastValue = variable.values[listCount - 1];
            //
            List<Variable> keyList = new List<Variable>(addCount);
            List<Variable> valueList = new List<Variable>(addCount);
            editor.model.Duplicate(lastKey, addCount, keyList);
            editor.model.Duplicate(lastValue, addCount, valueList);
            //
            for (int i = 0; i < addCount; i++) {
                Variable newKey = keyList[i];
                Variable newValue = valueList[i];
                variable.values.Add(newKey);
                variable.values.Add(newValue);
            }
        } else {
            // 简单创建N个
            DSField keysField = variable.type.GetField("keys");
            DSField valuesField = variable.type.GetField("values");
            VariableCfg elementCfg = variable.cfg.elementCfg;
            //
            for (int i = 0; i < addCount; i++) {
                Variable newKey = editor.model.CreateVariable(keysField); // key不应用集合的设置
                Variable newValue = editor.model.CreateVariable(valuesField, elementCfg);
                variable.values.Add(newKey);
                variable.values.Add(newValue);
            }
        }
    }

    private static void OnClickAdd(DataEditor editor, Variable variable) {
        Variable newKey;
        Variable newValue;
        int listCount = variable.values.Count;
        if (listCount > 0) {
            Variable lastKey = variable.values[listCount - 2];
            Variable lastValue = variable.values[listCount - 1];
            newKey = editor.model.Duplicate(lastKey);
            newValue = editor.model.Duplicate(lastValue);
        } else {
            DSField keysField = variable.type.GetField("keys");
            DSField valuesField = variable.type.GetField("values");
            VariableCfg elementCfg = variable.cfg.elementCfg;
            newKey = editor.model.CreateVariable(keysField);
            newValue = editor.model.CreateVariable(valuesField, elementCfg);
        }
        variable.values.Add(newKey);
        variable.values.Add(newValue);
    }

    private static void OnClickDelete(DataEditor editor, Variable variable) {
        int listCount = variable.values.Count;
        if (listCount > 0) {
            variable.values.RemoveAt(listCount - 1);
            variable.values.RemoveAt(listCount - 2);
        }
    }

    #region menu

    private static void ShowContextMenu(DataEditor editor, Variable variable, int pairIndex) {
        // 创建Menu不能使用池化的Label
        EditorState editorState = variable.GetEditorState<EditorState>();
        GenericMenu menu = new GenericMenu();
        if (editorState.moveIndex != -1) {
            menu.AddDisabledItem(new GUIContent($"index: {pairIndex}, moving: {editorState.moveIndex}"));
        } else {
            menu.AddDisabledItem(new GUIContent($"index: {pairIndex}, moving: -1"));
        }
        menu.AddSeparator("");
        //
        MenuContext context = new MenuContext(editor, variable, pairIndex);
        menu.AddItem(new GUIContent("Delete"), false, OnClickDelete, context);
        menu.AddItem(new GUIContent("Duplicate"), false, OnClickDuplicate, context);
        menu.AddItem(new GUIContent("MoveUp"), false, OnClickMoveUp, context);
        menu.AddItem(new GUIContent("MoveDown"), false, OnClickMoveDown, context);
        // 快速移动
        menu.AddItem(new GUIContent("Moving"), false, OnClickMoveTo, context);
        if (editorState.moveIndex != -1) {
            menu.AddItem(new GUIContent("MoveHere"), false, OnClickMoveHere, context);
        } else {
            menu.AddDisabledItem(new GUIContent("MoveHere"));
        }
        menu.ShowAsContext();
    }

    private static void OnClickMoveHere(object obj) {
        MenuContext context = (MenuContext)obj;
        Variable variable = context.variable;
        int index = context.index;

        EditorState editorState = variable.GetEditorState<EditorState>();
        int moveIndex = editorState.moveIndex;
        editorState.moveIndex = -1;
        //
        if (moveIndex == -1 || moveIndex == index) {
            return;
        }
        int keyIndex = moveIndex * 2;
        if (keyIndex < variable.values.Count) {
            Variable key = variable.values[keyIndex];
            Variable value = variable.values[keyIndex + 1];

            variable.values.RemoveAt(keyIndex); // key
            variable.values.RemoveAt(keyIndex); // value

            variable.values.Insert(index * 2, key);
            variable.values.Insert(index * 2 + 1, value);
        }
    }

    private static void OnClickMoveTo(object obj) {
        MenuContext context = (MenuContext)obj;
        Variable variable = context.variable;
        int index = context.index;

        EditorState editorState = variable.GetEditorState<EditorState>();
        editorState.moveIndex = index;
    }

    private static void ClearMoveIndex(Variable variable) {
        EditorState editorState = variable.GetEditorState<EditorState>();
        editorState.moveIndex = -1;
    }

    private static void OnClickDelete(object obj) {
        MenuContext context = (MenuContext)obj;
        Variable variable = context.variable;
        int index = context.index;
        ClearMoveIndex(variable);

        int keyIndex = index * 2;
        variable.values.RemoveAt(keyIndex); // key
        variable.values.RemoveAt(keyIndex); // value
    }

    private static void OnClickDuplicate(object obj) {
        MenuContext context = (MenuContext)obj;
        Variable variable = context.variable;
        DataEditor editor = context.editor;
        int index = context.index;
        ClearMoveIndex(variable);

        int keyIndex = index * 2;
        Variable srcKey = variable.values[keyIndex];
        Variable srcValue = variable.values[keyIndex + 1];
        Variable copiedKey = editor.model.Duplicate(srcKey);
        Variable copiedValue = editor.model.Duplicate(srcValue);
        variable.values.Insert(keyIndex + 2, copiedKey); // 插在目标Key后面
        variable.values.Insert(keyIndex + 3, copiedValue);
    }

    private static void OnClickMoveUp(object obj) {
        MenuContext context = (MenuContext)obj;
        Variable variable = context.variable;
        int index = context.index;
        ClearMoveIndex(variable);

        int keyIndex = index * 2;
        if (keyIndex == 0) {
            return;
        }
        variable.values.Swap(keyIndex, keyIndex - 2);
        variable.values.Swap(keyIndex + 1, keyIndex - 1);
    }

    private static void OnClickMoveDown(object obj) {
        MenuContext context = (MenuContext)obj;
        Variable variable = context.variable;
        int index = context.index;
        ClearMoveIndex(variable);

        int keyIndex = index * 2;
        if (keyIndex + 2 >= variable.values.Count) {
            return;
        }
        variable.values.Swap(keyIndex, keyIndex + 2);
        variable.values.Swap(keyIndex + 1, keyIndex + 3);
    }

    #endregion

    private class MenuContext
    {
        public readonly DataEditor editor;
        public readonly Variable variable;
        public readonly int index;

        public MenuContext(DataEditor editor, Variable variable, int index) {
            this.editor = editor;
            this.variable = variable;
            this.index = index;
        }
    }

    private class EditorState
    {
        public Vector2 scrollPos;
        public int moveIndex = -1; // pairIndex
    }

    /// <summary>
    /// 列表元素的名字缓存，避免频繁构建字符串
    /// </summary>
    private static readonly string[] keyNameCache = new string[100];
    private static readonly string[] valNameCache = new string[100];

    static MapVariableDrawer() {
        for (int index = 0; index < keyNameCache.Length; index++) {
            keyNameCache[index] = "K" + index;
            valNameCache[index] = "V" + index;
        }
    }

    private static string GetKeyName(int index) {
        return index >= 0 && index < keyNameCache.Length ? keyNameCache[index] : "K" + index;
    }

    private static string GetValueName(int index) {
        return index >= 0 && index < valNameCache.Length ? valNameCache[index] : "V" + index;
    }
}
}