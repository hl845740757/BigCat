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
using Wjybxx.BigCat.CoreEditor;
using Wjybxx.BigCat.CoreEditor.Core.Editor;
using Wjybxx.BigCat.UnityCore;
using Wjybxx.Commons;
using Object = System.Object;

namespace Wjybxx.BigCat.AnimatorEditor
{
/// <summary>
/// 帧动画编辑器增强版。
/// 
/// 注：使用独立的窗口打开帧动画编辑器，方便快速切换动画。
/// </summary>
public class SpriteAnimationClipEditor : EditorWindow
{
    private static readonly string[] toolBarNames = new[] { "编辑工具", "预览&同步工具" };
    private int _toolIndex;

    private SpriteAnimationClip _clip;
    private float _interval = 0.1f;
    private Vector2 _position;
    private float _rotation;
    private Vector2 _scrollPos;
    private int _frameIndex = 0; // 当前编辑帧号
    private GUILayoutOption[] _imageAreaOptions;
    private bool _hurtBoxFoldout;
    private Vector2 _hurtBoxScrollPos;
    private bool _damageFoldout;
    private Vector2 _damageScrollPos;
    private SpriteAnimationPreviewer _previewer;

    private readonly List<SpriteAnimationClip> _syncList = new List<SpriteAnimationClip>();
    private GUILayoutOption[] _syncListOptions;
    private SpriteAnimationPreviewer _rootPreviewer;
    private readonly GUIContent _pooledLabel = new GUIContent();

    private GameObject _rootObject;
    private int _rootObjectId;
    private GUILayoutOption[] _width100;
    private GUILayoutOption[] _width50;
    private GUILayoutOption[] _width20;
    private GUILayoutOption[] _noExpand;

    [MenuItem("Window/BigCat/SpriteAnimClipEditor")]
    private static void OpenWindow() {
        SpriteAnimationClipEditor win = GetWindow<SpriteAnimationClipEditor>("帧动画编辑器");
        win.minSize = new Vector2(400, 600);
        win.Show();
    }

    private GUIContent PooledLabel() => _pooledLabel.Reset();

    private void OnEnable() {
        _previewer = new SpriteAnimationPreviewer(_clip);
        _rootPreviewer = new SpriteAnimationPreviewer();
        _rootPreviewer.OnPlayRequested = LoadClipSprites;
        // 方便reload
        _imageAreaOptions = new[] { GUILayout.MinHeight(100), GUILayout.ExpandHeight(true) };
        _syncListOptions = new[] { GUILayout.MinHeight(300), GUILayout.ExpandHeight(true) };
        _width100 = new GUILayoutOption[] { GUILayout.Width(100) };
        _width50 = new GUILayoutOption[] { GUILayout.Width(50) };
        _width20 = new GUILayoutOption[] { GUILayout.Width(20) };
        _noExpand = new GUILayoutOption[] { GUILayout.ExpandHeight(false) };
    }

    private void OnGUI() {
        _toolIndex = GUILayout.Toolbar(_toolIndex, toolBarNames);
        if (_toolIndex == 0) {
            DrawClipEditor();
        } else if (_toolIndex == 1) {
            DrawClipSyncEditor();
        }
    }

    private void Update() {
        if (_toolIndex == 0) {
            if (_previewer.IsPlaying) {
                _previewer.Update();
            }
        } else if (_toolIndex == 1) {
            if (_rootPreviewer.IsPlaying) {
                _rootPreviewer.Update();
            }
        }
    }

    #region clip-editor

    /// <summary>
    /// 单个动画的编辑工具
    /// </summary>
    private void DrawClipEditor() {
        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
        var clip = EditorGUILayout.ObjectField("clip", _clip, typeof(SpriteAnimationClip), true) as SpriteAnimationClip;
        if (clip != _clip) {
            _clip = clip;
            _previewer.Clip = clip;
        }
        if (!clip) {
            EditorGUILayout.EndScrollView();
            return;
        }

        GUI.enabled = !_previewer.IsPlaying;
        EditorGUIUtility.labelWidth = 150;
        EditorGUILayout.FloatField("Duration", _clip.duration);
        _clip.loop = EditorGUILayout.Toggle("Loop", _clip.loop);
        _clip.weight = EditorGUILayout.FloatField("Weight", _clip.weight);
        UnityHelper.DrawSeparator();

        // 批量修改帧偏移
        EditorGUILayout.BeginHorizontal();
        _position = UnityHelper.DrawVector2("批量·帧坐标", _position);
        if (GUILayout.Button("Add", _width100)) {
            _clip.AddFramePosition(_position);
            EditorUtility.SetDirty(_clip);
        }
        if (GUILayout.Button("Set", _width100)) {
            _clip.SetFramePosition(_position);
            EditorUtility.SetDirty(_clip);
        }
        if (GUILayout.Button("Lerp", _width100)) {
            _clip.LerpFramePosition(_position);
            EditorUtility.SetDirty(_clip);
        }
        EditorGUILayout.EndHorizontal();

        // 批量修改帧旋转
        EditorGUILayout.BeginHorizontal();
        _rotation = EditorGUILayout.FloatField("批量·帧旋转", _rotation);
        if (GUILayout.Button("Add", _width100)) {
            _clip.AddFrameRotation(_rotation);
            EditorUtility.SetDirty(_clip);
        }
        if (GUILayout.Button("Set", _width100)) {
            _clip.SetFrameRotation(_rotation);
            EditorUtility.SetDirty(_clip);
        }
        if (GUILayout.Button("Lerp", _width100)) {
            _clip.LerpFrameRotation(_rotation);
            EditorUtility.SetDirty(_clip);
        }
        EditorGUILayout.EndHorizontal();

        // 批量修改帧时间
        EditorGUILayout.BeginHorizontal();
        _interval = EditorGUILayout.FloatField("批量·帧时长", _interval);
        _interval = Math.Max(0.01f, _interval);
        if (GUILayout.Button("Apply", _width100)) {
            _clip.SetFrameInterval(_interval);
            EditorUtility.SetDirty(_clip);
        }
        EditorGUILayout.EndHorizontal();

        // 帧图
        UnityHelper.DrawSeparator();
        EditorGUI.BeginChangeCheck();
        DrawFrames();
        if (EditorGUI.EndChangeCheck()) {
            EditorUtility.SetDirty(_clip);
        }
        GUI.enabled = true;
        // 事件
        // UnityHelper.DrawSeparator();
        // DrawEvents();

        // 绘制播放区 - 布局太紧凑考虑切页预览
        // UnityHelper.DrawSeparator();
        // // 更新UI之前更新动画
        // if (_previewer.IsPlaying) {
        //     _previewer.Update();
        // }
        // _previewer.OnInspectorGUI();
        EditorGUILayout.EndScrollView();

        // 持续绘制模拟Update
        if (_previewer.IsPlaying) {
            Repaint();
        }
    }

    /// <summary>
    /// 更改为翻页时预览
    /// </summary>
    private void DrawFrames() {
        EditorGUILayout.BeginHorizontal();
        // 当前帧数
        string controlName = "_count";
        GUI.SetNextControlName(controlName);
        int tempCount = Math.Max(0, EditorGUILayout.DelayedIntField("FrameCount", _clip.FrameCount));
        if (tempCount != _clip.FrameCount && GUI.GetNameOfFocusedControl() == controlName) {
            _clip.FrameCount = tempCount;
            EditorUtility.SetDirty(_clip);
        }
        if (GUILayout.Button("+", _width20)) {
            var lastFrame = _clip.FrameCount > 0 ? _clip.LastFrame : null;
            _clip.FrameCount++;
            InheritFrameProps(lastFrame, _clip.LastFrame); // 继承常用属性
            EditorUtility.SetDirty(_clip);
        }
        GUI.enabled = _clip.FrameCount > 0;
        if (GUILayout.Button("-", _width20)) {
            _clip.FrameCount--;
            EditorUtility.SetDirty(_clip);
        }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        Rect lastRect = GUILayoutUtility.GetLastRect();
        Rect imageRect = new Rect(lastRect.x, lastRect.yMax + 2, lastRect.width, 200);
        // 绘制图片 - 不使用自动布局，避免图片自动调整
        _frameIndex = ClampFrameIndex(_frameIndex, _clip.FrameCount);
        if (_frameIndex < _clip.FrameCount) {
            var frame = _clip[_frameIndex];
            Sprite sprite = frame.sprite;
            if (!sprite) {
                sprite = frame.sprite = SpritePathEditor.LoadSprite(frame.spritePath);
            }
            if (sprite) {
                EditorGUI.DrawTextureTransparent(imageRect, sprite.texture, ScaleMode.ScaleToFit);
            }
        }
        EditorGUILayout.Space(imageRect.height - 18); // 切页按钮显示在图片右下角

        // 切页按钮
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("");
        // 左翻页
        GUI.enabled = _frameIndex > 0;
        if (GUILayout.Button("◁", _width20)) {
            _frameIndex--;
        }
        GUI.enabled = true;
        // 当前帧索引
        controlName = "_frameIndex";
        GUI.SetNextControlName(controlName);
        int tempIndex = EditorGUILayout.DelayedIntField(_frameIndex, _width50);
        if (tempCount != _frameIndex && GUI.GetNameOfFocusedControl() == controlName) {
            _frameIndex = tempIndex;
        }
        // 右翻页
        GUI.enabled = _frameIndex + 1 < _clip.FrameCount;
        if (GUILayout.Button("▷", _width20)) {
            _frameIndex++;
        }
        GUILayout.Space(10);
        // 删除按钮
        GUI.enabled = _frameIndex < _clip.FrameCount;
        if (GUILayout.Button("Del", _width50)) {
            _clip.RemoveFrame(_frameIndex);
            _frameIndex = ClampFrameIndex(_frameIndex, _clip.FrameCount);
        }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        // 绘制帧信息
        EditorGUILayout.BeginVertical();
        if (_frameIndex < _clip.FrameCount) {
            var frame = _clip[_frameIndex];
            DrawFrameDetails(frame);
        }
        EditorGUILayout.EndVertical();
    }

    private static void InheritFrameProps(SpriteAnimationFrame srcFrame,
                                          SpriteAnimationFrame targetFrame) {
        if (srcFrame == null) return;
        targetFrame.spritePath = srcFrame.spritePath;
        targetFrame.spritePath.index++;
        targetFrame.duration = srcFrame.duration;
    }

    private static int ClampFrameIndex(int frameIndex, int frameCount) {
        return frameCount == 0 ? 0 : Math.Clamp(frameIndex, 0, frameCount - 1);
    }

    private void DrawFrameDetails(SpriteAnimationFrame frame) {
        GUIContent label = PooledLabel().WithText("SpritePath");
        SpritePath spritePath = SpritePathEditor.DoLayout(frame.spritePath, label, out bool exitGui);
        if (frame.spritePath != spritePath) {
            frame.spritePath = spritePath;
            frame.sprite = SpritePathEditor.LoadSprite(spritePath);
        }
        if (exitGui) {
            GUIUtility.ExitGUI();
        }

        frame.position = UnityHelper.DrawVector2("Position", frame.position);
        frame.rotation = EditorGUILayout.FloatField("Rotation", frame.rotation);

        float duration = Mathf.Max(0, EditorGUILayout.FloatField("Duration", frame.duration));
        if (!Mathf.Approximately(duration, frame.duration)) {
            frame.duration = duration;
            _clip.RefreshDuration();
        }
        GUI.enabled = false;
        EditorGUILayout.FloatField("EndTime", frame.endTime); // 编辑器下预览
        GUI.enabled = true;

        frame.graphic = EditorGUILayout.IntField(label.Reset().WithText("Graphic", "攻击盒形状，动态绘制"), frame.graphic);
        frame.interp = EditorGUILayout.IntField(label.Reset().WithText("Interp", "插值函数"), frame.interp);

        // 攻击包围盒
        DrawBoxes(ref _hurtBoxFoldout, ref _hurtBoxScrollPos, ref frame.hurtBoxes, false,
            label.Reset().WithText("HurtBoxes", "受击包围盒"));
        DrawBoxes(ref _damageFoldout, ref _damageScrollPos, ref frame.damageBoxes, true,
            label.Reset().WithText("DmgBoxes", "攻击包围盒"));
    }

    private void DrawBoxes(ref bool isExpanded, ref Vector2 scrollPos, ref AABB[] boxes,
                           bool isDamageBoxes, GUIContent label) {
        EditorGUILayout.BeginHorizontal();
        isExpanded = EditorGUILayout.Foldout(isExpanded, label);

        const int maxBox = 50; // 限制最大数量
        string controlName = label.text;
        GUI.SetNextControlName(controlName);
        int count = Mathf.Clamp(EditorGUILayout.DelayedIntField(boxes.Length), 0, maxBox);
        if (count != boxes.Length) {
            Array.Resize(ref boxes, count);
        }
        GUI.enabled = boxes.Length < maxBox;
        if (GUILayout.Button("+", _width20)) {
            Array.Resize(ref boxes, boxes.Length + 1);
        }
        GUI.enabled = boxes.Length > 0;
        if (GUILayout.Button("-", _width20)) {
            Array.Resize(ref boxes, boxes.Length - 1);
        }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, _noExpand);
        int indentLevel = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 1;
        for (int index = 0; index < boxes.Length; index++) {
            AABB aabb = boxes[index];
            //
            label = PooledLabel().WithText(UnityHelper.GetElementName(index));
            boxes[index] = AABBEditor.DoLayout(aabb, ref isExpanded, label);
            //
            if (UnityHelper.IsContextClickEvent(Event.current, GUILayoutUtility.GetLastRect())) {
                Event.current.Use();
                ShowBoxContextMenu(index, isDamageBoxes);
            }
        }
        EditorGUI.indentLevel = indentLevel;
        EditorGUILayout.EndScrollView();
    }

    private void ShowBoxContextMenu(int boxIndex, bool isDamageBoxes) {
        GenericMenu menu = new GenericMenu();
        menu.AddItem(new GUIContent("Delete"), false, _ => {
            SpriteAnimationFrame frame = _clip[_frameIndex];
            if (isDamageBoxes) {
                ArrayUtility.RemoveAt(ref frame.damageBoxes, boxIndex);
            } else {
                ArrayUtility.RemoveAt(ref frame.hurtBoxes, boxIndex);
            }
        }, null);
        menu.ShowAsContext();
    }

    #endregion

    #region sync-area

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
        UnityHelper.DrawSeparator();

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
        UnityHelper.DrawSeparator();

        // 播放区
        EditorGUILayout.HelpBox(PooledLabel().WithText("设置Root后可播放"));
        EditorGUILayout.BeginHorizontal();
        _rootObject = (GameObject)EditorGUILayout.ObjectField("Root", _rootObject, typeof(GameObject), true);
        if (GUILayout.Button("Bind")) {
            BindRootObject();
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

    private void BindRootObject() {
        if (!_rootObject) return;
        if (_rootObjectId != _rootObject.GetInstanceID()) {
            const string message = "播放动画时会在目标GameObject下创建子对象，请确保目标GameObject是临时对象";
            if (!EditorUtility.DisplayDialog("二次确认", message, "确认", "取消 ")) {
                return;
            }
            _rootObjectId = _rootObject.GetInstanceID();
        }
        if (_syncList.Count == 0) {
            return;
        }
        // 初始化Render和Previewer
        Transform rootTransform = _rootObject.transform;
        while (rootTransform.childCount < _syncList.Count) {
            GameObject child = new GameObject("Render: " + rootTransform.childCount);
            child.AddComponent<SpriteRenderer>();
            child.transform.SetParent(rootTransform);
        }
        while (_rootPreviewer.Followers.Count + 1 < _syncList.Count) {
            _rootPreviewer.Followers.Add(new SpriteAnimationPreviewer());
        }
        _rootPreviewer.Clip = _syncList[0];
        _rootPreviewer.Renderer = rootTransform.GetChild(0).GetComponent<SpriteRenderer>();
        _rootPreviewer.OrderInLayer = 0;
        for (int index = 1; index < _syncList.Count; index++) {
            SpriteAnimationClip clip = _syncList[index];
            SpriteAnimationPreviewer follower = _rootPreviewer.Followers[index - 1];
            follower.Renderer = rootTransform.GetChild(index).GetComponent<SpriteRenderer>();
            follower.Clip = clip;
            follower.OrderInLayer = 1; // 其它覆盖在上面
            _rootPreviewer.AddFollower(follower);
        }
    }

    private bool LoadClipSprites(SpriteAnimationPreviewer _) {
        if (_syncList.Count == 0) {
            return false;
        }
        foreach (SpriteAnimationClip clip in _syncList) {
            foreach (SpriteAnimationFrame frame in clip.frames) {
                frame.sprite = SpritePathEditor.LoadSprite(frame.spritePath);
            }
            clip.RefreshDuration();
        }
        return true;
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

    #endregion
}
}