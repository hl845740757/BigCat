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

package cn.wjybxx.common;

import it.unimi.dsi.fastutil.ints.IntOpenHashSet;
import it.unimi.dsi.fastutil.ints.IntSet;

import javax.annotation.Nullable;
import java.util.BitSet;
import java.util.Collection;
import java.util.List;
import java.util.function.ToIntFunction;

/**
 * @author wjybxx
 * date 2023/4/1
 */
public class EnumUtils {

    private EnumUtils() {
    }

    /**
     * 通过名字查找枚举。
     * 与{@link Enum#valueOf(Class, String)}区别在于返回null代替抛出异常。
     *
     * @param values 枚举集合
     * @param name   要查找的枚举名字
     * @param <T>    枚举类型
     * @return T
     */
    @Nullable
    public static <T extends Enum<T>> T forName(T[] values, String name) {
        for (T t : values) {
            if (t.name().equals(name)) {
                return t;
            }
        }
        return null;
    }

    /**
     * 通过名字查找枚举(忽略名字的大小写)。
     * 与{@link Enum#valueOf(Class, String)}区别在于返回null代替抛出异常。
     *
     * @param values 枚举集合
     * @param name   要查找的枚举名字
     * @param <T>    枚举类型
     * @return T
     */
    public static <T extends Enum<T>> T forNameIgnoreCase(T[] values, String name) {
        for (T t : values) {
            if (t.name().equalsIgnoreCase(name)) {
                return t;
            }
        }
        return null;
    }

    public static <T extends Enum<T>> BitSet toBitSet(T... values) {
        final BitSet bitSet = new BitSet(64); // 可以自动扩容
        for (Enum<?> e : values) {
            bitSet.set(e.ordinal());
        }
        return bitSet;
    }

    public static <T extends Enum<T>> BitSet toBitSet(Collection<T> values) {
        final BitSet bitSet = new BitSet(64); // 可以自动扩容
        for (Enum<?> e : values) {
            bitSet.set(e.ordinal());
        }
        return bitSet;
    }

    // region 枚举集合检查

    /**
     * 检查枚举集合是否为空
     */
    public static void requireNotEmpty(List<?> values) {
        if (values == null || values.isEmpty()) {
            throw new IllegalArgumentException("values is empty");
        }
    }

    /**
     * 检查枚举中的number是否存在重复
     */
    public static <T> void checkNumberDuplicate(List<T> values, ToIntFunction<? super T> func) {
        final IntSet numberSet = new IntOpenHashSet(values.size());
        for (T t : values) {
            final int number = func.applyAsInt(t);
            if (!numberSet.add(number)) {
                final String msg = String.format("The number is duplicate, num: %d, enum: %s", number, t.toString());
                throw new IllegalArgumentException(msg);
            }
        }
    }

    /**
     * 检查枚举中的number是否连续
     * 注意：如果集合为空的话，这里不会抛出异常
     *
     * @param baseNumber 期望的起始数字，null表示无要求
     */
    public static <T> void checkNumberContinuity(List<T> values, @Nullable Integer baseNumber, ToIntFunction<? super T> func) {
        if (values.isEmpty()) {
            return;
        }
        // 检查起始值
        if (baseNumber != null) {
            int firstNumber = func.applyAsInt(values.get(0));
            if (firstNumber != baseNumber) {
                throw new IllegalArgumentException(String.format("baseNumber expected: %d, but found: %d", baseNumber, firstNumber));
            }
        }
        // 检查连续性
        for (int index = 0; index < values.size() - 1; index++) {
            final int curNumber = func.applyAsInt(values.get(index));
            final int nextNumber = func.applyAsInt(values.get(index + 1));
            if (curNumber + 1 != nextNumber) {
                throw new IllegalArgumentException("the number or values is not continuity, value: " + values.get(index));
            }
        }
    }

    /**
     * 枚举的数字是否连续
     *
     * @return 如果集合为空，也返回true
     */
    public static <T> boolean isNumberContinuity(List<T> values, ToIntFunction<? super T> func) {
        if (values.size() <= 1) {
            return true;
        }
        for (int index = 0; index < values.size() - 1; index++) {
            final int curNumber = func.applyAsInt(values.get(index));
            final int nextNumber = func.applyAsInt(values.get(index + 1));
            if (curNumber + 1 != nextNumber) {
                return false;
            }
        }
        return true;
    }
    // endregion

}