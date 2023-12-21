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

package cn.wjybxx.common.collect;

import java.util.Comparator;
import java.util.Objects;

/**
 * @author wjybxx
 * date 2023/4/3
 */
public final class RefIndexedElement<E> implements IndexedElement {

    private final E e;
    private Object queue;
    private int index = INDEX_NOT_FOUNT;

    /** 封闭，允许未来切换实现 */
    private RefIndexedElement(E e) {
        this.e = Objects.requireNonNull(e);
    }

    public static <E> RefIndexedElement<E> of(E e) {
        return new RefIndexedElement<>(e);
    }

    public E get() {
        return e;
    }

    @Override
    public int collectionIndex(Object collection) {
        return this.queue == collection ? this.index : INDEX_NOT_FOUNT;
    }

    @Override
    public void collectionIndex(Object collection, int index) {
        if (index >= 0) {
            assert this.queue == collection || this.queue == null;
            this.queue = collection;
            this.index = index;
        } else {
            this.queue = null;
            this.index = INDEX_NOT_FOUNT;
        }
    }

    public static <E> Comparator<RefIndexedElement<E>> wrapComparator(Comparator<? super E> comparator) {
        return new ComparatorAdapter<>(Objects.requireNonNull(comparator));
    }

    private static class ComparatorAdapter<E> implements Comparator<RefIndexedElement<E>> {

        private final Comparator<? super E> adaptee;

        private ComparatorAdapter(Comparator<? super E> adaptee) {
            this.adaptee = adaptee;
        }

        @Override
        public int compare(RefIndexedElement<E> o1, RefIndexedElement<E> o2) {
            return adaptee.compare(o1.get(), o2.get());
        }
    }
}