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

import java.util.concurrent.*;

/**
 * 该接口主要用于我们的方法声明，避免直接声明{@link CompletableFuture}，使得未来没有修改的机会。
 * 未拆分为Future+Promise的结构了，避免接口太多
 *
 * @author wjybxx
 * date 2023/4/10
 */
public interface ICompletableFuture<T> extends Future<T>, CompletionStage<T> {

    /** {@inheritDoc} */
    @Override
    boolean isDone();

    /**
     * @return 如果future已进入完成状态，且是成功完成，则返回true。
     */
    boolean isSucceeded();

    /**
     * 如果future以任何形式的异常完成，则返回true。
     * (包括被取消)
     */
    boolean isFailed();

    /**
     * 获取关联的计算结果 -- 非阻塞。
     * 如果对应的计算失败，则抛出对应的异常。
     * 如果计算成功，则返回计算结果。
     * 如果计算尚未完成，则返回null。
     * <p>
     * 如果future关联的task没有返回值(操作完成返回null)，对于这种情况，你可以使用{@link #isSucceeded()}作为判断任务是否成功执行的更好选择。
     *
     * @throws CompletionException   计算失败
     * @throws CancellationException 被取消
     */
    default T getNow() {
        return getNow(null);
    }

    /**
     * 尝试获取计算结果 -- 非阻塞
     * 如果对应的计算失败，则抛出对应的异常。
     * 如果计算成功，则返回计算结果。
     * 如果计算尚未完成，则返回给定值。
     *
     * @throws CompletionException   计算失败
     * @throws CancellationException 被取消
     */
    T getNow(T valueIfAbsent);

    /**
     * 获取导致计算失败的原因。
     * 如果关联的任务已失败，则返回对应的失败原因，否则返回null。
     */
    Throwable cause();

    /** @return 如果任务在这期间进入了完成状态，则返回true */
    boolean await(long timeout, TimeUnit unit) throws InterruptedException;

    /** @return 如果任务在这期间进入了完成状态，则返回true */
    boolean awaitUninterruptedly(long timeout, TimeUnit unit);

    /**
     * 阻塞到任务完成
     *
     * @throws CompletionException   计算失败
     * @throws CancellationException 被取消
     */
    T join();

    //

    /**
     * 在一个链式调用中，取消常常达不到期望
     * 如果期望取消整个链上的任务，通常应该让你的任务共享一个上下文，通过取消上下文来取消
     */
    @Override
    boolean cancel(boolean mayInterruptIfRunning);

    /** @return 如果任务由未完成状态变为完成状态，则返回true */
    boolean complete(T value);

    /** @return 如果任务由未完成状态变为完成状态，则返回true */
    boolean completeExceptionally(Throwable ex);

    /**
     * 如果future绑定了一个运行上下文，则返回对应的上下文，否则返回null
     */
    FutureContext getCtx();

    /**
     * 转换为我们的future
     * 如果当前future的实现不是{@link CompletableFuture}的实力，需要创建一个新的future，并在进入完成状态时将结果传输到新的future上
     */
    @Override
    XCompletableFuture<T> toCompletableFuture();

}