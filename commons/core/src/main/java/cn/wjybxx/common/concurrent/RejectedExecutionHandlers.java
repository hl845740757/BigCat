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

import java.util.concurrent.RejectedExecutionException;

/**
 * @author wjybxx
 * date 2023/4/11
 */
public class RejectedExecutionHandlers {

    private static final Logger logger = LoggerFactory.getLogger(RejectedExecutionHandlers.class);

    private static final RejectedExecutionHandler ABORT_POLICY = (r, eventLoop) -> {
        throw new RejectedExecutionException();
    };

    private static final RejectedExecutionHandler CALLER_RUNS_POLICY = (r, eventLoop) -> {
        if (!eventLoop.isShuttingDown()) {
            r.run();
        }
    };

    private static final RejectedExecutionHandler DISCARD_POLICY = (r, eventLoop) -> {
    };

    private static final RejectedExecutionHandler RECORD_POLICY = (r, eventLoop) -> {
        logger.info("task {} is reject by {}.", r.getClass().getCanonicalName(), eventLoop.getClass().getCanonicalName());
    };

    private RejectedExecutionHandlers() {

    }

    /**
     * 抛出拒绝异常
     */
    public static RejectedExecutionHandler abort() {
        return ABORT_POLICY;
    }

    /**
     * 调用者执行策略：调用execute方法的线程执行。
     * 注意：
     * 1. 必须有序执行的任务不能使用该策略。
     * 2. 如果资源只能特定线程能访问，也不能使用该策略。
     */
    public static RejectedExecutionHandler callerRuns() {
        return CALLER_RUNS_POLICY;
    }

    /**
     * 丢弃任务
     */
    public static RejectedExecutionHandler discard() {
        return DISCARD_POLICY;
    }

    /**
     * 仅仅是记录一条错误日志
     */
    public static RejectedExecutionHandler record() {
        return RECORD_POLICY;
    }
}