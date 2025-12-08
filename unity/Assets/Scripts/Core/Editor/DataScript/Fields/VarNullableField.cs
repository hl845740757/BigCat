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
using Wjybxx.BigCatTool.DataScript;

namespace Wjybxx.BigCat.Editor.DataScript
{
/// <summary>
/// 
/// </summary>
public class VarNullableField : Foldout, IVarField
{
    private DataEditor _editor;
    private Variable _variable;
    private DSNamedType _buildType;

    public VarNullableField() {
        style.flexShrink = 0;
        this.RegisterCallback<ContextClickEvent>(ShowContextMenu);
    }

    /// <summary>
    /// 不会因解绑而清理
    /// </summary>
    public DSNamedType buildType => _buildType;

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
        DataEditorUtil.Bind(fieldView, variable[0], _editor);
    }

    /// <summary>
    /// 绑定数据后调用
    /// </summary>
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
        _buildType = _variable.type;
        contentContainer.Clear();
        foreach (Variable nestedVar in _variable.values) {
            VisualElement fieldView = DataEditorUtil.CreateField(nestedVar, this._editor);
            fieldView.tooltip = nestedVar.cfg.tooltip;
            DataEditorUtil.SetFieldLabel(fieldView, nestedVar.defineInfo.SimpleName);
            contentContainer.Add(fieldView);
        }
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