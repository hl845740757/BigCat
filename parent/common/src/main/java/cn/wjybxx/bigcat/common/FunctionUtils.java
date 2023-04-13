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

package cn.wjybxx.bigcat.common;

import org.apache.commons.lang3.exception.ExceptionUtils;

import java.util.concurrent.Callable;
import java.util.function.*;

/**
 * @author wjybxx
 * date 2023/3/31
 */
public class FunctionUtils {

    private static final Runnable _emptyRunnable = () -> {

    };

    private static final Function<?, ?> identity = t -> t;

    private static final Consumer<?> _emptyConsumer = v -> {
    };

    private static final IntConsumer _emptyIntConsumer = v -> {
    };

    private static final LongConsumer _emptyLongConsumer = v -> {
    };

    private static final BiConsumer<?, ?> _emptyBiConsumer = (a, b) -> {
    };

    private static final Predicate<?> _alwaysTrue = v -> true;

    private static final Predicate<?> _alwaysFalse = v -> false;

    private static final BiPredicate<?, ?> _biAlwaysTrue = (t, u) -> true;

    private static final BiPredicate<?, ?> _biAlwaysFalse = (t, u) -> false;

    // region 单例

    public static Runnable emptyRunnable() {
        return _emptyRunnable;
    }

    @SuppressWarnings("unchecked")
    public static <T> Function<T, T> identity() {
        return (Function<T, T>) identity;
    }

    @SuppressWarnings("unchecked")
    public static <T> Consumer<T> emptyConsumer() {
        return (Consumer<T>) _emptyConsumer;
    }

    public static IntConsumer emptyIntConsumer() {
        return _emptyIntConsumer;
    }

    public static LongConsumer emptyLongConsumer() {
        return _emptyLongConsumer;
    }

    @SuppressWarnings("unchecked")
    public static <T, U> BiConsumer<T, U> emptyBiConsumer() {
        return (BiConsumer<T, U>) _emptyBiConsumer;
    }

    @SuppressWarnings("unchecked")
    public static <T> Predicate<T> alwaysTrue() {
        return (Predicate<T>) _alwaysTrue;
    }

    @SuppressWarnings("unchecked")
    public static <T> Predicate<T> alwaysFalse() {
        return (Predicate<T>) _alwaysFalse;
    }

    @SuppressWarnings("unchecked")
    public static <T, U> BiPredicate<T, U> biAlwaysTrue() {
        return (BiPredicate<T, U>) _biAlwaysTrue;
    }

    @SuppressWarnings("unchecked")
    public static <T, U> BiPredicate<T, U> biAlwaysFalse() {
        return (BiPredicate<T, U>) _biAlwaysFalse;
    }
    // endregion

    // region 适配

    public static <T> Callable<T> toCallable(Runnable task, T result) {
        if (task == null) throw new NullPointerException();
        return new Runnable2CallbackAdapter<>(task, result);
    }

    public static Callable<Object> toCallable(Runnable task) {
        if (task == null) throw new NullPointerException();
        return new Runnable2CallbackAdapter<>(task, null);
    }

    public static Runnable toRunnable(Callable<?> task) {
        if (task == null) throw new NullPointerException();
        return new Callable2RunnableAdapter(task);
    }

    public static <T, R> Function<T, R> toFunction(Consumer<T> action, R result) {
        if (action == null) throw new NullPointerException();
        return new Consumer2FunctionAdapter<>(action, result);
    }

    public static <T> Function<T, Object> toFunction(Consumer<T> action) {
        if (action == null) throw new NullPointerException();
        return new Consumer2FunctionAdapter<>(action, null);
    }

    public static <T, R> Consumer<T> toConsumer(Function<T, R> action) {
        if (action == null) throw new NullPointerException();
        return new Function2ConsumerAdapter<>(action);
    }

    public static <T, R> Function<T, R> callableToFunction(Callable<R> callable) {
        if (callable == null) throw new NullPointerException();
        return new Callable2FunctionAdapter<>(callable);
    }

    // endregion

    // region
    private static class Runnable2CallbackAdapter<T> implements Callable<T> {

        final Runnable task;
        final T result;

        public Runnable2CallbackAdapter(Runnable task, T result) {
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
            return "Runnable2CallbackAdapter{" + "task=" + task + '}';
        }
    }

    private static class Callable2RunnableAdapter implements Runnable {

        final Callable<?> callable;

        public Callable2RunnableAdapter(Callable<?> callable) {
            this.callable = callable;
        }

        @Override
        public void run() {
            try {
                callable.call();
            } catch (Exception e) {
                ExceptionUtils.rethrow(e);
                ExceptionUtils.rethrow(e);
            }
        }

        @Override
        public String toString() {
            return "Callable2RunnableAdapter{" +
                    "callable=" + callable +
                    '}';
        }
    }

    private static class Consumer2FunctionAdapter<T, R> implements Function<T, R> {

        final Consumer<T> action;
        final R result;

        private Consumer2FunctionAdapter(Consumer<T> action, R result) {
            this.action = action;
            this.result = result;
        }

        @Override
        public R apply(T t) {
            action.accept(t);
            return result;
        }

        @Override
        public String toString() {
            return "Consumer2FunctionAdapter{" + "action=" + action + '}';
        }
    }

    private static class Function2ConsumerAdapter<T> implements Consumer<T> {

        final Function<T, ?> func;

        private Function2ConsumerAdapter(Function<T, ?> func) {
            this.func = func;
        }

        @Override
        public void accept(T t) {
            func.apply(t);
        }

        @Override
        public String toString() {
            return "Function2ConsumerAdapter{" +
                    "func=" + func +
                    '}';
        }
    }

    //

    private static class Callable2FunctionAdapter<T, R> implements Function<T, R> {

        final Callable<R> callable;

        private Callable2FunctionAdapter(Callable<R> callable) {
            this.callable = callable;
        }

        @Override
        public R apply(T t) {
            try {
                return callable.call();
            } catch (Exception e) {
                return ExceptionUtils.rethrow(e);
            }
        }

        @Override
        public String toString() {
            return "Callable2FunctionAdapter{" +
                    "callable=" + callable +
                    '}';
        }
    }
    // endregion

}