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
    private DataEditor _editor;
    private Variable _variable;

    public VarNullableField() {
        style.flexShrink = 0;
        this.RegisterCallback<ContextClickEvent>(ShowContextMenu);
    }

    public string label {
        get => text;
        set => text = value;
    }

    /// <summary>
    /// 刷新View
    /// </summary>
    /// <param name="rebuild"></param>
    public void Refresh(bool rebuild = false) {
        Variable variable = _variable;
        if (variable == null) return;
        if (variable.isNull) {
            contentContainer.SetEnabled(false);
            return;
        }
        contentContainer.SetEnabled(true);
        VisualElement fieldView = contentContainer[0];
        DataEditorUtil.Refresh(fieldView, rebuild);
    }

    /// <summary>
    /// 绑定数据后调用
    /// </summary>
    public void Bind(DataEditor editor, Variable variable) {
        bool typeChanged = _variable == null || _variable.type != variable.type;
        if (typeChanged) {
            contentContainer.Clear();
        }
        this._editor = editor;
        this._variable = variable;
        if (typeChanged) {
            foreach (Variable nestedVar in variable.values) {
                VisualElement fieldView = DataEditorUtil.CreateField(nestedVar, this._editor);
                contentContainer.Add(fieldView);
            }
        }
        Refresh();
    }

    public void Unbind() {
        Variable variable = _variable;
        if (variable == null) return;
        _editor = null;
        _variable = null;
        contentContainer.Clear();
    }

    #region context menu

    private void ShowContextMenu(ContextClickEvent evt) {
        evt.StopPropagation();
        if (evt.localMousePosition.y > 20) return; // 只检测顶部区域
        if (_variable == null) return;

        GenericMenu menu = new GenericMenu();
        // SetNull
        if (_variable.isNull) {
            menu.AddDisabledItem(new GUIContent("SetNull"), true);
            menu.AddItem(new GUIContent("SetNotNull"), false, OnClickSetNotNull, null);
        } else {
            menu.AddItem(new GUIContent("SetNull"), false, OnClickSetNull, null);
            menu.AddDisabledItem(new GUIContent("SetNotNull"), true);
        }
        menu.ShowAsContext();
    }

    private void OnClickSetNull(object _) {
        Variable variable = _variable;
        variable.isNull = true;
        variable.ApplyModifiedProperties();
        Refresh();
    }

    private void OnClickSetNotNull(object _) {
        Variable variable = _variable;
        variable.isNull = false;
        variable.ApplyModifiedProperties();
        Refresh();
    }

    #endregion
}
}