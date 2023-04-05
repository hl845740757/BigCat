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

package cn.wjybxx.bigcat.common.concurrent;

import javax.annotation.Nonnull;
import javax.annotation.concurrent.ThreadSafe;
import java.util.concurrent.CompletableFuture;

/**
 * 可监听的事件队列，用于在单线程架构下执行一些阻塞式操作
 * <p>
 * 监听器用于拦截插入到任务队列中的事件，队列在接收到一个事件时，将判断是否存在Watcher，
 * 1.如果不存在Watcher，事件将被插入任务队列。
 * 2.如果存在Watcher，事件将调用{@link Watcher#test(Object)}方法测试事件。
 * 2.1 如果不是Watcher等待的事件，事件将被插入任务队列。
 * 2.2 如果是Watcher等待的事件，将删除Watcher，然后调用{@link Watcher#onEvent(Object)}方法处理事件 -- 即：Watcher是一次性的。
 * <p>
 * 一些指导：
 * 1.监听器应该设定超时时间，不可无限阻塞，否则可能有死锁风险，或者总是超时失败 -- 如果任务队列是有界的。
 * 2.应当先watch再执行阻塞等操作，否则可能丢失信号
 * 3.在不需要使用的时候即使取消watch
 * 4.实现必须是线程安全的，因为事件的发布者通常是另一个线程 -- 通常可以通过{@link CompletableFuture}里实现跨线程数据传输
 * 5.监听和取消监听都是低频操作，因此可以简单实现为{@code synchronized}，但属性字段需要是{@code volatile}的（可参考测试用例实现）
 *
 * @author wjybxx
 * date 2023/4/5
 */
@ThreadSafe
public interface WatchableEventQueue<E> {

    /**
     * 监听队列中的事件，直到某一个事件发生。
     *
     * @param watcher 监听器
     * @throws NullPointerException watcher is null
     * @see Watcher
     */
    void watch(Watcher<? super E> watcher);

    /**
     * 取消监听
     *
     * @param watcher 用于判断是否是当前watcher
     */
    void cancelWatch(Watcher<?> watcher);

    /** 实现时要小心线程安全问题 */
    @ThreadSafe
    interface Watcher<E> {

        boolean test(@Nonnull E event);

        void onEvent(@Nonnull E event);

    }

}