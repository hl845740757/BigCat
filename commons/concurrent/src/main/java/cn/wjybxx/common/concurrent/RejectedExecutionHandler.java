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

import java.util.concurrent.RejectedExecutionException;

/**
 * 当提交的任务被拒绝时的处理策略，修改自JDK的或者说Netty的拒绝策略。
 *
 * @author wjybxx
 * date 2023/4/7
 */
public interface RejectedExecutionHandler {

    /**
     * Method that may be invoked by a {@link EventLoop} when
     * {@link EventLoop#execute execute} cannot accept a
     * task.  This may occur when no more threads or queue slots are
     * available because their bounds would be exceeded, or upon
     * shutdown of the Executor.
     *
     * <p>In the absence of other alternatives, the method may throw
     * an unchecked {@link RejectedExecutionException}, which will be
     * propagated to the caller of {@code execute}.
     *
     * @param r         the runnable task requested to be executed
     * @param eventLoop the executor attempting to execute this task
     * @throws RejectedExecutionException if there is no remedy
     */
    void rejected(Runnable r, EventLoop eventLoop);
}
