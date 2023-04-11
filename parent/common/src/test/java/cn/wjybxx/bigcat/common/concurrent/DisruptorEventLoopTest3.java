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

/**
 * 测试单生产者使用{@link DisruptorEventLoop#publish(long)}的时序
 *
 * @author wjybxx
 * date 2023/4/11
 */
public class DisruptorEventLoopTest3 {

    private DisruptorEventLoop consumer;
    private Producer producer;
    private volatile boolean alert;

    @BeforeEach
    void setUp() {
        consumer = EventLoopBuilder.newDisruptBuilder()
                .setThreadFactory(new DefaultThreadFactory("consumer"))
                .setAgent(new Agent())
                .build();

        producer = new Producer();
        producer.start();
    }

    @Test
    void timedWait() throws InterruptedException {
        ThreadUtils.sleepQuietly(5000);
        alert = true;
        producer.join();

        consumer.shutdown();
        consumer.terminationFuture().join();

        Agent agent = (Agent) consumer.getAgent();
        Assertions.assertTrue(agent.nextSequence > 0, "agent.nextSequence == " + agent.nextSequence);
        Assertions.assertTrue(agent.errorMsgList.isEmpty(), agent.errorMsgList::toString);
    }

    private class Producer extends Thread {

        public Producer() {
            super("Producer");
        }

        @Override
        public void run() {
            DisruptorEventLoop consumer = DisruptorEventLoopTest3.this.consumer;
            long sequence = -1;
            while (!alert && sequence < 1000000) {
                sequence = consumer.nextSequence();
                try {
                    RingBufferEvent event = consumer.getEvent(sequence);
                    event.type = 1;
                    event.longVal1 = sequence;
                } finally {
                    consumer.publish(sequence);
                }
            }
        }
    }

    private static class Agent implements EventLoopAgent<RingBufferEvent> {

        private long nextSequence;
        private final List<String> errorMsgList = new ArrayList<>();

        @Override
        public void onStart(EventLoop eventLoop) throws Exception {
            nextSequence = 0;
        }

        @Override
        public void onEvent(RingBufferEvent event) throws Exception {
            if (event.type < 1) {
                String msg = String.format("code1 event.type: %d (expected: > 0)", event.type);
                errorMsgList.add(msg);
                return;
            }

            if (event.longVal1 != nextSequence) {
                String msg = String.format("code2, nextSequence: %d (expected: = %d)", event.longVal1, nextSequence);
                errorMsgList.add(msg);
            }
            nextSequence++;
        }

        @Override
        public void update() throws Exception {

        }

        @Override
        public void onShutdown() throws Exception {

        }
    }
}