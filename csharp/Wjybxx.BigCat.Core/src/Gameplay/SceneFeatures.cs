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

#if UNITY_2021_3_OR_NEWER
using UnityEngine;
#endif

namespace Wjybxx.BigCat.Gameplay
{
/// <summary>
/// 场景特征值
/// </summary>
[Flags]
public enum SceneFeatures
{
    /// <summary>
    /// 启用是缩放时间队列
    /// </summary>
#if UNITY_2021_3_OR_NEWER
    [Tooltip("是否启用非缩放时间任务队列；如果确定不需要非缩放时间定时器，可以关闭该选项以减少开销")]
#endif
    EnableUnscaledTimeQueue = 0x01,
    /// <summary>
    /// 启用帧数队列
    /// </summary>
#if UNITY_2021_3_OR_NEWER
    [Tooltip("是否启用帧数时间任务队列；如果存在按帧Update的逻辑，则需要启用该选项")]
#endif
    EnableFrameQueue = 0x02,

    /// <summary>
    /// Scene默认启用非缩放时间队列
    /// </summary>
    Defaults = EnableUnscaledTimeQueue,
}
}