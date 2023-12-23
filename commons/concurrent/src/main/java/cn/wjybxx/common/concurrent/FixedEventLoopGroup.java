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

package cn.wjybxx.common.concurrent;

import javax.annotation.Nonnull;
import javax.annotation.concurrent.ThreadSafe;

/**
 * 固定数量{@link EventLoop}的事件循环线程组
 * 它提供了相同key选择相同{@link EventLoop}的方法。
 *
 * @author wjybxx
 * date 2023/4/7
 */
@ThreadSafe
public interface FixedEventLoopGroup extends EventLoopGroup {

    /**
     * 通过一个键选择一个{@link EventLoop}
     * 这提供了第二种绑定线程的方式，第一种方式是通过{@link #select()}分配一个线程，让业务对象持有{@link EventLoop}的引用。
     * 现在，你可以为用户分配一个键，通过键建立虚拟绑定。
     *
     * @param key 计算索引的键；限定int可保证选择性能
     * @apiNote 必须保证同一个key分配的结果一定是相同的
     */
    @Nonnull
    EventLoop select(int key);

    /**
     * 返回{@link EventLoop}的数量。
     */
    int numChildren();

}