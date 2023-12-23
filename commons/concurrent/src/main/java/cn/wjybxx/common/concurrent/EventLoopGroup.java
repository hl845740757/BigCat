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
import javax.annotation.concurrent.ThreadSafe;
import java.util.Collection;
import java.util.Iterator;
import java.util.List;
import java.util.concurrent.*;

/**
 * 事件循环线程组，它管理着一组{@link EventLoop}。
 * 它的本质是容器，它主要负责管理持有的EventLoop的生命周期。
 *
 * <h1>时序约定</h1>
 * 1.{@link EventLoopGroup}代表着一组线程，不对任务的执行时序提供任何保证，用户只能通过工具自行协调。<br>
 * 2.{@link #execute(Runnable)}{@link #submit(Runnable)}系列方法的时序等同于{@code schedule(task, 0, TimeUnit.SECONDS)}
 * <p>
 * Q: 为什么在接口层不提供严格的时序约定？<br>
 * A: 如果在接口层定义了严格的时序约定，实现类就会受到限制。
 * <p>
 * 1.时序很重要，在提供并发组件时应该详细的说明时序约定，否则用户将无所措手足。<br>
 * 2.EventLoopGroup也可以有自己的线程 - 一种常见的情况是Group是一个监控线程。
 *
 * @author wjybxx
 * date 2023/4/7
 */
@ThreadSafe
public interface EventLoopGroup extends ScheduledExecutorService, Iterable<EventLoop> {

    /**
     * 选择一个 {@link EventLoop}用于接下来的调度
     */
    @Nonnull
    EventLoop select();

    /** @implNote 实现时需要实现为不可变集合 */
    @Nonnull
    @Override
    Iterator<EventLoop> iterator();
    // ------------------------------ 生命周期相关方法 ----------------------------

    /**
     * 查询{@link EventLoopGroup}是否处于正在关闭状态。
     * 正在关闭状态下，拒绝接收新任务，当执行完所有任务后，进入关闭状态。
     *
     * @return 如果该{@link EventLoopGroup}管理的所有{@link EventLoop}正在关闭或已关闭则返回true
     */
    boolean isShuttingDown();

    /**
     * 查询{@link EventLoopGroup}是否处于关闭状态。
     * 关闭状态下，拒绝接收新任务，执行退出前的清理操作，执行完清理操作后，进入终止状态。
     *
     * @return 如果已关闭，则返回true
     */
    @Override
    boolean isShutdown();

    /**
     * 是否已进入终止状态，一旦进入终止状态，表示生命周期真正结束。
     *
     * @return 如果已处于终止状态，则返回true
     */
    @Override
    boolean isTerminated();

    /**
     * 等待EventLoopGroup进入终止状态
     * 等同于在{@link #terminationFuture()}进行阻塞操作。
     *
     * @param timeout 时间度量
     * @param unit    事件单位
     * @return 在方法返回前是否已进入终止状态
     * @throws InterruptedException 如果在等待期间线程被中断，则抛出该异常。
     */
    @Override
    boolean awaitTermination(long timeout, TimeUnit unit) throws InterruptedException;

    /**
     * 返回等待线程终止的future。
     * 返回的{@link CompletableFuture}会在该Group管理的所有{@link EventLoop}终止后收到通知.
     * <p>
     * （TODO Future是否可以返回所有被丢弃的任务，以修正shutdownNow？可能导致内存泄漏）
     */
    ICompletableFuture<?> terminationFuture();

    /**
     * 请求关闭 ExecutorService，不再接收新的任务。
     * ExecutorService在执行完现有任务后，进入关闭状态。
     * 如果 ExecutorService 正在关闭，或已经关闭，则方法不产生任何效果。
     * <p>
     * 该方法会立即返回，如果想等待 ExecutorService 进入终止状态，
     * 可以使用{@link #awaitTermination(long, TimeUnit)}或{@link #terminationFuture()} 进行等待
     */
    @Override
    void shutdown();

    /**
     * JDK文档：
     * 请求关闭 ExecutorService，<b>尝试取消所有正在执行的任务，停止所有待执行的任务，并不再接收新的任务。</b>
     * 如果 ExecutorService 已经关闭，则方法不产生任何效果。
     * <p>
     * 该方法会立即返回，如果想等待 ExecutorService 进入终止状态，可以使用{@link #awaitTermination(long, TimeUnit)}
     * 或{@link #terminationFuture()} 进行等待
     * <p>
     * 注意：不保证标准的实现，只保证尽快的关闭，基于以下原因：
     * <li>1. 可能无法安全的获取所有的任务(EventLoop架构属于多生产者单消费者模型，会尽量的避免其它线程消费数据)</li>
     * <li>2. 剩余任务数可能过多</li>
     *
     * @return may be empty。
     */
    @Nonnull
    @Override
    List<Runnable> shutdownNow();

    // region 基础任务调度

    @Override
    void execute(Runnable command);

    @Nonnull
    @Override
    ICompletableFuture<?> submit(Runnable task);

    @Nonnull
    @Override
    <V> ICompletableFuture<V> submit(Runnable task, V result);

    @Nonnull
    @Override
    <V> ICompletableFuture<V> submit(Callable<V> task);

    @Override
    IScheduledFuture<?> schedule(Runnable command, long delay, TimeUnit unit);

    @Override
    <V> IScheduledFuture<V> schedule(Callable<V> callable, long delay, TimeUnit unit);

    @Override
    IScheduledFuture<?> scheduleWithFixedDelay(Runnable command, long initialDelay, long delay, TimeUnit unit);

    /**
     * @throws IllegalArgumentException initialDelay < 0
     */
    @Override
    IScheduledFuture<?> scheduleAtFixedRate(Runnable command, long initialDelay, long period, TimeUnit unit);

    /** 通过builder执行更加灵活的任务 */
    <V> IScheduledFuture<V> schedule(ScheduleBuilder<V> builder);

    // endregion

    // region 不情愿的api
    // 个人觉得jdk将这几个api定义在接口里不好

    @Nonnull
    @Override
    <T> List<Future<T>> invokeAll(Collection<? extends Callable<T>> tasks) throws InterruptedException;

    @Nonnull
    @Override
    <T> List<Future<T>> invokeAll(Collection<? extends Callable<T>> tasks, long timeout, TimeUnit unit) throws InterruptedException;

    @Nonnull
    @Override
    <T> T invokeAny(Collection<? extends Callable<T>> tasks) throws InterruptedException, ExecutionException;

    @Override
    <T> T invokeAny(Collection<? extends Callable<T>> tasks, long timeout, TimeUnit unit) throws InterruptedException, ExecutionException, TimeoutException;

    // endregion
}