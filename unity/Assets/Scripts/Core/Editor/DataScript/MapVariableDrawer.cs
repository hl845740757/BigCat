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
using Wjybxx.Commons.Collections;
using static Wjybxx.BigCat.CoreEditor.DataEditorUtil;

namespace Wjybxx.BigCat.CoreEditor
{
/// <summary>
///
/// </summary>
internal class MapVariableDrawer : DataVariableDrawer
{
    private readonly GUILayoutOption[] _width20 = new[] { GUILayout.Width(20) };
    private readonly GUILayoutOption[] _noExpand = new[] { GUILayout.ExpandHeight(false) };
    private readonly GUIContent _emptyLabel = new GUIContent("Map Is Empty");

    public override void OnGUI(DataEditor editor, DataVariable variable, GUIContent label) {
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
        DataDisplayCfg displayCfg = variable.displayCfg;
        // DrawElements
        Rect rect = EditorGUILayout.BeginVertical();
        int indentLevel = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 1;
        if (variable.values.Count == 0) {
            EditorGUILayout.HelpBox(_emptyLabel);
        } else {
            if (displayCfg.scrollView) {
                editorState.scrollPos = EditorGUILayout.BeginScrollView(editorState.scrollPos, _noExpand);
            }
            for (int index = 0; index < variable.values.Count; index += 2) {
                if (index > 0) EditorGUILayout.Space(2);
                DrawElement(editor, variable, index);
            }
            if (displayCfg.scrollView) {
                EditorGUILayout.EndScrollView();
            }
        }
        EditorGUI.indentLevel = indentLevel;
        EditorGUILayout.EndVertical();

        // 清理移动数据
        if (IsPrimaryClickEvent(Event.current) && !rect.Contains(Event.current.mousePosition)) {
            editorState.moveIndex = -1;
        }
    }

    private void DrawElement(DataEditor editor, DataVariable variable, int index) {
        DataVariable varKey = variable.values[index];
        DataVariable varValue = variable.values[index + 1];
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

    private static void OnClickEnter(DataEditor editor, DataVariable variable, int count) {
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
            DataVariable lastKey = variable.values[listCount - 2];
            DataVariable lastValue = variable.values[listCount - 1];
            //
            List<DataVariable> keyList = new List<DataVariable>(addCount);
            List<DataVariable> valueList = new List<DataVariable>(addCount);
            editor.model.Duplicate(lastKey, addCount, keyList);
            editor.model.Duplicate(lastValue, addCount, valueList);
            //
            for (int i = 0; i < addCount; i++) {
                DataVariable newKey = keyList[i];
                DataVariable newValue = valueList[i];
                variable.values.Add(newKey);
                variable.values.Add(newValue);
            }
        } else {
            // 简单创建N个
            DSField keysField = variable.type.GetField("keys");
            DSField valuesField = variable.type.GetField("values");
            DataDisplayCfg elementCfg = variable.displayCfg.elementCfg;
            //
            for (int i = 0; i < addCount; i++) {
                DataVariable newKey = editor.model.CreateVariable(keysField); // key不应用集合的设置
                DataVariable newValue = editor.model.CreateVariable(valuesField, elementCfg);
                variable.values.Add(newKey);
                variable.values.Add(newValue);
            }
        }
    }

    private static void OnClickAdd(DataEditor editor, DataVariable variable) {
        DataVariable newKey;
        DataVariable newValue;
        int listCount = variable.values.Count;
        if (listCount > 0) {
            DataVariable lastKey = variable.values[listCount - 2];
            DataVariable lastValue = variable.values[listCount - 1];
            newKey = editor.model.Duplicate(lastKey);
            newValue = editor.model.Duplicate(lastValue);
        } else {
            DSField keysField = variable.type.GetField("keys");
            DSField valuesField = variable.type.GetField("values");
            DataDisplayCfg elementCfg = variable.displayCfg.elementCfg;
            newKey = editor.model.CreateVariable(keysField);
            newValue = editor.model.CreateVariable(valuesField, elementCfg);
        }
        variable.values.Add(newKey);
        variable.values.Add(newValue);
    }

    private static void OnClickDelete(DataEditor editor, DataVariable variable) {
        int listCount = variable.values.Count;
        if (listCount > 0) {
            variable.values.RemoveAt(listCount - 1);
            variable.values.RemoveAt(listCount - 2);
        }
    }

    #region menu

    private static void ShowContextMenu(DataEditor editor, DataVariable variable, int pairIndex) {
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
        DataVariable variable = context.variable;
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
            DataVariable key = variable.values[keyIndex];
            DataVariable value = variable.values[keyIndex + 1];

            variable.values.RemoveAt(keyIndex); // key
            variable.values.RemoveAt(keyIndex); // value

            variable.values.Insert(index * 2, key);
            variable.values.Insert(index * 2 + 1, value);
        }
    }

    private static void OnClickMoveTo(object obj) {
        MenuContext context = (MenuContext)obj;
        DataVariable variable = context.variable;
        int index = context.index;

        EditorState editorState = variable.GetEditorState<EditorState>();
        editorState.moveIndex = index;
    }

    private static void ClearMoveIndex(DataVariable variable) {
        EditorState editorState = variable.GetEditorState<EditorState>();
        editorState.moveIndex = -1;
    }

    private static void OnClickDelete(object obj) {
        MenuContext context = (MenuContext)obj;
        DataVariable variable = context.variable;
        int index = context.index;
        ClearMoveIndex(variable);

        int keyIndex = index * 2;
        variable.values.RemoveAt(keyIndex); // key
        variable.values.RemoveAt(keyIndex); // value
    }

    private static void OnClickDuplicate(object obj) {
        MenuContext context = (MenuContext)obj;
        DataVariable variable = context.variable;
        DataEditor editor = context.editor;
        int index = context.index;
        ClearMoveIndex(variable);

        int keyIndex = index * 2;
        DataVariable srcKey = variable.values[keyIndex];
        DataVariable srcValue = variable.values[keyIndex + 1];
        DataVariable copiedKey = editor.model.Duplicate(srcKey);
        DataVariable copiedValue = editor.model.Duplicate(srcValue);
        variable.values.Insert(keyIndex + 2, copiedKey); // 插在目标Key后面
        variable.values.Insert(keyIndex + 3, copiedValue);
    }

    private static void OnClickMoveUp(object obj) {
        MenuContext context = (MenuContext)obj;
        DataVariable variable = context.variable;
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
        DataVariable variable = context.variable;
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
        public readonly DataVariable variable;
        public readonly int index;

        public MenuContext(DataEditor editor, DataVariable variable, int index) {
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