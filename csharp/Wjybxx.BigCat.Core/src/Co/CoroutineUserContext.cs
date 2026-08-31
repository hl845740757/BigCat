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
using Wjybxx.BigCat.Util;
using Wjybxx.Commons.Concurrent;

namespace Wjybxx.BigCat.Co
{
/// <summary>
/// 协程用户上下文(装箱类型)
///
/// 注意：
/// 1.必须调用<see cref="Dispose"/>方法，否则会导致内存泄漏。
/// 2.用户如果不需要追踪协程信息，可启动协程后就Dispose。
/// 3.改为总是通过ID发起协程命令，可有效防止内存泄漏，提高安全性。
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct CoroutineUserContext : IDisposable
{
    private readonly CoroutineMgr _coroutineMgr;
    private readonly long _coroutineId;
    private readonly CancellationToken _cancelToken;
    private readonly object _userData;

    internal CoroutineUserContext(CoroutineMgr coroutineMgr, long coroutineId, CancellationToken cancelToken, object userData) {
        this._coroutineMgr = coroutineMgr;
        this._coroutineId = coroutineId;
        this._cancelToken = cancelToken;
        this._userData = userData;
    }

    #region context

    /// <summary>
    /// 协程关联的事件循环
    /// 注：可通过<code>await EventLoop</code>切换到事件循环线程。
    /// </summary>
    public IEventLoop EventLoop => _coroutineMgr.EventLoop;
    /// <summary>
    /// 协程ID
    /// </summary>
    public long CoroutineId => _coroutineId;
    /// <summary>
    /// 协程关联的取消令牌
    /// </summary>
    public CancellationToken CancelToken => _cancelToken;
    /// <summary>
    /// 用户自定义数据
    /// 注：可以将取消令牌存储在这里，以方便发起取消。
    /// </summary>
    public object UserData => _userData;

    /// <summary>
    /// 取消协程的执行
    /// 注：如果协程当前正在协程管理器上等待某个任务完成，则可以中断等待，从而立即响应取消信号。
    /// </summary>
    /// <param name="interruptIfRunning">是否中断协程</param>
    public void Cancel(bool interruptIfRunning = false) {
        _coroutineMgr.Cancel(_coroutineId, interruptIfRunning);
    }

    /// <summary>
    /// 获取协程的执行结果
    /// 注：只可在协程退出后调用。
    /// </summary>
    /// <returns></returns>
    public TaskResult GetCoroutineResult() => _coroutineMgr.GetResult(_coroutineId);

    /// <summary>
    /// 协程是否已执行结束
    /// </summary>
    public bool IsTerminated => _coroutineMgr.IsTerminated(_coroutineId);

    /// <summary>
    /// 查询是否已销毁
    /// </summary>
    public bool IsDisposed => _coroutineMgr.IsUserContextDisposed(_coroutineId);

    /// <summary>
    /// 销毁用户上下文
    /// </summary>
    public void Dispose() => _coroutineMgr.DisposeUserContext(_coroutineId);

    /// <summary>
    /// 拆箱
    /// </summary>
    /// <param name="cmdCodec">命令编解码器</param>
    /// <param name="resultCodec">结果编解码器</param>
    /// <typeparam name="T">输入类型</typeparam>
    /// <typeparam name="R">输出类型</typeparam>
    /// <returns></returns>
    public CoroutineUserContext<T, R> Unbox<T, R>(DataKey<T> cmdCodec, DataKey<R> resultCodec) {
        return new CoroutineUserContext<T, R>(_coroutineMgr, _coroutineId, _cancelToken, _userData, cmdCodec, resultCodec);
    }

    #endregion
}

/// <summary>
/// 协程用户上下文
///
/// 注意：
/// 1.必须调用<see cref="Dispose"/>方法，否则会导致内存泄漏。
/// 2.用户如果不需要追踪协程信息，可启动协程后就Dispose。
/// </summary>
public readonly struct CoroutineUserContext<T, R> : IDisposable
{
    private readonly CoroutineMgr _coroutineMgr;
    private readonly long _coroutineId;
    private readonly CancellationToken _cancelToken;
    private readonly object _userData;
    private readonly DataKey<T> _cmdCodec;
    private readonly DataKey<R> _resultCodec;

    internal CoroutineUserContext(CoroutineMgr coroutineMgr, long coroutineId, CancellationToken cancelToken, object userData,
                                  DataKey<T> cmdCodec, DataKey<R> resultCodec) {
        this._coroutineMgr = coroutineMgr;
        this._coroutineId = coroutineId;
        this._cancelToken = cancelToken;
        this._userData = userData;
        this._cmdCodec = cmdCodec ?? throw new ArgumentNullException(nameof(cmdCodec));
        this._resultCodec = resultCodec ?? throw new ArgumentNullException(nameof(resultCodec));
    }

    #region context

    public long CoroutineId => _coroutineId;
    public object UserData => _userData;
    /// <summary>
    /// 协程关联的事件循环
    ///
    /// 注：可通过<code>await EventLoop</code>切换到事件循环线程。
    /// </summary>
    public IEventLoop EventLoop => _coroutineMgr.EventLoop;

    /// <summary>
    /// 取消协程的执行
    /// 注：如果协程当前正在协程管理器上等待某个任务完成，则可以中断等待，从而立即响应取消信号。
    /// </summary>
    /// <param name="interruptIfRunning">是否中断协程</param>
    public void Cancel(bool interruptIfRunning = false) {
        _coroutineMgr.Cancel(_coroutineId, interruptIfRunning);
    }

    /// <summary>
    /// 获取协程的执行结果
    /// 注：只可在协程退出后调用。
    /// </summary>
    /// <returns></returns>
    public TaskResult GetCoroutineResult() => _coroutineMgr.GetResult(_coroutineId);

    /// <summary>
    /// 协程是否已执行结束
    /// </summary>
    public bool IsTerminated => _coroutineMgr.IsTerminated(_coroutineId);

    /// <summary>
    /// 查询是否已销毁
    /// </summary>
    public bool IsDisposed => _coroutineMgr.IsUserContextDisposed(_coroutineId);

    /// <summary>
    /// 销毁用户上下文
    /// </summary>
    public void Dispose() => _coroutineMgr.DisposeUserContext(_coroutineId);

    /// <summary>
    /// 装箱
    /// </summary>
    /// <returns></returns>
    public CoroutineUserContext Box() {
        return new CoroutineUserContext(_coroutineMgr, _coroutineId, _cancelToken, _userData);
    }

    #endregion

    #region channel

    /// <summary>
    /// 尝试读取一个结果
    /// </summary>
    public bool TryRead(out R result) {
        return _coroutineMgr.TryReadResult(_coroutineId, _resultCodec, out result);
    }

    
    /// <summary>
    /// 异步读取一个结果
    ///
    /// 1.如果当前有可用结果，则立即返回。
    /// 2.如果当前无可用结果，则在任务写入结果或协程退出时醒来。
    /// </summary>
    /// <returns></returns>
    public ValueFuture<R> ReadAsync(double timeout = 0) {
        return _coroutineMgr.ReadResultAsync(_coroutineId, _resultCodec, timeout);
    }
    
    /// <summary>
    /// 异步读取一个结果（压制异步结果的异常抛出，性能更好）
    ///
    /// 1.如果当前有可用结果，则立即返回。
    /// 2.如果当前无可用结果，则在任务写入结果或协程退出时醒来，必须显式检测结果的有效性。
    /// </summary>
    /// <returns></returns>
    public ValueFuture<TaskResult<R>> ReadAsync2(double timeout = 0) {
        return _coroutineMgr.ReadResultAsync2(_coroutineId, _resultCodec, timeout);
    }

    /// <summary>
    /// 向任务写入一个输入
    /// </summary>
    /// <param name="cmd">命令</param>
    public void Write(T cmd) {
        _coroutineMgr.WriteCmd(_coroutineId, _cmdCodec, cmd);
    }

    #endregion
}
}