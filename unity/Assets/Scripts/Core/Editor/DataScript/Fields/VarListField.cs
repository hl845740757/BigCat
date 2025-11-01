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
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Wjybxx.BigCat.CoreEditor.UIElements;
using Wjybxx.BigCatTool.DataScript;
using Wjybxx.Dson;
using Wjybxx.Dson.Text;

namespace Wjybxx.BigCat.CoreEditor.DataScript
{
/// <summary>
/// 通用List/Map字段布局
///
/// 注：
/// 1.由于DropdownButton和Foldout以及List的布局非常难以协调，因此我们不使用ToolbarMenu实现菜单栏，而是使用右键。
/// 2.HashSet和字典在编辑器中不执行去重操作，因为无法检测重复 - 无法执行元素的equals，以及无法确定输入结束。
/// 3.List/HashSet类字段，在数据变化的时候需要刷新Port，可递归查询NodeView
/// </summary>
public class VarListField : BindableElement, IVarField
{
    private readonly ListView _listView;
    private DataGraphEditor editor { get; set; }

    public VarListField() {
        _listView = new ListView()
        {
            showFoldoutHeader = true,
            showAddRemoveFooter = true
        };
        Add(_listView);
    }

    public string label {
        get => _listView.headerTitle;
        set => _listView.headerTitle = value;
    }

    /// <summary>
    /// 刷新View
    /// </summary>
    public void Refresh() {
        Variable variable = (Variable)userData;
        if (variable == null) return;
        if (variable.isNull) {
            _listView.SetEnabled(false);
            return;
        }
        _listView.SetEnabled(true);
        _listView.RefreshItems();
    }

    /// <summary>
    /// 绑定数据后调用
    /// </summary>
    public void Bind(DataGraphEditor editor, Variable variable) {
        this.editor = editor;
        this.userData = variable;
        // TODO 创建子节点
        // 刷新UI
        Refresh();
    }
}
}