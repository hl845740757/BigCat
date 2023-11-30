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

import cn.wjybxx.common.FunctionUtils;
import cn.wjybxx.common.ThreadUtils;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

/**
 * @author wjybxx
 * date 2023/4/11
 */
public class FutureBlockingTest {

    private EventLoopGroup consumer;

    @BeforeEach
    void setUp() {
        consumer = EventLoopGroupBuilder.newBuilder()
                .setNumberChildren(1)
                .setEventLoopFactory((parent, index) -> EventLoopBuilder.newDisruptBuilder()
                        .setParent(parent)
                        .setThreadFactory(new DefaultThreadFactory("consumer"))
                        .setAgent(new Agent())
                        .build())
                .build();
    }

    @Test
    void timedWait() throws InterruptedException {
        consumer.execute(FunctionUtils.emptyRunnable()); // 唤醒线程

        ThreadUtils.sleepQuietly(1000);
        consumer.shutdown();
        consumer.terminationFuture().join();
    }

    private static class Agent implements EventLoopAgent {

        EventLoop eventLoop;

        @Override
        public void inject(EventLoop eventLoop) {
            this.eventLoop = eventLoop;
        }

        @Override
        public void onStart() throws Exception {
            Assertions.assertThrowsExactly(BlockingOperationException.class, () -> {
                eventLoop.newPromise().join();
            });
        }

        @Override
        public void onEvent(RingBufferEvent event) throws Exception {

        }

        @Override
        public void update() throws Exception {

        }

        @Override
        public void onShutdown() throws Exception {

        }
    }


}