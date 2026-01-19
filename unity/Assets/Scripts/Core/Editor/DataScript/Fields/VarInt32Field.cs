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

using UnityEngine.UIElements;
using Wjybxx.BigCat.Editor.UIElements;

namespace Wjybxx.BigCat.Editor.DataScript
{
public class VarInt32Field : MIntegerField, IVarField
{
    private Variable _variable;

    public VarInt32Field() {
        this.RegisterValueChangedCallback(OnValueChanged);
    }

    public void Bind(DataEditor editor, Variable variable) {
        _variable = variable;
        VariableCfg variableCfg = variable.cfg;
        if (variableCfg.min != null) {
            hasMin = true;
            min = variableCfg.min.AsNumber().IntValue;
        }
        if (variableCfg.max != null) {
            hasMax = true;
            max = variableCfg.max.AsNumber().IntValue;
        }
        DataEditorUtil.SetFieldSize(this, variableCfg);
        DataEditorUtil.SetFieldLabelMargin(this, variableCfg);
        this.isDelayed = variableCfg.isDelayed;
        this.SetValueWithoutNotify(variable.intValue);
    }

    public void Unbind() {
        _variable = null;
    }

    private void OnValueChanged(ChangeEvent<int> evt) {
        if (_variable != null) {
            _variable.intValue = evt.newValue;
            _variable.ApplyModifiedProperties();
        }
    }

    public void Refresh(bool rebuild = false) {
        if (_variable != null) {
            SetValueWithoutNotify(_variable.intValue);
        }
    }
}
}