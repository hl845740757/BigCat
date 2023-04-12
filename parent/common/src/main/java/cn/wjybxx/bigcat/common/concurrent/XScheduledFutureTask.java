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

package cn.wjybxx.bigcat.common.concurrent;

import cn.wjybxx.bigcat.common.ThreadUtils;
import cn.wjybxx.bigcat.common.collect.IndexedPriorityQueue;

import java.util.concurrent.Callable;
import java.util.concurrent.Delayed;
import java.util.concurrent.TimeUnit;

/**
 * @author wjybxx
 * date 2023/4/10
 */
final class XScheduledFutureTask<V> extends XFutureTask<V> implements IScheduledFuture<V>,
        IndexedPriorityQueue.IndexedNode {

    /** 任务的唯一id - 如果构造时未传入，要小心可见性问题 */
    private long id;
    /**
     * 提前计算的，逻辑上的下次触发时间
     * 按照接口约定，用户也是可访问的，因此要提供可见性保证 -- 增加了一定的开销。
     * JDK之前的实现是非volatile，从而有可见性问题，后来的版本才加了volatile；netty现在都是非线程安全的。
     */
    private volatile long nextTriggerTime;
    /** 负数表示fixedDelay，正数表示fixedRate */
    private final long period;
    /** 在队列中的下标 */
    private int queueIndex = INDEX_NOT_IN_QUEUE;

    /** 所属的队列id */
    private int queueId;

    public XScheduledFutureTask(EventLoopFutureContext ctx, Callable<V> task,
                                long id, long nanoTime) {
        super(ctx, task);
        this.id = id;
        this.nextTriggerTime = nanoTime;
        this.period = 0;
    }

    private XScheduledFutureTask(EventLoopFutureContext ctx, Runnable task, V result,
                                 long id, long nanoTime, long period) {
        super(ctx, task, result);
        this.id = id;
        this.nextTriggerTime = nanoTime;
        this.period = period;
    }

    public static <V> XScheduledFutureTask<V> ofRunnable(EventLoopFutureContext ctx, Runnable task, V result,
                                                         long id, long nanoTime) {
        return new XScheduledFutureTask<>(ctx, task, result, id, nanoTime, 0);
    }


    public static <V> XScheduledFutureTask<V> ofPeriodicRunnable(EventLoopFutureContext ctx, Runnable task, V result,
                                                                 long id, long nanoTime, long period) {
        return new XScheduledFutureTask<>(ctx, task, result, id, nanoTime, validatePeriod(period));
    }

    private static long validatePeriod(long period) {
        if (period == 0) {
            throw new IllegalArgumentException("period: 0 (expected: != 0)");
        }
        return period;
    }

    private AbstractEventLoop eventLoop() {
        EventLoopFutureContext context = (EventLoopFutureContext) ctx;
        return (AbstractEventLoop) context.getEventLoop();
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

    long getDelayNanos(long tickNanoTime) {
        return nextTriggerTime - tickNanoTime;
    }
    //

    public boolean isPeriodic() {
        return period != 0;
    }

    @Override
    public long getDelay(TimeUnit unit) {
        return unit.convert(nextTriggerTime - eventLoop().getTime(), TimeUnit.NANOSECONDS);
    }

    @Override
    public void run() {
        AbstractEventLoop eventLoop = eventLoop();
        if (isDone()) { // 未及时从队列删除
            eventLoop.removeScheduled(this);
            return;
        }
        long tickTime = eventLoop.getTime();
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
        try {
            if (period == 0) {
                if (internal_setUncancellable()) { // 隐式测试isDone
                    V result = runTask();
                    internal_doComplete(result);
                }
            } else if (!isDone()) {
                runTask();
                if (!isDone()) {
                    setNextRunTime(tickTime);
                    return true;
                }
            }
        } catch (SucceededException ex) {
            @SuppressWarnings("unchecked") V result = (V) ex.getResult();
            internal_doComplete(result);
        } catch (Throwable ex) {
            ThreadUtils.recoveryInterrupted(ex);
            internal_doCompleteExceptionally(ex);
        }
        return false;
    }

    private void setNextRunTime(long tickTime) {
        if (period > 0) {
            //noinspection NonAtomicOperationOnVolatileField
            nextTriggerTime += period;
        } else {
            nextTriggerTime = tickTime - period;
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
    public int priorityQueueIndex(IndexedPriorityQueue<?> queue) {
        return queueIndex;
    }

    @Override
    public void priorityQueueIndex(IndexedPriorityQueue<?> queue, int index) {
        queueIndex = index;
    }

}