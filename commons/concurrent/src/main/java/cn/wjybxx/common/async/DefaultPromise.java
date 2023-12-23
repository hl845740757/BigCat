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

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import javax.annotation.Nonnull;
import java.util.Objects;
import java.util.function.BiConsumer;
import java.util.function.BiFunction;
import java.util.function.Consumer;
import java.util.function.Function;

/**
 * @author wjybxx
 * date 2023/4/3
 */
public class DefaultPromise<V> extends AbstractPromise<V> {

    private static final Logger logger = LoggerFactory.getLogger(DefaultPromise.class);

    // region 基础接口

    @Override
    public <U> FluentFuture<U> thenComposeApply(Function<? super V, ? extends FluentFuture<U>> fn) {
        Objects.requireNonNull(fn);
        final DefaultPromise<U> promise = newIncompletePromise();
        pushCompletionStack(new UniComposeApply<>(this, promise, fn));
        return promise;
    }

    @Override
    public <X extends Throwable> FluentFuture<V> thenComposeCatching(Class<X> exceptionType,
                                                                     Function<? super X, ? extends FluentFuture<V>> fallback) {
        Objects.requireNonNull(exceptionType);
        Objects.requireNonNull(fallback);
        final DefaultPromise<V> promise = newIncompletePromise();
        pushCompletionStack(new UniComposeCatching<>(this, promise, exceptionType, fallback));
        return promise;
    }

    @Override
    public <U> FluentFuture<U> thenComposeHandle(BiFunction<? super V, Throwable, ? extends FluentFuture<U>> fn) {
        Objects.requireNonNull(fn);
        final DefaultPromise<U> promise = newIncompletePromise();
        pushCompletionStack(new UniComposeHandle<>(this, promise, fn));
        return promise;
    }

    @Override
    public <U> FluentFuture<U> thenApply(Function<? super V, ? extends U> fn) {
        Objects.requireNonNull(fn);
        final DefaultPromise<U> promise = newIncompletePromise();
        pushCompletionStack(new UniApply<>(this, promise, fn));
        return promise;
    }

    @Override
    public FluentFuture<Void> thenAccept(Consumer<? super V> action) {
        Objects.requireNonNull(action);
        final DefaultPromise<Void> promise = newIncompletePromise();
        pushCompletionStack(new UniAccept<>(this, promise, action));
        return promise;
    }

    @Override
    public <X extends Throwable> FluentFuture<V> catching(Class<X> exceptionType, Function<? super X, ? extends V> fallback) {
        Objects.requireNonNull(fallback);
        final DefaultPromise<V> promise = newIncompletePromise();
        pushCompletionStack(new UniCaching<>(this, promise, exceptionType, fallback));
        return promise;
    }

    @Override
    public <U> FluentFuture<U> thenHandle(BiFunction<? super V, Throwable, ? extends U> fn) {
        Objects.requireNonNull(fn);
        final DefaultPromise<U> promise = newIncompletePromise();
        pushCompletionStack(new UniHandle<>(this, promise, fn));
        return promise;
    }

    @Override
    public FluentFuture<V> whenComplete(BiConsumer<? super V, ? super Throwable> action) {
        Objects.requireNonNull(action);
        final DefaultPromise<V> promise = newIncompletePromise();
        pushCompletionStack(new UniWhenComplete<>(this, promise, action));
        return promise;
    }

    @Override
    public FluentFuture<V> addListener(@Nonnull BiConsumer<? super V, ? super Throwable> action) {
        Objects.requireNonNull(action);
        if (action instanceof Completion completion) {
            pushCompletionStack(completion);
        } else {
            pushCompletionStack(new ActionWhenComplete<>(this, action));
        }
        return this;
    }

    /**
     * 创建一个具体的子类型对象，用于作为各个方法的返回值。
     */
    protected <U> DefaultPromise<U> newIncompletePromise() {
        return new DefaultPromise<>();
    }
    // endregion

    // region completion

    private static AbstractPromise<?> postFire(AbstractPromise<?> output, boolean nested) {
        if (nested) {
            return output;
        } else {
            postComplete(output);
            return null;
        }
    }

    /**
     * {@link UniCompletion}表示连接两个{@link FluentFuture}，因此持有一个输入，一个动作，和一个输出。
     */
    private static abstract class UniCompletion<V, U> extends Completion {

        AbstractPromise<V> input;
        AbstractPromise<U> output;

        UniCompletion(AbstractPromise<V> input, AbstractPromise<U> output) {
            this.input = input;
            this.output = output;
        }

    }

    private static class UniComposeApply<V, U> extends UniCompletion<V, U> {

        Function<? super V, ? extends FluentFuture<U>> fn;

        UniComposeApply(AbstractPromise<V> input, AbstractPromise<U> output,
                        Function<? super V, ? extends FluentFuture<U>> fn) {
            super(input, output);
            this.fn = fn;
        }

        @Override
        AbstractPromise<?> tryFire(boolean nested) {
            if (output.isDone()) {
                return null;
            }

            final Object r = input.result;
            if (r instanceof AltResult) {
                output.completeRelayThrowable((AltResult) r);
            } else {
                try {
                    final V value = input.decodeValue(r);
                    final FluentFuture<U> relay = fn.apply(value);
                    if (relay.isDone()) {
                        // 返回的是一个已完成的Future
                        UniRelay.completeRelay(output, relay);
                    } else {
                        relay.addListener(new UniRelay<>(relay, output));
                        return null;
                    }
                } catch (Throwable e) {
                    output.completeThrowable(e);
                }
            }
            return postFire(output, nested);
        }

    }

    private static class UniComposeCatching<V, X> extends UniCompletion<V, V> {

        Class<X> exceptionType;
        Function<? super X, ? extends FluentFuture<V>> fallback;

        public UniComposeCatching(AbstractPromise<V> input, AbstractPromise<V> output,
                                  Class<X> exceptionType,
                                  Function<? super X, ? extends FluentFuture<V>> fallback) {
            super(input, output);
            this.exceptionType = exceptionType;
            this.fallback = fallback;
        }

        @Override
        AbstractPromise<?> tryFire(boolean nested) {
            if (output.isDone()) {
                return null;
            }

            final Object r = input.result;
            final Throwable cause;
            if (r instanceof AltResult && exceptionType.isInstance((cause = ((AltResult) r).cause))) {
                try {
                    @SuppressWarnings("unchecked") final X castException = (X) cause;
                    FluentFuture<V> relay = fallback.apply(castException);
                    if (relay.isDone()) {
                        // 返回的是一个已完成的Future
                        UniRelay.completeRelay(output, relay);
                    } else {
                        relay.addListener(new UniRelay<>(relay, output));
                        return null;
                    }
                } catch (Throwable ex) {
                    output.completeThrowable(ex);
                }
            } else {
                output.completeRelay(r);
            }
            return postFire(output, nested);
        }

    }

    private static class UniComposeHandle<V, U> extends UniCompletion<V, U> {

        BiFunction<? super V, Throwable, ? extends FluentFuture<U>> fn;

        public UniComposeHandle(AbstractPromise<V> input, AbstractPromise<U> output,
                                BiFunction<? super V, Throwable, ? extends FluentFuture<U>> fn) {
            super(input, output);
            this.fn = fn;
        }

        @Override
        AbstractPromise<?> tryFire(boolean nested) {
            if (output.isDone()) {
                return null;
            }

            final Object r = input.result;
            final V value;
            final Throwable cause;
            if (r instanceof AltResult) {
                value = null;
                cause = ((AltResult) r).cause;
            } else {
                value = input.decodeValue(r);
                cause = null;
            }

            try {
                final FluentFuture<U> relay = fn.apply(value, cause);
                if (relay.isDone()) {
                    // 返回的是一个已完成的Future
                    UniRelay.completeRelay(output, relay);
                } else {
                    relay.addListener(new UniRelay<>(relay, output));
                    return null;
                }
            } catch (Throwable e) {
                output.completeThrowable(e);
            }
            return postFire(output, nested);
        }

    }


    private static class UniRelay<V> extends Completion implements BiConsumer<V, Throwable> {

        FluentFuture<V> input;
        AbstractPromise<V> output;

        UniRelay(FluentFuture<V> input, AbstractPromise<V> output) {
            this.input = input;
            this.output = output;
        }

        @Override
        AbstractPromise<?> tryFire(boolean nested) {
            if (output.isDone()) {
                return null;
            }
            completeRelay(output, input);
            return postFire(output, nested);
        }

        private static <U> void completeRelay(AbstractPromise<U> output, FluentFuture<U> relay) {
            if (relay instanceof AbstractPromise) {
                Object inResult = ((AbstractPromise<U>) relay).result;
                output.completeRelay(inResult);
                return;
            }
            // 万一有人实现了别的子类
            try {
                Throwable cause = relay.cause();
                if (cause != null) {
                    output.completeRelayThrowable(new AltResult(cause));
                } else {
                    output.completeValue(relay.getNow());
                }
            } catch (Throwable ex) {
                output.completeThrowable(ex);
            }
        }

        @Override
        public void accept(V v, Throwable throwable) {
            throw new AssertionError("unreachable");
        }

    }

    private static class UniApply<V, U> extends UniCompletion<V, U> {

        Function<? super V, ? extends U> fn;

        UniApply(AbstractPromise<V> input, AbstractPromise<U> output,
                 Function<? super V, ? extends U> fn) {
            super(input, output);
            this.fn = fn;
        }

        @Override
        AbstractPromise<?> tryFire(boolean nested) {
            if (output.isDone()) {
                return null;
            }

            final Object r = input.result;
            if (r instanceof AltResult) {
                output.completeRelayThrowable((AltResult) r);
            } else {
                try {
                    V value = input.decodeValue(r);
                    output.completeValue(fn.apply(value));
                } catch (Throwable e) {
                    output.completeThrowable(e);
                }
            }
            return postFire(output, nested);
        }

    }

    private static class UniAccept<V> extends UniCompletion<V, Void> {

        Consumer<? super V> action;

        UniAccept(AbstractPromise<V> input, AbstractPromise<Void> output, Consumer<? super V> action) {
            super(input, output);
            this.action = action;
        }

        @Override
        AbstractPromise<?> tryFire(boolean nested) {
            if (output.isDone()) {
                return null;
            }

            final Object r = input.result;
            if (r instanceof AltResult) {
                output.completeRelayThrowable((AltResult) r);
            } else {
                try {
                    V value = input.decodeValue(r);
                    action.accept(value);
                    output.completeValue(null);
                } catch (Throwable ex) {
                    output.completeThrowable(ex);
                }
            }
            return postFire(output, nested);
        }
    }

    private static class UniCaching<V, X> extends UniCompletion<V, V> {

        Class<X> exceptionType;
        Function<? super X, ? extends V> fallback;

        UniCaching(AbstractPromise<V> input, AbstractPromise<V> output,
                   Class<X> exceptionType, Function<? super X, ? extends V> fallback) {
            super(input, output);
            this.exceptionType = exceptionType;
            this.fallback = fallback;
        }

        @Override
        AbstractPromise<?> tryFire(boolean nested) {
            if (output.isDone()) {
                return null;
            }

            final Object r = input.result;
            final Throwable cause;
            if (r instanceof AltResult && exceptionType.isInstance((cause = ((AltResult) r).cause))) {
                try {
                    @SuppressWarnings("unchecked") final X castException = (X) cause;
                    output.completeValue(fallback.apply(castException));
                } catch (Throwable ex) {
                    output.completeThrowable(ex);
                }
            } else {
                output.completeRelay(r);
            }
            return postFire(output, nested);
        }
    }

    private static class UniHandle<V, U> extends UniCompletion<V, U> {

        BiFunction<? super V, ? super Throwable, ? extends U> fn;

        UniHandle(AbstractPromise<V> input, AbstractPromise<U> output,
                  BiFunction<? super V, ? super Throwable, ? extends U> fn) {
            super(input, output);
            this.fn = fn;
        }

        @Override
        AbstractPromise<?> tryFire(boolean nested) {
            if (output.isDone()) {
                return null;
            }

            final Object r = input.result;
            final V value;
            final Throwable cause;
            if (r instanceof AltResult) {
                value = null;
                cause = ((AltResult) r).cause;
            } else {
                value = input.decodeValue(r);
                cause = null;
            }

            try {
                output.completeValue(fn.apply(value, cause));
            } catch (Throwable ex) {
                output.completeThrowable(ex);
            }
            return postFire(output, nested);
        }
    }

    private static class UniWhenComplete<V> extends UniCompletion<V, V> {

        BiConsumer<? super V, ? super Throwable> action;

        UniWhenComplete(AbstractPromise<V> input, AbstractPromise<V> output,
                        BiConsumer<? super V, ? super Throwable> action) {
            super(input, output);
            this.action = action;
        }

        @Override
        AbstractPromise<?> tryFire(boolean nested) {
            if (output.isDone()) {
                return null;
            }

            final Object r = input.result;
            final V value;
            final Throwable cause;
            if (r instanceof AltResult) {
                value = null;
                cause = ((AltResult) r).cause;
            } else {
                value = input.decodeValue(r);
                cause = null;
            }

            try {
                action.accept(value, cause);
            } catch (Throwable ex) {
                if (cause != null && cause != ex) { // 这里的实现与JDK并不相同
                    cause.addSuppressed(ex);
                }
                // 记录日志，避免异常信息丢失
                logger.warn("UniWhenComplete.action.accept caught exception", ex);
            }
            // 始终以相同的结果完成
            output.completeRelay(r);
            return postFire(output, nested);
        }
    }

    private static class ActionWhenComplete<V> extends Completion {

        AbstractPromise<V> input;
        BiConsumer<? super V, ? super Throwable> action;

        ActionWhenComplete(AbstractPromise<V> input, BiConsumer<? super V, ? super Throwable> action) {
            this.input = input;
            this.action = action;
        }

        @Override
        AbstractPromise<?> tryFire(boolean nested) {
            final Object r = input.result;
            final V value;
            final Throwable cause;
            if (r instanceof AltResult) {
                value = null;
                cause = ((AltResult) r).cause;
            } else {
                value = input.decodeValue(r);
                cause = null;
            }

            try {
                action.accept(value, cause);
            } catch (Throwable ex) {
                if (cause != null && cause != ex) { // 这里的实现与JDK并不相同
                    cause.addSuppressed(ex);
                }
                // 记录日志，避免异常信息丢失
                logger.warn("ActionWhenComplete.action.accept caught exception", ex);
            }
            return null;
        }
    }
    // endregion

}
