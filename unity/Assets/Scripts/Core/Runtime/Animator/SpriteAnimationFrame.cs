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
using Wjybxx.BigCat.UnityCore;

namespace Wjybxx.BigCat.Animator
{
/// <summary>
/// 帧动画的一帧
/// </summary>
[Serializable]
public sealed class SpriteAnimationFrame : ISerializationCallbackReceiver
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
    /// </summary>
    public SpritePath spritePath;
    /// <summary>
    /// 动画偏移(本地坐标)
    /// </summary>
    [Tooltip("图片本地坐标")]
    public Vector2 position;
    /// <summary>
    /// z轴旋转
    /// (运行时应当进行规格化)
    /// </summary>
    [Tooltip("顺时针旋转值")]
    public float rotation;
    /// <summary>
    /// 该帧的持续时长
    ///
    /// 注：真实时间或帧数，取决于播放器。
    /// </summary>
    [Min(0f)]
    public float duration = 0.1f;
    /// <summary>
    /// 播放结束时间
    /// 
    /// 注：缓存字段，运行时使用；编辑器下预览时也可使用。
    /// </summary>
    [NonSerialized]
    public float endTime;

    /// <summary>
    /// 受击包围盒
    /// </summary>
    [Tooltip("受击包围盒")]
    public AABB[] hurtBoxes = Array.Empty<AABB>();
    /// <summary>
    /// 攻击包围盒
    /// 注：damage比hit更具区分度
    /// </summary>
    [Tooltip("攻击包围盒")]
    public AABB[] damageBoxes = Array.Empty<AABB>();

    /// <summary>
    /// 攻击盒形状
    ///
    /// 注：0表示HitBox就是最终形状，即离散的AABB。
    /// </summary>
    [Tooltip("攻击盒形状，动态绘制")]
    public int graphic;
    /// <summary>
    /// 攻击盒插值函数
    /// </summary>
    [Tooltip("插值函数")]
    public int interp;

    public SpriteAnimationFrame() {
    }

    public SpriteAnimationFrame(SpritePath spritePath, float duration) {
        this.spritePath = spritePath;
        this.duration = duration;

        this.sprite = null;
        this.position = default;
        this.rotation = 0;
    }

    public SpriteAnimationFrame(SpritePath spritePath, float duration,
                                Vector2 position, float rotation) {
        this.sprite = null;
        this.spritePath = spritePath;
        this.duration = duration;
        this.position = position;
        this.rotation = rotation;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SpriteAnimationFrame WithSprite(Sprite sprite) {
        return new SpriteAnimationFrame(spritePath, duration, position, rotation)
        {
            sprite = sprite,
            endTime = endTime,
            damageBoxes = damageBoxes,
            hurtBoxes = hurtBoxes,
            graphic = graphic,
            interp = interp
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SpriteAnimationFrame WithDuration(float duration) {
        return new SpriteAnimationFrame(spritePath, duration, position, rotation)
        {
            sprite = sprite,
            endTime = endTime,
            damageBoxes = damageBoxes,
            hurtBoxes = hurtBoxes,
            graphic = graphic,
            interp = interp,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SpriteAnimationFrame WithPosition(Vector2 position) {
        return new SpriteAnimationFrame(spritePath, duration, position, rotation)
        {
            sprite = sprite,
            endTime = endTime,
            damageBoxes = damageBoxes,
            hurtBoxes = hurtBoxes,
            graphic = graphic,
            interp = interp,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SpriteAnimationFrame WithRotation(float rotation) {
        return new SpriteAnimationFrame(spritePath, duration, position, rotation)
        {
            sprite = sprite,
            endTime = endTime,
            damageBoxes = damageBoxes,
            hurtBoxes = hurtBoxes,
            graphic = graphic,
            interp = interp,
        };
    }

    public void OnBeforeSerialize() {

    }

    public void OnAfterDeserialize() {
#if !UNITY_EDITOR
        spritePath.Intern();
#endif
    }
}
}