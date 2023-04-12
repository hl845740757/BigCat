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

import cn.wjybxx.bigcat.common.FunctionUtils;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

import java.util.concurrent.ThreadLocalRandom;
import java.util.concurrent.TimeUnit;

/**
 * @author wjybxx
 * date 2023/4/12
 */
public class FutureCombinerTest {

    private EventLoopGroup consumer;

    @BeforeEach
    void setUp() {
        consumer = EventLoopGroupBuilder.newBuilder()
                .setNumberChildren(4)
                .setEventLoopFactory((parent, index) -> EventLoopBuilder.newDisruptBuilder()
                        .setThreadFactory(new DefaultThreadFactory("consumer"))
                        .build())
                .build();
    }

    @Test
    void timedWait() {
        final ThreadLocalRandom random = ThreadLocalRandom.current();
        final TimeUnit milliseconds = TimeUnit.MILLISECONDS;

        final FutureCombiner combiner = FutureUtils.newCombiner();
        final int taskCount = 800;
        for (int i = 0; i < taskCount; i++) {
            long delay = random.nextLong(0, 2000);
            IScheduledFuture<?> future = consumer.schedule(FunctionUtils.emptyRunnable(), delay, milliseconds);
            combiner.add(future);
        }

        combiner.selectAll()
                .join();

        consumer.shutdown();
        consumer.terminationFuture().join();
    }
}