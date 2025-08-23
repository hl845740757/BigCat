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
using System.Threading;
using Wjybxx.Commons;
using Wjybxx.Commons.Attributes;
using Wjybxx.Commons.Concurrent;
using Wjybxx.Commons.Fx;
using Wjybxx.Commons.Logger;
using Wjybxx.Disruptor;

namespace Wjybxx.BigCat.Unity
{
/// <summary>
/// UnityEventLoop是指没有自己的线程，由引擎调度的EventLoop。
/// 因此它并不局限于Unity引擎 —— 放在这里也说明了问题。
///
/// 1.用户在构造时需要传入<see cref="Thread"/>引用。
/// 2.由用户手动调用<see cref="Internal_Start"/>启动事件循环，<see cref="Internal_Stop"/>停止事件循环。
/// 3.由用户手动调用<see cref="Internal_Update"/>处理队列中的事件和定时任务。
/// 4.该事件循环默认不会设置<see cref="SynchronizationContext"/>，以避免和Unity的同步上下文冲突。
/// 5.Unity下的等待策略应避免阻塞，由Unity线程控制主循环频率，可参考<see cref="UnityWaitStrategy"/>。
/// 
/// PS：该实现由<see cref="DisruptorEventLoop{T}"/>修改而来，主要变化为：内部线程驱动 => 外部线程驱动。
/// </summary>
/// <typeparam name="T"></typeparam>
public class UnityEventLoop<T> : AbstractEventLoop, IDisruptorEventLoop<T> where T : IAgentEvent
{
    private static readonly ILogger logger = LoggerFactory.GetLogger(typeof(UnityEventLoop<T>));

    private const int ST_UNSTARTED = (int)EventLoopState.Unstarted;
    private const int ST_STARTING = (int)EventLoopState.Starting;
    private const int ST_RUNNING = (int)EventLoopState.Running;
    private const int ST_SHUTTING_DOWN = (int)EventLoopState.ShuttingDown;
    private const int ST_SHUTDOWN = (int)EventLoopState.Shutdown;
    private const int ST_TERMINATED = (int)EventLoopState.Terminated;

    private const int MIN_BATCH_SIZE = 64;
    private const int MAX_BATCH_SIZE = 64 * 1024;
    /** 无效事件类型 */
    internal const int TYPE_INVALID = IAgentEvent.TYPE_INVALID;
    /** 基础事件类型，参数列表为：task */
    internal const int TYPE_RUNNABLE = 0;
    /** 删除延时任务，参数列表为：taskId */
    internal const int TYPE_REMOVE_SCHEDULE = -1;

    /** 线程本地时间 -- 时间的更新频率极高，进行缓存行填充隔离；使用volatile读写 */
    private PaddedInt64 _tickTime;
    /** 可消费的最大序号 -- 非死循环情况下，需要存储在外部；线程本地变量不填充 */
    private long availableSequence = -1;
    /** 线程状态 -- 变化频率低，不填充 */
    private volatile int state = ST_UNSTARTED;

    /** 事件队列 */
    private readonly EventSequencer<T> eventSequencer;
    /** 缓存值 -- 减少转发 */
    private readonly DataProvider<T> dataProvider;
    /** 缓存值 -- 减少运行时测试 */
    private readonly MpUnboundedBuffer<T>? unboundedBuffer;
    /** 周期性任务队列 -- 既有的任务都是先于Sequencer中的任务提交的 */
    private readonly DisruptorSchedulerHelper<T> schedulerHelper;
    /** 任务拒绝策略 */
    protected readonly RejectedExecutionHandler rejectedExecutionHandler;
    /** 内部代理 */
    protected readonly IEventLoopAgent<T> agent;
    /** 批量执行任务的大小 */
    private readonly int batchSize;
    /** 发布事件时是否采用copy的方式 -- C#特殊支持 */
    private readonly bool publishValueEventWithCopy;

    /** unity线程 -- 在构造时绑定 */
    private readonly Thread thread;
    private readonly long consumerId;
    /** 消费者屏障 -- 由于C#的委托不是接口，因此委托依赖的数据存储在EventLoop上 */
    private readonly ConsumerBarrier barrier;
    /** 消费进度 -- 最新设计下需要通过Barrier获取 */
    private readonly Sequence sequence;

    /** 进入运行状态的promise */
    private readonly IPromise<int> runningPromise;
    /** 进入终止状态的promise */
    private readonly IPromise<int> terminationPromise;
    /** 只读future - 缓存字段 */
    private readonly IFuture runningFuture;
    private readonly IFuture terminationFuture;

    public UnityEventLoop(UnityEventLoopBuilder<T> builder, bool bindAgent = true)
        : base(builder.Parent, builder.ModuleList) {
        this._tickTime.SetVolatile(ObjectUtil.SystemTicks());
        this.eventSequencer = builder.EventSequencer ?? throw new ArgumentException("builder.EventSequencer");
        this.dataProvider = eventSequencer.DataProvider;
        this.schedulerHelper = new DisruptorSchedulerHelper<T>(this);

        this.rejectedExecutionHandler = builder.RejectedExecutionHandler ?? RejectedExecutionHandlers.ABORT;
        this.agent = builder.Agent ?? EmptyAgent<T>.Inst;
        this.batchSize = Math.Clamp(builder.BatchSize, MIN_BATCH_SIZE, MAX_BATCH_SIZE);
        this.publishValueEventWithCopy = typeof(T).IsValueType && builder.PublishValueEventWithCopy;
        // 缓存
        if (dataProvider is MpUnboundedBuffer<T> unboundedBuffer2) {
            this.unboundedBuffer = unboundedBuffer2;
        } else {
            this.unboundedBuffer = null;
        }

        runningPromise = new Promise<int>(this);
        terminationPromise = new Promise<int>(this);
        runningFuture = runningPromise.AsReadonly();
        terminationFuture = terminationPromise.AsReadonly();

        // worker只依赖生产者屏障
        barrier = eventSequencer.NewSingleConsumerBarrier(builder.WaitStrategy);
        sequence = barrier.GroupSequence();
        thread = builder.Thread;
        consumerId = builder.ConsumerId;
        // 添加worker的sequence为网关sequence，生产者们会监听到线程的消费进度
        eventSequencer.AddGatingBarriers(barrier);

        // 绑定Agent
        if (bindAgent) {
            this.agent.Inject(this, consumerId);
        }
    }

    public IEventLoopAgent<T> Agent => agent;

    public override long TickTime {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _tickTime.GetVolatile();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private long UpdateTickTime() {
        long tickTime = ObjectUtil.SystemTicks();
        _tickTime.SetVolatile(tickTime);
        return tickTime;
    }

    protected override ISchedulerHelper SchedulerHelper {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => schedulerHelper;
    }

    #region 状态查询

    public sealed override EventLoopState State => (EventLoopState)state;
    public sealed override bool IsRunning => state == ST_RUNNING;
    public sealed override bool IsShuttingDown => state >= ST_SHUTTING_DOWN;
    public sealed override bool IsShutdown => state >= ST_SHUTDOWN;
    public sealed override bool IsTerminated => state == ST_TERMINATED;

    public sealed override IFuture RunningFuture => runningFuture;
    public override IFuture TerminationFuture => terminationFuture;

    public sealed override bool InEventLoop() {
        return this.thread == Thread.CurrentThread;
    }

    public sealed override bool InEventLoop(Thread thread) {
        return this.thread == thread;
    }

    #endregion

    #region 任务提交

    public override void Execute(ITask task) {
        if (task == null) throw new ArgumentNullException(nameof(task));
        if ((task.Options & TaskOptions.LOCAL_ORDER) != 0
            && task is IScheduledFutureTask futureTask
            && InEventLoop()) {
            schedulerHelper.DoSchedule(futureTask);
            return;
        }
        long sequence = NextSequence(1);
        if (sequence < 0) {
            rejectedExecutionHandler.Rejected(task, this);
            return;
        }
        PublishTask(task, sequence, task.Options);
    }

    public override void Execute(Action command, int options = 0) {
        if (command == null) throw new ArgumentNullException(nameof(command));
        long sequence = NextSequence(1);
        if (sequence < 0) {
            rejectedExecutionHandler.Rejected(ExecutorUtil.ToTask(command, options), this);
            return;
        }
        PublishTask(command, sequence, options);
    }

    /// <summary>
    /// Q: 如何保证算法的安全性的？
    /// A: 我们只需要保证申请到的sequence是有效的，且发布任务在{@link Worker#removeFromGatingBarriers()}之前即可。
    /// 因为{@link Worker#removeFromGatingBarriers()}之前申请到的sequence一定是有效的，它考虑了EventLoop的消费进度。
    /// 
    /// 关键时序：
    /// 1. {@link #isShuttingDown()}为true一定在{@link Worker#cleanBuffer()}之前。
    /// 2. {@link Worker#cleanBuffer()}必须等待在这之前申请到的sequence发布。
    /// 3. {@link Worker#cleanBuffer()}在所有生产者发布数据之后才{@link Worker#removeFromGatingBarriers()}
    /// 
    /// 因此，{@link Worker#cleanBuffer()}之前申请到的sequence是有效的；
    /// 又因为{@link #isShuttingDown()}为true一定在{@link Worker#cleanBuffer()}之前，
    /// 因此，如果sequence是在{@link #isShuttingDown()}为true之前申请到的，那么sequence一定是有效的，否则可能有效，也可能无效。
    ///
    /// sequence的处理见<see cref="NextSequence(int)"/>
    /// </summary>
    /// <param name="task"></param>
    /// <param name="sequence"></param>
    /// <param name="options"></param>
    private void PublishTask(object task, long sequence, int options) {
        if (publishValueEventWithCopy) {
            T eventObj = default;
            try {
                eventObj!.Type = 0;
                eventObj.Obj1 = task;
                eventObj.Options = options;
            }
            finally {
                eventSequencer.Publish(sequence, in eventObj);
            }
        } else {
            ref T eventObj = ref dataProvider.ProducerGetRef(sequence);
            try {
                eventObj.Type = 0;
                eventObj.Obj1 = task;
                eventObj.Options = options;
            }
            finally {
                eventSequencer.Publish(sequence);
            }
        }

        // RingBuffer不再私有，不可测试sequence == 0
        if (state == ST_UNSTARTED) {
            EnsureThreadStarted();
        } else if ((options & TaskOptions.WAKEUP_THREAD) != 0 && !InEventLoop()) {
            Wakeup();
        }
    }

    #endregion

    #region disruptor接口

    /** 获取事件循环的消费者id */
    [Beta]
    public long ConsumerId => consumerId;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetEvent(long sequence) {
        return dataProvider.ProducerGet(sequence);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetEventRef(long sequence) {
        return ref dataProvider.ProducerGetRef(sequence);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetEvent(long sequence, T eventObj) {
        dataProvider.ProducerSet(sequence, eventObj);
    }

    public long TryNextSequence(int size = 1) {
        if (IsShuttingDown) {
            return -1;
        }
        long sequence = eventSequencer.TryNext(size);
        if (sequence < 0) {
            return -1;
        }
        if (IsShuttingDown) {
            // 申请序号期间收到关闭序号
            if (size == 1) {
                eventSequencer.Publish(sequence);
            } else {
                long lo = sequence - (size - 1);
                eventSequencer.Publish(lo, sequence);
            }
            return -1;
        }
        return sequence;
    }

    public void Publish(long sequence, in T evt) {
        eventSequencer.Publish(sequence, in evt);
        if (state == ST_UNSTARTED) {
            EnsureThreadStarted();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long NextSequence() {
        return NextSequence(1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Publish(long sequence) {
        eventSequencer.Publish(sequence);
        // RingBuffer不再私有，不可测试sequence == 0
        if (state == ST_UNSTARTED) {
            EnsureThreadStarted();
        }
    }

    public long NextSequence(int size) {
        if (IsShuttingDown) {
            return -1;
        }
        long sequence;
        if (InEventLoop()) {
            // 如果在当前事件循环，不可阻塞
            sequence = eventSequencer.TryNext(size);
            if (sequence < 0) {
                return -1;
            }
        } else {
            sequence = eventSequencer.Next(size);
        }
        if (IsShuttingDown) {
            // 申请序号期间收到关闭序号
            if (size == 1) {
                eventSequencer.Publish(sequence);
            } else {
                long lo = sequence - (size - 1);
                eventSequencer.Publish(lo, sequence);
            }
            return -1;
        }
        return sequence;
    }

    public void Publish(long lo, long hi) {
        eventSequencer.Publish(lo, hi);
        if (state == ST_UNSTARTED) {
            EnsureThreadStarted();
        }
    }

    public void Subscribe(int type, IAgentEventHandler<T> handler) {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        if (!InEventLoop()) {
            throw new IllegalStateException();
        }
        agent.Subscribe(type, handler);
    }

    #endregion

    #region 线程状态切换

    public override void Wakeup() {
        if (!InEventLoop() && thread.IsAlive) {
            thread.Interrupt();
            agent.Wakeup();
        }
    }

    public override IFuture Start() {
        EnsureThreadStarted();
        return runningFuture;
    }

    public override void Shutdown() {
        if (!runningPromise.IsCompleted) { // 尚未启动成功就关闭
            runningPromise.TrySetCancelled(CancelCodes.REASON_SHUTDOWN);
        }
        int expectedState = state;
        for (;;) {
            if (expectedState >= ST_SHUTTING_DOWN) {
                return;
            }
            int realState = Interlocked.CompareExchange(ref state, ST_SHUTTING_DOWN, expectedState);
            if (realState == expectedState) {
                EnsureThreadTerminable(expectedState);
                return;
            }
            // retry
            expectedState = realState;
        }
    }

    public override List<ITask> ShutdownNow() {
        Shutdown();
        AdvanceRunState(ST_SHUTDOWN);
        // 这里不能操作ringBuffer中的数据，不能打破[多生产者单消费者]的架构
        return new List<ITask>(0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureThreadStarted() {
    }

    /// <summary>
    /// 确保线程可关闭
    /// </summary>
    /// <param name="oldState">切换为关闭状态之前的状态</param>
    private void EnsureThreadTerminable(int oldState) {
        if (oldState == ST_UNSTARTED) {
            // TODO 是否需要启动线程，进行更彻底的清理？
            state = ST_TERMINATED;
            RemoveFromGatingBarriers(); // 防死锁

            runningPromise.TrySetException(new StartFailedException("Stillborn"));
            terminationPromise.TrySetResult(0);
        } else {
            // 等待策略是根据alert信号判断EventLoop是否已开始关闭的，因此即使inEventLoop也需要alert，否则可能丢失信号，在waitFor处无法停止
            barrier.Alert();
            // 唤醒线程 - 如果线程可能阻塞在其它地方
            Wakeup();
        }
    }

    /// <summary>
    /// 如果事件循环尚未到达指定状态，则更新为指定状态
    /// </summary>
    /// <param name="targetState">要到达的状态</param>
    private void AdvanceRunState(int targetState) {
        int expectedState = state;
        for (;;) {
            if (expectedState >= targetState) {
                return;
            }
            int realState = Interlocked.CompareExchange(ref state, targetState, expectedState);
            if (realState >= targetState) { // == 表示CAS成功， > 表示已进入目标状态
                return;
            }
            // retry
            expectedState = realState;
        }
    }

    #endregion

    #region modules

    /** 事件循环启动 */
    protected virtual void OnStart() {
        agent.BeforeEventLoopStart();
        StartModules();
        agent.AfterEventLoopStart();
    }

    /** 事件循环开始退出 */
    protected virtual void OnShutdown() {
        agent.BeforeEventLoopShutdown();
        StopModules();
        DestroyModules();
        agent.AfterEventLoopShutdown();
    }

    /** 启动所有模块 */
    protected void StartModules() {
        // 模块的部分数据初始化
        foreach (EventLoopModule module in _moduleList) {
            if (module.Cid.shared) {
                continue;
            }
            if (module.Status == ComponentStatus.New) {
                module.SetEventLoop(this);
            }
        }
        // 解决模块之间的依赖
        foreach (EventLoopModule module in _moduleList) {
            if (!module.Cid.IsPrivateScript) {
                continue;
            }
            module.ResolveDependence();
        }
        // 顺序启动 - Start
        foreach (EventLoopModule module in _moduleList) {
            if (!module.Cid.IsPrivateScript) {
                continue;
            }
            module.InvokeStart();
        }
    }

    /** 停止所有模块 */
    protected void StopModules() {
        // 逆序停止
        for (int index = _moduleList.Count - 1; index >= 0; index--) {
            EventLoopModule module = _moduleList[index];
            if (!module.Cid.IsPrivateScript) {
                continue;
            }
            if (module.Status != ComponentStatus.Running) {
                continue; // 未启动
            }
            try {
                module.InvokeStop();
            }
            catch (Exception ex) {
                logger.Warn(ex, "stop module caught exception");
            }
        }
    }

    /** 销毁所有模块 -- 不删除引用 */
    protected void DestroyModules() {
        // 顺序销毁 -- 组件之间不能有时序依赖
        foreach (EventLoopModule module in _moduleList) {
            if (module.Cid.shared) {
                continue;
            }
            if (module.Status == ComponentStatus.New) {
                continue; // 未执行OnReady
            }
            try {
                module.InvokeDestroy();
            }
            catch (Exception ex) {
                logger.Warn(ex, "module.destroy caught exception");
            }
        }
    }

    /** 迭代所有模块 */
    protected void UpdateModules() {
        long tickTime = this.TickTime;
        while (agent.CheckMainLoop(tickTime)) {
            agent.BeforeMainLoop(tickTime);
            for (int i = 0; i < _earlyUpdateModuleList.Count; i++) {
                EventLoopModule module = _earlyUpdateModuleList[i];
                try {
                    module.EarlyUpdate();
                }
                catch (Exception ex) {
                    logger.Info(ex, "module.earlyUpdate caught exception");
                }
            }
            for (int i = 0; i < _updateModuleList.Count; i++) {
                EventLoopModule module = _updateModuleList[i];
                try {
                    module.Update();
                }
                catch (Exception ex) {
                    logger.Info(ex, "module.update caught exception");
                }
            }
            for (int i = 0; i < _lateUpdateModuleList.Count; i++) {
                EventLoopModule module = _lateUpdateModuleList[i];
                try {
                    module.LateUpdate();
                }
                catch (Exception ex) {
                    logger.Info(ex, "module.lateUpdate caught exception");
                }
            }
            agent.AfterMainLoop(tickTime);
        }
        agent.CustomUpdate(tickTime);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnInternalEvent(long curSequence, ref T evt) {
        int type = evt.Type;
        if (type == TYPE_REMOVE_SCHEDULE) {
            // 删除延时任务
            IScheduledFutureTask futureTask = (IScheduledFutureTask)evt.Obj1;
            long taskId = evt.LongVal1;
            int cancelCode = (int)evt.LongVal2;
            schedulerHelper.Cancel(futureTask, taskId, cancelCode);
        }
    }

    #endregion

    #region mainloop

    /// <summary>
    /// 启动事件循环
    /// 在启动失败的情况下，用户需要调用<see cref="Internal_Stop"/>
    /// </summary>
    /// <returns>是否启动成功</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public bool Internal_Start() {
        if (Thread.CurrentThread != thread) {
            throw new InvalidOperationException();
        }
        if (state != ST_UNSTARTED
            || Interlocked.CompareExchange(ref state, ST_STARTING, ST_UNSTARTED) != ST_UNSTARTED) {
            return false;
        }
        // 
        try {
            UpdateTickTime();
            if (!runningPromise.TrySetComputing()) {
                goto startFailed;
            }
            OnStart();
            AdvanceRunState(ST_RUNNING);
            if (!runningPromise.TrySetResult(0)) {
                goto startFailed;
            }
            return true;
        }
        catch (Exception e) {
            logger.Error(e, "thread exit due to exception!");
            runningPromise.TrySetException(new StartFailedException("StartFailed", e));
            goto startFailed;
        }
        // 启动失败直接进入清理状态，丢弃所有提交的任务
        startFailed:
        {
            AdvanceRunState(ST_SHUTDOWN);
            return false;
        }
    }

    /// <summary>
    /// 停止事件循环
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public void Internal_Stop() {
        if (Thread.CurrentThread != thread) {
            throw new InvalidOperationException();
        }
        try {
            // 清理ringBuffer中的数据
            CleanBuffer();
            schedulerHelper.ClearTaskQueue();
        }
        finally {
            RemoveFromGatingBarriers();
            AdvanceRunState(ST_SHUTDOWN);

            // 退出前进行必要的清理，释放系统资源
            try {
                OnShutdown();
            }
            catch (Exception e) {
                logger.Error(e, "thread exit caught exception!");
            }
            finally {
                state = ST_TERMINATED;
                terminationPromise.TrySetResult(0);
            }
        }
    }

    /// <summary>
    /// 执行一次线程循环
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public void Internal_Update() {
        if (Thread.CurrentThread != thread) {
            throw new InvalidOperationException();
        }
        // 和DisruptorEventLoop相比，其实就是去除了While循环
        if (state != ST_RUNNING) return;

        long nextSequence = sequence.GetVolatile() + 1L;
        try {
            long tickTime = UpdateTickTime();
            schedulerHelper.Update(tickTime, false);
            if (IsShutdown) {
                return; // 前面的任务可能未执行首次逻辑，后续的任务也不能执行
            }
            if (availableSequence < nextSequence
                && (availableSequence = barrier.WaitFor(nextSequence)) < nextSequence) {
                UpdateModules();
                return; // 等待超时
            }

            long batchEndSequence = Math.Min(availableSequence, nextSequence + batchSize - 1);
            long curSequence = RunTaskBatch(nextSequence, batchEndSequence);
            sequence.SetRelease(curSequence);
            // 无界队列尝试主动回收块
            if (unboundedBuffer != null) {
                unboundedBuffer.TryMoveHeadToNext(curSequence);
            }
            nextSequence = curSequence + 1;
            if (nextSequence <= batchEndSequence) {
                Debug.Assert(IsShuttingDown);
                return;
            }
            UpdateModules();
        }
        catch (ThreadInterruptedException e) {
            if (IsShuttingDown) {
                return;
            }
            logger.Warn(e, "receive a confusing signal");
        }
        catch (AlertException e) {
            if (IsShuttingDown) {
                return;
            }
            logger.Warn(e, "receive a confusing signal");
        }
        catch (ThreadAbortException) {
            ShutdownNow(); // unity下直接中断线程
        }
        catch (Exception e) {
            // 不好的等待策略实现
            logger.Error(e, "bad waitStrategy impl");
        }
    }

    /// <summary>
    /// 批量处理任务
    /// </summary>
    /// <param name="batchBeginSequence">批处理的第一个序号</param>
    /// <param name="batchEndSequence">批处理的最后一个序号</param>
    /// <returns>curSequence</returns>
    private long RunTaskBatch(long batchBeginSequence, long batchEndSequence) {
        DataProvider<T> dataProvider = this.dataProvider;
        IEventLoopAgent<T> agent = this.agent;
        for (long curSequence = batchBeginSequence; curSequence <= batchEndSequence; curSequence++) {
            ref T eventObj = ref dataProvider.ConsumerGetRef(curSequence);
            int type = eventObj.Type;
            try {
                if (type == 0) {
                    if (eventObj.Obj1 == null) {
                        // 由于c#支持值类型，用户设置错工厂容易导致该问题
                        logger.Warn("bad event factory");
                    } else if (eventObj.Obj1 is Action action) {
                        action();
                    } else {
                        ITask task = (ITask)eventObj.Obj1;
                        task.Run();
                    }
                } else if (type > 0) {
                    // 用户事件
                    agent.OnEvent(curSequence, ref eventObj);
                } else if (type != TYPE_INVALID) {
                    // 事件循环事件
                    OnInternalEvent(curSequence, ref eventObj);
                } else {
                    // 生产者放弃序号
                    if (IsShuttingDown) {
                        return curSequence;
                    }
                    logger.Warn("user published invalid event: " + eventObj); // 用户发布了非法数据
                }
            }
            catch (Exception ex) {
                logger.Info(ex, "execute task caught exception");
                if (IsShuttingDown) { // 可能是中断或Alert，检查关闭信号
                    return curSequence;
                }
            }
            finally {
                eventObj.Clean();
            }
        }
        return batchEndSequence;
    }

    /// <summary>
    /// 将自己从网关序列中删除
    /// 这是解决死锁问题的关键，如果不从gatingBarriers中移除，则生产者无法从{@link ProducerBarrier#next()}中退出，
    /// </summary>
    private void RemoveFromGatingBarriers() {
        eventSequencer.RemoveGatingBarrier(barrier);
    }

    /// <summary>
    /// 清理缓冲区
    /// </summary>
    private void CleanBuffer() {
        long startTimeMillis = ObjectUtil.SystemTickMillis();

        // 处理延迟任务，保证时序 -- 外部清理
        long tickTime = UpdateTickTime();
        schedulerHelper.Update(tickTime, true);
        schedulerHelper.ClearTaskQueue(); // 执行一次清理，避免下面的内部事件执行

        // 在新的架构下，EventSequencer可能是无界队列，这种情况下我们采用笨方法来清理；
        // 从当前序列开始消费，一直消费到最新的cursor，然后将自己从gatingBarrier中删除 -- 此时不论有界无界，生产者都将醒来。
        long nullCount = 0;
        long taskCount = 0;
        long discardCount = 0;

        DataProvider<T> dataProvider = this.dataProvider;
        IEventLoopAgent<T> agent = this.agent;
        ProducerBarrier producerBarrier = eventSequencer.ProducerBarrier;
        Sequence sequence = this.sequence;
        while (true) {
            long nextSequence = sequence.GetVolatile() + 1;
            if (nextSequence > producerBarrier.Sequence()) {
                break;
            }
            while (!producerBarrier.IsPublished(nextSequence)) {
                Thread.Sleep(1); // 等待发布-1ms
            }
            ref T eventObj = ref dataProvider.ConsumerGetRef(nextSequence);
            int type = eventObj.Type;
            try {
                if (type == TYPE_INVALID) {
                    nullCount++;
                    continue;
                }
                taskCount++;
                if (IsShutdown) {
                    discardCount++;
                    eventObj.CleanAll();
                    continue;
                }
                if (type == 0) {
                    if (eventObj.Obj1 == null) {
                        // 由于c#支持值类型，用户设置错工厂容易导致该问题
                        logger.Warn("bad event factory");
                    } else if (eventObj.Obj1 is Action action) {
                        action();
                    } else {
                        ITask task = (ITask)eventObj.Obj1;
                        task.Run();
                    }
                } else if (type > 0) {
                    // 用户事件
                    agent.OnEvent(nextSequence, ref eventObj);
                } else {
                    // 事件循环事件
                    OnInternalEvent(nextSequence, ref eventObj);
                }
            }
            catch (Exception ex) {
                logger.Info(ex, "execute task caught exception");
            }
            finally {
                eventObj.CleanAll();
                sequence.SetRelease(nextSequence);
            }
            // 由于生产者已停止，如果不moveHead，后续的查询性能将越来越差
            if (unboundedBuffer != null && !unboundedBuffer.InSameChunk(nextSequence - 1, nextSequence)) {
                unboundedBuffer.TryMoveHeadToNext(nextSequence);
            }
        }
        long costTime = ObjectUtil.SystemTickMillis() - startTimeMillis;
        logger.Info(
            $"cleanBuffer success!  nullCount = {nullCount}, taskCount = {taskCount}, discardCount {discardCount}, cost timeMillis = {costTime}");
    }

    #endregion
}
}