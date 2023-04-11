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

import cn.wjybxx.bigcat.common.ThreadUtils;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

import java.util.ArrayList;
import java.util.List;
import java.util.concurrent.ThreadLocalRandom;
import java.util.concurrent.TimeUnit;

/**
 * @author wjybxx
 * date 2023/4/11
 */
public class ScheduleTest {

    private DisruptorEventLoop consumer;

    @BeforeEach
    void setUp() {
        consumer = EventLoopBuilder.newDisruptBuilder()
                .setThreadFactory(new DefaultThreadFactory("consumer"))
                .build();
    }

    @Test
    void timedWait() {
        final ThreadLocalRandom random = ThreadLocalRandom.current();
        final TimeUnit milliseconds = TimeUnit.MILLISECONDS;
        for (int i = 0; i < 100; i++) {
            switch (random.nextInt(3)) {
                case 1 -> consumer.scheduleWithFixedDelay(new Counter(i), 100, 200, milliseconds);
                case 2 -> consumer.scheduleAtFixedRate(new Counter(i), 100, 200, milliseconds);
                default -> consumer.schedule(new Counter(i), 100, milliseconds);
            }
        }

        ThreadUtils.sleepQuietly(3000);
        consumer.shutdown();
        consumer.terminationFuture().join();

        Assertions.assertTrue(Counter.nextSequence > 0, "Counter.nextSequence is 0");
        List<String> errorMsgList = Counter.errorMsgList;
        Assertions.assertTrue(errorMsgList.isEmpty(), errorMsgList::toString);
    }

    private static final class Counter implements Runnable {

        private static long nextSequence = 0;
        private static final List<String> errorMsgList = new ArrayList<>();

        final long sequence;
        private boolean first = true;

        private Counter(long sequence) {
            this.sequence = sequence;
        }

        /** 运行在消费者线程下，数据私有 */
        @Override
        public void run() {
            if (first) {
                if (sequence != nextSequence) {
                    String msg = String.format("nextSequence: %d (expected: = %d)", sequence, nextSequence);
                    errorMsgList.add(msg);
                }
                nextSequence++;
                first = false;
            }
        }
    }

}