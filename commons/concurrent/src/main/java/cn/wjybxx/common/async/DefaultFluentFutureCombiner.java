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

import cn.wjybxx.common.concurrent.AggregateOptions;
import cn.wjybxx.common.concurrent.TaskInsufficientException;

import java.util.Objects;
import java.util.function.BiConsumer;

/**
 * @author wjybxx
 * date 2023/4/3
 */
class DefaultFluentFutureCombiner implements FluentFutureCombiner {

    private ChildListener childrenListener = new ChildListener();
    private int futureCount;

    DefaultFluentFutureCombiner() {
    }

    //region
    @Override
    public FluentFutureCombiner add(FluentFuture<?> future) {
        Objects.requireNonNull(future);
        ChildListener childrenListener = this.childrenListener;
        if (childrenListener == null) {
            throw new IllegalStateException("Adding futures is not allowed after finished adding");
        }
        ++futureCount;
        future.addListener(childrenListener);
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
    public FluentPromise<Object> anyOf() {
        return finish(AggregateOptions.anyOf());
    }

    @Override
    public FluentPromise<Object> selectN(int successRequire, boolean lazy) {
        return finish(AggregateOptions.selectN(successRequire, lazy));
    }

    private FluentPromise<Object> finish(AggregateOptions options) {
        Objects.requireNonNull(options);
        ChildListener childrenListener = this.childrenListener;
        if (childrenListener == null) {
            throw new IllegalStateException("Already finished");
        }
        this.childrenListener = null;

        FluentPromise<Object> aggregatePromise = SameThreads.newPromise();
        childrenListener.futureCount = futureCount;
        childrenListener.options = options;
        childrenListener.aggregatePromise = aggregatePromise;
        childrenListener.checkComplete();
        return aggregatePromise;
    }

    // endregion

    // region 内部实现

    private static class ChildListener implements BiConsumer<Object, Throwable> {

        private int succeedCount;
        private int doneCount;

        private Object result;
        private Throwable cause;

        private int futureCount;
        private AggregateOptions options;
        private FluentPromise<Object> aggregatePromise;

        @Override
        public void accept(Object r, Throwable throwable) {
            if (throwable == null) {
                result = encodeValue(r);
                succeedCount++;
            } else if (cause == null) { // 暂时保留第一个异常
                cause = throwable;
            }
            doneCount++;

            FluentPromise<Object> aggregatePromise = this.aggregatePromise;
            if (aggregatePromise != null && !aggregatePromise.isDone() && checkComplete()) {
                result = null;
                cause = null;
            }
        }

        boolean checkComplete() {
            int doneCount = this.doneCount;
            int succeedCount = this.succeedCount;

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