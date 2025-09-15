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
using System.Runtime.CompilerServices;
using UnityEngine;
using Wjybxx.BigCat.UnityCore;
using Wjybxx.Commons;

namespace Wjybxx.BigCat.Animator
{
/// <summary>
/// 序列帧动画资源抽象
///
/// 注：运行时修改帧动画信息时必须拷贝。
/// TODO 纳入资源管理，引用计数
/// </summary>
[CreateAssetMenu(menuName = "FrameAnimation/AnimationClip", fileName = "NewAnimationClip")]
public sealed class FrameAnimationClip : ScriptableObject
{
    /// <summary>
    /// 关联的动画帧
    /// (自定义样式)
    /// </summary>
    [HideInInspector]
    public AnimationFrame[] frames = Array.Empty<AnimationFrame>();
    /// <summary>
    /// 动画总时长（缓存值）
    ///
    /// 注：该信息在运行时可能意义不大，因为部分帧可能被循环播放。
    /// </summary>
    [Tooltip("动画总时长缓存")]
    [ReadOnly]
    public float duration;

    //////////////////////////////////////////////////////////////

    /// <summary>
    /// 获取指定帧
    /// </summary>
    /// <param name="index"></param>
    public AnimationFrame this[int index] {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => frames[index];
        set {
            frames[index] = value;
            RefreshDuration();
        }
    }

    /// <summary>
    /// 帧数
    /// </summary>
    public int FrameCount {
        get => this.frames.Length;
        set {
            Array.Resize(ref this.frames, value);
            RefreshDuration(); // 可能缩短
        }
    }

    /// <summary>
    /// 刷新动画时长
    /// </summary>
    public void RefreshDuration() {
        duration = 0;
        foreach (var frame in frames) {
            duration += frame.duration;
        }
    }

    /// <summary>
    /// 设置所有帧的间隔
    /// </summary>
    /// <param name="frameInterval">帧间隔</param>
    public void SetFrameInterval(float frameInterval) {
        duration = frames.Length * frameInterval;
        for (int index = 0; index < frames.Length; index++) {
            AnimationFrame frame = frames[index];
            frames[index] = frame.WithDuration(frameInterval);
        }
    }

    /// <summary>
    /// 获取某段区间的时长
    /// </summary>
    /// <param name="startFrame"></param>
    /// <param name="endFrame"></param>
    /// <returns></returns>
    public float GetSubDuration(int startFrame, int endFrame = -1) {
        if (endFrame == -1) endFrame = frames.Length - 1;
        float r = 0;
        for (int index = startFrame; index <= endFrame; index++) {
            r += frames[index].duration;
        }
        return r;
    }

    /// <summary>
    /// 添加帧
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="index"></param>
    public void AddFrame(AnimationFrame frame, int index = -1) {
        if (index == -1) {
            Array.Resize(ref frames, frames.Length + 1);
            frames[frames.Length - 1] = frame;
        } else {
            ArrayUtil.Insert(ref frames, index, frame);
        }
        duration += frame.duration;
    }

    public void AddFrames(List<AnimationFrame> targetFrames) {
        if (targetFrames.Count == 0) {
            return;
        }
        int prevLen = frames.Length;
        Array.Resize(ref frames, prevLen + targetFrames.Count);
        foreach (AnimationFrame frame in targetFrames) {
            frames[prevLen++] = frame;
            duration += frame.duration;
        }
    }

    /// <summary>
    /// 删除指定帧
    /// </summary>
    /// <param name="index"></param>
    public void RemoveFrame(int index) {
        AnimationFrame frame = frames[index];
        ArrayUtil.RemoveAt(ref frames, index);
        duration -= frame.duration;
    }

    /// <summary>
    /// 同步帧动画每帧的时间
    ///
    /// 注：主要用于同步身体各个部件之间的帧动画时长。
    /// </summary>
    /// <param name="clip"></param>
    /// <param name="target"></param>
    public static void SyncFrameDuration(FrameAnimationClip clip, FrameAnimationClip target) {
#if UNITY_EDITOR
        if (clip.FrameCount != target.FrameCount) {
            Debug.LogWarning($"{clip.name}.FrameCount != {target.name}.FrameCount");
        }
#endif
        int len = Mathf.Min(clip.FrameCount, target.FrameCount);
        for (int i = 0; i < len; i++) {
            AnimationFrame frame = clip.frames[i];
            target.frames[i] = target.frames[i].WithDuration(frame.duration);
        }
    }

    public static void SyncFrameDuration(FrameAnimationClip clip, List<FrameAnimationClip> targetList) {
        foreach (FrameAnimationClip target in targetList) {
            SyncFrameDuration(clip, target);
        }
    }

    private void OnValidate() {
        RefreshDuration();
    }
}
}