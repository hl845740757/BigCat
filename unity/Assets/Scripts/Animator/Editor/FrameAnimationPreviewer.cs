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
using Wjybxx.BigCat.Util;
using Wjybxx.Commons.Collections;

namespace Wjybxx.BigCat.AnimatorEditor
{
/// <summary>
/// 用于编辑器下预览帧动画
///
/// 注：用于各处预览帧动画
/// </summary>
public class FrameAnimationPreviewer
{
    private readonly Editor _editor;
    private FrameAnimationClip _clip;

    private SpriteRenderer _renderer;
    private PlayState _playState;
    private SpriteDrawMode _drawMode;
    private bool _flipX;
    private bool _flipY;
    private bool _loop = true;
    private int _startFrame = 0; // 动画起始帧
    private int _endFrame = -1; // 动画结束帧

    // Editor模式需要手动实现主循环，不能使用Time的time和deltaTime；此外，一帧可能执行多次OnGUI
    private float _tickTime;
    private float _deltaTime;
    private float _timeScale = 1f;
    private float _time;

    private readonly List<float> timeStamps = new List<float>(); // 每一帧的结束时间，play状态下可用
    private float _duration; // 播放区间的时长
    private int _frameIndex; // 当前帧号

    public FrameAnimationPreviewer(Editor editor, FrameAnimationClip clip) {
        _editor = editor;
        _clip = clip;
    }

    /// <summary>
    /// 当前关联的动画资源
    /// </summary>
    public FrameAnimationClip Clip {
        get => _clip;
        set => _clip = value;
    }

    /// <summary>
    /// 是否处于播放状态
    /// </summary>
    public bool IsPlaying => _playState == PlayState.Playing && _renderer;

    /// <summary>
    /// 心跳函数
    /// </summary>
    public void OnInspectorGUI() {
        // 避免多次查询
        float sinceStartup = Time.realtimeSinceStartup;
        _deltaTime = Math.Max(0, sinceStartup - _tickTime);
        _tickTime = sinceStartup;

        // 播放选项
        EditorGUILayout.BeginVertical();
        _renderer = (SpriteRenderer)EditorGUILayout.ObjectField("Renderer", _renderer, typeof(SpriteRenderer), true);
        _drawMode = (SpriteDrawMode)EditorGUILayout.EnumPopup("DrawMode", _drawMode);
        _flipX = EditorGUILayout.Toggle("flipX", _flipX);
        _flipY = EditorGUILayout.Toggle("flipY", _flipY);
        _loop = EditorGUILayout.Toggle("loop", _loop);
        _timeScale = EditorGUILayout.FloatField("timeScale", _timeScale);
        _startFrame = EditorGUILayout.IntField("startFrame", _startFrame);
        _endFrame = EditorGUILayout.IntField("endFrame", _endFrame);
        EditorGUILayout.EndVertical();
        //
        if (_renderer) {
            _renderer.drawMode = _drawMode;
            _renderer.flipX = _flipX;
            _renderer.flipY = _flipY;
        }

        // 播放按钮
        EditorGUILayout.BeginHorizontal();
        GUI.enabled = _renderer;
        if (GUILayout.Button("Play")) {
            OnClickPlayButton();
        }
        GUI.enabled = true;
        if (GUILayout.Button("Pause")) {
            _playState = PlayState.Paused;
        }
        if (GUILayout.Button("Stop")) {
            _playState = PlayState.Stopped;
        }
        EditorGUILayout.EndHorizontal();

        // 播放进度
        if (IsPlaying) {
            Update();
            // 播放模式下不可操作滚动条
            GUI.enabled = false;
            EditorGUILayout.Slider("time", _time, 0, _duration);
            EditorGUILayout.IntField("frame: ", _frameIndex);
            GUI.enabled = true;
        } else {
            // 非播放状态允许拖动时间轴
            float time = EditorGUILayout.Slider("time", _time, 0, _duration);
            if (!Mathf.Approximately(time, _time) && _renderer) {
                _time = time;
                _frameIndex = FindFrame(time);
                _renderer.sprite = _clip.frames[_frameIndex].sprite;
            }
            EditorGUILayout.IntField("frame: ", _frameIndex);
        }

        // 播放状态下需要持续刷新
        if (IsPlaying) {
            _editor.Repaint();
        }
    }

    private void Update() {
        float revisedTime = _time + (_startFrame > 0 ? timeStamps[_startFrame - 1] : 0);
        if (revisedTime + 0.01f >= timeStamps[_endFrame]) {
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
            if (revisedTime >= timeStamps[_frameIndex]) {
                _frameIndex++;
                _renderer.sprite = _clip.frames[_frameIndex].sprite;
            }
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

    private void OnClickPlayButton() {
        PlayState prevState = _playState;
        if (prevState == PlayState.Playing) {
            return;
        }
        _playState = PlayState.Playing;
        // 快照每一帧结束时间
        timeStamps.Clear();
        timeStamps.EnsureCapacity(_clip.FrameCount);
        for (int index = 0; index < _clip.frames.Length; index++) {
            AnimationFrame frame = _clip.frames[index];
            if (index == 0) {
                timeStamps.Add(frame.duration);
            } else {
                timeStamps.Add(frame.duration + timeStamps[index - 1]);
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
    }

    private enum PlayState
    {
        Stopped,
        Playing,
        Paused,
    }
}
}