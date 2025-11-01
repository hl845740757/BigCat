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
using System.IO;
using UnityEngine;
using UnityEditor;
using Wjybxx.BigCatTool.DataScript;
using Wjybxx.Commons;
using Wjybxx.BigCat.Core;
using Wjybxx.Commons.Pool;

namespace Wjybxx.BigCat.CoreEditor.DataScript
{
/// <summary>
/// 通用数据结构编辑器
///
/// 注：GUI应当通过Editor来操作Model，避免直接调用Model中的方法，Editor是View + Controller的结合体。
///
/// <h3>自定义绘制</h3>
/// 暂时没有做自定义变量绘制，因为如果支持<code>DataVariableDrawer</code>
/// 
/// </summary>
public class DataEditor : EditorWindow
{
    public DataEditorModel model { get; private set; }
    private EditorWindow window; // 外层壳编辑器

    public readonly ObjectPool<GUIContent> labelPool = new ObjectPool<GUIContent>(
        () => new GUIContent(), content => content.Reset()); // label池

    private NodeData _nodeData;

    [MenuItem("Window/BigCat/DataEditor")]
    private static void OpenWindow() {
        DataEditor win = GetWindow<DataEditor>("数据编辑器");
        win.minSize = new Vector2(400, 600);
        win.Show();
    }

    private void Awake() {

    }

    private void OnEnable() {

    }

    private void OnDisable() {
        // DestroyImmediate(_node);
    }

    private void OnDestroy() {
        // DestroyImmediate(model);
    }

    private void OnGUI() {
        

    }

    public void DrawVariable(Variable variable, GUIContent label) {

    }
}
}