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

import cn.wjybxx.common.MathCommon;

import javax.annotation.Nonnull;
import javax.annotation.concurrent.ThreadSafe;
import java.util.concurrent.atomic.AtomicInteger;

/**
 * @author wjybxx
 * date 2023/4/7
 */
public class DefaultChooserFactory implements EventLoopChooserFactory {

    @Nonnull
    @Override
    public EventLoopChooser newChooser(EventLoop[] children) {
        if (children.length == 1) {
            return new SingleEventLoopChooser(children[0]);
        }
        if (MathCommon.isPowerOfTwo(children.length)) {
            return new PowerOfTwoEventLoopChooser(children);
        }
        return new RoundRobinEventLoopChooser(children);
    }

    /**
     * 只有单个{@link EventLoop}的选择器
     */
    private static final class SingleEventLoopChooser implements EventLoopChooser {

        private final EventLoop eventLoop;

        private SingleEventLoopChooser(EventLoop eventLoop) {
            this.eventLoop = eventLoop;
        }

        @Nonnull
        @Override
        public EventLoop select() {
            return eventLoop;
        }

        @Override
        public EventLoop select(int key) {
            return eventLoop;
        }
    }

    /**
     * 如果线程数为2的整次幂，使用与运算代替除法
     */
    private static final class PowerOfTwoEventLoopChooser implements EventLoopChooser {

        private final AtomicInteger idx = new AtomicInteger();
        private final EventLoop[] executors;

        PowerOfTwoEventLoopChooser(EventLoop[] executors) {
            this.executors = executors;
        }

        @Nonnull
        @Override
        public EventLoop select() {
            int key = idx.getAndIncrement();
            return executors[key & (executors.length - 1)];
        }

        @Nonnull
        @Override
        public EventLoop select(int key) {
            return executors[key & (executors.length - 1)];
        }
    }

    /**
     * 简单轮询的方式进行EventLoop的负载均衡。
     */
    @ThreadSafe
    private static final class RoundRobinEventLoopChooser implements EventLoopChooser {

        private final AtomicInteger idx = new AtomicInteger();
        private final EventLoop[] executors;

        RoundRobinEventLoopChooser(EventLoop[] executors) {
            assert executors.length > 0;
            this.executors = executors;
        }

        @Nonnull
        @Override
        public EventLoop select() {
            int key = idx.getAndIncrement();
            return executors[Math.abs(key % executors.length)];
        }

        @Override
        public EventLoop select(int key) {
            return executors[Math.abs(key % executors.length)];
        }
    }

}