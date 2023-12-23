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

package cn.wjybxx.common.async;

import cn.wjybxx.common.NegativeChecker;
import cn.wjybxx.common.ThreadUtils;
import cn.wjybxx.common.collect.DefaultIndexedPriorityQueue;
import cn.wjybxx.common.collect.IndexedElement;
import cn.wjybxx.common.collect.IndexedPriorityQueue;
import cn.wjybxx.common.concurrent.FutureUtils;
import cn.wjybxx.common.concurrent.TimeSharingContext;
import cn.wjybxx.common.concurrent.TimeSharingTask;
import cn.wjybxx.common.concurrent.TimeSharingTimeoutException;
import cn.wjybxx.common.time.TimeProvider;

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
    public ScheduledFluentFuture<?> scheduleRun(@Nonnull Runnable task, long timeout) {
        Objects.requireNonNull(task);
        timeout = initialDelayChecker.check(timeout);

        final ScheduledFutureTask<?> scheduledFutureTask = new ScheduledFutureTask<>(this, Executors.callable(task), null,
                ++sequencer, nextTriggerTime(timeout), 0);
        delayExecute(scheduledFutureTask);
        return scheduledFutureTask;
    }

    @Nonnull
    @Override
    public <V> ScheduledFluentFuture<V> scheduleCall(@Nonnull Callable<V> task, long timeout) {
        Objects.requireNonNull(task);
        timeout = initialDelayChecker.check(timeout);

        final ScheduledFutureTask<V> scheduledFutureTask = new ScheduledFutureTask<>(this, task, null,
                ++sequencer, nextTriggerTime(timeout), 0);
        delayExecute(scheduledFutureTask);
        return scheduledFutureTask;
    }

    @Nonnull
    @Override
    public ScheduledFluentFuture<?> scheduleWithFixedDelay(@Nonnull Runnable task, long initialDelay, long period) {
        Objects.requireNonNull(task);
        initialDelay = initialDelayChecker.check(initialDelay);
        ensurePeriodGreaterThanZero(period);

        final ScheduledFutureTask<?> scheduledFutureTask = new ScheduledFutureTask<>(this, Executors.callable(task), null,
                ++sequencer, nextTriggerTime(initialDelay), -period);
        delayExecute(scheduledFutureTask);
        return scheduledFutureTask;
    }

    @Nonnull
    @Override
    public ScheduledFluentFuture<?> scheduleAtFixedRate(@Nonnull Runnable task, long initialDelay, long period) {
        if (initialDelay < 0) {
            throw new IllegalArgumentException("fixedRate initialDelay < 0");
        }
        Objects.requireNonNull(task);
        ensurePeriodGreaterThanZero(period);

        final ScheduledFutureTask<?> scheduledFutureTask = new ScheduledFutureTask<>(this, Executors.callable(task), null,
                ++sequencer, nextTriggerTime(initialDelay), period);
        delayExecute(scheduledFutureTask);
        return scheduledFutureTask;
    }

    @Nonnull
    @Override
    public <V> ScheduledFluentFuture<V> timeSharingWithFixedDelay(@Nonnull TimeSharingTask<V> task, long initialDelay, long period,
                                                                  long timeout) {
        Objects.requireNonNull(task);
        initialDelay = initialDelayChecker.check(initialDelay);
        ensurePeriodGreaterThanZero(period);
        checkTimeSharingTimeout(timeout);

        final TimeSharingContext timeSharingContext = new TimeSharingContext(timeout, timeProvider.getTime());
        final ScheduledFutureTask<V> scheduledFutureTask = new ScheduledFutureTask<>(this, FutureUtils.toCallable(task), timeSharingContext,
                ++sequencer, nextTriggerTime(initialDelay), -period);
        delayExecute(scheduledFutureTask);
        return scheduledFutureTask;
    }

    @Nonnull
    @Override
    public <V> ScheduledFluentFuture<V> timeSharingAtFixedRate(@Nonnull TimeSharingTask<V> task, long initialDelay, long period,
                                                               long timeout) {
        if (initialDelay < 0) {
            throw new IllegalArgumentException("fixedRate initialDelay < 0");
        }
        Objects.requireNonNull(task);
        ensurePeriodGreaterThanZero(period);
        checkTimeSharingTimeout(timeout);

        final TimeSharingContext timeSharingContext = new TimeSharingContext(timeout, timeProvider.getTime());
        final ScheduledFutureTask<V> scheduledFutureTask = new ScheduledFutureTask<>(this, FutureUtils.toCallable(task), timeSharingContext,
                ++sequencer, nextTriggerTime(initialDelay), period);
        delayExecute(scheduledFutureTask);
        return scheduledFutureTask;
    }

    @Override
    public void execute(@Nonnull Runnable command) {
        scheduleRun(command, 0);
    }

    @Override
    public FluentFuture<?> submitRun(@Nonnull Runnable command) {
        return scheduleRun(command, 0);
    }

    @Override
    public <V> FluentFuture<V> submitCall(@Nonnull Callable<V> command) {
        return scheduleCall(command, 0);
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

    private static class ScheduledFutureTask<V> extends DefaultPromise<V> implements ScheduledFluentFuture<V>, Runnable,
            IndexedElement,
            Comparable<ScheduledFutureTask<?>> {

        private DefaultSameThreadScheduledExecutor executor;
        private Callable<V> task;
        private TimeSharingContext timeSharingContext;
        private final long taskId;

        /** 提前计算的，逻辑上的下次触发时间 */
        private long nextTriggerTime;
        /** 负数表示fixedDelay，正数表示fixedRate */
        private final long period;
        /** 队列中的下标缓存 */
        private int queueIndex = INDEX_NOT_FOUNT;

        ScheduledFutureTask(DefaultSameThreadScheduledExecutor executor, Callable<V> task,
                            TimeSharingContext timeSharingContext,
                            long taskId, long nextTriggerTime, long period) {
            this.executor = executor;
            this.task = task;
            this.timeSharingContext = timeSharingContext;
            this.taskId = taskId;

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
        public int collectionIndex(Object collection) {
            return queueIndex;
        }

        @Override
        public void collectionIndex(Object collection, int index) {
            queueIndex = index;
        }

        @Override
        public boolean cancel() {
            boolean canceled = super.cancel();
            if (canceled) {
                if (queueIndex >= 0) {
                    executor.remove(this);
                }
                clean();
            }
            return canceled;
        }

        private void clean() {
            executor = null;
            task = null;
            timeSharingContext = null;
        }

        @Override
        public final void run() {
            if (!trigger(executor.tickTime)) {
                clean();
            }
        }

        /** @return 是否还需要继续运行 */
        boolean trigger(long tickTime) {
            assert !isDone(); // 单线程下不应该出现
            try {
                Callable<V> task = this.task;
                if (period == 0) {
                    V result = task.call();
                    if (result != FutureUtils.CONTINUE) { // 得出结果
                        complete(result);
                    } else {
                        completeExceptionally(TimeSharingTimeoutException.INSTANCE);
                    }
                } else if (!isDone()) {
                    TimeSharingContext timeSharingContext = this.timeSharingContext;
                    if (timeSharingContext != null) {
                        timeSharingContext.beforeCall(tickTime, nextTriggerTime, period);
                    }
                    V result = task.call();
                    if (result != FutureUtils.CONTINUE && FutureUtils.isTimeSharing(task)) { // 周期性任务，只有分时任务可以有结果
                        complete(result);
                        return false;
                    }
                    if (!isDone()) { // 未被取消
                        if (timeSharingContext != null && timeSharingContext.isTimeout()) { // 超时
                            completeExceptionally(TimeSharingTimeoutException.INSTANCE);
                            return false;
                        }
                        setNextRunTime(tickTime, timeSharingContext);
                        return true;
                    } else {
                        return false;
                    }
                }
            } catch (Throwable ex) {
                ThreadUtils.recoveryInterrupted(ex);
                completeExceptionally(ex);
            }
            return false;
        }

        final long getNextTriggerTime() {
            return nextTriggerTime;
        }

        private void setNextRunTime(long tickTime, TimeSharingContext timeSharingContext) {
            long maxDelay = timeSharingContext != null ? timeSharingContext.getTimeLeft() : Long.MAX_VALUE;
            if (period > 0) {
                nextTriggerTime += Math.min(maxDelay, period);
            } else {
                nextTriggerTime = tickTime + Math.min(maxDelay, -period);
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

}