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

package cn.wjybxx.common.async;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Deque;
import java.util.concurrent.TimeUnit;

/**
 * 默认的executor实现，通过限制每帧执行的任务数来平滑开销
 *
 * @author wjybxx
 * date 2023/4/3
 */
public class DefaultSameThreadExecutor extends AbstractSameThreadExecutor {

    private static final Logger logger = LoggerFactory.getLogger(DefaultSameThreadExecutor.class);

    private final int countLimit;
    private final long nanoTimeLimit;

    DefaultSameThreadExecutor() {
        this(-1, -1, TimeUnit.NANOSECONDS);
    }

    /**
     * @param countLimit 每帧允许运行的最大任务数，-1表示不限制；不可以为0
     * @param timeLimit  每帧允许的最大时间，-1表示不限制；不可以为0
     */
    DefaultSameThreadExecutor(int countLimit, long timeLimit, TimeUnit timeUnit) {
        ensureNegativeOneOrPositive(countLimit);
        ensureNegativeOneOrPositive(timeLimit);
        this.countLimit = countLimit;
        this.nanoTimeLimit = timeLimit > 0 ? timeUnit.toNanos(timeLimit) : -1;
    }

    private static void ensureNegativeOneOrPositive(long v) {
        if (!(v == -1 || v > 0)) {
            throw new IllegalArgumentException("v must be -1 or positive");
        }
    }

    @Override
    public boolean tick() {
        final int batchSize = this.countLimit;
        final long nanosPerFrame = this.nanoTimeLimit;
        final Deque<Runnable> taskQueue = this.taskQueue;

        // 频繁取系统时间的性能不好，因此分两个模式运行
        Runnable task;
        int count = 0;
        if (nanosPerFrame < 0) {
            while ((task = taskQueue.pollFirst()) != null) {
                try {
                    task.run();
                } catch (Exception e) {
                    logger.warn("task run caught exception", e);
                }

                if ((batchSize > 0 && ++count >= batchSize)) {
                    break; // 强制中断，避免占用太多资源或死循环风险
                }
            }
        } else {
            final long startTime = System.nanoTime();
            while ((task = taskQueue.pollFirst()) != null) {
                try {
                    task.run();
                } catch (Exception e) {
                    logger.warn("task run caught exception", e);
                }

                if ((batchSize > 0 && ++count >= batchSize)
                        || (System.nanoTime() - startTime >= nanosPerFrame)) {
                    break;  // 强制中断，避免占用太多资源或死循环风险
                }
            }
        }
        return taskQueue.size() > 0;
    }
}