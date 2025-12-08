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

using UnityEngine;
using UnityEngine.UIElements;
using Wjybxx.BigCat.CoreEditor.UIElements;

namespace Wjybxx.BigCat.CoreEditor.DataScript
{
public class VarColorField : MColorField, IVarField
{
    private Variable _variable;

    public VarColorField() {
        this.RegisterValueChangedCallback(OnValueChanged);
    }

    public void Bind(DataEditor editor, Variable variable) {
        _variable = variable;
        VariableCfg variableCfg = variable.cfg;
        DataEditorUtil.SetFieldLabelMargin(this, variableCfg);
        this.Refresh();
    }

    public void Unbind() {
        _variable = null;
    }

    private void OnValueChanged(ChangeEvent<Color> evt) {
        if (_variable == null) {
            return;
        }
        if (_variable.Count == 1) { // Color32
            _variable[0].intValue = UnityEditorUtil.AsInt32(evt.newValue);
        } else {
            _variable.colorValue = evt.newValue;
        }
        _variable.ApplyModifiedProperties();
    }

    public void Refresh(bool rebuild = false) {
        if (_variable == null) {
            return;
        }
        if (_variable.Count == 1) { // Color32
            Color32 color32 = UnityEditorUtil.AsColor32(_variable[0].intValue);
            this.SetValueWithoutNotify(color32);
        } else {
            SetValueWithoutNotify(_variable.colorValue);
        }
    }
}
}