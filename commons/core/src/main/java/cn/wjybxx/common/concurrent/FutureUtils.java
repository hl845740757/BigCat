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
import cn.wjybxx.common.box.ObjectHolder;
import cn.wjybxx.common.time.CachedTimeProvider;

import javax.annotation.Nullable;
import javax.annotation.concurrent.ThreadSafe;
import java.util.Objects;
import java.util.concurrent.*;
import java.util.function.BiFunction;

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

    public static <V> void setFuture(CompletableFuture<? super V> output, CompletableFuture<V> input) {
        Objects.requireNonNull(output, "output");
        input.whenComplete((v, throwable) -> {
            if (throwable != null) {
                output.completeExceptionally(throwable);
            } else {
                output.complete(v);
            }
        });
    }

    public static Throwable getCause(CompletableFuture<?> future) {
        if (future.isCompletedExceptionally()) {
            // 捕获异常的开销更大...我们这相当于一个visitor
            ObjectHolder<Throwable> causeHolder = new ObjectHolder<>();
            future.whenComplete((v, cause) -> causeHolder.set(cause));
            return FutureUtils.unwrapCompletionException(causeHolder.get());
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

    //

    public static boolean inEventLoop(@Nullable Executor executor) {
        return executor instanceof EventLoop && ((EventLoop) executor).inEventLoop();
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

    public static <V> XCompletableFuture<V> newPromise() {
        return new XCompletableFuture<>();
    }

    public static <V> XCompletableFuture<V> newSucceededFuture(V result) {
        XCompletableFuture<V> future = new XCompletableFuture<>(VobtrudeClosedFutureContext.ONLY_SELF);
        future.internal_doObtrudeValue(result);
        return future;
    }

    public static <V> XCompletableFuture<V> newFailedFuture(Throwable cause) {
        XCompletableFuture<V> future = new XCompletableFuture<>(VobtrudeClosedFutureContext.ONLY_SELF);
        future.internal_doObtrudeException(cause);
        return future;
    }

    public static void completeTerminationFuture(XCompletableFuture<?> terminationFuture) {
        TerminateFutureContext terminationFutureCtx = (TerminateFutureContext) terminationFuture.getCtx();
        terminationFutureCtx.terminate(terminationFuture);
    }

    public static FutureCombiner newCombiner() {
        return new DefaultFutureCombiner();
    }

    /** @param downward 是否向下流动 */
    public static FutureContext getVobtrdeClosedFutureContext(boolean downward) {
        if (downward) {
            return VobtrudeClosedFutureContext.DOWNWARD;
        } else {
            return VobtrudeClosedFutureContext.ONLY_SELF;
        }
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
        return new ThreadSafeCachedTimeProvider(eventLoop, curTime);
    }

    // region 适配类

    private static class VobtrudeClosedFutureContext implements FutureContext {

        private static final VobtrudeClosedFutureContext ONLY_SELF = new VobtrudeClosedFutureContext(false);
        private static final VobtrudeClosedFutureContext DOWNWARD = new VobtrudeClosedFutureContext(true);

        final boolean downward;

        private VobtrudeClosedFutureContext(boolean downward) {
            this.downward = downward;
        }

        @Override
        public FutureContext downContext(XCompletableFuture<?> future) {
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
    private static class ThreadSafeCachedTimeProvider implements CachedTimeProvider {

        private final EventLoop eventLoop;
        private volatile long time;

        private ThreadSafeCachedTimeProvider(EventLoop eventLoop, long time) {
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
            return "ThreadSafeCachedTimeProvider{" +
                    "curTime=" + time +
                    '}';
        }
    }
    // endregion

}