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

/**
 * 被索引的节点
 * 其缓存了其在队列中的索引，以提高查询效率;
 * 将索引存储在队列中的元素上，有一定的使用限制，仅适合元素最多存在于1~2个队列中的情况，否则查询效率降低，适得其反。
 */
public interface IndexedNode {

    /** 注意：未插入的节点的所以必须初始化为该值 */
    int INDEX_NOT_IN_QUEUE = -1;

    /**
     * 获取对象在队列中的索引
     *
     * @param queue 考虑到一个元素可能在多个队列中，因此传入队列引用
     */
    int queueIndex(Object queue);

    /**
     * 设置其在队列中的索引
     *
     * @param index 如果是删除元素，则索引为-1
     */
    void queueIndex(Object queue, int index);

}
