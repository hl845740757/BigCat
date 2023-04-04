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

import java.util.function.*;

/**
 * @author wjybxx
 * date 2023/3/31
 */
public class FunctionUtils {

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

    private static final Function<?, ?> identity = t -> t;

    //
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

    //

}