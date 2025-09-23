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
using Wjybxx.BigCat.Animator;
using Wjybxx.Commons;

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
    private Vector2 _offset;
    private float _rotation;
    private int _frameCount;
    private static bool _listMode = true; // 静态字段保留状态

    private Vector2 scrollPos;
    private GUILayoutOption[] scrollOptions;
    private GUILayoutOption[] _width150;
    private GUILayoutOption[] _width100;
    private GUILayoutOption[] _width20;
    //
    private string spriteDir;
    private readonly Dictionary<string, Sprite> _spriteAtlas = new Dictionary<string, Sprite>();
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
        spriteDir ??= Application.dataPath + "/Resources/";
        // 如果图片数量过多，默认初始化为List模式，否则极其占用CPU
        if (_clip.frames.Length > 20) {
            _listMode = true;
        }
        if (_clip.saveAsSpriteName) {
            return;
        }
        // 初始化Sprite
        for (int index = 0; index < _clip.frames.Length; index++) {
            ref SpriteAnimationFrame frame = ref _clip.frames[index];
            if (string.IsNullOrWhiteSpace(frame.spritePath)) {
                frame.sprite = null;
            } else {
                frame.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(frame.spritePath);
            }
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
        base.OnInspectorGUI();
        DrawSeparator();

        // 批量修改帧偏移
        EditorGUILayout.BeginHorizontal();
        _offset = DrawVector2("批量·帧偏移", _offset);
        if (GUILayout.Button("Add", _width100)) {
            _clip.AddFrameOffset(_offset);
            EditorUtility.SetDirty(_clip);
        }
        if (GUILayout.Button("Set", _width100)) {
            _clip.SetFrameOffset(_offset);
            EditorUtility.SetDirty(_clip);
        }
        if (GUILayout.Button("Lerp", _width100)) {
            _clip.LerpFrameOffset(_offset);
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
        _interval = EditorGUILayout.FloatField("批量·帧间隔", _interval);
        _interval = Math.Max(0.01f, _interval);
        if (GUILayout.Button("Apply", _width100)) {
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
        if (GUILayout.Button("Apply", _width100)) {
            _clip.FrameCount = _frameCount;
            EditorUtility.SetDirty(_clip);
        }
        EditorGUILayout.EndHorizontal();
        DrawSeparator();
        // 退出输入状态重置
        if (GUI.GetNameOfFocusedControl() != controlName) {
            _frameCount = _clip.FrameCount;
        }

        // 帧图
        DrawRawImages();
        DrawSeparator();

        // 拖拽添加区
        DrawDragArea();
        GUI.enabled = true;

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

    private static void DrawSeparator() {
        Rect rect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(rect, Color.gray);
        // EditorGUILayout.LabelField(SEPARATOR);
    }

    #region draw-dragArea

    private void ChangeSpriteDir(string folderPath) {
        if (folderPath == spriteDir) {
            return;
        }
        spriteDir = folderPath;
        RefreshSprites();
        Repaint();
    }

    /// <summary>
    /// 根据当前工作目录刷新Sprite的引用
    /// </summary>
    private void RefreshSprites() {
        _spriteAtlas.Clear();
        if (string.IsNullOrWhiteSpace(spriteDir)) {
            return;
        }
        foreach (string filePath in Directory.GetFiles(spriteDir)) {
            if (!SystemExtensions.IsImageFile(filePath)) {
                continue;
            }
            string assetPath = SystemExtensions.ConvertToAssetPath(filePath);
            Sprite sprite = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Sprite)) as Sprite;
            if (!sprite) {
                continue;
            }
            _spriteAtlas[sprite.name] = sprite;
        }
        // 如果是图集动画，更换文件夹时刷新Sprite
        if (!_clip.saveAsSpriteName) {
            return;
        }
        for (int index = 0; index < _clip.frames.Length; index++) {
            ref SpriteAnimationFrame frame = ref _clip.frames[index];
            if (string.IsNullOrWhiteSpace(frame.spritePath)) {
                continue;
            }
            _spriteAtlas.TryGetValue(frame.spritePath, out Sprite sprite);
            frame.sprite = sprite;
        }
    }

    /// <summary>
    /// 本来是想拖动文件夹到方块的，但框框画得不好看，拖拽也容易误操作退出Inspector界面
    /// </summary>
    private void DrawDragArea() {
        // 布局内打开新窗口会导致Bug
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("图集目录:");
        bool clickChangeDir = GUILayout.Button("选择") && Event.current.button == 0;
        bool clickAddFile = GUILayout.Button("添加文件", _width150);
        bool clickAddFolder = GUILayout.Button("添加文件夹", _width150);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.SelectableLabel(spriteDir);
        DrawSeparator();

        if (clickChangeDir) {
            string folderPath = EditorUtility.OpenFolderPanel("选择图集目录", spriteDir, "");
            if (!string.IsNullOrEmpty(folderPath)) {
                ChangeSpriteDir(folderPath);
            }
        }
        if (clickAddFile) {
            string filePath = EditorUtility.OpenFilePanel("添加图片文件", spriteDir, "");
            AddFrameByFile(filePath);
        }
        if (clickAddFolder) {
            string message = "该操作将导入图集目录的所有图片，是否确定？";
            if (EditorUtility.DisplayDialog("添加文件夹", message, "确定", "取消")) {
                AddFreamsByFolder(spriteDir);
            }
        }
    }

    private void AddFrameByFile(string filePath) {
        string assetPath = SystemExtensions.ConvertToAssetPath(filePath);
        Sprite sprite = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Sprite)) as Sprite;
        if (!sprite) {
            return;
        }
        string spritPtah = _clip.saveAsSpriteName ? sprite.name : AssetDatabase.GetAssetPath(sprite); // 标准地址
        _clip.AddFrame(new SpriteAnimationFrame(spritPtah, 0.1f)
        {
            sprite = sprite
        });
        EditorUtility.SetDirty(_clip);
    }

    private void AddFreamsByFolder(string folderPath) {
        // string assetPath = SystemExtensions.ConvertToAssetPath(folderPath);
        // AssetDatabase.LoadAllAssetsAtPath(assetPath); 不好使...
        List<SpriteAnimationFrame> frames = new List<SpriteAnimationFrame>(10);
        foreach (string filePath in Directory.GetFiles(folderPath)) {
            if (!SystemExtensions.IsImageFile(filePath)) {
                continue;
            }
            string assetPath = SystemExtensions.ConvertToAssetPath(filePath);
            Sprite sprite = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Sprite)) as Sprite;
            if (!sprite) {
                continue;
            }
            string spritPtah = _clip.saveAsSpriteName ? sprite.name : AssetDatabase.GetAssetPath(sprite); // 标准地址
            frames.Add(new SpriteAnimationFrame(spritPtah, 0.1f)
            {
                sprite = sprite
            });
        }
        _clip.AddFrames(frames);
        EditorUtility.SetDirty(_clip);
    }

    private void OpenSelectSpritePanel(ref SpriteAnimationFrame frame) {
        string filePath = EditorUtility.OpenFilePanel("选择图片", spriteDir, "");
        string assetPath = SystemExtensions.ConvertToAssetPath(filePath);
        Sprite sprite = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Sprite)) as Sprite;
        if (!sprite) {
            return;
        }
        string spritPtah = _clip.saveAsSpriteName ? sprite.name : AssetDatabase.GetAssetPath(sprite); // 标准地址
        frame.spritePath = spritPtah;
        frame.sprite = sprite;
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
            SortFrames(1);
            EditorUtility.SetDirty(_clip);
        }
        if (GUILayout.Button("排序 ↓")) {
            SortFrames(-1);
            EditorUtility.SetDirty(_clip);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.BeginVertical();
        GUILayout.FlexibleSpace();
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, scrollOptions);
        for (int index = 0, len = _clip.FrameCount; index < len; index++) {
            if (index > 0) {
                DrawSeparator();
            }
            //
            // EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(GetElementName(index));
            // EditorGUILayout.EndHorizontal();
            //
            ref SpriteAnimationFrame frame = ref _clip.frames[index];
            if (_listMode) {
                EditorGUILayout.BeginHorizontal();
                string spritePath = EditorGUILayout.TextField(frame.spritePath);
                if (spritePath != frame.spritePath) {
                    SetFramePath(ref frame, spritePath);
                }
                Sprite sprite = (Sprite)EditorGUILayout.ObjectField(frame.sprite, typeof(Sprite), false);
                if (sprite != frame.sprite) {
                    SetFrameSprite(ref frame, sprite);
                }
                EditorGUILayout.LabelField("duration", _width150);
                frame.duration = Mathf.Max(0, EditorGUILayout.FloatField(frame.duration));
                EditorGUILayout.LabelField("rotation", _width150);
                frame.rotation = Math.Clamp(EditorGUILayout.FloatField(frame.rotation), 0, 360);
                frame.offset = DrawVector2("offset", frame.offset);
                //
                EditorGUILayout.EndHorizontal();
            } else {
                EditorGUILayout.BeginVertical();
                string spritePath = EditorGUILayout.TextField(frame.spritePath);
                if (spritePath != frame.spritePath) {
                    SetFramePath(ref frame, spritePath);
                }
                // ObjectFiled指定label时有预览图
                Sprite sprite = (Sprite)EditorGUILayout.ObjectField("sprite", frame.sprite, typeof(Sprite), false);
                if (sprite != frame.sprite) {
                    SetFrameSprite(ref frame, sprite);
                }
                frame.duration = EditorGUILayout.FloatField("duration", frame.duration);
                frame.rotation = EditorGUILayout.FloatField("rotation", frame.rotation);
                //
                EditorGUILayout.BeginHorizontal();
                frame.offset = DrawVector2("offset", frame.offset);
                EditorGUILayout.EndHorizontal();
                //
                EditorGUILayout.EndVertical();
            }
            // 右键菜单
            if (Event.current.type == EventType.ContextClick &&
                GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition)) {
                Event.current.Use();
                // 创建Menu不能使用池化的Label
                GenericMenu menu = new GenericMenu();
                menu.AddDisabledItem(new GUIContent("index: " + index));
                menu.AddSeparator("");
                //
                menu.AddItem(new GUIContent("Insert"), false, OnClickInsert, index);
                menu.AddItem(new GUIContent("MoveUp"), false, OnClickMoveUp, index);
                menu.AddItem(new GUIContent("MoveDown"), false, OnClickMoveDown, index);
                menu.AddItem(new GUIContent("Delete"), false, OnClickDelete, index);
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

    private void SetFramePath(ref SpriteAnimationFrame frame, string spritePath) {
        frame.spritePath = spritePath;
        if (string.IsNullOrWhiteSpace(spritePath)) {
            frame.sprite = null;
        } else if (_clip.saveAsSpriteName) {
            _spriteAtlas.TryGetValue(spritePath, out frame.sprite);
        } else {
            frame.sprite = AssetDatabase.LoadAssetAtPath(spritePath, typeof(Sprite)) as Sprite;
        }
    }

    private void SetFrameSprite(ref SpriteAnimationFrame frame, Sprite sprite) {
        frame.sprite = sprite;
        if (!sprite) {
            frame.spritePath = "";
        } else {
            frame.spritePath = _clip.saveAsSpriteName ? sprite.name : AssetDatabase.GetAssetPath(sprite);
        }
    }

    private Vector2 DrawVector2(string label, Vector2 rawOffset) {
        EditorGUILayout.LabelField(label, _width100);
        EditorGUILayout.LabelField("x", _width20);
        float offsetX = EditorGUILayout.FloatField(rawOffset.x);
        EditorGUILayout.LabelField("y", _width20);
        float offsetY = EditorGUILayout.FloatField(rawOffset.y);
        return new Vector2(offsetX, offsetY);
    }

    private void Distinct() {
        HashSet<string> sprites = new(_clip.FrameCount);
        List<SpriteAnimationFrame> frames = new(_clip.FrameCount);
        foreach (SpriteAnimationFrame frame in _clip.frames) {
            if (!string.IsNullOrWhiteSpace(frame.spritePath) && sprites.Add(frame.spritePath)) {
                frames.Add(frame);
            }
        }
        _clip.FrameCount = 0;
        _clip.AddFrames(frames);
        EditorUtility.SetDirty(_clip);
    }

    private void SortFrames(int sign) {
        if (!_clip.saveAsSpriteName) {
            return;
        }
        Array.Sort(_clip.frames, (a, b) => {
            if (!string.IsNullOrWhiteSpace(a.spritePath) && !string.IsNullOrWhiteSpace(b.spritePath)) {
                string nameA = a.spritePath;
                string nameB = b.spritePath;
                // 如果都是数字，则按照数字排序
                if (int.TryParse(nameA, out int num1) && int.TryParse(nameB, out int num2)) {
                    return sign * num1.CompareTo(num2);
                }
                // 否则按照字符串排序
                return sign * string.Compare(nameA, nameB, StringComparison.Ordinal);
            }
            return a.sprite ? -1 : 1; // 无效帧排尾部
        });
    }

    private void OnClickInsert(object obj) {
        int index = (int)obj;
        _clip.AddFrame(new SpriteAnimationFrame(), index);
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

    private static readonly string[] elementNameCache = new string[300];
    // private const string SEPARATOR = "-----------------------------------------------------------------------------";

    static SpriteAnimationClipEditor() {
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