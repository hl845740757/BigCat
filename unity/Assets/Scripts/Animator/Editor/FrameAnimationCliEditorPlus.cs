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
using Object = System.Object;

namespace Wjybxx.BigCat.AnimatorEditor
{
/// <summary>
/// 帧动画编辑器增强版。
/// 
/// 注：使用独立的窗口打开帧动画编辑器，方便快速切换动画。
/// </summary>
public class FrameAnimationCliEditorPlus : EditorWindow
{
    private FrameAnimationClip _clip;
    private FrameAnimationClipEditor _clipEditor;
    private FrameAnimationPreviewer _previewer;

    private static readonly string[] toolBarNames = new[] { "编辑工具", "同步工具" };
    private int _toolIndex;
    private Vector2 scrollPos;

    private GUIContent _pooledLabel;
    private GUILayoutOption[] _syncListOptions;
    private List<FrameAnimationClip> _syncList = new List<FrameAnimationClip>();

    private GameObject _rootObject;
    private int _rootObjectId;
    private FrameAnimationPreviewer _rootPreviewer;

    [MenuItem("Window/BigCat/FAnimClipEditor")]
    private static void OpenWindow() {
        FrameAnimationCliEditorPlus win = GetWindow<FrameAnimationCliEditorPlus>("帧动画编辑器");
        win.minSize = new Vector2(400, 600);
        win.Show();
    }

    private GUIContent PooledLabel() => _pooledLabel.Reset();

    private void OnEnable() {
        if (_clip) {
            _clipEditor = (FrameAnimationClipEditor)Editor.CreateEditor(_clip, typeof(FrameAnimationClipEditor));
            _clipEditor.DisablePreviewer();
        }
        // 方便reload
        _previewer = new FrameAnimationPreviewer(_clip);
        _rootPreviewer = new FrameAnimationPreviewer(null);
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
        var clip = EditorGUILayout.ObjectField("clip", _clip, typeof(FrameAnimationClip), true) as FrameAnimationClip;
        if (clip != _clip) {
            _clip = clip;
            // 重建Editor对象
            if (_clipEditor) {
                DestroyImmediate(_clipEditor);
            }
            _clipEditor = (FrameAnimationClipEditor)Editor.CreateEditor(clip, typeof(FrameAnimationClipEditor));
            _clipEditor.DisablePreviewer();
            _previewer.Clip = clip;
        }
        if (clip) {
            _clipEditor.OnInspectorGUI();
            _previewer.OnInspectorGUI();
            // 动画播放期间需要持续绘制
            if (_previewer.IsPlaying) {
                Repaint();
            }
        }
        EditorGUILayout.EndScrollView();
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
            FrameAnimationClip animationClip = _syncList[index];
            animationClip = (FrameAnimationClip)EditorGUILayout.ObjectField(animationClip, typeof(FrameAnimationClip), false);
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
            FrameAnimationClip animationClip = _syncList[moveTopIndex];
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
        if (GUILayout.Button("同步间隔")) {
            SyncFrameInterval();
        }
        if (GUILayout.Button("同步序列")) {
            SyncFrameSprite();
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
            string assetPath = SystemExtensions.ConvertToAssetPath(filePath);
            if (AssetDatabase.LoadAssetAtPath(assetPath, typeof(FrameAnimationClip)) is FrameAnimationClip clip
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
        FrameAnimationClip baseClip = _syncList[0];
        _rootPreviewer.Clip = baseClip;
        _rootPreviewer.Renderer = GetChildRenderer(baseClip.name);
        _rootPreviewer.OrderInLayer = 0;
        //
        for (int index = 1; index < _syncList.Count; index++) {
            FrameAnimationClip clip = _syncList[index];
            FrameAnimationPreviewer follower = new FrameAnimationPreviewer(clip);
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

    /// <summary>
    /// 按照基础动画的帧间隔，设置其它动画的帧间隔
    /// </summary>
    private void SyncFrameInterval() {
        if (_syncList.Count <= 1) return;
        FrameAnimationClip baseClip = _syncList[0];
        for (int index = 1; index < _syncList.Count; index++) {
            FrameAnimationClip animationClip = _syncList[index];
            if (!animationClip) {
                continue;
            }
            FrameAnimationClip.SyncFrameDuration(baseClip, animationClip);
            EditorUtility.SetDirty(animationClip);
        }
    }

    /// <summary>
    /// 按照基础动画的图片(名)序列重排序其它动画的图片序列
    /// </summary>
    private void SyncFrameSprite() {
        if (_syncList.Count <= 1) return;
        FrameAnimationClip baseClip = _syncList[0];
        for (int index = 1; index < _syncList.Count; index++) {
            FrameAnimationClip animationClip = _syncList[index];
            if (animationClip) {
                continue;
            }
            FrameAnimationClip.SyncFrameOrder(baseClip, animationClip);
            EditorUtility.SetDirty(animationClip);
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