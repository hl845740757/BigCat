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

package cn.wjybxx.common.concurrent;

import cn.wjybxx.common.time.TimeProvider;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import javax.annotation.concurrent.ThreadSafe;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.TimeUnit;

/**
 * 事件循环
 * 它是单线程的，它保证任务不会并发执行，且任务的执行顺序和提交顺序一致。
 *
 * <h1>时序</h1>
 * 在{@link EventLoopGroup}的基础上，我们提供这样的时序保证：<br>
 * 1.如果 task1 的执行时间小于等于 task2 的执行时间，且 task1 先提交成功，则保证 task1 在 task2 之前执行。<br>
 * 它可以表述为：不保证后提交的高优先级的任务能先执行。<br>
 * 还可以表述为：消费者按照提交成功顺序执行是合法的。<br>*
 * （简单说，提高优先级是不保证的，但反向的优化——降低优先级，则是可以支持的）
 * <p>
 * 2.周期性任务的再提交 与 新任务的提交 之间不提供时序保证。<br>
 * 它可以表述为：任务只有首次运行时是满足上面的时序的。<br>
 * 如果你期望再次运行和新任务之间得到确定性时序，可以通过提交一个新任务代替自己实现。<br>
 * （简单说，允许降低周期性任务的再执行优先级）
 * <p>
 * 3. schedule系列方法的{@code initialDelay}和{@code delay}为负时，将转换为0。
 * fixedRate除外，fixedRate期望的是逻辑时间，总逻辑时间应当是可以根据次数计算的，转0会导致错误，因此禁止负数输入。
 * 另外，fixedRate由于自身的特性，因此难以和非fixedRate类型的任务达成时序关系。
 *
 * <p>
 * Q:为什么首次触发延迟小于0时可以转为0？
 * A:我们在上面提到，由于不保证后提交的任务能在先提交的任务之前执行，因此当多个任务都能运行时，按照提交顺序执行是合法的。<br>
 * 因此，我们只要保证能按照提交顺序执行就是合法的，当所有的初始延迟都负转0时，所有后续提交的任务的优先级都小于等于当前任务，
 * 因此后续提交的任务必定在当前任务之后执行，也就是按照提交顺序执行，因此是合法的。
 *
 * <h3>警告</h3>
 * 由于{@link EventLoop}都是单线程的，你需要避免死锁等问题。<br>
 * 1. 如果两个{@link EventLoop}存在交互，且其中一个使用有界任务队列，则有可能导致死锁，或大量任务超时。<br>
 * 2. 如果在{@link EventLoop}上执行阻塞或死循环操作，则可能导致死锁，或大量任务超时。<br>
 *
 * @author wjybxx
 * date 2023/4/7
 */
@ThreadSafe
public interface EventLoop extends FixedEventLoopGroup, TimeProvider {

    /**
     * {@link EventLoop}的实现类在运行期间尽量将自己发布到该变量上，以供用户访问。
     */
    ThreadLocal<EventLoop> CURRENT = new ThreadLocal<>();

    /**
     * @return this - 由于{@link EventLoop}表示单个线程，因此总是分配自己。
     */
    @Nonnull
    @Override
    EventLoop next();

    /**
     * @return this - 由于{@link EventLoop}表示单个线程，因此总是选中自己
     */
    @Nonnull
    @Override
    EventLoop select(int key);

    /**
     * 返回该EventLoop线程所在的线程组（管理该EventLoop的容器）。
     * 如果没有父节点，返回null。
     */
    @Nullable
    EventLoopGroup parent();

    // region

    /**
     * 当前线程的时间 -- 纳秒
     * 我们约定EventLoop是单线程，因此一定是一个死循环结构，不论是为了性能还是时序安全，使用缓存时间都是必要的。
     * 注意：接口中并不约定时间的更新时机，也不约定一个大循环只更新一次。
     * 也就是说：线程可能在任意时间点更新缓存的时间，只要不破坏线程安全性和约定的任务时序。
     * <p>
     * 提供该接口以匹配{@link #schedule(Runnable, long, TimeUnit)}等接口
     */
    @Override
    long getTime();

    /**
     * 测试当前线程是否是{@link EventLoop}所在线程。
     * 主要作用:
     * 1. 判断是否可访问线程封闭的数据。
     * 2. 防止死锁。
     * <p>
     * 警告：如果用户基于该测试实现分支逻辑，则可能导致时序错误，eg：
     * <pre>
     * {@code
     * 		if(eventLoop.inEventLoop()) {
     * 	    	doSomething();
     *        } else{
     * 			eventLoop.execute(() -> doSomething());
     *        }
     * }
     * </pre>
     * 假设现在有3个线程：A、B、C，他们进行了约定，线程A投递任务后，告诉线程B，线程B投递后告诉线程C，线程C再投递，以期望任务按照A、B、C的顺序处理。
     * 在某个巧合下，线程C可能就是执行者线程，结果C的任务可能在A和B的任务之前被处理，从而破坏了外部约定的时序。
     * <p>
     * 该方法一定要慎用，它有时候是无害的，有时候则是有害的，因此必须想明白是否需要提供全局时序保证！
     *
     * @return true/false
     */
    boolean inEventLoop();

    /**
     * 测试给定线程是否是当前事件循环线程
     */
    boolean inEventLoop(Thread thread);

    /**
     * 如果当前{@link EventLoop}线程陷入了阻塞状态，则将线程从阻塞中唤醒
     */
    void wakeup();

    /**
     * 创建一个线程绑定的future以执行任务，返回的future将禁止在当前EventLoop上执行阻塞操作。
     *
     * @see CompletableFuture#completedFuture(Object)
     * @see CompletableFuture#failedFuture(Throwable)
     */
    <V> XCompletableFuture<V> newPromise();

    default <V> XCompletableFuture<V> newSucceededFuture(V result) {
        return FutureUtils.newSucceededFuture(result);
    }

    default <V> XCompletableFuture<V> newFailedFuture(Throwable cause) {
        return FutureUtils.newFailedFuture(cause);
    }

    // endregion
}