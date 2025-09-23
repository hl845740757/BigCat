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
[CustomPropertyDrawer(typeof(StringEnumFieldAttribute))]
public class StringEnumPropertyDrawer : PropertyDrawer
{
    private GUIContent[] namesCache;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        StringEnumFieldAttribute fieldAttribute = (StringEnumFieldAttribute)this.attribute;
        string[] displayNames = fieldAttribute.displayNames;
        if (namesCache == null) {
            namesCache = new GUIContent[displayNames.Length];
            for (int i = 0; i < displayNames.Length; i++) {
                namesCache[i] = new GUIContent(displayNames[i]);
            }
        }
        // 暂时先Index查询吧...
        int index = Array.IndexOf(displayNames, property.stringValue);
        if (index < 0) index = 0;

        index = EditorGUI.Popup(position, label, index, namesCache);
        property.stringValue = displayNames[index];
    }
}
}