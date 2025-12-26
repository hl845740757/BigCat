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
/// 动画环绕模式
///
/// 注；Unity是掩码，因此支持组合，即：ClampOnce、ClampForever、LoopOnce...
/// </summary>
public enum EWrapMode
{
    /// <summary>
    /// 播放到末尾自动结束，并停留在最后一帧
    /// </summary>
    StopAtEnd = 0,
    /// <summary>
    /// 普通循环播放
    /// </summary>
    Loop = 1,
    /// <summary>
    /// ping-pong式循环播放
    /// 注意：单纯通过播放时间确定是Ping还是Pong，可能会因为浮点误差导致错误，因此动画播放逻辑应当维护状态。
    /// </summary>
    PingPong = 2,
    /// <summary>
    /// 动画停留在最后一帧，不触发结束
    /// 注意：播放时间仍然是继续前进的。
    /// </summary>
    Clamp = 3
}

public static class WrapModeExtensions
{
    /// <summary>
    /// 动画播放的误差
    /// </summary>
    public const float EPSILON = 0.0001f;

    /// <summary>
    /// 计算动画在当前播放时间对应的采样时间
    /// </summary>
    /// <param name="mode">环绕模式</param>
    /// <param name="playtime">播放时间</param>
    /// <param name="duration">动画时间</param>
    public static float GetSampleTime(this EWrapMode mode, float playtime, float duration) {
        if (playtime <= 0 || duration <= 0) {
            return 0;
        }
        switch (mode) {
            case EWrapMode.StopAtEnd: {
                return Mathf.Min(playtime, duration);
            }
            case EWrapMode.Loop: {
                return playtime % duration;
            }
            case EWrapMode.PingPong: {
                float cycle = duration * 2f;
                float t = playtime % cycle;
                return t < duration ? t : cycle - t;
            }
            case EWrapMode.Clamp: {
                return Mathf.Clamp(playtime, 0, duration);
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
        }
    }
}
}