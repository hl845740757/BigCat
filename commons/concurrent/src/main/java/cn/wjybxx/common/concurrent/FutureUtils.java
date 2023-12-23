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
import cn.wjybxx.common.annotation.Internal;
import cn.wjybxx.common.time.CachedTimeProvider;
import org.apache.commons.lang3.mutable.MutableObject;

import javax.annotation.Nullable;
import javax.annotation.concurrent.ThreadSafe;
import java.util.Objects;
import java.util.concurrent.*;
import java.util.function.BiFunction;
import java.util.function.Supplier;

/**
 * @author wjybxx
 * date 2023/4/9
 */
public class FutureUtils {

    private static final BiFunction<Object, Throwable, Object> FALLBACK_NULL_IF_FAILED = (o, throwable) -> {
        if (throwable != null) {
            return null;
        } else {
            return o;
        }
    };

    // region 异常处理

    /**
     * 如果future失败则返回null
     */
    @SuppressWarnings("unchecked")
    public static <V> BiFunction<V, Throwable, V> fallbackNullIfFailed() {
        return (BiFunction<V, Throwable, V>) FALLBACK_NULL_IF_FAILED;
    }

    /**
     * {@link CompletableFuture}总是使用{@link CompletionException}包装异常，我们需要找到原始异常
     */
    public static Throwable unwrapCompletionException(Throwable t) {
        while (t instanceof CompletionException && t.getCause() != null) {
            t = t.getCause();
        }
        return t;
    }
    // endregion

    // region future工具方法

    public static <V> XCompletableFuture<V> toXCompletableFuture(CompletionStage<V> stage) {
        if (stage instanceof ICompletableFuture<V> future) {
            return future.toCompletableFuture();
        }
        XCompletableFuture<V> future = new XCompletableFuture<>();
        setFuture(future, stage);
        return future;
    }

    public static <V> void setFuture(CompletableFuture<? super V> output, CompletionStage<V> input) {
        Objects.requireNonNull(output, "output");
        input.whenComplete((v, throwable) -> {
            if (throwable != null) {
                output.completeExceptionally(throwable);
            } else {
                output.complete(v);
            }
        });
    }

    public static <V> void setFutureAsync(CompletableFuture<? super V> output, CompletionStage<V> input, @Nullable Executor executor) {
        Objects.requireNonNull(output, "output");
        if (executor == null) {
            input.whenCompleteAsync((v, throwable) -> {
                if (throwable != null) {
                    output.completeExceptionally(throwable);
                } else {
                    output.complete(v);
                }
            });
        } else {
            input.whenCompleteAsync((v, throwable) -> {
                if (throwable != null) {
                    output.completeExceptionally(throwable);
                } else {
                    output.complete(v);
                }
            }, executor);
        }
    }

    public static <V> XCompletableFuture<V> setFutureAsync(EventLoop eventLoop, CompletionStage<V> input) {
        XCompletableFuture<V> result = eventLoop.newPromise();
        setFutureAsync(result, input, eventLoop);
        return result;
    }

    public static Throwable getCause(CompletableFuture<?> future) {
        if (future.isCompletedExceptionally()) {
            // 捕获异常的开销更大...我们这相当于一个visitor
            MutableObject<Throwable> causeHolder = new MutableObject<>();
            future.whenComplete((v, cause) -> causeHolder.setValue(cause));
            return FutureUtils.unwrapCompletionException(causeHolder.getValue());
        }
        return null;
    }

    /** @return 如果future在指定时间内进入了完成状态，则返回true */
    public static boolean await(CompletableFuture<?> future, long timeout, TimeUnit unit) throws InterruptedException {
        if (timeout <= 0) {
            return future.isDone();
        }
        if (future.isDone()) {
            return true;
        }

        try {
            future.get(timeout, unit);
            return true;
        } catch (TimeoutException ignore) {
            return false;
        } catch (ExecutionException ignore) {
            return true;
        }
    }

    /** @return 如果future在指定时间内进入了完成状态，则返回true */
    public static boolean awaitUninterruptedly(CompletableFuture<?> future, long timeout, TimeUnit unit) {
        if (timeout <= 0) {
            return future.isDone();
        }
        if (future.isDone()) {
            return true;
        }

        boolean interrupted = false;
        final long endTime = System.nanoTime() + unit.toNanos(timeout);
        try {
            do {
                final long remainNano = endTime - System.nanoTime();
                if (remainNano <= 0) {
                    return false;
                }

                try {
                    future.get(remainNano, TimeUnit.NANOSECONDS);
                    return true;
                } catch (TimeoutException ignore) {
                    return false;
                } catch (ExecutionException ignore) {
                    return true;
                } catch (InterruptedException e) {
                    interrupted = true;
                }
            } while (!future.isDone());
        } finally {
            if (interrupted) {
                ThreadUtils.recoveryInterrupted();
            }
        }
        return true; // 循环外isDone
    }

    @Internal
    public static boolean completeTerminationFuture(XCompletableFuture<?> terminationFuture) {
        TerminateFutureContext terminationFutureCtx = (TerminateFutureContext) terminationFuture.getCtx();
        return terminationFutureCtx.terminate(terminationFuture);
    }

    @Internal
    public static boolean completeTerminationFuture(XCompletableFuture<?> terminationFuture, Throwable cause) {
        TerminateFutureContext terminationFutureCtx = (TerminateFutureContext) terminationFuture.getCtx();
        return terminationFutureCtx.terminate(terminationFuture, cause);
    }

    // endregion

    // region future工厂方法
    public static <V> XCompletableFuture<V> newPromise() {
        return new XCompletableFuture<>();
    }

    public static <V> XCompletableFuture<V> newSucceededFuture(V result) {
        XCompletableFuture<V> future = new XCompletableFuture<>(ObtrudeClosedFutureContext.ONLY_SELF);
        future.internal_doComplete(result);
        return future;
    }

    public static <V> XCompletableFuture<V> newFailedFuture(Throwable cause) {
        XCompletableFuture<V> future = new XCompletableFuture<>(ObtrudeClosedFutureContext.ONLY_SELF);
        future.internal_doCompleteExceptionally(cause);
        return future;
    }

    public static FutureCombiner newCombiner() {
        return new DefaultFutureCombiner();
    }

    public static FutureCombiner newCombiner(EventLoop eventLoop) {
        return new DefaultFutureCombiner(eventLoop::newPromise);
    }

    public static FutureCombiner newCombiner(Supplier<XCompletableFuture<Object>> factory) {
        return new DefaultFutureCombiner(factory);
    }

    /** @param downward 是否向下流动 */
    public static FutureContext obtrdeClosedFutureContext(boolean downward) {
        if (downward) {
            return ObtrudeClosedFutureContext.DOWNWARD;
        } else {
            return ObtrudeClosedFutureContext.ONLY_SELF;
        }
    }

    // endregion

    public static boolean inEventLoop(@Nullable Executor executor) {
        return executor instanceof EventLoop eventLoop && eventLoop.inEventLoop();
    }

    public static void ensureInEventLoop(EventLoop eventLoop, String msg) {
        if (!eventLoop.inEventLoop()) {
            throw new GuardedOperationException(msg);
        }
    }

    public static void ensureInEventLoop(EventLoop eventLoop) {
        if (!eventLoop.inEventLoop()) {
            throw new GuardedOperationException("Must be called from EventLoop thread");
        }
    }

    /** @see #newTimeProvider(EventLoop, long) */
    public static CachedTimeProvider newTimeProvider(EventLoop eventLoop) {
        return new EventLoopTimeProvider(eventLoop, System.currentTimeMillis());
    }

    /**
     * 创建一个支持缓存的时间提供器，且可以多线程安全访问。
     * 你需要调用{@link CachedTimeProvider#setTime(long)}更新时间值，且应该只有一个线程调用更新方法。
     *
     * @param eventLoop 负责更新时间的线程
     * @param curTime   初始时间
     * @return timeProvider - threadSafe
     */
    public static CachedTimeProvider newTimeProvider(EventLoop eventLoop, long curTime) {
        return new EventLoopTimeProvider(eventLoop, curTime);
    }

    // region callable适配

    public static final Object CONTINUE = new Object();

    public static <V> Callable<V> toCallable(Runnable task, V result) {
        Objects.requireNonNull(task);
        return new RunnableAdapter<>(task, result);
    }

    public static <V> Callable<V> toCallable(TimeSharingTask<V> task) {
        Objects.requireNonNull(task);
        return new TimeSharingAdapter<>(task);
    }

    public static boolean isTimeSharing(Callable<?> task) {
        return task.getClass() == TimeSharingAdapter.class;
    }

    public static Object unwrapTask(Object task) {
        if (task == null) {
            return null;
        }
        if (task instanceof RunnableAdapter<?> adapter) {
            return adapter.task;
        }
        if (task instanceof TimeSharingAdapter<?> adapter) {
            return adapter.task;
        }
        return task;
    }

    // endregion

    // region 适配类

    private static class ObtrudeClosedFutureContext implements FutureContext {

        private static final ObtrudeClosedFutureContext ONLY_SELF = new ObtrudeClosedFutureContext(false);
        private static final ObtrudeClosedFutureContext DOWNWARD = new ObtrudeClosedFutureContext(true);

        final boolean downward;

        private ObtrudeClosedFutureContext(boolean downward) {
            this.downward = downward;
        }

        @Override
        public FutureContext downContext(XCompletableFuture<?> future, Executor actionExecutor) {
            return downward ? this : null;
        }

        @Override
        public <T> void obtrudeValue(XCompletableFuture<T> future, T value) {
            throw new UnsupportedOperationException();
        }

        @Override
        public void obtrudeException(XCompletableFuture<?> future, Throwable ex) {
            throw new UnsupportedOperationException();
        }
    }

    @ThreadSafe
    private static class EventLoopTimeProvider implements CachedTimeProvider {

        private final EventLoop eventLoop;
        private volatile long time;

        private EventLoopTimeProvider(EventLoop eventLoop, long time) {
            this.eventLoop = eventLoop;
            setTime(time);
        }

        public void setTime(long curTime) {
            if (eventLoop.inEventLoop()) {
                this.time = curTime;
            } else {
                throw new GuardedOperationException("setTime from another thread");
            }
        }

        @Override
        public long getTime() {
            return time;
        }

        @Override
        public String toString() {
            return "EventLoopTimeProvider{" +
                    "curTime=" + time +
                    '}';
        }
    }

    private static class RunnableAdapter<T> implements Callable<T> {

        final Runnable task;
        final T result;

        public RunnableAdapter(Runnable task, T result) {
            this.task = task;
            this.result = result;
        }

        @Override
        public T call() throws Exception {
            task.run();
            return result;
        }

        @Override
        public String toString() {
            return "Runnable2CallbackAdapter{task=" + task + '}';
        }
    }

    private static class TimeSharingAdapter<V> implements Callable<V> {

        final TimeSharingTask<V> task;

        private TimeSharingAdapter(TimeSharingTask<V> task) {
            this.task = task;
        }

        @SuppressWarnings("unchecked")
        @Override
        public V call() throws Exception {
            ResultHolder<V> resultHolder = task.step();
            if (resultHolder == null) {
                return (V) CONTINUE;
            }
            return resultHolder.result;
        }

        @Override
        public String toString() {
            return "TimeSharingAdapter{task=" + task + '}';
        }
    }
    // endregion

}