package cn.wjybxx.common.collect;

import java.util.NoSuchElementException;

/**
 * @param <E>
 * @author wjybxx
 * date 2023/12/1
 */
public final class EmptyDequeue<E> implements Dequeue<E> {

    public static final EmptyDequeue<?> INSTANCE = new EmptyDequeue<>();

    public EmptyDequeue() {
    }

    @SuppressWarnings("unchecked")
    public static <E> EmptyDequeue<E> getInstance() {
        return (EmptyDequeue<E>) INSTANCE;
    }

    @Override
    public void addFirst(E e) {
        throw new IllegalStateException();
    }

    @Override
    public void addLast(E e) {
        throw new IllegalStateException();
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
    public boolean offerFirst(E e) {
        return false;
    }

    @Override
    public boolean offerLast(E e) {
        return false;
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

}