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

package cn.wjybxx.common.concurrent.ext;

import com.lmax.disruptor.*;

import java.util.concurrent.TimeUnit;
import java.util.concurrent.locks.LockSupport;

/**
 * 先尝试一定次数自旋，在自旋失败后进行多次短时间的sleep
 * sleep次数用尽后抛出超时异常，退出等待
 *
 * @author wjybxx
 * date 2023/4/11
 */
public class TimeoutSleepingWaitStrategy implements WaitStrategy {

    private static final int DEFAULT_RETRIES = 200;
    private static final int DEFAULT_SLEEP_RETRIES = 10;
    private static final long DEFAULT_SLEEP_NANOS = 100_000; // 默认总睡眠1毫秒

    private final int retries;
    private final int sleepRetries;
    private final long sleepTimeNs;

    public TimeoutSleepingWaitStrategy() {
        this(DEFAULT_RETRIES, DEFAULT_SLEEP_RETRIES, DEFAULT_SLEEP_NANOS);
    }

    public TimeoutSleepingWaitStrategy(int retries) {
        this(retries, DEFAULT_SLEEP_RETRIES, DEFAULT_SLEEP_NANOS);
    }

    private TimeoutSleepingWaitStrategy(int retries, int sleepRetries) {
        this(retries, sleepRetries, DEFAULT_SLEEP_NANOS);
    }

    /**
     * @param retries      自旋次数
     * @param sleepRetries sleep次数
     * @param time         每次sleep的时间
     * @param timeUnit     sleep的时间单位
     */
    public TimeoutSleepingWaitStrategy(int retries, int sleepRetries, long time, TimeUnit timeUnit) {
        this(retries, sleepRetries, timeUnit.toNanos(time));
    }

    private TimeoutSleepingWaitStrategy(int retries, int sleepRetries, long sleepTimeNs) {
        this.retries = retries;
        this.sleepRetries = sleepRetries;
        this.sleepTimeNs = sleepTimeNs;
    }

    @Override
    public long waitFor(
            final long sequence, Sequence cursor, final Sequence dependentSequence, final SequenceBarrier barrier)
            throws AlertException, TimeoutException {

        int sleepRetries = this.sleepRetries;
        int counter = retries + sleepRetries;
        int yieldThreshold = sleepRetries + retries / 2; // 前半程自旋空转，后半程字段自旋尝试让出CPU

        long availableSequence;
        while ((availableSequence = dependentSequence.get()) < sequence) {
            barrier.checkAlert();

            if (counter > yieldThreshold) {
                --counter;
            } else if (counter > sleepRetries) {
                --counter;
                Thread.yield();
            } else if (counter > 0) {
                --counter;
                LockSupport.parkNanos(sleepTimeNs);
            } else {
                throw TimeoutException.INSTANCE;
            }
        }
        return availableSequence;
    }

    @Override
    public void signalAllWhenBlocking() {
    }

}