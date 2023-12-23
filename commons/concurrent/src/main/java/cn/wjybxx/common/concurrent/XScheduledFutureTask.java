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

import cn.wjybxx.common.ThreadUtils;
import cn.wjybxx.common.collect.IndexedElement;

import java.util.concurrent.Callable;
import java.util.concurrent.Delayed;
import java.util.concurrent.TimeUnit;

/**
 * @author wjybxx
 * date 2023/4/10
 */
final class XScheduledFutureTask<V> extends XFutureTask<V> implements IScheduledFuture<V>,
        IndexedElement {

    /** 任务的唯一id - 如果构造时未传入，要小心可见性问题 */
    private long id;
    /** 提前计算的，逻辑上的下次触发时间 - 非volatile，不对用户开放 */
    private long nextTriggerTime;
    /** 负数表示fixedDelay，正数表示fixedRate */
    private final long period;

    /** 所属的队列id */
    private int queueId;
    /** 在队列中的下标 */
    private int queueIndex = INDEX_NOT_FOUNT;

    /** 超时信息 */
    private TimeSharingContext timeSharingContext;
    /** 一些特征值 */
    private int flags;

    private XScheduledFutureTask(EventLoopFutureContext ctx, Callable<V> task,
                                 long id, long nanoTime, long period) {
        super(ctx, task);
        this.id = id;
        this.nextTriggerTime = nanoTime;
        this.period = period;
        this.flags = TaskFeature.defaultFlags;
    }

    static <V> XScheduledFutureTask<V> ofCallable(EventLoopFutureContext ctx, Callable<V> callable,
                                                  long id, long triggerTime) {
        return new XScheduledFutureTask<>(ctx, callable, id, triggerTime, 0);
    }

    static <V> XScheduledFutureTask<V> ofRunnable(EventLoopFutureContext ctx, Runnable task,
                                                  long id, long triggerTime) {
        return new XScheduledFutureTask<>(ctx, FutureUtils.toCallable(task, null), id, triggerTime, 0);
    }

    static <V> XScheduledFutureTask<V> ofPeriodic(EventLoopFutureContext ctx, Runnable task,
                                                  long id, long triggerTime, long period) {
        validatePeriod(period);
        return new XScheduledFutureTask<>(ctx, FutureUtils.toCallable(task, null), id, triggerTime, period);
    }

    private static void validatePeriod(long period) {
        if (period == 0) {
            throw new IllegalArgumentException("period: 0 (expected: != 0)");
        }
    }

    static <V> XScheduledFutureTask<V> ofBuilder(EventLoopFutureContext ctx, ScheduleBuilder<V> builder,
                                                 int id, long tickTime) {
        TimeUnit timeUnit = builder.getTimeUnit();
        long triggerTime = tickTime + timeUnit.toNanos(Math.max(0, builder.getInitialDelay()));
        long period;
        if (builder.isOnlyOnce()) {
            period = 0;
        } else if (builder.isFixedDelay()) {
            period = -1 * timeUnit.toNanos(builder.getPeriod());
        } else {
            period = timeUnit.toNanos(builder.getPeriod());
        }
        XScheduledFutureTask<V> result = new XScheduledFutureTask<>(ctx, builder.internal_getTask(), id, triggerTime, period);

        long timeout = builder.getTimeout();
        if (period != 0 && timeout != -1) {
            result.timeSharingContext = new TimeSharingContext(timeUnit.toNanos(timeout), tickTime);
        }
        result.flags = builder.internal_getFlags();
        return result;
    }

    private AbstractScheduledEventLoop eventLoop() {
        EventLoopFutureContext context = (EventLoopFutureContext) ctx;
        return (AbstractScheduledEventLoop) context.getEventLoop();
    }
    //

    long getId() {
        return id;
    }

    void setId(long id) {
        this.id = id;
    }

    int getQueueId() {
        return queueId;
    }

    void setQueueId(int queueId) {
        this.queueId = queueId;
    }

    long getNextTriggerTime() {
        return nextTriggerTime;
    }

    int getFlags() {
        return flags;
    }

    long getDelayNanos(long tickTime) {
        return nextTriggerTime - tickTime;
    }

    boolean isEnable(TaskFeature feature) {
        return feature.enabledIn(flags);
    }

    //

    @Override
    public boolean isPeriodic() {
        return period != 0;
    }

    @Override
    public long getDelay(TimeUnit unit) {
        return unit.convert(nextTriggerTime - eventLoop().nanoTime(), TimeUnit.NANOSECONDS);
    }

    @Override
    public void run() {
        AbstractScheduledEventLoop eventLoop = eventLoop();
        if (isDone()) { // 未及时从队列删除；不要尝试优化，可能尚未到触发时间
            eventLoop.removeScheduled(this);
            return;
        }
        long tickTime = eventLoop.nanoTime();
        if (tickTime < nextTriggerTime) { // 显式测试一次，适应多种EventLoop
            eventLoop.reSchedulePeriodic(this, false);
            return;
        }
        if (trigger(tickTime)) {
            eventLoop.reSchedulePeriodic(this, true);
        }
    }

    /**
     * 外部确定性触发，不需要回调的方式重新压入队列
     *
     * @return 如果需要再压入队列则返回true
     */
    boolean trigger(long tickTime) {
        Callable<V> task = getTask();
        try {
            if (period == 0) {
                if (internal_setUncancellable()) { // 隐式测试isDone
                    V result = task.call();
                    if (result != FutureUtils.CONTINUE) {  // 得出结果
                        internal_doComplete(result);
                    } else {
                        internal_doCompleteExceptionally(TimeSharingTimeoutException.INSTANCE);
                    }
                }
                return false;
            } else if (!isDone()) {
                TimeSharingContext timeSharingContext = this.timeSharingContext;
                if (timeSharingContext != null) {
                    timeSharingContext.beforeCall(tickTime, nextTriggerTime, period);
                }
                V result = task.call();
                if (result != FutureUtils.CONTINUE && FutureUtils.isTimeSharing(task)) { // 周期性任务，只有分时任务可以有结果
                    internal_doComplete(result);
                    return false;
                }
                if (!isDone()) { // 未被取消
                    if (timeSharingContext != null && timeSharingContext.isTimeout()) { // 超时
                        internal_doCompleteExceptionally(TimeSharingTimeoutException.INSTANCE);
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
            if (period != 0 && !FutureUtils.isTimeSharing(task)) {
                boolean caught = isEnable(TaskFeature.CAUGHT_THROWABLE)
                        || (isEnable(TaskFeature.CAUGHT_EXCEPTION) && ex instanceof Exception);
                if (caught) {
                    logger.info("periodic task caught exception", ex);
                    setNextRunTime(tickTime, timeSharingContext);
                    return true;
                }
            }
            internal_doCompleteExceptionally(ex);
        }
        return false;
    }

    private void setNextRunTime(long tickTime, TimeSharingContext timeSharingContext) {
        long maxDelay = timeSharingContext != null ? timeSharingContext.getTimeLeft() : Long.MAX_VALUE;
        if (period > 0) {
            nextTriggerTime = nextTriggerTime + Math.min(maxDelay, period);
        } else {
            nextTriggerTime = tickTime + Math.min(maxDelay, -period);
        }
    }

    @Override
    public boolean cancel(boolean mayInterruptIfRunning) {
        if (super.cancel(mayInterruptIfRunning)) {
            eventLoop().removeScheduled(this);
            return true;
        }
        return false;
    }

    boolean cancelWithoutRemove(boolean mayInterruptIfRunning) {
        return super.cancel(mayInterruptIfRunning);
    }

    @Override
    public int compareTo(Delayed other) {
        if (other == this) {
            return 0;
        }
        XScheduledFutureTask<?> that = (XScheduledFutureTask<?>) other;
        int r = Integer.compare(queueId, that.queueId);
        if (r != 0) {
            return r;
        }
        r = Long.compare(nextTriggerTime, that.nextTriggerTime);
        if (r != 0) {
            return r;
        }
        return Long.compare(id, that.id);
    }

    @Override
    public int collectionIndex(Object collection) {
        return queueIndex;
    }

    @Override
    public void collectionIndex(Object collection, int index) {
        queueIndex = index;
    }

}