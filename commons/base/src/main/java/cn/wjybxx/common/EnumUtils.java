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

import cn.wjybxx.common.dson.DsonEnum;
import cn.wjybxx.common.dson.DsonEnumMapper;
import it.unimi.dsi.fastutil.ints.Int2ObjectMap;
import it.unimi.dsi.fastutil.ints.Int2ObjectOpenHashMap;
import it.unimi.dsi.fastutil.ints.IntOpenHashSet;
import it.unimi.dsi.fastutil.ints.IntSet;

import javax.annotation.Nullable;
import java.lang.reflect.Array;
import java.util.*;
import java.util.function.ToIntFunction;
import java.util.stream.Collectors;

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
    //

    /**
     * 检查枚举的number是从指定值开始且连续
     *
     * @param baseNumber 初始值，null表示不限定
     */
    public static <T extends DsonEnum> void checkNumberContinuity(List<T> values, @Nullable Integer baseNumber) {
        checkNumberContinuity(values, baseNumber, DsonEnum::getNumber);
    }


    /**
     * 检查枚举中的number是否存在重复
     */
    public static <T extends DsonEnum> void checkNumberDuplicate(List<T> values) {
        checkNumberDuplicate(values, DsonEnum::getNumber);
    }

    /**
     * 枚举的数字是否连续
     *
     * @return 如果集合为空，也返回true
     */
    public static <T extends DsonEnum> boolean isNumberContinuity(List<T> values) {
        return isNumberContinuity(values, DsonEnum::getNumber);
    }
    // endregion

    // region 枚举映射

    /**
     * 根据枚举的values建立索引；
     *
     * @param values 枚举数组
     * @param <T>    枚举类型
     * @return unmodifiable
     */
    public static <T extends DsonEnum> DsonEnumMapper<T> mapping(final T[] values) {
        return mapping(values, false);
    }

    /**
     * 根据枚举的values建立索引；
     *
     * @param values    枚举数组
     * @param fastQuery 是否追求极致的查询性能
     * @param <T>       枚举类型
     * @return unmodifiable
     */
    @SuppressWarnings("unchecked")
    public static <T extends DsonEnum> DsonEnumMapper<T> mapping(T[] values, final boolean fastQuery) {
        if (values.length == 0) {
            return (DsonEnumMapper<T>) EmptyMapper.INSTANCE;
        }
        if (values.length == 1) {
            return new SingletonMapper<>(values[0]);
        }

        // 保护性拷贝，避免出现并发问题 - 不确定values()是否会被修改
        values = Arrays.copyOf(values, values.length);

        // 检查是否存在重复number，在拷贝之后检查才安全
        final Int2ObjectMap<T> result = new Int2ObjectOpenHashMap<>(values.length);
        for (T t : values) {
            if (result.containsKey(t.getNumber())) {
                throw new IllegalArgumentException(t.getClass().getSimpleName() + " number:" + t.getNumber() + " is duplicate");
            }
            result.put(t.getNumber(), t);
        }

        final int minNumber = minNumber(values);
        final int maxNumber = maxNumber(values);
        if (isArrayAvailable(minNumber, maxNumber, values.length, fastQuery)) {
            return new ArrayBasedMapper<>(values, minNumber, maxNumber);
        } else {
            return new MapBasedMapper<>(values, result);
        }
    }

    private static <T extends DsonEnum> int minNumber(T[] values) {
        return Arrays.stream(values)
                .mapToInt(DsonEnum::getNumber)
                .min()
                .orElseThrow();
    }

    private static <T extends DsonEnum> int maxNumber(T[] values) {
        return Arrays.stream(values)
                .mapToInt(DsonEnum::getNumber)
                .max()
                .orElseThrow();
    }

    private static boolean isArrayAvailable(int minNumber, int maxNumber, int length, boolean fastQuery) {
        if (ArrayBasedMapper.matchDefaultFactor(minNumber, maxNumber, length)) {
            return true;
        }
        if (fastQuery && ArrayBasedMapper.matchMinFactor(minNumber, maxNumber, length)) {
            return true;
        }
        return false;
    }

    private static class EmptyMapper<T extends DsonEnum> implements DsonEnumMapper<T> {

        private static final EmptyMapper<?> INSTANCE = new EmptyMapper<>();

        private EmptyMapper() {
        }

        @Nullable
        @Override
        public T forNumber(int number) {
            return null;
        }

        @Override
        public List<T> values() {
            return Collections.emptyList();
        }

        @Override
        public List<T> sortedValues() {
            return Collections.emptyList();
        }

        @Override
        public boolean isContinuous() {
            return true;
        }
    }

    private static class SingletonMapper<T extends DsonEnum> implements DsonEnumMapper<T> {

        private final List<T> values;

        private SingletonMapper(T val) {
            Objects.requireNonNull(val);
            values = List.of(val);
        }

        @Override
        public List<T> values() {
            return values;
        }

        @Override
        public List<T> sortedValues() {
            return values;
        }

        @Override
        public boolean isContinuous() {
            return true;
        }

        @Nullable
        @Override
        public T forNumber(int number) {
            final T singleton = values.get(0);
            if (number == singleton.getNumber()) {
                return singleton;
            }
            return null;
        }

        @Override
        public T checkedForNumber(int number) {
            final T singleton = values.get(0);
            if (number == singleton.getNumber()) {
                return singleton;
            }
            throw new IllegalArgumentException("No enum constant, number " + number);
        }
    }

    /**
     * 基于数组的映射，对于数量少的枚举效果好；
     * (可能存在一定空间浪费，空间换时间，如果数字基本连续，那么空间利用率很好)
     */
    private static class ArrayBasedMapper<T extends DsonEnum> implements DsonEnumMapper<T> {

        private static final float DEFAULT_FACTOR = 0.5f;
        private static final float MIN_FACTOR = 0.25f;

        private final List<T> values;
        private final List<T> sortedValues;
        private final T[] elements;

        private final int minNumber;
        private final int maxNumber;

        /**
         * @param values    枚举的所有元素
         * @param minNumber 枚举中的最小number
         * @param maxNumber 枚举中的最大number
         */
        @SuppressWarnings("unchecked")
        private ArrayBasedMapper(T[] values, int minNumber, int maxNumber) {
            this.values = List.of(values);
            this.minNumber = minNumber;
            this.maxNumber = maxNumber;

            // 数组真实长度
            final int capacity = capacity(minNumber, maxNumber);
            this.elements = (T[]) Array.newInstance(values.getClass().getComponentType(), capacity);

            // 存入数组(不一定连续)
            for (T e : values) {
                this.elements[toIndex(e.getNumber())] = e;
            }

            // 如果id是连续的，则elements就是最终的有序List
            if (capacity == values.length) {
                this.sortedValues = List.of(elements);
            } else {
                this.sortedValues = Arrays.stream(elements)
                        .filter(Objects::nonNull)
                        .collect(Collectors.toUnmodifiableList());
            }
        }

        @Nullable
        @Override
        public T forNumber(int number) {
            if (number < minNumber || number > maxNumber) {
                return null;
            }
            return elements[toIndex(number)];
        }

        @Override
        public List<T> values() {
            return values;
        }

        @Override
        public List<T> sortedValues() {
            return sortedValues;
        }

        @Override
        public boolean isContinuous() {
            return capacity(minNumber, maxNumber) == values.size();
        }

        private int toIndex(int number) {
            return number - minNumber;
        }

        private static boolean matchDefaultFactor(int minNumber, int maxNumber, int length) {
            return matchFactor(minNumber, maxNumber, length, DEFAULT_FACTOR);
        }

        private static boolean matchMinFactor(int minNumber, int maxNumber, int length) {
            return matchFactor(minNumber, maxNumber, length, MIN_FACTOR);
        }

        private static boolean matchFactor(int minNumber, int maxNumber, int length, float factor) {
            return length >= Math.ceil(capacity(minNumber, maxNumber) * factor);
        }

        private static int capacity(int minNumber, int maxNumber) {
            return maxNumber - minNumber + 1;
        }
    }

    /**
     * 基于map的映射。
     * 对于枚举值较多或数字取值范围散乱的枚举适合。
     */
    private static class MapBasedMapper<T extends DsonEnum> implements DsonEnumMapper<T> {

        private final List<T> values;
        private final List<T> sortedValues;
        private final Int2ObjectMap<T> mapping;

        private final boolean isContinuous;

        private MapBasedMapper(T[] values, Int2ObjectMap<T> mapping) {
            this.values = List.of(values);
            this.mapping = mapping;

            this.sortedValues = CollectionUtils.toImmutableList(this.values, Comparator.comparingInt(DsonEnum::getNumber));
            this.isContinuous = isNumberContinuity(sortedValues);
        }

        @Nullable
        @Override
        public T forNumber(int number) {
            return mapping.get(number);
        }

        @Override
        public List<T> values() {
            return values;
        }

        @Override
        public List<T> sortedValues() {
            return sortedValues;
        }

        @Override
        public boolean isContinuous() {
            return isContinuous;
        }
    }
    // endregion

}