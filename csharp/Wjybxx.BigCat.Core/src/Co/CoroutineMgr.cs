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
using System.Runtime.ExceptionServices;
using System.Threading;
using Wjybxx.BigCat.Gameplay;
using Wjybxx.BigCat.Util;
using Wjybxx.Commons;
using Wjybxx.Commons.Attributes;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Concurrent;
using Wjybxx.Commons.Logger;

#if UNITY_2021_3_OR_NEWER
using ILogger = Wjybxx.Commons.Logger.ILogger;
#endif

namespace Wjybxx.BigCat.Co
{
/// <summary>
/// 默认的协程管理器
///
/// 注：由于EventLoop的引用不可变更，因此CoroutineMgr对象池需要按EventLoop管理。
/// (Coroutine只有在协程退出，且用户销毁上下文的情况下才被回收，因此协程字典在Stop/Reset时不能直接清理 TODO 再考虑池化问题)
/// </summary>
[NotThreadSafe]
public class CoroutineMgr : ICoroutineMgr
{
    private static readonly ILogger logger = LoggerFactory.GetLogger(typeof(CoroutineMgr));

#nullable disable
    private IEventLoop _eventLoop;
    private ITime _time;
    private readonly Dictionary<long, Coroutine> _coroutineDic;
    private readonly TimerQueue _timerQueue;
    private Status _status;

    private readonly Action<object> _onCoroutineExit;
    private readonly Action<object> _onCancelRequest;
#nullable restore

    /// <summary>
    /// 
    /// </summary>
    /// <param name="eventLoop">关联的事件循环</param>
    /// <param name="time">时间</param>
    public CoroutineMgr(IEventLoop eventLoop, ITime time) {
        this._eventLoop = eventLoop ?? throw new ArgumentNullException(nameof(eventLoop));
        this._time = time ?? throw new ArgumentNullException(nameof(time));
        this._coroutineDic = new Dictionary<long, Coroutine>();
        this._timerQueue = new TimerQueue(eventLoop, time, 1);

        this._onCoroutineExit = OnCoroutineExit; // 监听协程完成
        this._onCancelRequest = OnCancellationRequest; // 监听Timer中的取消信号
    }

    /// <summary>
    /// 关联的事件循环
    /// </summary>
    public IEventLoop EventLoop {
        get => _eventLoop;
        set => _eventLoop = value;
    }
    /// <summary>
    /// 关联的时间
    /// </summary>
    public ITime Time {
        get => _time;
        set => _time = value;
    }
    /// <summary>
    /// 关联的TimerQueue
    /// </summary>
    public ITimerQueue TimerQueue => _timerQueue;

    #region 生命周期

    public bool IsRunning => _status == Status.Running;
    public bool IsShuttingDown => _status >= Status.ShuttingDown;
    public bool IsShutdown => _status >= Status.Shutdown;

    /// <summary>
    /// 启动协程调度器
    /// </summary>
    public void Start() {
        if (_status == Status.Unstarted) {
            _status = Status.Running;
            _timerQueue.Start();
        }
    }

    /// <summary>
    /// 更新协程相关任务
    /// </summary>
    public void Update() {
        _timerQueue.Update();
    }

    /// <summary>
    /// 停止协程和协程相关任务
    /// </summary>
    public void Stop(bool quietly = false) {
        if (_status < Status.Running) {
            return;
        }
        _status = Status.ShuttingDown;
        // 中断协程
        foreach (var coroutine in _coroutineDic.Values.ToArray()) {
            InterruptTask(coroutine);
            InterruptUser(coroutine);
        }
        _timerQueue.Stop(quietly);
        _status = Status.Shutdown;
    }

    /// <summary>
    /// 重置数据
    /// </summary>
    public void Reset() {
        // TODO
    }

    #endregion

    #region 协程

    public CoroutineUserContext StartCoroutine(Func<CoroutineTaskContext, ValueFuture> func, CoroutineStartArgs startArgs) {
        if (func == null) throw new ArgumentNullException(nameof(func));
        Coroutine coroutine = Coroutine.Acquire();
        coroutine.id = Coroutine.NextId();
        coroutine.cancelToken = startArgs.cancelToken;

        CoroutineUserContext userContext = new CoroutineUserContext(this, coroutine.id, startArgs.cancelToken, startArgs.userArg);
        CoroutineTaskContext taskContext = new CoroutineTaskContext(this, coroutine.id, startArgs.cancelToken,
            startArgs.startArg1, startArgs.startArg2);

        _coroutineDic.Add(coroutine.id, coroutine);
        try {
            ValueFuture future = func.Invoke(taskContext);
            coroutine.coResult = future;
            if (future.IsCompleted) {
                coroutine.IsTerminated = true;
                coroutine.coResult = future.Memorize();
                InterruptTask(coroutine);
            } else {
                future.GetAwaitable(_eventLoop, TaskOptions.STAGE_TRY_INLINE)
                    .GetAwaiter()
                    .OnCompleted(_onCoroutineExit, coroutine);
                // 监听取消令牌
                if (startArgs.cancelToken.CanBeCanceled) {
                    coroutine.cancelRegistration = startArgs.cancelToken.Register(_onCancelRequest, coroutine.id);
                }
            }
        }
        catch (Exception ex) {
            logger.Warn(ex, "coroutine start caught exception");
            coroutine.IsTerminated = true;
            coroutine.coResult = ValueFuture.FromException(ex);
            InterruptTask(coroutine); // 清理绑定的异步任务，否则不能安全回收；但回收异常失败的协程还是有风险的
        }
        return userContext;
    }

    public CoroutineUserContext<T, R> StartCoroutine<T, R>(Func<CoroutineTaskContext<T, R>, ValueFuture> func,
                                                           CoroutineStartArgs<T, R> startArgs) {
        if (func == null) throw new ArgumentNullException(nameof(func));
        Coroutine coroutine = Coroutine.Acquire();
        coroutine.id = Coroutine.NextId();
        coroutine.cancelToken = startArgs.cancelToken;

        CoroutineUserContext<T, R> userContext = new CoroutineUserContext<T, R>(this, coroutine.id, startArgs.cancelToken, startArgs.userArg,
            startArgs.inputCodec, startArgs.outputCodec);
        CoroutineTaskContext<T, R> taskContext = new CoroutineTaskContext<T, R>(this, coroutine.id, startArgs.cancelToken,
            startArgs.startArg1, startArgs.startArg2,
            startArgs.inputCodec, startArgs.outputCodec);

        _coroutineDic.Add(coroutine.id, coroutine);
        try {
            ValueFuture future = func.Invoke(taskContext);
            coroutine.coResult = future;
            if (future.IsCompleted) {
                coroutine.coResult = future.Memorize();
                coroutine.IsTerminated = true;
                InterruptTask(coroutine);
            } else {
                future.GetAwaitable(_eventLoop, TaskOptions.STAGE_TRY_INLINE)
                    .GetAwaiter()
                    .OnCompleted(_onCoroutineExit, coroutine);
                // 监听取消令牌
                if (startArgs.cancelToken.CanBeCanceled) {
                    coroutine.cancelRegistration = startArgs.cancelToken.Register(_onCancelRequest, coroutine.id);
                }
            }
        }
        catch (Exception ex) {
            coroutine.IsTerminated = true;
            coroutine.coResult = ValueFuture.FromException(ex);
            logger.Warn(ex, "coroutine start caught exception");
            InterruptTask(coroutine); // 清理绑定的异步任务，否则不能安全回收；但回收异常失败的协程还是有风险的
        }
        return userContext;
    }

    private void OnCoroutineExit(object state) {
        Coroutine coroutine = (Coroutine)state;
        coroutine.IsTerminated = true;
        coroutine.coResult = coroutine.coResult.Memorize();
        InterruptTask(coroutine);

        // 中断用户可能触发OnUserContextDisposed，因此需要保留id测试是否已回收
        long coroutineId = coroutine.id;
        InterruptUser(coroutine);
        if (coroutine.IsUserContextDisposed && _coroutineDic.Remove(coroutineId)) {
            Coroutine.Release(coroutine);
        }
    }

    private void OnUserContextDisposed(Coroutine coroutine) {
        if (coroutine.IsTerminated && _coroutineDic.Remove(coroutine.id)) {
            Coroutine.Release(coroutine);
        }
    }

    #endregion

    #region 上下文

    /// <summary>
    /// 检查协程是否已执行结束
    /// </summary>
    internal bool IsTerminated(long coroutineId) {
        CheckEventLoop();
        if (!_coroutineDic.TryGetValue(coroutineId, out Coroutine coroutine)) {
            throw new InvalidOperationException("coroutine disposed");
        }
        return coroutine.IsTerminated;
    }

    /// <summary>
    /// 获取协程执行结果
    /// </summary>
    internal TaskResult GetResult(long coroutineId) {
        CheckEventLoop();
        if (!_coroutineDic.TryGetValue(coroutineId, out Coroutine coroutine)) {
            throw new InvalidOperationException("coroutine disposed");
        }
        return coroutine.coResult.GetResult(TaskOptions.SUPPRESS_ALL_THROW, true);
    }

    /// <summary>
    /// 用户上下文是否已销毁
    /// </summary>
    internal bool IsUserContextDisposed(long coroutineId) {
        CheckEventLoop();
        if (!_coroutineDic.TryGetValue(coroutineId, out Coroutine coroutine)) {
            return true;
        }
        return coroutine.IsUserContextDisposed;
    }

    /// <summary>
    /// 销毁用户上下文
    /// </summary>
    internal void DisposeUserContext(long coroutineId) {
        CheckEventLoop();
        if (!_coroutineDic.TryGetValue(coroutineId, out Coroutine coroutine)) {
            return;
        }
        if (coroutine.IsUserContextDisposed) {
            return;
        }
        coroutine.IsUserContextDisposed = true;
        OnUserContextDisposed(coroutine);
    }

    /// <summary>
    /// 取消协程执行
    /// </summary>
    /// <param name="coroutineId">协程ID</param>
    /// <param name="interruptIfRunning">是否中断协程</param>
    internal void Cancel(long coroutineId, bool interruptIfRunning = false) {
        CheckEventLoop();
        if (!_coroutineDic.TryGetValue(coroutineId, out Coroutine coroutine)) {
            return;
        }
        coroutine.IsCancellationRequested = true;
        if (interruptIfRunning) {
            InterruptTask(coroutine);
        }
    }

    /// <summary>
    /// 收到取消令牌的取消信号
    /// </summary>
    /// <param name="state"></param>
    private void OnCancellationRequest(object state) {
        if (!_eventLoop.InEventLoop()) {
            _eventLoop.Execute(() => OnCancellationRequest(state));
            return;
        }
        long coroutineId = (long)state;
        Cancel(coroutineId); // 默认不中断协程
    }

    /// <summary>
    /// 中断协程
    /// </summary>
    private void InterruptTask(Coroutine coroutine) {
        coroutine.IsInterrupted = true;
        // 优先进入被中断状态
        ValuePromise<int> cmdPromise = coroutine.cmdReaderPromise;
        if (cmdPromise != null) {
            cmdPromise.TrySetException(coroutine.cmdReaderPromiseRid, new ThreadInterruptedException());
        }
        PromiseTask asyncTask = coroutine.asyncTask;
        if (asyncTask != null) {
            _timerQueue.Cancel(asyncTask.id);
        }
    }

    /// <summary>
    /// 中断用户
    /// </summary>
    private void InterruptUser(Coroutine coroutine) {
        ValuePromise<int> resultPromise = coroutine.resultReaderPromise;
        if (resultPromise != null) {
            resultPromise.TrySetException(coroutine.resultReaderPromiseRid, new ThreadInterruptedException());
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CheckEventLoop() {
        if (!_eventLoop.InEventLoop()) {
            throw new GuardedOperationException("Method must be called from the eventLoop thread");
        }
    }

    #endregion

    #region channel

    internal void WriteResult<T>(long coroutineId, DataKey<T> codec, T value) {
        CheckEventLoop();
        if (!_coroutineDic.TryGetValue(coroutineId, out Coroutine coroutine)) {
            throw new InvalidOperationException("coroutine disposed");
        }
        UnionValue unionValue = (ObjectUtil.IsNullableType<T>() && value == null) ? UnionValue.Null : codec.Box(value);
        coroutine.resultBuffer.Enqueue(unionValue);
        // 唤醒用户
        ValuePromise<int> promise = coroutine.resultReaderPromise;
        if (promise != null) {
            promise.TrySetResult(coroutine.resultReaderPromiseRid, 0);
        }
    }

    internal bool TryReadResult<T>(long coroutineId, DataKey<T> codec, out T value) {
        CheckEventLoop();
        if (!_coroutineDic.TryGetValue(coroutineId, out Coroutine coroutine)) {
            throw new InvalidOperationException("coroutine disposed");
        }
        if (coroutine.resultBuffer.TryDequeue(out UnionValue unionValue)) {
            value = unionValue.IsNull ? default : codec.Unbox(unionValue);
            return true;
        }
        value = default;
        return false;
    }

    [PooledTask(10)]
    internal async ValueFuture<T> ReadResultAsync<T>(long coroutineId, DataKey<T> codec, double timeout) {
        CheckEventLoop();
        if (!_coroutineDic.TryGetValue(coroutineId, out Coroutine coroutine)) {
            throw new InvalidOperationException("coroutine disposed");
        }
        if (coroutine.resultBuffer.TryDequeue(out UnionValue unionValue)) {
            T value = unionValue.IsNull ? default : codec.Unbox(unionValue);
            return value;
        }
        if (coroutine.resultReaderPromise != null) {
            throw new InvalidOperationException("read result task already exists");
        }
        if (coroutine.IsTerminated) { // 协程终止属于正常情况
            throw new OperationCanceledException("coroutine is terminated");
        }
        // 等待被唤醒
        ValuePromise<int> promise = ValuePromise<int>.Acquire(out int rid, _eventLoop);
        coroutine.resultReaderPromise = promise;
        coroutine.resultReaderPromiseRid = rid;
        long cancellerTaskId = 0;
        if (timeout > 0) {
            PromiseTask asyncTask = AddCanceller(promise, rid, timeout, coroutine.cancelToken);
            cancellerTaskId = asyncTask.id;
        }

        try {
            await promise.Future;
            unionValue = coroutine.resultBuffer.Dequeue();
            T value = unionValue.IsNull ? default : codec.Unbox(unionValue);
            return value;
        }
        finally {
            // 醒来先清理字段，因此它处无需校验promise字段的有效性
            Debug.Assert(coroutine.resultReaderPromise == promise);
            coroutine.resultReaderPromise = null;
            coroutine.resultReaderPromiseRid = 0;
            if (cancellerTaskId > 0) {
                _timerQueue.Cancel(cancellerTaskId);
            }
        }
    }

    [PooledTask(10)]
    internal async ValueFuture<TaskResult<T>> ReadResultAsync2<T>(long coroutineId, DataKey<T> codec, double timeout) {
        CheckEventLoop();
        if (!_coroutineDic.TryGetValue(coroutineId, out Coroutine coroutine)) {
            throw new InvalidOperationException("coroutine disposed");
        }
        if (coroutine.resultBuffer.TryDequeue(out UnionValue unionValue)) {
            T value = unionValue.IsNull ? default : codec.Unbox(unionValue);
            return TaskResult<T>.FromResult(value);
        }
        if (coroutine.resultReaderPromise != null) {
            throw new InvalidOperationException("read result task already exists");
        }
        if (coroutine.IsTerminated) { // 协程终止属于正常情况，传入协程的取消令牌并无意义
            return TaskResult<T>.FromCancelled();
        }
        // 等待被唤醒
        ValuePromise<int> promise = ValuePromise<int>.Acquire(out int rid, _eventLoop);
        coroutine.resultReaderPromise = promise;
        coroutine.resultReaderPromiseRid = rid;
        long cancellerTaskId = 0;
        if (timeout > 0) {
            PromiseTask asyncTask = AddCanceller(promise, rid, timeout, coroutine.cancelToken);
            cancellerTaskId = asyncTask.id;
        }
        TaskResult<int> result = await promise.Future.GetAwaitable2();
        // 醒来先清理字段，因此它处无需校验promise字段的有效性
        Debug.Assert(coroutine.resultReaderPromise == promise);
        coroutine.resultReaderPromise = null;
        coroutine.resultReaderPromiseRid = 0;
        if (cancellerTaskId > 0) {
            _timerQueue.Cancel(cancellerTaskId);
        }
        // 解析结果
        if (result.IsSucceeded) {
            unionValue = coroutine.resultBuffer.Dequeue();
            T value = unionValue.IsNull ? default : codec.Unbox(unionValue);
            return TaskResult<T>.FromResult(value);
        }
        return result.Cast<T>(); // 避免处理异常
    }

    internal void WriteCmd<T>(long coroutineId, DataKey<T> codec, T value) {
        CheckEventLoop();
        if (!_coroutineDic.TryGetValue(coroutineId, out Coroutine coroutine)) {
            throw new InvalidOperationException("coroutine disposed");
        }
        UnionValue unionValue = (ObjectUtil.IsNullableType<T>() && value == null) ? UnionValue.Null : codec.Box(value);
        coroutine.cmdBuffer.Enqueue(unionValue);
        // 唤醒Task TODO 可能要延迟唤醒？
        ValuePromise<int> promise = coroutine.cmdReaderPromise;
        if (promise != null) {
            promise.TrySetResult(coroutine.cmdReaderPromiseRid, 0);
        }
    }

    internal bool TryReadCmd<T>(long coroutineId, DataKey<T> codec, out T value) {
        CheckEventLoop();
        if (!_coroutineDic.TryGetValue(coroutineId, out Coroutine coroutine)) {
            throw new InvalidOperationException("coroutine disposed");
        }
        if (coroutine.cmdBuffer.TryDequeue(out UnionValue unionValue)) {
            value = unionValue.IsNull ? default : codec.Unbox(unionValue);
            return true;
        }
        value = default;
        return false;
    }

    [PooledTask(10)]
    internal async ValueFuture<T> ReadCmdAsync<T>(long coroutineId, DataKey<T> codec, double timeout) {
        CheckEventLoop();
        if (!_coroutineDic.TryGetValue(coroutineId, out Coroutine coroutine)) {
            throw new InvalidOperationException("coroutine disposed");
        }
        if (coroutine.cmdBuffer.TryDequeue(out UnionValue unionValue)) {
            T value = unionValue.IsNull ? default : codec.Unbox(unionValue);
            return value;
        }
        if (coroutine.cmdReaderPromise != null) {
            throw new InvalidOperationException("read cmd task already exists");
        }
        if (coroutine.IsCancellationRequested) { // 用户已取消任务
            throw new OperationCanceledException(coroutine.cancelToken);
        }
        // 等待被唤醒
        ValuePromise<int> promise = ValuePromise<int>.Acquire(out int rid, _eventLoop);
        coroutine.cmdReaderPromise = promise;
        coroutine.cmdReaderPromiseRid = rid;
        long cancellerTaskId = 0;
        if (timeout > 0) {
            PromiseTask asyncTask = AddCanceller(promise, rid, timeout, coroutine.cancelToken);
            cancellerTaskId = asyncTask.id;
        }

        try {
            await promise.Future;
            unionValue = coroutine.cmdBuffer.Dequeue();
            T value = unionValue.IsNull ? default : codec.Unbox(unionValue);
            return value;
        }
        finally {
            // 醒来先清理字段，因此它处无需校验promise字段的有效性
            Debug.Assert(coroutine.cmdReaderPromise == promise);
            coroutine.cmdReaderPromise = null;
            coroutine.cmdReaderPromiseRid = 0;
            if (cancellerTaskId > 0) {
                _timerQueue.Cancel(cancellerTaskId);
            }
        }
    }

    [PooledTask(10)]
    internal async ValueFuture<TaskResult<T>> ReadCmdAsync2<T>(long coroutineId, DataKey<T> codec, double timeout) {
        CheckEventLoop();
        if (!_coroutineDic.TryGetValue(coroutineId, out Coroutine coroutine)) {
            throw new InvalidOperationException("coroutine disposed");
        }
        if (coroutine.cmdBuffer.TryDequeue(out UnionValue unionValue)) {
            T value = unionValue.IsNull ? default : codec.Unbox(unionValue);
            return TaskResult<T>.FromResult(value);
        }
        if (coroutine.cmdReaderPromise != null) {
            throw new InvalidOperationException("read cmd task already exists");
        }
        if (coroutine.IsCancellationRequested) { // 用户已取消任务
            return TaskResult<T>.FromCancelled(coroutine.cancelToken);
        }
        // 等待被唤醒
        ValuePromise<int> promise = ValuePromise<int>.Acquire(out int rid, _eventLoop);
        coroutine.cmdReaderPromise = promise;
        coroutine.cmdReaderPromiseRid = rid;
        long cancellerTaskId = 0;
        if (timeout > 0) {
            PromiseTask asyncTask = AddCanceller(promise, rid, timeout, coroutine.cancelToken);
            cancellerTaskId = asyncTask.id;
        }
        TaskResult<int> result = await promise.Future.GetAwaitable2();
        // 醒来先清理字段，因此它处无需校验promise字段的有效性
        Debug.Assert(coroutine.cmdReaderPromise == promise);
        coroutine.cmdReaderPromise = null;
        coroutine.cmdReaderPromiseRid = 0;
        if (cancellerTaskId > 0) {
            _timerQueue.Cancel(cancellerTaskId);
        }
        // 解析结果
        if (result.IsSucceeded) {
            unionValue = coroutine.cmdBuffer.Dequeue();
            T value = unionValue.IsNull ? default : codec.Unbox(unionValue);
            return TaskResult<T>.FromResult(value);
        }
        return result.Cast<T>(); // 避免处理异常
    }

    #endregion

    #region 协程命令/指令

    /// <summary>
    /// 挂起一段时间（高频指令）
    /// </summary>
    [PooledTask]
    internal ValueFuture Sleep(long coroutineId, double delayTime, int delayFrame) {
        CheckEventLoop();
        if (!_coroutineDic.TryGetValue(coroutineId, out Coroutine coroutine)) {
            throw new InvalidOperationException("coroutine disposed");
        }
        if (coroutine.asyncTask != null) {
            throw new InvalidOperationException("There are pending asynchronous operations");
        }
        if (delayTime <= 0) delayTime = 0;
        if (delayFrame < 0) delayFrame = 0;
        // 传递取消令牌
        ValuePromise<int> promise = ValuePromise<int>.Acquire(out int rid, _eventLoop);
        PromiseTask asyncTask = PromiseTask.Acquire();
        asyncTask.id = PromiseTask.NextId();
        asyncTask.TaskType = TaskBuilder.TYPE_EMPTY;
        asyncTask.promise = promise;
        asyncTask.promiseRid = rid;
        asyncTask.cancelToken = coroutine.cancelToken;
        asyncTask.triggerTime = _timerQueue.GetTriggerTime(delayTime);
        asyncTask.gatingFrame = _timerQueue.GetGatingFrame(delayFrame);
        //
        asyncTask.BindCoroutine(coroutine);
        _timerQueue.Schedule(asyncTask);
        return promise.VoidFuture;
    }

    /// <summary>
    /// 等待外部信号（低频指令）
    /// (如果T是值类型，传输结果的时候会产生装箱)
    /// </summary>
    internal ValueFuture<T> Await<T>(long coroutineId, ValueFuture<T> future, double timeout) {
        CheckEventLoop();
        if (!_coroutineDic.TryGetValue(coroutineId, out Coroutine coroutine)) {
            throw new InvalidOperationException("coroutine disposed");
        }
        if (coroutine.asyncTask != null) {
            throw new InvalidOperationException("There are pending asynchronous operations");
        }
        if (future.IsCompleted) return future;
        if (timeout <= 0) timeout = 365 * DatetimeUtil.SecondsPerDay;
        //
        ValuePromise<object> promise = ValuePromise<object>.Acquire(out int rid, _eventLoop);
        PromiseTask asyncTask = AddCanceller(promise, rid, timeout, coroutine.cancelToken);
        asyncTask.BindCoroutine(coroutine);
        // 代替用户等待目标任务完成
        Await(this, future.Box(requireResult: true), asyncTask).Forget();
        return ValueFuture<T>.UnsafeCreate(promise, rid);
    }

    /// <summary>
    /// 等待外部信号（低频指令）
    /// </summary>
    internal ValueFuture Await(long coroutineId, ValueFuture future, double timeout) {
        CheckEventLoop();
        if (!_coroutineDic.TryGetValue(coroutineId, out Coroutine coroutine)) {
            throw new InvalidOperationException("coroutine disposed");
        }
        if (coroutine.asyncTask != null) {
            throw new InvalidOperationException("There are pending asynchronous operations");
        }
        if (future.IsCompleted) return future;
        if (timeout <= 0) timeout = 365 * DatetimeUtil.SecondsPerDay;
        //
        ValuePromise<object> promise = ValuePromise<object>.Acquire(out int rid, _eventLoop);
        PromiseTask asyncTask = AddCanceller(promise, rid, timeout, coroutine.cancelToken);
        asyncTask.BindCoroutine(coroutine);
        // 代替用户等待目标任务完成 
        Await(this, future, asyncTask).Forget();
        return promise.VoidFuture;
    }

    [PooledTask]
    private static async ValueFuture Await(CoroutineMgr coroutineMgr, ValueFuture future, PromiseTask timeoutTask) {
        const int options = TaskOptions.REQUIRE_RESULT | TaskOptions.SUPPRESS_ALL_THROW | TaskOptions.STAGE_TRY_INLINE;
        //
        long taskId = timeoutTask.id;
        TaskResult taskResult = await future.GetAwaitable2(coroutineMgr.EventLoop, options);
        if (timeoutTask.id != taskId) {
            return; // Task已完成或被回收
        }
        if (taskResult.IsSucceeded) {
            timeoutTask.TrySetResult(taskResult.Result);
        } else if (taskResult.IsFailed) {
            timeoutTask.TrySetException(taskResult.Exception!);
        } else {
            timeoutTask.TrySetException(taskResult.ExceptionDispatchInfo!);
        }
        // 取消超时任务
        coroutineMgr._timerQueue.Cancel(taskId);
    }

    private PromiseTask AddCanceller<T>(ValuePromise<T> promise, int rid, double timeout, CancellationToken cancelToken = default) {
        Debug.Assert(timeout > 0);
        PromiseTask asyncTask = PromiseTask.Acquire();
        asyncTask.id = PromiseTask.NextId();
        asyncTask.TaskType = TaskBuilder.TYPE_CANCELLER;
        asyncTask.promise = promise;
        asyncTask.promiseRid = rid;
        asyncTask.triggerTime = _timerQueue.GetTriggerTime(timeout);
        asyncTask.cancelToken = cancelToken;
        _timerQueue.Schedule(asyncTask);
        return asyncTask;
    }

    #endregion

    #region internal

    private enum Status
    {
        Unstarted = 0,
        Running = 1,
        ShuttingDown = 2,
        Shutdown = 3,
    }

    #endregion
}
}