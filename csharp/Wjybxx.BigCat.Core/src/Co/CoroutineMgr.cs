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
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using Wjybxx.BigCat.Gameplay;
using Wjybxx.BigCat.Util;
using Wjybxx.Commons;
using Wjybxx.Commons.Attributes;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Concurrent;
using Wjybxx.Commons.Logger;
using Wjybxx.Commons.Pool;

#if UNITY_2021_3_OR_NEWER
using ILogger = Wjybxx.Commons.Logger.ILogger;
#endif

namespace Wjybxx.BigCat.Co
{
/// <summary>
/// 默认的协程管理器
///
/// 注：由于协程会大规模使用<see cref="PromiseTask"/>，因此id统一使用long类型 —— 也更容易判别对象是否被重用。
/// </summary>
[NotThreadSafe]
public class CoroutineMgr : ICoroutineMgr
{
    private static readonly ILogger logger = LoggerFactory.GetLogger(typeof(CoroutineMgr));

    /// <summary>
    /// 用于分配全局id
    /// (不直接使用long类型以避免错误访问)
    /// </summary>
    private static PaddedInt64 _idGenerator = new PaddedInt64(0);
    /// <summary>
    /// 全局Task池
    /// </summary>
    internal static readonly ConcurrentObjectPool<PromiseTask> taskPool = new ConcurrentObjectPool<PromiseTask>(
        () => new PromiseTask(), task => task.Reset(), 1024); // TODO 抽取环境变量
    /// <summary>
    /// 全局协程对象池 
    /// </summary>
    private static readonly ConcurrentObjectPool<Coroutine> coroutinePool = new ConcurrentObjectPool<Coroutine>(
        () => new Coroutine(), coroutine => coroutine.Reset(), 1024); // TODO 抽取环境变量

    /// <summary>
    /// id到协程或定时任务的字典
    /// (协程和定时任务的id属于同一命名空间)
    /// </summary>
    private readonly Dictionary<long, object> id2ObjectDict = new Dictionary<long, object>(100);
#nullable disable
    // 基于非缩放时间等待的队列
    private readonly BetterIndexedPriorityQueue<PromiseTask> unscaledQueue0;
    private readonly BetterIndexedPriorityQueue<PromiseTask> unscaledQueue1;
    private readonly BetterIndexedPriorityQueue<PromiseTask> unscaledQueue2;
    private readonly BetterIndexedPriorityQueue<PromiseTask> unscaledQueue3;
    private readonly BetterIndexedPriorityQueue<PromiseTask> unscaledQueue4;
    private readonly BetterIndexedPriorityQueue<PromiseTask> unscaledQueue5;
    private readonly BetterIndexedPriorityQueue<PromiseTask> unscaledQueue6;
    private readonly BetterIndexedPriorityQueue<PromiseTask> unscaledQueue7;
    private readonly BetterIndexedPriorityQueue<PromiseTask> unscaledQueue8;
    private readonly BetterIndexedPriorityQueue<PromiseTask> unscaledQueue9;
    /// 基于逻辑时间等待的队列
    private readonly BetterIndexedPriorityQueue<PromiseTask> timeQueue1 = new(TaskComparer.Inst, TaskIndexHelper.GetInst(11));
    private readonly BetterIndexedPriorityQueue<PromiseTask> timeQueue2 = new(TaskComparer.Inst, TaskIndexHelper.GetInst(12));
    private readonly BetterIndexedPriorityQueue<PromiseTask> timeQueue3 = new(TaskComparer.Inst, TaskIndexHelper.GetInst(13));
    private readonly BetterIndexedPriorityQueue<PromiseTask> timeQueue4 = new(TaskComparer.Inst, TaskIndexHelper.GetInst(14));
    private readonly BetterIndexedPriorityQueue<PromiseTask> timeQueue5 = new(TaskComparer.Inst, TaskIndexHelper.GetInst(15));
    private readonly BetterIndexedPriorityQueue<PromiseTask> timeQueue6 = new(TaskComparer.Inst, TaskIndexHelper.GetInst(16));
    private readonly BetterIndexedPriorityQueue<PromiseTask> timeQueue7 = new(TaskComparer.Inst, TaskIndexHelper.GetInst(17));
    private readonly BetterIndexedPriorityQueue<PromiseTask> timeQueue8 = new(TaskComparer.Inst, TaskIndexHelper.GetInst(18));
    // 基于帧等待的队列 - 默认不启用
    private readonly BetterIndexedPriorityQueue<PromiseTask> frameQueue1;
    private readonly BetterIndexedPriorityQueue<PromiseTask> frameQueue2;
    private readonly BetterIndexedPriorityQueue<PromiseTask> frameQueue3;
    private readonly BetterIndexedPriorityQueue<PromiseTask> frameQueue4;
    private readonly BetterIndexedPriorityQueue<PromiseTask> frameQueue5;
    private readonly BetterIndexedPriorityQueue<PromiseTask> frameQueue6;
    private readonly BetterIndexedPriorityQueue<PromiseTask> frameQueue7;
    private readonly BetterIndexedPriorityQueue<PromiseTask> frameQueue8;

    private readonly IEventLoop _eventLoop;
    private readonly GTime _time;
    private readonly double _minPeriod;
    private readonly double _unscaledMinPeriod;

    private readonly TimerMgr _timerMgr;
    private readonly TimerMgr? _unscaledTimerMgr;
    private readonly TimerMgr? _frameTimerMgr;

    private readonly Action<object> _onCoroutineExit;
    private readonly Action<ICancelToken, object> _onCancelRequest;
    private long _lastTimerId;
    private bool _isShuttingDown;
#nullable restore

    /// <summary>
    /// 
    /// </summary>
    /// <param name="eventLoop">关联的事件循环</param>
    /// <param name="time">协程关联的时间轴</param>
    /// <param name="minPeriod">周期性任务的最小间隔(秒)</param>
    /// <param name="unscaledMinPeriod">周期性任务的最小间隔(秒)</param>
    /// <param name="enableUnscaledQueue">是否启用非缩放时间队列</param>
    /// <param name="enableFrameQueue">是否启用帧时间队列</param>
    public CoroutineMgr(IEventLoop eventLoop, GTime time,
                        double minPeriod = 0.01, double unscaledMinPeriod = 0.01,
                        bool enableUnscaledQueue = true, bool enableFrameQueue = false) {
        this._eventLoop = eventLoop ?? throw new ArgumentNullException(nameof(eventLoop));
        this._time = time ?? throw new ArgumentNullException(nameof(time));
        this._minPeriod = minPeriod;
        this._unscaledMinPeriod = unscaledMinPeriod;
        this._timerMgr = new TimerMgr(this, TimingType.Time);
        this._onCoroutineExit = OnCoroutineExit; // 监听协程完成
        this._onCancelRequest = OnCancelRequest; // 监听Timer中的取消信号

        if (enableUnscaledQueue) {
            _unscaledTimerMgr = new TimerMgr(this, TimingType.UnscaledTime);
            unscaledQueue0 = new BetterIndexedPriorityQueue<PromiseTask>(TaskComparer.Inst, TaskIndexHelper.GetInst(0));
            unscaledQueue1 = new BetterIndexedPriorityQueue<PromiseTask>(TaskComparer.Inst, TaskIndexHelper.GetInst(1));
            unscaledQueue2 = new BetterIndexedPriorityQueue<PromiseTask>(TaskComparer.Inst, TaskIndexHelper.GetInst(2));
            unscaledQueue3 = new BetterIndexedPriorityQueue<PromiseTask>(TaskComparer.Inst, TaskIndexHelper.GetInst(3));
            unscaledQueue4 = new BetterIndexedPriorityQueue<PromiseTask>(TaskComparer.Inst, TaskIndexHelper.GetInst(4));
            unscaledQueue5 = new BetterIndexedPriorityQueue<PromiseTask>(TaskComparer.Inst, TaskIndexHelper.GetInst(5));
            unscaledQueue6 = new BetterIndexedPriorityQueue<PromiseTask>(TaskComparer.Inst, TaskIndexHelper.GetInst(6));
            unscaledQueue7 = new BetterIndexedPriorityQueue<PromiseTask>(TaskComparer.Inst, TaskIndexHelper.GetInst(7));
            unscaledQueue8 = new BetterIndexedPriorityQueue<PromiseTask>(TaskComparer.Inst, TaskIndexHelper.GetInst(8));
            unscaledQueue9 = new BetterIndexedPriorityQueue<PromiseTask>(TaskComparer.Inst, TaskIndexHelper.GetInst(9));
        }
        if (enableFrameQueue) {
            _frameTimerMgr = new TimerMgr(this, TimingType.FrameCount);
            frameQueue1 = new BetterIndexedPriorityQueue<PromiseTask>(TaskComparer.Inst, TaskIndexHelper.GetInst(21));
            frameQueue2 = new BetterIndexedPriorityQueue<PromiseTask>(TaskComparer.Inst, TaskIndexHelper.GetInst(22));
            frameQueue3 = new BetterIndexedPriorityQueue<PromiseTask>(TaskComparer.Inst, TaskIndexHelper.GetInst(23));
            frameQueue4 = new BetterIndexedPriorityQueue<PromiseTask>(TaskComparer.Inst, TaskIndexHelper.GetInst(24));
            frameQueue5 = new BetterIndexedPriorityQueue<PromiseTask>(TaskComparer.Inst, TaskIndexHelper.GetInst(25));
            frameQueue6 = new BetterIndexedPriorityQueue<PromiseTask>(TaskComparer.Inst, TaskIndexHelper.GetInst(26));
            frameQueue7 = new BetterIndexedPriorityQueue<PromiseTask>(TaskComparer.Inst, TaskIndexHelper.GetInst(27));
            frameQueue8 = new BetterIndexedPriorityQueue<PromiseTask>(TaskComparer.Inst, TaskIndexHelper.GetInst(28));
        }
    }

    public static CoroutineMgr CreateFrom(CoroutineMgr coroutineMgr, GTime time,
                                          bool? enableUnscaledQueue = null, bool? enableFrameQueue = null) {
        return new CoroutineMgr(coroutineMgr.EventLoop, time,
            coroutineMgr.MinPeriod, coroutineMgr.UnscaledMinPeriod,
            enableUnscaledQueue: enableUnscaledQueue ?? coroutineMgr.UnscaledTimerMgr != null,
            enableFrameQueue: enableFrameQueue ?? coroutineMgr.FrameTimerMgr != null);
    }

#nullable disable
    public IEventLoop EventLoop => _eventLoop;
    public ITimerMgr TimerMgr => _timerMgr;
    public ITimerMgr? UnscaledTimerMgr => _unscaledTimerMgr;
    public ITimerMgr? FrameTimerMgr => _frameTimerMgr;
#nullable restore

    public GTime Time => _time;
    public double MinPeriod => _minPeriod;
    public double UnscaledMinPeriod => _unscaledMinPeriod;
    public long LastTimerId => _lastTimerId;

    /// <summary>
    /// 理论上可以为用户创建的Timer和协程创建的Timer分配不同的ID段，这样可以让用户的Timer使用Int类型Id
    /// </summary>
    /// <returns></returns>
    internal static long NextId() => _idGenerator.IncrementAndGet();

    #region Update

    public void Update(GameLoopPhase phase) {
        GTime gTime = _time;
        BetterIndexedPriorityQueue<PromiseTask> taskQueue;
        switch (phase) {
            case GameLoopPhase.BeginOfFrame: {
                if ((taskQueue = unscaledQueue0) != null && taskQueue.Count > 0) {
                    UpdateTimeQueue(taskQueue, gTime.UnscaledTime, gTime.FrameCount);
                }
                break;
            }
            //
            case GameLoopPhase.EarlyUpdate: {
                // 非缩放时间，缩放时间，帧数
                if ((taskQueue = unscaledQueue1) != null && taskQueue.Count > 0) {
                    UpdateTimeQueue(taskQueue, gTime.UnscaledTime, gTime.FrameCount);
                }
                if ((taskQueue = timeQueue1) != null && taskQueue.Count > 0) {
                    UpdateTimeQueue(taskQueue, gTime.Time, gTime.FrameCount);
                }
                if ((taskQueue = frameQueue1) != null && taskQueue.Count > 0) {
                    UpdateFrameQueue(taskQueue, gTime.FrameCount);
                }
                break;
            }
            case GameLoopPhase.PostEarlyUpdate: {
                // 帧数，缩放时间，非缩放时间
                if ((taskQueue = frameQueue2) != null && taskQueue.Count > 0) {
                    UpdateFrameQueue(taskQueue, gTime.FrameCount);
                }
                if ((taskQueue = timeQueue2) != null && taskQueue.Count > 0) {
                    UpdateTimeQueue(taskQueue, gTime.Time, gTime.FrameCount);
                }
                if ((taskQueue = unscaledQueue2) != null && taskQueue.Count > 0) {
                    UpdateTimeQueue(taskQueue, gTime.UnscaledTime, gTime.FrameCount);
                }
                break;
            }
            //
            case GameLoopPhase.FixedUpdate: {
                if ((taskQueue = unscaledQueue3) != null && taskQueue.Count > 0) {
                    UpdateTimeQueue(taskQueue, gTime.FixedUnscaledTime, gTime.FixedFrameCount);
                }
                if ((taskQueue = timeQueue3) != null && taskQueue.Count > 0) {
                    UpdateTimeQueue(taskQueue, gTime.FixedTime, gTime.FixedFrameCount);
                }
                if ((taskQueue = frameQueue3) != null && taskQueue.Count > 0) {
                    UpdateFrameQueue(taskQueue, gTime.FixedFrameCount);
                }
                break;
            }
            case GameLoopPhase.PostFixedUpdate: {
                if ((taskQueue = frameQueue4) != null && taskQueue.Count > 0) {
                    UpdateFrameQueue(taskQueue, gTime.FixedFrameCount);
                }
                if ((taskQueue = timeQueue4) != null && taskQueue.Count > 0) {
                    UpdateTimeQueue(taskQueue, gTime.FixedTime, gTime.FixedFrameCount);
                }
                if ((taskQueue = unscaledQueue4) != null && taskQueue.Count > 0) {
                    UpdateTimeQueue(taskQueue, gTime.FixedUnscaledTime, gTime.FixedFrameCount);
                }
                break;
            }
            //
            case GameLoopPhase.Update: {
                if ((taskQueue = unscaledQueue5) != null && taskQueue.Count > 0) {
                    UpdateTimeQueue(taskQueue, gTime.UnscaledTime, gTime.FrameCount);
                }
                if ((taskQueue = timeQueue5) != null && taskQueue.Count > 0) {
                    UpdateTimeQueue(taskQueue, gTime.Time, gTime.FrameCount);
                }
                if ((taskQueue = frameQueue5) != null && taskQueue.Count > 0) {
                    UpdateFrameQueue(taskQueue, gTime.FrameCount);
                }
                break;
            }
            case GameLoopPhase.PostUpdate: {
                if ((taskQueue = frameQueue6) != null && taskQueue.Count > 0) {
                    UpdateFrameQueue(taskQueue, gTime.FrameCount);
                }
                if ((taskQueue = timeQueue6) != null && taskQueue.Count > 0) {
                    UpdateTimeQueue(taskQueue, gTime.Time, gTime.FrameCount);
                }
                if ((taskQueue = unscaledQueue6) != null && taskQueue.Count > 0) {
                    UpdateTimeQueue(taskQueue, gTime.UnscaledTime, gTime.FrameCount);
                }
                break;
            }
            //
            case GameLoopPhase.LateUpdate: {
                if ((taskQueue = unscaledQueue7) != null && taskQueue.Count > 0) {
                    UpdateTimeQueue(taskQueue, gTime.UnscaledTime, gTime.FrameCount);
                }
                if ((taskQueue = timeQueue7) != null && taskQueue.Count > 0) {
                    UpdateTimeQueue(taskQueue, gTime.Time, gTime.FrameCount);
                }
                if ((taskQueue = frameQueue7) != null && taskQueue.Count > 0) {
                    UpdateFrameQueue(taskQueue, gTime.FrameCount);
                }
                break;
            }
            case GameLoopPhase.PostLateUpdate: {
                if ((taskQueue = frameQueue8) != null && taskQueue.Count > 0) {
                    UpdateFrameQueue(taskQueue, gTime.FrameCount);
                }
                if ((taskQueue = timeQueue8) != null && taskQueue.Count > 0) {
                    UpdateTimeQueue(taskQueue, gTime.Time, gTime.FrameCount);
                }
                if ((taskQueue = unscaledQueue8) != null && taskQueue.Count > 0) {
                    UpdateTimeQueue(taskQueue, gTime.UnscaledTime, gTime.FrameCount);
                }
                break;
            }
            //
            case GameLoopPhase.EndOfFrame: {
                if ((taskQueue = unscaledQueue9) != null && taskQueue.Count > 0) {
                    UpdateTimeQueue(taskQueue, gTime.UnscaledTime, gTime.FrameCount);
                }
                break;
            }
        }
    }

    private void UpdateTimeQueue(BetterIndexedPriorityQueue<PromiseTask> taskQueue, double tickTime, int frameCount) {
        PromiseTask futureTask;
        while (taskQueue.TryPeekHead(out futureTask)) {
            if (tickTime < futureTask.triggerTime) {
                break;
            }
            if (frameCount < futureTask.gatingFrame) { // 不满足帧数限制
                futureTask.triggerTime = tickTime + 0.001; // 下一帧再触发
                taskQueue.PriorityChanged(futureTask);
                continue;
            }
            taskQueue.Dequeue();
            futureTask.gatingFrame = 0; // 避免影响排序

            bool enqueued = false;
            if (futureTask.Trigger(tickTime)) {
                if (_isShuttingDown) {
                    futureTask.Cancel(CancelCodes.REASON_SHUTDOWN);
                } else {
                    taskQueue.Enqueue(futureTask);
                    enqueued = true;
                }
            }
            if (!enqueued) {
                taskPool.Release(futureTask);
            }
        }
    }

    private void UpdateFrameQueue(BetterIndexedPriorityQueue<PromiseTask> taskQueue, int frameCount) {
        PromiseTask futureTask;
        double tickTime = frameCount;
        while (taskQueue.TryPeekHead(out futureTask)) {
            if (tickTime < futureTask.triggerTime) {
                break;
            }
            taskQueue.Dequeue();
            futureTask.gatingFrame = 0; // 避免影响排序

            bool enqueued = false;
            if (futureTask.Trigger(tickTime)) {
                if (_isShuttingDown) {
                    futureTask.Cancel(CancelCodes.REASON_SHUTDOWN);
                } else {
                    taskQueue.Enqueue(futureTask);
                    enqueued = true;
                }
            }
            if (!enqueued) {
                taskPool.Release(futureTask);
            }
        }
    }

    public void Reset() {
        _lastTimerId = 0;
        _isShuttingDown = false;
        // 不能清理协程字典，否则会导致协程对象无法回收
    }

    public void Shutdown() {
        _isShuttingDown = true;
        foreach (long id in id2ObjectDict.Keys.ToArray()) {
            Cancel(id, true);
        }
    }

    #endregion

    #region 协程

    public CoroutineUserContext StartCoroutine(Func<CoroutineTaskContext, ValueFuture> func, CoroutineStartArgs startArgs) {
        if (func == null) throw new ArgumentNullException(nameof(func));
        Coroutine coroutine = coroutinePool.Acquire();
        coroutine.id = NextId();
        coroutine.coroutineMgr = this;

        CoroutineUserContext userContext = new CoroutineUserContext(this, coroutine, coroutine.id, startArgs.entityId);
        CoroutineTaskContext taskContext = new CoroutineTaskContext(this, coroutine, coroutine.id, startArgs.entityId,
            startArgs.startArg1, startArgs.startArg2);

        id2ObjectDict.Add(coroutine.id, coroutine);
        try {
            ValueFuture future = func.Invoke(taskContext);
            if (future.IsCompleted) {
                coroutine.coResult = future.Memorize();
                coroutine.InterruptTask();
            } else {
                future.OnCompleted(_onCoroutineExit, coroutine, _eventLoop, TaskOptions.STAGE_TRY_INLINE);
            }
        }
        catch (Exception ex) {
            logger.Warn(ex, "coroutine start caught exception");
            coroutine.IsTerminated = true;
            coroutine.coResult = ValueFuture.FromException(ex);
            coroutine.InterruptTask(); // 清理绑定的异步任务，否则不能安全回收；但回收异常失败的协程还是有风险的
        }
        return userContext;
    }

    public CoroutineUserContext<T, R> StartCoroutine<T, R>(Func<CoroutineTaskContext<T, R>, ValueFuture> func,
                                                           CoroutineStartArgs<T, R> startArgs) {
        if (func == null) throw new ArgumentNullException(nameof(func));
        Coroutine coroutine = coroutinePool.Acquire();
        coroutine.id = NextId();
        coroutine.coroutineMgr = this;

        CoroutineUserContext<T, R> userContext = new CoroutineUserContext<T, R>(this, coroutine, coroutine.id, startArgs.entityId,
            startArgs.inputCodec, startArgs.outputCodec);
        CoroutineTaskContext<T, R> taskContext = new CoroutineTaskContext<T, R>(this, coroutine, coroutine.id, startArgs.entityId,
            startArgs.startArg1, startArgs.startArg2,
            startArgs.inputCodec, startArgs.outputCodec);

        id2ObjectDict.Add(coroutine.id, coroutine);
        try {
            ValueFuture future = func.Invoke(taskContext);
            if (future.IsCompleted) {
                coroutine.coResult = future.Memorize();
                coroutine.InterruptTask();
            } else {
                future.OnCompleted(_onCoroutineExit, coroutine, _eventLoop, TaskOptions.STAGE_TRY_INLINE);
            }
        }
        catch (Exception ex) {
            coroutine.IsTerminated = true;
            coroutine.coResult = ValueFuture.FromException(ex);
            logger.Warn(ex, "coroutine start caught exception");
            coroutine.InterruptTask(); // 清理绑定的异步任务，否则不能安全回收；但回收异常失败的协程还是有风险的
        }
        return userContext;
    }

    private void OnCoroutineExit(object state) {
        Coroutine coroutine = (Coroutine)state;
        coroutine.IsTerminated = true;
        coroutine.coResult = coroutine.coResult.Memorize();
        coroutine.InterruptTask();

        // 中断用户可能触发OnUserContextDisposed，因此需要保留id测试是否已回收
        long coroutineId = coroutine.id;
        coroutine.InterruptUser();
        if (coroutine.IsUserContextDisposed && id2ObjectDict.Remove(coroutineId)) {
            coroutinePool.Release(coroutine);
        }
    }

    internal void OnUserContextDisposed(Coroutine coroutine) {
        if (coroutine.IsTerminated && id2ObjectDict.Remove(coroutine.id)) {
            coroutinePool.Release(coroutine);
        }
    }

    public void Cancel(long coroutineId, bool interruptIfRunning = false) {
        if (!id2ObjectDict.TryGetValue(coroutineId, out object obj)) return; // 这里不能直接删除
        if (obj is PromiseTask task) {
            CancelTimer(task, CancelCodes.REASON_DEFAULT);
        } else {
            Coroutine coroutine = (Coroutine)obj;
            coroutine.Cancel(coroutineId, interruptIfRunning); // 由回调触发回收
        }
    }

    public void Cancel(List<long> coroutineIds, bool interruptIfRunning = false) {
        foreach (long coroutineId in coroutineIds) {
            Cancel(coroutineId, interruptIfRunning);
        }
    }

    #endregion

    #region timer

#nullable disable

    private static bool IsFixedUpdatePhase(GameLoopPhase phase) {
        return phase == GameLoopPhase.FixedUpdate || phase == GameLoopPhase.PostFixedUpdate;
    }

    private void OnCancelRequest(ICancelToken cancelToken, object state) {
        long timerId = (long)state;
        Cancel(timerId);
    }

    internal void AddTimer(PromiseTask task, TimingType timingType, GameLoopPhase phase = GameLoopPhase.Update) {
        BetterIndexedPriorityQueue<PromiseTask> queue = GetQueue(timingType, phase);
        queue.Add(task);
        id2ObjectDict.Add(task.id, task);
        _lastTimerId = task.id;
        // 监听取消信号
        ICancelToken cancelToken = task.GetCancelToken();
        if (cancelToken.CanBeCancelled && task.IsEnabled(TaskOptions.LISTEN_CANCEL_TOKEN)) {
            task.cancelRegistration = cancelToken.ThenAcceptAsync(_eventLoop, _onCancelRequest, task.id, TaskOptions.STAGE_TRY_INLINE);
        }
    }

    /// <summary>
    /// 注意：调用该方法后不可再保留Timer的引用
    /// </summary>
    internal void CancelTimer(PromiseTask task, int cancelCode) {
        // Cancel可能触发递归删除，因此保留id校验
        long timerId = task.id;
        task.Cancel(cancelCode);
        if (id2ObjectDict.Remove(timerId)) {
            if (task.queueId >= 0) { // 当前可能正在执行
                GetQueue(task.queueId).Remove(task);
            }
            taskPool.Release(task);
        }
    }

    internal double GetTime(TimingType timingType, GameLoopPhase phase) {
        bool isFixedUpdatePhase = IsFixedUpdatePhase(phase);
        return timingType switch
        {
            TimingType.Time => isFixedUpdatePhase ? _time.FixedTime : _time.Time,
            TimingType.UnscaledTime => isFixedUpdatePhase ? _time.FixedUnscaledTime : _time.UnscaledTime,
            TimingType.FrameCount => isFixedUpdatePhase ? _time.FixedFrameCount : _time.FrameCount,
            _ => throw new ArgumentOutOfRangeException(nameof(timingType), timingType, null),
        };
    }

    internal int GetFrameCount(GameLoopPhase phase) {
        return IsFixedUpdatePhase(phase) ? _time.FixedFrameCount : _time.FrameCount;
    }

    internal void CheckQueue(TimingType timingType, GameLoopPhase phase) {
        if (GetQueue(timingType, phase) == null) {
            throw new InvalidOperationException($"{timingType}-{phase} is disabled");
        }
    }

    private BetterIndexedPriorityQueue<PromiseTask> GetQueue(TimingType timingType, GameLoopPhase phase) {
        return timingType switch
        {
            TimingType.Time => GetTimeQueue(phase),
            TimingType.UnscaledTime => GetUnscaledQueue(phase),
            TimingType.FrameCount => GetFrameQueue(phase),
            _ => throw new ArgumentOutOfRangeException(nameof(timingType)),
        };
    }

    private BetterIndexedPriorityQueue<PromiseTask> GetUnscaledQueue(GameLoopPhase phase) {
        return phase switch
        {
            GameLoopPhase.BeginOfFrame => unscaledQueue0,
            GameLoopPhase.EarlyUpdate => unscaledQueue1,
            GameLoopPhase.PostEarlyUpdate => unscaledQueue2,
            GameLoopPhase.FixedUpdate => unscaledQueue3,
            GameLoopPhase.PostFixedUpdate => unscaledQueue4,
            GameLoopPhase.Update => unscaledQueue5,
            GameLoopPhase.PostUpdate => unscaledQueue6,
            GameLoopPhase.LateUpdate => unscaledQueue7,
            GameLoopPhase.PostLateUpdate => unscaledQueue8,
            GameLoopPhase.EndOfFrame => unscaledQueue9,
            _ => throw new ArgumentOutOfRangeException(nameof(phase), phase, null)
        };
    }

    private BetterIndexedPriorityQueue<PromiseTask> GetTimeQueue(GameLoopPhase phase) {
        return phase switch
        {
            // GameLoopPhase.BeginOfFrame => timeQueue0,
            GameLoopPhase.EarlyUpdate => timeQueue1,
            GameLoopPhase.PostEarlyUpdate => timeQueue2,
            GameLoopPhase.FixedUpdate => timeQueue3,
            GameLoopPhase.PostFixedUpdate => timeQueue4,
            GameLoopPhase.Update => timeQueue5,
            GameLoopPhase.PostUpdate => timeQueue6,
            GameLoopPhase.LateUpdate => timeQueue7,
            GameLoopPhase.PostLateUpdate => timeQueue8,
            // GameLoopPhase.EndOfFrame => timeQueue9,
            _ => throw new ArgumentOutOfRangeException(nameof(phase), phase, null)
        };
    }

    private BetterIndexedPriorityQueue<PromiseTask> GetFrameQueue(GameLoopPhase phase) {
        return phase switch
        {
            // GameLoopPhase.BeginOfFrame => frameQueue0,
            GameLoopPhase.EarlyUpdate => frameQueue1,
            GameLoopPhase.PostEarlyUpdate => frameQueue2,
            GameLoopPhase.FixedUpdate => frameQueue3,
            GameLoopPhase.PostFixedUpdate => frameQueue4,
            GameLoopPhase.Update => frameQueue5,
            GameLoopPhase.PostUpdate => frameQueue6,
            GameLoopPhase.LateUpdate => frameQueue7,
            GameLoopPhase.PostLateUpdate => frameQueue8,
            // GameLoopPhase.EndOfFrame => frameQueue9,
            _ => throw new ArgumentOutOfRangeException(nameof(phase), phase, null)
        };
    }

    private BetterIndexedPriorityQueue<PromiseTask> GetQueue(int queueId) {
        return queueId switch
        {
            0 => unscaledQueue0,
            1 => unscaledQueue1,
            2 => unscaledQueue2,
            3 => unscaledQueue3,
            4 => unscaledQueue4,
            5 => unscaledQueue5,
            6 => unscaledQueue6,
            7 => unscaledQueue7,
            8 => unscaledQueue8,
            9 => unscaledQueue9,
            //
            11 => timeQueue1,
            12 => timeQueue2,
            13 => timeQueue3,
            14 => timeQueue4,
            15 => timeQueue5,
            16 => timeQueue6,
            17 => timeQueue7,
            18 => timeQueue8,
            //
            21 => frameQueue1,
            22 => frameQueue2,
            23 => frameQueue3,
            24 => frameQueue4,
            25 => frameQueue5,
            26 => frameQueue6,
            27 => frameQueue7,
            28 => frameQueue8,
            _ => throw new ArgumentOutOfRangeException(nameof(queueId), null, queueId.ToString())
        };
    }
#nullable restore

    #endregion

    #region 辅助类

    /// <summary>
    /// 任务触发时机排序
    /// </summary>
    private class TaskComparer : IComparer<PromiseTask>
    {
        public static readonly TaskComparer Inst = new TaskComparer();

        public int Compare(PromiseTask? x, PromiseTask? y) {
            // ReSharper disable PossibleNullReferenceException
            // 先按照触发时间排序
            int r = x.triggerTime.CompareTo(y.triggerTime);
            if (r != 0) return r;

            // 时间相同时，可能有下一帧触发的任务
            r = x.gatingFrame.CompareTo(y.gatingFrame);
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