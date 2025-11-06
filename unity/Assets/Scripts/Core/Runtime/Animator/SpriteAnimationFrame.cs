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
using UnityEngine;
using Wjybxx.BigCat.Core;
using Wjybxx.Commons;

namespace Wjybxx.BigCat.Animator
{
/// <summary>
/// 帧动画的一帧
/// </summary>
[Serializable]
public sealed class SpriteAnimationFrame
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
    /// 注：在运行时应当转小写并池化字符串。
    /// </summary>
    public ObjectPath spritePath;
    /// <summary>
    /// 图片坐标
    ///
    /// 注：图片bottom相对角色坐标的位置。
    /// </summary>
    public Vector2 position;
    /// <summary>
    /// 图片缩放
    /// </summary>
    public Vector2 scale = new Vector2(1.0f, 1.0f);
    /// <summary>
    /// z轴旋转(度)
    /// </summary>
    public float rotation;
    /// <summary>
    /// 该帧的持续时长
    /// </summary>
    [Min(0f)]
    public float duration = 0.1f;

    /// <summary>
    /// 受击包围盒
    /// </summary>
    [Tooltip("受击包围盒")]
    public MinMaxAABB[] hurtBoxes = Array.Empty<MinMaxAABB>();
    /// <summary>
    /// 攻击包围盒
    /// </summary>
    [Tooltip("攻击包围盒")]
    public MinMaxAABB[] damageBoxes = Array.Empty<MinMaxAABB>();
    /// <summary>
    /// 攻击盒插值函数
    ///
    /// 注：0表示HitBox就是最终形状，即离散的AABB。
    /// </summary>
    [Tooltip("攻击盒插值函数")]
    public int interp;

    /// <summary>
    /// 阴影
    /// </summary>
    public bool shadow = true;
    /// <summary>
    /// 色调
    /// </summary>
    public Color32 tint = new Color32(255, 255, 255, 255);

    public SpriteAnimationFrame() {
        spritePath.type = (int)ObjectPathType.SpriteOfGroup;
    }
}
}