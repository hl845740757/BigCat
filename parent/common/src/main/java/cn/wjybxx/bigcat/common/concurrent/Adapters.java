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

package cn.wjybxx.bigcat.common.concurrent;

import cn.wjybxx.bigcat.common.annotation.Internal;

import java.util.Objects;
import java.util.concurrent.Callable;

/**
 * @author wjybxx
 * date 2023/4/14
 */
@Internal
public final class Adapters {

    public static final Object CONTINUE = new Object();

    public static <V> Callable<V> toCallable(Runnable task, V result) {
        Objects.requireNonNull(task);
        return new RunnableAdapter<>(task, result);
    }

    /** 当任务未完成时返回{@link #CONTINUE}对象 */
    public static <V> Callable<V> toCallable(TimeSharingTask<V> task) {
        Objects.requireNonNull(task);
        return new TimeSharingAdapter<>(task);
    }

    public static boolean isTimeSharing(Callable<?> task) {
        return task.getClass() == TimeSharingAdapter.class;
    }

    public static Object unwrapTask(Object task) {
        if (task == null) {
            return null;
        }
        if (task instanceof RunnableAdapter<?> adapter) {
            return adapter.task;
        }
        if (task instanceof TimeSharingAdapter<?> adapter) {
            return adapter.task;
        }
        return task;
    }

    private static class RunnableAdapter<T> implements Callable<T> {

        final Runnable task;
        final T result;

        public RunnableAdapter(Runnable task, T result) {
            this.task = task;
            this.result = result;
        }

        @Override
        public T call() throws Exception {
            task.run();
            return result;
        }

        @Override
        public String toString() {
            return "Runnable2CallbackAdapter{" + "task=" + task + '}';
        }
    }

    private static class TimeSharingAdapter<V> implements Callable<V> {

        final TimeSharingTask<V> task;

        private TimeSharingAdapter(TimeSharingTask<V> task) {
            this.task = task;
        }

        @SuppressWarnings("unchecked")
        @Override
        public V call() throws Exception {
            ResultHolder<V> resultHolder = task.step();
            if (resultHolder == null) {
                return (V) CONTINUE;
            }
            return resultHolder.result;
        }
    }

}