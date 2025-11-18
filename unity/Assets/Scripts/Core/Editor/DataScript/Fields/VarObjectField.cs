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
using Wjybxx.Commons;
using Wjybxx.Dson;

namespace Wjybxx.BigCat.CoreEditor.DataScript
{
/// <summary>
/// 通用Object字段布局
///
/// TODO 支持自定义Object样式，自定义UXML文件继承该类
/// 注：为保持风格统一，ObjectField也不使用ToolbarMenu实现菜单栏。
/// </summary>
public class VarObjectField : Foldout, IVarField
{
    private DataEditor _editor;
    private Variable _variable;
    private DSNamedType _buildType;
    private EventCallback<ChangeEvent<int>> _onCtrlValueChanged1;
    private EventCallback<ChangeEvent<string>> _onCtrlValueChanged2;

    public VarObjectField() {
        style.flexShrink = 0;
        RegisterCallback<ContextClickEvent>(ShowContextMenu);
    }

    /// <summary>
    /// 不会因解绑而清理
    /// </summary>
    public DSNamedType buildType => _buildType;

    public string label {
        get => text;
        set => text = value;
    }

    /// <summary>
    /// 刷新View
    /// </summary>
    /// <param name="rebuild"></param>
    public void Refresh(bool rebuild = false) {
        Variable variable = _variable;
        if (variable == null) return;
        if (variable.isNull) {
            contentContainer.SetEnabled(false);
            return;
        }
        contentContainer.SetEnabled(true);
        // 延迟初始化
        VisualElement container = contentContainer;
        if (container.childCount == 0 && !ReferenceEquals(_buildType, variable.type)) {
            RebuildFieldViews();
        }
        for (int index = 0; index < container.childCount; index++) {
            VisualElement fieldView = container[index];
            Variable nestedVar = variable[index];
            if (!nestedVar.cfg.HasBranchCfg) {
                DataEditorUtil.Bind(fieldView, nestedVar, _editor);
                continue;
            }
            // 刷新标签类字段的可见性
            var branchCfg = FilterBranchCfg(variable, nestedVar.cfg.branchCfgs);
            if (branchCfg == null) {
                fieldView.SetDisplay(false);
                continue;
            }
            fieldView.SetDisplay(true);
            fieldView.tooltip = branchCfg.tooltip;
            DataEditorUtil.SetFieldLabel(fieldView, branchCfg.displayName);
            DataEditorUtil.Bind(fieldView, nestedVar, _editor);
        }
    }

    /// <summary>
    /// 绑定数据后调用
    /// </summary>
    public void Bind(DataEditor editor, Variable variable) {
        bool typeChanged = _buildType != variable.type;
        if (typeChanged) {
            UnregisterCtrlFieldEvents();
        }
        this._editor = editor;
        this._variable = variable;
        if (typeChanged) {
            RebuildFieldViews();
        }
        Refresh();
    }

    public void Unbind() {
        Variable variable = _variable;
        if (variable == null) return;
        UnregisterCtrlFieldEvents();
        // 递归解绑
        for (int i = 0, count = contentContainer.childCount; i < count; i++) {
            IVarField fieldView = (IVarField)contentContainer[i];
            fieldView.Unbind();
        }
        _editor = null;
        _variable = null;
    }

    private void RebuildFieldViews() {
        _buildType = _variable.type;
        contentContainer.Clear();
        foreach (Variable nestedVar in _variable.values) {
            VisualElement fieldView = DataEditorUtil.CreateField(nestedVar, this._editor);
            DataEditorUtil.SetFieldLabel(fieldView, nestedVar.defineInfo.SimpleName);
            fieldView.tooltip = nestedVar.cfg.tooltip;
            contentContainer.Add(fieldView);
        }
        RegisterCtrlFieldEvents();
    }

    private void UnregisterCtrlFieldEvents() {
        HashSet<VisualElement> ctrlFields = CollectCtrlFields();
        if (ctrlFields == null) {
            return;
        }
        foreach (VisualElement ctrlField in ctrlFields) {
            if (ctrlField is IntegerField integerField) {
                integerField.UnregisterValueChangedCallback(_onCtrlValueChanged1);
            } else if (ctrlField is PopupField<int> popupIntField) {
                popupIntField.UnregisterValueChangedCallback(_onCtrlValueChanged1);
            } else if (ctrlField is PopupField<string> popupStringField) {
                popupStringField.UnregisterValueChangedCallback(_onCtrlValueChanged2);
            } else if (ctrlField is TextField textField) {
                textField.UnregisterValueChangedCallback(_onCtrlValueChanged2);
            }
        }
    }

    private void RegisterCtrlFieldEvents() {
        HashSet<VisualElement> ctrlFields = CollectCtrlFields();
        if (ctrlFields == null) {
            return;
        }
        _onCtrlValueChanged1 ??= _ => Refresh();
        _onCtrlValueChanged2 ??= _ => Refresh();
        foreach (VisualElement ctrlField in ctrlFields) {
            if (ctrlField is IntegerField integerField) {
                integerField.RegisterValueChangedCallback(_onCtrlValueChanged1);
            } else if (ctrlField is PopupField<int> popupIntField) {
                popupIntField.RegisterValueChangedCallback(_onCtrlValueChanged1);
            } else if (ctrlField is PopupField<string> popupStringField) {
                popupStringField.RegisterValueChangedCallback(_onCtrlValueChanged2);
            } else if (ctrlField is TextField textField) {
                textField.RegisterValueChangedCallback(_onCtrlValueChanged2);
            }
        }
    }

    private HashSet<VisualElement> CollectCtrlFields() {
        if (_variable == null || contentContainer.childCount == 0) {
            return null;
        }
        HashSet<VisualElement> ctrlFields = null;
        foreach (Variable nestedVar in _variable.values) {
            if (!nestedVar.cfg.HasBranchCfg) {
                continue;
            }
            ctrlFields ??= new HashSet<VisualElement>(4);
            foreach (FieldBranchCfg branchCfg in nestedVar.cfg.branchCfgs) {
                VisualElement ctrlField = contentContainer[branchCfg.ctrlIndex];
                ctrlFields.Add(ctrlField);
            }
        }
        return ctrlFields;
    }

    private static FieldBranchCfg FilterBranchCfg(Variable container, List<FieldBranchCfg> branchCfgs) {
        for (int index = 0; index < branchCfgs.Count; index++) {
            FieldBranchCfg branchCfg = branchCfgs[index];
            Variable ctrlValue = container[branchCfg.ctrlIndex];
            // Debug.Assert(ctrlValue.defineInfo.SimpleName == branchCfg.ctrl);
            bool isMatch = ctrlValue.type.SimpleName == DSKeywords.TYPE_STRING
                ? ctrlValue.stringValue == branchCfg.value
                : ctrlValue.intValue == branchCfg.intValue;
            if (isMatch) {
                return branchCfg;
            }
        }
        return null;
    }

    #region context-menu

    private void ShowContextMenu(ContextClickEvent evt) {
        if (evt.localMousePosition.y > 20) return; // 只检测顶部区域
        if (_variable == null) return;
        evt.StopPropagation();

        Variable variable = _variable;
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
        if (DataEditorUtil.IsPastable(GUIUtility.systemCopyBuffer, DsonType.Object)) {
            menu.AddItem(new GUIContent("Paste"), false, OnClickPaste, null);
        } else {
            menu.AddDisabledItem(new GUIContent("Paste"));
        }
        // Reset
        menu.AddItem(new GUIContent("Reset"), false, OnClickReset, null);

        // 实例选择
        bool isPairType = DSUtil.IsPairType(variable.type);
        VariableCfg variableCfg = variable.cfg;
        if (variableCfg.HasSupportedInsts && !isPairType) {
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
        if (variableCfg.HasSupportedTypes && !isPairType) {
            string curTypeSymbol = _editor.GetDisplayName(variable.type);
            menu.AddDisabledItem(new GUIContent("ChangeType/" + curTypeSymbol));
            menu.AddSeparator("ChangeType/");
            //
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
        Variable variable = _variable;
        variable.isNull = true;
        variable.ApplyModifiedProperties();
        Refresh();
    }

    private void OnClickSetNotNull(object _) {
        Variable variable = _variable;
        variable.isNull = false;
        variable.ApplyModifiedProperties();
        Refresh();
    }

    private void OnClickCopy(object _) {
        Variable variable = _variable;
        DataEditorUtil.DoCopy(variable, _editor);
    }

    private void OnClickPaste(object _) {
        Variable variable = _variable;
        DataEditorUtil.DoPaste(variable, _editor);
        variable.ApplyModifiedProperties();
        Refresh(true);
    }

    private void OnClickReset(object _) {
        Variable variable = _variable;
        _editor.dataGraph.ResetVariable(variable);
        variable.ApplyModifiedProperties();
        Refresh(true);
    }

    private void OnClickResetWith(object obj) {
        int index = (int)obj;
        Variable variable = _variable;
        DSInst inst = variable.cfg.supportedInsts[index];
        _editor.dataGraph.ResetVariable(variable, inst.DsonValue);
        variable.ApplyModifiedProperties();
        Refresh(true);
    }

    private void OnClickChangeType(object obj) {
        int index = (int)obj;
        Variable variable = _variable;
        string typeSymbol = variable.cfg.supportedTypes[index];
        //
        DSNamedType enclosingType = (DSNamedType)variable.defineInfo.EnclosingElement;
        DSNamedType namedType = (DSNamedType)_editor.repository.ResolveTypeSymbol(enclosingType, typeSymbol);
        _editor.dataGraph.ChangeVariableType(variable, namedType);
        if (variable.isRoot) {
            _editor.dataGraph.InitOutputFields(variable.dataNode);
        }
        variable.ApplyModifiedProperties();
        Refresh(true);
    }

    #endregion
}
}