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

import cn.wjybxx.common.NegativeChecker;
import cn.wjybxx.common.concurrent.FutureUtils;
import cn.wjybxx.common.time.TimeProvider;

import javax.annotation.Nonnull;
import java.util.Collection;
import java.util.Objects;
import java.util.concurrent.Callable;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.Executor;
import java.util.concurrent.TimeUnit;
import java.util.function.BiFunction;

/**
 * @author wjybxx
 * date 2023/4/3
 */
public class SameThreads {

    private static final FluentFuture<?> EMPTY_FUTURE = newSucceededFuture(null);

    // region 异常处理

    /**
     * 如果future失败则返回null
     */
    @SuppressWarnings("unchecked")
    public static <V> BiFunction<V, Throwable, V> fallbackNullIfFailed() {
        return FutureUtils.fallbackNullIfFailed();
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
    public static <V> FluentFuture<V> newSucceededFuture(V result) {
        final DefaultPromise<V> promise = new DefaultPromise<>();
        promise.complete(result);
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
        promise.completeExceptionally(cause, logCause);
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
                promise.completeExceptionally(throwable);
            } else {
                promise.complete(v);
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
                promise.completeExceptionally(throwable);
            } else {
                promise.complete(v);
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
                promise.completeExceptionally(throwable);
            } else {
                promise.complete(v);
            }
        });
    }

    public static <V> void setFuture(CompletableFuture<? super V> promise, FluentFuture<V> future) {
        Objects.requireNonNull(promise, "promise");
        future.addListener((v, throwable) -> {
            if (throwable != null) {
                promise.completeExceptionally(throwable);
            } else {
                promise.complete(v);
            }
        });
    }

    /**
     * 创建一个future聚合器
     *
     * @return futureCombiner
     */
    public static FluentFutureCombiner newCombiner() {
        return new DefaultFluentFutureCombiner();
    }

    public static FluentFutureCombiner newCombiner(Collection<? extends FluentFuture<?>> futures) {
        return new DefaultFluentFutureCombiner()
                .addAll(futures);
    }

    public static SameThreadExecutor newExecutor() {
        return new DefaultSameThreadExecutor();
    }

    /**
     * @param countLimit 每帧允许运行的最大任务数，-1表示不限制；不可以为0
     * @param timeLimit  每帧允许的最大时间，-1表示不限制；不可以为0
     */
    public static SameThreadExecutor newExecutor(int countLimit, long timeLimit, TimeUnit timeUnit) {
        return new DefaultSameThreadExecutor(countLimit, timeLimit, timeUnit);
    }

    /**
     * 返回的{@link SameThreadScheduledExecutor#tick()}默认不执行tick过程中新增加的任务
     *
     * @param timeProvider 用于调度器获取当前时间
     */
    public static SameThreadScheduledExecutor newScheduledExecutor(TimeProvider timeProvider) {
        return new DefaultSameThreadScheduledExecutor(timeProvider);
    }

    public static SameThreadScheduledExecutor newScheduledExecutor(TimeProvider timeProvider, int initCapacity) {
        return new DefaultSameThreadScheduledExecutor(timeProvider, initCapacity);
    }

    public static SameThreadScheduledExecutor newScheduledExecutor(TimeProvider timeProvider, int initCapacity, NegativeChecker checker) {
        return new DefaultSameThreadScheduledExecutor(timeProvider, initCapacity, checker);
    }

    // endregion

    // region 适配和执行

    /**
     * @param executor 用于在当前线程延迟执行任务的Executor
     * @return future
     */
    public static FluentFuture<Void> runAsync(SameThreadExecutor executor, Runnable task) {
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
    public static <V> FluentFuture<V> callAsync(SameThreadExecutor executor, Callable<V> task) {
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
                output.complete(null);
            } catch (Throwable ex) {
                output.completeExceptionally(ex);
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
                output.complete(result);
            } catch (Throwable ex) {
                output.completeExceptionally(ex);
            }
        }
    }

    // endregion

}