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
using Wjybxx.Commons;

namespace Wjybxx.BigCat.Editor.DataScript
{
public class VarObjectPathField : BindableElement, IVarField
{
    private readonly ObjectPathField field = ObjectPathField.Create();
    private Variable _variable;

    public VarObjectPathField() {
        Add(field);
        field.isExpanded = false; // 默认折叠
        field.RegisterValueChangedCallback(OnValueChanged);
    }

    public string label {
        get => field.label;
        set => field.label = value;
    }

    public void Bind(DataEditor editor, Variable variable) {
        _variable = variable;
        field.SetValueWithoutNotify(variable.objectPathValue);
        field.isReadOnly = variable.cfg.HasPortCfg;
    }

    public void Unbind() {
        _variable = null;
    }

    private void OnValueChanged(ChangeEvent<ObjectPath> evt) {
        if (_variable != null) {
            _variable.objectPathValue = evt.newValue;
            _variable.ApplyModifiedProperties();
        }
    }

    public void Refresh(bool rebuild) {
        if (_variable != null) {
            field.SetValueWithoutNotify(_variable.objectPathValue);
        }
    }
}
}