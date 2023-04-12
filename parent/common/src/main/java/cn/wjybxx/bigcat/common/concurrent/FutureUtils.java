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
import cn.wjybxx.bigcat.common.box.ObjectHolder;

import javax.annotation.Nullable;
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
        XCompletableFuture<V> future = new XCompletableFuture<>(VobtrudeClosedFutureContext.INSTANCE);
        future.internal_doObtrudeValue(result);
        return future;
    }

    public static <V> XCompletableFuture<V> newFailedFuture(Throwable cause) {
        XCompletableFuture<V> future = new XCompletableFuture<>(VobtrudeClosedFutureContext.INSTANCE);
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

    public static FutureContext getVobtrdeClosedFutureContext() {
        return VobtrudeClosedFutureContext.INSTANCE;
    }

    private static class VobtrudeClosedFutureContext implements FutureContext {

        private static final VobtrudeClosedFutureContext INSTANCE = new VobtrudeClosedFutureContext();

        @Override
        public <T> void obtrudeValue(XCompletableFuture<T> future, T value) {
            throw new UnsupportedOperationException();
        }

        @Override
        public void obtrudeException(XCompletableFuture<?> future, Throwable ex) {
            throw new UnsupportedOperationException();
        }
    }

}