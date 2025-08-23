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
using Wjybxx.BigCat.Gameplay;
using Wjybxx.BigCat.Util;
using Wjybxx.Commons;
using Wjybxx.Commons.Attributes;
using Wjybxx.Commons.Concurrent;

namespace Wjybxx.BigCat.Co
{
/// <summary>
/// 协程用户上下文(装箱类型)
///
/// 注意：
/// 1.必须调用<see cref="Dispose"/>方法，否则会导致内存泄漏。
/// 2.用户如果不需要追踪协程信息，可启动协程后就Dispose。
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct CoroutineUserContext : IDisposable
{
    private readonly CoroutineMgr coroutineMgr;
    private readonly Coroutine coroutine;
    private readonly long token;
    private readonly long entityId;

    internal CoroutineUserContext(CoroutineMgr coroutineMgr, Coroutine coroutine, long token, long entityId) {
        this.coroutineMgr = coroutineMgr;
        this.coroutine = coroutine;
        this.token = token;
        this.entityId = entityId;
    }

    #region context

    public long CoroutineId => token;
    public long EntityId => entityId;
    /// <summary>
    /// 协程关联的事件循环
    ///
    /// 注：可通过<code>await EventLoop</code>切换到事件循环线程。
    /// </summary>
    public IEventLoop EventLoop => coroutineMgr.EventLoop;

    /// <summary>
    /// 取消协程的执行
    ///
    /// 注：如果协程当前正在协程管理器上等待某个任务完成，则可以中断等待，从而立即响应取消信号。
    /// </summary>
    /// <param name="interruptIfRunning">是否中断协程</param>
    public void Cancel(bool interruptIfRunning = false) {
        coroutine.Cancel(token, interruptIfRunning);
    }

    /// <summary>
    /// 协程是否已执行结束
    /// </summary>
    public bool IsTerminated => coroutine.GetTerminated(token);

    /// <summary>
    /// 获取协程自身的执行结果
    ///
    /// 注：只可在协程退出后调用。
    /// </summary>
    /// <returns></returns>
    [Beta("有点不想提供该方法")]
    public TaskResult GetCoroutineResult() {
        return coroutine.GetResult(token);
    }

    /// <summary>
    /// 查询是否已销毁
    /// </summary>
    public bool IsDisposed => coroutine.IsDisposed(token);

    /// <summary>
    /// 销毁用户上下文
    /// </summary>
    public void Dispose() {
        coroutine.DisposeUserContext(token);
    }

    /// <summary>
    /// 拆箱
    /// </summary>
    /// <param name="cmdCodec">命令编解码器</param>
    /// <param name="resultCodec">结果编解码器</param>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="R"></typeparam>
    /// <returns></returns>
    public CoroutineUserContext<T, R> Unbox<T, R>(DataKey<T> cmdCodec, DataKey<R> resultCodec) {
        return new CoroutineUserContext<T, R>(coroutineMgr, coroutine, token, entityId, cmdCodec, resultCodec);
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
[StructLayout(LayoutKind.Auto)]
public readonly struct CoroutineUserContext<T, R> : IDisposable
{
    private readonly CoroutineMgr coroutineMgr;
    private readonly Coroutine coroutine;
    private readonly long token;
    private readonly long entityId;
    private readonly DataKey<T> cmdCodec;
    private readonly DataKey<R> resultCodec;

    internal CoroutineUserContext(CoroutineMgr coroutineMgr, Coroutine coroutine, long token, long entityId,
                                  DataKey<T> cmdCodec, DataKey<R> resultCodec) {
        this.coroutineMgr = coroutineMgr;
        this.coroutine = coroutine;
        this.token = token;
        this.entityId = entityId;
        this.cmdCodec = cmdCodec;
        this.resultCodec = resultCodec;
    }

    #region context

    public long CoroutineId => token;
    public long EntityId => entityId;
    /// <summary>
    /// 协程关联的事件循环
    ///
    /// 注：可通过<code>await EventLoop</code>切换到事件循环线程。
    /// </summary>
    public IEventLoop EventLoop => coroutineMgr.EventLoop;

    /// <summary>
    /// 取消协程的执行
    ///
    /// 注：如果协程当前正在协程管理器上等待某个任务完成，则可以中断等待，从而立即响应取消信号。
    /// </summary>
    /// <param name="interruptIfRunning">是否中断协程</param>
    public void Cancel(bool interruptIfRunning = false) {
        coroutine.Cancel(token, interruptIfRunning);
    }

    /// <summary>
    /// 协程是否已执行结束
    /// </summary>
    public bool IsTerminated => coroutine.GetTerminated(token);

    /// <summary>
    /// 获取协程自身的执行结果
    ///
    /// 注：只可在协程退出后调用。
    /// </summary>
    /// <returns></returns>
    [Beta("有点不想提供该方法")]
    public TaskResult GetCoroutineResult() {
        return coroutine.GetResult(token);
    }

    /// <summary>
    /// 查询是否已销毁
    /// </summary>
    public bool IsDisposed => coroutine.IsDisposed(token);

    /// <summary>
    /// 销毁用户上下文
    /// </summary>
    public void Dispose() {
        coroutine.DisposeUserContext(token);
    }

    /// <summary>
    /// 装箱
    /// </summary>
    /// <returns></returns>
    public CoroutineUserContext Box() {
        return new CoroutineUserContext(coroutineMgr, coroutine, token, entityId);
    }

    #endregion

    #region channel

    /// <summary>
    /// 尝试读取一个结果
    /// </summary>
    public bool TryRead(out R result) {
        return coroutine.TryReadResult(token, resultCodec, out result);
    }

    /// <summary>
    /// 异步读取一个结果
    ///
    /// 1.如果当前有可用结果，则立即返回。
    /// 2.如果当前无可用结果，则在任务写入结果或协程退出时醒来，必须显式检测结果的有效性。
    /// 3.如果取消码是<see cref="CancelCodes.REASON_INTERRUPTED"/>，则表示协程已退出。
    /// </summary>
    /// <returns></returns>
    public ValueFuture<TaskResult<R>> ReadAsync(double timeout = 0, TimingType timingType = TimingType.Time,
                                                GameLoopPhase phase = GameLoopPhase.Update) {
        return coroutine.ReadResultAsync(token, resultCodec, timeout, timingType, phase);
    }

    /// <summary>
    /// 向任务写入一个输入
    /// </summary>
    /// <param name="cmd">命令</param>
    public void Write(T cmd) {
        coroutine.WriteCmd(token, cmdCodec, cmd);
    }

    #endregion
}
}