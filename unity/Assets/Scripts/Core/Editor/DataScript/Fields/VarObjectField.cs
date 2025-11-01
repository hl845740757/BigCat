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
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Wjybxx.BigCatTool.DataScript;

namespace Wjybxx.BigCat.CoreEditor.DataScript
{
/// <summary>
/// 通用Object字段布局
///
/// 注：为保持风格统一，ObjectField也不使用ToolbarMenu实现菜单栏。
/// </summary>
public class VarObjectField : Foldout, IVarField
{
    private DataGraphEditor editor { get; set; }

    public VarObjectField() {
    }

    public string label {
        get => text;
        set => text = value;
    }

    /// <summary>
    /// 刷新View
    /// </summary>
    public void Refresh() {
        Variable variable = (Variable)userData;
        if (variable == null) return;
        if (variable.isNull) {
            contentContainer.SetEnabled(false);
            return;
        }
        contentContainer.SetEnabled(true);
        VisualElement container = contentContainer;
        for (int index = 0; index < container.childCount; index++) {
            VisualElement fieldView = container[index];
            Variable nestedVar = variable.values[index];
            if (!nestedVar.cfg.HasBranchCfg) {
                DataEditorUtil.Refresh(fieldView);
                continue;
            }
            // 刷新标签类字段的可见性
            var branchCfg = DataEditorUtil.FilterBranchCfg(variable, nestedVar.cfg.branchCfgs);
            if (branchCfg == null) {
                fieldView.SetDisplay(false);
                continue;
            }
            fieldView.SetDisplay(true);
            fieldView.tooltip = branchCfg.tooltip;
            DataEditorUtil.SetFieldLabel(fieldView, branchCfg.displayName);
            DataEditorUtil.Refresh(fieldView);
        }
    }

    /// <summary>
    /// 绑定数据后调用
    /// </summary>
    public void Bind(DataGraphEditor editor, Variable variable) {
        this.editor = editor;
        this.userData = variable;
        // 递归创建字段
        foreach (Variable nestedVar in variable.values) {
            VisualElement fieldView = DataEditorUtil.CreateField(nestedVar, editor);
            DataEditorUtil.SetFieldLabel(fieldView, nestedVar.defineInfo.SimpleName);
            contentContainer.Add(fieldView);
        }
        // 需要监听Ctrl字段变更
        HashSet<VisualElement> ctrlFields = null;
        VisualElement container = contentContainer;
        foreach (Variable nestedVar in variable.values) {
            if (!nestedVar.cfg.HasBranchCfg) {
                continue;
            }
            ctrlFields ??= new HashSet<VisualElement>(4);
            foreach (FieldBranchCfg branchCfg in nestedVar.cfg.branchCfgs) {
                VisualElement ctrlField = container[branchCfg.ctrlIndex];
                if (!ctrlFields.Add(ctrlField)) { // 已监听
                    continue;
                }
                if (ctrlField is IntegerField integerField) {
                    integerField.RegisterValueChangedCallback(_ => Refresh());
                } else if (ctrlField is PopupField<int> popupIntField) {
                    popupIntField.RegisterValueChangedCallback(_ => Refresh());
                } else if (ctrlField is PopupField<string> popupStringField) {
                    popupStringField.RegisterValueChangedCallback(_ => Refresh());
                } else if (ctrlField is TextField textField) {
                    textField.RegisterValueChangedCallback(_ => Refresh());
                }
            }
        }
        RegisterCallback<ContextClickEvent>(ShowContextMenu);
        // 刷新UI
        Refresh();
    }

    private void ShowContextMenu(ContextClickEvent evt) {
        evt.StopPropagation();
        if (evt.localMousePosition.y > 20) return; // 只检测顶部区域
        if (editor == null) return;

        Variable variable = (Variable)userData;
        GenericMenu menu = new GenericMenu();
        // SetNull
        if (variable.isNull) {
            menu.AddDisabledItem(new GUIContent("SetNull"), true);
            menu.AddItem(new GUIContent("SetNotNull"), false, OnClickSetNotNull, null);
        } else {
            menu.AddItem(new GUIContent("SetNull"), false, OnClickSetNull, null);
            menu.AddDisabledItem(new GUIContent("SetNotNull"), true);
        }
        // Copy/Paste
        if (variable.isNull) {
            menu.AddDisabledItem(new GUIContent("Copy"));
        } else {
            menu.AddItem(new GUIContent("Copy"), false, OnClickCopy, null);
        }
        if (DataEditorUtil.IsPastable(GUIUtility.systemCopyBuffer)) {
            menu.AddItem(new GUIContent("Paste"), false, OnClickPaste, null);
        } else {
            menu.AddDisabledItem(new GUIContent("Paste"));
        }
        // Reset
        menu.AddItem(new GUIContent("Reset"), false, OnClickReset, null);

        // 实例选择
        VariableCfg variableCfg = variable.cfg;
        if (variableCfg.HasSupportedInsts) {
            GenericMenu.MenuFunction2 callback = OnClickResetWith;
            for (int index = 0; index < variableCfg.supportedInsts.Count; index++) {
                DSInst inst = variableCfg.supportedInsts[index];
                int spIndex = inst.SimpleName.LastIndexOf('/');
                string label = spIndex > 0 ? inst.SimpleName.Substring(spIndex + 1) : inst.SimpleName;
                label = "ResetWith/" + label;
                menu.AddItem(new GUIContent(label), false, callback, index);
            }
        } else {
            menu.AddDisabledItem(new GUIContent("ResetWith"));
        }
        // 多态类型选择
        if (variableCfg.HasSupportedTypes) {
            GenericMenu.MenuFunction2 callback = OnClickChangeType;
            for (int index = 0; index < variableCfg.supportedTypes.Count; index++) {
                string label = variableCfg.supportedTypes[index];
                label = "ChangeType/" + label;
                menu.AddItem(new GUIContent(label), false, callback, index);
            }
        } else {
            menu.AddDisabledItem(new GUIContent("ChangeType"));
        }
        menu.ShowAsContext();
    }

    private void OnClickSetNull(object _) {
        Variable variable = (Variable)userData;
        variable.isNull = true;
        variable.ApplyModifiedProperties();
        Refresh();
    }

    private void OnClickSetNotNull(object _) {
        Variable variable = (Variable)userData;
        variable.isNull = false;
        variable.ApplyModifiedProperties();
        Refresh();
    }

    private void OnClickCopy(object _) {
        Variable variable = (Variable)userData;
        DataEditorUtil.DoCopy(variable, editor.model);
        variable.ApplyModifiedProperties();
        Refresh();
    }

    private void OnClickPaste(object _) {
        Variable variable = (Variable)userData;
        DataEditorUtil.DoPaste(variable, editor.model);
        variable.ApplyModifiedProperties();
        Refresh();
    }

    private void OnClickReset(object _) {
        Variable variable = (Variable)userData;
        editor.model.ResetVariable(variable);
        variable.ApplyModifiedProperties();
        Refresh();
    }

    private void OnClickResetWith(object obj) {
        int index = (int)obj;
        Variable variable = (Variable)userData;
        DSInst inst = variable.cfg.supportedInsts[index];
        editor.model.ResetVariable(variable, inst.DsonValue);
        variable.ApplyModifiedProperties();
        Refresh();
    }

    private void OnClickChangeType(object obj) {
        int index = (int)obj;
        Variable variable = (Variable)userData;
        string typeSymbol = variable.cfg.supportedTypes[index];
        //
        DSNamedType namedType = editor.model.repository.ResolveTypeSymbol(null, typeSymbol) as DSNamedType;
        editor.model.ChangeVariableType(variable, namedType);
        variable.ApplyModifiedProperties();
        Refresh(); // 需要刷新port
    }
}
}