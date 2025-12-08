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
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Wjybxx.BigCatTool.DataScript;
using Wjybxx.Commons;
using Wjybxx.Dson;

namespace Wjybxx.BigCat.Editor.DataScript
{
/// <summary>
/// 通用List/Map字段布局
///
/// 注：
/// 1.由于DropdownButton和List的布局非常难以协调，因此我们不使用ToolbarMenu实现菜单栏，而是使用右键。
/// 2.HashSet和字典在编辑器中不执行去重操作，因为无法检测重复 - 无法执行元素的equals，以及无法确定输入结束。
/// 3.List/HashSet类字段，在数据变化的时候需要刷新Port，可递归查询NodeView
/// 4.由于我们支持多态，因此ListView的虚化功能可能导致我们频繁创建VisualElement。
/// </summary>
public class VarListField : BindableElement, IVarField
{
    private readonly ListView _listView;
    private DataEditor _editor;
    private Variable _variable;
    private int _movingIndex = -1;

    public VarListField() {
        _listView = new ListView
        {
            virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
            showFoldoutHeader = true,
            showAddRemoveFooter = true,
            reorderable = true,
        };
        _listView.itemsSource = Array.Empty<Variable>();
        _listView.makeItem = MakeItem;
        _listView.bindItem = BindItem;
        _listView.unbindItem = UnbindItem;
        _listView.itemsAdded += OnItemsAdded;
        _listView.itemsRemoved += OnItemRemoved;
        _listView.itemIndexChanged += OnItemIndexChanged;
        _listView.SetFoldout(false);
        this.Add(_listView);
        // 直接监听ContextClickEvent将无法拦截事件，因此监听原始的鼠标事件
        this.RegisterCallback<MouseDownEvent>(ShowListContextMenu);
    }

    public string label {
        get => _listView.headerTitle;
        set => _listView.headerTitle = value;
    }

    /// <summary>
    /// 刷新View
    /// </summary>
    /// <param name="rebuild"></param>
    public void Refresh(bool rebuild = false) {
        Variable variable = _variable;
        if (variable == null) return;
        if (variable.isNull) {
            _listView.GetFoldout().contentContainer.SetEnabled(false);
            return;
        }
        _listView.GetFoldout().contentContainer.SetEnabled(true);
        if (rebuild) {
            _listView.Rebuild();
        } else {
            _listView.RefreshItems();
        }
    }

    /// <summary>
    /// 绑定数据后调用
    /// </summary>
    public void Bind(DataEditor editor, Variable variable) {
        this.Unbind();
        this._editor = editor;
        this._variable = variable;
        _listView.itemsSource = variable.values;
        DataEditorUtil.SetMaxHeight(_listView, variable.cfg);
        // 刷新UI
        Refresh();
    }

    public void Unbind() {
        Variable variable = _variable;
        if (variable == null) return;
        _editor = null;
        _variable = null;
        //
        _listView.ClearSelection();
        _listView.itemsSource = Array.Empty<Variable>();
    }

    #region list-menu

    private void ShowListContextMenu(MouseDownEvent evt) {
        if (evt.button != (int)MouseButton.RightMouse || evt.localMousePosition.y > 20) return; // 只检测顶部区域
        if (_variable == null) return;
        evt.StopPropagation();

        Variable variable = _variable;
        GenericMenu menu = new GenericMenu();
        // SetNull
        if (variable.isNull) {
            menu.AddDisabledItem(new GUIContent("SetNull"), true);
            menu.AddItem(new GUIContent("SetNotNull"), false, OnClickSetNotNull, null);
        } else {
            menu.AddItem(new GUIContent("SetNull"), false, OnClickSetNull, null);
            menu.AddDisabledItem(new GUIContent("SetNotNull"), true);
        }
        // Copy/Paste
        if (variable.isNull) {
            menu.AddDisabledItem(new GUIContent("Copy"));
        } else {
            menu.AddItem(new GUIContent("Copy"), false, OnClickCopy, null);
        }
        DsonType expectedType = DSUtil.IsMapType(variable.type) ? DsonType.Object : DsonType.Array;
        if (DataEditorUtil.IsPastable(GUIUtility.systemCopyBuffer, expectedType)) {
            menu.AddItem(new GUIContent("Paste"), false, OnClickPaste, null);
        } else {
            menu.AddDisabledItem(new GUIContent("Paste"));
        }
        // Reset
        menu.AddItem(new GUIContent("Reset"), false, OnClickReset, null);
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

    private void OnClickCopy(object _) {
        Variable variable = _variable;
        DataEditorUtil.DoCopy(variable, _editor);
    }

    private void OnClickPaste(object _) {
        Variable variable = _variable;
        DataEditorUtil.DoPaste(variable, _editor);
        variable.ApplyModifiedProperties();
        Refresh(true);
    }

    private void OnClickReset(object _) {
        Variable variable = _variable;
        _editor.dataGraph.ResetVariable(variable);
        variable.ApplyModifiedProperties();
        Refresh(true);
    }

    #endregion

    #region list-item

    private VisualElement MakeItem() {
        VisualElement element = new VisualElement();
        element.RegisterCallback<ContextClickEvent>(ShowItemContextMenu, TrickleDown.TrickleDown);
        return element;
    }

    private void BindItem(VisualElement element, int index) {
        Variable variable = _variable;
        if (_variable == null || index >= _variable.Count) return; // 容量减少时可能出现
        //
        Variable nestedVar = variable[index];
        if (element.childCount == 0) {
            element.Add(DataEditorUtil.CreateField(nestedVar, _editor));
        } else {
            IVarField field = (IVarField)element[0];
            field.Bind(_editor, nestedVar);
        }
        element.userData = index; // 用于处理事件
        DataEditorUtil.SetFieldLabel(element[0], UnityEditorUtil.GetElementName(index));
    }

    private void UnbindItem(VisualElement element, int index) {
        if (element.childCount == 0) return;
        if (element[0] is IVarField field) {
            field.Unbind();
        }
        Variable variable = _variable;
        if (variable == null) {
            return;
        }
        // 多态类型或不可重用的类型直接清理
        element.userData = null;
        if (variable.cfg.HasSupportedTypes || !DataEditorUtil.IsCacheable(element[0])) {
            element.Clear();
        }
    }

    private void OnItemsAdded(IEnumerable<int> indices) {
        Variable variable = _variable;
        Variable previous = null;
        foreach (int index in indices) {
            if (previous == null) {
                if (index == 0) {
                    previous = DSUtil.IsMapType(variable.type)
                        ? _editor.dataGraph.CreateMapItem(variable)
                        : _editor.dataGraph.CreateListItem(variable);
                } else {
                    previous = variable[index - 1];
                    previous = _editor.dataGraph.Duplicate(previous);
                }
            } else {
                previous = _editor.dataGraph.Duplicate(previous);
            }
            variable[index] = previous;
        }
        variable.ApplyModifiedProperties();
    }

    private void OnItemRemoved(IEnumerable<int> indices) {
        Variable variable = _variable; // ListView已经执行删除
        variable.ApplyModifiedProperties();
    }

    private void OnItemIndexChanged(int src, int dest) {
        Variable variable = _variable;
        // variable.MoveTo(src, dest); // ListView已经执行移动
        variable.ApplyModifiedProperties();
    }

    private void ShowItemContextMenu(ContextClickEvent evt) {
        if (evt.localMousePosition.x > 60) return; // 只检测左部区域，避免和元素自身的事件冲突
        evt.StopPropagation();

        VisualElement element = (VisualElement)evt.currentTarget;
        GenericMenu menu = new GenericMenu();
        // 选中元素索引
        int index = (int)element.userData;
        if (_movingIndex != -1) {
            menu.AddDisabledItem(new GUIContent($"index: {index}, moving: {_movingIndex}"));
        } else {
            menu.AddDisabledItem(new GUIContent($"index: {index}, moving: -1"));
        }
        menu.AddSeparator("");

        object context = element.userData;
        menu.AddItem(new GUIContent("Delete"), false, OnClickDelete, context);
        menu.AddItem(new GUIContent("Insert"), false, OnClickInsert, context);
        // 元素移动
        if (index > 0) {
            menu.AddItem(new GUIContent("MoveTop"), false, OnClickMoveTop, context);
        } else {
            menu.AddDisabledItem(new GUIContent("MoveTop"), false);
        }
        menu.AddItem(new GUIContent("MoveUp"), false, OnClickMoveUp, context);
        menu.AddItem(new GUIContent("MoveDown"), false, OnClickMoveDown, context);
        menu.AddItem(new GUIContent("Moving"), false, OnClickMoveTo, context);
        if (_movingIndex != -1) {
            menu.AddItem(new GUIContent("MoveHere"), false, OnClickMoveHere, context);
        } else {
            menu.AddDisabledItem(new GUIContent("MoveHere"));
        }
        menu.ShowAsContext();
    }

    private void OnClickMoveHere(object obj) {
        Variable variable = _variable;
        int index = (int)obj;

        int movingIndex = _movingIndex;
        _movingIndex = -1;
        if (movingIndex == index) {
            return;
        }
        variable.MoveTo(movingIndex, index);
        Refresh();
    }

    private void OnClickMoveTo(object obj) {
        int index = (int)obj;
        _movingIndex = index;
    }

    private void ClearMoveIndex() {
        _movingIndex = -1;
    }

    private void OnClickMoveTop(object obj) {
        ClearMoveIndex();
        Variable variable = _variable;
        int index = (int)obj;
        if (index > 0) {
            variable.MoveTo(index, 0);
        }
        Refresh();
    }

    private void OnClickMoveUp(object obj) {
        ClearMoveIndex();
        Variable variable = _variable;
        int index = (int)obj;
        if (index > 0) {
            variable.MoveTo(index, index - 1);
        }
        Refresh();
    }

    private void OnClickMoveDown(object obj) {
        ClearMoveIndex();
        Variable variable = _variable;
        int index = (int)obj;
        if (index + 1 < variable.Count) {
            variable.MoveTo(index, index + 1);
        }
        Refresh();
    }

    private void OnClickInsert(object obj) {
        ClearMoveIndex();
        Variable variable = _variable;
        int index = (int)obj;

        Variable previous = variable[index];
        previous = _editor.dataGraph.Duplicate(previous);
        variable.Insert(index, previous);
        Refresh();
    }

    private void OnClickDelete(object obj) {
        ClearMoveIndex();
        Variable variable = _variable;
        int index = (int)obj;
        variable.RemoveAt(index);
        Refresh();
    }

    #endregion
}
}