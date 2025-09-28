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
using Wjybxx.BigCat.Animator;
using Wjybxx.BigCat.CoreEditor.Core.Editor;
using Wjybxx.BigCat.UnityCore;
using Wjybxx.Commons;
using Object = UnityEngine.Object;

namespace Wjybxx.BigCat.AnimatorEditor
{
/// <summary>
/// 序列帧动画编辑器
/// </summary>
[CustomEditor(typeof(SpriteAnimationClip))]
public class SpriteAnimationClipEditor : Editor
{
    private SpriteAnimationClip _clip;
    private SpriteAnimationPreviewer _previewer;

    private float _interval = 0.1f;
    private Vector2 _position;
    private float _rotation;
    private int _frameCount;

    private bool _imagesFoldout = true;
    private Vector2 _imagesScrollPos;
    private GUILayoutOption[] scrollOptions;
    private GUILayoutOption[] _width150;
    private GUILayoutOption[] _width100;
    private GUILayoutOption[] _width20;
    //
    private readonly GUIContent _pooledLabel = new GUIContent();

    private GUIContent PooledLabel() => _pooledLabel.Reset();

    private void Awake() {
        _clip = (SpriteAnimationClip)target;
        _previewer = new SpriteAnimationPreviewer(_clip);
        _frameCount = _clip.frames.Length;
        scrollOptions = new[]
        {
            GUILayout.MaxHeight(440),
            // GUILayout.ExpandHeight(true),
        };
        _width150 = new GUILayoutOption[] { GUILayout.MaxWidth(150) };
        _width100 = new GUILayoutOption[] { GUILayout.MaxWidth(100) };
        _width20 = new GUILayoutOption[] { GUILayout.MaxWidth(20) };
    }

    private void OnEnable() {
        // 初始化Sprite
        for (int index = 0; index < _clip.frames.Length; index++) {
            SpriteAnimationFrame frame = _clip.frames[index];
            frame.sprite = LoadSprite(frame.spritePath);
        }
    }

    /// <summary>
    /// 当前绑定的Clip
    /// </summary>
    public SpriteAnimationClip Clip => _clip;

    /// <summary>
    /// 启用预览视图
    /// </summary>
    public void EnablePreviewer() {
        if (_previewer == null) {
            _previewer = new SpriteAnimationPreviewer(_clip);
            Repaint();
        }
    }

    /// <summary>
    /// 禁用预览视图
    /// </summary>
    public void DisablePreviewer() {
        if (_previewer != null) {
            _previewer = null;
            Repaint();
        }
    }

    public override void OnInspectorGUI() {
        GUI.enabled = _previewer == null || !_previewer.IsPlaying;
        // base.OnInspectorGUI();
        EditorGUILayout.FloatField("Duration", _clip.duration);
        _clip.loop = EditorGUILayout.Toggle("Loop", _clip.loop);
        _clip.weight = EditorGUILayout.FloatField(PooledLabel().WithText("Weight", "动画融合默认权重"), _clip.weight);
        DrawSeparator();

        // 批量修改帧偏移
        EditorGUILayout.BeginHorizontal();
        _position = DrawVector2("批量·帧坐标", _position);
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
        DrawSeparator(Color.yellow);
        // 帧图
        DrawRawImages();
        GUI.enabled = true;
        //
        DrawSeparator(Color.yellow);
        DrawEvents();

        // 绘制播放区
        if (_previewer == null) return;
        DrawSeparator();
        // 更新UI之前更新动画
        if (_previewer.IsPlaying) {
            _previewer.Update();
        }
        _previewer.OnInspectorGUI();
        // 模拟Update
        if (_previewer.IsPlaying) {
            Repaint();
        }
    }

    private void OnClickAddFrame() {
        SpriteAnimationFrame frame = new SpriteAnimationFrame();
        // 超过一个元素时，拷贝前面元素的基础数据
        if (_clip.FrameCount > 0) {
            SpriteAnimationFrame prevFrame = _clip[_clip.FrameCount - 1];
            frame.sprite = prevFrame.sprite;
            frame.spritePath = prevFrame.spritePath;
            frame.duration = prevFrame.duration;
            frame.position = prevFrame.position;
            frame.rotation = prevFrame.rotation;
        }
        _clip.AddFrame(frame);
        EditorUtility.SetDirty(_clip);
    }

    private void OnClickDeleteFrame() {
        if (_clip.FrameCount > 0) {
            _clip.RemoveFrame(_clip.FrameCount - 1);
            EditorUtility.SetDirty(_clip);
        }
    }

    private static void DrawSeparator() {
        Rect rect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(rect, Color.gray);
        // EditorGUILayout.LabelField(SEPARATOR);
    }

    private static void DrawSeparator(Color color) {
        Rect rect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(rect, color);
        // EditorGUILayout.LabelField(SEPARATOR);
    }

    #region draw-events

    private void DrawEvents() {
        SerializedProperty propFrameArray = serializedObject.FindProperty("events");
        EditorGUILayout.PropertyField(propFrameArray, true);
        serializedObject.ApplyModifiedProperties();
    }

    #endregion

    #region draw-iamges

    /// <summary>
    /// 绘制原始图片
    /// </summary>
    private void DrawRawImages() {
        EditorGUILayout.BeginVertical();
        // 统一调整label宽度
        float labelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 150;

        // 帧数信息
        EditorGUILayout.BeginHorizontal();
        _imagesFoldout = EditorGUILayout.Foldout(_imagesFoldout, "Frames");
        //
        string controlName = "_frameCount";
        GUI.SetNextControlName(controlName);
        _frameCount = EditorGUILayout.IntField(_frameCount, _width100);
        _frameCount = Math.Max(0, _frameCount);
        if (GUILayout.Button("+", _width20)) {
            OnClickAddFrame();
        }
        if (GUILayout.Button("-", _width20)) {
            OnClickDeleteFrame();
        }
        if (GUILayout.Button("Apply", _width100)) {
            _clip.FrameCount = _frameCount;
            EditorUtility.SetDirty(_clip);
        }
        EditorGUILayout.EndHorizontal();
        // 退出输入状态重置输入
        if (GUI.GetNameOfFocusedControl() != controlName) {
            _frameCount = _clip.FrameCount;
        }
        // object数据写入到properties - 下面要使用frames
        serializedObject.Update();

        if (_imagesFoldout) {
            SerializedProperty propFrameArray = serializedObject.FindProperty("frames");
            _imagesScrollPos = EditorGUILayout.BeginScrollView(_imagesScrollPos, scrollOptions);
            for (int index = 0, len = _clip.FrameCount; index < len; index++) {
                if (index > 0) {
                    DrawSeparator();
                }
                SpriteAnimationFrame frame = _clip.frames[index];
                SerializedProperty propFrame = propFrameArray.GetArrayElementAtIndex(index);
                // 帧号和图片总是展示出来，直观
                EditorGUILayout.BeginHorizontal();
                // EditorGUILayout.LabelField();
                GUIContent guiContent = PooledLabel().WithText(GetElementName(index, propFrame.isExpanded));
                propFrame.isExpanded = EditorGUILayout.Foldout(propFrame.isExpanded, guiContent, true);
                EditorGUILayout.ObjectField(frame.sprite, typeof(Sprite), false);
                EditorGUILayout.EndHorizontal();

                Rect controlRect = GUILayoutUtility.GetLastRect();
                if (Event.current.type == EventType.ContextClick
                    && controlRect.Contains(Event.current.mousePosition)) {
                    ShowFrameContextMenu(index);
                    return;
                }
                if (!propFrame.isExpanded) {
                    continue;
                }
                // 图片路径
                // EditorGUILayout.LabelField("SpritePath");
                EditorGUILayout.BeginHorizontal();
                string groupPath = EditorGUILayout.TextField("SpriteGroup", frame.spritePath.groupPath);
                bool clickSelectSpriteGroup = GUILayout.Button("选择", _width100);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                int spriteIndex = EditorGUILayout.IntField("Index", frame.spritePath.index);
                bool clickSelectSprite = GUILayout.Button("选择", _width100);
                EditorGUILayout.EndHorizontal();
                //
                SpritePath spritePath = new SpritePath(groupPath, spriteIndex);
                if (clickSelectSpriteGroup) {
                    OnClickSelectSpriteGroup(frame);
                    GUIUtility.ExitGUI(); // 打开Panel后出当前GUI绘制
                } else if (clickSelectSprite) {
                    OnClickSelectSprite(frame);
                    GUIUtility.ExitGUI(); // 打开Panel后出当前GUI绘制
                } else if (spritePath != frame.spritePath) {
                    frame.spritePath = spritePath;
                    frame.sprite = LoadSprite(spritePath);
                    Repaint();
                }
                EditorGUILayout.BeginHorizontal();
                frame.position = DrawVector2("position", frame.position);
                EditorGUILayout.EndHorizontal();
                frame.rotation = EditorGUILayout.FloatField("rotation", frame.rotation);
                frame.duration = EditorGUILayout.FloatField("duration", frame.duration);
                //
                frame.graphic = EditorGUILayout.IntField("graphic", frame.graphic);
                frame.interp = EditorGUILayout.IntField("interp", frame.interp);
                //
                // 包围盒信息 -- 手动拼接常量字符串，避免频繁创建字符串
                SerializedProperty propHurtBoxes = propFrame.FindPropertyRelative("hurtBoxes");
                GUIContent content = propHurtBoxes.isExpanded
                    ? PooledLabel().WithText("Hurt Boxes " + UnityHelper.SYMBOL_FOLD_OUT)
                    : PooledLabel().WithText("Hurt Boxes " + UnityHelper.SYMBOL_FOLD_UP);
                EditorGUILayout.PropertyField(propHurtBoxes, content, true);
                //
                SerializedProperty propHitBoxes = propFrame.FindPropertyRelative("hitBoxes");
                content = propHitBoxes.isExpanded
                    ? PooledLabel().WithText("Hit Boxes " + UnityHelper.SYMBOL_FOLD_OUT)
                    : PooledLabel().WithText("Hit Boxes " + UnityHelper.SYMBOL_FOLD_UP);
                EditorGUILayout.PropertyField(propHitBoxes, content, true);
            }
            EditorGUILayout.EndScrollView();
            serializedObject.ApplyModifiedProperties();
        }
        EditorGUIUtility.labelWidth = labelWidth;
        EditorGUILayout.EndVertical();

        //
        if (EditorGUI.EndChangeCheck()) {
            _clip.RefreshDuration();
            EditorUtility.SetDirty(_clip);
        }
    }

    private Vector2 DrawVector2(string label, Vector2 vector2) {
        EditorGUILayout.LabelField(label, _width150);
        EditorGUILayout.LabelField("x", _width20);
        float x = EditorGUILayout.FloatField(vector2.x);
        EditorGUILayout.LabelField("y", _width20);
        float y = EditorGUILayout.FloatField(vector2.y);
        return new Vector2(x, y);
    }

    #region click-menu

    private void ShowFrameContextMenu(int index) {
        // 创建Menu不能使用池化的Label
        GenericMenu menu = new GenericMenu();
        menu.AddDisabledItem(new GUIContent("index: " + index));
        menu.AddSeparator("");
        //
        menu.AddItem(new GUIContent("Duplicate"), false, OnClickDuplicate, index);
        menu.AddItem(new GUIContent("MoveUp"), false, OnClickMoveUp, index);
        menu.AddItem(new GUIContent("MoveDown"), false, OnClickMoveDown, index);
        menu.AddItem(new GUIContent("Delete"), false, OnClickDelete, index);
        menu.AddItem(new GUIContent("Inherit HurtBox"), false, OnClickInheritHurtBox, index);
        menu.AddItem(new GUIContent("Inherit HitBox"), false, OnClickInheritHitBox, index);
        menu.ShowAsContext();
    }

    private void OnClickInheritHurtBox(object obj) {
        int index = (int)obj;
        if (index > 0) {
            SpriteAnimationFrame frame = _clip[index];
            SpriteAnimationFrame prevFrame = _clip[index - 1];
            frame.hurtBoxes = (AABB[])prevFrame.hurtBoxes.Clone();
        }
    }

    private void OnClickInheritHitBox(object obj) {
        int index = (int)obj;
        if (index > 0) {
            SpriteAnimationFrame frame = _clip[index];
            SpriteAnimationFrame prevFrame = _clip[index - 1];
            frame.hitBoxes = (AABB[])prevFrame.hitBoxes.Clone();
        }
    }

    private void OnClickDuplicate(object obj) {
        int index = (int)obj;
        SerializedProperty propFrameArray = serializedObject.FindProperty("frames");
        propFrameArray.GetArrayElementAtIndex(index).DuplicateCommand();
        serializedObject.ApplyModifiedProperties();
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

    #region SelectSprite

    private void OnClickSelectSpriteGroup(SpriteAnimationFrame frame) {
        SpritePathEditor.OnClickSelectSpriteGroup(ref frame.spritePath);
        LoadSprite(frame.spritePath);
    }

    private void OnClickSelectSprite(SpriteAnimationFrame frame) {
        SpritePathEditor.OnClickSelectSprite(ref frame.spritePath);
    }

    private static Sprite LoadSprite(SpritePath spritePath) {
        return SpritePathEditor.LoadSprite(spritePath);
    }

    #endregion

    #endregion

    #region cache

    private static readonly string[] elementNameCache1 = new string[100];
    private static readonly string[] elementNameCache2 = new string[100];

    static SpriteAnimationClipEditor() {
        for (int index = 0; index < elementNameCache1.Length; index++) {
            elementNameCache1[index] = $"Frame: {index}  {UnityHelper.GetFoldoutSymbol(true)}";
            elementNameCache2[index] = $"Frame: {index}  {UnityHelper.GetFoldoutSymbol(false)}";
        }
    }

    private static string GetElementName(int index, bool isExpanded) {
        if (index < 0 || index >= elementNameCache1.Length) {
            return $"Frame: {index}  {UnityHelper.GetFoldoutSymbol(isExpanded)}";
        }
        return isExpanded ? elementNameCache1[index] : elementNameCache2[index];
    }

    #endregion
}
}