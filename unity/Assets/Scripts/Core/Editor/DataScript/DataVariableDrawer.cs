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

using UnityEditor;
using UnityEngine;

namespace Wjybxx.BigCat.CoreEditor
{
/// <summary>
/// 数据绘制器
/// 
/// 1.类似<see cref="PropertyDrawer"/>，但使用自动布局（计算布局真的累死）。
/// 2.实现类需保持为无可变状态的，状态数据可保存在<see cref="DataVariable.editorState"/>字段上。
/// </summary>
public abstract class DataVariableDrawer
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="editor">用于获取上下文</param>
    /// <param name="variable">要绘制的变量</param>
    /// <param name="label">变量对应的label</param>
    public abstract void OnGUI(DataEditor editor, DataVariable variable, GUIContent label);
}
}