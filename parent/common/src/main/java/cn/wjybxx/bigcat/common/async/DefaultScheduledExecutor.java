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

import cn.wjybxx.bigcat.common.collect.DefaultIndexedPriorityQueue;
import cn.wjybxx.bigcat.common.collect.IndexedPriorityQueue;
import cn.wjybxx.bigcat.common.time.TimeProvider;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import javax.annotation.Nonnull;
import java.util.Comparator;
import java.util.Objects;
import java.util.concurrent.Callable;
import java.util.concurrent.Executors;

/**
 * @author wjybxx
 * date 2023/4/3
 */
public class DefaultScheduledExecutor implements SameThreadScheduledExecutor {

    private static final Logger logger = LoggerFactory.getLogger(DefaultScheduledExecutor.class);

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

    public DefaultScheduledExecutor(TimeProvider timeProvider) {
        this(timeProvider, DEFAULT_INITIAL_CAPACITY);
    }

    public DefaultScheduledExecutor(TimeProvider timeProvider, int initCapacity) {
        this(timeProvider, initCapacity, null);
    }

    public DefaultScheduledExecutor(TimeProvider timeProvider, NegativeChecker initialDelayChecker) {
        this(timeProvider, DEFAULT_INITIAL_CAPACITY, initialDelayChecker);
    }

    /**
     * @param initialDelayChecker 初始延迟的兼容性；默认允许，保持强时序。
     */
    public DefaultScheduledExecutor(TimeProvider timeProvider, int initCapacity, NegativeChecker initialDelayChecker) {
        this.timeProvider = Objects.requireNonNull(timeProvider, "timeProvider");
        this.taskQueue = new DefaultIndexedPriorityQueue<>(queueTaskComparator, initCapacity);
        this.initialDelayChecker = Objects.requireNonNullElse(initialDelayChecker, NegativeChecker.SUCCESS);
    }

    @Nonnull
    @Override
    public ScheduledFluentFuture<?> scheduleRun(long timeout, @Nonnull Runnable task) {
        Objects.requireNonNull(task);
        final ScheduledFutureTask<?> scheduledFutureTask = new ScheduledFutureTask<>(this, ++sequencer,
                Executors.callable(task), ScheduleType.ONCE, 0,
                null, nextTriggerTime(timeout));
        delayExecute(scheduledFutureTask);
        return scheduledFutureTask;
    }

    @Nonnull
    @Override
    public <V> ScheduledFluentFuture<V> scheduleCall(long timeout, @Nonnull Callable<V> task) {
        Objects.requireNonNull(task);
        final ScheduledFutureTask<V> scheduledFutureTask = new ScheduledFutureTask<>(this, ++sequencer,
                task, ScheduleType.ONCE, 0,
                null, nextTriggerTime(timeout));
        delayExecute(scheduledFutureTask);
        return scheduledFutureTask;
    }

    @Nonnull
    @Override
    public ScheduledFluentFuture<?> scheduleFixedDelay(long initialDelay, long period, @Nonnull Runnable task) {
        Objects.requireNonNull(task);
        initialDelay = checkFirstDelay(initialDelay);
        ensurePeriodGreaterThanZero(period);

        final ScheduledFutureTask<?> scheduledFutureTask = new ScheduledFutureTask<>(this, ++sequencer,
                Executors.callable(task), ScheduleType.FIXED_DELAY, period,
                null, nextTriggerTime(initialDelay));
        delayExecute(scheduledFutureTask);
        return scheduledFutureTask;
    }

    @Nonnull
    @Override
    public ScheduledFluentFuture<?> scheduleFixedRate(long initialDelay, long period, @Nonnull Runnable task) {
        Objects.requireNonNull(task);
        initialDelay = checkFirstDelay(initialDelay);
        ensurePeriodGreaterThanZero(period);

        final ScheduledFutureTask<?> scheduledFutureTask = new ScheduledFutureTask<>(this, ++sequencer,
                Executors.callable(task), ScheduleType.FIXED_RATE, period,
                null, nextTriggerTime(initialDelay));
        delayExecute(scheduledFutureTask);
        return scheduledFutureTask;
    }

    @Nonnull
    @Override
    public <V> ScheduledFluentFuture<V> timeSharingFixedDelay(long initialDelay, long period, @Nonnull TimeSharingCallable<V> task,
                                                              long timeout) {
        Objects.requireNonNull(task);
        initialDelay = checkFirstDelay(initialDelay);
        ensurePeriodGreaterThanZero(period);
        checkTimeSharingTimeout(timeout);

        final TimeSharingContext timeSharingContext = new TimeSharingContext(timeout, timeProvider.getTime());
        final ScheduledFutureTask<V> scheduledFutureTask = new ScheduledFutureTask<>(this, ++sequencer,
                task, ScheduleType.FIXED_DELAY, period,
                timeSharingContext, nextTriggerTime(initialDelay));
        delayExecute(scheduledFutureTask);
        return scheduledFutureTask;
    }

    @Nonnull
    @Override
    public <V> ScheduledFluentFuture<V> timeSharingFixedRate(long initialDelay, long period, @Nonnull TimeSharingCallable<V> task,
                                                             long timeout) {
        Objects.requireNonNull(task);
        initialDelay = checkFirstDelay(initialDelay);
        ensurePeriodGreaterThanZero(period);
        checkTimeSharingTimeout(timeout);

        final TimeSharingContext timeSharingContext = new TimeSharingContext(timeout, timeProvider.getTime());
        final ScheduledFutureTask<V> scheduledFutureTask = new ScheduledFutureTask<>(this, ++sequencer,
                task, ScheduleType.FIXED_RATE, period,
                timeSharingContext, nextTriggerTime(initialDelay));
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

        // 需要缓存下来，以供新增任务的时候判断
        final long curTime = timeProvider.getTime();
        tickTime = curTime;

        // 记录最后一个任务id，避免执行本次tick期间添加的任务
        long barrierTaskId = sequencer;
        ScheduledFutureTask<?> queueTask;
        try {
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
        } finally {
            tickTime = 0;
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
            if (tickTime > 0 && queueTask.getNextTriggerTime() < tickTime) {
                // 新任务插在既有要执行的任务前面，打印警告
                logger.warn("the nextTriggerTime of newTask is less than curTickTime, taskInfo " + queueTask.task);
            }
        }
    }

    private void remove(ScheduledFutureTask<?> queueTask) {
        taskQueue.removeTyped(queueTask);
    }

    private long curTime() {
        return timeProvider.getTime();
    }

    private long checkFirstDelay(long firstDelay) {
        return initialDelayChecker.check(firstDelay);
    }

    private static void ensurePeriodGreaterThanZero(long period) {
        if (period <= 0) {
            throw new IllegalArgumentException("period must be greater than 0");
        }
    }

    private void checkTimeSharingTimeout(long timeout) {
        // 允许首次延迟小于0会带来许多复杂度
        if (timeout <= 0) {
            throw new IllegalArgumentException("timeout must be gte 0");
        }
    }

    static class ScheduledFutureTask<V> extends DefaultPromise<V> implements ScheduledFluentFuture<V>, Runnable,
            IndexedPriorityQueue.IndexedNode,
            Comparable<ScheduledFutureTask<?>> {

        private final DefaultScheduledExecutor executor;
        private final long taskId;

        private Callable<?> task;
        private ScheduleType scheduleType;
        private long period;
        private TimeSharingContext timeSharingContext;

        /** 下次执行的时间 */
        private long nextTriggerTime;
        /** 在队列中下标 */
        private int queueIndex = INDEX_NOT_IN_QUEUE;

        ScheduledFutureTask(DefaultScheduledExecutor executor, long taskId,
                            Callable<?> task, ScheduleType scheduleType, long period,
                            TimeSharingContext timeSharingContext,
                            long nextTriggerTime) {
            this.executor = executor;
            this.taskId = taskId;

            this.task = task;
            this.scheduleType = scheduleType;
            this.period = period;
            this.timeSharingContext = timeSharingContext;

            this.nextTriggerTime = nextTriggerTime;
        }

        public ScheduleType getScheduleType() {
            return scheduleType;
        }

        public long getPeriod() {
            return this.period;
        }

        public boolean isPeriodic() {
            return period != 0;
        }

        @Override
        public long getDelay() {
            return Math.max(0, nextTriggerTime - executor.curTime());
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
            assert !isDone();
            try {
                if (period == 0) {
                    @SuppressWarnings("unchecked") final V result = (V) task.call();
                    trySuccess(result);
                } else {
                    final TimeSharingContext timeSharingContext = this.timeSharingContext;
                    if (timeSharingContext != null) {
                        timeSharingContext.beforeCall(executor.tickTime, nextTriggerTime, scheduleType);
                        final ResultHolder<V> holder;
                        try {
                            @SuppressWarnings("unchecked") final TimeSharingCallable<V> realTask = (TimeSharingCallable<V>) task;
                            holder = realTask.call();
                        } finally {
                            timeSharingContext.afterCall(executor.tickTime, nextTriggerTime, scheduleType); // 在赋值结果前更新上下文
                        }
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
            } catch (Throwable e) {
                tryFailure(e);
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
            if (scheduleType == ScheduleType.FIXED_RATE) {
                nextTriggerTime += period;
            } else {
                nextTriggerTime = executor.nextTriggerTime(period);
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

    private static class TimeSharingContext {

        /** 剩余时间 */
        long timeLeft;
        /** 上次触发时间，用于固定延迟下计算deltaTime */
        long lastTriggerTime;

        TimeSharingContext(long timeout, long lastTriggerTime) {
            this.timeLeft = timeout;
            this.lastTriggerTime = lastTriggerTime;
        }

        /**
         * @param realTriggerTime  真实触发时间 -- 真正被调度的时间
         * @param logicTriggerTime 逻辑触发时间 -- 调度前计算的应该被调度的时间
         * @see ScheduledFutureTask#updateNextTriggerTime()
         */
        void beforeCall(long realTriggerTime, long logicTriggerTime, ScheduleType scheduleType) {
            // 逻辑触发时间的更新是基于lastTriggerTime算的，所以一定大于
            assert logicTriggerTime >= lastTriggerTime;
            if (scheduleType == ScheduleType.FIXED_RATE) {
                timeLeft -= Math.max(0, logicTriggerTime - lastTriggerTime);
            } else {
                timeLeft -= Math.max(0, realTriggerTime - lastTriggerTime);
            }
        }

        void afterCall(long realTriggerTime, long logicTriggerTime, ScheduleType scheduleType) {
            if (scheduleType == ScheduleType.FIXED_RATE) {
                lastTriggerTime = logicTriggerTime;
            } else {
                lastTriggerTime = realTriggerTime;
            }
        }

        boolean isTimeout() {
            return timeLeft <= 0;
        }

    }

    private enum ScheduleType {

        ONCE(0),

        FIXED_DELAY(1),

        FIXED_RATE(2),
        ;

        private final int number;

        ScheduleType(int number) {
            this.number = number;
        }

        public int getNumber() {
            return number;
        }
    }
}