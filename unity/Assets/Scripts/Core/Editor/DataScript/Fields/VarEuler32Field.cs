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
using Wjybxx.BigCat.Core;
using Wjybxx.BigCat.CoreEditor.UIElements;

namespace Wjybxx.BigCat.CoreEditor.DataScript
{
public class VarEuler32Field : BindableElement, IVarField
{
    private readonly Euler32Field field = Euler32Field.Create();
    private Variable _variable;

    public VarEuler32Field() {
        Add(field);
        field.RegisterValueChangedCallback(OnValueChanged);
    }

    public string label {
        get => field.label;
        set => field.label = value;
    }

    public void Bind(DataEditor editor, Variable variable) {
        _variable = variable;
        field.SetValueWithoutNotify((Euler32)variable.intValue);
    }

    public void Unbind() {
        _variable = null;
    }

    private void OnValueChanged(ChangeEvent<Euler32> evt) {
        if (_variable != null) {
            _variable.intValue = evt.newValue;
            _variable.ApplyModifiedProperties();
        }
    }

    public void Refresh(bool rebuild = false) {
        if (_variable != null) {
            field.SetValueWithoutNotify((Euler32)_variable.intValue);
        }
    }
}
}