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
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine.UIElements;
using Wjybxx.BigCat.Util;
using Wjybxx.BigCatTool.DataScript;
using Wjybxx.Dson.Codec;

namespace Wjybxx.BigCat.Editor.DataScript
{
/// <summary>
/// <see cref="EnumSet{T}"/>的视图
/// </summary>
public class VarEnumSetField : Foldout, IVarField
{
    private DataEditor _editor;
    private Variable _variable;
    private DSNamedType _buildType;

    public VarEnumSetField() {
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
        Variable arrayField = variable.values[0];
        for (int index = 0; index < container.childCount; index++) {
            VisualElement fieldView = container[index];
            Variable nestedVar = arrayField[index];
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
        // 创建变量
        int wordCount = WordCount(maxIndex + 1);
        Variable arrayField = _variable.values[0];
        while (arrayField.Count < wordCount) {
            Variable nestedVar = _editor.dataGraph.CreateListItem(arrayField);
            arrayField.Add(nestedVar);
        }
        for (int idx = 0; idx < wordCount; idx++) {
            arrayField[idx].cfg = new VariableCfg()
            {
                maskNames = GetMaskNames(enumType, idx),
                encodeFeatures = SerializeFeatures.NumberFixed | SerializeFeatures.NumberHex
            };
        }
        // 创建视图
        while (contentContainer.childCount < wordCount) {
            contentContainer.Add(new VarInt32MaskField());
        }
        while (contentContainer.childCount > wordCount) {
            contentContainer.RemoveAt(contentContainer.childCount - 1);
        }
    }

    private static List<string> GetMaskNames(DSNamedType enumType, int wordIndex) {
        int minNum = wordIndex * 32;
        string[] tempNames = new string[32];
        int maxNum = minNum;
        foreach (DSEnumValue enumValue in enumType.EnclosedElements.Cast<DSEnumValue>()) {
            if (enumValue.Number < minNum) continue;
            if (enumValue.Number >= minNum + 32) break;
            //
            int idx = enumValue.Number - minNum;
            tempNames[idx] = enumValue.SimpleName;
            maxNum = Math.Max(maxNum, enumValue.Number);
        }
        int count = maxNum - minNum + 1;
        List<string> maskNames = new List<string>(count);
        for (int index = 0; index < count; index++) {
            maskNames.Add(tempNames[index] ?? index.ToString());
        }
        return maskNames;
    }

    #region internal

    private const int MAX_COUNT = 1024;
    private const int ADDRESS_BITS_PER_WORD = 5;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int WordIndex(int index) {
        return index >> ADDRESS_BITS_PER_WORD;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int WordCount(int bitCount) {
        return (bitCount >> ADDRESS_BITS_PER_WORD) + 1;
    }

    #endregion
}
}