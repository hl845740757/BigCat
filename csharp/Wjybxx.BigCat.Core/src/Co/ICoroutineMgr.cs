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
using System.Collections.Generic;
using Wjybxx.Commons.Concurrent;

namespace Wjybxx.BigCat.Co
{
/// <summary>
/// 协程管理器（调度器）
///
/// 注：
/// 1.协程管理器包含了Timer功能，这可以避免额外的TimerMgr导致的时序问题。
/// 2.由于协程会大规模用到Timer功能，因此id使用long类型。
/// </summary>
public interface ICoroutineMgr
{
    /// <summary>
    /// 关联的事件循环
    ///
    /// 注：任务可以通过<code>await EventLoop</code>切换到事件循环线程。
    /// </summary>
    IEventLoop EventLoop { get; }

    #region 协程接口

    /// <summary>
    /// 启动协程
    /// 
    /// 注：用户必须手动销毁上下文，否则可能导致内存泄漏；如果不关注协程执行结果，可启动后立即销毁。
    /// </summary>
    /// <param name="func"></param>
    /// <param name="startArgs">启动参数</param>
    /// <returns></returns>
    CoroutineUserContext StartCoroutine(Func<CoroutineTaskContext, ValueFuture> func,
                                        CoroutineStartArgs startArgs);

    /// <summary>
    /// 启动协程
    ///
    /// 注：用户必须手动销毁上下文，否则可能导致内存泄漏；如果不关注协程执行结果，可启动后立即销毁。
    /// </summary>
    /// <param name="func"></param>
    /// <param name="startArgs">启动参数</param>
    /// <returns></returns>
    CoroutineUserContext<T, R> StartCoroutine<T, R>(Func<CoroutineTaskContext<T, R>, ValueFuture> func,
                                                    CoroutineStartArgs<T, R> startArgs);

    #endregion

    #region Timer视图

#nullable disable
    /// <summary>
    /// 基于可缩放时间的定时器
    ///
    /// 注：默认在Update阶段调度任务。
    /// </summary>
    ITimerMgr TimerMgr { get; }

    /// <summary>
    /// 基于非缩放时间的定时器
    ///
    /// 注：
    /// 1.默认在EarlyUpdate阶段调度任务。
    /// 2.非缩放时间的定时器仅支持场景循环之外的阶段 —— 即场景内业务一般不应该使用该定时器。
    /// </summary>
    ITimerMgr UnscaledTimerMgr { get; }

    /// <summary>
    /// 基于帧数的定时器
    ///
    /// 注：默认在Update阶段调度任务。
    /// </summary>
    ITimerMgr FrameTimerMgr { get; }
#nullable restore

    #endregion

    #region 公共接口

    /// <summary>
    /// 取消协程或定时器
    /// </summary>
    /// <param name="coroutineId">协程id或定时任务id</param>
    /// <param name="interruptIfRunning">是否中断协程</param>
    void Cancel(long coroutineId, bool interruptIfRunning = false);

    /// <summary>
    /// 批量取消协程或定时器
    /// </summary>
    /// <param name="coroutineIds">协程id或定时任务id</param>
    /// <param name="interruptIfRunning">是否中断协程</param>
    void Cancel(List<long> coroutineIds, bool interruptIfRunning = false);

    #endregion
}
}