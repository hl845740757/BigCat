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

package cn.wjybxx.common.dson;

import javax.annotation.Nonnull;
import java.util.*;
import java.util.function.Consumer;
import java.util.function.IntFunction;
import java.util.function.Predicate;
import java.util.function.UnaryOperator;

/**
 * @author wjybxx
 * date - 2023/4/19
 */
public abstract class DsonArray<K> extends DsonValue implements List<DsonValue>, RandomAccess {

    final List<DsonValue> values;

    protected DsonArray(List<DsonValue> values) {
        this.values = values;
    }

    //

    public static <K> DsonArray<K> toImmutable(DsonArray<K> src) {
        return ImmutableDsons.dsonArray(src);
    }

    public static <K> DsonArray<K> empty() {
        return ImmutableDsons.dsonArray();
    }

    @Nonnull
    public abstract DsonHeader<K> getHeader();

    public abstract DsonArray<K> setHeader(DsonHeader<K> header);

    @Nonnull
    @Override
    public final DsonType getDsonType() {
        return DsonType.ARRAY;
    }

    public List<DsonValue> getValues() {
        return Collections.unmodifiableList(values);
    }

    // equals和hash不测试header，只要内容一致即可

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        return o instanceof DsonArray<?> that && values.equals(that.values);
    }

    @Override
    public int hashCode() {
        return values.hashCode();
    }

    @Override
    public String toString() {
        return getClass().getSimpleName() + "{" +
                "values=" + values +
                ", header=" + getHeader() +
                '}';
    }

    // region 代理实现

    @Override
    public int size() {
        return values.size();
    }

    @Override
    public boolean isEmpty() {
        return values.isEmpty();
    }

    @Override
    public boolean contains(Object o) {
        return values.contains(o);
    }

    @Override
    public <T> T[] toArray(T[] a) {
        return values.toArray(a);
    }

    @Override
    public boolean add(DsonValue dsonValue) {
        return values.add(dsonValue);
    }

    @Override
    public boolean remove(Object o) {
        return values.remove(o);
    }

    @Override
    public boolean containsAll(Collection<?> c) {
        return values.containsAll(c);
    }

    @Override
    public boolean addAll(Collection<? extends DsonValue> c) {
        return values.addAll(c);
    }

    @Override
    public boolean addAll(int index, Collection<? extends DsonValue> c) {
        return values.addAll(index, c);
    }

    @Override
    public boolean removeAll(Collection<?> c) {
        return values.removeAll(c);
    }

    @Override
    public boolean retainAll(Collection<?> c) {
        return values.retainAll(c);
    }

    @Override
    public void replaceAll(UnaryOperator<DsonValue> operator) {
        values.replaceAll(operator);
    }

    @Override
    public void sort(Comparator<? super DsonValue> c) {
        values.sort(c);
    }

    @Override
    public void clear() {
        values.clear();
    }

    @Override
    public DsonValue get(int index) {
        return values.get(index);
    }

    @Override
    public DsonValue set(int index, DsonValue element) {
        return values.set(index, element);
    }

    @Override
    public void add(int index, DsonValue element) {
        values.add(index, element);
    }

    @Override
    public DsonValue remove(int index) {
        return values.remove(index);
    }

    @Override
    public int indexOf(Object o) {
        return values.indexOf(o);
    }

    @Override
    public int lastIndexOf(Object o) {
        return values.lastIndexOf(o);
    }

    @Override
    public Iterator<DsonValue> iterator() {
        return values.iterator();
    }

    @Override
    public ListIterator<DsonValue> listIterator() {
        return values.listIterator();
    }

    @Override
    public ListIterator<DsonValue> listIterator(int index) {
        return values.listIterator(index);
    }

    @Override
    public List<DsonValue> subList(int fromIndex, int toIndex) {
        return values.subList(fromIndex, toIndex);
    }

    @Override
    public Spliterator<DsonValue> spliterator() {
        return values.spliterator();
    }

    @Override
    public Object[] toArray() {
        return values.toArray();
    }

    @Override
    public <T> T[] toArray(IntFunction<T[]> generator) {
        return values.toArray(generator);
    }

    @Override
    public boolean removeIf(Predicate<? super DsonValue> filter) {
        return values.removeIf(filter);
    }

    @Override
    public void forEach(Consumer<? super DsonValue> action) {
        values.forEach(action);
    }

    // endregion
}
