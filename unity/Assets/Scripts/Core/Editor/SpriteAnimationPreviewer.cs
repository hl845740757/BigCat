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

namespace Wjybxx.BigCat.AnimatorEditor
{
/// <summary>
/// 用于编辑器下预览帧动画
///
/// 注：外部需要调用<see cref="OnInspectorGUI"/>绘制UI，调用<see cref="Update"/>方法驱动动画播放；
/// 如果是自定义Window，可以在<code>EditorWindow.Update</code>>方法中调用该对象的Update；
/// 如果是普通的InspectorGUI，可以在<see cref="Editor.OnInspectorGUI"/>方法中调用该对象的Update。
/// </summary>
public class SpriteAnimationPreviewer
{
    private SpriteAnimationClip _clip;
    private SpriteRenderer _renderer;
    private PlayState _playState;
    private SpriteDrawMode _drawMode;
    private int _orderInLayer; // 不同步给Follower
    private bool _flipX;
    private bool _flipY;
    private bool _loop = true;
    private float _timeScale = 1f;
    private int _startFrame = 0; // 动画起始帧
    private int _endFrame = -1; // 动画结束帧

    // Editor模式需要手动实现主循环，不能使用Time的time和deltaTime；此外，一帧可能执行多次OnGUI
    private float _tickTime;
    private float _deltaTime;
    private float _time;

    private float _duration; // 播放区间的时长
    private int _frameIndex; // 当前帧号

    private readonly List<SpriteAnimationPreviewer> followers = new(); // 随同播放器
    private Predicate<SpriteAnimationPreviewer> _onPlayRequested;

    public SpriteAnimationPreviewer() {
    }

    public SpriteAnimationPreviewer(SpriteAnimationClip clip) {
        _clip = clip;
    }

    #region GUI

    /// <summary>
    /// 心跳函数
    /// </summary>
    public void OnInspectorGUI(bool hideRender = false) {
        EditorGUI.BeginChangeCheck();
        // 播放选项
        EditorGUILayout.BeginVertical();
        if (!hideRender) {
            _renderer = (SpriteRenderer)EditorGUILayout.ObjectField("Renderer", _renderer, typeof(SpriteRenderer), true);
        }
        _drawMode = (SpriteDrawMode)EditorGUILayout.EnumPopup("DrawMode", _drawMode);
        _orderInLayer = EditorGUILayout.IntField("OrderInLayer", _orderInLayer);
        _flipX = EditorGUILayout.Toggle("flipX", _flipX);
        _flipY = EditorGUILayout.Toggle("flipY", _flipY);
        _loop = EditorGUILayout.Toggle("loop", _loop);
        _timeScale = EditorGUILayout.FloatField("timeScale", _timeScale);
        _startFrame = EditorGUILayout.IntField("startFrame", _startFrame);
        _endFrame = EditorGUILayout.IntField("endFrame", _endFrame);
        EditorGUILayout.EndVertical();
        // 产生变化再同步
        if (EditorGUI.EndChangeCheck()) {
            ApplySetting();
        }

        // 播放按钮
        EditorGUILayout.BeginHorizontal();
        GUI.enabled = _clip && _renderer;
        if (GUILayout.Button("Play")) {
            Play();
        }
        GUI.enabled = true;
        if (GUILayout.Button("Pause")) {
            Pause();
        }
        if (GUILayout.Button("Stop")) {
            Stop();
        }
        EditorGUILayout.EndHorizontal();

        // 播放进度
        if (IsPlaying) {
            // 播放模式下不可操作滚动条
            GUI.enabled = false;
            EditorGUILayout.Slider("time", _time, 0, _duration);
            EditorGUILayout.IntField("frame: ", _frameIndex);
            GUI.enabled = true;
        } else {
            // 非播放状态允许拖动时间轴
            float time = EditorGUILayout.Slider("time", _time, 0, _duration);
            SetPlayTime(time);
            EditorGUILayout.IntField("frame: ", _frameIndex);
        }
    }

    public void Update() {
        float sinceStartup = Time.realtimeSinceStartup;
        _deltaTime = Math.Max(0, sinceStartup - _tickTime) * _timeScale;
        _tickTime = sinceStartup;
        if (!IsPlaying) {
            return;
        }
        float timeOffset = _startFrame > 0 ? _clip[_startFrame - 1].endTime : 0;
        float revisedTime = _time + timeOffset;
        if (revisedTime + _deltaTime >= _clip[_endFrame].endTime) {
            // 达到结束帧，判断是否重新开始 - 不跳帧
            if (_loop) {
                _time = 0;
                _frameIndex = _startFrame;
                SetSprite(_frameIndex);
            } else {
                _time = _duration;
            }
        } else {
            _time += _deltaTime;
            revisedTime += _deltaTime;
            // 判断是否进入下一帧
            if (revisedTime >= _clip[_frameIndex].endTime) {
                _frameIndex++;
                SetSprite(_frameIndex);
            }
        }
        foreach (SpriteAnimationPreviewer follower in followers) {
            follower.Update();
        }
    }

    private void SetSprite(int frameIndex) {
        if (!_renderer) return; // destroyed
        SpriteAnimationFrame frame = _clip[frameIndex];
        _renderer.sprite = frame.sprite;

        Vector2 position = frame.position;
        float rotation = frame.rotation;
        if (_flipX) {
            position.x *= -1;
            rotation *= -1;
        }
        if (_flipY) {
            position.y *= -1;
            rotation *= -1;
        }
        _renderer.transform.localPosition = position;
        _renderer.transform.localRotation = Quaternion.Euler(0, 0, rotation);
    }

    // TODO 绘制受击框和攻击框
    public void OnSceneGUI() {
        // Handles.DrawWireCube();
    }

    #endregion

    /// <summary>
    /// 是否处于播放状态
    /// </summary>
    public bool IsPlaying => _playState == PlayState.Playing && _renderer;

    /// <summary>
    /// 请求播放动画
    /// </summary>
    public void Play() {
        PlayState prevState = _playState;
        if (prevState == PlayState.Playing) {
            return;
        }
        if (_onPlayRequested != null && !_onPlayRequested(this)) {
            return;
        }
        if (!_clip || !_renderer) { // 初始化不正确-需放在OnPlay调用之后
            return;
        }
        _playState = PlayState.Playing;
        // 修正播放区间
        if (_endFrame == -1) {
            _endFrame = _clip.frames.Length - 1;
        }
        _startFrame = Math.Clamp(_startFrame, 0, _clip.FrameCount - 1);
        _endFrame = Math.Clamp(_endFrame, _startFrame, _clip.FrameCount - 1);
        _duration = _clip.GetSubDuration(_startFrame, _endFrame);

        _tickTime = Time.realtimeSinceStartup;
        _deltaTime = 0;
        // Stop重播还需重置播放时间
        if (prevState == PlayState.Stopped) {
            _time = 0;
            _frameIndex = _startFrame;
            SetSprite(_frameIndex);
        }
        // SceneView.RepaintAll();
        foreach (SpriteAnimationPreviewer follower in followers) {
            follower.Play();
        }
    }

    /// <summary>
    /// 暂停动画播放
    /// </summary>
    public void Pause() {
        if (_playState == PlayState.Playing) {
            _playState = PlayState.Paused;
        }
        foreach (SpriteAnimationPreviewer follower in followers) {
            follower.Pause();
        }
    }

    /// <summary>
    /// 停止动画播放，下次Play时重置时间
    /// </summary>
    public void Stop() {
        _playState = PlayState.Stopped;
        _time = 0; // 时间还是重置更好
        _frameIndex = _startFrame;
        SetSprite(_frameIndex);
        foreach (SpriteAnimationPreviewer follower in followers) {
            follower.Stop();
        }
    }

    /// <summary>
    /// 更改clip会停止动画播放
    /// </summary>
    public SpriteAnimationClip Clip {
        get => _clip;
        set {
            if (_clip == value) {
                return;
            }
            _clip = value;
            ResetPlayState();
        }
    }

    /// <summary>
    /// 更改renderer也会停止动画播放
    /// </summary>
    public SpriteRenderer Renderer {
        get => _renderer;
        set {
            if (_renderer == value) {
                return;
            }
            _renderer = value;
            ResetPlayState();
        }
    }

    /// <summary>
    /// 渲染顺序
    /// </summary>
    public int OrderInLayer {
        get => _orderInLayer;
        set {
            _orderInLayer = value;
            if (_renderer) {
                _renderer.sortingOrder = value;
            }
        }
    }

    /// <summary>
    /// 动画开始帧
    /// </summary>
    public int StartFrame {
        get => _startFrame;
        set => _startFrame = value;
    }

    /// <summary>
    /// 动画结束帧
    /// </summary>
    public int EndFrame {
        get => _endFrame;
        set => _endFrame = value;
    }

    /// <summary>
    /// 在用户请求Play时调用
    ///
    /// 1.由于Previewer是嵌套在其它编辑器下的，因此可能还有其它播放条件。
    /// 2.如果返回值为false则禁止播放。
    /// </summary>
    public Predicate<SpriteAnimationPreviewer> OnPlayRequested {
        get => _onPlayRequested;
        set => _onPlayRequested = value;
    }

    /// <summary>
    /// 所有的随同播放器
    /// </summary>
    public List<SpriteAnimationPreviewer> Followers => followers;

    /// <summary>
    /// 添加一个跟随播放器
    /// </summary>
    /// <param name="follower"></param>
    public void AddFollower(SpriteAnimationPreviewer follower) {
        followers.Add(follower);
        SyncSetting(follower);
    }

    /// <summary>
    /// 删除跟随播放器
    /// </summary>
    /// <param name="follower"></param>
    public void RemoveFollower(SpriteAnimationPreviewer follower) {
        followers.Remove(follower);
    }

    /// <summary>
    /// 重置播放状态
    /// </summary>
    private void ResetPlayState() {
        _playState = PlayState.Stopped;
        _startFrame = 0;
        _endFrame = -1;
        _duration = 0;
        _frameIndex = 0;
        // 同步停止
        foreach (SpriteAnimationPreviewer follower in followers) {
            follower.ResetPlayState();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    private void ApplySetting() {
        if (_renderer) {
            _renderer.drawMode = _drawMode;
            _renderer.sortingOrder = _orderInLayer;
            _renderer.flipX = _flipX;
            _renderer.flipY = _flipY;
            //
            if (_clip && _frameIndex < _clip.FrameCount) {
                SetSprite(_frameIndex);
            }
        }
        foreach (SpriteAnimationPreviewer follower in followers) {
            SyncSetting(follower);
        }
    }

    private void SyncSetting(SpriteAnimationPreviewer follower) {
        follower._drawMode = _drawMode;
        follower._flipX = _flipX;
        follower._flipY = _flipY;
        follower._loop = _loop;
        follower._startFrame = _startFrame;
        follower._endFrame = _endFrame;
        follower._timeScale = _timeScale;
        follower.ApplySetting();
    }

    /// <summary>
    /// 设置动画播放时间，用于同步多个帧动画的进度
    /// </summary>
    /// <param name="time"></param>
    private void SetPlayTime(float time) {
        if (Mathf.Approximately(time, _time) || !_renderer) {
            return;
        }
        _time = time;
        _frameIndex = _clip.SearchFrameByTime(time);
        SetSprite(_frameIndex);
        //
        foreach (SpriteAnimationPreviewer follower in followers) {
            follower.SetPlayTime(time);
        }
    }

    private enum PlayState
    {
        Stopped,
        Playing,
        Paused,
    }
}
}