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

import java.util.concurrent.Executor;

/**
 * @author wjybxx
 * date 2023/4/9
 */
public class TerminateFutureContext extends EventLoopFutureContext {

    public TerminateFutureContext() {
        super();
    }

    public TerminateFutureContext(EventLoop eventLoop) {
        super(eventLoop);
    }

    public boolean terminate(XCompletableFuture<?> future) {
        return super.complete(future, null);
    }

    public boolean terminate(XCompletableFuture<?> future, Throwable ex) {
        if (ex == null) {
            return super.complete(future, null);
        } else {
            return super.completeExceptionally(future, ex);
        }
    }

    @Override
    public FutureContext downContext(XCompletableFuture<?> future, Executor actionExecutor) {
        // 下游变为正常context，允许用户使future进入完成状态
        return new EventLoopFutureContext(eventLoop);
    }

    @Override
    public boolean cancel(XCompletableFuture<?> future, boolean mayInterruptIfRunning) {
        throw new GuardedOperationException();
    }

    @Override
    public <T> boolean complete(XCompletableFuture<T> future, T value) {
        throw new GuardedOperationException();
    }

    @Override
    public boolean completeExceptionally(XCompletableFuture<?> future, Throwable ex) {
        throw new GuardedOperationException();
    }

    @Override
    public <T> void obtrudeValue(XCompletableFuture<T> future, T value) {
        throw new GuardedOperationException();
    }

    @Override
    public void obtrudeException(XCompletableFuture<?> future, Throwable ex) {
        throw new GuardedOperationException();
    }

}