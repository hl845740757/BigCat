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
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Wjybxx.BigCat.Gameplay;
using Wjybxx.Commons.Attributes;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Concurrent;
using static Wjybxx.BigCat.Co.TaskBuilder;

namespace Wjybxx.BigCat.Co
{
/// <summary>
/// 定时任务队列
///
/// 1.由于EventLoop的引用不可变更，因此TimerQueue对象池需要按EventLoop管理。
/// 2.通过CTS取消任务虽然灵活，但成本较高，普通场景更适合通过ID取消。
/// 3.监听取消令牌不是必须的，延迟响应取消信号只是会导致一定时间的内存泄漏，游戏业务很少出现超长延迟的任务。
/// 4.不监听取消信号还允许EventLoop为null，使得适用于非EventLoop框架。
/// 5.时间可以是帧数，为避免定义过多的接口类型，我们统一使用double类型。
/// 6.如果不关注Timer的执行结果，务必调用<see cref="ValueFuture.Forget"/>，否则可能导致对象池失效。
/// </summary>
[NotThreadSafe]
public sealed class TimerQueue : ITimerQueue
{
#nullable disable
    private IEventLoop? _eventLoop;
    private ITimeProvider _time;
    private readonly Dictionary<long, PromiseTask> _taskDic;
    private readonly BetterIndexedPriorityQueue<PromiseTask> _taskQueue;
    private Status _status;
#nullable restore

    public TimerQueue(int initCapacity = 11) {
        _taskDic = new Dictionary<long, PromiseTask>(initCapacity);
        _taskQueue = new BetterIndexedPriorityQueue<PromiseTask>(
            TaskComparer.Inst, TaskIndexHelper.GetInst(0), initCapacity);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="eventLoop">所属的时间循环/线程</param>
    /// <param name="time">时间信息</param>
    /// <param name="queueId">队列ID</param>
    /// <param name="initCapacity">初始空间</param>
    public TimerQueue(IEventLoop? eventLoop, ITimeProvider time, int queueId, int initCapacity = 11) {
        this._eventLoop = eventLoop;
        this._time = time;
        this._taskDic = new Dictionary<long, PromiseTask>(initCapacity);
        this._taskQueue = new BetterIndexedPriorityQueue<PromiseTask>(
            TaskComparer.Inst, TaskIndexHelper.GetInst(queueId), initCapacity);
    }

    /// <summary>
    /// 绑定的事件循环（重用时覆盖）
    /// </summary>
    public IEventLoop? EventLoop {
        get => _eventLoop;
        set => _eventLoop = value;
    }

    /// <summary>
    /// 绑定的时间轴（重用时覆盖）
    /// </summary>
    public ITimeProvider Time {
        get => _time;
        set => _time = value ?? throw new ArgumentNullException(nameof(value));
    }

    #region 生命周期

    public bool IsRunning => _status == Status.Running;
    public bool IsShuttingDown => _status >= Status.ShuttingDown;
    public bool IsShutdown => _status >= Status.Shutdown;

    /// <summary>
    /// 启动定时器队列（可选，添加定时器时自动启动）
    /// </summary>
    public void Start() {
        if (_status == Status.Unstarted) {
            _status = Status.Running;
        }
    }

    /// <summary>
    /// 检查定时器
    /// </summary>
    public void Update() {
        double time = _time.Time;
        int frameCount = _time.FrameCount;
        while (_taskQueue.TryPeekHead(out PromiseTask task)) {
            if (time < task.triggerTime || frameCount < task.gatingFrame) {
                break;
            }
            _taskQueue.Dequeue();

            bool enqueued = false;
            if (task.Trigger(time)) {
                if (IsShuttingDown) {
                    task.Cancel();
                } else {
                    _taskQueue.Enqueue(task);
                    enqueued = true;
                }
            }
            if (!enqueued) {
                _taskDic.Remove(task.id);
                PromiseTask.Release(task);
            }
        }
    }

    /// <summary>
    /// 停止定时器
    /// </summary>
    /// <param name="quietly">是否静默关闭，不使运行中的任务进入取消状态</param>
    public void Stop(bool quietly = false) {
        if (IsShuttingDown) {
            return;
        }
        _status = Status.ShuttingDown;
        while (_taskQueue.TryDequeue(out PromiseTask task)) {
            if (!quietly) {
                task.Cancel();
            }
            _taskDic.Remove(task.id);
            PromiseTask.Release(task);
        }
        // 字典中包含挂起的Timer
        if (_taskDic.Count > 0) {
            foreach (PromiseTask task in _taskDic.Values.ToArray()) {
                if (!quietly) {
                    task.Cancel();
                }
                _taskDic.Remove(task.id);
                PromiseTask.Release(task);
            }
        }
        // 若还有残留则直接丢弃
        _status = Status.Shutdown;
        _taskQueue.Clear();
        _taskDic.Clear();
    }

    /// <summary>
    /// 重置对象
    /// </summary>
    public void Reset() {
        _status = Status.Unstarted;
        _eventLoop = null;
        _time = null;
        _taskDic.Clear();
        _taskQueue.Clear();
    }

    #endregion

    #region 交互接口

    /// <summary>
    /// 设置任务的调度选项
    /// </summary>
    public bool SetOptions(long timerId, int options) {
        if (_taskDic.TryGetValue(timerId, out PromiseTask task)) {
            task.options = options;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 设置Timer的下次执行延迟
    /// </summary>
    public bool SetNextDelay(long timerId, double nextDelay) {
        if (_taskDic.TryGetValue(timerId, out PromiseTask task)) {
            task.triggerTime = GetTriggerTime(nextDelay);
            _taskQueue.PriorityChanged(task);
            return true;
        }
        return false;
    }

    /// <summary>
    /// 设置Timer的执行周期
    /// </summary>
    public void SetPeriod(long timerId, double period) {
        if (_taskDic.TryGetValue(timerId, out PromiseTask task)) {
            task.period = CheckPeriod(period);
        }
    }

    /// <summary>
    /// 暂停Timer
    /// </summary>
    /// <param name="timerId"></param>
    public void Pause(long timerId) {
        if (!_taskDic.TryGetValue(timerId, out PromiseTask task)) {
            return;
        }
        if (!task.IsSuspended) {
            _taskQueue.Remove(task); // 当前可能在队列，也可能不在队列
            task.IsSuspended = true;
            task.nextDelay = Math.Max(0, task.triggerTime - _time.Time);
        }
    }

    /// <summary>
    /// 恢复Timer执行
    /// </summary>
    /// <param name="timerId"></param>
    /// <param name="nextDelay"></param>
    public void Resume(long timerId, double? nextDelay = null) {
        if (!_taskDic.TryGetValue(timerId, out PromiseTask task)) {
            return;
        }
        if (task.IsSuspended) {
            Debug.Assert(!_taskQueue.Contains(task)); // 不会在触发中调用Resume
            task.IsSuspended = false;
            task.triggerTime = GetTriggerTime(nextDelay ?? task.nextDelay);
            _taskQueue.Add(task);
        }
    }

    /// <summary>
    /// 取消定时器
    /// </summary>
    /// <param name="timerId">任务id</param>
    /// <returns>任务存在且取消成功时返回true</returns>
    public void Cancel(long timerId) {
        if (!_taskDic.TryGetValue(timerId, out PromiseTask task)) {
            return;
        }
        task.Cancel();
        // Task可能正在执行，则由Update函数回收
        if (_taskQueue.Remove(task)) {
            _taskDic.Remove(timerId);
            PromiseTask.Release(task);
        }
    }

    #endregion

    #region 调度接口

    public ValueFuture<T> Schedule<T>(in TaskBuilder<T> builder) {
        ThrowIfDisposed();
        ValuePromise<T> promise = ValuePromise<T>.Acquire(out int rid, _eventLoop);
        PromiseTask asyncTask = PromiseTask.Acquire();
        asyncTask.id = PromiseTask.NextId();
        asyncTask.TaskType = builder.Type;
        asyncTask.task = builder.Task;
        asyncTask.state = builder.State;
        asyncTask.cancelToken = builder.CancelToken;
        asyncTask.options = builder.Options;
        asyncTask.promise = promise;
        asyncTask.promiseRid = rid;
        // func装箱
        if (builder.Type == TYPE_FUNC) {
            asyncTask.invoker = PromiseTask.FuncInvoker<T>.invoker1;
        } else if (builder.Type == TYPE_FUNC_STATE) {
            asyncTask.invoker = PromiseTask.FuncInvoker<T>.invoker2;
        }
        // 触发时间
        asyncTask.ScheduleType = builder.ScheduleType;
        asyncTask.triggerTime = GetTriggerTime(builder.InitialDelay);
        if (builder.IsPeriodic) {
            asyncTask.period = CheckPeriod(builder.Period);
        }
        // 帧延迟需求
        int delayFrame = builder.HasExtraDelayFrame ? builder.ExtraDelayFrame : 0;
        asyncTask.gatingFrame = GetGatingFrame(delayFrame);
        // 超时信息
        if (builder.HasTimeout) {
            asyncTask.HasDeadline = true;
            asyncTask.deadline = GetTriggerTime(builder.Timeout);
        }
        if (builder.HasCountLimit) {
            asyncTask.HasCountdown = true;
            asyncTask.countdown = builder.CountLimit;
        }
        long taskId = asyncTask.id;
        Schedule(asyncTask); // 可能被回收
        return promise.Future.WithTaskId(taskId);
    }

    public ValueFuture ScheduleAction(Action action, double delay, CancellationToken cancelToken = default) {
        ThrowIfDisposed();
        ValuePromise<int> promise = ValuePromise<int>.Acquire(out int rid, _eventLoop);
        PromiseTask asyncTask = PromiseTask.Acquire();
        asyncTask.id = PromiseTask.NextId();
        asyncTask.TaskType = TYPE_ACTION;
        asyncTask.task = action;
        asyncTask.state = cancelToken;
        asyncTask.promise = promise;
        asyncTask.promiseRid = rid;
        asyncTask.triggerTime = GetTriggerTime(delay);
        asyncTask.gatingFrame = GetGatingFrame();

        long taskId = asyncTask.id;
        Schedule(asyncTask); // 可能被回收
        return promise.VoidFuture.WithTaskId(taskId);
    }

    public ValueFuture ScheduleAction(Action<object> action, object state, double delay, CancellationToken cancelToken = default) {
        ThrowIfDisposed();
        ValuePromise<int> promise = ValuePromise<int>.Acquire(out int rid, _eventLoop);
        PromiseTask asyncTask = PromiseTask.Acquire();
        asyncTask.id = PromiseTask.NextId();
        asyncTask.TaskType = TYPE_ACTION_STATE;
        asyncTask.task = action;
        asyncTask.state = state;
        asyncTask.cancelToken = cancelToken;
        asyncTask.promise = promise;
        asyncTask.promiseRid = rid;
        asyncTask.triggerTime = GetTriggerTime(delay);
        asyncTask.gatingFrame = GetGatingFrame();

        long taskId = asyncTask.id;
        Schedule(asyncTask); // 可能被回收
        return promise.VoidFuture.WithTaskId(taskId);
    }

    public ValueFuture<T> ScheduleFunc<T>(Func<T> action, double delay, CancellationToken cancelToken = default) {
        ThrowIfDisposed();
        ValuePromise<T> promise = ValuePromise<T>.Acquire(out int rid, _eventLoop);
        PromiseTask asyncTask = PromiseTask.Acquire();
        asyncTask.id = PromiseTask.NextId();
        asyncTask.TaskType = TYPE_FUNC;
        asyncTask.invoker = PromiseTask.FuncInvoker<T>.invoker1; // 装箱结果
        asyncTask.task = action;
        asyncTask.state = cancelToken;
        asyncTask.promise = promise;
        asyncTask.promiseRid = rid;
        asyncTask.triggerTime = GetTriggerTime(delay);
        asyncTask.gatingFrame = GetGatingFrame();

        long taskId = asyncTask.id;
        Schedule(asyncTask); // 可能被回收
        return promise.Future.WithTaskId(taskId);
    }

    public ValueFuture<T> ScheduleFunc<T>(Func<object, T> action, object state, double delay, CancellationToken cancelToken = default) {
        ThrowIfDisposed();
        ValuePromise<T> promise = ValuePromise<T>.Acquire(out int rid, _eventLoop);
        PromiseTask asyncTask = PromiseTask.Acquire();
        asyncTask.id = PromiseTask.NextId();
        asyncTask.TaskType = TYPE_FUNC_STATE;
        asyncTask.invoker = PromiseTask.FuncInvoker<T>.invoker2; // 装箱结果
        asyncTask.task = action;
        asyncTask.state = state;
        asyncTask.cancelToken = cancelToken;
        asyncTask.promise = promise;
        asyncTask.promiseRid = rid;
        asyncTask.triggerTime = GetTriggerTime(delay);
        asyncTask.gatingFrame = GetGatingFrame();

        long taskId = asyncTask.id;
        Schedule(asyncTask); // 可能被回收
        return promise.Future.WithTaskId(taskId);
    }

    public ValueFuture ScheduleWithFixedDelay(Action action, double delay, double period, CancellationToken cancelToken = default) {
        ThrowIfDisposed();
        ValuePromise<int> promise = ValuePromise<int>.Acquire(out int rid, _eventLoop);
        PromiseTask asyncTask = PromiseTask.Acquire();
        asyncTask.id = PromiseTask.NextId();
        asyncTask.TaskType = TYPE_ACTION;
        asyncTask.task = action;
        asyncTask.state = cancelToken;
        asyncTask.promise = promise;
        asyncTask.promiseRid = rid;
        asyncTask.triggerTime = GetTriggerTime(delay);
        asyncTask.gatingFrame = GetGatingFrame();
        asyncTask.period = CheckPeriod(period);
        asyncTask.ScheduleType = SCHEDULE_FIXED_DELAY;
        //
        long taskId = asyncTask.id;
        Schedule(asyncTask); // 可能被回收
        return promise.VoidFuture.WithTaskId(taskId);
    }

    public ValueFuture ScheduleWithFixedDelay(Action<object> action, object state, double delay, double period, CancellationToken cancelToken = default) {
        ThrowIfDisposed();
        ValuePromise<int> promise = ValuePromise<int>.Acquire(out int rid, _eventLoop);
        PromiseTask asyncTask = PromiseTask.Acquire();
        asyncTask.id = PromiseTask.NextId();
        asyncTask.TaskType = TYPE_ACTION_STATE;
        asyncTask.task = action;
        asyncTask.state = state;
        asyncTask.cancelToken = cancelToken;
        asyncTask.promise = promise;
        asyncTask.promiseRid = rid;
        asyncTask.triggerTime = GetTriggerTime(delay);
        asyncTask.gatingFrame = GetGatingFrame();
        asyncTask.period = CheckPeriod(period);
        asyncTask.ScheduleType = SCHEDULE_FIXED_DELAY;
        //
        long taskId = asyncTask.id;
        Schedule(asyncTask); // 可能被回收
        return promise.VoidFuture.WithTaskId(taskId);
    }

    public ValueFuture ScheduleAtFixedRate(Action action, double delay, double period, CancellationToken cancelToken = default) {
        ThrowIfDisposed();
        ValuePromise<int> promise = ValuePromise<int>.Acquire(out int rid, _eventLoop);
        PromiseTask asyncTask = PromiseTask.Acquire();
        asyncTask.id = PromiseTask.NextId();
        asyncTask.TaskType = TYPE_ACTION;
        asyncTask.task = action;
        asyncTask.state = cancelToken;
        asyncTask.promise = promise;
        asyncTask.promiseRid = rid;
        asyncTask.triggerTime = GetTriggerTime(delay);
        asyncTask.gatingFrame = GetGatingFrame();
        asyncTask.period = CheckPeriod(period);
        asyncTask.ScheduleType = SCHEDULE_FIXED_RATE;
        //
        long taskId = asyncTask.id;
        Schedule(asyncTask); // 可能被回收
        return promise.VoidFuture.WithTaskId(taskId);
    }

    public ValueFuture ScheduleAtFixedRate(Action<object> action, object state, double delay, double period, CancellationToken cancelToken = default) {
        ThrowIfDisposed();
        ValuePromise<int> promise = ValuePromise<int>.Acquire(out int rid, _eventLoop);
        PromiseTask asyncTask = PromiseTask.Acquire();
        asyncTask.id = PromiseTask.NextId();
        asyncTask.TaskType = TYPE_ACTION_STATE;
        asyncTask.task = action;
        asyncTask.state = state;
        asyncTask.cancelToken = cancelToken;
        asyncTask.promise = promise;
        asyncTask.promiseRid = rid;
        asyncTask.triggerTime = GetTriggerTime(delay);
        asyncTask.gatingFrame = GetGatingFrame();
        asyncTask.period = CheckPeriod(period);
        asyncTask.ScheduleType = SCHEDULE_FIXED_RATE;
        //
        long taskId = asyncTask.id;
        Schedule(asyncTask); // 可能被回收
        return promise.VoidFuture.WithTaskId(taskId);
    }

    private void ThrowIfDisposed() {
        if (_time == null) {
            throw new InvalidOperationException("TimerQueue is disposed");
        }
    }

    /// <summary>
    /// 将任务添加到调度队列
    /// 注：立即使Task进入被取消状态可能违背正常的时序期望，await语法安全。
    /// </summary>
    internal void Schedule(PromiseTask task) {
        if (IsShuttingDown) {
            task.Cancel();
            PromiseTask.Release(task);
            return;
        }
        if (_status == Status.Unstarted) {
            _status = Status.Running;
        }
        _taskQueue.Add(task);
        _taskDic.Add(task.id, task);
        // TODO 监听取消令牌？
        if ((task.options & TaskOptions.LISTEN_CANCEL_TOKEN) != 0) {

        }
    }

    /// <summary>
    /// 获取任务的首次触发回时间
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal double GetTriggerTime(double delay) => _time.Time + Math.Max(0, delay);

    /// <summary>
    /// 获取任务的首次触发帧
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int GetGatingFrame(int extraDelay = 0) => _time.FrameCount + Math.Max(0, extraDelay);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double CheckPeriod(double period) {
        return period > 0 ? period : throw new ArgumentException("period must be greater than 0");
    }

    private enum Status
    {
        Unstarted = 0,
        Running = 1,
        ShuttingDown = 2,
        Shutdown = 3
    }

    private class TaskComparer : IComparer<PromiseTask>
    {
        public static readonly TaskComparer Inst = new TaskComparer();

        public int Compare(PromiseTask? x, PromiseTask? y) {
            // ReSharper disable PossibleNullReferenceException
            // 先按照触发帧排序，可避免调度时修正TriggerTime
            int r = x.gatingFrame.CompareTo(y.gatingFrame);
            if (r != 0) return r;

            // 再按照触发时间排序
            r = x.triggerTime.CompareTo(y.triggerTime);
            if (r != 0) return r;

            // 尚未触发的新任务优先
            r = x.IsTriggered.CompareTo(y.IsTriggered);
            if (r != 0) return r;

            return x.id.CompareTo(y.id);
        }
    }

    #endregion
}
}