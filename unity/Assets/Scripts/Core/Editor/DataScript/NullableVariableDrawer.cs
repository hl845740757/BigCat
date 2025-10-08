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
using UnityEngine;
using Wjybxx.BigCatTool.DataScript;

namespace Wjybxx.BigCat.CoreEditor
{
internal class NullableVariableDrawer : DataVariableDrawer
{
    private readonly GUIContent _emptyLabel = new GUIContent("Struct Is Null");
    private readonly GUILayoutOption[] _width50 = new[] { GUILayout.Width(50) };

    public override void OnGUI(DataEditor editor, DataVariable variable, GUIContent label) {
        // label和功能按钮一行
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label);
        if (variable.isNull) {
            EditorGUILayout.HelpBox(_emptyLabel);
            if (GUILayout.Button("Create", EditorStyles.toolbarButton, _width50)) {
                editor.model.CreateValues(variable);
            }
        } else {
            if (GUILayout.Button("SetNull", EditorStyles.toolbarButton, _width50)) {
                editor.model.ResetVariable(variable);
            }
        }
        EditorGUILayout.EndHorizontal();

        // DrawValue
        if (variable.isNull) {
            return;
        }
        editor.DrawVariable(variable.values[0], GUIContent.none);
    }
}
}