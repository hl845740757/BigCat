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

import java.util.Objects;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.CompletionStage;
import java.util.concurrent.atomic.AtomicInteger;
import java.util.function.BiConsumer;
import java.util.function.Supplier;

/**
 * @author wjybxx
 * date 2023/4/12
 */
class DefaultFutureCombiner implements FutureCombiner {

    private Supplier<XCompletableFuture<Object>> factory;
    private ChildListener childrenListener = new ChildListener();
    private int futureCount;

    DefaultFutureCombiner() {
    }

    DefaultFutureCombiner(Supplier<XCompletableFuture<Object>> factory) {
        this.factory = factory;
    }

    //region
    @Override
    public FutureCombiner add(CompletionStage<?> future) {
        Objects.requireNonNull(future);
        ChildListener childrenListener = this.childrenListener;
        if (childrenListener == null) {
            throw new IllegalStateException("Adding futures is not allowed after finished adding");
        }
        ++futureCount;
        future.whenComplete(childrenListener);
        return this;
    }

    @Override
    public int futureCount() {
        return futureCount;
    }

    @Override
    public void clear() {
        futureCount = 0;
        childrenListener = new ChildListener();
    }

    // endregion

    // region

    @Override
    public XCompletableFuture<Object> anyOf() {
        return finish(AggregateOptions.anyOf());
    }

    @Override
    public XCompletableFuture<Object> selectN(int successRequire, boolean lazy) {
        return finish(AggregateOptions.selectN(successRequire, lazy));
    }

    private XCompletableFuture<Object> finish(AggregateOptions options) {
        Objects.requireNonNull(options);
        ChildListener childrenListener = this.childrenListener;
        if (childrenListener == null) {
            throw new IllegalStateException("Already finished");
        }
        this.childrenListener = null;

        // 数据存储在ChildListener上有助于扩展
        XCompletableFuture<Object> aggregatePromise = factory == null ? new XCompletableFuture<>() : factory.get();
        childrenListener.futureCount = this.futureCount;
        childrenListener.options = options;
        childrenListener.aggregatePromise = aggregatePromise;
        childrenListener.checkComplete();
        return aggregatePromise;
    }

    // region 内部实现

    private static class ChildListener implements BiConsumer<Object, Throwable> {

        private final AtomicInteger succeedCount = new AtomicInteger();
        private final AtomicInteger doneCount = new AtomicInteger();

        /** 非volatile，虽然存在竞争，但重复赋值是安全的，通过promise发布到其它线程 */
        private Object result;
        private Throwable cause;

        /** 非volatile，其可见性由{@link #aggregatePromise}保证 */
        private int futureCount;
        private AggregateOptions options;
        private volatile CompletableFuture<Object> aggregatePromise;

        @Override
        public void accept(Object r, Throwable throwable) {
            // 我们先增加succeedCount，再增加doneCount，读取时先读取doneCount，再读取succeedCount，
            // 就可以保证succeedCount是比doneCount更新的值，才可以提前判断是否立即失败
            if (throwable == null) {
                result = encodeValue(r);
                succeedCount.incrementAndGet();
            } else {
                cause = throwable;
            }
            doneCount.incrementAndGet();

            CompletableFuture<Object> aggregatePromise = this.aggregatePromise;
            if (aggregatePromise != null && !aggregatePromise.isDone() && checkComplete()) {
                result = null;
                cause = null;
            }
        }

        boolean checkComplete() {
            // 字段的读取顺序不可以调整
            final int doneCount = this.doneCount.get();
            final int succeedCount = this.succeedCount.get();
            if (doneCount < succeedCount) { // 退出竞争，另一个线程来完成
                return false;
            }

            // 没有任务，立即完成
            if (futureCount == 0) {
                return aggregatePromise.complete(null);
            }
            if (options.isAnyOf()) {
                if (doneCount == 0) {
                    return false;
                }
                if (result != null) { // anyOf下尽量返回成功
                    return aggregatePromise.complete(decodeValue(result));
                } else {
                    return aggregatePromise.completeExceptionally(cause);
                }
            }

            // 懒模式需要等待所有任务完成
            if (options.lazy && doneCount < futureCount) {
                return false;
            }
            // 包含了require小于等于0的情况
            final int successRequire = options.successRequire;
            if (succeedCount >= successRequire) {
                return aggregatePromise.complete(null);
            }
            // 剩余的任务不足以达到成功，则立即失败；包含了require大于futureCount的情况
            if (succeedCount + (futureCount - doneCount) < successRequire) {
                if (cause == null) {
                    cause = TaskInsufficientException.create(futureCount, doneCount, succeedCount, successRequire);
                }
                return aggregatePromise.completeExceptionally(cause);
            }
            return false;
        }
    }

    private static final Object NIL = new Object();

    private static Object encodeValue(Object val) {
        return val == null ? NIL : val;
    }

    private static Object decodeValue(Object r) {
        return r == NIL ? null : r;
    }

    // endregion
}