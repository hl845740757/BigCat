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
using UnityEngine;
using Wjybxx.BigCatTool.DataScript;
using Wjybxx.Commons;
using Wjybxx.Dson;
using Wjybxx.Dson.Text;
using static Wjybxx.BigCat.CoreEditor.DataEditorUtil;

namespace Wjybxx.BigCat.CoreEditor
{
/// <summary>
/// 默认的数据结构编辑器
/// </summary>
public class ObjectVariableDrawer : DataVariableDrawer
{
    private readonly GUILayoutOption[] _width15 = new[] { GUILayout.Width(15) };
    private readonly GUILayoutOption[] _noExpand = new[] { GUILayout.ExpandHeight(false) };
    private readonly GUIContent _emptyLabel = new GUIContent("Object Is Null");

    public ObjectVariableDrawer() {

    }

    public override void OnGUI(DataEditor editor, DataVariable variable, GUIContent label) {
        // 功能条，标准的显示不出来...
        EditorGUILayout.BeginHorizontal();
        variable.isExpanded = EditorGUILayout.Foldout(variable.isExpanded, label);
        if (GUILayout.Button("┆", EditorStyles.toolbarButton, _width15)) {
            OnClickToolbarButton(editor, variable);
            Event.current.Use();
        }
        EditorGUILayout.EndHorizontal();
        if (!variable.isExpanded) {
            return;
        }

        EditorState editorState = variable.GetEditorState<EditorState>();
        DataDisplayCfg displayCfg = variable.displayCfg;
        //
        EditorGUILayout.BeginVertical();
        int indentLevel = EditorGUI.indentLevel;
        if (variable.defineInfo.Kind == DSElementKind.Field) {
            EditorGUI.indentLevel++;
        }
        if (variable.isNull) {
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
    }

    #region menu

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

    private static void OnClickToolbarButton(DataEditor editor, DataVariable variable) {
        GenericMenu menu = new GenericMenu();
        // Copy/Paste
        if (variable.isNull) {
            menu.AddDisabledItem(new GUIContent("Copy"));
        } else {
            menu.AddItem(new GUIContent("Copy"), false, OnClickCopy, new MenuContext(editor, variable, 0));
        }
        string copyBuffer = GUIUtility.systemCopyBuffer;
        if (MaybeDsonObject(copyBuffer)) {
            menu.AddItem(new GUIContent("Paste"), false, OnClickPaste, new MenuContext(editor, variable, 0));
        } else {
            menu.AddDisabledItem(new GUIContent("Paste"));
        }
        // Create/Reset
        if (variable.isNull) {
            menu.AddItem(new GUIContent("Create"), false, OnClickCreateValues, new MenuContext(editor, variable, 0));
            menu.AddDisabledItem(new GUIContent("SetNull"));
            menu.AddDisabledItem(new GUIContent("Reset"));
        } else {
            menu.AddDisabledItem(new GUIContent("Create"));
            menu.AddItem(new GUIContent("SetNull"), false, OnClickSetNull, new MenuContext(editor, variable, 0));
            menu.AddItem(new GUIContent("Reset"), false, _ => editor.model.ResetVariable(variable), null);
        }
        // 实例选择
        DataDisplayCfg displayCfg = variable.displayCfg;
        if (displayCfg.HasSupportedInsts) {
            GenericMenu.MenuFunction2 callback = OnClickResetWith;
            for (int index = 0; index < displayCfg.supportedInsts.Count; index++) {
                DSInst inst = displayCfg.supportedInsts[index];
                int spIndex = inst.SimpleName.LastIndexOf('/');
                string label = spIndex > 0 ? inst.SimpleName.Substring(spIndex + 1) : inst.SimpleName;
                label = "ResetWith/" + label;
                menu.AddItem(new GUIContent(label), false, callback, new MenuContext(editor, variable, index));
            }
        } else {
            menu.AddDisabledItem(new GUIContent("ResetWith"));
        }
        // 多态类型选择
        if (displayCfg.HasSupportedTypes) {
            GenericMenu.MenuFunction2 callback = OnClickChangeType;
            for (int index = 0; index < displayCfg.supportedTypes.Length; index++) {
                string label = displayCfg.supportedTypes[index];
                label = "ChangeType/" + label;
                menu.AddItem(new GUIContent(label), false, callback, new MenuContext(editor, variable, index));
            }
        }
        menu.ShowAsContext();
    }

    private static bool MaybeDsonObject(string text) {
        if (string.IsNullOrWhiteSpace(text)) {
            return false;
        }
        text = text.Trim();
        if (string.IsNullOrWhiteSpace(text)) {
            return false;
        }
        return text[0] == '{' && text[text.Length - 1] == '}';
    }

    private static void OnClickPaste(object obj) {
        MenuContext context = (MenuContext)obj;
        DataEditor editor = context.editor;
        DataVariable variable = context.variable;

        string copyBuffer = GUIUtility.systemCopyBuffer;
        if (string.IsNullOrWhiteSpace(copyBuffer)) {
            return;
        }
        GUIUtility.systemCopyBuffer = "";
        try {
            DsonValue dsonValue = Dsons.FromDson(copyBuffer);
            editor.model.ResetVariable(variable, dsonValue);
        }
        catch (Exception) {
            Debug.Log("invalid copy buffer: " + copyBuffer);
        }
    }

    private static void OnClickCopy(object obj) {
        MenuContext context = (MenuContext)obj;
        DataEditor editor = context.editor;
        DataVariable variable = context.variable;
        if (variable.isNull) {
            return;
        }
        DsonValue dsonValue = editor.model.Encode(variable);
        GUIUtility.systemCopyBuffer = dsonValue.ToDson(ObjectStyle.Indent, editor.model.writerSettings);
    }

    private static void OnClickCreateValues(object obj) {
        MenuContext context = (MenuContext)obj;
        DataEditor editor = context.editor;
        DataVariable variable = context.variable;
        if (variable.isNull) {
            editor.model.CreateValues(variable);
        }
    }

    private static void OnClickSetNull(object obj) {
        MenuContext context = (MenuContext)obj;
        DataEditor editor = context.editor;
        DataVariable variable = context.variable;
        editor.model.ResetVariable(variable, true);
    }

    private static void OnClickResetWith(object obj) {
        MenuContext context = (MenuContext)obj;
        DataEditor editor = context.editor;
        DataVariable variable = context.variable;
        DSInst inst = variable.displayCfg.supportedInsts[context.index];
        editor.model.ResetVariable(variable, inst.DsonValue);
    }

    private static void OnClickChangeType(object obj) {
        MenuContext context = (MenuContext)obj;
        DataEditor editor = context.editor;
        DataVariable variable = context.variable;
        string typeSymbol = variable.displayCfg.supportedTypes[context.index];
        // 找不到目标类型会抛出异常
        DSNamedType namedType = editor.model.repository.ResolveTypeSymbol(null, typeSymbol) as DSNamedType;
        if (namedType == null) {
            return;
        }
        editor.model.ChangeVariableType(variable, namedType);
    }

    #endregion

    private static void DrawElement(DataEditor editor, DataVariable container, int index) {
        DataVariable value = container.values[index];
        GUIContent label = editor.labelPool.Acquire();

        // 标签字段 - 条件展示
        DataDisplayCfg valueDisplayCfg = value.displayCfg;
        if (valueDisplayCfg.HasBranchCfg) {
            BranchFieldCfg branchCfg = FilterBranchFieldCfg(container, valueDisplayCfg.branchCfgs);
            if (branchCfg == null) {
                return;
            }
            label.WithText(branchCfg.displayName, branchCfg.tooltip);
        } else {
            string displayName = ObjectUtil.BlankToDef(
                valueDisplayCfg.displayName, value.defineInfo.SimpleName);
            label.WithText(displayName, valueDisplayCfg.tooltip);
        }
        editor.DrawVariable(value, label);
        editor.labelPool.Release(label); // label已回收
    }

    private class EditorState
    {
        public Vector2 scrollPos;
        public bool isSelected;
    }
}
}