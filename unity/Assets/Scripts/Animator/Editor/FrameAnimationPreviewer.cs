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
/// 如果是自定义Window，可以在<see cref="EditorWindow.Update"/>方法中调用该对象的Update；
/// 如果是普通的InspectorGUI，可以在<see cref="Editor.OnInspectorGUI"/>方法中调用该对象的Update。
/// </summary>
public class FrameAnimationPreviewer
{
    private FrameAnimationClip _clip;
    private SpriteRenderer _renderer;
    private PlayState _playState;
    private SpriteDrawMode _drawMode;
    private int _orderInLayer; // 不同步
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

    private readonly List<float> _timeStamps = new List<float>(); // 每一帧的结束时间，play状态下可用
    private float _duration; // 播放区间的时长
    private int _frameIndex; // 当前帧号

    /// <summary>
    /// 随同播放器
    /// </summary>
    private readonly List<FrameAnimationPreviewer> followers = new();

    public FrameAnimationPreviewer(FrameAnimationClip clip) {
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
        _orderInLayer = EditorGUILayout.LayerField("orderInLayer", _orderInLayer);
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
        float revisedTime = _time + (_startFrame > 0 ? _timeStamps[_startFrame - 1] : 0);
        if (revisedTime + 0.01f >= _timeStamps[_endFrame]) {
            // 达到结束帧，判断是否重新开始 - 不跳帧
            if (_loop) {
                _time = 0;
                _frameIndex = _startFrame;
                _renderer.sprite = _clip.frames[_frameIndex].sprite;
            } else {
                _time = _duration;
            }
        } else {
            _time += _deltaTime;
            revisedTime += _deltaTime;
            // 判断是否进入下一帧
            if (revisedTime >= _timeStamps[_frameIndex]) {
                _frameIndex++;
                _renderer.sprite = _clip.frames[_frameIndex].sprite;
            }
        }
        foreach (FrameAnimationPreviewer follower in followers) {
            follower.Update();
        }
    }

    private int FindFrame(float time) {
        for (int index = _startFrame; index < _clip.frames.Length; index++) {
            time -= _clip.frames[index].duration;
            if (time <= 0) {
                return index;
            }
        }
        return _clip.frames.Length - 1;
    }

    #endregion

    /// <summary>
    /// 是否处于播放状态
    /// </summary>
    public bool IsPlaying => _playState == PlayState.Playing && _renderer;

    /// <summary>
    /// 开始播放动画
    /// </summary>
    public void Play() {
        if (!_clip || !_renderer) {
            return;
        }
        PlayState prevState = _playState;
        if (prevState == PlayState.Playing) {
            return;
        }
        _playState = PlayState.Playing;
        // 快照每一帧结束时间
        _timeStamps.Clear();
        _timeStamps.EnsureCapacity(_clip.FrameCount);
        for (int index = 0; index < _clip.frames.Length; index++) {
            AnimationFrame frame = _clip.frames[index];
            if (index == 0) {
                _timeStamps.Add(frame.duration);
            } else {
                _timeStamps.Add(frame.duration + _timeStamps[index - 1]);
            }
        }
        // 修正播放区间
        if (_endFrame == -1) {
            _endFrame = _clip.frames.Length - 1;
        }
        _startFrame = Math.Clamp(_startFrame, 0, _clip.FrameCount - 1);
        _endFrame = Math.Clamp(_endFrame, 0, _clip.FrameCount - 1);
        if (_endFrame < _startFrame) {
            _endFrame = _startFrame;
        }
        _duration = _clip.GetSubDuration(_startFrame, _endFrame);

        _tickTime = Time.realtimeSinceStartup;
        _deltaTime = 0;
        // Stop重播还需重置播放时间
        if (prevState == PlayState.Stopped) {
            _time = 0;
            _frameIndex = _startFrame;
            _renderer.sprite = _clip.frames[_frameIndex].sprite;
        }
        // SceneView.RepaintAll();
        foreach (FrameAnimationPreviewer follower in followers) {
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
        foreach (FrameAnimationPreviewer follower in followers) {
            follower.Pause();
        }
    }

    /// <summary>
    /// 停止动画播放，下次Play时重置时间
    /// </summary>
    public void Stop() {
        _playState = PlayState.Stopped;
        foreach (FrameAnimationPreviewer follower in followers) {
            follower.Stop();
        }
    }

    /// <summary>
    /// 更改clip会停止动画播放
    /// </summary>
    public FrameAnimationClip Clip {
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
    /// 所有的跟随播放器
    /// </summary>
    public List<FrameAnimationPreviewer> Followers => followers;

    /// <summary>
    /// 添加一个跟随播放器
    /// </summary>
    /// <param name="follower"></param>
    public void AddFollower(FrameAnimationPreviewer follower) {
        followers.Add(follower);
        SyncSetting(follower);
    }

    /// <summary>
    /// 删除跟随播放器
    /// </summary>
    /// <param name="follower"></param>
    public void RemoveFollower(FrameAnimationPreviewer follower) {
        followers.Remove(follower);
    }

    /// <summary>
    /// 重置播放状态
    /// </summary>
    private void ResetPlayState() {
        _playState = PlayState.Stopped;
        _startFrame = 0;
        _endFrame = -1;
        _timeStamps.Clear();
        _duration = 0;
        _frameIndex = 0;
        // 同步停止
        foreach (FrameAnimationPreviewer follower in followers) {
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
        }
        foreach (FrameAnimationPreviewer follower in followers) {
            SyncSetting(follower);
        }
    }

    private void SyncSetting(FrameAnimationPreviewer follower) {
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
        _frameIndex = FindFrame(time);
        _renderer.sprite = _clip.frames[_frameIndex].sprite;
        //
        foreach (FrameAnimationPreviewer follower in followers) {
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