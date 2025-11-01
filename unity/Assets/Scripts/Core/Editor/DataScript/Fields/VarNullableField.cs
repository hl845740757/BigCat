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
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wjybxx.BigCat.CoreEditor.DataScript
{
/// <summary>
/// 
/// </summary>
public class VarNullableField : Foldout, IVarField
{
    private DataGraphEditor editor { get; set; }

    public VarNullableField() {

    }

    public string label {
        get => text;
        set => text = value;
    }

    /// <summary>
    /// 刷新View
    /// </summary>
    public void Refresh() {
        Variable variable = (Variable)userData;
        if (variable == null) return;
        if (variable.isNull) {
            contentContainer.SetEnabled(false);
            return;
        }
        contentContainer.SetEnabled(true);
        //
        VisualElement fieldView = contentContainer[0];
        DataEditorUtil.Refresh(fieldView);
    }

    /// <summary>
    /// 绑定数据后调用
    /// </summary>
    public void Bind(DataGraphEditor editor, Variable variable) {
        this.editor = editor;
        this.userData = variable;
        foreach (Variable nestedVar in variable.values) {
            VisualElement fieldView = DataEditorUtil.CreateField(nestedVar, this.editor);
            contentContainer.Add(fieldView);
        }
        RegisterCallback<ContextClickEvent>(ShowContextMenu);
        Refresh();
    }

    private void ShowContextMenu(ContextClickEvent evt) {
        evt.StopPropagation();
        if (evt.localMousePosition.y > 20) return; // 只检测顶部区域
        if (editor == null) return;

        Variable variable = (Variable)userData;
        GenericMenu menu = new GenericMenu();
        // SetNull
        if (variable.isNull) {
            menu.AddDisabledItem(new GUIContent("SetNull"), true);
            menu.AddItem(new GUIContent("SetNotNull"), false, OnClickSetNotNull, null);
        } else {
            menu.AddItem(new GUIContent("SetNull"), false, OnClickSetNull, null);
            menu.AddDisabledItem(new GUIContent("SetNotNull"), true);
        }
        menu.ShowAsContext();
    }

    private void OnClickSetNull(object _) {
        Variable variable = (Variable)userData;
        variable.isNull = true;
        variable.ApplyModifiedProperties();
        Refresh();
    }

    private void OnClickSetNotNull(object _) {
        Variable variable = (Variable)userData;
        variable.isNull = false;
        variable.ApplyModifiedProperties();
        Refresh();
    }
}
}