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
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Wjybxx.BigCatTool.DataScript;
using Wjybxx.Commons;
using Wjybxx.BigCat.Core;
using Wjybxx.Commons.Collections;
using static Wjybxx.BigCat.CoreEditor.DataEditorUtil;

namespace Wjybxx.BigCat.CoreEditor
{
/// <summary>
///
/// 虽然<see cref="ReorderableList"/>可以提供拖拽排序功能，但需要手动计算布局，非常麻烦，因此我们通过功能按钮提供排序支持。
/// 本想通过快捷键快速移动的，但焦点很难保持在目标元素上；最后通过鼠标右键选择移动目标点，发现还挺方便，因为可以跨滚动条移动...
/// </summary>
internal class ListVariableDrawer : DataVariableDrawer
{
    private readonly GUILayoutOption[] _width20 = new[] { GUILayout.Width(20) };
    private readonly GUILayoutOption[] _noExpand = new[] { GUILayout.ExpandHeight(false) };
    private readonly GUIContent _emptyLabel = new GUIContent("List Is Empty");

    public override void OnGUI(DataEditor editor, DataVariable variable, GUIContent label) {
        EditorGUILayout.BeginHorizontal();
        variable.isExpanded = EditorGUILayout.Foldout(variable.isExpanded, label);

        const string controlName = "_count";
        GUI.SetNextControlName(controlName);
        // 列表长度
        int listCount = variable.values.Count;
        int count = Math.Max(0, EditorGUILayout.DelayedIntField(listCount));
        if (count != listCount && GUI.GetNameOfFocusedControl() == controlName) {
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
            for (int index = 0; index < variable.values.Count; index++) {
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
        DataVariable value = variable.values[index];
        GUIContent label = editor.labelPool.Acquire();

        editor.DrawVariable(value, label.WithText(UnityEditorUtil.GetElementName(index)));
        editor.labelPool.Release(label); // label已回收

        Event evt = Event.current; // 这里无法监听到左键事件 - 被内部控件消耗
        if (IsContextClickEvent(evt, GUILayoutUtility.GetLastRect())) {
            ShowContextMenu(editor, variable, index);
            evt.Use();
        }
    }

    private static void OnClickEnter(DataEditor editor, DataVariable variable, int count) {
        int listCount = variable.values.Count;
        if (count == listCount) {
            return;
        }
        if (count < listCount) {
            int deleteCount = listCount - count;
            variable.values.RemoveRange(count, deleteCount);
            return;
        }
        int addCount = count - listCount;
        if (addCount > 200) {
            Debug.LogError("Adding too many elements at once");
            return;
        }
        variable.values.EnsureCapacity(count);
        if (listCount > 0) {
            // 最后一个拷贝N次 - 批量Duplicate以减少中间对象
            DataVariable lastValue = variable.values[listCount - 1];
            editor.model.Duplicate(lastValue, addCount, variable.values);
        } else {
            // 简单创建N个
            DSField valuesField = variable.type.GetField("values");
            DataDisplayCfg elementCfg = variable.displayCfg.elementCfg;
            for (int i = 0; i < addCount; i++) {
                DataVariable newValue = editor.model.CreateVariable(valuesField, elementCfg);
                variable.values.Add(newValue);
            }
        }
    }

    private static void OnClickAdd(DataEditor editor, DataVariable variable) {
        DataVariable newValue;
        if (variable.values.TryPeekLast(out DataVariable lastValue)) {
            newValue = editor.model.Duplicate(lastValue);
        } else {
            DSField valuesField = variable.type.GetField("values");
            DataDisplayCfg elementCfg = variable.displayCfg.elementCfg;
            newValue = editor.model.CreateVariable(valuesField, elementCfg);
        }
        variable.values.Add(newValue);
    }

    private static void OnClickDelete(DataEditor editor, DataVariable variable) {
        variable.values.TryRemoveLast(out _);
    }

    #region menu

    private static void ShowContextMenu(DataEditor editor, DataVariable variable, int index) {
        // 创建Menu不能使用池化的Label
        EditorState editorState = variable.GetEditorState<EditorState>();
        GenericMenu menu = new GenericMenu();
        if (editorState.moveIndex != -1) {
            menu.AddDisabledItem(new GUIContent($"index: {index}, moving: {editorState.moveIndex}"));
        } else {
            menu.AddDisabledItem(new GUIContent($"index: {index}, moving: -1"));
        }
        menu.AddSeparator("");
        //
        MenuContext context = new MenuContext(editor, variable, index);
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
        if (moveIndex < variable.values.Count) {
            DataVariable value = variable.values[moveIndex];
            variable.values.RemoveAt(moveIndex);
            variable.values.Insert(index, value);
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

        variable.values.RemoveAt(index);
    }

    private static void OnClickDuplicate(object obj) {
        MenuContext context = (MenuContext)obj;
        DataVariable variable = context.variable;
        DataEditor editor = context.editor;
        int index = context.index;
        ClearMoveIndex(variable);

        DataVariable srcValue = variable.values[index];
        DataVariable copiedValue = editor.model.Duplicate(srcValue);
        variable.values.Insert(index + 1, copiedValue);
    }

    private static void OnClickMoveUp(object obj) {
        MenuContext context = (MenuContext)obj;
        DataVariable variable = context.variable;
        int index = context.index;
        ClearMoveIndex(variable);

        if (index == 0) {
            return;
        }
        variable.values.Swap(index, index - 1);
    }

    private static void OnClickMoveDown(object obj) {
        MenuContext context = (MenuContext)obj;
        DataVariable variable = context.variable;
        int index = context.index;
        ClearMoveIndex(variable);

        if (index == variable.values.Count - 1) {
            return;
        }
        variable.values.Swap(index, index + 1);
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
        public int moveIndex = -1;
        public int selected = -1;
    }
}
}