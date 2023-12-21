package cn.wjybxx.common.collect;

import javax.annotation.Nonnull;
import java.util.*;
import java.util.function.Consumer;
import java.util.function.IntFunction;
import java.util.function.Predicate;
import java.util.stream.Stream;

/**
 * 滑动式双端队列
 * 1.当达到容量限制时，将自动移除另一端的元素。
 * 2.不支持插入null元素
 * 3.使用代理的方式实现，避免JDK新增接口忘记处理。
 *
 * @author wjybxx
 * date 2023/12/1
 */
public class BoundedArrayDeque<E> implements Deque<E> {

    private int capacity;
    private final DequeOverflowBehavior overflowBehavior;
    private final ArrayDeque<E> arrayDeque;

    public BoundedArrayDeque(int capacity, DequeOverflowBehavior overflowBehavior) {
        if (capacity < 0) throw new IllegalArgumentException("maxSize: " + capacity);
        this.capacity = capacity;
        this.overflowBehavior = overflowBehavior;
        if (capacity <= 10) {
            this.arrayDeque = new ArrayDeque<>(capacity);
        } else {
            this.arrayDeque = new ArrayDeque<>();
        }
    }

    /**
     * @param capacity         新的容量
     * @param overflowBehavior 容量缩小时的策略，不会保存
     */
    public void setCapacity(int capacity, DequeOverflowBehavior overflowBehavior) {
        if (capacity < 0) {
            throw new IllegalArgumentException("maxSize: " + capacity);
        }
        if (this.capacity == capacity) {
            return;
        }
        if (capacity < arrayDeque.size()) {
            switch (overflowBehavior) {
                case DISCARD_HEAD -> {
                    while (arrayDeque.size() > capacity) {
                        arrayDeque.pollFirst();
                    }
                }
                case DISCARD_TAIL -> {
                    while (arrayDeque.size() > capacity) {
                        arrayDeque.pollLast();
                    }
                }
                default -> throw new IllegalStateException();
            }
        }
        this.capacity = capacity;
    }

    // region queue

    @Override
    public boolean offer(E e) {
        return offerLast(e);
    }

    @Override
    public E remove() {
        return arrayDeque.remove();
    }

    @Override
    public E poll() {
        return arrayDeque.poll();
    }

    @Override
    public E element() {
        return arrayDeque.element();
    }

    @Override
    public E peek() {
        return arrayDeque.peek();
    }

    /** @throws IllegalStateException 队列已满 */
    @Override
    public boolean add(E e) {
        if (capacity == 0) {
            return false;
        }
        addLast(e);
        return true;
    }

    /** @throws IllegalStateException 队列已满 */
    @Override
    public boolean addAll(@Nonnull Collection<? extends E> c) {
        if (capacity == 0 || c.isEmpty()) {
            return false;
        }
        for (E e : c) {
            addLast(e);
        }
        return true;
    }

    // endregion

    // region deque

    /** @throws IllegalStateException 队列已满 */
    @Override
    public void addFirst(E e) {
        if (!offerFirst(e)) {
            throw new IllegalStateException("Queue is full");
        }
    }

    /** @throws IllegalStateException 队列已满 */
    @Override
    public void addLast(E e) {
        if (!offerLast(e)) {
            throw new IllegalStateException("Queue is full");
        }
    }

    @Override
    public boolean offerFirst(E e) {
        Objects.requireNonNull(e);
        if (arrayDeque.size() == capacity) {
            if (!overflowBehavior.allowDiscardTail()) {
                return false;
            }
            arrayDeque.pollLast();
        }
        arrayDeque.offerFirst(e);
        return true;
    }

    @Override
    public boolean offerLast(E e) {
        Objects.requireNonNull(e);
        if (arrayDeque.size() == capacity) {
            if (!overflowBehavior.allowDiscardHead()) {
                return false;
            }
            arrayDeque.pollFirst();
        }
        arrayDeque.offerLast(e);
        return true;
    }

    @Override
    public E getFirst() {
        return arrayDeque.getFirst();
    }

    @Override
    public E peekFirst() {
        return arrayDeque.peekFirst();
    }

    @Override
    public E removeFirst() {
        return arrayDeque.removeFirst();
    }

    @Override
    public E pollFirst() {
        return arrayDeque.pollFirst();
    }

    @Override
    public E getLast() {
        return arrayDeque.getLast();
    }

    @Override
    public E peekLast() {
        return arrayDeque.peekLast();
    }

    @Override
    public E removeLast() {
        return arrayDeque.removeLast();
    }

    @Override
    public E pollLast() {
        return arrayDeque.pollLast();
    }

    // endregion

    // region stack

    @Override
    public void push(E e) {
        addFirst(e);
    }

    @Override
    public E pop() {
        return arrayDeque.pop();
    }

    // endregion

    // region forward

    @Override
    public int size() {
        return arrayDeque.size();
    }

    @Override
    public boolean isEmpty() {
        return arrayDeque.isEmpty();
    }

    @Override
    public void clear() {
        arrayDeque.clear();
    }

    @Override
    public boolean contains(Object o) {
        return arrayDeque.contains(o);
    }

    @Override
    public boolean remove(Object o) {
        return arrayDeque.remove(o);
    }

    @Override
    public boolean removeFirstOccurrence(Object o) {
        return arrayDeque.removeFirstOccurrence(o);
    }

    @Override
    public boolean removeLastOccurrence(Object o) {
        return arrayDeque.removeLastOccurrence(o);
    }

    @Override
    public boolean removeIf(Predicate<? super E> filter) {
        return arrayDeque.removeIf(filter);
    }

    @Override
    public boolean containsAll(@Nonnull Collection<?> c) {
        return arrayDeque.containsAll(c);
    }

    @Override
    public boolean removeAll(@Nonnull Collection<?> c) {
        return arrayDeque.removeAll(c);
    }

    @Override
    public boolean retainAll(@Nonnull Collection<?> c) {
        return arrayDeque.retainAll(c);
    }

    @Nonnull
    @Override
    public Iterator<E> iterator() {
        return arrayDeque.iterator();
    }

    @Nonnull
    @Override
    public Iterator<E> descendingIterator() {
        return arrayDeque.descendingIterator();
    }

    @Override
    public Spliterator<E> spliterator() {
        return arrayDeque.spliterator();
    }

    @Override
    public void forEach(Consumer<? super E> action) {
        arrayDeque.forEach(action);
    }

    @Nonnull
    @Override
    public Object[] toArray() {
        return arrayDeque.toArray();
    }

    @Nonnull
    @Override
    public <T> T[] toArray(T[] a) {
        return arrayDeque.toArray(a);
    }

    @Override
    public <T> T[] toArray(IntFunction<T[]> generator) {
        return arrayDeque.toArray(generator);
    }

    @Override
    public Stream<E> stream() {
        return arrayDeque.stream();
    }

    @Override
    public Stream<E> parallelStream() {
        return arrayDeque.parallelStream();
    }

    // endregion
}