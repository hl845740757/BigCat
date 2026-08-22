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
using Wjybxx.BigCatTool.DataScript;

namespace Wjybxx.BigCat.Editor.DataScript
{
public class VarInt32PopupField : PopupField<int>, IVarField
{
    private Variable _variable;

    public VarInt32PopupField() {
        labelElement.name = DataEditorUtil.LABEL_ELEMENT_NAME;
        this.RegisterValueChangedCallback(OnValueChanged);
    }

    public void Bind(DataEditor editor, Variable variable) {
        _variable = variable;
        VariableCfg variableCfg = variable.cfg;
        if (variable.type.Kind == DSElementKind.Enum) {
            VariableCfg typeCfg = editor.dataGraph.GetVariableCfg(variable.type);
            choices = typeCfg.intPopValues;
            formatListItemCallback = typeCfg.intPopNameFunc;
            formatSelectedValueCallback = typeCfg.intPopNameFunc;
        } else if (variableCfg.targetEnum != null) {
            // 支持映射到enum
            DSTypeElement enumType = editor.repository.ResolveTypeSymbol(variable.defineInfo, variableCfg.targetEnum);
            VariableCfg enumCfg = editor.dataGraph.GetVariableCfg(enumType);
            choices = enumCfg.intPopValues;
            formatListItemCallback = enumCfg.intPopNameFunc;
            formatSelectedValueCallback = enumCfg.intPopNameFunc;
        } else {
            choices = variableCfg.intPopValues;
            formatListItemCallback = variableCfg.intPopNameFunc;
            formatSelectedValueCallback = variableCfg.intPopNameFunc;
        }
        //
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