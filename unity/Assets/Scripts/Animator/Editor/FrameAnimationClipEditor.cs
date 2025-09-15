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
using System.Linq;
using UnityEditor;
using UnityEngine;
using Wjybxx.BigCat.Animator;
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;
using Object = UnityEngine.Object;

namespace Wjybxx.BigCat.AnimatorEditor
{
/// <summary>
/// 序列帧动画编辑器
/// </summary>
[CustomEditor(typeof(FrameAnimationClip))]
public class FrameAnimationClipEditor : Editor
{
    private FrameAnimationClip _clip;
    private FrameAnimationPreviewer _previewer;

    private float _interval = 0.1f;
    private int _frameCount;
    private static bool _listMode = false; // 静态字段保留状态

    private Vector2 scrollPos;
    private GUILayoutOption[] scrollOptions;
    private GUIContent contentIndex;
    private GUIContent contentMoveUp;
    private GUIContent contentMoveDown;
    private GUIContent contentDelete;

    //
    private static string lastFilePath;
    // private static string lastFolderPath;

    private void Awake() {
        _clip = (FrameAnimationClip)target;
        _previewer = new FrameAnimationPreviewer(this, _clip);
        _frameCount = _clip.frames.Length;

        scrollOptions = new[]
        {
            GUILayout.MaxHeight(440),
            // GUILayout.ExpandHeight(true),
        };
        contentIndex = new GUIContent();
        contentMoveUp = new GUIContent("MoveUp");
        contentMoveDown = new GUIContent("MoveDown");
        contentDelete = new GUIContent("Delete");
    }

    // reload以后会调用OnEnable
    private void OnEnable() {
        _previewer = new FrameAnimationPreviewer(this, _clip);
    }

    private void OnDestroy() {
        _clip = null;
    }

    public override void OnInspectorGUI() {
        GUI.enabled = !_previewer.IsPlaying;
        base.OnInspectorGUI();
        EditorGUILayout.LabelField(SEPARATOR);

        // 批量修改帧时间
        EditorGUILayout.BeginHorizontal();
        _interval = EditorGUILayout.FloatField("批量·帧间隔", _interval);
        _interval = Math.Max(0.01f, _interval);
        if (GUILayout.Button("Apply")) {
            _clip.SetFrameInterval(_interval);
            EditorUtility.SetDirty(_clip);
        }
        EditorGUILayout.EndHorizontal();

        // 设置帧数 - 数组长度
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("动画帧数");
        //
        string controlName = "_frameCount";
        GUI.SetNextControlName(controlName);
        _frameCount = EditorGUILayout.IntField(_frameCount);
        _frameCount = Math.Max(0, _frameCount);
        if (GUILayout.Button("Apply")) {
            _clip.FrameCount = _frameCount;
            EditorUtility.SetDirty(_clip);
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.LabelField(SEPARATOR);
        // 退出输入状态重置
        if (GUI.GetNameOfFocusedControl() != controlName) {
            _frameCount = _clip.FrameCount;
        }

        // 帧图
        DrawRawImages();
        EditorGUILayout.LabelField(SEPARATOR);
        GUI.enabled = true;

        // 拖拽添加区
        DrawDragArea();
        EditorGUILayout.LabelField(SEPARATOR);

        // 绘制播放区
        _previewer.OnInspectorGUI();
    }

    #region draw-dragArea

    /// <summary>
    /// 本来是想拖动文件夹到方块的，但框框画得不好看，拖拽也容易误操作退出Inspector界面
    /// </summary>
    private void DrawDragArea() {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("添加文件")) {
            lastFilePath = EditorUtility.OpenFilePanel("添加图片文件", lastFilePath, "");
            string assetPath = lastFilePath.Replace(Application.dataPath, "Assets");
            Sprite sprite = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Sprite)) as Sprite;
            if (sprite) {
                _clip.AddFrame(new AnimationFrame(sprite, 0.1f));
                EditorUtility.SetDirty(_clip);
                // Repaint(); // Repaint the editor window to show the new selection
            }
        }
        if (GUILayout.Button("添加文件夹")) {
            lastFilePath = EditorUtility.OpenFolderPanel("添加文件夹", lastFilePath, "");
            if (!string.IsNullOrEmpty(lastFilePath)) { // 空白表取消
                AddFreamsByFolder(lastFilePath);
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private void AddFreamsByFolder(string folderPath) {
        // string assetPath = folderPath.Replace(Application.dataPath, "Assets");
        // AssetDatabase.LoadAllAssetsAtPath(assetPath); 不好使...
        List<AnimationFrame> frames = new List<AnimationFrame>(10);
        foreach (string filePath in Directory.GetFiles(folderPath)) {
            if (!filePath.EndsWith(".png") && !filePath.EndsWith(".jpg")) {
                continue;
            }
            string assetPath = filePath.Replace(Application.dataPath, "Assets");
            Sprite sprite = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Sprite)) as Sprite;
            if (sprite) {
                frames.Add(new AnimationFrame(sprite, 0.1f));
            }
        }
        _clip.AddFrames(frames);
        EditorUtility.SetDirty(_clip);
    }

    #endregion

    #region draw-iamges

    /// <summary>
    /// 绘制原始图片
    /// </summary>
    private void DrawRawImages() {
        EditorGUILayout.BeginHorizontal();
        _listMode = EditorGUILayout.Toggle("列表模式", _listMode);
        if (GUILayout.Button("去重")) {
            Distinct();
        }
        if (GUILayout.Button("排序 ↑")) {
            Array.Sort(_clip.frames, (a, b) => CompareFrame(a, b, 1));
            EditorUtility.SetDirty(_clip);
        }
        if (GUILayout.Button("排序 ↓")) {
            Array.Sort(_clip.frames, (a, b) => CompareFrame(a, b, -1));
            EditorUtility.SetDirty(_clip);
        }
        EditorGUILayout.EndHorizontal();

        // 有滚动条的情况下，无需折叠
        // foldout = EditorGUILayout.Foldout(foldout, foldout ? "折叠" : "展开");
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.BeginVertical();
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, scrollOptions);
        for (int index = 0, len = _clip.FrameCount; index < len; index++) {
            EditorGUILayout.LabelField(GetElementName(index), index > 0 ? SEPARATOR : null);
            AnimationFrame animationFrame = _clip[index];
            if (_listMode) {
                EditorGUILayout.BeginHorizontal();
                Sprite sprite = (Sprite)EditorGUILayout.ObjectField(animationFrame.sprite, typeof(Sprite), false);
                float duration = EditorGUILayout.FloatField("duration", animationFrame.duration);
                duration = Math.Max(0.01f, duration);
                _clip[index] = new AnimationFrame(sprite, duration);
                EditorGUILayout.EndHorizontal();
            } else {
                EditorGUILayout.BeginVertical();
                // ObjectFiled指定label时有预览图
                Sprite sprite = (Sprite)EditorGUILayout.ObjectField("sprite", animationFrame.sprite, typeof(Sprite), false);
                float duration = EditorGUILayout.FloatField("duration", animationFrame.duration);
                duration = Math.Max(0.01f, duration);
                _clip[index] = new AnimationFrame(sprite, duration);
                EditorGUILayout.EndVertical();
            }
            // 右键菜单
            if (Event.current.type == EventType.ContextClick &&
                GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition)) {
                Event.current.Use();
                GenericMenu menu = new GenericMenu();
                // 标注选择的元素
                contentIndex.text = GetElementName(index);
                menu.AddDisabledItem(contentIndex);
                menu.AddSeparator("");
                //
                menu.AddItem(contentMoveUp, false, OnClickMoveUp, index);
                menu.AddItem(contentMoveDown, false, OnClickMoveDown, index);
                menu.AddItem(contentDelete, false, OnClickDelete, index);
                menu.ShowAsContext();
            }
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
        //
        if (EditorGUI.EndChangeCheck()) {
            // animationClip.RefreshDuration();
            EditorUtility.SetDirty(_clip);
        }
    }

    private void Distinct() {
        HashSet<Sprite> sprites = new(_clip.FrameCount);
        List<AnimationFrame> frames = new(_clip.FrameCount);
        foreach (AnimationFrame frame in _clip.frames) {
            if (frame.sprite && sprites.Add(frame.sprite)) {
                frames.Add(frame);
            }
        }
        _clip.frames = frames.ToArray();
        _clip.RefreshDuration();
        EditorUtility.SetDirty(_clip);
    }

    private static int CompareFrame(AnimationFrame a, AnimationFrame b, int sign) {
        if (a.sprite && b.sprite) {
            string nameA = a.sprite.name;
            string nameB = b.sprite.name;
            // 如果都是数字，则按照数字排序
            if (int.TryParse(nameA, out int num1) && int.TryParse(nameB, out int num2)) {
                return sign * num1.CompareTo(num2);
            }
            // 否则按照字符串排序
            return sign * string.Compare(nameA, nameB, StringComparison.Ordinal);
        }
        return a.sprite ? -1 : 1; // 无效帧排尾部
    }

    private void OnClickMoveUp(object obj) {
        int index = (int)obj;
        if (index > 0) {
            _clip.frames.Swap(index, index - 1);
            EditorUtility.SetDirty(_clip);
        }
    }

    private void OnClickMoveDown(object obj) {
        int index = (int)obj;
        if (index < _clip.FrameCount - 1) {
            _clip.frames.Swap(index, index + 1);
            EditorUtility.SetDirty(_clip);
        }
    }

    private void OnClickDelete(object obj) {
        int index = (int)obj;
        if (index < _clip.FrameCount) {
            _clip.RemoveFrame(index);
            EditorUtility.SetDirty(_clip);
        }
    }

    #endregion

    #region cache

    private static readonly string[] elementNameCache = new string[100];
    private const string SEPARATOR = "-----------------------------------------------------------------------------";

    static FrameAnimationClipEditor() {
        for (int i = 0; i < elementNameCache.Length; i++) {
            elementNameCache[i] = "Frame: " + i;
        }
    }

    private static string GetElementName(int index) {
        return index < elementNameCache.Length ? elementNameCache[index] : "Frame: " + index;
    }

    #endregion
}
}