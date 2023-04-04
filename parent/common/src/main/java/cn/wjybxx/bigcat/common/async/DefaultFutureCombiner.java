/*
 * Copyright 2023 wjybxx
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

package cn.wjybxx.bigcat.common.async;

import java.util.Objects;
import java.util.function.BiConsumer;

/**
 * @author wjybxx
 * date 2023/4/3
 */
public class DefaultFutureCombiner implements FutureCombiner {

    private final ChildListener childrenListener = new ChildListener();
    private AggregateOptions options;

    private int futureCount;
    private int succeedCount;
    private int doneCount;

    private FluentPromise<Object> aggregatePromise;
    /** 成功结果，只有Any模式下记录 */
    private Object result;
    /** 异常信息，所有模式下都记录，但只记录一个 */
    private Throwable cause;

    DefaultFutureCombiner() {
    }

    //region
    @Override
    public FutureCombiner add(FluentFuture<?> future) {
        Objects.requireNonNull(future);
        checkAddFutureAllowed();
        ++futureCount;
        future.addListener(childrenListener);
        return this;
    }

    private void checkAddFutureAllowed() {
        if (aggregatePromise != null) {
            throw new IllegalStateException("Adding futures is not allowed after finished adding");
        }
    }

    @Override
    public int futureCount() {
        return futureCount;
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
        checkFinishAllowed();
        this.options = options;
        this.aggregatePromise = FutureUtils.newPromise();
        childrenListener.checkComplete();
        return aggregatePromise;
    }

    private void checkFinishAllowed() {
        if (null != this.aggregatePromise) {
            throw new IllegalStateException("Already finished");
        }
    }
    // endregion

    // region 内部实现

    private class ChildListener implements BiConsumer<Object, Throwable> {

        @Override
        public void accept(Object r, Throwable throwable) {
            doneCount++;
            if (throwable == null) {
                succeedCount++;
                if (options.mode == Mode.ANY) {
                    result = r;
                }
            } else if (cause == null) {
                // 暂时保留第一个异常
                cause = throwable;
            }

            if (aggregatePromise != null) {
                checkComplete();
            }
        }

        void checkComplete() {
            // 没有任务，立即完成
            if (futureCount == 0) {
                aggregatePromise.trySuccess(result);
                return;
            }

            if (options.mode == Mode.ANY) {
                if (doneCount == 0) {
                    return;
                }
                if (cause == null) {
                    aggregatePromise.trySuccess(result);
                } else {
                    aggregatePromise.tryFailure(cause);
                }
                return;
            }

            // 懒模式需要等待所有任务完成
            if (options.lazy && doneCount < futureCount) {
                return;
            }
            // 包含了require小于等于0的情况
            final int successRequire = options.successRequire;
            if (succeedCount >= successRequire) {
                aggregatePromise.trySuccess(result);
                return;
            }
            // 剩余的任务不足以达到成功，则立即失败；包含了require大于futureCount的情况
            if (succeedCount + (futureCount - doneCount) < successRequire) {
                if (cause == null) {
                    cause = TaskInsufficientException.create(futureCount, successRequire);
                }
                aggregatePromise.tryFailure(cause);
            }
        }
    }

    private enum Mode {
        ANY,
        SELECT,
    }

    private static class AggregateOptions {

        public final Mode mode;
        public final int successRequire;
        public final boolean lazy;

        AggregateOptions(Mode mode, int successRequire, boolean lazy) {
            this.mode = mode;
            this.successRequire = successRequire;
            this.lazy = lazy;
        }

        private static final AggregateOptions ANY = new AggregateOptions(Mode.ANY, 0, false);

        public static AggregateOptions anyOf() {
            return ANY;
        }

        public static AggregateOptions selectN(int successRequire, boolean lazy) {
            if (successRequire < 0) {
                throw new IllegalArgumentException();
            }
            return new AggregateOptions(Mode.SELECT, successRequire, lazy);
        }
    }

    private static class TaskInsufficientException extends RuntimeException implements NoLogRequiredException {

        public TaskInsufficientException() {
        }

        public TaskInsufficientException(String message) {
            super(message);
        }

        public final Throwable fillInStackTrace() {
            return this;
        }

        public static TaskInsufficientException create(int futureCount, int require) {
            final String msg = String.format("futureCount :%d, successRequire :%d", futureCount, require);
            return new TaskInsufficientException(msg);
        }

    }

    // endregion
}