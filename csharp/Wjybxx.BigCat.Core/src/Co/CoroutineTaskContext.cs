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
using Wjybxx.Commons.Concurrent;

namespace Wjybxx.BigCat.Co
{
/// <summary>
/// 协程任务上下文
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct CoroutineTaskContext
{
    private readonly CoroutineMgr coroutineMgr;
    private readonly Coroutine coroutine;
    private readonly long token;
    private readonly long entityId;
    private readonly object startArg1;
    private readonly object startArg2;

    internal CoroutineTaskContext(CoroutineMgr coroutineMgr, Coroutine coroutine, long token,
                                  long entityId, object startArg1, object startArg2) {
        this.coroutineMgr = coroutineMgr;
        this.coroutine = coroutine;
        this.token = token;
        this.entityId = entityId;
        this.startArg1 = startArg1;
        this.startArg2 = startArg2;
    }

    #region context

    public long CoroutineId => token;
    public long EntityId => entityId;
    public object StartArg1 => startArg1;
    public object StartArg2 => startArg2;

    /// <summary>
    /// 协程关联的事件循环
    ///
    /// 注：
    /// 1.可通过<code>await EventLoop</code>切换到事件循环线程。
    /// 2.可跨线程访问。
    /// </summary>
    public IEventLoop EventLoop => coroutineMgr.EventLoop;
    /// <summary>
    /// 协程关联的时间轴
    /// </summary>
    public IReadonlyTime Time => coroutineMgr.Time;
    /// <summary>
    /// 是否已收到取消信号
    /// </summary>
    public bool IsCancelRequest => coroutine.GetCancelRequested(token);

    /// <summary>
    /// 拆箱上下文
    /// </summary>
    public CoroutineTaskContext<In, Out> Unbox<In, Out>(DataKey<In> inputCodec, DataKey<Out> outputCodec) {
        return new CoroutineTaskContext<In, Out>(coroutineMgr, coroutine, token,
            entityId, startArg1, startArg2, inputCodec, outputCodec);
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
    /// <param name="time">睡眠时间，秒 or 帧</param>
    /// <param name="extraDelayFrame">额外延迟帧</param>
    /// <param name="timingType">计时类型</param>
    /// <param name="phase">等待队列</param>
    /// <returns></returns>
    public ValueFuture Sleep(double time, int extraDelayFrame = 1, TimingType timingType = TimingType.Time,
                             GameLoopPhase phase = GameLoopPhase.Update) {
        return coroutine.Sleep(token, time, extraDelayFrame, timingType, phase);
    }

    /// <summary>
    /// 睡眠一定帧数
    ///
    /// 注：
    /// 1.如果睡眠帧数为0，且当前帧尚未执行到目标阶段，则会在当前帧醒来。
    /// 2.该方法其实是<see cref="Sleep"/>的快捷方法。
    /// </summary>
    /// <param name="frameCount">睡眠帧首</param>
    /// <param name="phase">等待队列</param>
    /// <returns></returns>
    public ValueFuture SleepFrame(int frameCount, GameLoopPhase phase = GameLoopPhase.Update) {
        return coroutine.Sleep(token, frameCount, 0, TimingType.FrameCount, phase);
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
    /// <param name="timingType">计时类型</param>
    /// <param name="phase">等待队列</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public ValueFuture<T> Await<T>(ValueFuture<T> future, double timeout = 0, TimingType timingType = TimingType.Time,
                                   GameLoopPhase phase = GameLoopPhase.Update) {
        return coroutine.Await(token, future, timeout, timingType, phase);
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
    /// <param name="timingType">计时类型</param>
    /// <param name="phase">等待队列</param>
    /// <returns></returns>
    public ValueFuture Await(ValueFuture future, double timeout = 0, TimingType timingType = TimingType.Time,
                             GameLoopPhase phase = GameLoopPhase.Update) {
        return coroutine.Await(token, future, timeout, timingType, phase);
    }

    #endregion
}

/// <summary>
/// 协程任务上下文
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct CoroutineTaskContext<In, Out>
{
    private readonly CoroutineMgr coroutineMgr;
    private readonly Coroutine coroutine;
    private readonly long token;
    private readonly long entityId;
    private readonly object startArg1;
    private readonly object startArg2;
    private readonly DataKey<In> inputCodec;
    private readonly DataKey<Out> outputCodec;

    internal CoroutineTaskContext(CoroutineMgr coroutineMgr, Coroutine coroutine, long token,
                                  long entityId, object startArg1, object startArg2,
                                  DataKey<In> inputCodec, DataKey<Out> outputCodec) {
        this.coroutineMgr = coroutineMgr;
        this.coroutine = coroutine;
        this.token = token;
        this.entityId = entityId;
        this.startArg1 = startArg1;
        this.startArg2 = startArg2;
        this.inputCodec = inputCodec;
        this.outputCodec = outputCodec;
    }

    #region context

    public long CoroutineId => token;
    public long EntityId => entityId;
    public object StartArg1 => startArg1;
    public object StartArg2 => startArg2;

    /// <summary>
    /// 协程关联的事件循环
    ///
    /// 注：
    /// 1.可通过<code>await EventLoop</code>切换到事件循环线程。
    /// 2.可跨线程访问。
    /// </summary>
    public IEventLoop EventLoop => coroutineMgr.EventLoop;
    /// <summary>
    /// 协程关联的时间轴
    /// </summary>
    public IReadonlyTime Time => coroutineMgr.Time;
    /// <summary>
    /// 是否已收到取消信号
    /// </summary>
    public bool IsCancelRequest => coroutine.GetCancelRequested(token);

    /// <summary>
    /// 上下文装箱
    /// </summary>
    /// <returns></returns>
    public CoroutineTaskContext Box() {
        return new CoroutineTaskContext(coroutineMgr, coroutine, token, entityId, startArg1, startArg2);
    }

    #endregion

    #region channel

    /// <summary>
    /// 尝试读取一个用户输入
    /// </summary>
    public bool TryRead(out In cmd) {
        return coroutine.TryReadCmd(token, inputCodec, out cmd);
    }

    /// <summary>
    /// 异步读取一个用户输入
    /// 
    /// 1.如果当前有可用输入，则立即返回。
    /// 2.如果当前无可用输入，则在用户写入输入或协程被中断（取消时）醒来，必须显式检测结果的有效性。
    /// </summary>
    /// <param name="timeout">超时时间</param>
    /// <param name="timingType">计时类型</param>
    /// <param name="phase">等待队列</param>
    /// <returns></returns>
    public ValueFuture<TaskResult<In>> ReadAsync(double timeout = 0, TimingType timingType = TimingType.Time,
                                                 GameLoopPhase phase = GameLoopPhase.Update) {
        return coroutine.ReadCmdAsync(token, inputCodec, timeout, timingType, phase);
    }

    /// <summary>
    /// 上报一个结果
    /// </summary>
    /// <param name="result"></param>
    public void Write(Out result) {
        coroutine.WriteResult(token, outputCodec, result);
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
    /// <param name="time">睡眠时间，秒 or 帧</param>
    /// <param name="extraDelayFrame">额外延迟帧</param>
    /// <param name="timingType">计时类型</param>
    /// <param name="phase">等待队列</param>
    /// <returns></returns>
    public ValueFuture Sleep(double time, int extraDelayFrame = 1, TimingType timingType = TimingType.Time,
                             GameLoopPhase phase = GameLoopPhase.Update) {
        return coroutine.Sleep(token, time, extraDelayFrame, timingType, phase);
    }

    /// <summary>
    /// 睡眠一定帧数
    ///
    /// 注：是<see cref="Sleep"/>的快捷方法。
    /// </summary>
    /// <param name="frameCount">睡眠帧首</param>
    /// <param name="phase">等待队列</param>
    /// <returns></returns>
    public ValueFuture SleepFrame(int frameCount, GameLoopPhase phase = GameLoopPhase.Update) {
        return coroutine.Sleep(token, frameCount, 0, TimingType.FrameCount, phase);
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
    /// <param name="timingType">计时类型</param>
    /// <param name="phase">等待队列</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public ValueFuture<T> Await<T>(ValueFuture<T> future, double timeout = 0, TimingType timingType = TimingType.Time,
                                   GameLoopPhase phase = GameLoopPhase.Update) {
        return coroutine.Await(token, future, timeout, timingType, phase);
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
    /// <param name="timingType">计时类型</param>
    /// <param name="phase">等待队列</param>
    /// <returns></returns>
    public ValueFuture Await(ValueFuture future, double timeout = 0, TimingType timingType = TimingType.Time,
                             GameLoopPhase phase = GameLoopPhase.Update) {
        return coroutine.Await(token, future, timeout, timingType, phase);
    }

    #endregion
}
}