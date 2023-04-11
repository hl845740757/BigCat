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

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.util.*;
import java.util.concurrent.*;
import java.util.function.Consumer;

/**
 * @author wjybxx
 * date 2023/4/7
 */
public abstract class AbstractEventLoop extends AbstractExecutorService implements EventLoop {

    protected static final Logger logger = LoggerFactory.getLogger(AbstractEventLoop.class);

    protected final EventLoopGroup parent;
    protected final Collection<EventLoop> selfCollection = Collections.singleton(this);
    protected final EventLoopFutureContext futureContext;

    protected AbstractEventLoop(@Nullable EventLoopGroup parent) {
        this.parent = parent;
        this.futureContext = new EventLoopFutureContext(this);
    }

    public AbstractEventLoop(EventLoopGroup parent, EventLoopFutureContext futureContext) {
        this.parent = parent;
        this.futureContext = futureContext;
    }

    @Nullable
    @Override
    public EventLoopGroup parent() {
        return parent;
    }

    @Nonnull
    @Override
    public EventLoop next() {
        return this;
    }

    @Nonnull
    @Override
    public EventLoop select(int key) {
        return this;
    }

    @Override
    public int numChildren() {
        return 1;
    }

    @Override
    public <V> XCompletableFuture<V> newPromise() {
        return new XCompletableFuture<>(futureContext);
    }

    /**
     * 请求将当前任务重新压入队列
     * 1.一定从当前线程调用
     * 2.如果无法继续调度任务，则取消任务
     *
     * @param triggered 是否是执行之后压入队列
     */
    protected abstract void reSchedulePeriodic(XScheduledFutureTask<?> futureTask, boolean triggered);

    /**
     * 请求删除给定的任务
     * 1.可能从其它线程调用，需考虑线程安全问题
     */
    protected abstract void removeScheduled(XScheduledFutureTask<?> futureTask);

    // --------------------------------------- 任务提交 ----------------------------------------

    @Override
    protected final <T> RunnableFuture<T> newTaskFor(@Nonnull Runnable runnable, T value) {
        return new XFutureTask<>(futureContext, runnable, value);
    }

    @Override
    protected final <T> RunnableFuture<T> newTaskFor(@Nonnull Callable<T> callable) {
        return new XFutureTask<>(futureContext, callable);
    }

    @Nonnull
    @Override
    public final ICompletableFuture<?> submit(@Nonnull Runnable task) {
        return (ICompletableFuture<?>) super.submit(task);
    }

    @Nonnull
    @Override
    public final <T> ICompletableFuture<T> submit(@Nonnull Runnable task, T result) {
        return (ICompletableFuture<T>) super.submit(task, result);
    }

    @Nonnull
    @Override
    public final <T> ICompletableFuture<T> submit(@Nonnull Callable<T> task) {
        return (ICompletableFuture<T>) super.submit(task);
    }

    @Override
    public IScheduledFuture<?> schedule(Runnable command, long delay, TimeUnit unit) {
        Objects.requireNonNull(command);
        Objects.requireNonNull(unit);
        delay = Math.max(0, delay);

        return schedule(new XScheduledFutureTask<>(futureContext, command, null,
                0, triggerTime(delay, unit)));
    }

    @Override
    public <V> IScheduledFuture<V> schedule(Callable<V> callable, long delay, TimeUnit unit) {
        Objects.requireNonNull(callable);
        Objects.requireNonNull(unit);
        delay = Math.max(0, delay);

        return schedule(new XScheduledFutureTask<>(futureContext, callable,
                0, triggerTime(delay, unit)));
    }

    @Override
    public IScheduledFuture<?> scheduleWithFixedDelay(Runnable command, long initialDelay, long delay, TimeUnit unit) {
        Objects.requireNonNull(command);
        Objects.requireNonNull(unit);
        initialDelay = Math.max(0, initialDelay);
        validatePeriod(delay);

        return schedule(new XScheduledFutureTask<>(futureContext, command, null,
                0, triggerTime(initialDelay, unit), -unit.toNanos(delay)));
    }

    @Override
    public IScheduledFuture<?> scheduleAtFixedRate(Runnable command, long initialDelay, long period, TimeUnit unit) {
        Objects.requireNonNull(command);
        Objects.requireNonNull(unit);
        validateInitialDelay(initialDelay); // fixedRate禁止负延迟输入
        validatePeriod(period);

        return schedule(new XScheduledFutureTask<>(futureContext, command, null,
                0, triggerTime(initialDelay, unit), unit.toNanos(period)));
    }

    protected long triggerTime(long delay, TimeUnit unit) {
        return getTime() + unit.toNanos(delay);
    }

    /** @implNote 需要在放入任务队列之前初始化id */
    protected <V> IScheduledFuture<V> schedule(XScheduledFutureTask<V> futureTask) {
        execute(futureTask);
        return futureTask;
    }

    /** 适用于禁止初始延迟小于0的情况 */
    protected static void validateInitialDelay(long initialDelay) {
        if (initialDelay < 0) {
            throw new IllegalArgumentException(
                    String.format("initialDelay: %d (expected: >= 0)", initialDelay));
        }
    }

    protected static void validatePeriod(long period) {
        if (period == 0) {
            throw new IllegalArgumentException("period: 0 (expected: != 0)");
        }
    }

    // -------------------------------------- invoke阻塞调用检测 --------------------------------------
    @Nonnull
    @Override
    public <T> T invokeAny(@Nonnull Collection<? extends Callable<T>> tasks) throws InterruptedException, ExecutionException {
        throwIfInEventLoop("invokeAny");
        return super.invokeAny(tasks);
    }

    @Nonnull
    @Override
    public <T> T invokeAny(@Nonnull Collection<? extends Callable<T>> tasks, long timeout, @Nonnull TimeUnit unit)
            throws InterruptedException, ExecutionException, TimeoutException {
        throwIfInEventLoop("invokeAny");
        return super.invokeAny(tasks, timeout, unit);
    }

    @Nonnull
    @Override
    public <T> List<Future<T>> invokeAll(@Nonnull Collection<? extends Callable<T>> tasks)
            throws InterruptedException {
        throwIfInEventLoop("invokeAll");
        return super.invokeAll(tasks);
    }

    @Nonnull
    @Override
    public <T> List<Future<T>> invokeAll(@Nonnull Collection<? extends Callable<T>> tasks,
                                         long timeout, @Nonnull TimeUnit unit) throws InterruptedException {
        throwIfInEventLoop("invokeAll");
        return super.invokeAll(tasks, timeout, unit);
    }

    protected final void throwIfInEventLoop(String method) {
        if (inEventLoop()) {
            throw new BlockingOperationException("Calling " + method + " from within the EventLoop is not allowed");
        }
    }

    // ---------------------------------------- 迭代 ---------------------------------------

    @Nonnull
    @Override
    public final Iterator<EventLoop> iterator() {
        return selfCollection.iterator();
    }

    @Override
    public final void forEach(Consumer<? super EventLoop> action) {
        selfCollection.forEach(action);
    }

    @Override
    public final Spliterator<EventLoop> spliterator() {
        return selfCollection.spliterator();
    }

    // ---------------------------------------- 工具方法 ---------------------------------------

    protected static void safeExecute(Runnable task) {
        try {
            task.run();
        } catch (Throwable t) {
            if (t instanceof VirtualMachineError) {
                logger.error("A task raised an exception. Task: {}", task, t);
            } else {
                logger.warn("A task raised an exception. Task: {}", task, t);
            }
        }
    }

    protected static void logCause(Throwable t) {
        if (t instanceof VirtualMachineError) {
            logger.error("A task raised an exception.", t);
        } else {
            logger.warn("A task raised an exception.", t);
        }
    }

}