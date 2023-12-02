package cn.wjybxx.common.collect;

import java.util.NoSuchElementException;

/**
 * 双端队列
 *
 * @param <E>
 * @author wjybxx
 * date 2023/12/1
 */
public interface Dequeue<E> {

    /**
     * @throws IllegalStateException 如果队列已满
     */
    void addFirst(E e);

    /**
     * @throws IllegalStateException 如果队列已满
     */
    void addLast(E e);

    /**
     * @return 插入成功则返回true，否则返回false（队列已满）
     */
    boolean offerFirst(E e);

    /**
     * @return 插入成功则返回true，否则返回false（队列已满）
     */
    boolean offerLast(E e);

    /**
     * @throws NoSuchElementException 如果队列为空
     */
    E getFirst();

    /**
     * @return 如果队列为空，则返回null
     */
    E peekFirst();

    /**
     * @throws NoSuchElementException 如果队列为空
     */
    E removeFirst();

    /**
     * @return 如果队列为空，则返回null
     */
    E pollFirst();

    /**
     * @throws NoSuchElementException 如果队列为空
     */
    E getLast();

    /**
     * @return 如果队列为空，则返回null
     */
    E peekLast();

    /**
     * @throws NoSuchElementException 如果队列为空
     */
    E removeLast();

    /**
     * @return 如果队列为空，则返回null
     */
    E pollLast();

    int size();

    boolean isEmpty();

    void clear();

}