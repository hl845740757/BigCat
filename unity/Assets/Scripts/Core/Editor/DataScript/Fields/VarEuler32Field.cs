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

    public string label {
        get => field.label;
        set => field.label = value;
    }

    public void Bind(DataGraphEditor editor, Variable variable) {
        userData = variable;
        field.SetValueWithoutNotify((Euler32)variable.intValue);
        field.RegisterValueChangedCallback(evt => {
            evt.StopPropagation();
            variable.intValue = evt.newValue;
            variable.ApplyModifiedProperties();
        });
    }

    public void Refresh() {
        if (userData is Variable variable) {
            field.SetValueWithoutNotify((Euler32)variable.intValue);
        }
    }
}
}