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
using Wjybxx.BigCat.CoreEditor.UIElements;

namespace Wjybxx.BigCat.CoreEditor.DataScript
{
public class VarObjectPathField : BindableElement, IVarField
{
    private readonly ObjectPathField field = ObjectPathField.Create();

    public VarObjectPathField() {
    }

    public string label {
        get => field.label;
        set => field.label = value;
    }

    public void Bind(DataGraphEditor editor, Variable variable) {
        userData = variable;
        field.SetValueWithoutNotify(variable.objectPathValue);
        field.RegisterValueChangedCallback(evt => {
            evt.StopPropagation();
            variable.objectPathValue = evt.newValue;
            variable.ApplyModifiedProperties();
            variable.portView?.OnValueChanged(evt.previousValue, evt.newValue);
        });
    }

    public void Refresh() {
        if (userData is Variable variable) {
            field.SetValueWithoutNotify(variable.objectPathValue);
        }
    }
}
}