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

        XScheduledFutureTask<Object> futureTask = XScheduledFutureTask.ofRunnable(futureContext, command, 0, triggerTime(delay, unit));
        execute(futureTask);
        return futureTask;
    }

    @Override
    public <V> IScheduledFuture<V> schedule(Callable<V> callable, long delay, TimeUnit unit) {
        Objects.requireNonNull(callable);
        Objects.requireNonNull(unit);
        delay = Math.max(0, delay);

        XScheduledFutureTask<V> futureTask = XScheduledFutureTask.ofCallable(futureContext, callable, 0, triggerTime(delay, unit));
        execute(futureTask);
        return futureTask;
    }

    @Override
    public IScheduledFuture<?> scheduleWithFixedDelay(Runnable command, long initialDelay, long delay, TimeUnit unit) {
        Objects.requireNonNull(command);
        Objects.requireNonNull(unit);
        initialDelay = Math.max(0, initialDelay);
        ScheduleBuilder.validatePeriod(delay);

        XScheduledFutureTask<Object> futureTask = XScheduledFutureTask.ofPeriodic(futureContext, command,
                0, triggerTime(initialDelay, unit), -unit.toNanos(delay));// fixedDelay将period转负
        execute(futureTask);
        return futureTask;
    }

    @Override
    public IScheduledFuture<?> scheduleAtFixedRate(Runnable command, long initialDelay, long period, TimeUnit unit) {
        Objects.requireNonNull(command);
        Objects.requireNonNull(unit);
        ScheduleBuilder.validateInitialDelay(initialDelay); // fixedRate禁止负延迟输入
        ScheduleBuilder.validatePeriod(period);

        XScheduledFutureTask<Object> futureTask = XScheduledFutureTask.ofPeriodic(futureContext, command,
                0, triggerTime(initialDelay, unit), unit.toNanos(period)); // fixedRate保持period为正
        execute(futureTask);
        return futureTask;
    }

    @Override
    public <V> IScheduledFuture<V> schedule(ScheduleBuilder<V> builder) {
        Objects.requireNonNull(builder);
        XScheduledFutureTask<V> futureTask = XScheduledFutureTask.ofBuilder(futureContext, builder, 0, nanoTime());
        execute(futureTask);
        return futureTask;
    }

    final long triggerTime(long delay, TimeUnit unit) {
        return nanoTime() + unit.toNanos(delay);
    }

    /**
     * 当前线程的时间 -- 纳秒（非时间戳）
     * 1. 可以使用缓存的时间，也可以使用{@link System#nanoTime()}实时查询，只要不破坏任务的执行约定即可。
     * 2. 如果使用缓存时间，接口中并不约定时间的更新时机，也不约定一个大循环只更新一次。也就是说，线程可能在任意时间点更新缓存的时间，只要不破坏线程安全性和约定的任务时序。
     */
    protected abstract long nanoTime();

    /**
     * 请求将当前任务重新压入队列
     * 1.一定从当前线程调用
     * 2.如果无法继续调度任务，则取消任务
     *
     * @param triggered 是否是执行之后压入队列；通常用于在执行成功之后降低优先级
     */
    abstract void reSchedulePeriodic(XScheduledFutureTask<?> futureTask, boolean triggered);

    /**
     * 请求删除给定的任务
     * 1.可能从其它线程调用，需考虑线程安全问题
     */
    abstract void removeScheduled(XScheduledFutureTask<?> futureTask);

}