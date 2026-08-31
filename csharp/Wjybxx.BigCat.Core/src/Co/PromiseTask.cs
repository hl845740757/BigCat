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
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using Wjybxx.Commons;
using Wjybxx.Commons.Concurrent;
using Wjybxx.Commons.Pool;
using static Wjybxx.BigCat.Co.TaskBuilder;

namespace Wjybxx.BigCat.Co
{
/// <summary>
/// 异步任务(TimerTask)
///
/// 注：
/// 1.如果任务包含结果，会产生装箱；通常问题不大，因为包含结果的延时任务占比很小。
/// 2.该对象不可返回给用户！否则可能导致内存泄漏，或池化复用错误。
/// 3.Task由调度器触发回收，在确定从所有队列中删除后才能执行回收。
/// 4.由于存在多处修改<see cref="ValuePromise{T}"/>状态的情况，因此需要校验rid -- 但都在EventLoop线程更新Promise。
/// 5.游戏业务通常不需要注册CTS监听器，延迟响应取消信号通常不影响正确性 -- 延时任务总是不保证即时性。
/// </summary>
[StructLayout(LayoutKind.Auto)]
internal sealed class PromiseTask
{
    #region 常量

    private const int MASK_TASK_TYPE = 0x0F;
    private const int MASK_SCHEDULE_TYPE = 0xF0;

    private const int OFFSET_TASK_TYPE = 0;
    private const int OFFSET_SCHEDULE_TYPE = 4;

    private const int MASK_TRIGGERED = 1 << 18; // 是否已完成首次触发
    private const int MASK_HAS_DEADLINE = 1 << 19; // 是否包含截止时间
    private const int MASK_HAS_COUNTDOWN = 1 << 20; // 延时任务有次数限制
    private const int MASK_STARTED = 1 << 21; // 任务是否已启动
    private const int MASK_STOPPED = 1 << 22; // 任务是否已停止
    private const int MASK_SUSPENDED = 1 << 23; // 任务是否挂起状态

    #endregion

#nullable disable
    /// <summary>
    /// 任务id
    /// </summary>
    public long id = -1;
    /// <summary>
    /// 用户委托的封装类
    /// (如果是绑定协程的异步任务，该字段存储协程对象的引用)
    /// </summary>
    public object invoker;
    /// <summary>
    /// 用户的委托
    /// </summary>
    public object task;
    /// <summary>
    /// 任务的上下文
    /// </summary>
    public object state;
    /// <summary>
    /// 任务关联的取消令牌
    /// </summary>
    public CancellationToken cancelToken;
    /// <summary>
    /// 任务选项
    /// </summary>
    public int options;

    /// <summary>
    /// 触发时间（时间或帧数）
    /// </summary>
    public double triggerTime;
    /// <summary>
    /// 触发周期（时间或帧数）
    /// </summary>
    public double period;
    /// <summary>
    /// 触发帧号限制
    /// 注：在基于时间触发的任务中，用于强制Sleep0下一帧触发。
    /// </summary>
    public int gatingFrame;
    /// <summary>
    /// 剩余触发次数
    /// </summary>
    public int countdown;
    /// <summary>
    /// 截止时间
    /// </summary>
    public double deadline;
    /// <summary>
    /// 下次执行延迟(暂停时记录)
    /// </summary>
    public double nextDelay;

    /// <summary>
    /// 关联的Promise
    ///
    /// 1.泛型参数为int或object - 写入结果时会装箱，但有利于对象复用。
    /// 2.协程的Await任务存在多处赋值，但约定都在EventLoop线程。
    /// </summary>
    public IValuePromise promise;
    /// <summary>
    /// 关联promise的rid
    /// </summary>
    public int promiseRid;

    /// <summary>
    /// 辅助控制标识
    /// </summary>
    private int ctl;
    /// <summary>
    /// 当前所属的队列
    /// </summary>
    internal int queueId = -1;
    /// <summary>
    /// 在队列中的索引
    /// </summary>
    internal int qIndex = -1;
#nullable restore

    public PromiseTask() {
    }

    /// <summary>
    /// 重置对象，用于池化
    /// </summary>
    public void Reset() {
        id = -1;
        invoker = null;
        task = null;
        state = null;
        cancelToken = default;
        options = 0;
        promise = null;
        promiseRid = -1;

        triggerTime = 0;
        period = 0;
        gatingFrame = 0;
        countdown = 0;
        deadline = 0;
        nextDelay = 0;

        ctl = 0;
        queueId = -1;
        qIndex = -1;
    }

    #region 调度

    /// <summary>
    /// 外部确定性触发
    /// </summary>
    /// <param name="tickTime">当前时间戳</param>
    /// <returns>是否还需要压入队列</returns>
    /// <returns></returns>
    public bool Trigger(double tickTime) {
        // 标记为已触发
        bool firstTrigger = (ctl & MASK_TRIGGERED) == 0;
        if (firstTrigger) {
            ctl |= MASK_TRIGGERED;
        }
        // 存在多处更新Promise的逻辑，因此先检测Promise的有效性 -- Promise可能会被提前回收
        // IValuePromise promise = this.promise;
        if (promise.IsRecycledOrCompleted(promiseRid)) {
            return false;
        }
        // 检测取消
        if (cancelToken.IsCancellationRequested) {
            TrySetCancelled(cancelToken);
            return false;
        }
        // 一次性任务
        int scheduleType = ScheduleType;
        if (scheduleType == SCHEDULE_ONCE) {
            int type = TaskType;
            if (type == TYPE_CANCELLER) {
                TrySetCancelled();
                return false;
            }
            try {
                object result = RunTask();
                TrySetResult(result);
            }
            catch (Exception ex) {
                TrySetException(ex);
            }
            return false;
        }
        // 周期性任务 - Computing状态更新非必要
        try {
            RunTask();
        }
        catch (Exception ex) {
            ThreadUtil.RecoveryInterrupted(ex);
            if (!CanCaughtException(ex)) {
                TrySetException(ex);
                return false;
            }
            FutureLogger.LogCause(ex, "periodic task caught exception");
        }
        // 再次检查Promise的有效性
        if (promise.IsRecycledOrCompleted(promiseRid)) {
            return false;
        }
        // 任务执行后检测取消
        if (cancelToken.IsCancellationRequested) {
            TrySetCancelled(cancelToken);
            return false;
        }
        // 未被取消的情况下检测超时
        if (HasDeadline && deadline <= tickTime) {
            TrySetCancelled(CancellationToken.None);
            return false;
        }
        // 检测次数限制
        if (HasCountdown && (--countdown < 1)) {
            TrySetCancelled(CancellationToken.None);
            return false;
        }
        SetNextRunTime(tickTime, scheduleType);
        return true;
    }

    private object? RunTask() {
        int type = (ctl & MASK_TASK_TYPE) >> OFFSET_TASK_TYPE;
        switch (type) {
            case TYPE_EMPTY: {
                return null;
            }
            case TYPE_ACTION: {
                Action task = (Action)this.task;
                task();
                return null;
            }
            case TYPE_ACTION_STATE: {
                Action<object> task = (Action<object>)this.task;
                task(state);
                return null;
            }
            case TYPE_FUNC: {
                if (this.invoker is Func<object, object> invoker) {
                    return invoker(task);
                }
                Delegate d = (Delegate)this.invoker;
                return d.DynamicInvoke(task);
            }
            case TYPE_FUNC_STATE: {
                if (this.invoker is Func<object, object, object> invoker) {
                    return invoker(task, state);
                }
                Delegate d = (Delegate)this.invoker;
                return d.DynamicInvoke(task, state);
            }
            default: {
                throw new AssertionError("type: " + type);
            }
        }
    }

    private bool CanCaughtException(Exception _) {
        return ScheduleType != SCHEDULE_ONCE
               && TaskOptions.IsEnabled(options, TaskOptions.CAUGHT_EXCEPTION);
    }

    private void SetNextRunTime(double tickTime, int scheduleType) {
        if (scheduleType == ScheduledTaskBuilder.SCHEDULE_FIXED_RATE) {
            triggerTime = triggerTime + period; // 逻辑时间
        } else {
            triggerTime = tickTime + period; // 真实时间
        }
    }

    #endregion

    #region props

    /// <summary>
    /// 任务类型
    /// </summary>
    public int TaskType {
        get => (ctl & MASK_TASK_TYPE) >> OFFSET_TASK_TYPE;
        set => ctl = BitFlags.SetField(ctl, MASK_TASK_TYPE, OFFSET_TASK_TYPE, value);
    }
    /// <summary>
    /// 调度类型
    /// </summary>
    public int ScheduleType {
        get => (ctl & MASK_SCHEDULE_TYPE) >> OFFSET_SCHEDULE_TYPE;
        set => ctl = BitFlags.SetField(ctl, MASK_SCHEDULE_TYPE, OFFSET_SCHEDULE_TYPE, value);
    }

    /// <summary>
    /// 是否已完成首次触发
    /// </summary>
    public bool IsTriggered => (ctl & MASK_TRIGGERED) != 0;
    /// <summary>
    /// 是否是周期性任务
    /// </summary>
    public bool IsPeriodic => (ctl & MASK_SCHEDULE_TYPE) != 0;

    /// <summary>
    /// 是否处于挂起状态
    /// </summary>
    public bool IsSuspended {
        get => (ctl & MASK_SUSPENDED) != 0;
        set => SetCtlBit(MASK_SUSPENDED, value);
    }
    /// <summary>
    /// 是否包含执行次数限制
    /// </summary>
    public bool HasCountdown {
        get => (ctl & MASK_HAS_COUNTDOWN) != 0;
        set => SetCtlBit(MASK_HAS_COUNTDOWN, value);
    }
    /// <summary>
    /// 是否包含执行时间限制
    /// </summary>
    public bool HasDeadline {
        get => (ctl & MASK_HAS_DEADLINE) != 0;
        set => SetCtlBit(MASK_HAS_DEADLINE, value);
    }

    /// <summary>
    /// 是否启用了指定选项
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsEnabled(int optionMask) {
        return (options & optionMask) == optionMask;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetCtlBit(int mask, bool enable) {
        if (enable) {
            ctl |= mask;
        } else {
            ctl &= ~mask;
        }
    }

    #endregion

    #region internal

    /// <summary>
    /// 取消执行
    /// 注：可能是检测到取消信号，也可能是其它原因，调度器主动停止任务。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Cancel(CancellationToken cts = default) {
        TrySetCancelled(cts);
    }

    /// <summary>
    /// 绑定协程
    /// </summary>
    public void BindCoroutine(Coroutine coroutine) {
        Debug.Assert(this.invoker == null);
        this.invoker = coroutine;
        coroutine.asyncTask = this;
    }

    /// <summary>
    /// 解绑协程
    /// 注：协程对象在任务完成后可能会创建新的异步任务，因此在唤醒协程（赋值结果）之前需要先解除绑定。
    /// </summary>
    public void UnbindCoroutine() {
        if (this.invoker is Coroutine coroutine && coroutine.asyncTask == this) {
            coroutine.asyncTask = null;
            this.invoker = null;
        }
    }

    #endregion

    #region Set-Result

    /// <summary>
    /// 尝试将Promise置为完成状态
    /// 注：用于设置<see cref="Func{TResult}"/>的执行结果。
    /// </summary>
    public bool TrySetResult(object? value) {
        UnbindCoroutine();
        // ValuePromise不支持多次设置结果 -- 理论上都在事件循环线程设置Promise结果，先检查后执行不应该出现异常
        return !promise.IsRecycled(promiseRid) && promise.TrySetResult(promiseRid, value);
    }

    public bool TrySetResult<T>(T? value) {
        UnbindCoroutine();
        return !promise.IsRecycled(promiseRid) && promise.TrySetResult(promiseRid, value);
    }

    public bool TrySetException(Exception ex) {
        UnbindCoroutine();
        return !promise.IsRecycled(promiseRid) && promise.TrySetException(promiseRid, ex);
    }

    public bool TrySetException(ExceptionDispatchInfo dispatchInfo) {
        UnbindCoroutine();
        return !promise.IsRecycled(promiseRid) && promise.TrySetException(promiseRid, dispatchInfo);
    }

    public bool TrySetCancelled(CancellationToken cts = default) {
        UnbindCoroutine();
        return !promise.IsRecycled(promiseRid) && promise.TrySetCancelled(promiseRid, cts);
    }

    #endregion

    #region 池化

    /// <summary>
    /// Task对象池 TODO 抽取环境变量
    /// </summary>
    private static readonly ConcurrentObjectPool<PromiseTask> taskPool = new(
        () => new PromiseTask(), task => task.Reset(), 2048);

    /// <summary>
    /// 全局ID生成器
    /// </summary>
    private static PaddedInt64 _idGenerator = new PaddedInt64(0);

    /// <summary>
    /// 申请Task对象
    /// </summary>
    public static PromiseTask Acquire() {
        return taskPool.Acquire();
    }

    /// <summary>
    /// 将Task归还到对象池
    /// </summary>
    public static void Release(PromiseTask task) {
        taskPool.Release(task);
    }

    /// <summary>
    /// 全局ID分配
    /// </summary>
    /// <returns></returns>
    public static long NextId() => _idGenerator.IncrementAndGet();

    #endregion

    /// <summary>
    /// 用于对需要返回结果的延时任务<see cref="Func{TResult}"/>进行装箱，避免动态反射调用。
    /// 由于包含结果的延时任务占比极小，因此装箱更有利于池化。
    /// </summary>
    public static class FuncInvoker<R>
    {
        // ReSharper disable InconsistentNaming
        public static readonly Func<object, object> invoker1 = (_func) => {
            Func<R> func = (Func<R>)_func;
            return func();
        };
        public static readonly Func<object, object, object> invoker2 = (_func, arg) => {
            Func<object, R> func = (Func<object, R>)_func;
            return func(arg);
        };
    }
}
}