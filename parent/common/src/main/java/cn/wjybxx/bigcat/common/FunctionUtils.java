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

import java.util.concurrent.Callable;
import java.util.function.*;

/**
 * @author wjybxx
 * date 2023/3/31
 */
public class FunctionUtils {

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
        return new RunnableAdapter<>(task, result);
    }

    public static Callable<Object> toCallable(Runnable task) {
        if (task == null) throw new NullPointerException();
        return new RunnableAdapter<>(task, null);
    }

    public static <T, R> Function<T, R> toFunction(Consumer<T> action, R result) {
        if (action == null) throw new NullPointerException();
        return new FunctionAdapter<>(action, result);
    }

    public static <T> Function<T, Object> toFunction(Consumer<T> action) {
        if (action == null) throw new NullPointerException();
        return new FunctionAdapter<>(action, null);
    }

    public static <T> Function<T, Object> toConsumer(Consumer<T> action) {
        if (action == null) throw new NullPointerException();
        return new FunctionAdapter<>(action, null);
    }

    // endregion

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
            return "RunnableAdapter1{" + "task=" + task + '}';
        }
    }

    private static class FunctionAdapter<T, R> implements Function<T, R> {

        final Consumer<T> action;
        final R result;

        private FunctionAdapter(Consumer<T> action, R result) {
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
            return "FunctionAdapter{" + "action=" + action + '}';
        }
    }
}