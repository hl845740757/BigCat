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

namespace Wjybxx.BigCat.Animator
{
/// <summary>
/// 基于帧动画的(角色)动作
///
/// 注：该对象属于模型资产文件的一部分。
/// </summary>
[Serializable]
public sealed class FrameAnimationAction : ISerializationCallbackReceiver
{
    /// <summary>
    /// 动作名
    /// </summary>
    [Tooltip("动作名，snake_case")]
    public string name;
    /// <summary>
    /// 动画资源
    ///
    /// 注：将模型的所有动作刻录在单个Clip上的加载效率更高。
    /// </summary>
    public FrameAnimationClip clip;
    /// <summary>
    /// 动画启动帧
    /// </summary>
    [Tooltip("动画开始帧，包含")]
    public int startFrame;
    /// <summary>
    /// 动画结束帧
    /// </summary>
    [Tooltip("动画结束帧，包含")]
    public int endFrame;
    /// <summary>
    /// 动画权重
    /// </summary>
    [Tooltip("动画融合权重，如果不存在A => B的动画融合配置，则双方都使用默认额权重值")]
    public float weight = 0.5f;
    /// <summary>
    /// 动画偏移
    /// </summary>
    [Tooltip("动画偏移")]
    public Vector2 offset;

    // TODO 受击碰撞盒？
    public FrameAnimationAction() {
    }

    public FrameAnimationAction(FrameAnimationAction src) {
        this.name = src.name;
        this.clip = src.clip;
        this.startFrame = src.startFrame;
        this.endFrame = src.endFrame;
        this.weight = src.weight;
        this.offset = src.offset;
    }

    public void OnBeforeSerialize() {

    }

    public void OnAfterDeserialize() {
        // endFrame = Math.Max(startFrame, endFrame);
    }
}
}