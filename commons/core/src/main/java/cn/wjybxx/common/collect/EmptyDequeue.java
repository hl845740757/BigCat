package cn.wjybxx.common.collect;

import org.apache.commons.lang3.ArrayUtils;

import javax.annotation.Nonnull;
import java.util.*;
import java.util.function.Consumer;
import java.util.function.IntFunction;
import java.util.function.Predicate;
import java.util.stream.Stream;

/**
 * @param <E>
 * @author wjybxx
 * date 2023/12/1
 */
public final class EmptyDequeue<E> implements Deque<E> {

    public static final EmptyDequeue<?> INSTANCE = new EmptyDequeue<>();
    private static final String MSG_FULL = "queue is full";

    public EmptyDequeue() {
    }

    @SuppressWarnings("unchecked")
    public static <E> EmptyDequeue<E> getInstance() {
        return (EmptyDequeue<E>) INSTANCE;
    }

    // region queue
    @Override
    public boolean add(E e) {
        throw new IllegalStateException(MSG_FULL);
    }

    @Override
    public boolean offer(E e) {
        return false;
    }

    @Override
    public E remove() {
        throw new NoSuchElementException();
    }

    @Override
    public E poll() {
        return null;
    }

    @Override
    public E element() {
        throw new NoSuchElementException();
    }

    @Override
    public E peek() {
        return null;
    }

    // endregion

    // region stack

    @Override
    public void push(E e) {
        throw new IllegalStateException(MSG_FULL);
    }

    @Override
    public E pop() {
        throw new NoSuchElementException();
    }

    // endregion

    // region dequeue

    @Override
    public void addFirst(E e) {
        throw new IllegalStateException(MSG_FULL);
    }

    @Override
    public void addLast(E e) {
        throw new IllegalStateException(MSG_FULL);
    }

    @Override
    public boolean offerFirst(E e) {
        return false;
    }

    @Override
    public boolean offerLast(E e) {
        return false;
    }

    @Override
    public E getFirst() {
        throw new NoSuchElementException();
    }

    @Override
    public E getLast() {
        throw new NoSuchElementException();
    }

    @Override
    public E removeFirst() {
        throw new NoSuchElementException();
    }

    @Override
    public E removeLast() {
        throw new NoSuchElementException();
    }

    @Override
    public E peekFirst() {
        return null;
    }

    @Override
    public E pollFirst() {
        return null;
    }

    @Override
    public E peekLast() {
        return null;
    }

    @Override
    public E pollLast() {
        return null;
    }
    // endregion

    // region

    @Override
    public int size() {
        return 0;
    }

    @Override
    public boolean isEmpty() {
        return true;
    }

    @Override
    public void clear() {

    }

    @Override
    public boolean contains(Object o) {
        return false;
    }

    @Override
    public boolean remove(Object o) {
        return false;
    }

    @Override
    public boolean removeFirstOccurrence(Object o) {
        return false;
    }

    @Override
    public boolean removeLastOccurrence(Object o) {
        return false;
    }

    @Override
    public boolean containsAll(@Nonnull Collection<?> c) {
        return c.isEmpty();
    }

    @Override
    public boolean removeAll(@Nonnull Collection<?> c) {
        return false;
    }

    @Override
    public boolean addAll(Collection<? extends E> c) {
        if (c.isEmpty()) return false;
        throw new IllegalStateException(MSG_FULL);
    }

    @Override
    public boolean retainAll(@Nonnull Collection<?> c) {
        return false;
    }

    @Nonnull
    @Override
    public Object[] toArray() {
        return ArrayUtils.EMPTY_OBJECT_ARRAY;
    }

    @Nonnull
    @Override
    public <T> T[] toArray(T[] a) {
        if (a.length == 0) return a;
        return Arrays.copyOf(a, 0);
    }

    @Override
    public <T> T[] toArray(IntFunction<T[]> generator) {
        return generator.apply(0);
    }

    @Override
    public Spliterator<E> spliterator() {
        return Spliterators.emptySpliterator();
    }

    @Override
    public Stream<E> stream() {
        return Stream.empty();
    }

    @Override
    public Stream<E> parallelStream() {
        return Stream.empty();
    }

    @SuppressWarnings("unchecked")
    @Nonnull
    @Override
    public Iterator<E> iterator() {
        return (Iterator<E>) EmptyIterator.EMPTY_ITERATOR;
    }

    @Nonnull
    @Override
    public Iterator<E> descendingIterator() {
        return iterator();
    }

    @Override
    public void forEach(Consumer<? super E> action) {

    }

    @Override
    public boolean removeIf(Predicate<? super E> filter) {
        return false;
    }

    @Override
    public Deque<E> reversed() {
        return this;
    }

    // endregion

    private static class EmptyIterator<E> implements Iterator<E> {

        public static final EmptyIterator<?> EMPTY_ITERATOR = new EmptyIterator<>();

        @Override
        public boolean hasNext() {
            return false;
        }

        @Override
        public E next() {
            throw new NoSuchElementException();
        }
    }

}