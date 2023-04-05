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

package cn.wjybxx.bigcat.common.collect;

import java.util.Comparator;
import java.util.Objects;

/**
 * @author wjybxx
 * date 2023/4/3
 */
public final class RefIndexedNode<E> implements IndexedPriorityQueue.IndexedNode {

    private final E e;
    private IndexedPriorityQueue<?> queue;
    private int index = INDEX_NOT_IN_QUEUE;

    /** 封闭，允许未来切换实现 */
    private RefIndexedNode(E e) {
        this.e = Objects.requireNonNull(e);
    }

    public static <E> RefIndexedNode<E> of(E e) {
        return new RefIndexedNode<>(e);
    }

    public E get() {
        return e;
    }

    @Override
    public int priorityQueueIndex(IndexedPriorityQueue<?> queue) {
        return this.queue == queue ? this.index : INDEX_NOT_IN_QUEUE;
    }

    @Override
    public void priorityQueueIndex(IndexedPriorityQueue<?> queue, int index) {
        if (index >= 0) {
            assert this.queue == null || this.queue == queue;
            this.queue = queue;
            this.index = index;
        } else {
            this.queue = null;
            this.index = INDEX_NOT_IN_QUEUE;
        }
    }

    public static <E> Comparator<RefIndexedNode<E>> wrapComparator(Comparator<? super E> comparator) {
        return new ComparatorAdapter<>(Objects.requireNonNull(comparator));
    }

    private static class ComparatorAdapter<E> implements Comparator<RefIndexedNode<E>> {

        private final Comparator<? super E> adaptee;

        private ComparatorAdapter(Comparator<? super E> adaptee) {
            this.adaptee = adaptee;
        }

        @Override
        public int compare(RefIndexedNode<E> o1, RefIndexedNode<E> o2) {
            return adaptee.compare(o1.get(), o2.get());
        }
    }
}