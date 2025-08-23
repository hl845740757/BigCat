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
using Wjybxx.BigCat.Gameplay;
using Wjybxx.Commons.Attributes;
using Wjybxx.Commons.Concurrent;

namespace Wjybxx.BigCat.Co
{
/// <summary>
/// Timer接口
///
/// 注：
/// 1.尽量避免FixedUpdate阶段的周期性任务。
/// 2.时间可以是帧数，为避免定义过多的接口类型，我们统一使用double类型。
/// 3.默认只在执行时检测取消信号，不主动监听取消令牌；用户可以通过<see cref="TaskOptions"/>启用，或是自己监听然后调用Cancel接口。
/// 4.如果不关注Timer的执行结果，务必调用<see cref="ValueFuture.Forget"/>。
/// 5.为保证安全性，延迟时间为0时，默认不在当前帧触发；如果需要当前帧触发，可通过<see cref="TaskBuilder"/>指定额外延迟帧为0实现。
/// </summary>
[NotThreadSafe]
public interface ITimerMgr
{
    /// <summary>
    /// 最近分配的一个Timer的ID
    ///
    /// Q：为什么不直接在各个Schedule方法返回TimerId？
    /// A：如果返回结果类型为元组(Tuple)类型，用户可能忘记处理Future；而方法的参数已经较多，再增加out参数也不明智。
    /// </summary>
    long LastTimerId { get; }

    /// <summary>
    /// 创建一个高度自定义的Timer
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    ValueFuture<T> Schedule<T>(in TaskBuilder<T> builder);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="action">要调度的任务</param>
    /// <param name="delay">延迟时间，秒</param>
    /// <param name="cancelToken">取消令牌</param>
    /// <returns></returns>
    ValueFuture ScheduleAction(Action action, double delay, ICancelToken? cancelToken = null);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="action">要调度的任务</param>
    /// <param name="timerArg">timer参数，注意<see cref="IContext"/>类型</param>
    /// <param name="delay">延迟时间，秒</param>
    /// <returns></returns>
    ValueFuture ScheduleAction(Action<object> action, object timerArg, double delay);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="action">要调度的任务</param>
    /// <param name="delay">延迟时间，秒</param>
    /// <param name="cancelToken">取消令牌</param>
    /// <returns></returns>
    ValueFuture<T> ScheduleFunc<T>(Func<T> action, double delay, ICancelToken? cancelToken = null);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="action">要调度的任务</param>
    /// <param name="timerArg">timer参数，注意<see cref="IContext"/>类型</param>
    /// <param name="delay">延迟时间，秒</param>
    /// <returns></returns>
    ValueFuture<T> ScheduleFunc<T>(Func<object, T> action, object timerArg, double delay);

    /// <summary>
    /// 以固定延迟执行给定的任务
    /// 
    /// 注：FixedDelay只保证两次任务的执行间隔一定大于等于给定延迟。
    /// </summary>
    /// <param name="action">要调度的任务</param>
    /// <param name="delay">首次执行延迟</param>
    /// <param name="period">后续执行间隔</param>
    /// <param name="countLimit">执行次数限制</param>
    /// <param name="cancelToken">取消令牌</param>
    /// <returns></returns>
    ValueFuture ScheduleWithFixedDelay(Action action, double delay, double period, int countLimit = -1, ICancelToken? cancelToken = null);

    /// <summary>
    /// 以固定延迟执行给定的任务
    /// 
    /// 注：FixedDelay只保证两次任务的执行间隔一定大于等于给定延迟。
    /// </summary>
    /// <param name="action">要调度的任务</param>
    /// <param name="timerArg">timer参数，注意<see cref="IContext"/>类型</param>
    /// <param name="delay">首次执行延迟</param>
    /// <param name="period">后续执行间隔</param>
    /// <param name="countLimit">执行次数限制</param>
    /// <returns></returns>
    ValueFuture ScheduleWithFixedDelay(Action<object> action, object timerArg, double delay, double period, int countLimit = -1);

    /// <summary>
    /// 以固定频率执行给定的任务（慎用）
    ///
    /// 注：FixedRate会尽可能保证总的执行次数。
    /// </summary>
    /// <param name="action">要调度的任务</param>
    /// <param name="delay">首次执行延迟</param>
    /// <param name="period">后续执行间隔</param>
    /// <param name="countLimit">执行次数限制</param>
    /// <param name="cancelToken">取消令牌</param>
    /// <returns></returns>
    ValueFuture ScheduleAtFixedRate(Action action, double delay, double period, int countLimit = -1, ICancelToken? cancelToken = null);

    /// <summary>
    /// 以固定频率执行给定的任务（慎用）
    ///
    /// 注：FixedRate会尽可能保证总的执行次数。
    /// </summary>
    /// <param name="action">要调度的任务</param>
    /// <param name="timerArg">timer参数，注意<see cref="IContext"/>类型</param>
    /// <param name="delay">首次执行延迟</param>
    /// <param name="period">后续执行间隔</param>
    /// <param name="countLimit">执行次数限制</param>
    /// <returns></returns>
    ValueFuture ScheduleAtFixedRate(Action<object> action, object timerArg, double delay, double period, int countLimit = -1);

    /// <summary>
    /// 取消协程或定时器
    /// </summary>
    /// <param name="timerId">定时任务id</param>
    void Cancel(long timerId);

    /// <summary>
    /// 批量取消协程或定时器
    /// </summary>
    /// <param name="timerIds">定时任务id</param>
    void Cancel(List<long> timerIds);
}
}