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

import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

import java.util.List;
import java.util.StringJoiner;
import java.util.concurrent.TimeUnit;

/**
 * 测试{@link ScheduleBuilder}
 *
 * @author wjybxx
 * date 2023/4/11
 */
public class ScheduleTest2 {

    private final List<String> stringList = List.of("hello", "world", "a", "b", "c");

    private DisruptorEventLoop consumer;
    private StringJoiner joiner;
    private int index = 0;

    private String expectedString;

    @BeforeEach
    void setUp() {
        consumer = EventLoopBuilder.newDisruptBuilder()
                .setThreadFactory(new DefaultThreadFactory("consumer"))
                .build();

        joiner = new StringJoiner(",");

        StringJoiner tempJoiner = new StringJoiner(",");
        stringList.forEach(tempJoiner::add);
        expectedString = tempJoiner.toString();
    }

    @AfterEach
    void tearDown() {
        consumer.shutdown();
        consumer.terminationFuture().join();
    }

    ResultHolder<String> timeSharingJoinString() {
        joiner.add(stringList.get(index++));
        if (index >= stringList.size()) {
            return ResultHolder.succeeded(joiner.toString());
        }
        return null;
    }

    ResultHolder<String> untilJoinStringSuccess() {
        ResultHolder<String> holder;
        //noinspection StatementWithEmptyBody
        while ((holder = timeSharingJoinString()) == null) {
        }
        return holder;
    }

    @Test
    void testOnlyOnceFail() {
        IScheduledFuture<String> future = consumer.schedule(ScheduleBuilder.newTimeSharing(this::timeSharingJoinString)
                .setOnlyOnce(0));

        future.awaitUninterruptedly(300, TimeUnit.MILLISECONDS);
        Assertions.assertTrue(future.cause() instanceof TimeSharingTimeoutException);
    }

    @Test
    void testOnlyOnceSuccess() {
        String result = consumer.schedule(ScheduleBuilder.newTimeSharing(this::untilJoinStringSuccess)
                        .setOnlyOnce(0))
                .join();

        Assertions.assertEquals(expectedString, result);
    }

    @Test
    void testCallableSuccess() {
        String result = consumer.schedule(ScheduleBuilder.newCallable(() -> untilJoinStringSuccess().result)
                        .setOnlyOnce(0))
                .join();

        Assertions.assertEquals(expectedString, result);
    }

    //
    @Test
    void testTimeSharingComplete() {
        String result = consumer.schedule(ScheduleBuilder.newTimeSharing(this::timeSharingJoinString)
                        .setFixedDelay(0, 200))
                .join();

        Assertions.assertEquals(expectedString, result);
    }

    @Test
    void testTimeSharingTimeout() {
        IScheduledFuture<String> future = consumer.schedule(ScheduleBuilder.newTimeSharing(this::timeSharingJoinString)
                .setFixedDelay(0, 200)
                .setTimeoutByCount(1));

        future.awaitUninterruptedly(300, TimeUnit.MILLISECONDS);
        Assertions.assertTrue(future.cause() instanceof TimeSharingTimeoutException);
    }

    @Test
    void testRunnableTimeout() {
        IScheduledFuture<?> future = consumer.schedule(ScheduleBuilder.newRunnable(() -> {
                })
                .setFixedDelay(0, 200)
                .setTimeoutByCount(1));

        future.awaitUninterruptedly(300, TimeUnit.MILLISECONDS);
        Assertions.assertTrue(future.cause() instanceof TimeSharingTimeoutException);
    }

    @Test
    void testCallableTimeout() {
        IScheduledFuture<?> future = consumer.schedule(ScheduleBuilder.newCallable(() -> "hello world")
                .setFixedDelay(0, 200)
                .setTimeoutByCount(1));

        future.awaitUninterruptedly(300, TimeUnit.MILLISECONDS);
        Assertions.assertTrue(future.cause() instanceof TimeSharingTimeoutException);
    }

}