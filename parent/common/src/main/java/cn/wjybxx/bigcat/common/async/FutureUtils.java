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

package cn.wjybxx.bigcat.common.async;

import cn.wjybxx.bigcat.common.time.TimeProvider;

import javax.annotation.Nonnull;
import java.util.Collection;
import java.util.Objects;
import java.util.concurrent.*;
import java.util.function.BiFunction;
import java.util.function.Consumer;
import java.util.function.Function;

/**
 * @author wjybxx
 * date 2023/4/3
 */
public class FutureUtils {

    private static final FluentFuture<?> EMPTY_FUTURE = newSucceedFuture(null);

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

    // region future工厂

    @SuppressWarnings("unchecked")
    public static <V> FluentFuture<V> emptyFuture() {
        return (FluentFuture<V>) EMPTY_FUTURE;
    }

    public static <V> FluentPromise<V> newPromise() {
        return new DefaultPromise<>();
    }

    /**
     * 创建一个{@link FluentFuture}，该future表示它关联的任务早已成功。因此{@link FluentFuture#isSucceeded()} 总是返回true。
     * 所有添加到该future上的监听器都会立即被通知。
     *
     * @param <V>    the type of value
     * @param result 任务的执行结果
     * @return Future
     */
    public static <V> FluentFuture<V> newSucceedFuture(V result) {
        final DefaultPromise<V> promise = new DefaultPromise<>();
        promise.trySuccess(result);
        return promise;
    }

    /**
     * 创建一个{@link FluentFuture}，该future表示它关联的任务早已失败。因此{@link FluentFuture#isFailed()}总是返回true。
     * 所有添加到该future上的监听器都会立即被通知。
     *
     * @param <V>   the type of value
     * @param cause 任务失败的原因
     * @return Future
     */
    public static <V> FluentFuture<V> newFailedFuture(@Nonnull Throwable cause) {
        return newFailedFuture(cause, true);
    }

    /**
     * 创建一个{@link FluentFuture}，该future表示它关联的任务早已失败。因此{@link FluentFuture#isFailed()}总是返回true。
     * 所有添加到该future上的监听器都会立即被通知。
     *
     * @param cause    任务失败的原因
     * @param logCause 是否自动记录日志
     * @return Future
     */
    public static <V> FluentFuture<V> newFailedFuture(@Nonnull Throwable cause, boolean logCause) {
        final DefaultPromise<V> promise = new DefaultPromise<>();
        promise.tryFailure(cause, logCause);
        return promise;
    }

    /** 将单线程的Future转换为JDK的Future */
    public static <V> CompletableFuture<V> toJDKFuture(FluentFuture<V> future) {
        final CompletableFuture<V> jdkFuture = new CompletableFuture<>();
        future.addListener(((v, throwable) -> {
            if (throwable != null) {
                jdkFuture.completeExceptionally(throwable);
            } else {
                jdkFuture.complete(v);
            }
        }));
        return jdkFuture;
    }

    /**
     * 将JDK的Future转换为本地线程的Future
     * 注意：务必确保{@link CompletableFuture}完成时是在逻辑线程;否则需指定一个线程安全的{@link Executor}
     *
     * @param jdkFuture 请确保该
     */
    public static <V> FluentFuture<V> fromJDKFuture(CompletableFuture<V> jdkFuture) {
        FluentPromise<V> promise = newPromise();
        jdkFuture.whenComplete((v, throwable) -> {
            if (throwable != null) {
                promise.tryFailure(throwable);
            } else {
                promise.trySuccess(v);
            }
        });
        return promise;
    }

    /** @param executor 请确保是线程安全的，可多线程提交任务的 */
    public static <V> FluentFuture<V> fromJDKFuture(CompletableFuture<V> jdkFuture, Executor executor) {
        Objects.requireNonNull(executor);
        FluentPromise<V> promise = newPromise();
        jdkFuture.whenCompleteAsync((v, throwable) -> {
            if (throwable != null) {
                promise.tryFailure(throwable);
            } else {
                promise.trySuccess(v);
            }
        }, executor);
        return promise;
    }

    /**
     * 将future的结果传输到promise上
     */
    public static <V> void setFuture(FluentPromise<? super V> promise, FluentFuture<V> future) {
        Objects.requireNonNull(promise, "promise");
        future.addListener((v, throwable) -> {
            if (throwable != null) {
                promise.tryFailure(throwable);
            } else {
                promise.trySuccess(v);
            }
        });
    }

    /**
     * 创建一个future聚合器
     *
     * @return futureCombiner
     */
    public static FutureCombiner newCombiner() {
        return new DefaultFutureCombiner();
    }

    public static FutureCombiner newCombiner(Collection<? extends FluentFuture<?>> futures) {
        return new DefaultFutureCombiner()
                .addAll(futures);
    }

    public static SameThreadExecutor newSameThreadExecutor() {
        return new DefaultSameThreadExecutor();
    }

    /**
     * @param countLimit 每帧允许运行的最大任务数，-1表示不限制；不可以为0
     * @param timeLimit  每帧允许的最大时间，-1表示不限制；不可以为0
     */
    public static SameThreadExecutor newSameThreadExecutor(int countLimit, long timeLimit, TimeUnit timeUnit) {
        return new DefaultSameThreadExecutor(countLimit, timeLimit, timeUnit);
    }

    /**
     * 返回的{@link SameThreadScheduledExecutor#tick()}默认不执行tick过程中新增加的任务
     *
     * @param timeProvider 用于调度器获取当前时间
     */
    public static SameThreadScheduledExecutor newScheduledExecutor(TimeProvider timeProvider) {
        return new DefaultScheduledExecutor(timeProvider);
    }

    // endregion

    // region 适配和执行

    /**
     * @param executor 用于在当前线程延迟执行任务的Executor
     * @return future
     */
    public static FluentFuture<Void> runAsync(Executor executor, Runnable task) {
        Objects.requireNonNull(executor, "executor");
        Objects.requireNonNull(task, "task");
        final FluentPromise<Void> promise = new DefaultPromise<>();
        final Runnable asyncRun = new AsyncRun(promise, task);
        executor.execute(asyncRun);
        return promise;
    }

    /**
     * @param executor 用于在当前线程延迟执行任务的Executor
     * @return future
     */
    public static <V> FluentFuture<V> callAsync(Executor executor, Callable<V> task) {
        Objects.requireNonNull(executor, "executor");
        Objects.requireNonNull(task, "task");
        final FluentPromise<V> promise = new DefaultPromise<>();
        final Runnable asyncRun = new AsyncCall<>(promise, task);
        executor.execute(asyncRun);
        return promise;
    }

    public static <V> FluentFuture<V> runFluent(FluentTask<V> task) {
        try {
            return task.run();
        } catch (Throwable e) {
            return newFailedFuture(e);
        }
    }

    // endregion

    // region 内部实现

    private static final class AsyncRun implements Runnable {

        final FluentPromise<Void> output;
        final Runnable action;

        AsyncRun(FluentPromise<Void> output, Runnable action) {
            this.output = output;
            this.action = action;
        }

        @Override
        public void run() {
            if (output.isDone()) {
                return;
            }
            try {
                action.run();
                output.trySuccess(null);
            } catch (Throwable ex) {
                output.tryFailure(ex);
            }
        }
    }

    private static class AsyncCall<U> implements Runnable {

        final FluentPromise<U> output;
        final Callable<U> fn;

        AsyncCall(FluentPromise<U> output, Callable<U> fn) {
            this.output = output;
            this.fn = fn;
        }

        @Override
        public void run() {
            if (output.isDone()) {
                return;
            }
            try {
                final U result = fn.call();
                output.trySuccess(result);
            } catch (Throwable ex) {
                output.tryFailure(ex);
            }
        }
    }

    // endregion

}