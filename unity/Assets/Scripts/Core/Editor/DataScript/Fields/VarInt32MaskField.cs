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
/// <summary>
/// 多选时无法像原生的EnumField一样显示为A,B,C格式：
/// 1.GetDisplayedValue(int) 无法重写
/// 2.formatSelectedValueCallback 只有预设选项才会走到(IsPowerOf2)
/// </summary>
public class VarInt32MaskField : MaskField, IVarField
{
    private Variable _variable;

    public VarInt32MaskField() {
        labelElement.name = DataEditorUtil.LABEL_ELEMENT_NAME;
        this.RegisterValueChangedCallback(OnValueChanged);
    }

    public void Bind(DataEditor editor, Variable variable) {
        _variable = variable;
        VariableCfg variableCfg = variable.cfg;
        if (variable.type.IsEnum) {
            VariableCfg typeCfg = editor.dataGraph.GetVariableCfg(variable.type);
            choices = typeCfg.maskNames;
        } else {
            // 支持映射到Indexes枚举
            if (variableCfg.maskIndexEnum != null) {
                DSTypeElement enumType = editor.repository.ResolveTypeSymbol(variable.defineInfo, variableCfg.maskIndexEnum);
                VariableCfg enumCfg = editor.dataGraph.GetVariableCfg(enumType);
                choices = enumCfg.maskNames;
            } else {
                choices = variableCfg.maskNames;
            }
        }
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