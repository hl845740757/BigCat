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

namespace Wjybxx.BigCat.UI
{
/// <summary>
/// 窗口特征值
/// </summary>
[Flags]
public enum WindowFeatures
{
    /// <summary>
    /// 启用是缩放时间队列
    /// </summary>
    [Tooltip("是否启用非缩放时间任务队列；如果确定不需要非缩放时间定时器，可以关闭该选项以减少开销")]
    EnableUnscaledTimeQueue = 0x01,
    /// <summary>
    /// 启用帧数队列
    /// </summary>
    [Tooltip("是否启用帧数时间任务队列；如果存在按帧Update的逻辑，则需要启用该选项")]
    EnableFrameQueue = 0x02,

    /// <summary>
    /// 不可手动关闭
    /// </summary>
    [Tooltip("是否是常驻UI -- 常驻UI不会被模糊关闭，只能强制关闭")]
    Unclosable = 0x10,
    /// <summary>
    /// 跨桌面UI
    /// </summary>
    [Tooltip("是否是跨桌面UI -- 跨桌面UI在切换桌面时，自动切换到新桌面，常用于主界面UI")]
    CrossDesktop = 0x20,

    /// <summary>
    /// Window默认不需要启用非缩放时间队列
    /// </summary>
    Defaults = 0,
}
}