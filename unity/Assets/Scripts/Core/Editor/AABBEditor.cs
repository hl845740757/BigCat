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
using Wjybxx.BigCat.UnityCore;

namespace Wjybxx.BigCat.CoreEditor
{
/// <summary>
/// AABB编辑器
///
/// PS：PropertyDrawer不能使用自动布局方法，计算布局真的难写...
/// </summary>
[CustomPropertyDrawer(typeof(AABB))]
public class AABBEditor : PropertyDrawer
{
    private static readonly string[] _modeDisplay = { "Min + Max", "Min + Size", "Center + Size", "Bottom + Size" };
    private static readonly int[] _modeValues = { 0, 1, 2, 3 };
    private static int _mode = 0;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        EditorGUI.BeginProperty(position, label, property);
        // 
        Rect foldRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(foldRect, property.isExpanded, GUIContent.none);
        // Draw label
        EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
        if (!property.isExpanded) {
            return;
        }
        // 缓存属性会导致修改所有属性...
        SerializedProperty pMin = property.FindPropertyRelative("min");
        SerializedProperty pMax = property.FindPropertyRelative("max");
        AABB aabb = new AABB(pMin.vector3Value, pMax.vector3Value);

        // 功能按钮 - Rect的xy是minX和minY...
        Rect modeRect = new Rect(position.x + 150, position.y, 100, EditorGUIUtility.singleLineHeight);
        _mode = EditorGUI.IntPopup(modeRect, _mode, _modeDisplay, _modeValues);

        Rect repairRect = new Rect(modeRect.x + 120, modeRect.y, 100, EditorGUIUtility.singleLineHeight);
        if (GUI.Button(repairRect, "Repair")) {
            aabb.Repair();
        }
        Rect minRect = new Rect(position.x, position.y + 20, position.width, EditorGUIUtility.singleLineHeight);
        Rect maxRect = new Rect(position.x, position.y + 38, position.width, EditorGUIUtility.singleLineHeight);
        switch (_mode) {
            default: {
                aabb.min = EditorGUI.Vector3Field(minRect, "Min", aabb.min);
                aabb.max = EditorGUI.Vector3Field(maxRect, "Max", aabb.max);
                break;
            }
            case 1: {
                // 不能直接赋值到AABB，修改Min导致Size变化
                Vector3 min = EditorGUI.Vector3Field(minRect, "Min", aabb.min);
                Vector3 size = EditorGUI.Vector3Field(maxRect, "Size", aabb.Size);
                aabb.min = min;
                aabb.max = min + size;
                break;
            }
            case 2: {
                aabb.Center = EditorGUI.Vector3Field(minRect, "Center", aabb.Center);
                aabb.Size = EditorGUI.Vector3Field(maxRect, "Size", aabb.Size);
                break;
            }
            case 3: {
                aabb.Bottom = EditorGUI.Vector3Field(minRect, "Bottom", aabb.Bottom);
                aabb.Size = EditorGUI.Vector3Field(maxRect, "Size", aabb.Size);
                break;
            }
        }
        pMin.vector3Value = aabb.min;
        pMax.vector3Value = aabb.max;
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
        if (property.isExpanded) {
            return EditorGUIUtility.singleLineHeight * 3 + (2 * 2); // 3排
        }
        return EditorGUIUtility.singleLineHeight;
    }
}
}