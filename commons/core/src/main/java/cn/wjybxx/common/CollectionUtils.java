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

import cn.wjybxx.common.collect.DelayedCompressList;
import cn.wjybxx.common.collect.DelayedCompressListImpl;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.util.*;
import java.util.function.Predicate;
import java.util.stream.Stream;
import java.util.stream.StreamSupport;

import static cn.wjybxx.common.ObjectUtils.nullToDef;

/**
 * @author wjybxx
 * date 2023/3/31
 */
@SuppressWarnings("unused")
public class CollectionUtils {

    public static final int INDEX_NOT_FOUND = -1;

    private CollectionUtils() {

    }

    // region 特殊工厂
    public static <E> DelayedCompressList<E> newDelayedCompressList() {
        return new DelayedCompressListImpl<>();
    }

    public static <E> DelayedCompressList<E> newDelayedCompressList(int initCapacity) {
        return new DelayedCompressListImpl<>(initCapacity);
    }

    public static <E> DelayedCompressList<E> newDelayedCompressList(Collection<? extends E> src) {
        return new DelayedCompressListImpl<>(src);
    }
    // endregion

    // region list扩展

    /**
     * 注意：如果list包含null，且def也是null，则返回Null时无法判断是否来自集合。
     */
    public static <E> E getOrDefault(List<E> elements, int index, E def) {
        if (index < 0) throw new IndexOutOfBoundsException(index);
        if (elements == null || index >= elements.size()) {
            return def;
        }
        return elements.get(index);
    }

    public static <E> E firstOrDefault(List<E> elements, E def) {
        if (elements == null || elements.isEmpty()) {
            return def;
        }
        return elements.get(0);
    }

    public static <E> E lastOrDefault(List<E> elements, E def) {
        if (elements == null || elements.isEmpty()) {
            return def;
        }
        return elements.getLast();
//        return elements.get(elements.size() - 1);
    }

    /** 删除list的前n个元素 */
    public static void removeFirstN(List<?> list, int n) {
        if (n <= 0) {
            return;
        }
        if (list.size() <= n) {
            list.clear();
        } else {
            list.subList(0, n).clear();
        }
    }

    /** 删除list的后n个元素 */
    public static void removeLastN(List<?> list, int n) {
        if (n <= 0) {
            return;
        }
        if (list.size() <= n) {
            list.clear();
        } else {
            list.subList(list.size() - n, list.size()).clear();
        }
    }

    /** 移除list中第一个匹配的元素 -- 最好是数组列表 */
    public static <E> boolean removeFirstMatch(List<E> list, Predicate<? super E> predicate) {
        if (list.size() == 0) {
            return false;
        }
        final int index = indexOfCustom(list, predicate);
        if (index >= 0) {
            list.remove(index);
            return true;
        }
        return false;
    }

    /** 移除List中最后一个匹配的元素  -- 最好是数组列表 */
    public static <E> boolean removeLastMatch(List<E> list, Predicate<? super E> predicate) {
        if (list.size() == 0) {
            return false;
        }
        final int index = lastIndexOfCustom(list, predicate);
        if (index >= 0) {
            list.remove(index);
            return true;
        }
        return false;
    }

    /**
     * 删除指定位置的元素，可以选择是否保持列表中元素的顺序，当不需要保持顺序时可以对删除性能进行优化
     * 注意：应当小心使用该特性，能够使用该特性的场景不多，应当慎之又慎。
     *
     * @param ordered 是否保持之前的顺序。
     * @return 删除的元素
     */
    public static <E> E removeAt(List<E> list, int index, boolean ordered) {
        if (ordered) {
            return list.remove(index);
        } else {
            // 将最后一个元素赋值到要删除的位置，然后删除最后一个
            final E deleted = list.get(index);
            final int tailIndex = list.size() - 1;
            if (index < tailIndex) {
                list.set(index, list.get(tailIndex));
            }
            list.remove(tailIndex);
            return deleted;
        }
    }

    /**
     * 删除满足条件的元素，且不保持列表中元素的顺序 -- 慎用该方法。
     *
     * @return 删除的元素个数
     */
    public static <E> int unorderedRemoveIf(List<E> list, Predicate<? super E> filter) {
        Objects.requireNonNull(filter);
        final int originSize = list.size();
        if (originSize == 0) {
            return 0;
        }
        int size = originSize;
        for (int index = 0; index < size; ) {
            final E e = list.get(index);
            if (!filter.test(e)) {
                index++;
                continue;
            }
            size--; // tailIndex
            if (index < size) {
                list.set(index, list.get(size));
            }
            list.remove(size);
        }
        return originSize - size;
    }
    //

    /** @param list 最好为数组列表 */
    public static <E> int indexOfCustom(List<E> list, Predicate<? super E> indexFunc) {
        for (int i = 0, size = list.size(); i < size; i++) {
            if (indexFunc.test(list.get(i))) {
                return i;
            }
        }
        return -1;
    }

    /** @param list 最好为数组列表 */
    public static <E> int lastIndexOfCustom(List<E> list, Predicate<? super E> indexFunc) {
        for (int i = list.size() - 1; i >= 0; i--) {
            if (indexFunc.test(list.get(i))) {
                return i;
            }
        }
        return -1;
    }

    /** @param list 最好为数组列表 */
    public static <E> boolean containsCustom(List<E> list, Predicate<? super E> indexFunc) {
        return indexOfCustom(list, indexFunc) >= 0;
    }

    /** @param list 最好为数组列表 */
    public static <E> E findFirst(List<E> list, Predicate<? super E> indexFunc) {
        for (int i = 0, size = list.size(); i < size; i++) {
            final E e = list.get(i);
            if (indexFunc.test(e)) {
                return e;
            }
        }
        return null;
    }

    /** @param list 最好为数组列表 */
    public static <E> E findLast(List<E> list, Predicate<? super E> indexFunc) {
        for (int i = list.size() - 1; i >= 0; i--) {
            final E e = list.get(i);
            if (indexFunc.test(e)) {
                return e;
            }
        }
        return null;
    }

    /** 获取从某个索引开始的子列表 */
    public static <E> List<E> subList(List<E> src, int index) {
        return src.subList(index, src.size());
    }

    // region 使用“==”操作集合
    // 注意：对于拆装箱的对象慎用

    public static boolean containsRef(List<?> list, Object element) {
        return indexOfRef(list, element, 0) >= 0;
    }

    /** 查找对象引用在数组中的索引 */
    public static int indexOfRef(List<?> list, Object element) {
        return indexOfRef(list, element, 0);
    }

    /**
     * 查找对象引用在List中的索引
     *
     * @param element    要查找的元素
     * @param startIndex 开始下标
     */
    public static int indexOfRef(List<?> list, Object element, int startIndex) {
        Objects.requireNonNull(list, "list");
        if (startIndex >= list.size()) {
            return INDEX_NOT_FOUND;
        }
        if (startIndex < 0) {
            startIndex = 0;
        }
        for (int i = startIndex, size = list.size(); i < size; i++) {
            if (list.get(i) == element) {
                return i;
            }
        }
        return INDEX_NOT_FOUND;
    }

    /** 反向查找对象引用在List中的索引 */
    public static int lastIndexOfRef(List<?> list, Object element) {
        return lastIndexOfRef(list, element, Integer.MAX_VALUE);
    }

    /**
     * 反向查找对象引用在List中的索引
     *
     * @param element    要查找的元素
     * @param startIndex 开始下标
     */
    public static int lastIndexOfRef(List<?> list, Object element, int startIndex) {
        Objects.requireNonNull(list, "list");
        if (startIndex < 0) {
            return INDEX_NOT_FOUND;
        }
        if (startIndex >= list.size()) {
            startIndex = list.size() - 1;
        }
        for (int i = startIndex; i >= 0; i--) {
            if (list.get(i) == element) {
                return i;
            }
        }
        return -1;
    }

    /** 使用“==”删除对象 */
    public static boolean removeRef(List<?> list, Object element) {
        final int index = indexOfRef(list, element);
        if (index < 0) {
            return false;
        }
        list.remove(index);
        return true;
    }

    /** 使用“==”删除对象 */
    public static boolean removeRef(List<?> list, Object element, boolean ordered) {
        final int index = indexOfRef(list, element);
        if (index < 0) {
            return false;
        }
        removeAt(list, index, ordered);
        return true;
    }
    // endregion

    // region 数组

    /** 判断是否存在给定元素的引用 */
    public static <T> boolean containsRef(T[] list, Object element) {
        return indexOfRef(list, element, 0) >= 0;
    }

    /** 查找对象引用在数组中的索引 */
    public static <T> int indexOfRef(T[] list, Object element) {
        return indexOfRef(list, element, 0);
    }

    /**
     * 查找对象引用在数组中的索引
     *
     * @param element    要查找的元素
     * @param startIndex 开始下标
     */
    public static <T> int indexOfRef(T[] list, Object element, int startIndex) {
        Objects.requireNonNull(list, "list");
        if (startIndex >= list.length) {
            return INDEX_NOT_FOUND;
        }
        if (startIndex < 0) {
            startIndex = 0;
        }
        for (int i = startIndex, size = list.length; i < size; i++) {
            if (list[i] == element) {
                return i;
            }
        }
        return INDEX_NOT_FOUND;
    }

    /** 反向查找对象引用在数组中的索引 */
    public static <T> int lastIndexOfRef(T[] list, Object element) {
        return lastIndexOfRef(list, element, Integer.MAX_VALUE);
    }

    /**
     * 反向查找对象引用在数组中的索引
     *
     * @param element    要查找的元素
     * @param startIndex 开始下标
     */
    public static <T> int lastIndexOfRef(T[] list, Object element, int startIndex) {
        Objects.requireNonNull(list, "list");
        if (startIndex < 0) {
            return INDEX_NOT_FOUND;
        }
        if (startIndex >= list.length) {
            startIndex = list.length - 1;
        }
        for (int i = startIndex; i >= 0; i--) {
            if (list[i] == element) {
                return i;
            }
        }
        return -1;
    }

    // endregion

    // region arrayList快捷方法

    public static <E> ArrayList<E> newArrayList(E a) {
        final ArrayList<E> result = new ArrayList<>(1);
        result.add(a);
        return result;
    }

    public static <E> ArrayList<E> newArrayList(E a, E b) {
        final ArrayList<E> result = new ArrayList<>(2);
        result.add(a);
        result.add(b);
        return result;
    }

    public static <E> ArrayList<E> newArrayList(E a, E b, E c) {
        final ArrayList<E> result = new ArrayList<>(3);
        result.add(a);
        result.add(b);
        result.add(c);
        return result;
    }

    public static <E> boolean addAll(ArrayList<E> self, Collection<? extends E> other) {
        if (other == null || other.isEmpty()) {
            return false;
        }
        return self.addAll(other);
    }

    public static <E> boolean removeAll(ArrayList<E> self, Collection<? extends E> other) {
        if (other == null || other.isEmpty()) {
            return false;
        }
        return self.removeAll(other);
    }

    public static <E> List<E> toList(E[] array) {
        return Arrays.asList(array);
    }

    public static <E> List<E> toList(final E[] array, int offset, int length) {
        if (offset == 0 && length == array.length) {
            return Arrays.asList(array);
        } else {
            return Arrays.asList(array).subList(offset, offset + length);
        }
    }

    public static <E> ArrayList<E> toArrayList(final E[] array) {
        return new ArrayList<>(new ArrayHolder<>(array, 0, array.length));
    }

    public static <E> ArrayList<E> toArrayList(final E[] array, int offset, int length) {
        // arrayList不支持数组的构造参数，因此无法绕过冗余的拷贝
        return new ArrayList<>(new ArrayHolder<>(array, offset, length));
    }

    // endregion

    /** 连接多个列表 */
    @SafeVarargs
    public static <E> List<E> union(List<E> first, List<? extends E> second, List<E>... more) {
        int size = first.size() + second.size();
        for (List<?> m : more) {
            size = Math.addExact(size, m.size());
        }

        final ArrayList<E> result = new ArrayList<>(size);
        addAll(result, first);
        addAll(result, second);
        for (List<E> m : more) {
            addAll(result, m);
        }
        return result;
    }

    @Nonnull
    public static <E> List<E> toImmutableList(@Nullable Collection<E> src) {
        return (src == null || src.isEmpty()) ? List.of() : List.copyOf(src);
    }

    /**
     * @param comparator 在转换前进行一次排序
     */
    @Nonnull
    public static <E> List<E> toImmutableList(@Nullable Collection<E> src, Comparator<? super E> comparator) {
        if (src == null || src.isEmpty()) {
            return List.of();
        }
        @SuppressWarnings("unchecked") final E[] elements = (E[]) src.toArray();
        Arrays.sort(elements, comparator);
        return List.of(elements);
    }

    /** 如果列表为Null，则返回空数组列表 */
    public static <E> List<E> nullToArrayList(@Nullable List<E> src) {
        return src == null ? new ArrayList<>() : src;
    }

    /** 如果列表为Null，则返回空列表 */
    public static <E> List<E> nullToEmptyList(@Nullable List<E> src) {
        return src == null ? List.of() : src;
    }

    // endregion

    // region set

    public static <E> HashSet<E> newHashSet(int size) {
        return new HashSet<>(capacity(size));
    }

    public static <E> LinkedHashSet<E> newLinkedHashSet(int size) {
        return new LinkedHashSet<>(capacity(size));
    }

    public static <E> Set<E> newIdentityHashSet(int size) {
        return Collections.newSetFromMap(new IdentityHashMap<>(size));
    }

    @Nonnull
    @SuppressWarnings("unchecked")
    public static <E> Set<E> toImmutableSet(@Nullable Collection<E> src) {
        if ((src == null || src.isEmpty())) {
            return Set.of();
        }
        if (src instanceof EnumSet<?> enumSet) {
            // EnumSet使用代理的方式更好，但要先拷贝保证不可变
            return (Set<E>) Collections.unmodifiableSet(enumSet.clone());
        }
        // 在Set的copy方法中会先调用new HashSet拷贝数据。
        // 我们进行一次判断并显式调用toArray可减少一次不必要的拷贝
        if (src.getClass() == HashSet.class) {
            return (Set<E>) Set.of(src.toArray());
        } else {
            return Set.copyOf(src);
        }
    }

    /** 用于需要保持元素顺序的场景 */
    public static <E> Set<E> toImmutableLinkedHashSet(@Nullable Collection<E> src) {
        if (src == null || src.isEmpty()) {
            return Set.of();
        }
        return Collections.unmodifiableSet(new LinkedHashSet<>(src));
    }
    // endregion

    // region map

    /** 创建一个能存储指定元素数量的HashMap */
    public static <K, V> HashMap<K, V> newHashMap(int size) {
        return new HashMap<>(capacity(size));
    }

    /** 创建一个包含初始kv的HashMap */
    public static <K, V> HashMap<K, V> newHashMap(K k, V v) {
        HashMap<K, V> map = new HashMap<>(4);
        map.put(k, v);
        return map;
    }

    public static <K, V> HashMap<K, V> newHashMap(K k, V v, K k2, V v2) {
        HashMap<K, V> map = new HashMap<>(4);
        map.put(k, v);
        map.put(k2, v2);
        return map;
    }

    /** 创建一个能存储指定元素数量的LinkedHashMap */
    public static <K, V> LinkedHashMap<K, V> newLinkedHashMap(int size) {
        return new LinkedHashMap<>(capacity(size));
    }

    /** 创建一个包含初始kv的LinkedHashMap */
    public static <K, V> LinkedHashMap<K, V> newLinkedHashMap(K k, V v) {
        LinkedHashMap<K, V> map = new LinkedHashMap<>(4);
        map.put(k, v);
        return map;
    }

    public static <K, V> LinkedHashMap<K, V> newLinkedHashMap(K k, V v, K k2, V v2) {
        LinkedHashMap<K, V> map = new LinkedHashMap<>(4);
        map.put(k, v);
        map.put(k2, v2);
        return map;
    }

    public static <K, V> IdentityHashMap<K, V> newIdentityHashMap(int size) {
        return new IdentityHashMap<>(size);
    }

    /** 如果给定键不存在则抛出异常 */
    public static <K, V> V getOrThrow(Map<K, V> map, K key) {
        V v = map.get(key);
        if (v == null && !map.containsKey(key)) {
            throw new IllegalArgumentException(String.format("key is absent, key %s", key));
        }
        return v;
    }

    public static <K, V> V getOrThrow(Map<K, V> map, K key, String property) {
        V v = map.get(key);
        if (v == null && !map.containsKey(key)) {
            throw new IllegalArgumentException(String.format("%s is absent, key %s", nullToDef(property, "key"), key));
        }
        return v;
    }

    /** @throws NoSuchElementException 如果map为空 */
    public static <K> K firstKey(Map<K, ?> map) {
        // jdk21 LinkedHashMap总算可以获取首个键值对了...
        if (map instanceof SequencedMap<K, ?> sequencedMap) {
            return sequencedMap.firstEntry().getKey();
        } else {
            return map.keySet().iterator().next();
        }
    }

    /** @throws NoSuchElementException 如果map为空 */
    public static <K, V> Map.Entry<K, V> firstEntry(Map<K, V> map) {
        if (map instanceof SequencedMap<K, V> sequencedMap) {
            return sequencedMap.firstEntry();
        } else {
            return map.entrySet().iterator().next();
        }
    }

    public static <K, V> Map<V, K> inverseMap(Map<K, V> src) {
        Map<V, K> out = newHashMap(src.size());
        src.forEach((k, v) -> out.put(v, k));
        return out;
    }

    public static <K, V> Map<V, K> inverseMap(Map<K, V> src, Map<V, K> out) {
        src.forEach((k, v) -> out.put(v, k));
        return out;
    }

    @SuppressWarnings("unchecked")
    @Nonnull
    public static <K, V> Map<K, V> toImmutableMap(@Nullable Map<K, V> src) {
        if ((src == null || src.isEmpty())) {
            return Map.of();
        }
        if (src instanceof EnumMap<?, ?> enumMap) { // EnumMap使用代理的方式更好，但要先拷贝保证不可变
            return (Map<K, V>) Collections.unmodifiableMap(enumMap.clone());
        }
        return Map.copyOf(src);
    }

    /** 转换为不可变的{@link LinkedHashMap}，通常用于需要保留Key的顺序的场景 */
    public static <K, V> Map<K, V> toImmutableLinkedHashMap(@Nullable Map<K, V> src) {
        if ((src == null || src.isEmpty())) {
            return Map.of();
        }
        return Collections.unmodifiableMap(new LinkedHashMap<>(src));
    }
    // endregion

    // region 通用扩展

    public static boolean isEmpty(Map<?, ?> map) {
        return map == null || map.isEmpty();
    }

    public static boolean isEmpty(Collection<?> collection) {
        return collection == null || collection.isEmpty();
    }

    public static boolean isNotEmptyList(@Nullable Object obj) {
        return obj instanceof List<?> && ((List<?>) obj).size() > 0;
    }

    public static boolean isNotEmptyCollection(@Nullable Object obj) {
        return obj instanceof Collection<?> && ((Collection<?>) obj).size() > 0;
    }

    public static boolean isNotEmptyMap(@Nullable Object obj) {
        return obj instanceof Map<?, ?> && ((Map<?, ?>) obj).size() > 0;
    }

    public static void clear(@Nullable Collection<?> collection) {
        if (collection != null) collection.clear();
    }

    public static void clear(@Nullable Map<?, ?> map) {
        if (map != null) map.clear();
    }

    /** 如果两个集合存在公共元素，则返回true */
    public static boolean joint(Collection<?> source, Collection<?> candidates) {
        if (isEmpty(source) || isEmpty(candidates)) {
            return false;
        }
        return !Collections.disjoint(source, candidates);
    }

    /** 如果两个集合没有任何公共元素，则返回true */
    public static boolean disjoint(Collection<?> source, Collection<?> candidates) {
        if (isEmpty(source) || isEmpty(candidates)) {
            return true;
        }
        return Collections.disjoint(source, candidates);
    }

    /** 获取集合的首个元素 */
    public static <E> E first(Collection<E> elements) {
        if (elements instanceof SequencedCollection<E> sequenced) {
            return sequenced.getFirst();
        }
        return elements.iterator().next();
    }

    /** 如果集合不为空，则返回第一个元素，否则返回默认值 */
    @Nullable
    public static <E> E firstOrDefault(Collection<E> collection, E def) {
        if (collection == null || collection.isEmpty()) {
            return def;
        }
        if (collection instanceof SequencedCollection<E> sequenced) {
            return sequenced.getFirst();
        }
        return collection.iterator().next();
    }

    /**
     * 移除集合中第一个匹配的元素
     *
     * @param collection 可修改的集合
     * @param predicate  删除条件，为true的删除。
     * @param <E>        集合中的元素类型
     * @return 是否成功删除了一个元素
     */
    public static <E> boolean removeFirstMatch(Collection<E> collection, Predicate<? super E> predicate) {
        if (collection.size() == 0) {
            return false;
        }
        if (collection instanceof RandomAccess) {
            final List<E> list = (List<E>) collection;
            return removeFirstMatch(list, predicate);
        }
        for (Iterator<E> itr = collection.iterator(); itr.hasNext(); ) {
            if (predicate.test(itr.next())) {
                itr.remove();
                return true;
            }
        }
        return false;
    }

    /**
     * 使用“==”删除第一个匹配的元素
     */
    public static boolean removeRef(Collection<?> collection, Object element) {
        if (collection.isEmpty()) {
            return false;
        }
        if (collection instanceof RandomAccess) {
            final List<?> list = (List<?>) collection;
            return removeRef(list, element);
        }
        for (Iterator<?> iterator = collection.iterator(); iterator.hasNext(); ) {
            final Object e = iterator.next();
            if (e == element) {
                iterator.remove();
                return true;
            }
        }
        return false;
    }

    // endregion

    // region stream

    /** Converts iterator to a stream. */
    public static <T> Stream<T> streamOf(final Iterator<T> iterator) {
        Spliterator<T> spliterator = Spliterators.spliteratorUnknownSize(iterator, 0);
        return StreamSupport.stream(spliterator, false);
    }

    /** Converts interable to a non-parallel stream. */
    public static <T> Stream<T> streamOf(final Iterable<T> iterable) {
        return StreamSupport.stream(iterable.spliterator(), false);
    }

    // endregion

    // region 减少库依赖的方法

    public static int capacity(int expectedSize) {
        Preconditions.checkNonNegative(expectedSize, "expectedSize");
        if (expectedSize < 3) {
            return 4;
        }
        if (expectedSize < MathCommon.MAX_POWER_OF_TWO) {
            return (int) ((float) expectedSize / 0.75F + 1.0F);
        }
        return Integer.MAX_VALUE;
    }

    // endregion

    private static class ArrayHolder<E> extends AbstractCollection<E> {

        final E[] array;
        final int offset;
        final int length;

        private ArrayHolder(E[] array, int offset, int length) {
            this.array = Objects.requireNonNull(array);
            this.offset = offset;
            this.length = length;
        }

        @Nonnull
        @Override
        public Object[] toArray() {
            if (offset == 0 && length == array.length) {
                return array;
            }
            Object[] dest = new Object[length];
            System.arraycopy(array, offset, dest, 0, length);
            return dest;
        }

        @Nonnull
        @Override
        public Iterator<E> iterator() {
            throw new UnsupportedOperationException();
        }

        @Override
        public int size() {
            return length;
        }
    }

}