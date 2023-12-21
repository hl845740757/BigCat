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
 * 被索引的元素
 * 1.索引信息存储在元素上，可大幅提高查找效率；
 * 2.如果对象可能存在多个集合中，慎重实现该接口，更建议为每个集合设置一个粘合对象；
 * <p>
 * 将对象在队列中的索引存储在元素上，可以有效提高查询效率，但该设计是危险的。
 * 另一种折中方式是让用户像{@link java.lang.ref.Reference}一样使用自己的对象，这种方式的话用户的使用体验上会差一些。
 * 现在做了个简单实现：{@link RefIndexedElement}
 */
public interface IndexedElement {

    /** 注意：未插入的节点的所以必须初始化为该值 */
    int INDEX_NOT_FOUNT = -1;

    /**
     * 获取对象在集合中的索引
     *
     * @param collection 考虑到一个元素可能在多个队列中，因此传入队列引用
     */
    int collectionIndex(Object collection);

    /**
     * 设置其在集合中的索引
     *
     * @param index 如果是删除元素，则索引为-1
     */
    void collectionIndex(Object collection, int index);

}
