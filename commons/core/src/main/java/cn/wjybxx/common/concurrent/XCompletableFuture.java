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

import org.apache.commons.lang3.exception.ExceptionUtils;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import javax.annotation.Nonnull;
import java.util.Objects;
import java.util.concurrent.*;
import java.util.function.*;

/**
 * 其实future也写过几版，一版参考netty，一版参考jdk，我们单线程的future就是从jdk和Guava修改来的。
 * 不过，在正式的使用中，尽量避免再实现新的并发组件，虽然{@link CompletableFuture}中接口数多到爆炸，
 * 但JDK的future是大家必须要学习使用的，应当避免让大家再学习新的组件。
 * <p>
 * 1.jdk的查询接口给的不够友好，不能判断是否是成功完成，不能直接获取cause...
 * 2.不建议再继承重写方法签名了。。。在提供流式语法的api上，重写子类的工作很大，应当考虑组合的方式扩展。
 * (不要因为与future无关的功能而再继承, netty的channelPromise我觉得就设计得不好)
 * <p>
 * Q: 为什么在底层自动记录异常日志？
 * A: 实际使用的时候发现，如果靠写业务的时候保证不丢失异常信息，十分危险，如果疏忽将导致异常信息丢失，异常信息十分重要，不可轻易丢失。
 *
 * @author wjybxx
 * date 2023/4/9
 */
public class XCompletableFuture<T> extends CompletableFuture<T> implements ICompletableFuture<T> {

    static final Logger logger = LoggerFactory.getLogger(XCompletableFuture.class);
    static final boolean enableLogError = Boolean.parseBoolean(System.getProperty("cn.wjybxx.common.concurrent.XCompletableFuture.logError", "true"));

    protected final FutureContext ctx;

    public XCompletableFuture() {
        this(null);
    }

    public XCompletableFuture(FutureContext ctx) {
        this.ctx = ctx;
    }

    //

    @Override
    public final boolean isSucceeded() {
        return super.isDone() && !super.isCompletedExceptionally();
    }

    @Override
    public boolean isFailed() {
        return super.isCompletedExceptionally();
    }

    @Override
    public final T getNow() {
        return super.getNow(null);
    }

    @Override
    public final Throwable cause() {
        return FutureUtils.getCause(this);
    }

    //

    @Override
    public final T get() throws InterruptedException, ExecutionException {
        FutureContext ctx = this.ctx;
        if (ctx != null && !isDone()) {
            checkDeadLock(ctx);
        }
        return super.get();
    }

    @Override
    public final T get(long timeout, TimeUnit unit) throws InterruptedException, ExecutionException, TimeoutException {
        FutureContext ctx = this.ctx;
        if (ctx != null && !isDone()) {
            checkDeadLock(ctx);
        }
        return super.get(timeout, unit);
    }

    /**
     * 等待一段时间，直到future进入完成状态，或超时，或被中断
     *
     * @return 如果future在指定时间内进入了完成状态，则返回true
     */
    public final boolean await(long timeout, TimeUnit unit) throws InterruptedException {
        if (isDone()) {
            return true;
        }
        FutureContext ctx = this.ctx;
        if (ctx != null) {
            checkDeadLock(ctx);
        }
        return FutureUtils.await(this, timeout, unit);
    }

    /**
     * 等待一段时间，直到future进入完成状态，或超时
     * 等待期间不响应中断请求
     *
     * @return 如果future在指定时间内进入了完成状态，则返回true
     */
    public final boolean awaitUninterruptedly(long timeout, TimeUnit unit) {
        if (isDone()) {
            return true;
        }
        FutureContext ctx = this.ctx;
        if (ctx != null) {
            checkDeadLock(ctx);
        }
        return FutureUtils.awaitUninterruptedly(this, timeout, unit);
    }

    @Override
    public final T join() {
        FutureContext ctx = this.ctx;
        if (ctx != null && !isDone()) {
            checkDeadLock(ctx);
        }
        return super.join();
    }

    //
    @Override
    public FutureContext getCtx() {
        return ctx;
    }

    @Override
    public <U> XCompletableFuture<U> newIncompleteFuture() {
        XCompletableFuture<U> future = new XCompletableFuture<>(downContext());
        if (ctx != null) {
            ctx.reportFuture(this, future);
        }
        return future;
    }

    protected FutureContext downContext() {
        FutureContext ctx = this.ctx;
        return ctx != null ? ctx.downContext(this) : null;
    }

    protected void checkDeadLock(FutureContext ctx) {
        if (ctx != null && ctx.checkDeadlock(this)) {
            throw new BlockingOperationException();
        }
    }

    private static void logCause(Throwable x) {
        if (enableLogError && !(x instanceof CompletionException) // 通常是二次封装
                && !(x instanceof NoLogRequiredException)) {
            logger.info("future completed with exception", x);
        }
    }

    // endregion

    // Completable是Future+Promise的实现，有时我们不希望暴露这些接口

    @Override
    public boolean cancel(boolean mayInterruptIfRunning) {
        FutureContext ctx = this.ctx;
        if (ctx != null) {
            return ctx.cancel(this, mayInterruptIfRunning);
        } else {
            return internal_doCancel(mayInterruptIfRunning);
        }
    }

    @Override
    public boolean complete(T value) {
        FutureContext ctx = this.ctx;
        if (ctx != null) {
            return ctx.complete(this, value);
        } else {
            return internal_doComplete(value);
        }
    }

    @Override
    public boolean completeExceptionally(Throwable ex) {
        Objects.requireNonNull(ex);
        FutureContext ctx = this.ctx;
        if (ctx != null) {
            return ctx.completeExceptionally(this, ex);
        } else {
            return internal_doCompleteExceptionally(ex);
        }
    }

    @Override
    public void obtrudeValue(T value) {
        FutureContext ctx = this.ctx;
        if (ctx != null) {
            ctx.obtrudeValue(this, value);
        } else {
            internal_doObtrudeValue(value);
        }
    }

    @Override
    public void obtrudeException(Throwable ex) {
        Objects.requireNonNull(ex);
        FutureContext ctx = this.ctx;
        if (ctx != null) {
            ctx.obtrudeException(this, ex);
        } else {
            internal_doObtrudeException(ex);
        }
    }

    protected final boolean internal_doCancel(boolean mayInterruptIfRunning) {
        return super.cancel(mayInterruptIfRunning);
    }

    protected boolean internal_doComplete(T value) {
        return super.complete(value);
    }

    protected boolean internal_doCompleteExceptionally(Throwable ex) {
        logCause(ex);
        return super.completeExceptionally(ex);
    }

    protected final void internal_doObtrudeValue(T value) {
        super.obtrudeValue(value);
    }

    protected final void internal_doObtrudeException(Throwable ex) {
        logCause(ex);
        super.obtrudeException(ex);
    }
    //

    // region 扩展

    public <X extends Throwable> XCompletableFuture<T> catching(Class<X> exceptionType, Function<? super X, ? extends T> fallback) {
        Objects.requireNonNull(exceptionType);
        Objects.requireNonNull(fallback);
        return handle(new Catching2Handler<>(exceptionType, fallback));
    }

    public <X extends Throwable> XCompletableFuture<T> catchingAsync(
            Class<X> exceptionType, Function<? super X, ? extends T> fallback,
            Executor executor) {
        Objects.requireNonNull(exceptionType);
        Objects.requireNonNull(fallback);
        return handleAsync(new Catching2Handler<>(exceptionType, fallback), executor);
    }

    public <X> XCompletableFuture<T> thenComposeCatching(
            Class<X> exceptionType,
            Function<? super X, ? extends CompletionStage<T>> fallback) {
        Objects.requireNonNull(exceptionType);
        Objects.requireNonNull(fallback);
        return thenComposeHandle(new Catching2ComposeHandler<>(exceptionType, fallback, this));
    }

    public <X extends Throwable> XCompletableFuture<T> thenComposeCatchingAsync(
            Class<X> exceptionType,
            Function<? super X, ? extends CompletionStage<T>> fallback,
            @Nonnull Executor executor) {
        Objects.requireNonNull(exceptionType);
        Objects.requireNonNull(fallback);
        return thenComposeHandleAsync(new Catching2ComposeHandler<>(exceptionType, fallback, this), executor);
    }

    public <U> XCompletableFuture<U> thenComposeHandle(
            BiFunction<? super T, Throwable, ? extends CompletionStage<U>> fn) {
        Objects.requireNonNull(fn);
        return handle(fn)
                .thenCompose(Function.identity());
    }

    public <U> XCompletableFuture<U> thenComposeHandleAsync(
            BiFunction<? super T, Throwable, ? extends CompletionStage<U>> fn,
            @Nonnull Executor executor) {
        return handleAsync(fn, executor)
                .thenCompose(Function.identity());
    }

    private static class Catching2Handler<X, V> implements BiFunction<V, Throwable, V> {

        Class<X> exceptionType;
        Function<? super X, ? extends V> fallback;

        public Catching2Handler(Class<X> exceptionType,
                                Function<? super X, ? extends V> fallback) {
            this.exceptionType = exceptionType;
            this.fallback = fallback;
        }

        @Override
        public V apply(V v, Throwable cause) {
            if (cause != null) {
                if (exceptionType.isInstance(cause)) {
                    return fallback.apply(exceptionType.cast(cause));
                }
                ExceptionUtils.rethrow(cause); // 无法恢复，保持失败 - 会导致被wrap
            }
            return v;
        }
    }

    private static class Catching2ComposeHandler<X, V> implements BiFunction<V, Throwable, CompletionStage<V>> {

        Class<X> exceptionType;
        Function<? super X, ? extends CompletionStage<V>> fallback;
        CompletionStage<V> input;

        public Catching2ComposeHandler(Class<X> exceptionType,
                                       Function<? super X, ? extends CompletionStage<V>> fallback,
                                       CompletionStage<V> input) {
            this.exceptionType = exceptionType;
            this.fallback = fallback;
            this.input = input;
        }

        @Override
        public CompletionStage<V> apply(V v, Throwable cause) {
            if (cause != null && exceptionType.isInstance(cause)) {
                return fallback.apply(exceptionType.cast(cause));
            } else {
                return input;
            }
        }
    }

    // endregion

    // region 类型转换

    @SuppressWarnings("unchecked")
    @Override
    public <U> XCompletableFuture<U> thenApply(Function<? super T, ? extends U> fn) {
        return (XCompletableFuture<U>) super.thenApply(fn);
    }

    @SuppressWarnings("unchecked")
    @Override
    public <U> XCompletableFuture<U> thenApplyAsync(Function<? super T, ? extends U> fn) {
        return (XCompletableFuture<U>) super.thenApplyAsync(fn);
    }

    @SuppressWarnings("unchecked")
    @Override
    public <U> XCompletableFuture<U> thenApplyAsync(Function<? super T, ? extends U> fn, Executor executor) {
        return (XCompletableFuture<U>) super.thenApplyAsync(fn, executor);
    }

    @Override
    public XCompletableFuture<Void> thenAccept(Consumer<? super T> action) {
        return (XCompletableFuture<Void>) super.thenAccept(action);
    }

    @Override
    public XCompletableFuture<Void> thenAcceptAsync(Consumer<? super T> action) {
        return (XCompletableFuture<Void>) super.thenAcceptAsync(action);
    }

    @Override
    public XCompletableFuture<Void> thenAcceptAsync(Consumer<? super T> action, Executor executor) {
        return (XCompletableFuture<Void>) super.thenAcceptAsync(action, executor);
    }

    @Override
    public XCompletableFuture<Void> thenRun(Runnable action) {
        return (XCompletableFuture<Void>) super.thenRun(action);
    }

    @Override
    public XCompletableFuture<Void> thenRunAsync(Runnable action) {
        return (XCompletableFuture<Void>) super.thenRunAsync(action);
    }

    @Override
    public XCompletableFuture<Void> thenRunAsync(Runnable action, Executor executor) {
        return (XCompletableFuture<Void>) super.thenRunAsync(action, executor);
    }

    @SuppressWarnings("unchecked")
    @Override
    public <U, V1> XCompletableFuture<V1> thenCombine(CompletionStage<? extends U> other, BiFunction<? super T, ? super U, ? extends V1> fn) {
        return (XCompletableFuture<V1>) super.thenCombine(other, fn);
    }

    @SuppressWarnings("unchecked")
    @Override
    public <U, V1> XCompletableFuture<V1> thenCombineAsync(CompletionStage<? extends U> other, BiFunction<? super T, ? super U, ? extends V1> fn) {
        return (XCompletableFuture<V1>) super.thenCombineAsync(other, fn);
    }

    @SuppressWarnings("unchecked")
    @Override
    public <U, V1> XCompletableFuture<V1> thenCombineAsync(CompletionStage<? extends U> other, BiFunction<? super T, ? super U, ? extends V1> fn, Executor executor) {
        return (XCompletableFuture<V1>) super.thenCombineAsync(other, fn, executor);
    }

    @Override
    public <U> XCompletableFuture<Void> thenAcceptBoth(CompletionStage<? extends U> other, BiConsumer<? super T, ? super U> action) {
        return (XCompletableFuture<Void>) super.thenAcceptBoth(other, action);
    }

    @Override
    public <U> XCompletableFuture<Void> thenAcceptBothAsync(CompletionStage<? extends U> other, BiConsumer<? super T, ? super U> action) {
        return (XCompletableFuture<Void>) super.thenAcceptBothAsync(other, action);
    }

    @Override
    public <U> XCompletableFuture<Void> thenAcceptBothAsync(CompletionStage<? extends U> other, BiConsumer<? super T, ? super U> action, Executor executor) {
        return (XCompletableFuture<Void>) super.thenAcceptBothAsync(other, action, executor);
    }

    @Override
    public XCompletableFuture<Void> runAfterBoth(CompletionStage<?> other, Runnable action) {
        return (XCompletableFuture<Void>) super.runAfterBoth(other, action);
    }

    @Override
    public XCompletableFuture<Void> runAfterBothAsync(CompletionStage<?> other, Runnable action) {
        return (XCompletableFuture<Void>) super.runAfterBothAsync(other, action);
    }

    @Override
    public XCompletableFuture<Void> runAfterBothAsync(CompletionStage<?> other, Runnable action, Executor executor) {
        return (XCompletableFuture<Void>) super.runAfterBothAsync(other, action, executor);
    }

    @Override
    public <U> XCompletableFuture<U> applyToEither(CompletionStage<? extends T> other, Function<? super T, U> fn) {
        return (XCompletableFuture<U>) super.applyToEither(other, fn);
    }

    @Override
    public <U> XCompletableFuture<U> applyToEitherAsync(CompletionStage<? extends T> other, Function<? super T, U> fn) {
        return (XCompletableFuture<U>) super.applyToEitherAsync(other, fn);
    }

    @Override
    public <U> XCompletableFuture<U> applyToEitherAsync(CompletionStage<? extends T> other, Function<? super T, U> fn, Executor executor) {
        return (XCompletableFuture<U>) super.applyToEitherAsync(other, fn, executor);
    }

    @Override
    public XCompletableFuture<Void> acceptEither(CompletionStage<? extends T> other, Consumer<? super T> action) {
        return (XCompletableFuture<Void>) super.acceptEither(other, action);
    }

    @Override
    public XCompletableFuture<Void> acceptEitherAsync(CompletionStage<? extends T> other, Consumer<? super T> action) {
        return (XCompletableFuture<Void>) super.acceptEitherAsync(other, action);
    }

    @Override
    public XCompletableFuture<Void> acceptEitherAsync(CompletionStage<? extends T> other, Consumer<? super T> action, Executor executor) {
        return (XCompletableFuture<Void>) super.acceptEitherAsync(other, action, executor);
    }

    @Override
    public XCompletableFuture<Void> runAfterEither(CompletionStage<?> other, Runnable action) {
        return (XCompletableFuture<Void>) super.runAfterEither(other, action);
    }

    @Override
    public XCompletableFuture<Void> runAfterEitherAsync(CompletionStage<?> other, Runnable action) {
        return (XCompletableFuture<Void>) super.runAfterEitherAsync(other, action);
    }

    @Override
    public XCompletableFuture<Void> runAfterEitherAsync(CompletionStage<?> other, Runnable action, Executor executor) {
        return (XCompletableFuture<Void>) super.runAfterEitherAsync(other, action, executor);
    }

    @Override
    public <U> XCompletableFuture<U> thenCompose(Function<? super T, ? extends CompletionStage<U>> fn) {
        return (XCompletableFuture<U>) super.thenCompose(fn);
    }

    @Override
    public <U> XCompletableFuture<U> thenComposeAsync(Function<? super T, ? extends CompletionStage<U>> fn) {
        return (XCompletableFuture<U>) super.thenComposeAsync(fn);
    }

    @Override
    public <U> XCompletableFuture<U> thenComposeAsync(Function<? super T, ? extends CompletionStage<U>> fn, Executor executor) {
        return (XCompletableFuture<U>) super.thenComposeAsync(fn, executor);
    }

    @Override
    public XCompletableFuture<T> whenComplete(BiConsumer<? super T, ? super Throwable> action) {
        return (XCompletableFuture<T>) super.whenComplete(action);
    }

    @Override
    public XCompletableFuture<T> whenCompleteAsync(BiConsumer<? super T, ? super Throwable> action) {
        return (XCompletableFuture<T>) super.whenCompleteAsync(action);
    }

    @Override
    public XCompletableFuture<T> whenCompleteAsync(BiConsumer<? super T, ? super Throwable> action, Executor executor) {
        return (XCompletableFuture<T>) super.whenCompleteAsync(action, executor);
    }

    @SuppressWarnings("unchecked")
    @Override
    public <U> XCompletableFuture<U> handle(BiFunction<? super T, Throwable, ? extends U> fn) {
        return (XCompletableFuture<U>) super.handle(fn);
    }

    @SuppressWarnings("unchecked")
    @Override
    public <U> XCompletableFuture<U> handleAsync(BiFunction<? super T, Throwable, ? extends U> fn) {
        return (XCompletableFuture<U>) super.handleAsync(fn);
    }

    @SuppressWarnings("unchecked")
    @Override
    public <U> XCompletableFuture<U> handleAsync(BiFunction<? super T, Throwable, ? extends U> fn, Executor executor) {
        return (XCompletableFuture<U>) super.handleAsync(fn, executor);
    }

    @Override
    public XCompletableFuture<T> toCompletableFuture() {
        return (XCompletableFuture<T>) super.toCompletableFuture();
    }

    @Override
    public XCompletableFuture<T> exceptionally(Function<Throwable, ? extends T> fn) {
        return (XCompletableFuture<T>) super.exceptionally(fn);
    }

    @Override
    public XCompletableFuture<T> exceptionallyAsync(Function<Throwable, ? extends T> fn) {
        return (XCompletableFuture<T>) super.exceptionallyAsync(fn);
    }

    @Override
    public XCompletableFuture<T> exceptionallyAsync(Function<Throwable, ? extends T> fn, Executor executor) {
        return (XCompletableFuture<T>) super.exceptionallyAsync(fn, executor);
    }

    @Override
    public XCompletableFuture<T> exceptionallyCompose(Function<Throwable, ? extends CompletionStage<T>> fn) {
        return (XCompletableFuture<T>) super.exceptionallyCompose(fn);
    }

    @Override
    public XCompletableFuture<T> exceptionallyComposeAsync(Function<Throwable, ? extends CompletionStage<T>> fn) {
        return (XCompletableFuture<T>) super.exceptionallyComposeAsync(fn);
    }

    @Override
    public XCompletableFuture<T> exceptionallyComposeAsync(Function<Throwable, ? extends CompletionStage<T>> fn, Executor executor) {
        return (XCompletableFuture<T>) super.exceptionallyComposeAsync(fn, executor);
    }

    @Override
    public XCompletableFuture<T> completeAsync(Supplier<? extends T> supplier, Executor executor) {
        return (XCompletableFuture<T>) super.completeAsync(supplier, executor);
    }

    @Override
    public XCompletableFuture<T> completeAsync(Supplier<? extends T> supplier) {
        return (XCompletableFuture<T>) super.completeAsync(supplier);
    }

    @Override
    public XCompletableFuture<T> orTimeout(long timeout, TimeUnit unit) {
        return (XCompletableFuture<T>) super.orTimeout(timeout, unit);
    }

    @Override
    public XCompletableFuture<T> completeOnTimeout(T value, long timeout, TimeUnit unit) {
        return (XCompletableFuture<T>) super.completeOnTimeout(value, timeout, unit);
    }

    @Override
    public XCompletableFuture<T> copy() {
        return (XCompletableFuture<T>) super.copy();
    }

    // endregion

}