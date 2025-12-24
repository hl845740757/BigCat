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
using UnityEditor;
using UnityEngine;
using Wjybxx.BigCat.Core;

namespace Wjybxx.BigCat.Editor
{
[CustomEditor(typeof(SpriteGroup))]
public class SpriteGroupEditor : UnityEditor.Editor
{
    private SpriteGroup _group;
    private Vector2 scrollPos;
    private GUILayoutOption[] scrollOptions;
    //
    // private static bool _foldOut = true;
    private static bool _listMode = true; // 静态字段保留状态

    private void Awake() {
        _group = target as SpriteGroup;
        scrollOptions = new[] { GUILayout.MaxHeight(440) };
    }

    private void OnEnable() {
        if (_group.Count > 20) {
            _listMode = true;
        }
    }

    public override void OnInspectorGUI() {
        DrawControlArea();
        DrawSeparator();

        DrawImages();
        DrawSeparator();
    }

    private static void DrawSeparator() {
        Rect rect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(rect, Color.gray);
    }

    private void DrawImages() {
        EditorGUILayout.BeginVertical();
        _listMode = EditorGUILayout.Toggle("列表模式", _listMode);
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, scrollOptions);
        for (int index = 0; index < _group.sprites.Length; index++) {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.IntField("Index", index);
            Sprite sprite = _group.sprites[index];
            if (_listMode) {
                EditorGUILayout.ObjectField(sprite, typeof(Sprite), false);
            } else {
                EditorGUILayout.ObjectField("Sprite", sprite, typeof(Sprite), false);
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawControlArea() {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("资产目录:");
        bool clickRefresh = GUILayout.Button("刷新文件") && Event.current.button == 0;
        EditorGUILayout.EndHorizontal();
        //
        string bindFolder = EditorGUILayout.DelayedTextField("绑定目录", _group.bindFolder);
        bool preferName = EditorGUILayout.Toggle("Name唯一", _group.preferName);
        bool sequenced = EditorGUILayout.Toggle("序列图", _group.sequenced);
        //
        if (bindFolder != _group.bindFolder) {
            _group.bindFolder = bindFolder;
            EditorUtility.SetDirty(_group);
        }
        if (preferName != _group.preferName) {
            _group.preferName = preferName;
            EditorUtility.SetDirty(_group);
        }
        if (sequenced != _group.sequenced) {
            _group.sequenced = sequenced;
            EditorUtility.SetDirty(_group);
        }
        //
        if (clickRefresh) {
            SpriteGroup.RefreshSprites(_group);
        }
    }
}
}