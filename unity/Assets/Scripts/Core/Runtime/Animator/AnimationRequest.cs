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
using Wjybxx.Dson.Codec.Attributes;

namespace Wjybxx.BigCat.Animator
{
/// <summary>
/// 动画播放请求
/// </summary>
[DsonSerializable]
public struct AnimationRequest
{
    public string clipPath; // 播放的动画
    public EWrapMode wrapMode; // 动画播放模式
    public int startFrame; // 开始帧
    public int endFrame; // 结束帧
    public Vector2 offset; // 动画偏移
    public AnimationOptions options; // 选项
}

/// <summary>
/// 动画播放选项
/// </summary>
[Flags]
public enum AnimationOptions
{
    None = 0,
    EnableOffset = 0x01, // 启用偏移
    EnableRange = 0x02, // 启用区间
    EnableShadow = 0x04, // 启用阴影
    EnableSortOrder = 0x08, // 启用动画层级
    ReservedMotion = 0x10, // 保留动作配置(禁止自动删除)
    FlipX = 0x20, // X轴翻转
    FlipY = 0x40, // Y轴翻转
    Expand = 0x80, // 展开
}
}