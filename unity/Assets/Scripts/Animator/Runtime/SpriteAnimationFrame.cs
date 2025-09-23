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
using UnityEngine.U2D;

namespace Wjybxx.BigCat.Animator
{
/// <summary>
/// 帧动画的一帧，非资源加载单位
/// </summary>
[Serializable]
public struct SpriteAnimationFrame
{
    /// <summary>
    /// 关联的图片
    ///
    /// 注：缓存字段，运行时使用。
    /// </summary>
    [NonSerialized]
    public Sprite sprite;
    /// <summary>
    /// 关联的图片路径
    /// 
    /// 注：
    /// 1.如果是图集动画，则该路径是文件名；
    /// 2.<see cref="SpriteAtlas"/>不支持根据Index获取Sprite，效率上可能有一定缺陷。
    /// </summary>
    public string spritePath;
    /// <summary>
    /// 该帧的持续时长
    ///
    /// 注：真实时间或帧数，取决于播放器。
    /// </summary>
    [Min(0f)]
    public float duration;
    /// <summary>
    /// 动画偏移
    /// </summary>
    public Vector2 offset;
    /// <summary>
    /// z轴旋转
    /// </summary>
    [Tooltip("顺时针旋转值")]
    public float rotation;

    public SpriteAnimationFrame(string spritePath, float duration) {
        this.spritePath = spritePath;
        this.duration = duration;

        this.sprite = null;
        this.offset = default;
        this.rotation = 0;
    }

    public SpriteAnimationFrame(string spritePath, float duration, Vector2 offset, float rotation) {
        this.sprite = null;
        this.spritePath = spritePath;
        this.duration = duration;
        this.offset = offset;
        this.rotation = rotation;
    }

    public SpriteAnimationFrame(Sprite sprite, string spritePath, float duration, Vector2 offset, float rotation) {
        this.sprite = sprite;
        this.spritePath = spritePath;
        this.duration = duration;
        this.offset = offset;
        this.rotation = rotation;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SpriteAnimationFrame WithSprite(Sprite sprite) {
        return new SpriteAnimationFrame(sprite, spritePath, duration, offset, rotation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SpriteAnimationFrame WithDuration(float duration) {
        return new SpriteAnimationFrame(sprite, spritePath, duration, offset, rotation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SpriteAnimationFrame WithOffset(Vector2 offset) {
        return new SpriteAnimationFrame(sprite, spritePath, duration, offset, rotation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SpriteAnimationFrame WithRotation(float rotation) {
        return new SpriteAnimationFrame(sprite, spritePath, duration, offset, rotation);
    }
}
}