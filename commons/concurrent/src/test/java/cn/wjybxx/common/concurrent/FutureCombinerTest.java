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

import cn.wjybxx.common.ex.NoLogRequiredException;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

import java.util.concurrent.ThreadLocalRandom;
import java.util.concurrent.TimeUnit;

/**
 * 确保并发情况下不会由于竞争触发失败
 *
 * @author wjybxx
 * date 2023/4/12
 */
public class FutureCombinerTest {

    private EventLoopGroup consumer;

    @BeforeEach
    void setUp() {
        DefaultThreadFactory threadFactory = new DefaultThreadFactory("consumer");
        consumer = EventLoopGroupBuilder.newBuilder()
                .setNumberChildren(4)
                .setEventLoopFactory((parent, index) -> EventLoopBuilder.newDisruptBuilder()
                        .setParent(parent)
                        .setThreadFactory(threadFactory)
                        .build())
                .build();
    }

    @Test
    void timedWait() {
        final ThreadLocalRandom random = ThreadLocalRandom.current();
        final TimeUnit milliseconds = TimeUnit.MILLISECONDS;

        final FutureCombiner combiner = FutureUtils.newCombiner();
        final int taskCount = 20000;
        int succeedCount = 0;
        for (int i = 0; i < taskCount; i++) {
            long delay = random.nextLong(0, 50);
            IScheduledFuture<?> future;
            if (random.nextBoolean()) {
                succeedCount++;
                future = consumer.schedule(task_success, delay, milliseconds);
            } else {
                future = consumer.schedule(task_failure, delay, milliseconds);
            }
            combiner.add(future);
        }

        Assertions.assertNull(combiner.selectN(succeedCount, false)
                .join());

        consumer.shutdown();
        consumer.terminationFuture().join();
    }

    private static final Runnable task_success = () -> {
    };
    private static final Runnable task_failure = () -> {
        throw LightweightException.INSTANCE;
    };

    private static class LightweightException extends RuntimeException implements NoLogRequiredException {

        static final LightweightException INSTANCE = new LightweightException();

        public LightweightException() {
            super("", null, false, false);
        }
    }
}