#region LICENSE

// Copyright 2026 wjybxx(845740757@qq.com)
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

using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wjybxx.BigCatTool.DataScript;
using Wjybxx.Dson.Codec;

namespace Wjybxx.BigCat.Editor.DataScript
{
public class VarEnumSet64Field : Foldout, IVarField
{
    private DataEditor _editor;
    private Variable _variable;
    private DSNamedType _buildType;

    public VarEnumSet64Field() {
        this.GetToggle().labelElement.name = DataEditorUtil.LABEL_ELEMENT_NAME;
    }

    public string label {
        get => text;
        set => text = value;
    }

    public void Refresh(bool rebuild = false) {
        Variable variable = _variable;
        if (variable == null) return;
        //
        VisualElement container = contentContainer;
        for (int index = 0; index < container.childCount; index++) {
            VisualElement fieldView = container[index];
            Variable nestedVar = variable[index];
            DataEditorUtil.Bind(fieldView, nestedVar, _editor);
        }
    }

    public void Bind(DataEditor editor, Variable variable) {
        bool typeChanged = _buildType != variable.type;
        this._editor = editor;
        this._variable = variable;
        if (typeChanged) {
            RebuildFieldViews();
        }
        Refresh();
    }

    public void Unbind() {
        Variable variable = _variable;
        if (variable == null) return;
        // 递归解绑
        for (int i = 0, count = contentContainer.childCount; i < count; i++) {
            IVarField fieldView = (IVarField)contentContainer[i];
            fieldView.Unbind();
        }
        _editor = null;
        _variable = null;
    }

    private void RebuildFieldViews() {
        DSNamedType enumType = (DSNamedType)_variable.type.TypeArguments[0];
        int maxIndex = enumType.EnclosedElements.Cast<DSEnumValue>()
            .Select(e => e.Number)
            .Max();
        Variable variable = _variable;
        //
        for (int idx = 0; idx < variable.Count; idx++) {
            variable[idx].cfg = new VariableCfg()
            {
                maskNames = VarEnumSetField.GetMaskNames(enumType, idx),
                encodeFeatures = SerializeFeatures.NumberFixed | SerializeFeatures.NumberHex
            };
        }
        while (contentContainer.childCount < variable.Count) {
            contentContainer.Add(new VarInt32MaskField());
        }
        // 可能只有32位
        contentContainer[1].SetDisplay(maxIndex >= 32);
    }
}
}