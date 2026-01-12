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

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Wjybxx.BigCat.Editor.UIElements;

namespace Wjybxx.BigCat.Editor.DataScript
{
public class VarStringField : MTextField, IVarField
{
    private Variable _variable;
    private readonly DropdownField dropdownField;

    public VarStringField() {
        this.RegisterCallback<ContextClickEvent>(ShowContextMenu);
        this.RegisterValueChangedCallback(OnValueChanged);
        // 支持候选项
        dropdownField = new DropdownField();
        dropdownField.style.width = 20f;
        dropdownField.style.flexDirection = FlexDirection.RowReverse;
        dropdownField.RegisterValueChangedCallback(evt => {
            this.SetValueWithoutNotify(evt.newValue);
            this.OnValueChanged(evt);
        });
        this.Add(dropdownField);
    }

    public void Bind(DataEditor editor, Variable variable) {
        _variable = variable;
        VariableCfg variableCfg = variable.cfg;
        DataEditorUtil.SetFieldLabelMargin(this, variableCfg);
        // Text为Null的情况下设置其它属性可能引发NPE
        this.SetValueWithoutNotify(variable.stringValue ?? "");
        this.isDelayed = variableCfg.isDelayed;
        this.multiline = variableCfg.isMultiline;
        // 候选者下拉菜单
        if (variableCfg.candidatesValues != null) {
            dropdownField.choices = variableCfg.candidatesValues;
            dropdownField.SetDisplay(true);
        } else {
            dropdownField.SetDisplay(false);
        }
    }

    public void Unbind() {
        _variable = null;
    }

    private void OnValueChanged(ChangeEvent<string> evt) {
        if (_variable != null) {
            _variable.stringValue = evt.newValue;
            _variable.ApplyModifiedProperties();
        }
    }

    public void Refresh(bool rebuild = false) {
        if (_variable == null) {
            return;
        }
        SetValueWithoutNotify(_variable.stringValue);
        if (_variable.isNull) {
            textInputBase.SetEnabled(false);
            textInputBase.text = "value is null"; // 其实再使用一个Label更安全
        } else {
            textInputBase.SetEnabled(true);
            textInputBase.text = _variable.stringValue ?? "";
        }
    }

    private void ShowContextMenu(ContextClickEvent evt) {
        if (_variable == null) return;
        if (evt.localMousePosition.x > 100f) return; // 只响应标签区域
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
}
}