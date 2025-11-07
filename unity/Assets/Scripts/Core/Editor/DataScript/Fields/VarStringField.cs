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
using Wjybxx.BigCat.CoreEditor.UIElements;

namespace Wjybxx.BigCat.CoreEditor.DataScript
{
public class VarStringField : MTextField, IVarField
{
    private Variable _variable;

    public VarStringField() {
        this.RegisterCallback<ContextClickEvent>(ShowContextMenu);
        this.RegisterValueChangedCallback(OnValueChanged);
    }

    public void Bind(DataEditor editor, Variable variable) {
        _variable = variable;
        VariableCfg variableCfg = variable.cfg;
        DataEditorUtil.SetFieldLabelMargin(this, variableCfg);
        // Text为Null的情况下设置其它属性可能引发NPE
        this.SetValueWithoutNotify(variable.stringValue ?? "");
        this.isDelayed = variableCfg.isDelayed;
        this.multiline = variableCfg.isMultiline;
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