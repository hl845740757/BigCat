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
using Wjybxx.Commons;
using Wjybxx.Commons.Concurrent;
using static Wjybxx.BigCat.Co.TaskBuilder;

namespace Wjybxx.BigCat.Co
{
/// <summary>
/// 小型异步任务
///
/// 注：
/// 1.如果任务包含结果，会产生装箱；通常问题不大，因为包含结果的延时任务占比很小。
/// 2.该对象不可返回给用户！否则可能导致内存泄漏，复用错误。
/// 3.Task不可主动调用回收，应当由调度器触发回收。
/// 4.由于存在多处修改<see cref="ValuePromise{T}"/>状态的情况，因此需要校验rid -- 但都在EventLoop线程更新Promise。
/// </summary>
[StructLayout(LayoutKind.Auto)]
internal sealed class PromiseTask
{
    #region 常量

    private const int MASK_TASK_TYPE = 0x0F;
    private const int MASK_SCHEDULE_TYPE = 0xF0;

    private const int OFFSET_TASK_TYPE = 0;
    private const int OFFSET_SCHEDULE_TYPE = 4;

    private const int MASK_BASED_ON_UNSCALED_TIME = 1 << 16; // 基于非缩放时间触发
    private const int MASK_BASED_ON_FRAME_COUNT = 1 << 17; // 基于帧数触发
    private const int MASK_TRIGGERED = 1 << 18; // 是否已完成首次触发
    private const int MASK_HAS_DEADLINE = 1 << 19; // 是否包含截止时间
    private const int MASK_HAS_COUNTDOWN = 1 << 20; // 延时任务有次数限制
    private const int MASK_STARTED = 1 << 21; // 任务是否已启动
    private const int MASK_COROUTINE_TASK = 1 << 22; // 协程绑定任务

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
    public object ctx;
    /// <summary>
    /// 任务选项
    /// </summary>
    public int options;

    /// <summary>
    /// 触发时间 or 触发帧数
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
    /// 截止时间
    /// </summary>
    public double deadline;
    /// <summary>
    /// 剩余触发次数
    /// </summary>
    public int countdown;

    /// <summary>
    /// 关联的Promise
    ///
    /// 1.泛型参数为int或object - 写入结果时会装箱，但有利于对象复用。
    /// 2.Await任务存在多处赋值，但约定都在EventLoop线程。
    /// </summary>
    public IValuePromise promise;
    /// <summary>
    /// 关联promise的rid
    /// </summary>
    public int promiseRid;
    /// <summary>
    /// 接收用户取消信号的句柄
    /// </summary>
    internal Registration cancelRegistration;

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
        ctx = null;
        options = 0;
        promise = null;
        promiseRid = -1;

        triggerTime = 0;
        period = 0;
        gatingFrame = 0;
        deadline = 0;
        countdown = 0;
        cancelRegistration = default;

        ctl = 0;
        queueId = -1;
        qIndex = -1;
    }

    /// <summary>
    /// 获取上下文中的取消令牌
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ICancelToken GetCancelToken() {
        return ExecutorUtil.GetCancelToken(ctx, options);
    }

    /// <summary>
    /// 取消执行
    ///
    /// 注：可能是检测到取消信号，也可能是其它原因，调动器主动停止任务。
    /// </summary>
    /// <param name="cancelCode"></param>
    public void Cancel(int cancelCode) {
        TrySetCancelled(cancelCode);
    }

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
        IValuePromise promise = this.promise;
        if (IsRecycledOrCompleted(promise, promiseRid)) {
            return false;
        }
        // 检测取消
        ICancelToken cancelToken = GetCancelToken();
        if (cancelToken.IsRequested) {
            TrySetCancelled(cancelToken.CancelCode);
            return false;
        }
        // 一次性任务
        int scheduleType = ScheduleType;
        if (scheduleType == SCHEDULE_ONCE) {
            if (TaskType == TYPE_AWAIT) {
                TrySetCancelled(CancelCodes.REASON_TIMEOUT);
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
        // 任务执行后检测取消
        if (cancelToken.IsRequested || IsRecycledOrCompleted(promise, promiseRid)) {
            TrySetCancelled(cancelToken, CancelCodes.REASON_DEFAULT);
            return false;
        }
        // 未被取消的情况下检测超时
        if (HasDeadline && deadline <= tickTime) {
            TrySetException(StacklessCancellationException.Timeout);
            return false;
        }
        // 检测次数限制
        if (HasCountdown && (--countdown < 1)) {
            TrySetException(StacklessCancellationException.CountLimit);
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
            case TYPE_ACTION_CTX: {
                Action<object> task = (Action<object>)this.task;
                task(ctx);
                return null;
            }
            case TYPE_FUNC: {
                Func<object, object> invoker = (Func<object, object>)this.invoker;
                return invoker(task);
            }
            case TYPE_FUNC_CTX: {
                Func<object, object, object> invoker = (Func<object, object, object>)this.invoker;
                return invoker(task, ctx);
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
        double maxDelay = HasDeadline ? (deadline - tickTime) : (365 * DatetimeUtil.SecondsPerDay);
        if (scheduleType == SCHEDULE_FIXED_RATE) {
            triggerTime = triggerTime + Math.Min(period, maxDelay); // 逻辑时间
        } else {
            triggerTime = tickTime + Math.Min(period, maxDelay); // 真实时间
        }
    }

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
    /// 是否是协程关联的任务(双向绑定的任务)
    /// </summary>
    public bool IsCoroutineTask {
        get => (ctl & MASK_COROUTINE_TASK) != 0;
        set => SetCtlBit(MASK_COROUTINE_TASK, value);
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
    /// 是否包含执行时间限制
    /// </summary>
    public bool HasDeadline {
        get => (ctl & MASK_HAS_DEADLINE) != 0;
        set => SetCtlBit(MASK_HAS_DEADLINE, value);
    }
    /// <summary>
    /// 是否包含执行次数限制
    /// </summary>
    public bool HasCountdown {
        get => (ctl & MASK_HAS_COUNTDOWN) != 0;
        set => SetCtlBit(MASK_HAS_COUNTDOWN, value);
    }

    /// <summary>
    /// 是否启用了指定选项
    /// </summary>
    public bool IsEnabled(int optionMask) {
        return (options & optionMask) != 0;
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

    #region Set-Result

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsRecycledOrCompleted(IValuePromise promise, int rid) {
        return promise.IsRecycled(rid) || promise.GetStatus(rid).IsCompleted();
    }

    /// <summary>
    /// 尝试将Promise置为完成状态
    ///
    /// 注：用于设置<see cref="Func{TResult}"/>的执行结果。
    /// </summary>
    /// <param name="value"></param>
    public bool TrySetResult(object? value) {
        Unbind();
        CloseRegistration();
        // ValuePromise不支持多次设置结果 -- 理论上都在事件循环线程设置Promise结果，先检查后执行不应该出现异常
        return !promise.IsRecycled(promiseRid) && promise.TrySetResult(promiseRid, value);
    }

    public bool TrySetResult<T>(T? value) {
        Unbind();
        CloseRegistration();
        return !promise.IsRecycled(promiseRid) && promise.TrySetResult(promiseRid, value);
    }

    public bool TrySetException(Exception ex) {
        Unbind();
        CloseRegistration();
        return !promise.IsRecycled(promiseRid) && promise.TrySetException(promiseRid, ex);
    }

    public bool TrySetException(ExceptionDispatchInfo dispatchInfo) {
        Unbind();
        CloseRegistration();
        return !promise.IsRecycled(promiseRid) && promise.TrySetException(promiseRid, dispatchInfo);
    }

    public bool TrySetCancelled(ICancelToken cancelToken, int def) {
        Unbind();
        CloseRegistration();
        int cancelCode = cancelToken.CancelCode;
        if (cancelCode == 0) cancelCode = def;
        return !promise.IsRecycled(promiseRid) && promise.TrySetCancelled(promiseRid, cancelCode);
    }

    public bool TrySetCancelled(int cancelCode) {
        Unbind();
        CloseRegistration();
        return !promise.IsRecycled(promiseRid) && promise.TrySetCancelled(promiseRid, cancelCode);
    }

    /// <summary>
    /// 协程对象在任务完成后可能会创建新的任务，因此在唤醒协程之前需要先解除绑定。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Unbind() {
        if (IsCoroutineTask && this.invoker is Coroutine coroutine) {
            Debug.Assert(coroutine.asyncTask == this);
            coroutine.asyncTask = null;
            this.invoker = null;
        }
    }

    /// <summary>
    /// 关闭取消令牌的监听
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CloseRegistration() {
        Registration registration = this.cancelRegistration;
        this.cancelRegistration = default;
        registration.Dispose();
    }

    #endregion

    /// <summary>
    /// 用于对需要返回结果的延时任务<see cref="Func{TResult}"/>进行装箱，避免动态反射调用。
    /// </summary>
    internal static class FuncInvoker<R>
    {
        // ReSharper disable InconsistentNaming
        public static readonly Func<object, object> wrapper0 = (_func) => {
            Func<R> func = (Func<R>)_func;
            return func();
        };
        public static readonly Func<object, object, object> wrapper1 = (_func, arg) => {
            Func<object, R> func = (Func<object, R>)_func;
            return func(arg);
        };
    }
}
}