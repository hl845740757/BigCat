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

import cn.wjybxx.common.concurrent.StacklessCancellationException;

import javax.annotation.Nonnull;
import java.util.ArrayDeque;
import java.util.Deque;
import java.util.Objects;
import java.util.concurrent.Callable;

/**
 * @author wjybxx
 * date 2023/4/3
 */
abstract class AbstractSameThreadExecutor implements SameThreadExecutor {

    final Deque<Runnable> taskQueue = new ArrayDeque<>();
    boolean shutdown;

    @Override
    public void execute(@Nonnull Runnable command) {
        Objects.requireNonNull(command);
        if (shutdown) {
            return;
        }
        taskQueue.add(command);
    }

    @Override
    public FluentFuture<?> submitRun(@Nonnull Runnable command) {
        Objects.requireNonNull(command);
        if (shutdown) { // 默认直接取消，暂不添加拒绝处理器
            return SameThreads.newFailedFuture(StacklessCancellationException.INSTANCE);
        }
        return SameThreads.runAsync(this, command);
    }

    @Override
    public <V> FluentFuture<V> submitCall(@Nonnull Callable<V> command) {
        Objects.requireNonNull(command);
        if (shutdown) {
            return SameThreads.newFailedFuture(StacklessCancellationException.INSTANCE);
        }
        return SameThreads.callAsync(this, command);
    }

    @Override
    public boolean isShutdown() {
        return shutdown;
    }

    @Override
    public void shutdown() {
        taskQueue.clear();
        shutdown = true;
    }

}