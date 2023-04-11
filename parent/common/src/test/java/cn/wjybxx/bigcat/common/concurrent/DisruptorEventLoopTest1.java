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
import java.util.concurrent.Executor;

/**
 * 测试正常使用{@link Executor#execute(Runnable)}提交任务的时序
 *
 * @author wjybxx
 * date 2023/4/11
 */
public class DisruptorEventLoopTest1 {

    private DisruptorEventLoop consumer;
    private Producer producer;
    private volatile boolean alert;

    @BeforeEach
    void setUp() {
        consumer = EventLoopBuilder.newDisruptBuilder()
                .setThreadFactory(new DefaultThreadFactory("consumer"))
                .build();

        producer = new Producer(1);
        producer.start();
    }

    @Test
    void timedWait() throws InterruptedException {
        ThreadUtils.sleepQuietly(5000);
        alert = true;
        producer.join();

        consumer.shutdown();
        consumer.terminationFuture().join();

        Assertions.assertTrue(Counter.nextSequence > 0, "Counter.nextSequence is 0");
        List<String> errorMsgList = Counter.errorMsgList;
        Assertions.assertTrue(errorMsgList.isEmpty(), errorMsgList::toString);
    }

    private class Producer extends Thread {

        final int type;

        public Producer(int type) {
            super("Producer-" + type);
            this.type = type;
        }

        @Override
        public void run() {
            DisruptorEventLoop consumer = DisruptorEventLoopTest1.this.consumer;
            long sequencer = 0;
            while (!alert && sequencer < 1000000) {
                consumer.execute(new Counter(sequencer++));
            }
        }
    }

    private static final class Counter implements Runnable {

        private static long nextSequence = 0;
        private static final List<String> errorMsgList = new ArrayList<>();

        final long sequence;

        private Counter(long sequence) {
            this.sequence = sequence;
        }

        /** 运行在消费者线程下，数据私有 */
        @Override
        public void run() {
            if (sequence != nextSequence) {
                String msg = String.format("nextSequence: %d (expected: = %d)", sequence, nextSequence);
                errorMsgList.add(msg);
            }
            nextSequence++;
        }
    }

}