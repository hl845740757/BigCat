package cn.wjybxx.common.collect;

import java.util.ArrayDeque;

/**
 * 滑动式双端队列
 * 1.当达到容量限制时，将自动移除另一端的元素。
 * 2.不支持插入null元素
 *
 * @author wjybxx
 * date 2023/12/1
 */
public class SlidingDequeue<E> implements Dequeue<E> {

    private int maxSize;
    private final ArrayDeque<E> arrayDeque;

    public SlidingDequeue(int maxSize) {
        if (maxSize < 0) throw new IllegalArgumentException("maxSize: " + maxSize);
        this.maxSize = maxSize;
        if (maxSize <= 10) {
            this.arrayDeque = new ArrayDeque<>(maxSize);
        } else {
            this.arrayDeque = new ArrayDeque<>();
        }
    }

    /**
     * @param maxSize     新的size限制
     * @param discardHead true表示丢弃head端数据，false表示丢弃tail端数据
     */
    public void setMaxSize(int maxSize, boolean discardHead) {
        if (maxSize < 0) throw new IllegalArgumentException("maxSize: " + maxSize);
        if (this.maxSize == maxSize) {
            return;
        }
        if (maxSize < arrayDeque.size()) {
            if (discardHead) {
                while (arrayDeque.size() > maxSize) {
                    arrayDeque.pollFirst();
                }
            } else {
                while (arrayDeque.size() > maxSize) {
                    arrayDeque.pollLast();
                }
            }
        }
        this.maxSize = maxSize;
    }

    @Override
    public void addFirst(E e) {
        if (arrayDeque.size() == maxSize) {
            throw new IllegalStateException();
        }
        arrayDeque.addFirst(e);
    }

    @Override
    public void addLast(E e) {
        if (arrayDeque.size() == maxSize) {
            throw new IllegalStateException();
        }
        arrayDeque.addLast(e);
    }

    @Override
    public boolean offerFirst(E e) {
        if (maxSize == 0) {
            return false;
        }
        if (arrayDeque.size() == maxSize) {
            arrayDeque.removeLast();
        }
        arrayDeque.offerFirst(e);
        return true;
    }

    @Override
    public boolean offerLast(E e) {
        if (maxSize == 0) {
            return false;
        }
        if (arrayDeque.size() == maxSize) {
            arrayDeque.removeFirst();
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

}