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
using System.Runtime.CompilerServices;
using UnityEngine;
using Wjybxx.BigCat.Assetor;
using Wjybxx.BigCat.Core;
using Wjybxx.Commons;

namespace Wjybxx.BigCat.Animator
{
/// <summary>
/// 动画播放控制器
/// 
/// 注：该控制器适用于普通场景，战斗系统使用定制的控制器。
/// </summary>
public class SpriteAnimationCtrl
{
    /// <summary>
    /// 渲染器
    /// </summary>
    public readonly SpriteRenderer renderer;
    /// <summary>
    /// 当前控制的transform(应用图片偏移)
    /// 注：如果为null，则表示不应该帧动画偏移。
    /// </summary>
    public readonly Transform transform;

    // 动画播放上下文，除调试外应避免修改
    /// <summary>
    /// 当前播放的动画
    /// </summary>
    public SpriteAnimationClip clip;
    /// <summary>
    /// 环绕模式
    /// </summary>
    public EWrapMode wrapMode;
    /// <summary>
    /// 播放区间
    /// </summary>
    public int startFrame;
    public int endFrame;
    /// <summary>
    /// 当前播放时间(只读)
    ///
    /// 注：time在Play状态下是一直前进的，要想获取动画对应的采样时间，请使用<see cref="SampleTime"/>。
    /// </summary>
    public float time;
    /// <summary>
    /// 当前帧号
    /// </summary>
    private int index;
    /// <summary>
    /// 当前帧结束时间
    /// </summary>
    private float threshold;
    /// <summary>
    /// 图组加载句柄
    /// </summary>
    private AssetHandle groupHandle;
    /// <summary>
    /// 播放状态
    /// </summary>
    private Status status;
    /// <summary>
    /// 播放方向
    /// </summary>
    private Direction direction;

    public SpriteAnimationCtrl(SpriteRenderer renderer, Transform transform) {
        this.renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        this.transform = transform; // 允许null
    }

    /// <summary>
    /// 是否处于停止状态
    /// </summary>
    public bool IsStopped => status == Status.Stopped;
    /// <summary>
    /// 是否处于播放状态
    /// </summary>
    public bool IsPlaying => status == Status.Playing;
    /// <summary>
    /// 是否处于暂停状态
    /// </summary>
    public bool IsPaused => status == Status.Paused;
    /// <summary>
    /// 获取动画的采样时间(规格化时间)
    /// </summary>
    public float SampleTime => wrapMode.GetSampleTime(time, clip.duration);

    /// <summary>
    /// 当前帧
    /// </summary>
    public SpriteAnimationFrame CurFrame {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => clip[index];
    }
    public bool FlipX {
        get => renderer.flipX;
        set => renderer.flipX = value;
    }
    public bool FlipY {
        get => renderer.flipY;
        set => renderer.flipY = value;
    }
    public int SortingOrder {
        get => renderer.sortingOrder;
        set => renderer.sortingOrder = value;
    }

    #region 流程

    /// <summary>
    /// 播放动画
    /// </summary>
    /// <param name="clip">动画片段</param>
    /// <param name="wrapMode">环绕模式</param>
    /// <param name="startFrame">开始帧</param>
    /// <param name="endFrame">结束帧</param>
    /// 
    public void Play(SpriteAnimationClip clip, EWrapMode wrapMode, int startFrame = 0, int endFrame = -1) {
        this.clip = clip;
        this.wrapMode = wrapMode;
        this.startFrame = startFrame;
        this.endFrame = endFrame < 0 ? clip.FrameCount - 1 : endFrame;
        this.index = startFrame;
        this.time = 0;
        this.status = Status.Playing;
        this.direction = Direction.Ping;
        this.threshold = clip[index].duration;
        //
        ApplyFrame();
        CheckSpriteStatus();
    }

    /// <summary>
    /// 暂停播放
    /// </summary>
    public void Pause() {
        if (this.status == Status.Playing) {
            this.status = Status.Paused;
        }
    }

    /// <summary>
    /// 恢复播放
    /// </summary>
    public void Resume() {
        if (this.status == Status.Paused) {
            this.status = Status.Playing;
        }
    }

    /// <summary>
    /// 停止播放
    /// </summary>
    public void Stop() {
        this.status = Status.Stopped;
        this.clip = null;
        this.groupHandle = default;
    }

    /// <summary>
    /// 更新动画
    ///
    /// 注；如果图片是异步加载的，则最后一张图片可能在播放结束时仍未完成加载；要想确保图片显示出来，需要继续Update；
    /// 否则应再调用一次<see cref="Stop"/>以避免内存泄漏。
    /// </summary>
    public void Update(float deltaTime) {
        if (status != Status.Playing) {
            CheckSpriteStatus();
            return;
        }
        float availTime = deltaTime;
        do {
            time += availTime;
            availTime = 0;
            if (time > threshold) {
                availTime = time - threshold;
                time = threshold;
                MoveNext();
            }
            CheckSpriteStatus();
        } while (availTime > 0 && IsPlaying);
    }

    #endregion

    #region move-next

    private void MoveNext() {
        switch (wrapMode) {
            case EWrapMode.StopAtEnd: {
                if (index >= endFrame) {
                    index = endFrame;
                    status = Status.Stopped;
                } else {
                    index++;
                    threshold += clip[index].duration;
                    ApplyFrame();
                }
                break;
            }
            case EWrapMode.Clamp: {
                if (index >= endFrame) {
                    index = endFrame;
                    // 继续更新阈值，避免后续更新delta异常
                    threshold += clip[index].duration;
                } else {
                    index++;
                    threshold += clip[index].duration;
                    ApplyFrame();
                }
                break;
            }
            case EWrapMode.Loop: {
                if (index >= endFrame) {
                    index = startFrame;
                } else {
                    index++;
                }
                threshold += clip[index].duration;
                ApplyFrame();
                break;
            }
            case EWrapMode.PingPong: {
                if (direction == Direction.Ping) {
                    if (index >= endFrame) {
                        index = endFrame;
                        direction = Direction.Pong;
                    } else {
                        index++;
                    }
                } else {
                    if (index <= startFrame) {
                        index = startFrame;
                        direction = Direction.Ping;
                    } else {
                        index--;
                    }
                }
                threshold += clip[index].duration;
                ApplyFrame();
                break;
            }
            default: {
                throw new ArgumentOutOfRangeException(nameof(wrapMode));
            }
        }
    }

    private void ApplyFrame() {
        SpriteAnimationFrame frame = clip[index];
        if (transform) {
            Vector2 position = frame.position;
            float rotation = frame.rotation;
            if (renderer.flipX) {
                position.x *= -1;
                rotation *= -1;
            }
            if (renderer.flipY) {
                position.y *= -1;
                rotation *= -1;
            }
            transform.localScale = frame.scale;
            transform.localPosition = position / clip.ppu;
            transform.localRotation = rotation == 0
                ? Quaternion.identity
                : Quaternion.Euler(0, 0, rotation);
        }
        renderer.color = frame.tint; // TODO 线性插值
        // 测试环境不使用资源加载
#if UNITY_EDITOR
        if (frame.sprite) {
            renderer.sprite = frame.sprite;
            return;
        }
#endif
        ObjectPath spritePath = frame.spritePath;
        if (spritePath.IsEmpty) {
            renderer.sprite = null;
            groupHandle = default;
        } else {
            groupHandle = ResourceManager.Inst.LoadAssetAsync<SpriteGroup>(spritePath.collection);
        }
    }

    private void CheckSpriteStatus() {
        if (groupHandle.IsNullHandle) {
            return;
        }
        if (groupHandle.IsCompleted) {
            SpriteGroup spriteGroup = groupHandle.GetAsset<SpriteGroup>();
            if (spriteGroup) {
                ObjectPath spritePath = clip[index].spritePath;
                renderer.sprite = spriteGroup.GetSprite(spritePath);
            }
            groupHandle = default;
        }
    }

    #endregion

    private enum Status : byte
    {
        Stopped = 0,
        Playing = 1,
        Paused = 2,
    }

    private enum Direction : byte
    {
        Ping = 0,
        Pong = 1,
    }
}
}