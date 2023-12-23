/*
 * Copyright 2023 wjybxx(845740757@qq.com)
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

package cn.wjybxx.common.concurrent;

import cn.wjybxx.common.MathCommon;
import cn.wjybxx.common.annotation.Beta;
import cn.wjybxx.common.collect.DefaultIndexedPriorityQueue;
import cn.wjybxx.common.collect.IndexedPriorityQueue;
import cn.wjybxx.common.concurrent.ext.MpscSequenceBarrier;
import com.lmax.disruptor.*;
import org.jctools.queues.IndexedQueueSizeUtil;
import org.jctools.queues.MpscUnboundedXaddArrayQueue;
import org.jctools.queues.MpscUnboundedXaddArrayQueue2;

import javax.annotation.Nonnull;
import java.lang.invoke.MethodHandles;
import java.lang.invoke.VarHandle;
import java.util.Collections;
import java.util.List;
import java.util.Objects;
import java.util.concurrent.ThreadFactory;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.locks.LockSupport;

/**
 * 基于{@link MpscUnboundedXaddArrayQueue}实现的无界事件循环
 * 一个无界的事件循环通常是必须的，以避免死锁问题；不过，单方面无界有时也不能·完全避免死锁，与之交互的线程如果是有界队列，仍可能产生问题。
 * 这里对Disruptor中的等待机制进行了适配，以便用户使用相同的接口等待事件。
 *
 * <h3>无界队列</h3>
 * 1. 事件队列不能使用{@link RingBufferEvent}
 * 2. 任务的取消是不安全的
 * 3. 只有消费者等生产者的情况，没有生产者等消费者的情况。
 *
 * @author wjybxx
 * date 2023/4/10
 */
public class DefaultEventLoop extends AbstractScheduledEventLoop {

    private static final int MIN_BATCH_SIZE = 64;
    private static final int MAX_BATCH_SIZE = 64 * 1024;

    private static final int HIGHER_PRIORITY_QUEUE_ID = 0;
    private static final int LOWER_PRIORITY_QUEUE_ID = 1;

    private static final Runnable _emptyRunnable = () -> {};
    private static final Runnable _invalidRunnable = () -> {};

    // 填充开始 - 字段定义顺序不要随意调整
    @SuppressWarnings("unused")
    private long p1, p2, p3, p4, p5, p6, p7, p8;

    /** 线程状态 */
    private volatile int state = ST_NOT_STARTED;
    @SuppressWarnings("unused")
    private long p9, p10, p11, p12, p13, p14, p15, p16;

    /** 线程本地时间 -- 纳秒 */
    private volatile long nanoTime;
    @SuppressWarnings("unused")
    private long p17, p18, p19, p20, p21, p22, p23, p24;

    /** 事件队列 */
    private final MpscUnboundedXaddArrayQueue2<Runnable> ringBuffer;
    /** 主要用于唤醒消费者（当前EventLoop） */
    private final MpscSequencer sequencer;
    /** 用于减少发布事件开销 */
    private final MpscUnboundedXaddArrayQueue2.OfferHooker<Runnable> translator;

    /** 周期性任务队列 -- 都是先于taskQueue中的任务提交的 -- 暂不提供带缓存行填充的实现 */
    private final IndexedPriorityQueue<XScheduledFutureTask<?>> scheduledTaskQueue;
    /** 批量执行任务的大小 */
    private final int taskBatchSize;
    /** 任务拒绝策略 */
    private final RejectedExecutionHandler rejectedExecutionHandler;
    /** 内部代理 */
    private final EventLoopAgent agent;
    /** 外部门面 */
    private final EventLoopModule mainModule;

    private final Thread thread;
    private final Worker worker;
    private final XCompletableFuture<?> terminationFuture = new XCompletableFuture<>(new TerminateFutureContext(this));
    private final XCompletableFuture<?> runningFuture = new XCompletableFuture<>(new TerminateFutureContext(this));

    @SuppressWarnings("unused")
    private long p25, p26, p27, p28, p29, p30, p31, p32;
    // 填充结束

    public DefaultEventLoop(EventLoopBuilder.DefaultBuilder builder) {
        super(builder.getParent());

        WaitStrategy waitStrategy = Objects.requireNonNull(builder.getWaitStrategy(), "waitStrategy");
        ThreadFactory threadFactory = Objects.requireNonNull(builder.getThreadFactory(), "threadFactory");

        this.nanoTime = System.nanoTime();
        this.ringBuffer = new MpscUnboundedXaddArrayQueue2<>(Math.max(64, builder.getChunkSize()), builder.getMaxPooledChunks());
        this.sequencer = new MpscSequencer(waitStrategy, ringBuffer);
        this.translator = new Translator();

        this.scheduledTaskQueue = new DefaultIndexedPriorityQueue<>(XScheduledFutureTask::compareTo, 64);
        this.taskBatchSize = MathCommon.clamp(builder.getBatchSize(), MIN_BATCH_SIZE, MAX_BATCH_SIZE);
        this.rejectedExecutionHandler = Objects.requireNonNull(builder.getRejectedExecutionHandler());
        this.agent = Objects.requireNonNullElse(builder.getAgent(), EmptyAgent.getInstance());
        this.mainModule = builder.getMainModule();

        // 它不依赖于其它消费者，只依赖生产者的sequence
        worker = new Worker(sequencer.newBarrier());
        thread = Objects.requireNonNull(threadFactory.newThread(worker), "newThread");
        DefaultThreadFactory.checkUncaughtExceptionHandler(thread);

        // 完成绑定
        this.agent.inject(this);
    }

    // region 状态查询

    @Override
    public State getState() {
        return State.valueOf(state);
    }

    @Override
    public boolean isRunning() {
        return state == ST_RUNNING;
    }

    @Override
    public final boolean isShuttingDown() {
        return state >= ST_SHUTTING_DOWN;
    }

    @Override
    public final boolean isShutdown() {
        return state >= ST_SHUTDOWN;
    }

    @Override
    public final boolean isTerminated() {
        return state == ST_TERMINATED;
    }

    @Override
    public final boolean awaitTermination(long timeout, @Nonnull TimeUnit unit) throws InterruptedException {
        return terminationFuture().await(timeout, unit);
    }

    @Override
    public final ICompletableFuture<?> terminationFuture() {
        return terminationFuture;
    }

    @Override
    public ICompletableFuture<?> runningFuture() {
        return runningFuture;
    }

    @Override
    public final boolean inEventLoop() {
        return thread == Thread.currentThread();
    }

    @Override
    public final boolean inEventLoop(Thread thread) {
        return this.thread == thread;
    }

    @Override
    public final void wakeup() {
        if (!inEventLoop() && thread.isAlive()) {
            thread.interrupt();
            agent.wakeup();
        }
    }

    /**
     * 当前任务数
     * 注意：返回值是一个估算值
     */
    @Beta
    public int taskCount() {
        return ringBuffer.size();
    }

    /** EventLoop绑定的Agent（代理） */
    public EventLoopAgent getAgent() {
        return agent;
    }

    @Override
    public EventLoopModule mainModule() {
        return mainModule;
    }

    // endregion

    // region 任务提交

    @Override
    public void execute(@Nonnull Runnable task) {
        Objects.requireNonNull(task, "task");
        if (isShuttingDown()) {
            rejectedExecutionHandler.rejected(task, this);
            return;
        }
        ringBuffer.offerX(task, translator);
    }

    private class Translator implements MpscUnboundedXaddArrayQueue2.OfferHooker<Runnable> {

        @Override
        public Runnable translate(Runnable task, long sequence) {
            if (isShuttingDown()) {
                return _invalidRunnable;
            }
            if (task instanceof XScheduledFutureTask<?> futureTask) {
                futureTask.setId(sequence); // nice
                if (futureTask.isEnable(TaskFeature.LOW_PRIORITY)) {
                    futureTask.setQueueId(LOWER_PRIORITY_QUEUE_ID);
                }
            }
            return task;
        }

        @Override
        public void hook(Runnable srcEvent, Runnable destEvent, long sequence) {
            sequencer.signalAllWhenBlocking(); // 理论上发布失败的情况下可不调用该方法，不过概率小，优化的意义不大
            if (srcEvent != destEvent) {
                rejectedExecutionHandler.rejected(srcEvent, DefaultEventLoop.this);
            } else if (sequence == 0 && !inEventLoop()) {
                // 确保线程已启动 -- ringBuffer私有的情况下才可以测试 sequence == 0
                ensureThreadStarted();
            }
        }
    }

    @Override
    final void reSchedulePeriodic(XScheduledFutureTask<?> futureTask, boolean triggered) {
        assert inEventLoop();
        if (isShuttingDown()) {
            futureTask.cancel(false);
            return;
        }
        scheduledTaskQueue.add(futureTask);
    }

    @Override
    final void removeScheduled(XScheduledFutureTask<?> futureTask) {
        if (inEventLoop()) {
            scheduledTaskQueue.removeTyped(futureTask);
        } else {
            execute(() -> scheduledTaskQueue.removeTyped(futureTask));
        }
    }

    @Override
    protected final long nanoTime() {
        return nanoTime;
    }

    // endregion

    // region 线程状态切换

    @Override
    public ICompletableFuture<?> start() {
        ensureThreadStarted();
        return runningFuture;
    }

    @Override
    public void shutdown() {
        if (!runningFuture.isDone()) {
            FutureUtils.completeTerminationFuture(runningFuture, new StartFailedException("Shutdown"));
        }

        int expectedState = state;
        for (; ; ) {
            if (expectedState >= ST_SHUTTING_DOWN) {
                // 已被其它线程关闭
                return;
            }

            int realState = compareAndExchangeState(expectedState, ST_SHUTTING_DOWN);
            if (realState == expectedState) {
                // CAS成功，当前线程负责了关闭
                ensureThreadTerminable(expectedState);
                return;
            }
            // retry
            expectedState = realState;
        }
    }

    @Nonnull
    @Override
    public List<Runnable> shutdownNow() {
        shutdown();
        advanceRunState(ST_SHUTDOWN);
        // 这里不能操作ringBuffer中的数据，不能打破[多生产者单消费者]的架构
        return Collections.emptyList();
    }

    private void ensureThreadStarted() {
        if (state == ST_NOT_STARTED
                && STATE.compareAndSet(this, ST_NOT_STARTED, ST_STARTING)) {
            thread.start();
        }
    }

    private void ensureThreadTerminable(int oldState) {
        if (oldState == ST_NOT_STARTED) {
            // TODO 是否需要启动线程，进行更彻底的清理？
            state = ST_TERMINATED;

            FutureUtils.completeTerminationFuture(runningFuture, new StartFailedException("Termination"));
            FutureUtils.completeTerminationFuture(terminationFuture);
        } else {
            // 等待策略是根据alert信号判断EventLoop是否已开始关闭的，因此即使inEventLoop也需要alert，否则可能丢失信号，在waitFor处无法停止
            worker.sequenceBarrier.alert();
            // 唤醒线程 - 如果线程可能阻塞在其它地方
            wakeup();
        }
    }

    /**
     * 将运行状态转换为给定目标，或者至少保留给定状态。
     *
     * @param targetState 期望的目标状态
     */
    private void advanceRunState(int targetState) {
        int expectedState = state;
        for (; ; ) {
            if (expectedState >= targetState) {
                return;
            }
            int realState = compareAndExchangeState(expectedState, targetState);
            if (realState >= targetState) { // == 表示CAS成功， > 表示已进入目标状态
                return;
            }
            // retry
            expectedState = realState;
        }
    }

    private int compareAndExchangeState(int expectedState, int targetState) {
        return (int) STATE.compareAndExchange(this, expectedState, targetState);
    }

    // endregion

    private class Worker implements Runnable {

        private final SequenceBarrier sequenceBarrier;

        private Worker(SequenceBarrier sequenceBarrier) {
            this.sequenceBarrier = sequenceBarrier;
        }

        @Override
        public void run() {
            outer:
            try {
                if (runningFuture.isDone()) {
                    break outer;
                }

                nanoTime = System.nanoTime();
                agent.onStart();

                advanceRunState(ST_RUNNING);
                FutureUtils.completeTerminationFuture(runningFuture);

                if (runningFuture.isSucceeded()) {
                    loop();
                }
            } catch (Throwable e) {
                logger.error("thread exit due to exception!", e);
                if (!runningFuture.isDone()) { // 启动失败
                    FutureUtils.completeTerminationFuture(runningFuture, new StartFailedException("StartFailed", e));
                }
            } finally {
                // 如果是非正常退出，需要切换到正在关闭状态 - 告知其它线程，已经开始关闭
                advanceRunState(ST_SHUTTING_DOWN);
                if (!runningFuture.isSucceeded()) {
                    advanceRunState(ST_SHUTDOWN); // 启动失败直接进入清理状态，丢弃所有提交的任务
                }

                try {
                    // 清理ringBuffer中的数据
                    cleanRingBuffer();
                } finally {
                    // 标记为已进入最终清理阶段
                    advanceRunState(ST_SHUTDOWN);

                    // 退出前进行必要的清理，释放系统资源
                    try {
                        agent.onShutdown();
                    } catch (Throwable e) {
                        logger.error("thread clean caught exception!", e);
                    } finally {
                        // 设置为终止状态
                        state = ST_TERMINATED;
                        FutureUtils.completeTerminationFuture(terminationFuture);
                    }
                }
            }
        }

        private void loop() {
            final MpscUnboundedXaddArrayQueue2<Runnable> ringBuffer = DefaultEventLoop.this.ringBuffer;
            final SequenceBarrier sequenceBarrier = this.sequenceBarrier;
            final int taskBatchSize = DefaultEventLoop.this.taskBatchSize;

            long nextSequence;
            long availableSequence;
            long batchEndSequence;

            // 不使用while(true)避免有大量任务堆积的时候长时间无法退出
            while (!isShuttingDown()) {
                nanoTime = System.nanoTime();
                try {
                    // 等待生产者生产数据
                    nextSequence = ringBuffer.lvConsumerIndex();
                    availableSequence = sequenceBarrier.waitFor(nextSequence);

                    // 多生产者模型下不可频繁调用waitFor，会在查询可用sequence时产生巨大的开销，因此查询之后本地切割为小批次，避免用户循环得不到执行
                    while (nextSequence <= availableSequence) {
                        nanoTime = System.nanoTime();
                        processScheduledQueue(nanoTime, taskBatchSize, false);

                        batchEndSequence = Math.min(availableSequence, nextSequence + taskBatchSize - 1);
                        nextSequence = runTaskBatch(nextSequence, batchEndSequence) + 1;

                        if (nextSequence != batchEndSequence + 1) { // 未消费完毕，应当是开始退出了
                            assert isShuttingDown();
                            break;
                        }
                        invokeAgentUpdate();
                    }
                } catch (AlertException | InterruptedException e) {
                    // 请求了关闭 -- BatchEventProcessor实现中并没有处理等待过程中的中断异常
                    if (isShuttingDown()) {
                        break;
                    }
                } catch (TimeoutException e) {
                    // 优先先响应关闭，若未关闭，表用户主动退出等待，执行一次用户循环
                    if (isShuttingDown()) {
                        break;
                    }
                    nanoTime = System.nanoTime();
                    processScheduledQueue(nanoTime, taskBatchSize, false);
                    invokeAgentUpdate();
                } catch (Throwable e) {
                    // 不好的等待策略实现
                    logger.error("bad waitStrategy impl", e);
                }
            }
        }

        private void invokeAgentUpdate() {
            try {
                agent.update();
            } catch (Throwable t) {
                if (t instanceof VirtualMachineError) {
                    logger.error("loopOnce caught exception", t);
                } else {
                    logger.warn("loopOnce caught exception", t);
                }
            }
        }

        /**
         * 处理周期性任务，传入的限制只有在遇见低优先级任务的时候才生效，因此限制为0则表示遇见低优先级任务立即结束
         * (为避免时序错误，处理周期性任务期间不响应关闭，不容易安全实现)
         *
         * @param limit            批量执行的任务数限制
         * @param shuttingDownMode 是否是退出模式
         */
        private void processScheduledQueue(long tickTime, int limit, boolean shuttingDownMode) {
            final DefaultEventLoop eventLoop = DefaultEventLoop.this;
            final IndexedPriorityQueue<XScheduledFutureTask<?>> taskQueue = eventLoop.scheduledTaskQueue;

            long count = 0;
            XScheduledFutureTask<?> queueTask;
            while ((queueTask = taskQueue.peek()) != null) {
                if (queueTask.isDone()) {
                    taskQueue.poll(); // 未及时删除的任务
                    continue;
                }

                // 优先级最高的任务不需要执行，那么后面的也不需要执行
                if (tickTime < queueTask.getNextTriggerTime()) {
                    return;
                }

                int preQueueId = queueTask.getQueueId();
                taskQueue.poll();
                if (shuttingDownMode) {
                    // 关闭模式下，不执行低优先级任务，不再重复执行任务
                    if (preQueueId == LOWER_PRIORITY_QUEUE_ID || queueTask.trigger(tickTime)) {
                        queueTask.cancelWithoutRemove(false);
                    }
                } else {
                    // 非关闭模式下，检测批处理限制 -- 这里暂不响应关闭
                    count++;
                    if (queueTask.trigger(tickTime)) {
                        taskQueue.offer(queueTask);
                    }
                    if (preQueueId == LOWER_PRIORITY_QUEUE_ID && (count >= limit)) {
                        return;
                    }
                }
            }
        }

        /** @return curSequence */
        private long runTaskBatch(final long batchBeginSequence, final long batchEndSequence) {
            MpscUnboundedXaddArrayQueue2<Runnable> ringBuffer = DefaultEventLoop.this.ringBuffer;
            EventLoopAgent agent = DefaultEventLoop.this.agent;

            Runnable event;
            for (long curSequence = batchBeginSequence; curSequence <= batchEndSequence; curSequence++) {
                event = ringBuffer.poll();
                assert event != null;
                try {
                    if (event.getClass() == RingBufferEvent.class) { // 相对instanceOf更快
                        RingBufferEvent agentEvent = (RingBufferEvent) event;
                        if (agentEvent.getType() > 0) {
                            agent.onEvent(agentEvent);
                        } else {
                            agentEvent.castObj0ToRunnable().run();
                        }
                    } else if (event != _invalidRunnable) {
                        event.run();
                    } else {
                        if (isShuttingDown()) { // 生产者在观察到关闭时发布了不连续的数据
                            return curSequence;
                        }
                        logger.warn("user published invalid event: " + event); // 用户发布了非法数据
                    }
                } catch (Throwable t) {
                    logCause(t);
                    if (t instanceof InterruptedException && isShuttingDown()) {
                        return curSequence; // 响应关闭，避免丢失中断信号
                    }
                }
            }
            return batchEndSequence;
        }

        private void cleanRingBuffer() {
            final long startTimeMillis = System.currentTimeMillis();
            final MpscUnboundedXaddArrayQueue2<Runnable> ringBuffer = DefaultEventLoop.this.ringBuffer;
            final EventLoopAgent agent = DefaultEventLoop.this.agent;

            // 处理延迟任务
            nanoTime = System.nanoTime();
            processScheduledQueue(nanoTime, 0, true);
            scheduledTaskQueue.clearIgnoringIndexes();

            long taskCount = 0;
            long discardCount = 0;
            Runnable event;
            while ((event = ringBuffer.poll()) != null) {  // poll在有生产者发布数据时会进行阻塞
                taskCount++;
                if (isShutdown()) { // 如果已进入shutdown阶段，则直接丢弃任务
                    discardCount++;
                    continue;
                }
                try {
                    if (event.getClass() == RingBufferEvent.class) {
                        RingBufferEvent agentEvent = (RingBufferEvent) event;
                        if (agentEvent.getType() > 0) {
                            agent.onEvent(agentEvent);
                        } else {
                            agentEvent.castObj0ToRunnable().run();
                        }
                    } else if (event != _invalidRunnable) {
                        event.run();
                    }
                } catch (Throwable t) {
                    logCause(t);
                }
            }
            logger.info("cleanRingBuffer success! taskCount = {}, discardCount {}, cost timeMillis = {}",
                    taskCount, discardCount, (System.currentTimeMillis() - startTimeMillis));
        }

    }

    /** 生产者之间协调 -- 序号分配和发布 */
    private static class MpscSequencer extends Sequence implements Sequencer {

        final WaitStrategy waitStrategy;
        final MpscUnboundedXaddArrayQueue2<?> taskQueue;
        final CursorSequence cursorSequence;

        public MpscSequencer(WaitStrategy waitStrategy, MpscUnboundedXaddArrayQueue2<?> taskQueue) {
            this.waitStrategy = waitStrategy;
            this.taskQueue = taskQueue;
            this.cursorSequence = new CursorSequence(taskQueue);
        }

        @Override
        public final int getBufferSize() {
            return -1;
        }

        @Override
        public long remainingCapacity() {
            return -1;
        }

        /** 获取生产者的序号 */
        @Override
        public final long getCursor() {
            // 在Disruptor中，序号从-1开始，而JCTools中默认序号0，因此需要减1
            return taskQueue.lvProducerIndex() - 1;
        }

        /** 获取消费者的序号 */
        @Override
        public long getMinimumSequence() {
            return taskQueue.lvConsumerIndex() - 1;
        }

        /** 创建消费者用于等待的屏障 */
        @Override
        public SequenceBarrier newBarrier(Sequence... sequencesToTrack) {
            if (sequencesToTrack.length != 0) throw new IllegalArgumentException();
            return new MpscSequenceBarrier(this, waitStrategy, cursorSequence);
        }

        /** 唤醒阻塞的消费者 -- 即EventLoop；提交任务后应当调用该方法 */
        public void signalAllWhenBlocking() {
            waitStrategy.signalAllWhenBlocking();
        }

        @Override
        public long getHighestPublishedSequence(long nextSequence, long availableSequence) {
            // availableSequence就是cursor的序号，但JCTools并不支持查询后续序号是否可用 -- 实现了也没有意义
            // 因此我们只能返回availableSequence，并由EventLoop阻塞式等待对应的元素发布，即只能使用poll拉取元素
            return availableSequence;
        }

        // endregion

        // region 不会被调用和需要屏蔽的方法
        @Override
        public boolean hasAvailableCapacity(int requiredCapacity) {
            throw new UnsupportedOperationException();
        }

        @Override
        public long next() {
            throw new UnsupportedOperationException();
        }

        @Override
        public long next(int n) {
            throw new UnsupportedOperationException();
        }

        @Override
        public long tryNext() {
            throw new UnsupportedOperationException();
        }

        @Override
        public long tryNext(int n) {
            throw new UnsupportedOperationException();
        }

        @Override
        public void publish(long lo, long hi) {
            throw new UnsupportedOperationException();
        }

        @Override
        public void publish(long sequence) {
            throw new UnsupportedOperationException();
        }

        @Override
        public boolean isAvailable(long sequence) {
            throw new UnsupportedOperationException();
        }

        @Override
        public void claim(long sequence) {
            throw new UnsupportedOperationException();
        }

        /** 添加生产者需要等待的消费者序号 -- 该方法只有EventLoop调用 */
        @Override
        public final void addGatingSequences(Sequence... gatingSequences) {
            throw new UnsupportedOperationException();
        }

        /** 移除生产者需要等待的消费者序号 -- 该方法只有EventLoop调用 */
        @Override
        public boolean removeGatingSequence(Sequence sequence) {
            throw new UnsupportedOperationException();
        }

        @Override
        public <T> EventPoller<T> newPoller(DataProvider<T> provider, Sequence... gatingSequences) {
            throw new UnsupportedOperationException();
        }

        // endregion

    }

    /** 为等待策略提供获取生产者序号的途径 -- 等待策略是不能修改序号的 */
    private static class CursorSequence extends Sequence {

        final IndexedQueueSizeUtil.IndexedQueue taskQueue;

        private CursorSequence(IndexedQueueSizeUtil.IndexedQueue taskQueue) {
            this.taskQueue = taskQueue;
        }

        @Override
        public long get() {
            return taskQueue.lvProducerIndex() - 1;
        }

        @Override
        public void set(long value) {
            throw new UnsupportedOperationException();
        }

        @Override
        public void setVolatile(long value) {
            throw new UnsupportedOperationException();
        }

        @Override
        public boolean compareAndSet(long expectedValue, long newValue) {
            throw new UnsupportedOperationException();
        }

        @Override
        public long incrementAndGet() {
            throw new UnsupportedOperationException();
        }

        @Override
        public long addAndGet(long increment) {
            throw new UnsupportedOperationException();
        }

        @Override
        public String toString() {
            return "CursorSequence{" +
                    "index=" + get() +
                    "}";
        }
    }

    private static final VarHandle STATE;

    static {
        try {
            MethodHandles.Lookup l = MethodHandles.lookup();
            STATE = l.findVarHandle(DefaultEventLoop.class, "state", int.class);
        } catch (ReflectiveOperationException e) {
            throw new ExceptionInInitializerError(e);
        }

        // Reduce the risk of rare disastrous classloading in first call to
        // LockSupport.park: https://bugs.openjdk.java.net/browse/JDK-8074773
        Class<?> ensureLoaded = LockSupport.class;
    }
}
