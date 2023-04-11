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
import it.unimi.dsi.fastutil.ints.Int2LongArrayMap;
import it.unimi.dsi.fastutil.ints.Int2LongMap;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

import java.util.ArrayList;
import java.util.List;
import java.util.concurrent.RejectedExecutionException;

/**
 * 测试多生产者使用{@link DisruptorEventLoop#publish(long)}发布任务的时序
 *
 * @author wjybxx
 * date 2023/4/11
 */
public class DisruptorEventLoopTest4 {

    private static final int PRODUCER_COUNT = 4;

    private DisruptorEventLoop consumer;
    private List<Producer> producerList;
    private volatile boolean alert;

    @BeforeEach
    void setUp() {
        consumer = EventLoopBuilder.newDisruptBuilder()
                .setThreadFactory(new DefaultThreadFactory("consumer"))
                .setAgent(new Agent())
                .build();

        producerList = new ArrayList<>(PRODUCER_COUNT);
        for (int i = 1; i <= PRODUCER_COUNT; i++) {
            producerList.add(new Producer(i));
        }
        producerList.forEach(Thread::start);
    }

    @Test
    void timedWait() throws InterruptedException {
        ThreadUtils.sleepQuietly(5000);

        consumer.shutdown();
        consumer.terminationFuture().join();

        alert = true;
        producerList.forEach(ThreadUtils::joinUninterruptedly);

        Agent agent = (Agent) consumer.getAgent();
        Assertions.assertTrue(agent.sequenceMap.size() > 0, "agent.sequenceMap.size == 0");
        Assertions.assertTrue(agent.errorMsgList.isEmpty(), agent.errorMsgList::toString);
    }

    private class Producer extends Thread {

        private final int type;

        public Producer(int type) {
            super("Producer-" + type);
            this.type = type;
            if (type <= 0) { // 0是系统任务
                throw new IllegalArgumentException();
            }
        }

        @Override
        public void run() {
            DisruptorEventLoop consumer = DisruptorEventLoopTest4.this.consumer;
            long localSequence = 0;
            while (!alert && localSequence < 1000000) {
                try {
                    long sequence = consumer.nextSequence();
                    try {
                        RingBufferEvent event = consumer.getEvent(sequence);
                        event.type = type;
                        event.longVal1 = localSequence++;
                    } finally {
                        consumer.publish(sequence);
                    }
                } catch (RejectedExecutionException ignore) {
                    break;
                }
            }
        }
    }

    private static class Agent implements EventLoopAgent<RingBufferEvent> {

        private final Int2LongMap sequenceMap = new Int2LongArrayMap();
        private final List<String> errorMsgList = new ArrayList<>();

        @Override
        public void onStart(EventLoop eventLoop) throws Exception {
            sequenceMap.clear();
        }

        @Override
        public void onEvent(RingBufferEvent event) throws Exception {
            if (event.type < 1) {
                String msg = String.format("code1 event.type: %d (expected: > 0)", event.type);
                errorMsgList.add(msg);
                return;
            }

            long nextSequence = sequenceMap.get(event.type);
            if (event.longVal1 != nextSequence) {
                String msg = String.format("code2 event.type: %d, nextSequence: %d (expected: = %d)", event.type, event.longVal1, nextSequence);
                errorMsgList.add(msg);
            }
            sequenceMap.put(event.type, nextSequence + 1);
        }

        @Override
        public void update() throws Exception {

        }

        @Override
        public void onShutdown() throws Exception {

        }
    }
}