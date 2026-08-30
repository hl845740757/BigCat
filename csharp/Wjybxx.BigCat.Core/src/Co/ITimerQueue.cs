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
using System.Threading;
using Wjybxx.Commons.Concurrent;

namespace Wjybxx.BigCat.Co
{
/// <summary>
/// 定时器队列的基础抽象接口（不包含生命周期相关函数）
/// 注：返回的<see cref="ValueFuture.TaskId"/>即为TimerId。
/// </summary>
public interface ITimerQueue
{
    ValueFuture<T> Schedule<T>(in TaskBuilder<T> builder);

    ValueFuture ScheduleAction(Action action, double delay, CancellationToken cancelToken = default);

    ValueFuture ScheduleAction(Action<object> action, object state, double delay, CancellationToken cancelToken = default);

    ValueFuture<T> ScheduleFunc<T>(Func<T> action, double delay, CancellationToken cancelToken = default);

    ValueFuture<T> ScheduleFunc<T>(Func<object, T> action, object state, double delay, CancellationToken cancelToken = default);

    ValueFuture ScheduleWithFixedDelay(Action action, double delay, double period, CancellationToken cancelToken = default);

    ValueFuture ScheduleWithFixedDelay(Action<object> action, object state, double delay, double period, CancellationToken cancelToken = default);

    ValueFuture ScheduleAtFixedRate(Action action, double delay, double period, CancellationToken cancelToken = default);

    ValueFuture ScheduleAtFixedRate(Action<object> action, object state, double delay, double period, CancellationToken cancelToken = default);

    /// <summary>
    /// 暂停Timer
    /// </summary>
    /// <param name="timerId"></param>
    void Pause(long timerId);

    /// <summary>
    /// 恢复Timer
    /// 注：尽量避免修改固定帧率类定时任务的触发时间。
    /// </summary>
    /// <param name="timerId"></param>
    /// <param name="nextDelay">恢复后的首次延迟</param>
    void Resume(long timerId, double? nextDelay = null);

    /// <summary>
    /// 取消Timer
    /// </summary>
    /// <param name="timerId"></param>
    void Cancel(long timerId);
}
}