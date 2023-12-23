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

import java.util.Objects;
import java.util.concurrent.Callable;
import java.util.concurrent.TimeUnit;

/**
 * @author wjybxx
 * date 2023/4/14
 */
public final class ScheduleBuilder<V> {

    private static final byte TYPE_ONCE = 0;
    private static final byte TYPE_FIXED_DELAY = 1;
    private static final byte TYPE_FIXED_RATE = 2;

    private Callable<V> task;
    private int flags = TaskFeature.defaultFlags;

    private byte scheduleType = 0;
    private long initialDelay;
    private long period;
    private long timeout = -1;
    private TimeUnit timeUnit = TimeUnit.MILLISECONDS;

    private ScheduleBuilder(Callable<V> task) {
        this.task = task;
    }

    public static <V> ScheduleBuilder<V> newCallable(Callable<V> task) {
        Objects.requireNonNull(task);
        return new ScheduleBuilder<>(task);
    }

    public static ScheduleBuilder<Object> newRunnable(Runnable task) {
        return new ScheduleBuilder<>(FutureUtils.toCallable(task, null));
    }

    public static <V> ScheduleBuilder<V> newRunnable(Runnable task, V result) {
        return new ScheduleBuilder<>(FutureUtils.toCallable(task, result));
    }

    public static <V> ScheduleBuilder<V> newTimeSharing(TimeSharingTask<V> task) {
        return new ScheduleBuilder<>(FutureUtils.toCallable(task));
    }

    // region 调度方式

    public long getInitialDelay() {
        return initialDelay;
    }

    public long getPeriod() {
        return period;
    }

    public ScheduleBuilder<V> setOnlyOnce(long delay) {
        this.scheduleType = TYPE_ONCE;
        this.initialDelay = delay;
        this.period = 0;
        return this;
    }

    public boolean isOnlyOnce() {
        return scheduleType == TYPE_ONCE;
    }

    public ScheduleBuilder<V> setFixedDelay(long initialDelay, long period) {
        validatePeriod(period);
        this.scheduleType = TYPE_FIXED_DELAY;
        this.initialDelay = initialDelay;
        this.period = period;
        return this;
    }

    public boolean isFixedDelay() {
        return scheduleType == TYPE_FIXED_DELAY;
    }

    public ScheduleBuilder<V> setFixedRate(long initialDelay, long period) {
        validateInitialDelay(initialDelay);
        validatePeriod(period);
        this.scheduleType = TYPE_FIXED_RATE;
        this.initialDelay = initialDelay;
        this.period = period;
        return this;
    }

    public boolean isFixedRate() {
        return scheduleType == TYPE_FIXED_RATE;
    }

    /**
     * 设置周期性任务的超时时间（非分时任务也可以）
     * <p>
     * 注意：
     * 1. -1表示无限制，大于等于0表示有限制
     * 2. 我们总是在执行任务后检查是否超时，以确保至少会执行一次
     * 3. 超时是一个不准确的调度，不保证超时后能立即结束
     */
    public ScheduleBuilder<V> setTimeout(long timeout) {
        if (timeout < -1) {
            throw new IllegalArgumentException("invalid timeout " + timeout);
        }
        this.timeout = timeout;
        return this;
    }

    /**
     * 通过预估执行次数限制超时时间
     * 该方法对于fixedRate类型的任务有帮助
     */
    public ScheduleBuilder<V> setTimeoutByCount(int count) {
        if (count < 0) {
            throw new IllegalArgumentException("invalid count " + count);
        }
        // 这里需要max(0,)，否则可能使得timeout的值越界，initialDelay可能是小于0的
        if (count == 0) {
            this.timeout = Math.max(0, initialDelay);
        } else {
            this.timeout = Math.max(0, initialDelay + (count - 1) * period);
        }
        return this;
    }

    public long getTimeout() {
        return timeout;
    }

    public boolean hasTimeout() {
        return timeout != -1;
    }

    /**
     * 设置时间单位
     */
    public ScheduleBuilder<V> setTimeUnit(TimeUnit timeUnit) {
        this.timeUnit = Objects.requireNonNull(timeUnit);
        return this;
    }

    public TimeUnit getTimeUnit() {
        return timeUnit;
    }

    // endregion

    // region 开关特效

    public ScheduleBuilder<V> enable(TaskFeature feature) {
        this.flags = feature.setEnable(flags, true);
        return this;
    }

    public ScheduleBuilder<V> disable(TaskFeature feature) {
        this.flags = feature.setEnable(flags, false);
        return this;
    }

    // endregion

    // region 内部接口

    Callable<V> internal_getTask() {
        return task;
    }

    int internal_getFlags() {
        return flags;
    }

    /** 适用于禁止初始延迟小于0的情况 */
    static void validateInitialDelay(long initialDelay) {
        if (initialDelay < 0) {
            throw new IllegalArgumentException(
                    String.format("initialDelay: %d (expected: >= 0)", initialDelay));
        }
    }

    static void validatePeriod(long period) {
        if (period == 0) {
            throw new IllegalArgumentException("period: 0 (expected: != 0)");
        }
    }

    // endregion

}