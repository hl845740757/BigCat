/*
 * Copyright 2023 wjybxx
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

package cn.wjybxx.bigcat.common.async;

import cn.wjybxx.bigcat.common.ThreadUtils;
import cn.wjybxx.bigcat.common.collect.DefaultIndexedPriorityQueue;
import cn.wjybxx.bigcat.common.collect.IndexedPriorityQueue;
import cn.wjybxx.bigcat.common.concurrent.ResultHolder;
import cn.wjybxx.bigcat.common.concurrent.SucceededException;
import cn.wjybxx.bigcat.common.concurrent.TimeSharingCallable;
import cn.wjybxx.bigcat.common.concurrent.TimeSharingTimeoutException;
import cn.wjybxx.bigcat.common.time.TimeProvider;

import javax.annotation.Nonnull;
import java.util.Comparator;
import java.util.Objects;
import java.util.concurrent.Callable;
import java.util.concurrent.Executors;

/**
 * @author wjybxx
 * date 2023/4/3
 */
public class DefaultSameThreadScheduledExecutor implements SameThreadScheduledExecutor {

    private static final Comparator<ScheduledFutureTask<?>> queueTaskComparator = ScheduledFutureTask::compareTo;
    private static final int DEFAULT_INITIAL_CAPACITY = 16;

    private final TimeProvider timeProvider;
    private final NegativeChecker initialDelayChecker;
    private final IndexedPriorityQueue<ScheduledFutureTask<?>> taskQueue;

    /** 为任务分配唯一id，确保先入先出 */
    private long sequencer = 0;
    /** 当前帧的时间戳，缓存下来以避免在tick的过程中产生变化 */
    private long tickTime;
    /** 是否已关闭 */
    private boolean shutdown;

    DefaultSameThreadScheduledExecutor(TimeProvider timeProvider) {
        this(timeProvider, DEFAULT_INITIAL_CAPACITY);
    }

    DefaultSameThreadScheduledExecutor(TimeProvider timeProvider, int initCapacity) {
        this(timeProvider, initCapacity, null);
    }

    DefaultSameThreadScheduledExecutor(TimeProvider timeProvider, NegativeChecker initialDelayChecker) {
        this(timeProvider, DEFAULT_INITIAL_CAPACITY, initialDelayChecker);
    }

    /**
     * @param initialDelayChecker 初始延迟的兼容性；默认允许，保持强时序。
     */
    DefaultSameThreadScheduledExecutor(TimeProvider timeProvider, int initCapacity, NegativeChecker initialDelayChecker) {
        this.timeProvider = Objects.requireNonNull(timeProvider, "timeProvider");
        this.taskQueue = new DefaultIndexedPriorityQueue<>(queueTaskComparator, initCapacity);
        this.initialDelayChecker = Objects.requireNonNullElse(initialDelayChecker, NegativeChecker.ZERO);
    }

    @Nonnull
    @Override
    public ScheduledFluentFuture<?> scheduleRun(long timeout, @Nonnull Runnable task) {
        Objects.requireNonNull(task);
        timeout = initialDelayChecker.check(timeout);

        final ScheduledFutureTask<?> scheduledFutureTask = new ScheduledFutureTask<>(this, Executors.callable(task),
                ++sequencer, nextTriggerTime(timeout), 0);
        delayExecute(scheduledFutureTask);
        return scheduledFutureTask;
    }

    @Nonnull
    @Override
    public <V> ScheduledFluentFuture<V> scheduleCall(long timeout, @Nonnull Callable<V> task) {
        Objects.requireNonNull(task);
        timeout = initialDelayChecker.check(timeout);

        final ScheduledFutureTask<V> scheduledFutureTask = new ScheduledFutureTask<>(this, task,
                ++sequencer, nextTriggerTime(timeout), 0);
        delayExecute(scheduledFutureTask);
        return scheduledFutureTask;
    }

    @Nonnull
    @Override
    public ScheduledFluentFuture<?> scheduleWithFixedDelay(long initialDelay, long period, @Nonnull Runnable task) {
        Objects.requireNonNull(task);
        initialDelay = initialDelayChecker.check(initialDelay);
        ensurePeriodGreaterThanZero(period);

        final ScheduledFutureTask<?> scheduledFutureTask = new ScheduledFutureTask<>(this, Executors.callable(task),
                ++sequencer, nextTriggerTime(initialDelay), -period);
        delayExecute(scheduledFutureTask);
        return scheduledFutureTask;
    }

    @Nonnull
    @Override
    public ScheduledFluentFuture<?> scheduleAtFixedRate(long initialDelay, long period, @Nonnull Runnable task) {
        if (initialDelay < 0) {
            throw new IllegalArgumentException("fixedRate initialDelay < 0");
        }
        Objects.requireNonNull(task);
        ensurePeriodGreaterThanZero(period);

        final ScheduledFutureTask<?> scheduledFutureTask = new ScheduledFutureTask<>(this, Executors.callable(task),
                ++sequencer, nextTriggerTime(initialDelay), period);
        delayExecute(scheduledFutureTask);
        return scheduledFutureTask;
    }

    @Nonnull
    @Override
    public <V> ScheduledFluentFuture<V> timeSharingWithFixedDelay(long initialDelay, long period, @Nonnull TimeSharingCallable<V> task,
                                                                  long timeout) {
        Objects.requireNonNull(task);
        initialDelay = initialDelayChecker.check(initialDelay);
        ensurePeriodGreaterThanZero(period);
        checkTimeSharingTimeout(timeout);

        final TimeSharingContext<V> timeSharingContext = new TimeSharingContext<>(task, timeout, timeProvider.getTime());
        final ScheduledFutureTask<V> scheduledFutureTask = new ScheduledFutureTask<>(this, timeSharingContext,
                ++sequencer, nextTriggerTime(initialDelay), -period);
        delayExecute(scheduledFutureTask);
        return scheduledFutureTask;
    }

    @Nonnull
    @Override
    public <V> ScheduledFluentFuture<V> timeSharingAtFixedRate(long initialDelay, long period, @Nonnull TimeSharingCallable<V> task,
                                                               long timeout) {
        if (initialDelay < 0) {
            throw new IllegalArgumentException("fixedRate initialDelay < 0");
        }
        Objects.requireNonNull(task);
        ensurePeriodGreaterThanZero(period);
        checkTimeSharingTimeout(timeout);

        final TimeSharingContext<V> timeSharingContext = new TimeSharingContext<V>(task, timeout, timeProvider.getTime());
        final ScheduledFutureTask<V> scheduledFutureTask = new ScheduledFutureTask<>(this, timeSharingContext,
                ++sequencer, nextTriggerTime(initialDelay), period
        );
        delayExecute(scheduledFutureTask);
        return scheduledFutureTask;
    }

    @Override
    public void execute(@Nonnull Runnable command) {
        scheduleRun(0, command);
    }

    @Override
    public FluentFuture<?> submitRun(@Nonnull Runnable command) {
        return scheduleRun(0, command);
    }

    @Override
    public <V> FluentFuture<V> submitCall(@Nonnull Callable<V> command) {
        return scheduleCall(0, command);
    }

    @Override
    public boolean tick() {
        final IndexedPriorityQueue<ScheduledFutureTask<?>> taskQueue = this.taskQueue;
        if (taskQueue.isEmpty()) {
            return false;
        }

        // 需要缓存下来，一来用于task计算下次调度时间，一来避免优先级错乱
        final long curTime = timeProvider.getTime();
        tickTime = curTime;

        // 记录最后一个任务id，避免执行本次tick期间添加的任务
        long barrierTaskId = sequencer;
        ScheduledFutureTask<?> queueTask;

        while ((queueTask = taskQueue.peek()) != null) {
            // 优先级最高的任务不需要执行，那么后面的也不需要执行
            if (curTime < queueTask.getNextTriggerTime()) {
                return false;
            }
            // 本次tick期间新增的任务，不立即执行，避免死循环或占用过多cpu
            if (queueTask.taskId > barrierTaskId) {
                return true;
            }

            taskQueue.poll();
            queueTask.run();

            if (queueTask.isPeriodic() && !queueTask.isDone()) {
                if (isShutdown()) { // 已请求关闭
                    queueTask.cancel();
                } else {
                    taskQueue.offer(queueTask);
                }
            }
        }
        return false;
    }

    @Override
    public boolean isShutdown() {
        return shutdown;
    }

    @Override
    public void shutdown() {
        taskQueue.clear(); // 不调用优化方法，队列可能重新使用
        shutdown = true;
    }

    // region 内部实现

    private long nextTriggerTime(long delay) {
        final long r = timeProvider.getTime() + delay;
        if (delay > 0 && r < 0) { // 溢出
            throw new IllegalArgumentException(String.format("overflow, nextTriggerTime: %d, delay: %d", r, delay));
        }
        return r;
    }

    private void delayExecute(ScheduledFutureTask<?> queueTask) {
        if (isShutdown()) {
            // 默认直接取消，暂不添加拒绝处理器
            queueTask.cancel();
        } else {
            taskQueue.add(queueTask);
        }
    }

    private void remove(ScheduledFutureTask<?> queueTask) {
        taskQueue.removeTyped(queueTask);
    }

    private long curTime() {
        return timeProvider.getTime();
    }

    private static void ensurePeriodGreaterThanZero(long period) {
        if (period <= 0) {
            throw new IllegalArgumentException("period must be greater than 0");
        }
    }

    private void checkTimeSharingTimeout(long timeout) {
        if (timeout <= 0) {
            throw new IllegalArgumentException("timeout must be gte 0");
        }
    }

    static class ScheduledFutureTask<V> extends DefaultPromise<V> implements ScheduledFluentFuture<V>, Runnable,
            IndexedPriorityQueue.IndexedNode,
            Comparable<ScheduledFutureTask<?>> {

        private final DefaultSameThreadScheduledExecutor executor;
        private Callable<V> task;
        private final long taskId;

        /** 提前计算的，逻辑上的下次触发时间 */
        private long nextTriggerTime;
        /** 负数表示fixedDelay，正数表示fixedRate */
        private final long period;
        /** 队列中的下标缓存 */
        private int queueIndex = INDEX_NOT_IN_QUEUE;

        ScheduledFutureTask(DefaultSameThreadScheduledExecutor executor, Callable<V> task,
                            long taskId, long nextTriggerTime, long period) {
            this.executor = executor;
            this.taskId = taskId;
            this.task = task;

            this.nextTriggerTime = nextTriggerTime;
            this.period = period;
        }

        public long getPeriod() {
            return this.period;
        }

        public boolean isPeriodic() {
            return period != 0;
        }

        @Override
        public long getDelay() {
            return nextTriggerTime - executor.curTime();
        }

        @Override
        public int priorityQueueIndex(IndexedPriorityQueue<?> queue) {
            return queueIndex;
        }

        @Override
        public void priorityQueueIndex(IndexedPriorityQueue<?> queue, int index) {
            queueIndex = index;
        }

        @Override
        public boolean cancel() {
            boolean canceled = super.cancel();
            if (canceled) {
                task = null;
                if (queueIndex >= 0) {
                    executor.remove(this);
                }
            }
            return canceled;
        }

        @Override
        public final void run() {
            assert !isDone(); // 单线程下不应该出现
            try {
                if (period == 0) {
                    final V result = task.call();
                    trySuccess(result);
                } else {
                    if (task instanceof TimeSharingContext<V> timeSharingContext) {
                        timeSharingContext.beforeCall(executor.tickTime, nextTriggerTime, period);
                        final ResultHolder<V> holder = timeSharingContext.task.call();
                        timeSharingContext.afterCall(executor.tickTime, nextTriggerTime, period);

                        if (holder != null) { // 得到结果
                            trySuccess(holder.result);
                            return;
                        }
                        if (timeSharingContext.isTimeout()) {  // 时间片用尽
                            tryFailure(TimeSharingTimeoutException.INSTANCE);
                            return;
                        }
                    } else {
                        task.call();
                    }
                    updateNextTriggerTime();
                }
            } catch (SucceededException ex) {
                @SuppressWarnings("unchecked") V result = (V) ex.getResult();
                trySuccess(result);
            } catch (Throwable ex) {
                ThreadUtils.recoveryInterrupted(ex);
                tryFailure(ex);
            } finally {
                if (isDone()) {
                    task = null;
                }
            }
        }

        final long getNextTriggerTime() {
            return nextTriggerTime;
        }

        /** 任务执行一次之后，更新状态下次执行时间。 */
        void updateNextTriggerTime() {
            if (period > 0) {
                nextTriggerTime += period;
            } else {
                nextTriggerTime = executor.nextTriggerTime(-period);
            }
        }

        @Override
        public int compareTo(@Nonnull ScheduledFutureTask<?> that) {
            if (this == that) {
                return 0;
            }
            final int r1 = Long.compare(nextTriggerTime, that.nextTriggerTime);
            if (r1 != 0) {
                return r1;
            }
            return Long.compare(taskId, that.taskId);
        }

    }

    private static class TimeSharingContext<V> implements Callable<V> {

        Callable<ResultHolder<V>> task;
        /** 剩余时间 */
        long timeLeft;
        /** 上次触发时间，用于固定延迟下计算deltaTime */
        long lastTriggerTime;

        TimeSharingContext(Callable<ResultHolder<V>> task, long timeout, long initTime) {
            this.task = task;
            this.timeLeft = timeout;
            this.lastTriggerTime = initTime;
        }

        @Override
        public V call() {
            throw new AssertionError();
        }

        /**
         * @param realTriggerTime  真实触发时间 -- 真正被调度的时间
         * @param logicTriggerTime 逻辑触发时间 -- 调度前计算的应该被调度的时间
         * @see ScheduledFutureTask#updateNextTriggerTime()
         */
        void beforeCall(long realTriggerTime, long logicTriggerTime, long period) {
            // 逻辑触发时间的更新是基于lastTriggerTime算的，所以一定大于
            assert logicTriggerTime >= lastTriggerTime;
            if (period > 0) {
                timeLeft -= Math.max(0, logicTriggerTime - lastTriggerTime);
            } else {
                timeLeft -= Math.max(0, realTriggerTime - lastTriggerTime);
            }
        }

        void afterCall(long realTriggerTime, long logicTriggerTime, long period) {
            if (period > 0) {
                lastTriggerTime = logicTriggerTime;
            } else {
                lastTriggerTime = realTriggerTime;
            }
        }

        boolean isTimeout() {
            return timeLeft <= 0;
        }

    }

}