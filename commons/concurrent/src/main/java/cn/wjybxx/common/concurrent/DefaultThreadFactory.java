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

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import javax.annotation.Nonnull;
import java.util.concurrent.ThreadFactory;
import java.util.concurrent.atomic.AtomicInteger;

/**
 * 借鉴于Netty的DefaultThreadFactory 和JDK的Executors中的DefaultThreadFactory实现
 *
 * @author wjybxx
 * date 2023/3/31
 */
public class DefaultThreadFactory implements ThreadFactory {

    private static final Logger logger = LoggerFactory.getLogger(DefaultThreadFactory.class);
    /** 未指定的优先级 */
    private static final int UNASSIGNED_PRIORITY = -1;
    /** 线程池id，避免name相同时的冲突 */
    private static final AtomicInteger poolId = new AtomicInteger();

    /** 该线程池内，下一个线程的id */
    private final AtomicInteger nextId = new AtomicInteger();
    /** 线程命名前缀：poolId -  poolName */
    private final String prefix;
    /** 是否是守护线程 */
    private final boolean daemon;
    /** 线程优先级 */
    private final int priority;

    public DefaultThreadFactory(String poolName) {
        this(poolName, false);
    }

    public DefaultThreadFactory(String poolName, boolean daemon) {
        this(poolName, daemon, UNASSIGNED_PRIORITY);
    }

    public DefaultThreadFactory(String poolName, boolean daemon, int priority) {
        if (poolName == null) {
            throw new NullPointerException("poolName");
        }
        if (priority != UNASSIGNED_PRIORITY) {
            if (priority < Thread.MIN_PRIORITY || priority > Thread.MAX_PRIORITY) {
                throw new IllegalArgumentException(
                        "priority: " + priority + " (expected: Thread.MIN_PRIORITY <= priority <= Thread.MAX_PRIORITY)");
            }
        }
        // Worker-1:MyThread-1
        prefix = "Pool-" + poolId.incrementAndGet() + ":" + poolName + "-";
        this.daemon = daemon;
        this.priority = priority;
    }

    @Override
    public Thread newThread(@Nonnull Runnable r) {
        Thread t = new Thread(r, prefix + nextId.getAndIncrement());
        try {
            if (t.isDaemon() != daemon) {
                t.setDaemon(daemon);
            }

            if (priority != UNASSIGNED_PRIORITY && t.getPriority() != priority) {
                t.setPriority(priority);
            }
        } catch (Exception ignored) {
            // Doesn't matter even if failed to set.
        }

        // 记录异常
        if (t.getUncaughtExceptionHandler() == null) {
            t.setUncaughtExceptionHandler(DefaultUncaughtExceptionHandler.INSTANCE);
        }

        return t;
    }

    public static void checkUncaughtExceptionHandler(Thread thread) {
        if (thread.getUncaughtExceptionHandler() == null) {
            thread.setUncaughtExceptionHandler(DefaultUncaughtExceptionHandler.INSTANCE);
        }
    }

    private static class DefaultUncaughtExceptionHandler implements Thread.UncaughtExceptionHandler {

        private static final DefaultUncaughtExceptionHandler INSTANCE = new DefaultUncaughtExceptionHandler();

        @Override
        public void uncaughtException(Thread t, Throwable e) {
            logger.warn("thread exited due to uncaughtException, threadName {}", t.getName(), e);
        }
    }

    @Override
    public String toString() {
        return "DefaultThreadFactory{" +
                "prefix='" + prefix + '\'' +
                ", daemon=" + daemon +
                ", priority=" + priority +
                '}';
    }
}