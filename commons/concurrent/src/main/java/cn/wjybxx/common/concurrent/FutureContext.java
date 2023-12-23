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
import java.util.concurrent.TimeUnit;

/**
 * future运行时的上下文，进行一些逻辑控制(比如：任务取消请求)
 * 如果链上的任务是可能在多线程运行的，Context的实现要小心线程安全问题
 *
 * @author wjybxx
 * date 2023/4/9
 */
public interface FutureContext {

    /**
     * 为新的下游任务分配一个context
     * 1.默认返回Null，比较安全
     * 2.如果期望返回自己或返回新的context，可以重写该实现 -- 返回自身时要小心线程安全问题。
     *
     * @param future         当前future
     * @param actionExecutor 下一个行为关联的executor，可能为null
     */
    default FutureContext downContext(XCompletableFuture<?> future, Executor actionExecutor) {
        return null;
    }

    /**
     * 检查死锁 -- 单线程下支持阻塞操作API，绕不开死锁检测
     * 在任务未完成的情况下，当用户调用以下阻塞方法时将检查死锁
     * {@link XCompletableFuture#get()}
     * {@link XCompletableFuture#get(long, TimeUnit)}
     * {@link XCompletableFuture#await(long, TimeUnit)}
     * {@link XCompletableFuture#awaitUninterruptedly(long, TimeUnit)}
     * {@link XCompletableFuture#join()}
     * <p>
     * 如果该方法返回false，当用户调用以上方法时，将抛出{@link BlockingOperationException}
     *
     * @return 如果可能死锁则返回true，否则返回false
     */
    default boolean checkDeadlock(XCompletableFuture<?> future) {
        return false;
    }

    /**
     * 创建了一个下游future
     * 1.该方法可用于追踪调用链的尾部，用于异常处理等逻辑
     * （以后重写Future的时候删除该接口）
     *
     * @param future     当前future
     * @param downFuture 新的future
     */
    default void reportFuture(XCompletableFuture<?> future, XCompletableFuture<?> downFuture) {

    }

    // region 用户写操作检查

    default boolean cancel(XCompletableFuture<?> future, boolean mayInterruptIfRunning) {
        if (future.ctx != this) throw new IllegalStateException();
        return future.internal_doCancel(mayInterruptIfRunning);
    }

    default <T> boolean complete(XCompletableFuture<T> future, T value) {
        if (future.ctx != this) throw new IllegalStateException();
        return future.internal_doComplete(value);
    }

    default boolean completeExceptionally(XCompletableFuture<?> future, Throwable ex) {
        if (future.ctx != this) throw new IllegalStateException();
        return future.internal_doCompleteExceptionally(ex);
    }

    default <T> void obtrudeValue(XCompletableFuture<T> future, T value) {
        if (future.ctx != this) throw new IllegalStateException();
        future.internal_doObtrudeValue(value);
    }

    default void obtrudeException(XCompletableFuture<?> future, Throwable ex) {
        if (future.ctx != this) throw new IllegalStateException();
        future.internal_doObtrudeException(ex);
    }

    // endregion
}