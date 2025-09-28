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
using Wjybxx.BigCat.Animator;
using Wjybxx.BigCat.UnityCore;
using Object = System.Object;

namespace Wjybxx.BigCat.AnimatorEditor
{
/// <summary>
/// 帧动画编辑器增强版。
/// 
/// 注：使用独立的窗口打开帧动画编辑器，方便快速切换动画。
/// </summary>
public class SpriteAnimationClipEditorPlus : EditorWindow
{
    private SpriteAnimationClip _clip;
    private SpriteAnimationClipEditor _clipEditor;
    private SpriteAnimationPreviewer _previewer;

    private static readonly string[] toolBarNames = new[] { "编辑工具", "同步工具" };
    private int _toolIndex;
    private Vector2 scrollPos;

    private GUIContent _pooledLabel;
    private GUILayoutOption[] _syncListOptions;
    private List<SpriteAnimationClip> _syncList = new List<SpriteAnimationClip>();

    private GameObject _rootObject;
    private int _rootObjectId;
    private SpriteAnimationPreviewer _rootPreviewer;

    [MenuItem("Window/BigCat/SpriteAnimClipEditor")]
    private static void OpenWindow() {
        SpriteAnimationClipEditorPlus win = GetWindow<SpriteAnimationClipEditorPlus>("帧动画编辑器");
        win.minSize = new Vector2(400, 600);
        win.Show();
    }

    private GUIContent PooledLabel() => _pooledLabel.Reset();

    private void OnEnable() {
        if (_clip) {
            _clipEditor = (SpriteAnimationClipEditor)Editor.CreateEditor(_clip, typeof(SpriteAnimationClipEditor));
            _clipEditor.DisablePreviewer();
        }
        // 方便reload
        _previewer = new SpriteAnimationPreviewer(_clip);
        _rootPreviewer = new SpriteAnimationPreviewer();
        //
        _pooledLabel = new GUIContent();
        _syncListOptions = new GUILayoutOption[] { GUILayout.MinHeight(300), GUILayout.ExpandHeight(true) };
    }

    private void OnDisable() {
        if (_clipEditor) {
            DestroyImmediate(_clipEditor);
            _clipEditor = null;
        }
    }

    private void OnGUI() {
        _toolIndex = GUILayout.Toolbar(_toolIndex, toolBarNames);
        if (_toolIndex == 0) {
            DrawClipEditor();
        } else if (_toolIndex == 1) {
            DrawClipSyncEditor();
        }
    }

    private static void DrawSeparator() {
        Rect rect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(rect, Color.gray);
        // EditorGUILayout.LabelField(SEPARATOR);
    }

    /// <summary>
    /// 单个动画的编辑工具
    /// </summary>
    private void DrawClipEditor() {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        var clip = EditorGUILayout.ObjectField("clip", _clip, typeof(SpriteAnimationClip), true) as SpriteAnimationClip;
        if (clip != _clip) {
            _clip = clip;
            // 重建Editor对象
            if (_clipEditor) {
                DestroyImmediate(_clipEditor);
            }
            _clipEditor = (SpriteAnimationClipEditor)Editor.CreateEditor(clip, typeof(SpriteAnimationClipEditor));
            _clipEditor.DisablePreviewer();
            _previewer.Clip = clip;
        }
        if (clip) {
            _clipEditor.OnInspectorGUI();
            _previewer.OnInspectorGUI();
        }
        EditorGUILayout.EndScrollView();

        // 动画播放期间需要持续绘制
        if (_previewer.IsPlaying) {
            Repaint();
        }
    }

    /// <summary>
    /// 多个帧动画的同步工具
    /// </summary>
    private void DrawClipSyncEditor() {
        EditorGUILayout.HelpBox(PooledLabel().WithText("同步队列：(拖拽到列表区添加)"));
        EditorGUILayout.BeginVertical(_syncListOptions);
        int moveTopIndex = -1;
        int deleteIndex = -1;
        for (int index = 0; index < _syncList.Count; index++) {
            EditorGUILayout.BeginHorizontal();
            SpriteAnimationClip animationClip = _syncList[index];
            animationClip = (SpriteAnimationClip)EditorGUILayout.ObjectField(animationClip, typeof(SpriteAnimationClip), false);
            _syncList[index] = animationClip;
            if (GUILayout.Button("置顶") && Event.current.button == 0) {
                moveTopIndex = index;
            }
            if (GUILayout.Button("删除") && Event.current.button == 0) {
                deleteIndex = index;
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
        Rect controlRect = GUILayoutUtility.GetLastRect();

        // 循环外处理移动和删除事件
        if (moveTopIndex >= 0) {
            SpriteAnimationClip animationClip = _syncList[moveTopIndex];
            _syncList.RemoveAt(moveTopIndex);
            _syncList.Insert(0, animationClip);
            Repaint();
        }
        if (deleteIndex >= 0) {
            _syncList.RemoveAt(deleteIndex);
            Repaint();
        }
        DrawSeparator();

        //
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("同步序列")) {
            SyncFrameSprite();
            GUIUtility.ExitGUI(); // 打开新窗口，中断当前GUI
        }
        if (GUILayout.Button("同步帧长")) {
            SyncFrameInterval();
        }
        if (GUILayout.Button("同步坐标")) {
            SyncFramePosition();
        }
        if (GUILayout.Button("同步旋转")) {
            SyncFrameRotation();
        }
        if (GUILayout.Button("清空列表")) {
            _syncList.Clear();
            Repaint();
        }
        EditorGUILayout.EndHorizontal();
        DrawSeparator();

        // 播放区
        EditorGUILayout.HelpBox(PooledLabel().WithText("选择Root并初始化Render可播放"));
        EditorGUILayout.BeginHorizontal();
        _rootObject = (GameObject)EditorGUILayout.ObjectField("Root", _rootObject, typeof(GameObject), true);
        if (GUILayout.Button("InitRenderers")) {
            InitRenderers();
        }
        EditorGUILayout.EndHorizontal();
        //
        _rootPreviewer.OnInspectorGUI(true);
        if (_rootPreviewer.IsPlaying) {
            Repaint();
        }
        // 检查拖拽事件
        CheckDragAddEvent(controlRect);
    }

    private void CheckDragAddEvent(Rect controlRect) {
        Event evt = Event.current;
        if (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform) return;
        if (!controlRect.Contains(evt.mousePosition)) return;
        //
        DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
        if (evt.type != EventType.DragPerform) return;
        // 拖拽结束 - path是文件全路径
        foreach (string filePath in DragAndDrop.paths) {
            string assetPath = UnityHelper.ConvertToAssetPath(filePath);
            if (AssetDatabase.LoadAssetAtPath(assetPath, typeof(SpriteAnimationClip)) is SpriteAnimationClip clip
                && !_syncList.Contains(clip)) {
                _syncList.Add(clip);
            }
        }
    }

    private void InitRenderers() {
        if (!_rootObject) return;
        if (_rootObjectId != _rootObject.GetInstanceID()) {
            const string message = "该操作会为目标对象创建子对象，请确保目标GameObject是临时对象";
            if (!EditorUtility.DisplayDialog("二次确认", message, "确认", "取消 ")) {
                return;
            }
            _rootObjectId = _rootObject.GetInstanceID();
        }
        // 先清理
        _rootPreviewer.Renderer = null;
        _rootPreviewer.Followers.Clear();
        if (_syncList.Count == 0) {
            return;
        }
        SpriteAnimationClip baseClip = _syncList[0];
        _rootPreviewer.Clip = baseClip;
        _rootPreviewer.Renderer = GetChildRenderer(baseClip.name);
        _rootPreviewer.OrderInLayer = 0;
        //
        for (int index = 1; index < _syncList.Count; index++) {
            SpriteAnimationClip clip = _syncList[index];
            SpriteAnimationPreviewer follower = new SpriteAnimationPreviewer(clip);
            follower.Renderer = GetChildRenderer(clip.name);
            follower.OrderInLayer = 1; // 其它覆盖在上面
            _rootPreviewer.AddFollower(follower);
        }
    }

    private SpriteRenderer GetChildRenderer(string name) {
        Transform transform = _rootObject.transform.Find(name);
        if (transform) {
            transform.gameObject.SetActive(true);
            return transform.gameObject.GetComponent<SpriteRenderer>();
        }
        GameObject child = new GameObject(name);
        child.transform.SetParent(_rootObject.transform);
        return child.AddComponent<SpriteRenderer>();
    }

    private void SyncFrameRotation() {
        if (_syncList.Count <= 1) return;
        SpriteAnimationClip baseClip = _syncList[0];
        for (int index = 1; index < _syncList.Count; index++) {
            SpriteAnimationClip animationClip = _syncList[index];
            if (!animationClip) {
                continue;
            }
            SpriteAnimationClip.SyncFrameRotation(baseClip, animationClip);
            EditorUtility.SetDirty(animationClip);
        }
    }

    private void SyncFramePosition() {
        if (_syncList.Count <= 1) return;
        SpriteAnimationClip baseClip = _syncList[0];
        for (int index = 1; index < _syncList.Count; index++) {
            SpriteAnimationClip animationClip = _syncList[index];
            if (!animationClip) {
                continue;
            }
            SpriteAnimationClip.SyncFramePosition(baseClip, animationClip);
            EditorUtility.SetDirty(animationClip);
        }
    }

    private void SyncFrameInterval() {
        if (_syncList.Count <= 1) return;
        SpriteAnimationClip baseClip = _syncList[0];
        for (int index = 1; index < _syncList.Count; index++) {
            SpriteAnimationClip animationClip = _syncList[index];
            if (!animationClip) {
                continue;
            }
            SpriteAnimationClip.SyncFrameDuration(baseClip, animationClip);
            EditorUtility.SetDirty(animationClip);
        }
    }

    private void SyncFrameSprite() {
        if (_syncList.Count <= 1) return;
        const string message = "该操作将同步第一个动画的帧序信息到其它模型，确定同步吗？";
        if (!EditorUtility.DisplayDialog("二次确认", message, "确定", "取消")) {
            return;
        }
        SpriteAnimationClip baseClip = _syncList[0];
        for (int index = 1; index < _syncList.Count; index++) {
            SpriteAnimationClip animClip = _syncList[index];
            if (!animClip || animClip == baseClip) {
                continue;
            }
            string groupPath = EditorUtility.OpenFilePanel("选择SpriteGroup：" + animClip.name, "", "asset");
            if (string.IsNullOrWhiteSpace(groupPath)) {
                continue;
            }
            groupPath = UnityHelper.ConvertToAssetPath(groupPath);
            SpriteGroup spriteGroup = AssetDatabase.LoadAssetAtPath<SpriteGroup>(groupPath);
            if (spriteGroup) {
                groupPath = spriteGroup.preferName ? spriteGroup.name : AssetDatabase.GetAssetPath(spriteGroup);
                SpriteAnimationClip.SyncFrameOrder(baseClip, animClip, groupPath);
                EditorUtility.SetDirty(animClip);
            }
        }
    }

    private void Update() {
        if (_toolIndex == 0) {
            if (_clipEditor && _previewer.IsPlaying) {
                _previewer.Update();
            }
        } else if (_toolIndex == 1) {
            if (_rootPreviewer.IsPlaying) {
                _rootPreviewer.Update();
            }
        }
    }
}
}