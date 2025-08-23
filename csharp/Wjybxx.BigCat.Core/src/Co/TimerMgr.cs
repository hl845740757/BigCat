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
using System.Runtime.CompilerServices;
using Wjybxx.BigCat.Gameplay;
using Wjybxx.Commons.Concurrent;
using static Wjybxx.BigCat.Co.CoroutineMgr;
using static Wjybxx.BigCat.Co.TaskBuilder;

namespace Wjybxx.BigCat.Co
{
/// <summary>
/// 该实现与<see cref="CoroutineMgr"/>绑定。
/// </summary>
internal sealed class TimerMgr : ITimerMgr
{
    private readonly CoroutineMgr _coroutineMgr;
    private readonly TimingType _timingType;

    public TimerMgr(CoroutineMgr coroutineMgr, TimingType timingType) {
        _coroutineMgr = coroutineMgr;
        _timingType = timingType;
    }

    public long LastTimerId => _coroutineMgr.LastTimerId;

    public ValueFuture<T> Schedule<T>(in TaskBuilder<T> builder) {
        GameLoopPhase phase = builder.SchedulePhase;
        _coroutineMgr.CheckQueue(_timingType, phase);
        //
        ValuePromise<T> promise = ValuePromise<T>.Acquire(out int rid, _coroutineMgr.EventLoop);
        PromiseTask asyncTask = taskPool.Acquire();
        asyncTask.id = NextId();
        asyncTask.TaskType = builder.Type;
        asyncTask.task = builder.Task;
        asyncTask.ctx = builder.Context;
        asyncTask.options = builder.Options;
        asyncTask.promise = promise;
        asyncTask.promiseRid = rid;
        // func装箱
        if (builder.Type == TYPE_FUNC) {
            asyncTask.invoker = PromiseTask.FuncInvoker<T>.wrapper0;
        } else if (builder.Type == TYPE_FUNC_CTX) {
            asyncTask.invoker = PromiseTask.FuncInvoker<T>.wrapper1;
        }
        // 触发时间
        asyncTask.ScheduleType = builder.ScheduleType;
        asyncTask.triggerTime = _coroutineMgr.GetTime(_timingType, phase) + Math.Max(0, builder.InitialDelay);
        if (builder.IsPeriodic) {
            asyncTask.period = CorrectPeriod(builder.Period);
        }
        if (builder.HasExtraDelayFrame && _timingType != TimingType.FrameCount) {
            asyncTask.gatingFrame = _coroutineMgr.GetFrameCount(phase) + builder.ExtraDelayFrame;
        }
        // 超时信息
        if (builder.HasTimeout) {
            asyncTask.HasDeadline = true;
            asyncTask.deadline = _coroutineMgr.GetTime(_timingType, phase) + Math.Max(0, builder.Timeout);
        }
        if (builder.HasCountLimit) {
            asyncTask.HasCountdown = true;
            asyncTask.countdown = builder.CountLimit;
        }
        _coroutineMgr.AddTimer(asyncTask, _timingType, phase);
        return promise.Future;
    }

    public ValueFuture ScheduleAction(Action action, double delay, ICancelToken? cancelToken = null) {
        ValuePromise<int> promise = ValuePromise<int>.Acquire(out int rid, _coroutineMgr.EventLoop);
        PromiseTask asyncTask = taskPool.Acquire();
        asyncTask.id = NextId();
        asyncTask.TaskType = TYPE_ACTION;
        asyncTask.task = action;
        asyncTask.ctx = cancelToken;
        asyncTask.promise = promise;
        asyncTask.promiseRid = rid;
        asyncTask.triggerTime = GetTriggerTime(delay);
        asyncTask.gatingFrame = GetGatingFrame();

        _coroutineMgr.AddTimer(asyncTask, _timingType);
        return promise.VoidFuture;
    }

    public ValueFuture ScheduleAction(Action<object> action, object timerArg, double delay) {
        ValuePromise<int> promise = ValuePromise<int>.Acquire(out int rid, _coroutineMgr.EventLoop);
        PromiseTask asyncTask = taskPool.Acquire();
        asyncTask.id = NextId();
        asyncTask.TaskType = TYPE_ACTION_CTX;
        asyncTask.task = action;
        asyncTask.ctx = timerArg;
        asyncTask.promise = promise;
        asyncTask.promiseRid = rid;
        asyncTask.triggerTime = GetTriggerTime(delay);
        asyncTask.gatingFrame = GetGatingFrame();

        _coroutineMgr.AddTimer(asyncTask, _timingType);
        return promise.VoidFuture;
    }

    public ValueFuture<T> ScheduleFunc<T>(Func<T> action, double delay, ICancelToken? cancelToken = null) {
        ValuePromise<T> promise = ValuePromise<T>.Acquire(out int rid, _coroutineMgr.EventLoop);
        PromiseTask asyncTask = taskPool.Acquire();
        asyncTask.id = NextId();
        asyncTask.TaskType = TYPE_FUNC;
        asyncTask.invoker = PromiseTask.FuncInvoker<T>.wrapper0; // 装箱结果
        asyncTask.task = action;
        asyncTask.ctx = cancelToken;
        asyncTask.promise = promise;
        asyncTask.promiseRid = rid;
        asyncTask.triggerTime = GetTriggerTime(delay);
        asyncTask.gatingFrame = GetGatingFrame();

        _coroutineMgr.AddTimer(asyncTask, _timingType);
        return promise.Future;
    }

    public ValueFuture<T> ScheduleFunc<T>(Func<object, T> action, object timerArg, double delay) {
        ValuePromise<T> promise = ValuePromise<T>.Acquire(out int rid, _coroutineMgr.EventLoop);
        PromiseTask asyncTask = taskPool.Acquire();
        asyncTask.id = NextId();
        asyncTask.TaskType = TYPE_FUNC_CTX;
        asyncTask.invoker = PromiseTask.FuncInvoker<T>.wrapper1; // 装箱结果
        asyncTask.task = action;
        asyncTask.ctx = timerArg;
        asyncTask.promise = promise;
        asyncTask.promiseRid = rid;
        asyncTask.triggerTime = GetTriggerTime(delay);
        asyncTask.gatingFrame = GetGatingFrame();

        _coroutineMgr.AddTimer(asyncTask, _timingType);
        return promise.Future;
    }

    public ValueFuture ScheduleWithFixedDelay(Action action, double delay, double period, int countLimit = -1, ICancelToken? cancelToken = null) {
        ValuePromise<int> promise = ValuePromise<int>.Acquire(out int rid, _coroutineMgr.EventLoop);
        PromiseTask asyncTask = taskPool.Acquire();
        asyncTask.id = NextId();
        asyncTask.TaskType = TYPE_ACTION;
        asyncTask.task = action;
        asyncTask.ctx = cancelToken;
        asyncTask.promise = promise;
        asyncTask.promiseRid = rid;
        asyncTask.triggerTime = GetTriggerTime(delay);
        asyncTask.gatingFrame = GetGatingFrame();

        asyncTask.period = CorrectPeriod(period);
        asyncTask.ScheduleType = SCHEDULE_FIXED_DELAY;
        if (countLimit > 0) {
            asyncTask.countdown = countLimit;
            asyncTask.HasCountdown = true;
        }
        _coroutineMgr.AddTimer(asyncTask, _timingType);
        return promise.VoidFuture;
    }

    public ValueFuture ScheduleWithFixedDelay(Action<object> action, object timerArg, double delay, double period, int countLimit = -1) {
        ValuePromise<int> promise = ValuePromise<int>.Acquire(out int rid, _coroutineMgr.EventLoop);
        PromiseTask asyncTask = taskPool.Acquire();
        asyncTask.id = NextId();
        asyncTask.TaskType = TYPE_ACTION_CTX;
        asyncTask.task = action;
        asyncTask.ctx = timerArg;
        asyncTask.promise = promise;
        asyncTask.promiseRid = rid;
        asyncTask.triggerTime = GetTriggerTime(delay);
        asyncTask.gatingFrame = GetGatingFrame();

        asyncTask.period = CorrectPeriod(period);
        asyncTask.ScheduleType = SCHEDULE_FIXED_DELAY;
        if (countLimit > 0) {
            asyncTask.countdown = countLimit;
            asyncTask.HasCountdown = true;
        }
        _coroutineMgr.AddTimer(asyncTask, _timingType);
        return promise.VoidFuture;
    }

    public ValueFuture ScheduleAtFixedRate(Action action, double delay, double period, int countLimit = -1, ICancelToken? cancelToken = null) {
        ValuePromise<int> promise = ValuePromise<int>.Acquire(out int rid, _coroutineMgr.EventLoop);
        PromiseTask asyncTask = taskPool.Acquire();
        asyncTask.id = NextId();
        asyncTask.TaskType = TYPE_ACTION;
        asyncTask.task = action;
        asyncTask.ctx = cancelToken;
        asyncTask.promise = promise;
        asyncTask.promiseRid = rid;
        asyncTask.triggerTime = GetTriggerTime(delay);
        asyncTask.gatingFrame = GetGatingFrame();

        asyncTask.period = CorrectPeriod(period);
        asyncTask.ScheduleType = SCHEDULE_FIXED_RATE;
        if (countLimit > 0) {
            asyncTask.countdown = countLimit;
            asyncTask.HasCountdown = true;
        }
        _coroutineMgr.AddTimer(asyncTask, _timingType);
        return promise.VoidFuture;
    }

    public ValueFuture ScheduleAtFixedRate(Action<object> action, object timerArg, double delay, double period, int countLimit = -1) {
        ValuePromise<int> promise = ValuePromise<int>.Acquire(out int rid, _coroutineMgr.EventLoop);
        PromiseTask asyncTask = taskPool.Acquire();
        asyncTask.id = NextId();
        asyncTask.TaskType = TYPE_ACTION_CTX;
        asyncTask.task = action;
        asyncTask.ctx = timerArg;
        asyncTask.promise = promise;
        asyncTask.promiseRid = rid;
        asyncTask.triggerTime = GetTriggerTime(delay);
        asyncTask.gatingFrame = GetGatingFrame();

        asyncTask.period = CorrectPeriod(period);
        asyncTask.ScheduleType = SCHEDULE_FIXED_RATE;
        if (countLimit > 0) {
            asyncTask.countdown = countLimit;
            asyncTask.HasCountdown = true;
        }
        _coroutineMgr.AddTimer(asyncTask, _timingType);
        return promise.VoidFuture;
    }

    public void Cancel(long timerId) {
        _coroutineMgr.Cancel(timerId);
    }

    public void Cancel(List<long> timerIds) {
        _coroutineMgr.Cancel(timerIds);
    }

    #region internal

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private double GetTriggerTime(double delay) {
        return _coroutineMgr.GetTime(_timingType, GameLoopPhase.Update) + Math.Max(0, delay);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetGatingFrame() {
        return _coroutineMgr.GetFrameCount(GameLoopPhase.Update) + 1;
    }

    private double CorrectPeriod(double period) {
        return _timingType switch
        {
            TimingType.Time => Math.Max(_coroutineMgr.MinPeriod, period),
            TimingType.UnscaledTime => Math.Max(_coroutineMgr.UnscaledMinPeriod, period),
            TimingType.FrameCount => Math.Max(1, period),
            _ => throw new ArgumentOutOfRangeException(nameof(_timingType), _timingType, null)
        };
    }

    #endregion
}
}