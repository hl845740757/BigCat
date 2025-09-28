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
using Wjybxx.BigCat.UnityCore;

namespace Wjybxx.BigCat.CoreEditor
{
[CustomEditor(typeof(SpriteGroup))]
public class SpriteGroupEditor : Editor
{
    private SpriteGroup _group;

    private Vector2 scrollPos;
    private GUILayoutOption[] scrollOptions;
    //
    // private static bool _foldOut = true;
    private static bool _listMode = true; // 静态字段保留状态

    private void Awake() {
        _group = target as SpriteGroup;

        scrollOptions = new[]
        {
            GUILayout.MaxHeight(440),
            // GUILayout.ExpandHeight(true),
        };
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
        EditorGUILayout.SelectableLabel(AssetDatabase.GetAssetPath(_group));
        bool preferName = EditorGUILayout.Toggle("Name唯一", _group.preferName);
        if (preferName != _group.preferName) { // 只有一个属性，手动管理dirty
            _group.preferName = preferName;
            EditorUtility.SetDirty(_group);
        }
        EditorGUILayout.Space(10);

        // 布局内打开新窗口会导致Bug
        if (clickRefresh) {
            RefreshSprites();
        }
    }

    private void RefreshSprites() {
        HashSet<Sprite> spriteSet = new HashSet<Sprite>(_group.sprites);
        List<Sprite> tempSprites = new(10);

        // 目录变更不一定需要清理引用，因为关联的文件可能也在目录下，但需要清理不在目录下的文件
        string groupAssetDir = AssetDatabase.GetAssetPath(_group);
        groupAssetDir = groupAssetDir.Substring(0, groupAssetDir.LastIndexOf('/'));
        for (int index = 0; index < _group.sprites.Length; index++) {
            Sprite sprite = _group[index];
            if (!sprite) continue;

            string assetPath = AssetDatabase.GetAssetPath(sprite);
            if (assetPath.StartsWith(groupAssetDir) && assetPath.IndexOf('/', groupAssetDir.Length) <= 0) {
                continue; // 仍在当前目录下
            }
            _group[index] = null; // 只增不删，保持索引稳定
        }
        // 其实可以填充到null元素所在的位置，我们暂不优化 - dataPath是以assets结尾的
        string groupDir = UnityHelper.ConvertToFilePath(groupAssetDir);
        foreach (string filePath in Directory.GetFiles(groupDir)) {
            if (!UnityHelper.IsImageFile(filePath)) {
                continue;
            }
            string assetPath = UnityHelper.ConvertToAssetPath(filePath);
            Sprite sprite = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Sprite)) as Sprite;
            if (!sprite) {
                continue;
            }
            if (!spriteSet.Add(sprite)) {
                continue;
            }
            tempSprites.Add(sprite);
        }
        // 图片通常命名为数字类型，尽量让数字小的排前面
        tempSprites.Sort(CompareSprite);
        ArrayUtility.AddRange(ref _group.sprites, tempSprites.ToArray());
        EditorUtility.SetDirty(_group);
    }

    private static int CompareSprite(Sprite a, Sprite b) {
        string nameA = a.name;
        string nameB = b.name;
        // 如果都是数字，则按照数字排序
        bool b1 = int.TryParse(nameA, out int num1);
        bool b2 = int.TryParse(nameB, out int num2);
        if (b1 && b2) {
            return num1.CompareTo(num2);
        }
        // 数字排普通字符串前面
        if (b1) return -1;
        if (b2) return 1;
        // 否则按照字符串排序
        return string.Compare(nameA, nameB, StringComparison.Ordinal);
    }
}
}