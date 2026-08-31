#region LICENSE

// Copyright 2023-2024 wjybxx(845740757@qq.com)
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
using System.Threading;
using NUnit.Framework;
using Wjybxx.BigCat.Co;
using Wjybxx.BigCat.Gameplay;
using Wjybxx.BigCat.Util;
using Wjybxx.Commons;
using Wjybxx.Commons.Concurrent;
using Wjybxx.Disruptor;

namespace Wjybxx.BigCat.Tests.Co
{
/// <summary>
/// 协程测试
///
/// 测试模型：
/// 1.协程的交互接口都限制在事件循环线程，因此测试主体通过<see cref="RunFrames"/>提交为定时任务，整体在事件循环内执行。
/// 2.定时任务的每次触发即一个逻辑帧（推进时间轴 + 驱动协程调度），通过执行次数限制总帧数。
/// 3.为使断言可靠，测试不依赖真实时间，帧内的时间推进量固定为<see cref="FrameDelta"/>。
/// 4.帧回调内的断言失败会通过定时任务的future传播到测试线程，因此可直接在帧内断言。
/// </summary>
public class CoroutineTest
{
    /** 输入输出编解码器 -- 同名key要求类型一致，故提取为静态字段 */
    private static readonly DataKey<int> IntInputCodec = DataKeys.NewIntKey("co_test_input");
    private static readonly DataKey<int> IntOutputCodec = DataKeys.NewIntKey("co_test_output");
    private static readonly DataKey<string> StrInputCodec = DataKeys.NewStringKey("co_test_str_input");
    private static readonly DataKey<string> StrOutputCodec = DataKeys.NewStringKey("co_test_str_output");

    /** 每个逻辑帧推进的时间，秒 */
    private const double FrameDelta = 0.01;
    /** 定时任务的调度间隔，仅影响测试耗时，与逻辑帧的时间语义无关 */
    private static readonly TimeSpan SchedulePeriod = TimeSpan.FromMilliseconds(1);

    private IEventLoop _eventLoop;
    private GTime _time;
    private CoroutineMgr _coroutineMgr;

    [SetUp]
    public void SetUp() {
        _eventLoop = new DisruptorEventLoopBuilder<AgentEvent>()
        {
            ThreadFactory = new DefaultThreadFactory("CoroutineTest", true),
            EventSequencer = new RingBufferEventSequencer<AgentEvent>.Builder(AgentEvent.FACTORY).Build()
        }.Build();
        _time = new GTime();
        _time.Restart();
        _coroutineMgr = new CoroutineMgr(_eventLoop, _time);
        _coroutineMgr.Start();
    }

    [TearDown]
    public void TearDown() {
        _eventLoop.Shutdown();
    }

    #region 测试模型

    /// <summary>
    /// 在事件循环内运行给定帧数的测试主体。
    ///
    /// 每帧先推进时间轴并驱动协程调度，然后回调<paramref name="onFrame"/>，其参数为当前帧号（从1开始）。
    /// 帧回调内可直接调用协程接口并断言 -- 已处于事件循环线程。
    /// </summary>
    /// <param name="frameCount">总帧数</param>
    /// <param name="onFrame">帧回调，参数为当前帧号</param>
    private void RunFrames(int frameCount, Action<int> onFrame) {
        ScheduledTaskBuilder<int> builder = ScheduledTaskBuilder.NewAction(() => {
            _time.Update(FrameDelta);
            _coroutineMgr.Update();
            onFrame(_time.FrameCount);
        });
        builder.SetFixedDelay(0, 1, SchedulePeriod);
        builder.CountLimit = frameCount;

        IFuture<int> future = _eventLoop.Schedule(in builder).AsFuture();
        future.Await(); // Await只等待终止，不抛异常
        // 跑满执行次数时任务进入取消状态，属正常终止；帧回调抛出异常（含断言失败）则进入失败状态
        if (!future.IsCancelled) {
            future.Join(); // 原样抛出帧回调中的异常，不做包装
            Assert.Fail("帧驱动任务应因跑满执行次数而终止，实际却正常完成");
        }
    }

    /// <summary>
    /// 在事件循环内执行一次性操作，用于无需帧推进的场景
    /// </summary>
    private void RunOnce(Action action) {
        _eventLoop.SubmitAction(action).AsFuture().Join();
    }

    /// <summary>
    /// 启动一个int通道的协程
    /// 注：须在事件循环线程调用。
    /// </summary>
    private CoroutineUserContext<int, int> StartIntCoroutine(Func<CoroutineTaskContext<int, int>, ValueFuture> func,
                                                             CancellationToken cancelToken = default) {
        return _coroutineMgr.StartCoroutine(func, new CoroutineStartArgs<int, int>()
        {
            cancelToken = cancelToken,
            inputCodec = IntInputCodec,
            outputCodec = IntOutputCodec,
        });
    }

    #endregion

    #region 基础通道

    /// <summary>
    /// 用户写入命令 -> 协程读取 -> 协程写回结果 -> 用户读取
    /// </summary>
    [Test]
    public void TestEcho() {
        List<int> received = new List<int>();
        List<int> echoes = new List<int>();
        CoroutineUserContext<int, int> ctx = default;

        RunFrames(8, frame => {
            switch (frame) {
                case 1: {
                    ctx = StartIntCoroutine(async context => {
                        for (int i = 0; i < 3; i++) {
                            int cmd = await context.ReadAsync();
                            received.Add(cmd);
                            context.Write(cmd * 2);
                        }
                    });
                    return; // 协程刚启动，尚无结果可读
                }
                // 每隔一帧写入一个命令
                case 2:
                case 4:
                case 6: {
                    ctx.Write(frame * 10);
                    break;
                }
            }
            // 协程写回的结果在其被写入的次帧可读
            if (ctx.TryRead(out int result)) {
                echoes.Add(result);
            }
            if (frame == 8) {
                Assert.IsTrue(ctx.IsTerminated, "协程读满3个命令后应执行结束");
                ctx.Dispose();
            }
        });

        Assert.AreEqual(new[] { 20, 40, 60 }, received, "协程应按写入顺序读取到全部命令");
        Assert.AreEqual(new[] { 40, 80, 120 }, echoes, "用户应读取到协程写回的全部结果");
    }

    /// <summary>
    /// 命令缓冲区可堆积：先写入多个命令，协程后续逐个读取时应立即返回
    /// </summary>
    [Test]
    public void TestBufferedCmd() {
        List<int> received = new List<int>();

        RunFrames(4, frame => {
            switch (frame) {
                case 1: {
                    CoroutineUserContext<int, int> ctx = StartIntCoroutine(async context => {
                        // 先睡一帧，让用户有机会堆积命令
                        await context.Sleep(0);
                        for (int i = 0; i < 3; i++) {
                            received.Add(await context.ReadAsync());
                        }
                    });
                    // 协程尚未开始读取，此时连续写入
                    ctx.Write(1);
                    ctx.Write(2);
                    ctx.Write(3);
                    break;
                }
                case 4: {
                    Assert.AreEqual(new[] { 1, 2, 3 }, received, "堆积的命令应按FIFO顺序被读取");
                    break;
                }
            }
        });
    }

    /// <summary>
    /// TryRead在无数据时返回false，不应抛异常
    /// </summary>
    [Test]
    public void TestTryReadWhenEmpty() {
        RunFrames(3, frame => {
            switch (frame) {
                case 1: {
                    CoroutineUserContext<int, int> ctx = StartIntCoroutine(async context => {
                        await context.ReadAsync();
                    });
                    Assert.IsFalse(ctx.TryRead(out int _), "缓冲区为空时TryRead应返回false");
                    ctx.Cancel(true); // 中断以便回收
                    ctx.Dispose();
                    break;
                }
            }
        });
    }

    /// <summary>
    /// 引用类型通道应能正常传递值，包括null
    /// </summary>
    [Test]
    public void TestStringChannelWithNull() {
        List<string> received = new List<string>();

        RunFrames(3, frame => {
            switch (frame) {
                case 1: {
                    CoroutineUserContext<string, string> ctx = _coroutineMgr.StartCoroutine(async context => {
                        for (int i = 0; i < 2; i++) {
                            received.Add(await context.ReadAsync());
                        }
                    }, new CoroutineStartArgs<string, string>()
                    {
                        inputCodec = StrInputCodec,
                        outputCodec = StrOutputCodec,
                    });
                    ctx.Write("hello");
                    ctx.Write(null);
                    break;
                }
                case 3: {
                    Assert.AreEqual(new[] { "hello", null }, received, "引用类型通道应能传递字符串与null");
                    break;
                }
            }
        });
    }

    #endregion

    #region Sleep

    /// <summary>
    /// Sleep的帧语义：
    /// 1.Sleep(0)在下一帧醒来；
    /// 2.extraDelayFrame额外推迟指定帧数；
    /// 3.按时间Sleep需累积到目标时间才醒来。
    /// </summary>
    [Test]
    public void TestSleepFrameSemantics() {
        List<int> wakeFrames = new List<int>();

        RunFrames(10, frame => {
            if (frame == 1) {
                StartIntCoroutine(async context => {
                    await context.Sleep(0);
                    wakeFrames.Add(context.Time.FrameCount);
                    await context.Sleep(0, 2);
                    wakeFrames.Add(context.Time.FrameCount);
                    await context.Sleep(FrameDelta * 2.5);
                    wakeFrames.Add(context.Time.FrameCount);
                });
            }
        });

        // 协程启动于帧1：Sleep(0)于帧2醒来；Sleep(0,2)于帧4醒来；Sleep(0.025)跨3帧于帧7醒来
        Assert.AreEqual(new[] { 2, 4, 7 }, wakeFrames, "Sleep应按帧与时间语义依次醒来");
    }

    /// <summary>
    /// 协程存在挂起中的异步操作时，不允许再发起新的异步操作
    /// </summary>
    [Test]
    public void TestConcurrentSleepRejected() {
        Exception caught = null;

        RunFrames(8, frame => {
            switch (frame) {
                case 1: {
                    StartIntCoroutine(async context => {
                        ValueFuture first = context.Sleep(FrameDelta * 5);
                        try {
                            await context.Sleep(0); // 第一个Sleep尚未完成
                        }
                        catch (Exception ex) {
                            caught = ex;
                        }
                        await first;
                    });
                    break;
                }
                case 8: {
                    Assert.IsInstanceOf<InvalidOperationException>(caught,
                        "已存在挂起的异步操作时，再次Sleep应抛出InvalidOperationException");
                    break;
                }
            }
        });
    }

    #endregion

    #region Await

    /// <summary>
    /// 协程可等待外部future，并在其完成后取得结果
    /// </summary>
    [Test]
    public void TestAwaitExternalFuture() {
        int awaited = 0;
        ValuePromise<int> external = null;
        int rid = 0;
        CoroutineUserContext<int, int> ctx = default;

        RunFrames(6, frame => {
            switch (frame) {
                case 1: {
                    external = ValuePromise<int>.Acquire(out rid, _eventLoop);
                    ctx = StartIntCoroutine(async context => {
                        awaited = await context.Await(external.Future);
                    });
                    break;
                }
                case 2: {
                    Assert.AreEqual(0, awaited, "外部future未完成前协程应保持挂起");
                    external.TrySetResult(rid, 99);
                    break;
                }
                case 6: {
                    Assert.AreEqual(99, awaited, "外部future完成后协程应取得其结果");
                    Assert.IsTrue(ctx.IsTerminated);
                    ctx.Dispose();
                    break;
                }
            }
        });
    }

    /// <summary>
    /// 等待外部future超时后，协程应观测到取消
    /// </summary>
    [Test]
    public void TestAwaitTimeout() {
        Exception caught = null;

        RunFrames(8, frame => {
            switch (frame) {
                case 1: {
                    // 始终不完成的外部future
                    ValuePromise<int> external = ValuePromise<int>.Acquire(out int _, _eventLoop);
                    StartIntCoroutine(async context => {
                        try {
                            await context.Await(external.Future, FrameDelta * 3);
                        }
                        catch (Exception ex) {
                            caught = ex;
                        }
                    });
                    break;
                }
                case 8: {
                    Assert.IsInstanceOf<OperationCanceledException>(caught,
                        "等待外部future超时后应观测到OperationCanceledException");
                    break;
                }
            }
        });
    }

    #endregion

    #region 超时

    /// <summary>
    /// ReadAsync带超时：超时后抛出OperationCanceledException
    /// </summary>
    [Test]
    public void TestReadAsyncTimeoutThrows() {
        Exception caught = null;

        RunFrames(8, frame => {
            switch (frame) {
                case 1: {
                    StartIntCoroutine(async context => {
                        try {
                            await context.ReadAsync(FrameDelta * 3);
                        }
                        catch (Exception ex) {
                            caught = ex;
                        }
                    });
                    break;
                }
                case 8: {
                    Assert.IsInstanceOf<OperationCanceledException>(caught,
                        "ReadAsync超时后应抛出OperationCanceledException");
                    break;
                }
            }
        });
    }

    /// <summary>
    /// ReadAsync2带超时：以TaskResult形式返回取消状态，不抛异常
    /// </summary>
    [Test]
    public void TestReadAsync2TimeoutReturnsCancelled() {
        TaskResult<int> result = default;
        bool completed = false;

        RunFrames(8, frame => {
            switch (frame) {
                case 1: {
                    StartIntCoroutine(async context => {
                        result = await context.ReadAsync2(FrameDelta * 3);
                        completed = true;
                    });
                    break;
                }
                case 8: {
                    Assert.IsTrue(completed, "ReadAsync2超时后协程应正常继续执行，而非抛出异常");
                    Assert.IsFalse(result.IsSucceeded);
                    Assert.IsTrue(result.IsCancelled, "ReadAsync2超时应表现为取消状态");
                    break;
                }
            }
        });
    }

    /// <summary>
    /// 超时前收到命令时应正常返回，且已注册的超时任务不应误伤后续逻辑
    /// </summary>
    [Test]
    public void TestReadAsyncBeforeTimeout() {
        int received = 0;
        bool timedOut = false;
        CoroutineUserContext<int, int> ctx = default;

        RunFrames(22, frame => {
            switch (frame) {
                case 1: {
                    ctx = StartIntCoroutine(async context => {
                        try {
                            received = await context.ReadAsync(FrameDelta * 10);
                        }
                        catch (OperationCanceledException) {
                            timedOut = true;
                        }
                        // 继续睡够原超时时长，确认超时任务已被清理
                        await context.Sleep(FrameDelta * 15);
                    });
                    break;
                }
                case 2: {
                    ctx.Write(7); // 远早于超时
                    break;
                }
                case 22: {
                    Assert.IsFalse(timedOut, "在超时前写入命令，不应观测到超时");
                    Assert.AreEqual(7, received);
                    Assert.IsTrue(ctx.IsTerminated, "协程应正常执行至结束");
                    ctx.Dispose();
                    break;
                }
            }
        });
    }

    #endregion

    #region 取消与中断

    /// <summary>
    /// 通过用户上下文取消并中断协程：挂起中的读取应被中断唤醒
    /// </summary>
    [Test]
    public void TestCancelInterruptsRead() {
        Exception caught = null;
        CoroutineUserContext<int, int> ctx = default;

        RunFrames(4, frame => {
            switch (frame) {
                case 1: {
                    ctx = StartIntCoroutine(async context => {
                        try {
                            await context.ReadAsync();
                        }
                        catch (Exception ex) {
                            caught = ex;
                        }
                    });
                    break;
                }
                case 2: {
                    Assert.IsNull(caught, "取消前协程应保持挂起");
                    ctx.Cancel(true);
                    // 中断是同步生效的
                    Assert.IsInstanceOf<ThreadInterruptedException>(caught,
                        "中断挂起中的读取应抛出ThreadInterruptedException");
                    Assert.IsTrue(ctx.IsTerminated);
                    ctx.Dispose();
                    break;
                }
            }
        });
    }

    /// <summary>
    /// 仅取消不中断时，协程不会被立即唤醒
    /// </summary>
    [Test]
    public void TestCancelWithoutInterrupt() {
        Exception caught = null;
        CoroutineUserContext<int, int> ctx = default;

        RunFrames(6, frame => {
            switch (frame) {
                case 1: {
                    ctx = StartIntCoroutine(async context => {
                        try {
                            await context.ReadAsync();
                        }
                        catch (Exception ex) {
                            caught = ex;
                        }
                    });
                    break;
                }
                case 2: {
                    ctx.Cancel(false);
                    break;
                }
                case 5: {
                    Assert.IsNull(caught, "interruptIfRunning为false时不应中断挂起中的读取");
                    Assert.IsFalse(ctx.IsTerminated, "协程应仍处于挂起状态");
                    // 收尾：中断并回收
                    ctx.Cancel(true);
                    ctx.Dispose();
                    break;
                }
            }
        });
    }

    /// <summary>
    /// 取消后，协程再次发起ReadAsync应立即失败（不再挂起）
    /// </summary>
    [Test]
    public void TestReadAfterCancelFailsFast() {
        Exception caught = null;
        CoroutineUserContext<int, int> ctx = default;

        RunFrames(5, frame => {
            switch (frame) {
                case 1: {
                    ctx = StartIntCoroutine(async context => {
                        await context.Sleep(0);
                        try {
                            await context.ReadAsync();
                        }
                        catch (Exception ex) {
                            caught = ex;
                        }
                    });
                    // 协程处于Sleep中（无挂起的读取），取消只置上标记
                    ctx.Cancel(false);
                    break;
                }
                case 5: {
                    Assert.IsInstanceOf<OperationCanceledException>(caught,
                        "已收到取消信号后再发起ReadAsync应立即抛出OperationCanceledException");
                    ctx.Dispose();
                    break;
                }
            }
        });
    }

    /// <summary>
    /// 通过取消令牌取消：应能跨线程触发，并中断协程的Sleep
    /// </summary>
    [Test]
    public void TestCancelTokenInterruptsSleep() {
        Exception caught = null;
        bool sleepCompleted = false;
        CancellationTokenSource cts = new CancellationTokenSource();
        CoroutineUserContext<int, int> ctx = default;

        RunFrames(3, frame => {
            switch (frame) {
                case 1: {
                    ctx = StartIntCoroutine(async context => {
                        try {
                            await context.Sleep(3600); // 长时间睡眠，只能由取消信号唤醒
                            sleepCompleted = true;
                        }
                        catch (Exception ex) {
                            caught = ex;
                        }
                    }, cts.Token);
                    break;
                }
                case 2: {
                    Assert.IsNull(caught, "取消前协程应保持睡眠");
                    break;
                }
            }
        });

        // 从测试线程触发，验证取消信号能被投递到事件循环线程
        cts.Cancel();
        RunFrames(2, frame => {
            if (frame == 3) { // 帧号在同一时间轴上延续
                Assert.IsFalse(sleepCompleted, "睡眠不应正常完成");
                Assert.IsInstanceOf<OperationCanceledException>(caught,
                    "取消令牌应中断Sleep并使其进入取消状态");
                Assert.IsTrue(ctx.IsTerminated);
                ctx.Dispose();
            }
        });
        cts.Dispose();
    }

    /// <summary>
    /// 取消令牌应传递到协程上下文，供其自行检查
    /// </summary>
    [Test]
    public void TestCancelTokenVisibleToContext() {
        bool tokenCanBeCanceled = false;
        bool observedBefore = true;
        CancellationTokenSource cts = new CancellationTokenSource();
        CoroutineUserContext<int, int> ctx = default;

        RunFrames(3, frame => {
            switch (frame) {
                case 1: {
                    ctx = StartIntCoroutine(async context => {
                        tokenCanBeCanceled = context.CancelToken.CanBeCanceled;
                        observedBefore = context.CancelToken.IsCancellationRequested;
                        await context.ReadAsync();
                    }, cts.Token);
                    break;
                }
                case 2: {
                    Assert.IsTrue(tokenCanBeCanceled, "启动参数中的取消令牌应传递到协程上下文");
                    Assert.IsFalse(observedBefore, "取消前IsCancellationRequested应为false");
                    Assert.AreEqual(cts.Token, ctx.Box().CancelToken, "用户上下文应持有同一取消令牌");
                    break;
                }
            }
        });

        cts.Cancel();
        RunFrames(2, frame => {
            if (frame == 3) {
                Assert.IsTrue(ctx.IsTerminated, "取消令牌应中断挂起中的读取");
                ctx.Dispose();
            }
        });
        cts.Dispose();
    }

    /// <summary>
    /// 协程管理器停止时，应中断所有存活协程，避免其永久挂起
    /// </summary>
    [Test]
    public void TestStopInterruptsCoroutines() {
        Exception caught = null;
        CoroutineUserContext<int, int> ctx = default;

        RunFrames(3, frame => {
            switch (frame) {
                case 1: {
                    ctx = StartIntCoroutine(async context => {
                        try {
                            await context.ReadAsync();
                        }
                        catch (Exception ex) {
                            caught = ex;
                        }
                    });
                    break;
                }
                case 2: {
                    _coroutineMgr.Stop();
                    Assert.IsInstanceOf<ThreadInterruptedException>(caught,
                        "Stop应中断挂起中的协程，而非任其永久挂起");
                    Assert.IsTrue(_coroutineMgr.IsShutdown);
                    ctx.Dispose();
                    break;
                }
            }
        });
    }

    #endregion

    #region 协程终止后的读取语义

    /// <summary>
    /// 协程结束后，用户侧ReadAsync应立即失败，ReadAsync2应返回取消状态。
    /// 注：两个变体的差异只在"抛异常"与"压制异常"，取消语义须一致。
    /// </summary>
    [Test]
    public void TestReadAfterCoroutineTerminated() {
        TaskResult<int> suppressed = default;
        Exception thrown = null;
        CoroutineUserContext<int, int> target = default;
        CoroutineUserContext<int, int> observer = default;

        RunFrames(6, frame => {
            switch (frame) {
                case 1: {
                    target = StartIntCoroutine(async context => {
                        await context.Sleep(0); // 次帧即结束
                    });
                    break;
                }
                case 3: {
                    Assert.IsTrue(target.IsTerminated, "协程应已结束");
                    // 用户侧读取接口的await需在协程内进行，故借助观测协程
                    observer = StartIntCoroutine(async _ => {
                        suppressed = await target.ReadAsync2();
                        try {
                            await target.ReadAsync();
                        }
                        catch (Exception ex) {
                            thrown = ex;
                        }
                    });
                    break;
                }
                case 6: {
                    Assert.IsFalse(suppressed.IsSucceeded);
                    Assert.IsTrue(suppressed.IsCancelled, "协程已结束时ReadAsync2应返回取消状态");
                    Assert.IsInstanceOf<OperationCanceledException>(thrown,
                        "协程已结束时ReadAsync应抛出OperationCanceledException");
                    observer.Dispose();
                    target.Dispose();
                    break;
                }
            }
        });
    }

    /// <summary>
    /// 协程已写入的结果在其结束后仍可被读取，不应丢失
    /// </summary>
    [Test]
    public void TestPendingResultReadableAfterTerminated() {
        List<int> results = new List<int>();
        CoroutineUserContext<int, int> ctx = default;

        RunFrames(4, frame => {
            switch (frame) {
                case 1: {
                    ctx = StartIntCoroutine(async context => {
                        context.Write(1);
                        context.Write(2);
                        await context.Sleep(0);
                    });
                    break;
                }
                case 3: {
                    Assert.IsTrue(ctx.IsTerminated, "协程应已结束");
                    // 协程已结束，但此前写入的结果仍应可读
                    while (ctx.TryRead(out int v)) {
                        results.Add(v);
                    }
                    Assert.AreEqual(new[] { 1, 2 }, results, "协程结束前写入的结果在其结束后仍应可读");
                    ctx.Dispose();
                    break;
                }
            }
        });
    }

    #endregion

    #region 生命周期与错误契约

    /// <summary>
    /// 协程函数抛出的异常应被捕获，并反映在协程执行结果中
    /// </summary>
    [Test]
    public void TestCoroutineException() {
        CoroutineUserContext<int, int> ctx = default;

        RunFrames(4, frame => {
            switch (frame) {
                case 1: {
                    ctx = StartIntCoroutine(async context => {
                        await context.Sleep(0);
                        throw new InvalidOperationException("boom");
                    });
                    break;
                }
                case 3: {
                    Assert.IsTrue(ctx.IsTerminated, "抛出异常的协程应进入结束状态");
                    TaskResult coResult = ctx.GetCoroutineResult();
                    Assert.IsFalse(coResult.IsSucceeded, "协程结果应为失败");
                    Assert.IsInstanceOf<InvalidOperationException>(coResult.Exception);
                    ctx.Dispose();
                    break;
                }
            }
        });
    }

    /// <summary>
    /// 协程正常结束时应处于结束状态
    /// </summary>
    [Test]
    public void TestCoroutineTerminatedNormally() {
        CoroutineUserContext<int, int> ctx = default;

        RunFrames(4, frame => {
            switch (frame) {
                case 1: {
                    ctx = StartIntCoroutine(async context => {
                        await context.Sleep(0);
                    });
                    Assert.IsFalse(ctx.IsTerminated, "协程尚未执行完毕");
                    break;
                }
                case 3: {
                    Assert.IsTrue(ctx.IsTerminated, "协程执行完毕后应进入结束状态");
                    ctx.Dispose();
                    break;
                }
            }
        });
    }

    /// <summary>
    /// 协程启动阶段（首个挂起点之前）抛出的异常应被捕获，并反映在协程结果中
    /// </summary>
    [Test]
    public void TestExceptionBeforeFirstSuspend() {
        RunFrames(2, frame => {
            if (frame == 1) {
                CoroutineUserContext<int, int> ctx = StartIntCoroutine(
                    _ => throw new ArgumentException("start-fail"));

                Assert.IsTrue(ctx.IsTerminated, "启动即失败的协程应立即处于结束状态");
                TaskResult coResult = ctx.GetCoroutineResult();
                Assert.IsFalse(coResult.IsSucceeded, "协程结果应为失败");
                Assert.IsInstanceOf<ArgumentException>(coResult.Exception);
                ctx.Dispose();
            }
        });
    }

    /// <summary>
    /// 协程函数为null时应抛出ArgumentNullException
    /// </summary>
    [Test]
    public void TestStartCoroutineNullFunc() {
        RunOnce(() => {
            Assert.Throws<ArgumentNullException>(() => _coroutineMgr.StartCoroutine(
                (Func<CoroutineTaskContext<int, int>, ValueFuture>)null,
                new CoroutineStartArgs<int, int>()
                {
                    inputCodec = IntInputCodec,
                    outputCodec = IntOutputCodec,
                }));
        });
    }

    /// <summary>
    /// 启动参数应传递到协程上下文与用户上下文
    /// </summary>
    [Test]
    public void TestStartArgs() {
        object arg1 = new object();
        object arg2 = "arg2";
        object userArg = 42;
        object observedArg1 = null;
        object observedArg2 = null;
        long observedId = 0;
        CoroutineUserContext<int, int> ctx = default;

        RunFrames(3, frame => {
            switch (frame) {
                case 1: {
                    ctx = _coroutineMgr.StartCoroutine(async context => {
                        observedArg1 = context.StartArg1;
                        observedArg2 = context.StartArg2;
                        observedId = context.CoroutineId;
                        await context.ReadAsync();
                    }, new CoroutineStartArgs<int, int>()
                    {
                        startArg1 = arg1,
                        startArg2 = arg2,
                        userArg = userArg,
                        inputCodec = IntInputCodec,
                        outputCodec = IntOutputCodec,
                    });
                    break;
                }
                case 2: {
                    Assert.AreSame(arg1, observedArg1, "startArg1应传递到协程上下文");
                    Assert.AreEqual(arg2, observedArg2, "startArg2应传递到协程上下文");
                    Assert.AreEqual(userArg, ctx.UserData, "userArg应传递到用户上下文");
                    Assert.AreEqual(ctx.CoroutineId, observedId, "两侧上下文的协程ID应一致");
                    ctx.Cancel(true);
                    ctx.Dispose();
                    break;
                }
            }
        });
    }

    /// <summary>
    /// Dispose具备幂等性，且Dispose后可查询到已销毁状态
    /// </summary>
    [Test]
    public void TestDisposeIdempotent() {
        RunFrames(3, frame => {
            if (frame == 1) {
                CoroutineUserContext<int, int> ctx = StartIntCoroutine(async context => {
                    await context.ReadAsync();
                });

                Assert.IsFalse(ctx.IsDisposed, "Dispose前IsDisposed应为false");
                ctx.Dispose();
                ctx.Dispose(); // 重复Dispose不应抛异常
                Assert.IsTrue(ctx.IsDisposed, "Dispose后IsDisposed应为true");

                // 协程尚未结束，中断以完成回收
                _coroutineMgr.Stop();
            }
        });
    }

    /// <summary>
    /// 协程结束且用户上下文销毁后，协程对象被回收，其ID不再可查询
    /// </summary>
    [Test]
    public void TestQueryAfterRecycled() {
        CoroutineUserContext<int, int> ctx = default;

        RunFrames(4, frame => {
            switch (frame) {
                case 1: {
                    ctx = StartIntCoroutine(async context => {
                        await context.Sleep(0);
                    });
                    break;
                }
                case 3: {
                    ctx.Dispose(); // 协程已结束 + 上下文销毁 => 回收
                    Assert.Throws<InvalidOperationException>(() => {
                        bool _ = ctx.IsTerminated;
                    }, "协程已回收后查询状态应抛出InvalidOperationException");
                    break;
                }
            }
        });
    }

    /// <summary>
    /// 同一时刻只允许存在一个挂起的读取任务
    /// </summary>
    [Test]
    public void TestDuplicateReadRejected() {
        Exception caught = null;
        CoroutineUserContext<int, int> ctx = default;

        RunFrames(3, frame => {
            switch (frame) {
                case 1: {
                    ctx = StartIntCoroutine(async context => {
                        ValueFuture<int> first = context.ReadAsync();
                        try {
                            await context.ReadAsync(); // 已存在挂起的读取
                        }
                        catch (Exception ex) {
                            caught = ex;
                        }
                        await first;
                    });
                    break;
                }
                case 2: {
                    Assert.IsInstanceOf<InvalidOperationException>(caught,
                        "重复发起ReadAsync应抛出InvalidOperationException");
                    ctx.Cancel(true);
                    ctx.Dispose();
                    break;
                }
            }
        });
    }

    /// <summary>
    /// 协程的交互接口限制在事件循环线程，跨线程调用应被拒绝
    /// </summary>
    [Test]
    public void TestThreadGuard() {
        CoroutineUserContext<int, int> ctx = default;
        RunOnce(() => {
            ctx = StartIntCoroutine(async context => {
                await context.ReadAsync();
            });
        });

        // 当前处于测试线程，而非事件循环线程
        Assert.Throws<GuardedOperationException>(() => ctx.Write(1), "跨线程Write应被拒绝");
        Assert.Throws<GuardedOperationException>(() => {
            bool _ = ctx.IsTerminated;
        }, "跨线程查询状态应被拒绝");

        RunOnce(() => {
            ctx.Cancel(true);
            ctx.Dispose();
        });
    }

    #endregion

    #region 装箱与拆箱

    /// <summary>
    /// 上下文装箱/拆箱后应保持等效的协程标识与交互能力
    /// </summary>
    [Test]
    public void TestBoxUnbox() {
        int received = 0;
        long boxedTaskCtxId = 0;
        CoroutineUserContext<int, int> ctx = default;

        RunFrames(5, frame => {
            switch (frame) {
                case 1: {
                    ctx = StartIntCoroutine(async context => {
                        CoroutineTaskContext boxed = context.Box();
                        boxedTaskCtxId = boxed.CoroutineId;
                        CoroutineTaskContext<int, int> unboxed = boxed.Unbox(IntInputCodec, IntOutputCodec);
                        received = await unboxed.ReadAsync();
                    });
                    break;
                }
                case 2: {
                    Assert.AreEqual(ctx.CoroutineId, boxedTaskCtxId, "任务上下文装箱后协程ID应保持一致");
                    CoroutineUserContext boxed = ctx.Box();
                    Assert.AreEqual(ctx.CoroutineId, boxed.CoroutineId, "用户上下文装箱后协程ID应保持一致");
                    boxed.Unbox(IntInputCodec, IntOutputCodec).Write(5);
                    break;
                }
                case 5: {
                    Assert.AreEqual(5, received, "经装箱/拆箱的上下文应能正常通信");
                    Assert.IsTrue(ctx.IsTerminated);
                    ctx.Dispose();
                    break;
                }
            }
        });
    }

    #endregion

    #region 多协程

    /// <summary>
    /// 多个协程应彼此独立：各自的通道互不干扰
    /// </summary>
    [Test]
    public void TestMultipleCoroutinesIsolated() {
        List<int> receivedA = new List<int>();
        List<int> receivedB = new List<int>();

        RunFrames(4, frame => {
            switch (frame) {
                case 1: {
                    CoroutineUserContext<int, int> ctxA = StartIntCoroutine(async context => {
                        receivedA.Add(await context.ReadAsync());
                    });
                    CoroutineUserContext<int, int> ctxB = StartIntCoroutine(async context => {
                        receivedB.Add(await context.ReadAsync());
                    });
                    Assert.AreNotEqual(ctxA.CoroutineId, ctxB.CoroutineId, "不同协程应分配不同ID");

                    ctxA.Write(100);
                    ctxB.Write(200);
                    ctxA.Dispose();
                    ctxB.Dispose();
                    break;
                }
                case 4: {
                    Assert.AreEqual(new[] { 100 }, receivedA, "协程A只应收到写给它的命令");
                    Assert.AreEqual(new[] { 200 }, receivedB, "协程B只应收到写给它的命令");
                    break;
                }
            }
        });
    }

    /// <summary>
    /// 取消其中一个协程不应影响其它协程
    /// </summary>
    [Test]
    public void TestCancelOneCoroutineOnly() {
        Exception caughtA = null;
        int receivedB = 0;
        CoroutineUserContext<int, int> ctxA = default;
        CoroutineUserContext<int, int> ctxB = default;

        RunFrames(5, frame => {
            switch (frame) {
                case 1: {
                    ctxA = StartIntCoroutine(async context => {
                        try {
                            await context.ReadAsync();
                        }
                        catch (Exception ex) {
                            caughtA = ex;
                        }
                    });
                    ctxB = StartIntCoroutine(async context => {
                        receivedB = await context.ReadAsync();
                    });
                    break;
                }
                case 2: {
                    ctxA.Cancel(true);
                    Assert.IsInstanceOf<ThreadInterruptedException>(caughtA, "协程A应被中断");
                    Assert.IsFalse(ctxB.IsTerminated, "协程B不应受影响");
                    ctxB.Write(7);
                    break;
                }
                case 5: {
                    Assert.AreEqual(7, receivedB, "协程B应能继续正常通信");
                    ctxA.Dispose();
                    ctxB.Dispose();
                    break;
                }
            }
        });
    }

    #endregion
}
}
