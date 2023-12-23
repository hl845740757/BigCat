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

import cn.wjybxx.common.concurrent.TimeSharingTask;
import cn.wjybxx.common.concurrent.TimeSharingTimeoutException;

import javax.annotation.Nonnull;
import javax.annotation.concurrent.NotThreadSafe;
import java.util.concurrent.Callable;
import java.util.concurrent.Executors;

/**
 * {@inheritDoc}
 * 定时任务调度器
 *
 * <h3>时序保证</h3>
 * 1. 单次执行的任务之间，有严格的时序保证，当过期时间(超时时间)相同时，先提交的一定先执行。
 * 2. 周期性执行的的任务，仅首次执行具备时序保证，当进入周期运行时，与其它任务之间便不具备时序保证。
 *
 * <h3>避免死循环</h3>
 * 子类实现必须在保证时序的条件下解决可能的死循环问题。
 * Q: 死循环是如何产生的？
 * A: 对于周期性任务，我们严格要求了周期间隔大于0，因此周期性的任务不会引发无限循环问题。
 * 但如果用户基于{@link #scheduleRun(Runnable, long)}实现循环，则在执行回调时可能添加一个立即执行的task（超时时间小于等于0），则可能陷入死循环。
 * 这种情况一般不是有意为之，而是某些特殊情况下产生的，比如：下次执行的延迟是计算出来的，而算出来的延迟总是为0或负数（线程缓存了时间戳，导致计算结果同一帧不会变化）。
 * 如果很好的限制了单帧执行的任务数，可以避免死循环。不过，错误的调用仍然可能导致其它任务得不到执行。
 *
 * @author wjybxx
 * date 2023/4/3
 */
@NotThreadSafe
public interface SameThreadScheduledExecutor extends SameThreadExecutor {

    /** 时序等同于{@code scheduleRun(0, command)} */
    @Override
    void execute(@Nonnull Runnable command);

    /** 时序等同于{@code scheduleRun(0, command)} */
    @Override
    FluentFuture<?> submitRun(@Nonnull Runnable command);

    /** 时序等同于{@code scheduleCall(0, command)} */
    @Override
    <V> FluentFuture<V> submitCall(@Nonnull Callable<V> command);

    /**
     * 创建一个在指定延迟之后执行一次的任务。
     * 该类型的任务有严格的时序保证！你认为先执行的一定先执行。
     *
     * @param task    需要执行的任务
     * @param timeout 过期时间，毫秒。允许小于0，但如果小于0，可能会影响当前帧的部分任务。
     */
    @Nonnull
    default ScheduledFluentFuture<?> scheduleRun(@Nonnull Runnable task, long timeout) {
        return scheduleCall(Executors.callable(task), timeout);
    }

    /**
     * 创建一个在指定延迟之后执行一次的任务。
     * 该类型的任务有严格的时序保证！你认为先执行的一定先执行。
     *
     * @param task    需要执行的任务
     * @param timeout 过期时间，毫秒。允许小于0，但如果小于0，可能会影响当前帧的部分任务。
     */
    @Nonnull
    <V> ScheduledFluentFuture<V> scheduleCall(@Nonnull Callable<V> task, long timeout);

    /**
     * 创建一个以固定延迟执行的任务。
     * (注意：任何周期性的任务与其它任务之间都不具备时序保证)
     *
     * @param task         定时执行的任务
     * @param initialDelay 首次执行延迟，毫秒。允许小于0，但如果小于0，可能会影响当前帧的部分任务。
     * @param period       循环周期，毫秒，必须大于0。
     */
    @Nonnull
    ScheduledFluentFuture<?> scheduleWithFixedDelay(@Nonnull Runnable task, long initialDelay, long period);

    /**
     * 创建一个以固定频率执行的任务。
     * (注意：任何周期性的任务与其它任务之间都不具备时序保证)
     *
     * @param task         定时执行的任务
     * @param initialDelay 首次执行延迟，毫秒。允许小于0，但如果小于0，可能会影响当前帧的部分任务。
     * @param period       执行间隔，毫秒，必须大于0
     */
    @Nonnull
    ScheduledFluentFuture<?> scheduleAtFixedRate(@Nonnull Runnable task, long initialDelay, long period);

    /**
     * 给定的任务将按照给定周期被调度，直到得到结果或超时。
     * 如果任务超时，将以{@link TimeSharingTimeoutException}异常结束
     *
     * @param timeout 超时时间
     * @see #scheduleWithFixedDelay(Runnable, long, long)
     */
    @Nonnull
    <V> ScheduledFluentFuture<V> timeSharingWithFixedDelay(@Nonnull TimeSharingTask<V> task, long initialDelay, long period,
                                                           long timeout);

    /**
     * 给定的任务将按照给定周期被调度，直到得到结果或超时。
     * 如果任务超时，将以{@link TimeSharingTimeoutException}异常结束.
     *
     * @param timeout 超时时间
     * @see #scheduleAtFixedRate(Runnable, long, long)
     */
    @Nonnull
    <V> ScheduledFluentFuture<V> timeSharingAtFixedRate(@Nonnull TimeSharingTask<V> task, long initialDelay, long period,
                                                        long timeout);
}