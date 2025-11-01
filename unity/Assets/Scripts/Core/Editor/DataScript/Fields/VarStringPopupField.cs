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

using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Wjybxx.BigCat.CoreEditor.DataScript
{
public class VarStringPopupField : PopupField<string>, IVarField
{
    public VarStringPopupField() {
    }

    public void Bind(DataGraphEditor editor, Variable variable) {
        userData = variable;
        VariableCfg variableCfg = variable.cfg;
        choices = variableCfg.stringPopValues;
        formatListItemCallback = variableCfg.stringPopNameFunc;
        formatSelectedValueCallback = variableCfg.stringPopNameFunc;
        //
        DataEditorUtil.SetFieldLabelMargin(this, variableCfg);
        this.SetValueWithoutNotify(variable.stringValue);
        this.RegisterValueChangedCallback(evt => {
            evt.StopPropagation();
            variable.stringValue = evt.newValue;
            variable.ApplyModifiedProperties();
        });
    }

    public void Refresh() {
        if (userData is Variable variable) {
            SetValueWithoutNotify(variable.stringValue);
        }
    }
}
}