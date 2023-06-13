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

import javax.annotation.Nullable;
import java.util.Objects;
import java.util.concurrent.Callable;
import java.util.concurrent.TimeUnit;

/**
 * @author wjybxx
 * date - 2023/4/7
 */
abstract class AbstractScheduledEventLoop extends AbstractEventLoop {

    public AbstractScheduledEventLoop(@Nullable EventLoopGroup parent) {
        super(parent);
    }

    public AbstractScheduledEventLoop(EventLoopGroup parent, EventLoopFutureContext futureContext) {
        super(parent, futureContext);
    }

    @Override
    public IScheduledFuture<?> schedule(Runnable command, long delay, TimeUnit unit) {
        Objects.requireNonNull(command);
        Objects.requireNonNull(unit);
        delay = Math.max(0, delay);

        return schedule(XScheduledFutureTask.ofRunnable(futureContext, command,
                0, triggerTime(delay, unit)));
    }

    @Override
    public <V> IScheduledFuture<V> schedule(Callable<V> callable, long delay, TimeUnit unit) {
        Objects.requireNonNull(callable);
        Objects.requireNonNull(unit);
        delay = Math.max(0, delay);

        return schedule(XScheduledFutureTask.ofCallable(futureContext, callable,
                0, triggerTime(delay, unit)));
    }

    @Override
    public IScheduledFuture<?> scheduleWithFixedDelay(Runnable command, long initialDelay, long delay, TimeUnit unit) {
        Objects.requireNonNull(command);
        Objects.requireNonNull(unit);
        initialDelay = Math.max(0, initialDelay);
        ScheduleBuilder.validatePeriod(delay);

        return schedule(XScheduledFutureTask.ofPeriodicRunnable(futureContext, command,
                0, triggerTime(initialDelay, unit), -unit.toNanos(delay)));
    }

    @Override
    public IScheduledFuture<?> scheduleAtFixedRate(Runnable command, long initialDelay, long period, TimeUnit unit) {
        Objects.requireNonNull(command);
        Objects.requireNonNull(unit);
        ScheduleBuilder.validateInitialDelay(initialDelay); // fixedRate禁止负延迟输入
        ScheduleBuilder.validatePeriod(period);

        return schedule(XScheduledFutureTask.ofPeriodicRunnable(futureContext, command,
                0, triggerTime(initialDelay, unit), unit.toNanos(period)));
    }

    @Override
    public <V> IScheduledFuture<V> schedule(ScheduleBuilder<V> builder) {
        Objects.requireNonNull(builder);
        return schedule(XScheduledFutureTask.ofBuilder(futureContext, builder,
                0, getTime()));
    }

    final long triggerTime(long delay, TimeUnit unit) {
        return getTime() + unit.toNanos(delay);
    }

    /** @implNote 需要在放入任务队列之前初始化id */
    <V> IScheduledFuture<V> schedule(XScheduledFutureTask<V> futureTask) {
        execute(futureTask);
        return futureTask;
    }

    /**
     * 请求将当前任务重新压入队列
     * 1.一定从当前线程调用
     * 2.如果无法继续调度任务，则取消任务
     *
     * @param triggered 是否是执行之后压入队列
     */
    abstract void reSchedulePeriodic(XScheduledFutureTask<?> futureTask, boolean triggered);

    /**
     * 请求删除给定的任务
     * 1.可能从其它线程调用，需考虑线程安全问题
     */
    abstract void removeScheduled(XScheduledFutureTask<?> futureTask);

}