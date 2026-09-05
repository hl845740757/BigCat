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
using System.Runtime.InteropServices;
using System.Threading;
using Wjybxx.BigCat.Gameplay;
using Wjybxx.BigCat.Util;
using Wjybxx.Commons.Concurrent;

namespace Wjybxx.BigCat.Co
{
/// <summary>
/// 协程任务上下文
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct CoroutineTaskContext
{
    private readonly CoroutineMgr _coroutineMgr;
    private readonly long _coroutineId;
    private readonly CancellationToken _cancelToken;
    private readonly object _startArg;

    internal CoroutineTaskContext(CoroutineMgr coroutineMgr, long coroutineId,
                                  CancellationToken cancelToken, object startArg) {
        this._coroutineMgr = coroutineMgr;
        this._coroutineId = coroutineId;
        this._cancelToken = cancelToken;
        this._startArg = startArg;
    }

    #region context

    public long CoroutineId => _coroutineId;
    public CancellationToken CancelToken => _cancelToken;
    public object StartArg => _startArg;

    /// <summary>
    /// 协程关联的事件循环
    ///
    /// 注：可通过<code>await EventLoop</code>切换到事件循环线程。
    /// </summary>
    public IEventLoop EventLoop => _coroutineMgr.EventLoop;
    /// <summary>
    /// 协程关联的时间轴
    /// </summary>
    public ITime Time => _coroutineMgr.Time;
    /// <summary>
    /// 是否已收到取消信号
    /// </summary>
    public bool IsCancellationRequested => _cancelToken.IsCancellationRequested;

    /// <summary>
    /// 拆箱上下文
    /// </summary>
    public CoroutineTaskContext<In, Out> Unbox<In, Out>(DataKey<In> inputCodec, DataKey<Out> outputCodec) {
        return new CoroutineTaskContext<In, Out>(_coroutineMgr, _coroutineId, _cancelToken, _startArg,
            inputCodec, outputCodec);
    }

    #endregion

    #region 协程命令

    /// <summary>
    /// 睡眠N秒
    /// 
    /// 注：
    /// 1.睡眠时间为0合法，默认在下一帧update的时候醒来。
    /// 2.如果时间和额外延迟帧数都为0，且当前帧尚未执行到目标阶段，则会在当前帧醒来。
    /// 3.额外延迟帧用于强制Sleep0下一帧才醒来。
    /// </summary>
    /// <param name="time">睡眠时间，秒</param>
    /// <param name="delayFrame">额外延迟帧</param>
    /// <returns></returns>
    public ValueFuture Sleep(double time, int delayFrame = 0) {
        return _coroutineMgr.Sleep(_coroutineId, time, delayFrame);
    }

    /// <summary>
    /// 等待目标Future进入完成状态
    /// 
    /// 注：
    /// 1.返回的future固定在事件循环线程通知。
    /// 2.返回的Future会在协程被关闭时进入取消状态。
    /// </summary>
    /// <param name="future">要等待的future</param>
    /// <param name="timeout">超时时间</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public ValueFuture<T> Await<T>(ValueFuture<T> future, double timeout = 0) {
        return _coroutineMgr.Await(_coroutineId, future, timeout);
    }

    /// <summary>
    /// 等待目标Future进入完成状态
    /// 
    /// 注：
    /// 1.返回的future固定在事件循环线程通知。
    /// 2.返回的Future会在协程被关闭时进入取消状态。
    /// </summary>
    /// <param name="future">要等待的future</param>
    /// <param name="timeout">超时时间</param>
    /// <returns></returns>
    public ValueFuture Await(ValueFuture future, double timeout = 0) {
        return _coroutineMgr.Await(_coroutineId, future, timeout);
    }

    #endregion
}

/// <summary>
/// 协程任务上下文
/// </summary>
public readonly struct CoroutineTaskContext<In, Out>
{
    private readonly CoroutineMgr _coroutineMgr;
    private readonly long _coroutineId;
    private readonly CancellationToken _cancelToken;
    private readonly object _startArg;
    private readonly DataKey<In> _inputCodec;
    private readonly DataKey<Out> _outputCodec;

    internal CoroutineTaskContext(CoroutineMgr coroutineMgr, long coroutineId, CancellationToken cancelToken,
                                  object startArg, DataKey<In> inputCodec, DataKey<Out> outputCodec) {
        this._coroutineMgr = coroutineMgr;
        this._coroutineId = coroutineId;
        this._cancelToken = cancelToken;
        this._startArg = startArg;
        this._inputCodec = inputCodec;
        this._outputCodec = outputCodec;
    }

    #region context

    public long CoroutineId => _coroutineId;
    public CancellationToken CancelToken => _cancelToken;
    public object StartArg => _startArg;

    /// <summary>
    /// 协程关联的事件循环
    ///
    /// 注：
    /// 1.可通过<code>await EventLoop</code>切换到事件循环线程。
    /// 2.可跨线程访问。
    /// </summary>
    public IEventLoop EventLoop => _coroutineMgr.EventLoop;
    /// <summary>
    /// 协程关联的时间轴
    /// </summary>
    public ITime Time => _coroutineMgr.Time;

    /// <summary>
    /// 上下文装箱
    /// </summary>
    /// <returns></returns>
    public CoroutineTaskContext Box() {
        return new CoroutineTaskContext(_coroutineMgr, _coroutineId, _cancelToken, _startArg);
    }

    #endregion

    #region channel

    /// <summary>
    /// 尝试读取一个用户输入
    /// </summary>
    public bool TryRead(out In cmd) {
        return _coroutineMgr.TryReadCmd(_coroutineId, _inputCodec, out cmd);
    }

    /// <summary>
    /// 异步读取一个用户输入
    /// 
    /// 1.如果当前有可用输入，则立即返回。
    /// 2.如果当前无可用输入，则在用户写入输入或协程被中断醒来。
    /// </summary>
    /// <param name="timeout">超时时间</param>
    /// <returns></returns>
    public ValueFuture<In> ReadAsync(double timeout = 0) {
        return _coroutineMgr.ReadCmdAsync(_coroutineId, _inputCodec, timeout);
    }

    /// <summary>
    /// 异步读取一个用户输入（压制异步结果的异常抛出，性能更好）
    /// 
    /// 1.如果当前有可用输入，则立即返回。
    /// 2.如果当前无可用输入，则在用户写入输入或协程被中断醒来，必须显式检测结果的有效性。
    /// </summary>
    /// <param name="timeout">超时时间</param>
    /// <returns></returns>
    public ValueFuture<TaskResult<In>> ReadAsync2(double timeout = 0) {
        return _coroutineMgr.ReadCmdAsync2(_coroutineId, _inputCodec, timeout);
    }

    /// <summary>
    /// 上报一个结果
    /// </summary>
    /// <param name="result"></param>
    public void Write(Out result) {
        _coroutineMgr.WriteResult(_coroutineId, _outputCodec, result);
    }

    #endregion

    #region 协程命令

    /// <summary>
    /// 睡眠N秒
    /// 
    /// 注：
    /// 1.睡眠时间为0合法，默认在下一帧update的时候醒来。
    /// 2.如果时间和额外延迟帧数都为0，且当前帧尚未执行到目标阶段，则会在当前帧醒来。
    /// 3.额外延迟帧用于强制Sleep0下一帧才醒来。
    /// </summary>
    /// <param name="time">睡眠时间，秒</param>
    /// <param name="delayFrame">额外延迟帧</param>
    /// <returns></returns>
    public ValueFuture Sleep(double time, int delayFrame = 0) {
        return _coroutineMgr.Sleep(_coroutineId, time, delayFrame);
    }

    /// <summary>
    /// 等待目标Future进入完成状态
    /// 
    /// 注：
    /// 1.返回的future固定在事件循环线程通知。
    /// 2.返回的Future会在协程被关闭时进入失败状态（被中断）。
    /// </summary>
    /// <param name="future">要等待的future</param>
    /// <param name="timeout">超时时间</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public ValueFuture<T> Await<T>(ValueFuture<T> future, double timeout = 0) {
        return _coroutineMgr.Await(_coroutineId, future, timeout);
    }

    /// <summary>
    /// 等待目标Future进入完成状态
    /// 
    /// 注：
    /// 1.返回的future固定在事件循环线程通知。
    /// 2.返回的Future会在协程被关闭时进入失败状态（被中断）。
    /// </summary>
    /// <param name="future">要等待的future</param>
    /// <param name="timeout">超时时间</param>
    /// <returns></returns>
    public ValueFuture Await(ValueFuture future, double timeout = 0) {
        return _coroutineMgr.Await(_coroutineId, future, timeout);
    }

    #endregion
}
}