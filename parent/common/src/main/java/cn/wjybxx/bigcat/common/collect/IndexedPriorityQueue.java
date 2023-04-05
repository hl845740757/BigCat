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

import java.util.Queue;

/**
 * 参考自netty的实现
 * 由于{@link java.util.Collection}中的API是基于Object的，不利于查询性能，添加了一些限定类型的方法。
 * <p>
 * 将Node在队列中的索引存储在元素上，其目的提高查询效率，但该设计是危险的。
 * 另一种折中方式是让用户像{@link java.lang.ref.Reference}一样使用自己的对象，
 * 这种方式的话就可以提供一个受信任的Node实现，提高安全性，不过用户的使用体验上会差一些。
 * 现在做了个简单实现：{@link RefIndexedNode}
 *
 * @author wjybxx
 * date 2023/4/3
 */
public interface IndexedPriorityQueue<T extends IndexedPriorityQueue.IndexedNode> extends Queue<T> {

    boolean removeTyped(T node);

    boolean containsTyped(T node);

    void priorityChanged(T node);

    /**
     * 清除队列中的所有元素，并不更新队列中节点的索引，通常用在最后清理释放内存的时候。
     * 即在清理的时候不调用{@link IndexedNode#priorityQueueIndex(IndexedPriorityQueue, int)}方法进行进通知。
     * (请确保调用该方法后，不会再访问该队列)
     */
    void clearIgnoringIndexes();

    /** 优先级队列中的节点，为提高效率，缓存了其在队列中的索引，以提高查询效率 */
    interface IndexedNode {

        /** 注意：未插入的节点的所以必须初始化为该值 */
        int INDEX_NOT_IN_QUEUE = -1;

        /**
         * 获取对象在队列中的索引；
         *
         * @param queue 考虑到一个元素可能在多个队列中，因此传入队列引用
         */
        int priorityQueueIndex(IndexedPriorityQueue<?> queue);

        /**
         * 设置其在队列中的索引
         *
         * @param index 如果是删除元素，则索引为-1
         */
        void priorityQueueIndex(IndexedPriorityQueue<?> queue, int index);

    }

}