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

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.util.*;
import java.util.function.BiConsumer;
import java.util.function.BiPredicate;
import java.util.function.Consumer;
import java.util.function.Predicate;
import java.util.stream.Stream;
import java.util.stream.StreamSupport;

/**
 * @author wjybxx
 * date 2023/3/31
 */
public class CollectionUtils {

    private CollectionUtils() {

    }

    // region list扩展

    /**
     * 注意：如果list包含null，且def也是null，则返回Null时无法判断是否来自集合。
     */
    public static <E> E getOrDefault(List<E> elements, int index, E def) {
        if (elements == null || index < 0 || index >= elements.size()) {
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
        return elements.get(elements.size() - 1);
    }

    /** 删除list的前n个元素 */
    public static void removeTopN(List<?> list, int n) {
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
        } else if (n <= 8) {
            int size = list.size();
            for (; n > 0; n--, size--) {
                list.remove(size - 1);
            }
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
            int tailIndex = size - 1;
            if (index < tailIndex) {
                list.set(index, list.get(tailIndex));
            }
            list.remove(tailIndex);
            size--;
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

    /** 返回List中第一个不为null的元素 */
    public static <E> E firstPresent(List<E> list) {
        for (int i = 0, size = list.size(); i < size; i++) {
            final E e = list.get(i);
            if (e != null) return e;
        }
        return null;
    }

    /** 返回list中最后一个不为null的元素 */
    public static <E> E lastPresent(List<E> list) {
        for (int i = list.size() - 1; i >= 0; i--) {
            final E e = list.get(i);
            if (e != null) return e;
        }
        return null;
    }

    /** 返回List中非null元素个数 */
    public static int presentCount(List<?> list) {
        int count = 0;
        for (int i = list.size() - 1; i >= 0; i--) {
            final Object e = list.get(i);
            if (e != null) count++;
        }
        return count;
    }

    // region 使用“==”操作集合
    // 注意：对于拆装箱的对象慎用

    public static boolean containsRef(List<?> list, Object element) {
        for (int i = 0, size = list.size(); i < size; i++) {
            if (list.get(i) == element) {
                return true;
            }
        }
        return false;
    }

    /**
     * 使用“==”查询元素位置
     */
    public static int indexOfRef(List<?> list, Object element) {
        for (int i = 0, size = list.size(); i < size; i++) {
            if (list.get(i) == element) {
                return i;
            }
        }
        return -1;
    }

    public static int lastIndexOfRef(List<?> list, Object element) {
        for (int i = list.size() - 1; i >= 0; i--) {
            if (list.get(i) == element) {
                return i;
            }
        }
        return -1;
    }

    /**
     * 使用“==”删除对象
     */
    public static boolean removeRef(List<?> list, Object element) {
        final int index = indexOfRef(list, element);
        if (index < 0) {
            return false;
        }
        removeAt(list, index, true);
        return true;
    }

    /**
     * 使用“==”删除对象
     */
    public static boolean removeRef(List<?> list, Object element, boolean ordered) {
        final int index = indexOfRef(list, element);
        if (index < 0) {
            return false;
        }
        removeAt(list, index, ordered);
        return true;
    }
    // endregion

    // region arrayList快捷方法

    public static <E> ArrayList<E> newArrayList() {
        return new ArrayList<>();
    }

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

    // endregion

    /** 连接多个列表 */
    @SafeVarargs
    public static <E> List<E> concat(List<E> first, List<? extends E> second, List<E>... more) {
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
    // endregion


    // region 通用扩展

    @Nullable
    public static <E> E firstOrDefault(Collection<E> collection, E def) {
        if (collection == null || collection.isEmpty()) {
            return def;
        }
        if (collection instanceof List) {
            return ((List<E>) collection).get(0);
        }
        if (collection instanceof SortedSet) {
            return ((SortedSet<E>) collection).first();
        }
        return collection.iterator().next();
    }

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

    /** 如果两个集合存在公共元素，则返回true */
    public static boolean intersect(Collection<?> source, Collection<?> candidates) {
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

    /**
     * 删除集合中满足条件的元素，并对删除的元素执行后续逻辑。
     * 建议map使用{@link #removeIfAndThen(Map, BiPredicate, BiConsumer)}进行删除
     *
     * @param collection 可修改的集合
     * @param filter     什么样的元素需要删除
     * @param action     元素删除之后执行逻辑
     * @param <E>        元素的类型。注意：不可以是{@link Map.Entry}
     * @return 返回删除成功的元素
     */
    public static <E> int removeIfAndThen(final Collection<E> collection, final Predicate<E> filter,
                                          @Nullable final Consumer<E> action) {
        if (collection.size() == 0) {
            return 0;
        }

        int removed = 0;
        E e;
        for (Iterator<E> iterator = collection.iterator(); iterator.hasNext(); ) {
            e = iterator.next();
            if (filter.test(e)) {
                iterator.remove();
                removed++;
                if (null != action) {
                    action.accept(e);
                }
            }
        }
        return removed;
    }

    /**
     * 移除map中符合条件的元素，并对删除的元素执行后续的操作
     *
     * @param map       必须是可修改的map
     * @param predicate 过滤条件，为真的删除
     * @param then      元素删除之后执行的逻辑
     * @param <K>       the type of key
     * @param <V>       the type of value
     * @return 删除的元素数量
     */
    public static <K, V> int removeIfAndThen(final Map<K, V> map, final BiPredicate<? super K, ? super V> predicate,
                                             @Nullable final BiConsumer<K, V> then) {
        if (map.size() == 0) {
            return 0;
        }

        int removed = 0;
        // entry在调用remove之后不可再访问,因此需要将key-value保存下来
        Map.Entry<K, V> kvEntry;
        K k;
        V v;
        for (Iterator<Map.Entry<K, V>> itr = map.entrySet().iterator(); itr.hasNext(); ) {
            kvEntry = itr.next();
            k = kvEntry.getKey();
            v = kvEntry.getValue();
            if (predicate.test(k, v)) {
                itr.remove();
                removed++;
                if (null != then) {
                    then.accept(k, v);
                }
            }
        }
        return removed;
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
     * 使用“==”删除对象
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

    @Nonnull
    @SuppressWarnings("unchecked")
    public static <E> Set<E> toImmutableSet(@Nullable Collection<E> src) {
        if ((src == null || src.isEmpty())) {
            return Set.of();
        }
        // 在Set的copy方法中会先调用new HashSet拷贝数据。
        // 我们进行一次判断并显式调用toArray可减少一次不必要的拷贝
        if (src.getClass() == HashSet.class) {
            return (Set<E>) Set.of(src.toArray());
        } else {
            return Set.copyOf(src);
        }
    }

    @Nonnull
    public static <K, V> Map<K, V> toImmutableMap(@Nullable Map<K, V> src) {
        return (src == null || src.isEmpty()) ? Map.of() : Map.copyOf(src);
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

    // region check

    /** 检查集合里是否存在null，如果元素里存在null则抛出异常 */
    public static void checkNullElements(Collection<?> c) {
        if (c instanceof RandomAccess && c instanceof List) {
            // produce less garbage
            List<?> list = (List<?>) c;
            int size = list.size();
            for (int i = 0; i < size; i++) {
                if (list.get(i) == null) {
                    throw new IllegalArgumentException("c contains null values");
                }
            }
        } else {
            for (Object element : c) {
                if (element == null) {
                    throw new IllegalArgumentException("c contains null values");
                }
            }
        }
    }

    public static <K, V> V checkGet(Map<K, V> map, K key, String property) {
        V v = map.get(key);
        if (v == null && !map.containsKey(key)) {
            throwAbsentException(key, property);
        }
        return v;
    }

    public static <K, V> void requireNotContains(Map<K, V> map, K key, String property) {
        if (map.containsKey(key)) {
            throwDuplicateException(key, property);
        }
    }

    public static <K, V> void requireContains(Map<K, V> map, K key, String property) {
        if (!map.containsKey(key)) {
            throwAbsentException(key, property);
        }
    }

    private static <K> void throwDuplicateException(K key, String property) {
        if (property == null) {
            throw new IllegalArgumentException("key is duplicate: " + key);
        } else {
            throw new IllegalArgumentException(property + " is duplicate: " + key);
        }
    }

    private static <K> void throwAbsentException(K key, String property) {
        if (property == null) {
            throw new IllegalArgumentException("key is absent: " + key);
        } else {
            throw new IllegalArgumentException(property + " is absent: " + key);
        }
    }

    // endregion
}