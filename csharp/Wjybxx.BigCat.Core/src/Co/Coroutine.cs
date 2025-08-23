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
using System.Runtime.ExceptionServices;
using Wjybxx.BigCat.Gameplay;
using Wjybxx.BigCat.Util;
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Concurrent;
using static Wjybxx.BigCat.Co.CoroutineMgr;

namespace Wjybxx.BigCat.Co
{
/// <summary>
/// 协程抽象
///
/// 1.协程对象在复用时会更新实例id，表示已被重用
/// 2.当输入输出类型都是引用类型时，全部转为object
/// </summary>
internal sealed class Coroutine
{
    // private static readonly ILogger logger = LoggerFactory.GetLogger(typeof(Coroutine));
    public static readonly DataKey<object> CODEC_INPUT = DataKeys.NewObjectKey("co_input");
    public static readonly DataKey<object> CODEC_OUTPUT = DataKeys.NewObjectKey("co_output");

    private const int MASK_CANCEL_REQUESTED = 0x01; // 已收到取消信号
    private const int MASK_INTERRUPTED = 0x02; // 已收到中断请求
    private const int MASK_TERMINATED = 0x04; // 已执行结束
    private const int MASK_USER_CONTEXT_DISPOSED = 0x08; // 用户上下文已销毁

#nullable disable
    /// <summary>
    /// 协程id
    /// </summary>
    public long id = -1;
    /// <summary>
    /// 关联的协程管理器
    /// (避免暴露给用户，否则可能导致封装泄漏)
    /// </summary>
    public CoroutineMgr coroutineMgr;
    /// <summary>
    /// 辅助控制标识
    /// </summary>
    private int ctl;

    /// <summary>
    /// 用户命令缓冲区
    /// 注：只能在事件循环线程下访问。
    /// </summary>
    private readonly Queue<UnionValue> _cmdBuffer = new Queue<UnionValue>();
    /// <summary>
    /// 任务结果缓冲区
    /// 注：只能在事件循环线程下访问。
    /// </summary>
    private readonly Queue<UnionValue> _resultBuffer = new Queue<UnionValue>();

    /// <summary>
    /// 协程自身的执行结果
    /// </summary>
    internal ValueFuture coResult;
    /// <summary>
    /// 关联的异步任务
    ///
    /// 注：为了避免额外的查询，固存储在协程对象上，由协程管理器维护。
    /// </summary>
    internal PromiseTask asyncTask;
    /// <summary>
    /// 协程任务读取用户命令的Promise
    ///
    /// 注：有三处设置结果的地方：用户写入命令、超时取消、协程被中断。
    /// </summary>
    private ValuePromise<int>? _cmdReaderPromise;
    /// <summary>
    /// 用户读取任务结果的Promise
    ///
    /// 注：有三处设置结果的地方：协程返回结果、超时取消、协程结束。
    /// </summary>
    private ValuePromise<int>? _resultReaderPromise;
    private int _cmdReaderPromiseRid;
    private int _resultReaderPromiseRid;
#nullable restore

    public Coroutine() {
    }

    public void Reset() {
        id = -1;
        coroutineMgr = null;
        ctl = 0;
        _cmdBuffer.Clear();
        _resultBuffer.Clear();

        coResult = default;
        asyncTask = null;
        _cmdReaderPromise = null;
        _resultReaderPromise = null;
        _cmdReaderPromiseRid = 0;
        _resultReaderPromiseRid = 0;
    }

    #region 上下文

    /// <summary>
    /// 协程是否已收到取消信号
    /// </summary>
    private bool IsCancelRequested {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (ctl & MASK_CANCEL_REQUESTED) != 0;
    }

    /// <summary>
    /// 查询是否已收到取消信号
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public bool GetCancelRequested(long token) {
        CheckTokenAndEventLoop(token);
        return IsCancelRequested;
    }

    /// <summary>
    /// 请求取消协程任务
    /// </summary>
    public void Cancel(long token, bool interruptIfRunning) {
        CheckTokenAndEventLoop(token);
        ctl |= MASK_CANCEL_REQUESTED;
        if (interruptIfRunning) {
            InterruptTask();
        }
    }

    /// <summary>
    /// 用户上下文是否已销毁
    /// </summary>
    public bool IsUserContextDisposed => (ctl & MASK_USER_CONTEXT_DISPOSED) != 0;

    /// <summary>
    /// 销毁用户上下文
    /// </summary>
    /// <param name="token"></param>
    public void DisposeUserContext(long token) {
        CheckEventLoop();
        if (IsDisposed(token)) { // 允许反复调用
            return;
        }
        if (IsUserContextDisposed) {
            return;
        }
        ctl |= MASK_USER_CONTEXT_DISPOSED;
        coroutineMgr.OnUserContextDisposed(this);
    }

    /// <summary>
    /// 中断协程
    /// </summary>
    internal void InterruptTask() {
        ctl |= MASK_INTERRUPTED;
        PromiseTask asyncTask = this.asyncTask;
        if (asyncTask != null) {
            coroutineMgr.CancelTimer(asyncTask, CancelCodes.REASON_INTERRUPTED);
        }
        ValuePromise<int> cmdPromise = _cmdReaderPromise;
        if (cmdPromise != null) {
            cmdPromise.TrySetCancelled(_cmdReaderPromiseRid, CancelCodes.REASON_INTERRUPTED);
        }
    }

    /// <summary>
    /// 中断用户
    /// </summary>
    internal void InterruptUser() {
        ValuePromise<int> resultPromise = _resultReaderPromise;
        if (resultPromise != null) {
            resultPromise.TrySetCancelled(_resultReaderPromiseRid, CancelCodes.REASON_INTERRUPTED);
        }
    }

    /// <summary>
    /// 协程是否已执行结束
    /// </summary>
    public bool IsTerminated {
        get => (ctl & MASK_TERMINATED) != 0;
        set => SetCtlBit(MASK_TERMINATED, value);
    }

    /// <summary>
    /// 协程是否已执行结束
    /// </summary>
    public bool GetTerminated(long token) {
        CheckTokenAndEventLoop(token);
        return IsTerminated;
    }

    /// <summary>
    /// 获取协程的执行结果
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public TaskResult GetResult(long token) {
        CheckTokenAndEventLoop(token);
        if (!IsTerminated) {
            throw new Exception("Coroutine is not terminated.");
        }
        return coResult.GetAwaiter(SuppressedTypes.All).GetResult();
    }

    /// <summary>
    /// 协程是否已被回收
    ///
    /// 注：只能在事件循环线程调用，跨线程调用结果不准确。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsDisposed(long token) {
        Debug.Assert(token != -1);
        return token != this.id;
    }

    #endregion

    #region channel

    public void WriteCmd<T>(long token, DataKey<T> codec, T value) {
        CheckTokenAndEventLoop(token);
        UnionValue unionValue = (ObjectUtil.IsNullableType<T>() && value == null) ? UnionValue.Null : codec.Box(value);
        _cmdBuffer.Enqueue(unionValue);
        // 唤醒Task
        ValuePromise<int> promise = _cmdReaderPromise;
        if (promise != null) {
            promise.TrySetResult(_cmdReaderPromiseRid, 0);
        }
    }

    public bool TryReadCmd<T>(long token, DataKey<T> codec, out T value) {
        CheckTokenAndEventLoop(token);
        if (_cmdBuffer.TryDequeue(out UnionValue unionValue)) {
            value = unionValue.IsNull ? default : codec.Unbox(unionValue);
            return true;
        }
        value = default;
        return false;
    }

    public async ValueFuture<TaskResult<T>> ReadCmdAsync<T>(long token, DataKey<T> codec, double timeout,
                                                            TimingType timingType, GameLoopPhase phase) {
        CheckTokenAndEventLoop(token);
        if (_cmdBuffer.TryDequeue(out UnionValue unionValue)) {
            T value = unionValue.IsNull ? default : codec.Unbox(unionValue);
            return TaskResult<T>.FromResult(value);
        }
        if (IsCancelRequested) { // 用户已取消任务
            return TaskResult<T>.FromCancelled(CancelCodes.REASON_INTERRUPTED);
        }
        if (_cmdReaderPromise != null) {
            throw new InvalidOperationException("read cmd task already exists");
        }
        // 等待被唤醒
        ValuePromise<int> promise = ValuePromise<int>.Acquire(out int rid, coroutineMgr.EventLoop);
        _cmdReaderPromise = promise;
        _cmdReaderPromiseRid = rid;
        if (timeout > 0) {
            AddCanceller(promise, rid, timeout, timingType, phase);
        }
        TaskResult<int> result = await promise.Future.GetAwaitable(SuppressedTypes.All);
        // 醒来先清理字段，因此它处无需校验promise字段的有效性
        Debug.Assert(_cmdReaderPromise == promise);
        _cmdReaderPromise = null;
        _cmdReaderPromiseRid = 0;
        //
        if (result.IsSucceeded) {
            unionValue = _cmdBuffer.Dequeue();
            T value = unionValue.IsNull ? default : codec.Unbox(unionValue);
            return TaskResult<T>.FromResult(value);
        }
        return result.CastFailed<T>(); // 避免处理异常
    }

    public void WriteResult<T>(long token, DataKey<T> codec, T value) {
        CheckTokenAndEventLoop(token);
        UnionValue unionValue = (ObjectUtil.IsNullableType<T>() && value == null) ? UnionValue.Null : codec.Box(value);
        _resultBuffer.Enqueue(unionValue);
        // 唤醒用户
        ValuePromise<int> promise = _resultReaderPromise;
        if (promise != null) {
            promise.TrySetResult(_resultReaderPromiseRid, 0);
        }
    }

    public bool TryReadResult<T>(long token, DataKey<T> codec, out T value) {
        CheckTokenAndEventLoop(token);
        if (_resultBuffer.TryDequeue(out UnionValue unionValue)) {
            value = unionValue.IsNull ? default : codec.Unbox(unionValue);
            return true;
        }
        value = default;
        return false;
    }

    public async ValueFuture<TaskResult<T>> ReadResultAsync<T>(long token, DataKey<T> codec, double timeout,
                                                               TimingType timingType, GameLoopPhase phase) {
        CheckTokenAndEventLoop(token);
        if (_resultBuffer.TryDequeue(out UnionValue unionValue)) {
            T value = unionValue.IsNull ? default : codec.Unbox(unionValue);
            return TaskResult<T>.FromResult(value);
        }
        if (IsTerminated) { // 协程已终止
            return TaskResult<T>.FromCancelled(CancelCodes.REASON_INTERRUPTED);
        }
        if (_resultReaderPromise != null) {
            throw new InvalidOperationException("read result task already exists");
        }
        // 等待被唤醒
        ValuePromise<int> promise = ValuePromise<int>.Acquire(out int rid, coroutineMgr.EventLoop);
        _resultReaderPromise = promise;
        _resultReaderPromiseRid = rid;
        if (timeout > 0) {
            AddCanceller(promise, rid, timeout, timingType, phase);
        }
        TaskResult<int> result = await promise.Future.GetAwaitable(SuppressedTypes.All);
        // 醒来先清理字段，因此它处无需校验promise字段的有效性
        Debug.Assert(_resultReaderPromise == promise);
        _resultReaderPromise = null;
        _resultReaderPromiseRid = 0;
        //
        if (result.IsSucceeded) {
            unionValue = _resultBuffer.Dequeue();
            T value = unionValue.IsNull ? default : codec.Unbox(unionValue);
            return TaskResult<T>.FromResult(value);
        }
        return result.CastFailed<T>(); // 避免处理异常
    }

    private void AddCanceller(ValuePromise<int> promise, int promiseRid, double timeout,
                              TimingType timingType, GameLoopPhase phase) {
        PromiseTask asyncTask = taskPool.Acquire();
        asyncTask.id = NextId();
        asyncTask.TaskType = TaskBuilder.TYPE_AWAIT;
        asyncTask.promise = promise;
        asyncTask.promiseRid = promiseRid;
        asyncTask.triggerTime = coroutineMgr.GetTime(timingType, phase) + Math.Max(0, timeout);
        // 不是协程绑定任务
        // Bind(this, asyncTask);
        coroutineMgr.AddTimer(asyncTask, timingType, phase);
    }

    #endregion

    #region 协程指令

    public ValueFuture Sleep(long token, double delayTime, int delayFrame, TimingType timingType, GameLoopPhase phase) {
        CheckTokenAndEventLoop(token);
        coroutineMgr.CheckQueue(timingType, phase);
        if (this.asyncTask != null) {
            throw new InvalidOperationException("There are pending asynchronous operations");
        }
        if (delayTime <= 0) delayTime = 0;
        if (delayFrame < 0) delayFrame = 0;

        ValuePromise<int> promise = ValuePromise<int>.Acquire(out int rid, coroutineMgr.EventLoop);
        PromiseTask asyncTask = taskPool.Acquire();
        asyncTask.id = NextId();
        asyncTask.TaskType = TaskBuilder.TYPE_EMPTY;
        asyncTask.promise = promise;
        asyncTask.promiseRid = rid;
        asyncTask.triggerTime = coroutineMgr.GetTime(timingType, phase) + delayTime;
        if (timingType != TimingType.FrameCount) {
            asyncTask.gatingFrame = coroutineMgr.GetFrameCount(phase) + delayFrame;
        }
        Bind(this, asyncTask);
        coroutineMgr.AddTimer(asyncTask, timingType, phase);
        return promise.VoidFuture;
    }

    public ValueFuture<T> Await<T>(long token, ValueFuture<T> future, double timeout, TimingType timingType, GameLoopPhase phase) {
        CheckTokenAndEventLoop(token);
        coroutineMgr.CheckQueue(timingType, phase);
        if (this.asyncTask != null) {
            throw new InvalidOperationException("There are pending asynchronous operations");
        }
        if (future.IsCompleted) {
            return future;
        }
        if (timeout <= 0) timeout = 365 * DatetimeUtil.SecondsPerDay;

        ValuePromise<T> promise = ValuePromise<T>.Acquire(out int rid, coroutineMgr.EventLoop);
        PromiseTask asyncTask = taskPool.Acquire();
        asyncTask.id = NextId();
        asyncTask.TaskType = TaskBuilder.TYPE_AWAIT;
        asyncTask.promise = promise;
        asyncTask.promiseRid = rid;
        asyncTask.triggerTime = coroutineMgr.GetTime(timingType, phase) + timeout;
        //
        Bind(this, asyncTask);
        coroutineMgr.AddTimer(asyncTask, timingType, phase);
        // 代替用户等待目标任务完成 
        Await(coroutineMgr, future, asyncTask).Forget();
        return promise.Future;
    }

    public ValueFuture Await(long token, ValueFuture future, double timeout, TimingType timingType, GameLoopPhase phase) {
        CheckTokenAndEventLoop(token);
        coroutineMgr.CheckQueue(timingType, phase);
        if (this.asyncTask != null) {
            throw new InvalidOperationException("There are pending asynchronous operations");
        }
        if (future.IsCompleted) {
            return future;
        }
        if (timeout <= 0) timeout = 365 * DatetimeUtil.SecondsPerDay;

        ValuePromise<int> promise = ValuePromise<int>.Acquire(out int rid, coroutineMgr.EventLoop);
        PromiseTask asyncTask = taskPool.Acquire();
        asyncTask.id = NextId();
        asyncTask.TaskType = TaskBuilder.TYPE_AWAIT;
        asyncTask.promise = promise;
        asyncTask.promiseRid = rid;
        asyncTask.triggerTime = coroutineMgr.GetTime(timingType, phase) + timeout;
        //
        Bind(this, asyncTask);
        coroutineMgr.AddTimer(asyncTask, timingType, phase);
        // 代替用户等待目标任务完成 
        Await(coroutineMgr, future, asyncTask).Forget();
        return promise.VoidFuture;
    }

    private static void Bind(Coroutine coroutine, PromiseTask asyncTask) {
        asyncTask.IsCoroutineTask = true;
        asyncTask.invoker = coroutine;
        coroutine.asyncTask = asyncTask;
    }

    // 这里生成的状态机会进入对象池...其实不是很想
    private static async ValueFuture Await<T>(CoroutineMgr coroutineMgr, ValueFuture<T> future, PromiseTask timeoutTask) {
        long taskId = timeoutTask.id;
        TaskResult<T> taskResult = await future.GetAwaitable(coroutineMgr.EventLoop, SuppressedTypes.All, TaskOptions.STAGE_TRY_INLINE);
        if (timeoutTask.id != taskId) {
            return; // Task已完成或被回收
        }
        if (taskResult.IsSucceeded) {
            timeoutTask.TrySetResult(taskResult.Result);
        } else if (taskResult.IsCancelled) { // 避免不必要的堆栈恢复
            timeoutTask.TrySetException(taskResult.Exception!);
        } else {
            timeoutTask.TrySetException(taskResult.ExceptionDispatchInfo!);
        }
    }

    private static async ValueFuture Await(CoroutineMgr coroutineMgr, ValueFuture future, PromiseTask timeoutTask) {
        long taskId = timeoutTask.id;
        TaskResult taskResult = await future.GetAwaitable(coroutineMgr.EventLoop, SuppressedTypes.All, TaskOptions.STAGE_TRY_INLINE);
        if (timeoutTask.id != taskId) {
            return; // Task已完成或被回收
        }
        if (taskResult.IsSucceeded) {
            timeoutTask.TrySetResult(0);
        } else if (taskResult.IsCancelled) { // 避免不必要的堆栈恢复
            timeoutTask.TrySetException(taskResult.Exception!);
        } else {
            timeoutTask.TrySetException(taskResult.ExceptionDispatchInfo!);
        }
    }

    #endregion

    #region internal

    private void CheckEventLoop() {
        CoroutineMgr coroutineMgr = this.coroutineMgr;
        if (coroutineMgr == null) {
            throw new InvalidOperationException("coroutine already disposed");
        }
        if (!coroutineMgr.EventLoop.InEventLoop()) {
            throw new GuardedOperationException("Method must be called from the eventLoop thread");
        }
    }

    private void CheckTokenAndEventLoop(long token) {
        CoroutineMgr coroutineMgr = this.coroutineMgr;
        if (coroutineMgr == null) {
            throw new InvalidOperationException("coroutine already disposed");
        }
        if (!coroutineMgr.EventLoop.InEventLoop()) {
            throw new GuardedOperationException("Method must be called from the eventLoop thread");
        }
        if (token != this.id) {
            throw new InvalidOperationException("coroutine already disposed");
        }
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
}
}