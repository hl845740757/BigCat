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
/// </summary>
[CreateAssetMenu(menuName = "BigCat/SpriteAnimationClip", fileName = "NewAnimationClip")]
public sealed class SpriteAnimationClip : ScriptableObject
{
    /// <summary>
    /// 动画的帧数据
    ///
    /// 注：在运行时可以使用ref以避免拷贝。
    /// </summary>
    [HideInInspector]
    public SpriteAnimationFrame[] frames = Array.Empty<SpriteAnimationFrame>();

    /// <summary>
    /// 是否是图集动画
    /// </summary>
    [Tooltip("是否保存为Sprite文件的简单名，如果Sprite存储在SpriteAtlas中，则可以勾选")]
    public bool saveAsSpriteName = true;
    /// <summary>
    /// 
    /// </summary>
    [Tooltip("动画融合的默认权重")]
    public float weight;
    /// <summary>
    /// 动画总时长（缓存值）
    ///
    /// 注：运行时可调用<see cref="RefreshDuration"/>确保缓存值的正确性。
    /// </summary>
    [Tooltip("动画总时长缓存")]
    [ReadOnly]
    public float duration;

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
            Array.Resize(ref frames, value);
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
    /// 设置所有帧的偏移
    /// </summary>
    /// <param name="offset"></param>
    public void SetFrameOffset(Vector2 offset) {
        for (int index = 0; index < frames.Length; index++) {
            var frame = frames[index];
            frames[index] = frame.WithOffset(offset);
        }
    }

    /// <summary>
    /// 设置所有帧的偏移
    /// </summary>
    /// <param name="offset"></param>
    public void AddFrameOffset(Vector2 offset) {
        for (int index = 0; index < frames.Length; index++) {
            var frame = frames[index];
            frames[index] = frame.WithOffset(frame.offset + offset);
        }
    }

    /// <summary>
    /// 线性插值帧偏移
    /// </summary>
    /// <param name="offset"></param>
    public void LerpFrameOffset(Vector2 offset) {
        if (frames.Length <= 1) return;
        Vector2 baseOffset = frames[0].offset;
        for (int index = 1; index < frames.Length; index++) {
            var frame = frames[index];
            frames[index] = frame.WithOffset(baseOffset + offset * index);
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

#if UNITY_EDITOR
    private void Reset() {
        frames = Array.Empty<SpriteAnimationFrame>();
        duration = 0;
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
    /// 同步帧偏移
    /// </summary>
    /// <param name="source"></param>
    /// <param name="target"></param>
    public static void SyncFrameOffset(SpriteAnimationClip source, SpriteAnimationClip target) {
        if (source == target) {
            return;
        }
        if (source.FrameCount != target.FrameCount) {
            Debug.LogWarning($"{source.name}.FrameCount != {target.name}.FrameCount");
        }
        int len = Mathf.Min(source.FrameCount, target.FrameCount);
        for (int i = 0; i < len; i++) {
            SpriteAnimationFrame sourceFrame = source.frames[i];
            target.frames[i] = target.frames[i].WithOffset(sourceFrame.offset);
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
    public static void SyncFrameOrder(SpriteAnimationClip source, SpriteAnimationClip target) {
        if (source == target) {
            return;
        }
        if (source.FrameCount != target.FrameCount) {
            Debug.LogWarning($"{source.name}.FrameCount != {target.name}.FrameCount");
        }
        // 先建立索引，方便快速查询
        Dictionary<string, SpriteAnimationFrame> frameDic = new();
        foreach (SpriteAnimationFrame frame in target.frames) {
            if (frame.sprite) {
                frameDic.TryAdd(frame.sprite.name, frame);
            }
        }
        // 根据基础动画的图片名字重排序
        List<SpriteAnimationFrame> frameList = new List<SpriteAnimationFrame>();
        foreach (SpriteAnimationFrame sourceFrame in source.frames) {
            if (sourceFrame.sprite && frameDic.TryGetValue(sourceFrame.sprite.name, out SpriteAnimationFrame frame)) {
                frameList.Add(frame);
            } else {
                frameList.Add(default);
            }
        }
        target.FrameCount = 0; // 先清理再批量添加的效率更好
        target.AddFrames(frameList);
    }
#endif
}
}