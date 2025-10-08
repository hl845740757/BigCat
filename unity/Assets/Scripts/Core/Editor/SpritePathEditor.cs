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
using Wjybxx.BigCat.UnityCore;

namespace Wjybxx.BigCat.CoreEditor.Core.Editor
{
[CustomPropertyDrawer(typeof(SpritePath))]
public class SpritePathEditor : PropertyDrawer
{
    private static readonly string[] _searchFolders = new[] { "Assets/Resources/Sprites", "Assets/Sprites" };
    private static readonly Dictionary<string, SpriteGroup> _nameToSpriteGroup = new();
    private static double lastSearchTime;
    private static readonly GUILayoutOption[] _width120 = new GUILayoutOption[] { GUILayout.Width(120) };
    private static readonly GUILayoutOption[] _width50 = new GUILayoutOption[] { GUILayout.Width(50) };

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        EditorGUI.BeginProperty(position, label, property);
        // Draw label
        Rect foldRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(foldRect, property.isExpanded, label);
        if (!property.isExpanded) {
            return;
        }
        SerializedProperty pGroupPath = property.FindPropertyRelative("groupPath");
        SerializedProperty pIndex = property.FindPropertyRelative("index");
        SpritePath spritePath = new SpritePath(pGroupPath.stringValue, pIndex.intValue);
        // x+缩进
        Rect groupRect = new Rect(position.x + 10, position.y + 18, position.width - 120, EditorGUIUtility.singleLineHeight);
        Rect indexRect = new Rect(position.x + 10, position.y + 38, position.width - 120, EditorGUIUtility.singleLineHeight);

        spritePath.groupPath = EditorGUI.TextField(groupRect, "groupPath", spritePath.groupPath);
        spritePath.index = EditorGUI.IntField(indexRect, "index", spritePath.index);
        // 右对齐
        groupRect.x = position.xMax - 100;
        groupRect.width = 100;
        if (GUI.Button(groupRect, "选择")) {
            OnClickSelectSpriteGroup(ref spritePath);
            WriteBack(pGroupPath, pIndex, spritePath);
            property.serializedObject.ApplyModifiedProperties();
            GUIUtility.ExitGUI();
        }

        EditorGUIUtility.labelWidth = 20;
        indexRect.x = position.xMax - 100;
        indexRect.width = 100;
        if (GUI.Button(indexRect, "选择")) {
            OnClickSelectSprite(ref spritePath);
            WriteBack(pGroupPath, pIndex, spritePath);
            property.serializedObject.ApplyModifiedProperties();
            GUIUtility.ExitGUI();
        }
        WriteBack(pGroupPath, pIndex, spritePath);
        EditorGUI.EndProperty();
    }

    private static void WriteBack(SerializedProperty pGroupPath, SerializedProperty pIndex,
                                  SpritePath spritePath) {
        pGroupPath.stringValue = spritePath.groupPath;
        pIndex.intValue = spritePath.index;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
        if (property.isExpanded) {
            return EditorGUIUtility.singleLineHeight * 3 + (2 * 2); // 3排
        }
        return EditorGUIUtility.singleLineHeight;
    }

    /// <summary>
    ///
    /// 如果返回true表示应当调用<see cref="GUIUtility.ExitGUI"/>退出当前绘制。
    /// </summary>
    /// <returns>是否需要退出当前GUI</returns>
    public static bool DoLayout(ref SpritePath spritePath, ref bool isExpanded, GUIContent label) {
        EditorGUILayout.BeginVertical();
        isExpanded = EditorGUILayout.Foldout(isExpanded, label);
        if (!isExpanded) {
            EditorGUILayout.EndVertical();
            return false;
        }

        EditorGUILayout.BeginHorizontal();
        spritePath.groupPath = EditorGUILayout.TextField("groupPath", spritePath.groupPath, _width120);
        if (GUILayout.Button("选择", _width50)) {
            OnClickSelectSpriteGroup(ref spritePath);
            return true;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        spritePath.index = EditorGUILayout.IntField("index", spritePath.index);
        if (GUILayout.Button("选择", _width50)) {
            OnClickSelectSprite(ref spritePath);
            return true;
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        return false;
    }

    #region events

    internal static bool OnClickSelectSpriteGroup(ref SpritePath spritePath) {
        SpriteGroup spriteGroup = LoadSpriteGroup(spritePath.groupPath);
        string groupAssetFolder = spriteGroup ? UnityHelper.GetAssetFolderPath(spriteGroup) : _searchFolders[0];
        string filePath = EditorUtility.OpenFilePanel("选择SpriteGroup", groupAssetFolder, "asset");
        if (string.IsNullOrEmpty(filePath)) {
            return false;
        }
        string assetPath = UnityHelper.ConvertToAssetPath(filePath);
        spriteGroup = AssetDatabase.LoadAssetAtPath<SpriteGroup>(assetPath);
        if (spriteGroup) {
            spritePath.groupPath = spriteGroup.preferName ? spriteGroup.name : assetPath;
        } else {
            spritePath.groupPath = null;
        }
        return true;
    }

    internal static bool OnClickSelectSprite(ref SpritePath spritePath) {
        SpriteGroup spriteGroup = LoadSpriteGroup(spritePath.groupPath);
        if (!spriteGroup) {
            return false;
        }
        string groupAssetFolder = spriteGroup ? UnityHelper.GetAssetFolderPath(spriteGroup) : _searchFolders[0];
        string filePath = EditorUtility.OpenFilePanel("选择图片", groupAssetFolder, "png");
        if (string.IsNullOrEmpty(filePath)) {
            return false;
        }
        string assetPath = UnityHelper.ConvertToAssetPath(filePath);
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (sprite) {
            spritePath.index = spriteGroup.IndexOf(sprite.name);
        } else {
            spritePath.index = -1;
        }
        return true;
    }

    private static SpriteGroup LoadSpriteGroup(string groupPath) {
        if (string.IsNullOrWhiteSpace(groupPath)) {
            return null;
        }
        if (groupPath.LastIndexOf('/') > 0) {
            return AssetDatabase.LoadAssetAtPath<SpriteGroup>(groupPath);
        }
        // name引用
        string assetName = groupPath;
        if (_nameToSpriteGroup.TryGetValue(assetName, out SpriteGroup spriteGroup)) {
            if (spriteGroup && assetName == spriteGroup.name) {
                return spriteGroup;
            }
            _nameToSpriteGroup.Remove(assetName);
        }
        // 避免频繁检索资源
        if (Time.realtimeSinceStartup - lastSearchTime < 1) {
            return null;
        }
        lastSearchTime = Time.realtimeSinceStartup;
        foreach (string guid in AssetDatabase.FindAssets("t:SpriteGroup", _searchFolders)) {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            spriteGroup = AssetDatabase.LoadAssetAtPath<SpriteGroup>(assetPath);
            if (!spriteGroup) {
                continue;
            }
            if (spriteGroup.preferName) {
                _nameToSpriteGroup[spriteGroup.name] = spriteGroup;
            }
        }
        _nameToSpriteGroup.TryGetValue(assetName, out spriteGroup);
        return spriteGroup;
    }

    internal static Sprite LoadSprite(SpritePath spritePath) {
        if (spritePath.IsEmpty || spritePath.index < 0) {
            return null;
        }
        SpriteGroup spriteGroup = LoadSpriteGroup(spritePath.groupPath);
        if (spriteGroup) {
            return spriteGroup.GetSprite(spritePath.index);
        }
        return null;
    }

    #endregion
}
}