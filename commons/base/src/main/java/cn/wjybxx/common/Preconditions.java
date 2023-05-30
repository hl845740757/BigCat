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

package cn.wjybxx.common;

import org.apache.commons.lang3.StringUtils;

import javax.annotation.Nullable;
import java.util.Collection;
import java.util.Map;

import static cn.wjybxx.common.ObjectUtils.nullToDef;
import static cn.wjybxx.common.ObjectUtils.toStringIfNotNull;

/**
 * 这里包含了一些我们常用的前置条件检查，没有做太多的优化。
 *
 * @author wjybxx
 * date - 2023/4/27
 */
public class Preconditions {

    // region checkArgument

    public static void checkArgument(boolean b) {
        if (!b) {
            throw new IllegalArgumentException();
        }
    }

    public static void checkArgument(boolean b, @Nullable Object message) {
        if (!b) {
            throw new IllegalArgumentException(toStringIfNotNull(message));
        }
    }

    public static void checkState(boolean expression) {
        if (!expression) {
            throw new IllegalStateException();
        }
    }

    public static void checkState(boolean expression, @Nullable Object message) {
        if (!expression) {
            throw new IllegalStateException(toStringIfNotNull(message));
        }
    }
    // endregion

    // region null检查

    public static <T> T checkNotNull(T v) {
        if (v == null) throw new NullPointerException();
        return v;
    }

    public static <T> T checkNotNull(T v, @Nullable Object message) {
        if (v == null) throw new NullPointerException(toStringIfNotNull(message));
        return v;
    }

    // endregion

    // region 数字检查
    public static int checkPositive(int v, String name) {
        if (v <= 0) {
            throw new IllegalArgumentException(String.format("%s expected positive, but found: %d", nullToDef(name, "value"), v));
        }
        return v;
    }

    public static long checkPositive(long v, String name) {
        if (v <= 0) {
            throw new IllegalArgumentException(String.format("%s expected positive, but found: %d", nullToDef(name, "value"), v));
        }
        return v;
    }

    public static int checkNonNegative(int v, String name) {
        if (v < 0) {
            throw new IllegalArgumentException(String.format("%s expected nonnegative, but found: %d", nullToDef(name, "value"), v));
        }
        return v;
    }

    public static long checkNonNegative(long v, String name) {
        if (v < 0) {
            throw new IllegalArgumentException(String.format("%s expected nonnegative, but found: %d", nullToDef(name, "value"), v));
        }
        return v;
    }

    public static int checkBetween(int v, int min, int max) {
        if (v < min || v > max) {
            throw new IllegalArgumentException(String.format("value expected between range[%d, %d], but found: %d", min, max, v));
        }
        return v;
    }

    public static long checkBetween(long v, long min, long max) {
        if (v < min || v > max) {
            throw new IllegalArgumentException(String.format("value expected between range[%d, %d], but found: %d", min, max, v));
        }
        return v;
    }
    // endregion

    // region 字符串检查

    public static void checkNotEmpty(String value, @Nullable String msg) {
        if (value == null || value.isEmpty()) {
            throw new IllegalArgumentException(nullToDef(msg, "value cant be empty"));
        }
    }

    public static void checkNotBlank(String value, @Nullable String msg) {
        if (value == null || value.isBlank()) {
            throw new IllegalArgumentException(nullToDef(msg, "value cant be blank"));
        }
    }

    public static String checkNotContainsWhiteSpace(String value, @Nullable String msg) {
        if (StringUtils.containsWhitespace(value)) {
            throw new IllegalArgumentException(nullToDef(msg, "value cant contain white space"));
        }
        return value;
    }

    // endregion

    // region 集合检查

    public static <K, V> void checkContains(Map<K, V> map, K key,
                                            @Nullable String property) {
        if (!map.containsKey(key)) {
            throw new IllegalArgumentException(String.format("%s is absent, key %s", nullToDef(property, "key"), key));
        }
    }

    public static <K, V> void checkNotContains(Map<K, V> map, K key,
                                               @Nullable String property) {
        if (map.containsKey(key)) {
            throw new IllegalArgumentException(String.format("%s is duplicate, key %s", nullToDef(property, "key"), key));
        }
    }

    public static void checkNotEmpty(Collection<?> collection, @Nullable String msg) {
        if (collection == null || collection.isEmpty()) {
            throw new IllegalArgumentException(nullToDef(msg, "collection cant be empty"));
        }
    }

    public static void checkNotEmpty(Object[] array, @Nullable String msg) {
        if (array == null || array.length == 0) {
            throw new IllegalArgumentException(nullToDef(msg, "array cant be empty"));
        }
    }

    /** 检查集合里是否存在null，如果元素里存在null则抛出异常 */
    public static void checkNullElements(Collection<?> c) {
        for (Object element : c) {
            if (element == null) {
                throw new IllegalArgumentException("collection contains null values");
            }
        }
    }

    /** 检查数组里是否存在null，如果元素里存在null则抛出异常 */
    public static void checkNullElements(Object[] array) {
        for (Object element : array) {
            if (element == null) {
                throw new IllegalArgumentException("array contains null values");
            }
        }
    }
    // endregion

}