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
using UnityEngine.UIElements;
using Wjybxx.BigCat.Editor.UIElements;
using Wjybxx.Dson.Types;

namespace Wjybxx.BigCat.Editor.DataScript
{
/// <summary>
/// 暂时没有很好的解决方案，先用字符串框顶一段时间
/// </summary>
public class VarFxp64Field : MTextField, IVarField
{
    private Variable _variable;

    public VarFxp64Field() {
        labelElement.name = DataEditorUtil.LABEL_ELEMENT_NAME;
        this.RegisterValueChangedCallback(OnValueChanged);
    }

    public void Bind(DataEditor editor, Variable variable) {
        _variable = variable;
        VariableCfg variableCfg = variable.cfg;
        this.SetValueWithoutNotify(variable.fxp64Value.ToString());
        this.isDelayed = variableCfg.isDelayed;
    }

    public void Unbind() {
        _variable = null;
    }

    private void OnValueChanged(ChangeEvent<string> evt) {
        if (_variable == null) {
            return;
        }
        string newValue = evt.newValue;
        if (newValue.Length == 0) {
            _variable.fxp64Value = default;
            _variable.ApplyModifiedProperties();
            return;
        }
        int index = newValue.IndexOf('.');
        if (index + 1 == newValue.Length) {
            return;
        }
        if (IsParsable(newValue)) {
            _variable.fxp64Value = Fxp64.Parse(newValue);
            _variable.ApplyModifiedProperties();
        } else {
            SetValueWithoutNotify(evt.previousValue);
        }
    }

    public void Refresh(bool rebuild = false) {
        if (_variable != null) {
            SetValueWithoutNotify(_variable.fxp64Value.ToString());
        }
    }

    private static bool IsParsable(string text) {
        if (!double.TryParse(text, out _)) return false;
        int index = text.IndexOf('.');
        if (index == -1) return true;
        return index + 1 != text.Length
               && index + 5 >= text.Length;
    }
}
}