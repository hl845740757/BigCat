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

import java.util.Queue;

/**
 * 参考自netty的实现
 * 由于{@link java.util.Collection}中的API是基于Object的，不利于查询性能，添加了一些限定类型的方法。
 * <p>
 * 将Node在队列中的索引存储在元素上，其目的提高查询效率，但该设计是危险的。
 * 另一种折中方式是让用户像{@link java.lang.ref.Reference}一样使用自己的对象，这种方式的话用户的使用体验上会差一些。
 * 现在做了个简单实现：{@link RefIndexedNode}
 *
 * @author wjybxx
 * date 2023/4/3
 */
public interface IndexedPriorityQueue<T extends IndexedNode> extends Queue<T> {

    boolean removeTyped(T node);

    boolean containsTyped(T node);

    /**
     * 队列中节点元素的优先级发生变化时，将通过该方法通知队列调整
     *
     * @param node 发生优先级变更的节点
     */
    void priorityChanged(T node);

    /**
     * 清除队列中的所有元素，并不更新队列中节点的索引，通常用在最后清理释放内存的时候。
     * 即在清理的时候不调用{@link IndexedNode#queueIndex(Object, int)}方法进行进通知。
     * (请确保调用该方法后，不会再访问该队列)
     */
    void clearIgnoringIndexes();

}