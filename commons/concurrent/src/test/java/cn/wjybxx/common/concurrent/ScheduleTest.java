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

import cn.wjybxx.common.ThreadUtils;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

import java.util.concurrent.ThreadLocalRandom;
import java.util.concurrent.TimeUnit;

/**
 * 测试initDelay相同时，任务是否按照按提交顺序执行
 *
 * @author wjybxx
 * date 2023/4/11
 */
public class ScheduleTest {

    private Counter counter;
    private DisruptorEventLoop consumer;

    @BeforeEach
    void setUp() {
        counter = new Counter();
        consumer = EventLoopBuilder.newDisruptBuilder()
                .setThreadFactory(new DefaultThreadFactory("consumer"))
                .build();
    }

    @Test
    void timedWait() {
        final ThreadLocalRandom random = ThreadLocalRandom.current();
        final TimeUnit milliseconds = TimeUnit.MILLISECONDS;
        for (int i = 0; i < 100; i++) {
            Runnable newTask = counter.newTask(1, i);
            switch (random.nextInt(3)) {
                case 1 -> consumer.scheduleWithFixedDelay(newTask, 100, 200, milliseconds);
                case 2 -> consumer.scheduleAtFixedRate(newTask, 100, 200, milliseconds);
                default -> consumer.schedule(newTask, 100, milliseconds);
            }
        }

        ThreadUtils.sleepQuietly(3000);
        consumer.shutdown();
        consumer.terminationFuture().join();

        Assertions.assertTrue(counter.getSequenceMap().size() > 0, "Counter.sequenceMap.size == 0");
        Assertions.assertTrue(counter.getErrorMsgList().isEmpty(), counter.getErrorMsgList()::toString);
    }

}