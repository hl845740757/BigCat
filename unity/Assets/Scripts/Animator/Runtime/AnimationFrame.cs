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
using Wjybxx.Commons.Attributes;

namespace Wjybxx.BigCat.Animator
{
/// <summary>
/// 动画的一帧，非资源加载单位
/// </summary>
[Serializable]
public struct AnimationFrame
{
    /// <summary>
    /// 关联的图片
    /// </summary>
    public Sprite sprite;
    /// <summary>
    /// 该帧的持续时长
    ///
    /// PS: 虽然也可以替换为该帧的结束时间，但在编辑器中不容易管理。
    /// </summary>
    [Min(0f)]
    public float duration;

    public AnimationFrame(Sprite sprite, float duration) {
        if (duration < 0) {
            throw new Exception("Duration must be greater than 0");
        }
        this.sprite = sprite;
        this.duration = duration;
    }

    public AnimationFrame WithDuration(float duration) {
        return new AnimationFrame(sprite, duration);
    }
}
}