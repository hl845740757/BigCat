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

import cn.wjybxx.bigcat.common.collect.IndexedPriorityQueue.IndexedNode;

import javax.annotation.Nonnull;
import java.util.*;

/**
 * 参考自Netty的实现
 *
 * @author wjybxx
 * date 2023/4/3
 */
public class DefaultIndexedPriorityQueue<T extends IndexedNode> extends AbstractQueue<T>
        implements IndexedPriorityQueue<T> {

    private static final IndexedNode[] EMPTY_ARRAY = new IndexedNode[0];
    private static final int DEFAULT_CAPACITY = 16;

    private final Comparator<? super T> comparator;
    private T[] queue;
    private int size;

    public DefaultIndexedPriorityQueue(Comparator<? super T> comparator) {
        this(comparator, DEFAULT_CAPACITY);
    }

    @SuppressWarnings("unchecked")
    public DefaultIndexedPriorityQueue(Comparator<? super T> comparator, int initialSize) {
        this.comparator = Objects.requireNonNull(comparator, "comparator");
        queue = (T[]) (initialSize != 0 ? new IndexedNode[initialSize] : EMPTY_ARRAY);
    }

    @Override
    public int size() {
        return size;
    }

    @Override
    public boolean isEmpty() {
        return size == 0;
    }

    @Override
    public boolean contains(Object o) {
        if (!(o instanceof IndexedNode)) {
            return false;
        }
        IndexedNode node = (IndexedNode) o;
        return contains(node, node.priorityQueueIndex(this));
    }

    @Override
    public boolean containsTyped(T node) {
        return contains(node, node.priorityQueueIndex(this));
    }

    @Override
    public void clear() {
        for (int i = 0; i < size; ++i) {
            T node = queue[i];
            if (node != null) {
                setChildIndex(node, IndexedNode.INDEX_NOT_IN_QUEUE);
                queue[i] = null;
            }
        }
        size = 0;
    }

    @Override
    public void clearIgnoringIndexes() {
        Arrays.fill(queue, null);
        size = 0;
    }

    @Override
    public boolean offer(T e) {
        if (e.priorityQueueIndex(this) != IndexedNode.INDEX_NOT_IN_QUEUE) {
            throw new IllegalArgumentException("e.priorityQueueIndex(): " + e.priorityQueueIndex(this) +
                    " (expected: " + IndexedNode.INDEX_NOT_IN_QUEUE + ") + e: " + e);
        }

        if (size >= queue.length) {
            final int grow = (queue.length < 64) ? (queue.length + 2) : (queue.length >>> 1);
            queue = Arrays.copyOf(queue, queue.length + grow);
        }

        bubbleUp(size++, e);
        return true;
    }

    @Override
    public T poll() {
        if (size == 0) {
            return null;
        }
        T node = queue[0];
        removeAt(0, node);
        return node;
    }

    @Override
    public T peek() {
        return (size == 0) ? null : queue[0];
    }

    @SuppressWarnings("unchecked")
    @Override
    public boolean remove(Object o) {
        if (!(o instanceof IndexedNode)) {
            return false;
        }

        final T node = (T) o;
        return removeTyped(node);
    }

    @Override
    public boolean removeTyped(T node) {
        int i = node.priorityQueueIndex(this);
        if (!contains(node, i)) {
            return false;
        }
        removeAt(i, node);
        return true;
    }

    @Override
    public void priorityChanged(T node) {
        int i = node.priorityQueueIndex(this);
        if (!contains(node, i)) {
            return;
        }

        if (i == 0) {
            bubbleDown(i, node);
        } else {
            int iParent = (i - 1) >>> 1;
            T parent = queue[iParent];
            if (comparator.compare(node, parent) < 0) {
                bubbleUp(i, node);
            } else {
                bubbleDown(i, node);
            }
        }
    }

    @Nonnull
    @Override
    public Iterator<T> iterator() {
        return new PriorityQueueIterator();
    }

    private final class PriorityQueueIterator implements Iterator<T> {

        private int index;

        @Override
        public boolean hasNext() {
            return index < size;
        }

        @Override
        public T next() {
            if (index >= size) {
                throw new NoSuchElementException();
            }
            return queue[index++];
        }

    }

    private void setChildIndex(T child, int k) {
        child.priorityQueueIndex(this, k);
        assert child.priorityQueueIndex(this) == k : String.format("expected: %d, but found: %d", k, child.priorityQueueIndex(this));
    }

    private boolean contains(IndexedNode node, int idx) {
        // 使用equals是无意义的，如果要使用equals，那么索引i会导致漏判断
        return idx >= 0 && idx < size && node == queue[idx];
    }

    private void removeAt(int idx, T node) {
        setChildIndex(node, IndexedNode.INDEX_NOT_IN_QUEUE);

        int newSize = --size;
        if (newSize == idx) { // 如果删除的是最后一个元素则无需交换
            queue[idx] = null;
            return;
        }

        T moved = queue[idx] = queue[newSize];
        queue[newSize] = null;

        if (idx == 0 || comparator.compare(node, moved) < 0) {
            bubbleDown(idx, moved);
        } else {
            bubbleUp(idx, moved);
        }
    }

    private void bubbleDown(int k, T node) {
        final int half = size >>> 1;
        while (k < half) {
            int iChild = (k << 1) + 1;
            T child = queue[iChild];

            // 找到最小的子节点，如果父节点大于最小子节点，则与最小子节点交换
            int iRightChild = iChild + 1;
            if (iRightChild < size && comparator.compare(child, queue[iRightChild]) > 0) {
                child = queue[iChild = iRightChild];
            }
            if (comparator.compare(node, child) <= 0) {
                break;
            }

            queue[k] = child;
            setChildIndex(child, k);

            k = iChild;
        }

        queue[k] = node;
        setChildIndex(node, k);
    }

    private void bubbleUp(int k, T node) {
        while (k > 0) {
            int iParent = (k - 1) >>> 1;
            T parent = queue[iParent];

            // 如果node小于父节点，则node要与父节点进行交换
            if (comparator.compare(node, parent) >= 0) {
                break;
            }

            queue[k] = parent;
            setChildIndex(parent, k);

            k = iParent;
        }

        queue[k] = node;
        setChildIndex(node, k);
    }

}