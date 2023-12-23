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
import cn.wjybxx.common.ThreadUtils;
import cn.wjybxx.common.annotation.Beta;
import cn.wjybxx.common.collect.DefaultIndexedPriorityQueue;
import cn.wjybxx.common.collect.IndexedPriorityQueue;
import com.lmax.disruptor.*;

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
 * 基于Disruptor框架的事件循环。
 * 这个实现持有私有的RingBuffer，可以有最好的性能。
 * <p>
 * 关于时序正确性：
 * 1.由于{@link #scheduledTaskQueue}的任务都是从{@link #ringBuffer}中拉取出来的，因此都是先于{@link #ringBuffer}中剩余的任务的。
 * 2.我们总是先取得一个时间快照，然后先执行{@link #scheduledTaskQueue}中的任务，再执行{@link #ringBuffer}中的任务，因此满足优先级相同时，先提交的任务先执行的约定
 * -- 反之，如果不使用时间快照，就可能导致后提交的任务先满足触发时间。
 *
 * @author wjybxx
 * date 2023/4/10
 */
public class DisruptorEventLoop extends AbstractScheduledEventLoop {

    private static final int MIN_BATCH_SIZE = 64;
    private static final int MAX_BATCH_SIZE = 64 * 1024;
    private static final int BATCH_PUBLISH_THRESHOLD = 1024;

    private static final int HIGHER_PRIORITY_QUEUE_ID = 0;
    private static final int LOWER_PRIORITY_QUEUE_ID = 1;

    private static final int TYPE_CLEAN_DEADLINE = -2;
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
    private final RingBuffer<RingBufferEvent> ringBuffer;
    /** 周期性任务队列 -- 都是先于RingBuffer中的任务提交的 -- 暂不提供带缓存行填充的实现 */
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

    public DisruptorEventLoop(EventLoopBuilder.DisruptorBuilder builder) {
        super(builder.getParent());

        WaitStrategy waitStrategy = Objects.requireNonNull(builder.getWaitStrategy(), "waitStrategy");
        ThreadFactory threadFactory = Objects.requireNonNull(builder.getThreadFactory(), "threadFactory");

        this.nanoTime = System.nanoTime();
        this.ringBuffer = RingBuffer.createMultiProducer(RingBufferEvent::new,
                builder.getRingBufferSize(),
                waitStrategy);
        this.scheduledTaskQueue = new DefaultIndexedPriorityQueue<>(XScheduledFutureTask::compareTo, 64);
        this.taskBatchSize = MathCommon.clamp(builder.getBatchSize(), MIN_BATCH_SIZE, MAX_BATCH_SIZE);
        this.rejectedExecutionHandler = Objects.requireNonNull(builder.getRejectedExecutionHandler());
        this.agent = Objects.requireNonNullElse(builder.getAgent(), EmptyAgent.getInstance());
        this.mainModule = builder.getMainModule();

        // 它不依赖于其它消费者，只依赖生产者的sequence
        worker = new Worker(ringBuffer.newBarrier());
        thread = Objects.requireNonNull(threadFactory.newThread(worker), "newThread");
        DefaultThreadFactory.checkUncaughtExceptionHandler(thread);

        // 添加worker的sequence为网关sequence，生产者们会监听到线程的消费进度
        ringBuffer.addGatingSequences(worker.sequence);

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
     * 注意：返回值是一个估算值！
     */
    @Beta
    public int taskCount() {
        long count = ringBuffer.getCursor() - worker.sequence.get();
        if (count >= ringBuffer.getBufferSize()) {
            return ringBuffer.getBufferSize();
        }
        return Math.max(0, (int) count);
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
        if (inEventLoop()) {
            // 当前线程调用，需要使用tryNext以避免死锁
            try {
                tryPublish(task, ringBuffer.tryNext(1));
            } catch (InsufficientCapacityException ignore) {
                rejectedExecutionHandler.rejected(task, this);
            }
        } else {
            // 其它线程调用，可能阻塞
            tryPublish(task, ringBuffer.next(1));
        }
    }

    /**
     * Q: 如何保证算法的安全性的？
     * A: 我们只需要保证申请到的sequence是有效的，且发布任务在{@link Worker#removeFromGatingSequence()}之前即可。
     * 因为{@link Worker#removeFromGatingSequence()}之前申请到的sequence一定是有效的，它考虑了EventLoop的消费进度。
     * <p>
     * 关键时序：
     * 1. {@link #isShuttingDown()}为true一定在{@link Worker#cleanRingBuffer()}之前。
     * 2. {@link Worker#cleanRingBuffer()}必须等待在这之前申请到的sequence发布。
     * 3. {@link Worker#cleanRingBuffer()}在所有生产者发布数据之后才{@link Worker#removeFromGatingSequence()}
     * <p>
     * 因此，{@link Worker#cleanRingBuffer()}之前申请到的sequence是有效的；
     * 又因为{@link #isShuttingDown()}为true一定在{@link Worker#cleanRingBuffer()}之前，
     * 因此，如果sequence是在{@link #isShuttingDown()}为true之前申请到的，那么sequence一定是有效的，否则可能有效，也可能无效。
     */
    private void tryPublish(@Nonnull Runnable task, long sequence) {
        if (isShuttingDown()) {
            // 先发布sequence，避免拒绝逻辑可能产生的阻塞，不可以覆盖数据
            ringBuffer.publish(sequence);
            rejectedExecutionHandler.rejected(task, this);
        } else {
            RingBufferEvent event = ringBuffer.get(sequence);
            if (task.getClass() == RingBufferEvent.class) { // 相对instanceof更快
                RingBufferEvent userEvent = (RingBufferEvent) task;
                event.copyFrom(userEvent);
            } else {
                event.internal_setType(0);
                event.obj0 = task;
                if (task instanceof XScheduledFutureTask<?> futureTask) {
                    futureTask.setId(sequence); // nice
                    if (futureTask.isEnable(TaskFeature.LOW_PRIORITY)) {
                        futureTask.setQueueId(LOWER_PRIORITY_QUEUE_ID);
                    }
                }
            }
            ringBuffer.publish(sequence);

            // 确保线程已启动 -- ringBuffer私有的情况下才可以测试 sequence == 0
            if (sequence == 0 && !inEventLoop()) {
                ensureThreadStarted();
            }
        }
    }

    public final RingBufferEvent getEvent(long sequence) {
        checkSequence(sequence);
        return ringBuffer.get(sequence);
    }

    private static void checkSequence(long sequence) {
        if (sequence < 0) {
            throw new IllegalArgumentException("invalid sequence " + sequence);
        }
    }

    /**
     * 开放的特殊接口
     * 1.按照规范，在调用该方法后，必须在finally块中进行发布。
     * 2.事件类型必须大于等于0，否则可能导致异常
     * 3.返回值为-1时必须检查
     * <pre> {@code
     *      long sequence = eventLoop.nextSequence();
     *      try {
     *          RingBufferEvent event = eventLoop.getEvent(sequence);
     *          // Do work.
     *      } finally {
     *          eventLoop.publish(sequence)
     *      }
     * }</pre>
     *
     * @return 如果申请成功，则返回对应的sequence，否则返回 -1
     */
    @Beta
    public final long nextSequence() {
        return nextSequence(1);
    }

    @Beta
    public final void publish(long sequence) {
        checkSequence(sequence);
        ringBuffer.publish(sequence);
        if (sequence == 0 && !inEventLoop()) {
            ensureThreadStarted();
        }
    }

    /**
     * 1.按照规范，在调用该方法后，必须在finally块中进行发布。
     * 2.事件类型必须大于等于0，否则可能导致异常
     * 3.返回值为-1时必须检查
     * <pre>{@code
     *   int n = 10;
     *   long hi = eventLoop.nextSequence(n);
     *   try {
     *      long lo = hi - (n - 1);
     *      for (long sequence = lo; sequence <= hi; sequence++) {
     *          RingBufferEvent event = eventLoop.getEvent(sequence);
     *          // Do work.
     *      }
     *   } finally {
     *      eventLoop.publish(lo, hi);
     *   }
     * }</pre>
     *
     * @param size 申请的空间大小
     * @return 如果申请成功，则返回申请空间的最大序号，否则返回-1
     */
    @Beta
    public final long nextSequence(int size) {
        if (isShuttingDown()) {
            return -1;
        }
        long sequence;
        if (inEventLoop()) {
            try {
                sequence = ringBuffer.tryNext(size);
            } catch (InsufficientCapacityException ignore) {
                return -1;
            }
        } else {
            sequence = ringBuffer.next(size);
        }
        if (isShuttingDown()) {
            // sequence不一定有效了，申请的全部序号都要发布
            long lo = sequence - (size - 1);
            ringBuffer.publish(lo, sequence);
            return -1;
        }
        return sequence;
    }

    /**
     * @param lo inclusive
     * @param hi inclusive
     */
    @Beta
    public final void publish(long lo, long hi) {
        checkSequence(lo);
        ringBuffer.publish(lo, hi);
        if (lo == 0 && !inEventLoop()) {
            ensureThreadStarted();
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
        }
        // else 等待任务超时弹出时再删除 -- 延迟删除可能存在内存泄漏，但压任务又可能导致阻塞（有界队列）
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
            worker.removeFromGatingSequence(); // 防死锁

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

    /**
     * 实现{@link RingBuffer}的消费者，实现基本和{@link BatchEventProcessor}一致。
     * 但解决了两个问题：
     * 1. 生产者调用{@link RingBuffer#next()}时，如果消费者已关闭，则会死锁！为避免死锁不得不使用{@link RingBuffer#tryNext()}，但是那样的代码并不友好。
     * 2. 内存泄漏问题，使用{@link BatchEventProcessor}在关闭时无法清理{@link RingBuffer}中的数据。
     */
    private class Worker implements Runnable {

        private final Sequence sequence = new Sequence(Sequencer.INITIAL_CURSOR_VALUE);
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
            final SequenceBarrier sequenceBarrier = this.sequenceBarrier;
            final int taskBatchSize = DisruptorEventLoop.this.taskBatchSize;

            final Sequence sequence = this.sequence;
            long nextSequence = sequence.get() + 1L;
            long availableSequence;
            long batchEndSequence;

            // 不使用while(true)避免有大量任务堆积的时候长时间无法退出
            while (!isShuttingDown()) {
                nanoTime = System.nanoTime();
                try {
                    // 等待生产者生产数据
                    availableSequence = sequenceBarrier.waitFor(nextSequence);

                    // 多生产者模型下不可频繁调用waitFor，会在查询可用sequence时产生巨大的开销，因此查询之后本地切割为小批次，避免用户循环得不到执行
                    while (nextSequence <= availableSequence) {
                        nanoTime = System.nanoTime();
                        processScheduledQueue(nanoTime, taskBatchSize, false);

                        batchEndSequence = Math.min(availableSequence, nextSequence + taskBatchSize - 1);
                        nextSequence = runTaskBatch(nextSequence, batchEndSequence) + 1;
                        sequence.set(nextSequence - 1);

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
            final DisruptorEventLoop eventLoop = DisruptorEventLoop.this;
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
            RingBuffer<RingBufferEvent> ringBuffer = DisruptorEventLoop.this.ringBuffer;
            EventLoopAgent agent = DisruptorEventLoop.this.agent;
            RingBufferEvent event;
            for (long curSequence = batchBeginSequence; curSequence <= batchEndSequence; curSequence++) {
                event = ringBuffer.get(curSequence);
                try {
                    if (event.getType() > 0) {
                        agent.onEvent(event);
                    } else if (event.getType() == 0) {
                        event.castObj0ToRunnable().run();
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
                } finally {
                    event.clean();
                }

                // 避免长时间不发布sequence阻塞生产者，最后一个sequence外部发布
                if (((curSequence - batchBeginSequence) & BATCH_PUBLISH_THRESHOLD) == 0
                        && curSequence < batchEndSequence) {
                    sequence.set(curSequence);
                }
            }
            return batchEndSequence;
        }

        /**
         * 这是解决死锁问题的关键，如果不从gatingSequence中移除，则{@link RingBuffer#next(int)} 方法可能死锁。
         * 该方法是线程安全的
         */
        private void removeFromGatingSequence() {
            ringBuffer.removeGatingSequence(sequence);
        }

        private void cleanRingBuffer() {
            final long startTimeMillis = System.currentTimeMillis();
            final RingBuffer<RingBufferEvent> ringBuffer = DisruptorEventLoop.this.ringBuffer;
            final EventLoopAgent agent = DisruptorEventLoop.this.agent;

            // 处理延迟任务
            nanoTime = System.nanoTime();
            processScheduledQueue(nanoTime, 0, true);
            scheduledTaskQueue.clearIgnoringIndexes();

            // 当所有的槽位都填充后，所有生产者将阻塞，此时可以删除gatingSequence
            // 当生产者获取到新的sequence后，将观察到线程处于关闭状态，从而避免破坏数据
            long nextSequence = sequence.get() + 1;
            long lastSequence = sequence.get() + ringBuffer.getBufferSize();
            waitAllSequencePublished(ringBuffer, nextSequence, lastSequence);
            removeFromGatingSequence();

            // 由于所有的数据都是受保护的，不会被覆盖，因此可以继续消费
            long nullCount = 0;
            long taskCount = 0;
            long discardCount = 0;
            for (; nextSequence <= lastSequence; nextSequence++) {
                final RingBufferEvent event = ringBuffer.get(nextSequence);
                if (event.getType() == TYPE_CLEAN_DEADLINE) { // 后面都是空白区
                    break;
                }
                if (event.getType() < 0) { // 生产者在观察到关闭时发布了不连续的数据
                    nullCount++;
                    continue;
                }
                taskCount++;
                if (isShutdown()) { // 如果已进入shutdown阶段，则直接丢弃任务
                    discardCount++;
                    event.cleanAll();
                    continue;
                }
                try {
                    if (event.getType() > 0) {
                        agent.onEvent(event);
                    } else {
                        event.castObj0ToRunnable().run();
                    }
                } catch (Throwable t) {
                    logCause(t);
                } finally {
                    event.cleanAll();
                }
            }
            sequence.set(lastSequence);
            logger.info("cleanRingBuffer success!  nullCount = {}, taskCount = {}, discardCount {}, cost timeMillis = {}",
                    nullCount, taskCount, discardCount, (System.currentTimeMillis() - startTimeMillis));
        }

        @SuppressWarnings("deprecation")
        private void waitAllSequencePublished(RingBuffer<RingBufferEvent> ringBuffer, long nextSequence, long lastSequence) {
            long highestPublishedSequence = nextSequence - 1;
            while (true) {
                // 必须保证发布的连续性
                while (ringBuffer.isPublished(highestPublishedSequence + 1)) {
                    highestPublishedSequence++;
                }
                // 真实的生产者发布了该序号
                if (highestPublishedSequence == lastSequence) {
                    return;
                }

                long cursor = ringBuffer.getCursor();
                if (highestPublishedSequence != cursor) { // 已发布区间不连续
                    ThreadUtils.sleepQuietly(1);
                    continue;
                }
                int size = Math.toIntExact(lastSequence - cursor);
                if (size < 1) { // 其它生产者将填充
                    ThreadUtils.sleepQuietly(1);
                    continue;
                }
                try {
                    // 消费者不再更新的情况下，申请成功就应该是全部
                    long seq = ringBuffer.tryNext(size);
                    RingBufferEvent event = ringBuffer.get(cursor + 1);
                    event.internal_setType(TYPE_CLEAN_DEADLINE);
                    ringBuffer.publish(seq);
                    return;
                } catch (InsufficientCapacityException ignore) {
                    ThreadUtils.sleepQuietly(1);
                }
            }
        }
    }

    private static final VarHandle STATE;

    static {
        try {
            MethodHandles.Lookup l = MethodHandles.lookup();
            STATE = l.findVarHandle(DisruptorEventLoop.class, "state", int.class);
        } catch (ReflectiveOperationException e) {
            throw new ExceptionInInitializerError(e);
        }

        // Reduce the risk of rare disastrous classloading in first call to
        // LockSupport.park: https://bugs.openjdk.java.net/browse/JDK-8074773
        Class<?> ensureLoaded = LockSupport.class;
    }
}
