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

/**
 * 测试多生产者使用{@link DisruptorEventLoop#execute(Runnable)}发布任务的时序
 *
 * @author wjybxx
 * date 2023/4/11
 */
public class DisruptorEventLoopTest2 {

    private static final int PRODUCER_COUNT = 4;

    private DisruptorEventLoop consumer;
    private List<Producer> producerList;
    private volatile boolean alert;

    @BeforeEach
    void setUp() {
        consumer = EventLoopBuilder.newDisruptBuilder()
                .setThreadFactory(new DefaultThreadFactory("consumer"))
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
        alert = true;

        consumer.terminationFuture().join();
        producerList.forEach(ThreadUtils::joinUninterruptedly);

        Assertions.assertTrue(Counter.sequenceMap.size() > 0, "Counter.sequenceMap.size is 0");
        List<String> errorMsgList = Counter.errorMsgList;
        Assertions.assertTrue(errorMsgList.isEmpty(), errorMsgList::toString);
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
            DisruptorEventLoop consumer = DisruptorEventLoopTest2.this.consumer;
            long localSequence = 0;
            while (!alert && localSequence < 1000000) {
                consumer.execute(new Counter(type, localSequence++));
            }
        }
    }

    private static final class Counter implements Runnable {

        private static final Int2LongMap sequenceMap = new Int2LongArrayMap();
        private static final List<String> errorMsgList = new ArrayList<>();

        final int type;
        final long sequence;

        private Counter(int type, long sequence) {
            this.type = type;
            this.sequence = sequence;
        }

        /** 运行在消费者线程下，数据私有 */
        @Override
        public void run() {
            long nextSequence = sequenceMap.get(type);
            if (sequence != nextSequence) {
                errorMsgList.add(String.format("code2, event.type: %d, nextSequence: %d (expected: = %d)",
                        type, sequence, nextSequence));
            }
            sequenceMap.put(type, nextSequence + 1);
        }
    }

}