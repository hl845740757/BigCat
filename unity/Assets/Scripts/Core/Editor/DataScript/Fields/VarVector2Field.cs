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
public class VarVector2Field : MVector2Field, IVarField
{
    public VarVector2Field() {
    }

    public void Bind(DataGraphEditor editor, Variable variable) {
        userData = variable;
        VariableCfg variableCfg = variable.cfg;
        DataEditorUtil.SetVectorFieldMargin(this, this.labelElement, variableCfg);
        this.isDelayed = variableCfg.isDelayed;
        this.SetValueWithoutNotify(variable.vector2Value);
        this.RegisterValueChangedCallback(evt => {
            evt.StopPropagation();
            variable.vector2Value = evt.newValue;
            variable.ApplyModifiedProperties();
        });
    }

    public void Refresh() {
        if (userData is Variable variable) {
            SetValueWithoutNotify(variable.vector2Value);
        }
    }
}
}