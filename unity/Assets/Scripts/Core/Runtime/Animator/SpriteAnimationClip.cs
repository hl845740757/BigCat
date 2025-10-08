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
/// 序列帧动画数据抽象
/// </summary>
[CreateAssetMenu(menuName = "BigCat/SpriteAnimationClip", fileName = "NewAnimationClip")]
public sealed class SpriteAnimationClip : ScriptableObject
{
    /// <summary>
    /// 动画的帧数据
    /// </summary>
    public SpriteAnimationFrame[] frames = Array.Empty<SpriteAnimationFrame>();
    /// <summary>
    /// 动画事件
    /// </summary>
    public AnimationEvent[] events = Array.Empty<AnimationEvent>();

    /// <summary>
    /// 动画总时长（缓存值）
    ///
    /// 注：运行时可调用<see cref="RefreshDuration"/>确保缓存值的正确性。
    /// </summary>
    [ReadOnly]
    public float duration;
    /// <summary>
    /// 是否循环播放
    /// </summary>
    public bool loop = true;
    /// <summary>
    /// 动画融合的默认权重
    /// </summary>
    public float weight = 0.5f;

    //////////////////////////////////////////////////////////////

    /// <summary>
    /// 获取指定帧
    /// </summary>
    /// <param name="index"></param>
    public SpriteAnimationFrame this[int index] {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => frames[index];
        set {
            // 根据delta计算其实会有一点误差，我们在序列化保存前再纠正一次
            float delta = value.duration - frames[index].duration;
            frames[index] = value;
            duration += delta;
        }
    }

    /// <summary>
    /// 帧数
    /// </summary>
    public int FrameCount {
        get => frames.Length;
        set {
            int preLength = frames.Length;
            Array.Resize(ref frames, value);
            // 扩展数据时不可以为null
            for (int index = preLength; index < value; index++) {
                frames[index] = new SpriteAnimationFrame();
            }
            RefreshDuration();
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
    public void AddFrame(SpriteAnimationFrame frame, int index = -1) {
        if (index == -1) {
            Array.Resize(ref frames, frames.Length + 1);
            frames[frames.Length - 1] = frame;
        } else {
            ArrayUtil.Insert(ref frames, index, frame);
        }
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
        var frame = frames[index];
        ArrayUtil.RemoveAt(ref frames, index);
        duration -= frame.duration;
    }

    /// <summary>
    /// 设置所有帧的间隔
    /// </summary>
    /// <param name="frameInterval">帧间隔</param>
    public void SetFrameInterval(float frameInterval) {
        for (int index = 0; index < frames.Length; index++) {
            var frame = frames[index];
            frames[index] = frame.WithDuration(frameInterval);
        }
        duration = frames.Length * frameInterval;
    }

    /// <summary>
    /// 设置所有帧的坐标
    /// </summary>
    /// <param name="position"></param>
    public void SetFramePosition(Vector2 position) {
        for (int index = 0; index < frames.Length; index++) {
            var frame = frames[index];
            frames[index] = frame.WithPosition(position);
        }
    }

    /// <summary>
    /// 设置所有帧的坐标
    /// </summary>
    /// <param name="position"></param>
    public void AddFramePosition(Vector2 position) {
        for (int index = 0; index < frames.Length; index++) {
            var frame = frames[index];
            frames[index] = frame.WithPosition(frame.position + position);
        }
    }

    /// <summary>
    /// 线性插值帧坐标
    /// </summary>
    /// <param name="position"></param>
    public void LerpFramePosition(Vector2 position) {
        if (frames.Length <= 1) return;
        Vector2 baseOffset = frames[0].position;
        for (int index = 1; index < frames.Length; index++) {
            var frame = frames[index];
            frames[index] = frame.WithPosition(baseOffset + position * index);
        }
    }

    /// <summary>
    /// 设置所有帧的旋转值
    /// </summary>
    /// <param name="rotation"></param>
    public void SetFrameRotation(float rotation) {
        for (int index = 0; index < frames.Length; index++) {
            var frame = frames[index];
            frames[index] = frame.WithRotation(rotation);
        }
    }

    /// <summary>
    /// 设置所有帧的旋转值
    /// </summary>
    /// <param name="rotation"></param>
    public void AddFrameRotation(float rotation) {
        for (int index = 0; index < frames.Length; index++) {
            var frame = frames[index];
            frames[index] = frame.WithRotation(frame.rotation + rotation);
        }
    }

    /// <summary>
    /// 线性插值帧旋转
    /// </summary>
    /// <param name="rotation"></param>
    public void LerpFrameRotation(float rotation) {
        if (frames.Length <= 1) return;
        float baseRotation = frames[0].rotation;
        for (int index = 1; index < frames.Length; index++) {
            var frame = frames[index];
            frames[index] = frame.WithRotation(baseRotation + rotation * index);
        }
    }

    /// <summary>
    /// 应用目标图组（贴图）
    /// 注：该接口仅修改Sprite，不修改配置的路径引用。
    /// </summary>
    /// <param name="spriteGroup"></param>
    public void ApplySpriteGroup(SpriteGroup spriteGroup) {
        for (int index = 1; index < frames.Length; index++) {
            var frame = frames[index];
            frame.sprite = spriteGroup.GetSprite(frame.spritePath.index);
        }
    }

#if UNITY_EDITOR
    private void Reset() {
        frames = Array.Empty<SpriteAnimationFrame>();
        events = Array.Empty<AnimationEvent>();
        duration = 0;
        weight = 0.5f;
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
            target.frames[i] = target.frames[i].WithDuration(sourceFrame.duration);
        }
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
            target.frames[i] = target.frames[i].WithPosition(sourceFrame.position);
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
            target.frames[i] = target.frames[i].WithRotation(sourceFrame.rotation);
        }
    }

    /// <summary>
    /// 按照基础动画的图片(名)序列重排序其它动画的图片序列
    /// </summary>
    /// <param name="source"></param>
    /// <param name="target"></param>
    /// <param name="groupPath">目标图组</param>
    public static void SyncFrameOrder(SpriteAnimationClip source, SpriteAnimationClip target,
                                      string groupPath) {
        if (source == target) {
            return;
        }
        if (source.FrameCount != target.FrameCount) {
            Debug.LogWarning($"{source.name}.FrameCount != {target.name}.FrameCount");
        }
        target.FrameCount = source.FrameCount;
        for (int index = 0; index < source.frames.Length; index++) {
            SpriteAnimationFrame sourceFrame = source.frames[index];
            SpriteAnimationFrame targetFrame = target[index];
            targetFrame.spritePath.groupPath = groupPath;
            targetFrame.spritePath.index = sourceFrame.spritePath.index;
        }
    }
#endif
}
}