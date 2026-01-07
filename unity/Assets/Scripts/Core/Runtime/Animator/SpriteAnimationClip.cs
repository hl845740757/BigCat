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
using Wjybxx.Commons;

namespace Wjybxx.BigCat.Animator
{
/// <summary>
/// 序列帧动画数据抽象
/// </summary>
[CreateAssetMenu(menuName = "BigCat/SpriteAnimationClip", fileName = "NewAnimationClip")]
public sealed class SpriteAnimationClip : ScriptableObject
{
    /// <summary>
    /// 帧数据
    /// </summary>
    public SpriteAnimationFrame[] frames = Array.Empty<SpriteAnimationFrame>();
    /// <summary>
    /// 动画总时长（缓存值）
    ///
    /// 注：运行时可调用<see cref="RefreshDuration"/>确保缓存值的正确性。
    /// </summary>
    public float duration;
    /// <summary>
    /// 是否开启阴影
    /// </summary>
    public bool shadow;
    /// <summary>
    /// 是否循环(测试用)
    /// </summary>
    public bool loop;
    /// <summary>
    /// 图片ppu
    /// </summary>
    public float ppu = 100;
    /// <summary>
    /// 动画片段信息
    /// 注：用于将多个动画clip集成为单个动画资产，减少资产数。
    /// </summary>
    public Segment[] segments = Array.Empty<Segment>();

    //////////////////////////////////////////////////////////////

    /// <summary>
    /// 获取指定帧
    /// </summary>
    /// <param name="index"></param>
    public SpriteAnimationFrame this[int index] {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => frames[index];
        set {
            float delta = value.duration - frames[index].duration;
            frames[index] = value;
            if (delta != 0) {
                RefreshDuration();
            }
        }
    }

    /// <summary>
    /// 帧数
    /// </summary>
    public int FrameCount {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => frames.Length;
        set {
            int preLength = frames.Length;
            if (preLength == value) {
                return;
            }
            Array.Resize(ref frames, value);
            for (int index = preLength; index < value; index++) {
                frames[index] = new SpriteAnimationFrame();
            }
            RefreshDuration();
        }
    }

    /// <summary>
    /// 最后一帧
    /// </summary>
    public SpriteAnimationFrame LastFrame {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => frames[frames.Length - 1];
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
    /// 根据播放时间搜索关联的帧号
    /// 
    /// 注意：该方法只能在正确维护帧信息缓存的情况下调用。
    /// </summary>
    /// <param name="time">搜索参数</param>
    /// <param name="endTime">搜索截止的时间</param>
    /// <returns>如果大于总播放时间，则固定返回最后一帧</returns>
    public int SearchFrameByTime(float time, out float endTime) {
        return SearchFrameByTime(time, 0, frames.Length - 1, out endTime);
    }

    /// <summary>
    /// 根据播放时间搜索关联的帧号
    /// 
    /// 注意：该方法只能在正确维护帧信息缓存的情况下调用。
    /// </summary>
    /// <param name="time">搜索参数</param>
    /// <param name="endTime">搜索截止的时间</param>
    /// <param name="startIndex">开始帧</param>
    /// <param name="endIndex">结束帧</param>
    /// <returns>如果大于总播放时间，则固定返回最后一帧</returns>
    public int SearchFrameByTime(float time, int startIndex, int endIndex, out float endTime) {
        endTime = 0;
        for (int index = startIndex; index <= endIndex; index++) {
            endTime += frames[index].duration;
            if (time <= endTime) return index;
        }
        return endIndex;
    }

    /// <summary>
    /// 获取子区间持续时间
    /// </summary>
    /// <param name="startIndex">开始索引，包含</param>
    /// <param name="endIndex">结束索引，包含</param>
    /// <returns></returns>
    public float GetDuration(int startIndex, int endIndex) {
        float subDuration = 0;
        for (int index = startIndex; index <= endIndex; index++) {
            subDuration += frames[index].duration;
        }
        return subDuration;
    }

    /// <summary>
    /// 添加帧
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="index"></param>
    public void AddFrame(SpriteAnimationFrame frame, int index = -1) {
        if (index == -1) {
            index = frames.Length;
        }
        ArrayUtil.Insert(ref frames, index, frame);
        duration += frame.duration;
    }

    /// <summary>
    /// 批量添加
    /// </summary>
    /// <param name="targetFrames"></param>
    public void AddFrames(List<SpriteAnimationFrame> targetFrames) {
        if (targetFrames.Count == 0) {
            return;
        }
        int prevLen = frames.Length;
        Array.Resize(ref frames, prevLen + targetFrames.Count);
        foreach (var frame in targetFrames) {
            frames[prevLen++] = frame;
            duration += frame.duration;
        }
    }

    /// <summary>
    /// 删除指定帧
    /// </summary>
    /// <param name="index"></param>
    public void RemoveFrame(int index) {
        if (index < 0 || index >= frames.Length) {
            return;
        }
        ArrayUtil.RemoveAt(ref frames, index);
        RefreshDuration();
    }

    /// <summary>
    /// 确保时间的正确性
    /// </summary>
    private void OnEnable() {
        // 处理数据兼容
        if (ppu <= 0) {
            ppu = 100;
        }
        RefreshDuration();
    }

#if UNITY_EDITOR
    private void Reset() {
        frames = Array.Empty<SpriteAnimationFrame>();
        duration = 0;
        segments = Array.Empty<Segment>();
    }

    /// <summary>
    /// 设置所有帧的间隔
    /// </summary>
    /// <param name="frameInterval">帧间隔</param>
    public void SetFrameInterval(float frameInterval) {
        duration = 0;
        for (int index = 0; index < frames.Length; index++) {
            var frame = frames[index];
            frames[index].duration = frameInterval;
            duration += frame.duration;
        }
    }

    /// <summary>
    /// 设置所有帧的间隔
    /// </summary>
    /// <param name="frameInterval">帧间隔</param>
    public void AddFrameInterval(float frameInterval) {
        duration = 0;
        for (int index = 0; index < frames.Length; index++) {
            var frame = frames[index];
            frames[index].duration += frameInterval;
            duration += frame.duration;
        }
    }

    /// <summary>
    /// 线性插值帧间隔
    /// </summary>
    /// <param name="frameInterval">帧间隔</param>
    public void LerpFrameInterval(float frameInterval) {
        if (frames.Length <= 1) return;
        float baseValue = frames[0].duration;
        duration = baseValue;
        for (int index = 1; index < frames.Length; index++) {
            var frame = frames[index];
            frames[index].duration = baseValue + frameInterval * index;
            duration += frame.duration;
        }
    }

    /// <summary>
    /// 设置所有帧的坐标
    /// </summary>
    /// <param name="position"></param>
    public void SetFramePosition(Vector2 position) {
        for (int index = 0; index < frames.Length; index++) {
            frames[index].position = position;
        }
    }

    /// <summary>
    /// 设置所有帧的坐标
    /// </summary>
    /// <param name="position"></param>
    public void AddFramePosition(Vector2 position) {
        for (int index = 0; index < frames.Length; index++) {
            frames[index].position += position;
        }
    }

    /// <summary>
    /// 线性插值帧坐标
    /// </summary>
    /// <param name="position"></param>
    public void LerpFramePosition(Vector2 position) {
        if (frames.Length <= 1) return;
        Vector2 baseValue = frames[0].position;
        for (int index = 1; index < frames.Length; index++) {
            frames[index].position = baseValue + position * index;
        }
    }

    /// <summary>
    /// 设置所有帧的缩放
    /// </summary>
    /// <param name="scale"></param>
    public void SetFrameScale(Vector2 scale) {
        for (int index = 0; index < frames.Length; index++) {
            frames[index].scale = scale;
        }
    }

    /// <summary>
    /// 设置所有帧的缩放
    /// </summary>
    /// <param name="scale"></param>
    public void AddFrameScale(Vector2 scale) {
        for (int index = 0; index < frames.Length; index++) {
            frames[index].scale += scale;
        }
    }

    /// <summary>
    /// 线性插值帧缩放
    /// </summary>
    /// <param name="scale"></param>
    public void LerpFrameScale(Vector2 scale) {
        if (frames.Length <= 1) return;
        Vector2 baseValue = frames[0].scale;
        for (int index = 1; index < frames.Length; index++) {
            frames[index].scale = baseValue + scale * index;
        }
    }

    /// <summary>
    /// 设置所有帧的旋转值
    /// </summary>
    /// <param name="rotation"></param>
    public void SetFrameRotation(float rotation) {
        for (int index = 0; index < frames.Length; index++) {
            frames[index].rotation = rotation;
        }
    }

    /// <summary>
    /// 设置所有帧的旋转值
    /// </summary>
    /// <param name="rotation"></param>
    public void AddFrameRotation(float rotation) {
        for (int index = 0; index < frames.Length; index++) {
            frames[index].rotation += rotation;
        }
    }

    /// <summary>
    /// 线性插值帧旋转
    /// </summary>
    /// <param name="rotation"></param>
    public void LerpFrameRotation(float rotation) {
        if (frames.Length <= 1) return;
        float baseValue = frames[0].rotation;
        for (int index = 1; index < frames.Length; index++) {
            frames[index].rotation = baseValue + rotation * index;
        }
    }

    /// <summary>
    /// 同步帧动画每帧的时间
    ///
    /// 注：主要用于同步身体各个部件之间的帧动画时长。
    /// </summary>
    /// <param name="source"></param>
    /// <param name="target"></param>
    public static void SyncFrameDuration(SpriteAnimationClip source, SpriteAnimationClip target) {
        if (source == target) {
            return;
        }
        if (source.FrameCount != target.FrameCount) {
            Debug.LogWarning($"{source.name}.FrameCount != {target.name}.FrameCount");
        }
        int len = Mathf.Min(source.FrameCount, target.FrameCount);
        for (int i = 0; i < len; i++) {
            SpriteAnimationFrame sourceFrame = source.frames[i];
            target.frames[i].duration = sourceFrame.duration;
        }
        target.RefreshDuration();
    }

    /// <summary>
    /// 同步帧坐标
    /// </summary>
    /// <param name="source"></param>
    /// <param name="target"></param>
    public static void SyncFramePosition(SpriteAnimationClip source, SpriteAnimationClip target) {
        if (source == target) {
            return;
        }
        if (source.FrameCount != target.FrameCount) {
            Debug.LogWarning($"{source.name}.FrameCount != {target.name}.FrameCount");
        }
        int len = Mathf.Min(source.FrameCount, target.FrameCount);
        for (int i = 0; i < len; i++) {
            SpriteAnimationFrame sourceFrame = source.frames[i];
            target.frames[i].position = sourceFrame.position;
        }
    }

    /// <summary>
    /// 同步帧旋转值
    /// </summary>
    /// <param name="source"></param>
    /// <param name="target"></param>
    public static void SyncFrameRotation(SpriteAnimationClip source, SpriteAnimationClip target) {
        if (source == target) {
            return;
        }
        if (source.FrameCount != target.FrameCount) {
            Debug.LogWarning($"{source.name}.FrameCount != {target.name}.FrameCount");
        }
        int len = Mathf.Min(source.FrameCount, target.FrameCount);
        for (int i = 0; i < len; i++) {
            SpriteAnimationFrame sourceFrame = source.frames[i];
            target.frames[i].rotation = sourceFrame.rotation;
        }
    }

    /// <summary>
    /// 按照基础动画的图片(名)序列重排序其它动画的图片序列
    /// 
    /// 注：需要保持动画归属的图组数据一致，可通过工具同步。
    /// </summary>
    public static void SyncFrameOrder(SpriteAnimationClip source, SpriteAnimationClip target) {
        if (source == target) {
            return;
        }
        if (source.FrameCount != target.FrameCount) {
            Debug.LogWarning($"{source.name}.FrameCount != {target.name}.FrameCount");
        }
        target.FrameCount = source.FrameCount;
        for (int index = 0; index < source.frames.Length; index++) {
            SpriteAnimationFrame sourceFrame = source.frames[index];
            SpriteAnimationFrame targetFrame = target.frames[index];
            targetFrame.spritePath.localPath = sourceFrame.spritePath.localPath;
            targetFrame.spritePath.localId = sourceFrame.spritePath.localId;
        }
    }
#endif

    /// <summary>
    /// 子动画片段
    /// </summary>
    [Serializable]
    public struct Segment
    {
        /// <summary>
        /// 动画名
        /// </summary>
        public string name;
        /// <summary>
        /// 动画开始帧
        /// </summary>
        public int startFrame;
        /// <summary>
        /// 动画结束帧
        /// </summary>
        public int endFrame;
        /// <summary>
        /// 是否开启阴影
        /// </summary>
        public bool shadow;
        /// <summary>
        /// 是否循环播放
        /// </summary>
        public bool loop;
        /// <summary>
        /// 关联的clip(缓存)
        /// </summary>
        public SpriteAnimationClip clip { get; internal set; }
    }
}
}