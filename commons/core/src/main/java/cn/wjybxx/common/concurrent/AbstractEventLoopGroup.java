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

import javax.annotation.Nonnull;
import java.util.Collection;
import java.util.List;
import java.util.concurrent.*;

/**
 * 默认的实现仅仅是简单的将任务分配给某个{@link EventLoop}执行
 *
 * @author wjybxx
 * date 2023/4/8
 */
public abstract class AbstractEventLoopGroup implements EventLoopGroup {

    @Override
    public void execute(@Nonnull Runnable command) {
        select().execute(command);
    }

    @Nonnull
    @Override
    public ICompletableFuture<?> submit(@Nonnull Runnable task) {
        return select().submit(task);
    }

    @Nonnull
    @Override
    public <T> ICompletableFuture<T> submit(@Nonnull Runnable task, T result) {
        return select().submit(task, result);
    }

    @Nonnull
    @Override
    public <T> ICompletableFuture<T> submit(@Nonnull Callable<T> task) {
        return select().submit(task);
    }

    @Override
    public IScheduledFuture<?> schedule(Runnable command, long delay, TimeUnit unit) {
        return select().schedule(command, delay, unit);
    }

    @Override
    public <V> IScheduledFuture<V> schedule(Callable<V> callable, long delay, TimeUnit unit) {
        return select().schedule(callable, delay, unit);
    }

    @Override
    public IScheduledFuture<?> scheduleWithFixedDelay(Runnable command, long initialDelay, long delay, TimeUnit unit) {
        return select().scheduleWithFixedDelay(command, initialDelay, delay, unit);
    }

    @Override
    public IScheduledFuture<?> scheduleAtFixedRate(Runnable command, long initialDelay, long period, TimeUnit unit) {
        return select().scheduleAtFixedRate(command, initialDelay, period, unit);
    }

    @Override
    public <V> IScheduledFuture<V> schedule(ScheduleBuilder<V> builder) {
        return select().schedule(builder);
    }

    // 以下API并不常用，因此不做优化

    @Nonnull
    @Override
    public <T> List<Future<T>> invokeAll(@Nonnull Collection<? extends Callable<T>> tasks)
            throws InterruptedException {
        return select().invokeAll(tasks);
    }

    @Nonnull
    @Override
    public <T> List<Future<T>> invokeAll(@Nonnull Collection<? extends Callable<T>> tasks, long timeout, @Nonnull TimeUnit unit)
            throws InterruptedException {
        return select().invokeAll(tasks, timeout, unit);
    }

    @Nonnull
    @Override
    public <T> T invokeAny(@Nonnull Collection<? extends Callable<T>> tasks)
            throws InterruptedException, ExecutionException {
        return select().invokeAny(tasks);
    }

    @Override
    public <T> T invokeAny(@Nonnull Collection<? extends Callable<T>> tasks, long timeout, @Nonnull TimeUnit unit)
            throws InterruptedException, ExecutionException, TimeoutException {
        return select().invokeAny(tasks, timeout, unit);
    }

}