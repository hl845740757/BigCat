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

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import javax.annotation.Nonnull;
import java.util.*;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicInteger;
import java.util.function.BiConsumer;
import java.util.function.Consumer;

/**
 * @author wjybxx
 * date 2023/4/8
 */
public class DefaultFixedEventLoopGroup extends AbstractEventLoopGroup implements FixedEventLoopGroup {

    private static final Logger logger = LoggerFactory.getLogger(DefaultFixedEventLoopGroup.class);
    /**
     * 监听所有子节点关闭的Listener，当所有的子节点关闭时，会收到关闭成功事件
     */
    private final XCompletableFuture<?> terminationFuture = new XCompletableFuture<>(new TerminateFutureContext());

    /**
     * 包含的子节点们，用数组，方便分配下一个EventExecutor(通过计算索引来分配)
     */
    private final EventLoop[] children;
    /**
     * 只读的子节点集合，封装为一个集合，方便迭代，用于实现{@link Iterable}接口
     */
    private final List<EventLoop> readonlyChildren;
    /**
     * 选择下一个EventExecutor的方式，策略模式的运用。将选择算法交给Chooser
     * 目前看见两种： 与操作计算 和 取模操作计算。
     */
    private final EventLoopChooser chooser;

    public DefaultFixedEventLoopGroup(EventLoopGroupBuilder builder) {
        int numberChildren = builder.getNumberChildren();
        if (numberChildren < 0) {
            throw new IllegalArgumentException("numberChildren must greater than 0");
        }
        EventLoopFactory eventLoopFactory = builder.getEventLoopFactory();
        if (eventLoopFactory == null) {
            throw new NullPointerException("eventLoopFactory");
        }
        EventLoopChooserFactory chooserFactory = builder.getChooserFactory();
        if (chooserFactory == null) {
            chooserFactory = new DefaultChooserFactory();
        }

        children = new EventLoop[numberChildren];
        for (int i = 0; i < numberChildren; i++) {
            EventLoop eventLoop = Objects.requireNonNull(eventLoopFactory.newChild(this, i));
            children[i] = eventLoop;
        }
        readonlyChildren = List.of(children);
        chooser = chooserFactory.newChooser(children);

        final ChildrenTerminateListener terminationListener = new ChildrenTerminateListener();
        for (EventLoop child : children) {
            child.terminationFuture().whenComplete(terminationListener);
        }
    }

    // -------------------------------------  子类生命周期管理 --------------------------------

    @Override
    public XCompletableFuture<?> terminationFuture() {
        return terminationFuture;
    }

    @Override
    public boolean isShuttingDown() {
        return Arrays.stream(children).allMatch(EventLoop::isShuttingDown);
    }

    @Override
    public boolean isShutdown() {
        return Arrays.stream(children).allMatch(EventLoop::isShutdown);
    }

    @Override
    public boolean isTerminated() {
        return Arrays.stream(children).allMatch(EventLoop::isTerminated);
    }

    @Override
    public boolean awaitTermination(long timeout, @Nonnull TimeUnit unit) throws InterruptedException {
        return terminationFuture().await(timeout, unit);
    }

    @Override
    public void shutdown() {
        forEach(EventLoop::shutdown);
    }

    @Nonnull
    @Override
    public List<Runnable> shutdownNow() {
        List<Runnable> tasks = new ArrayList<>();
        for (EventLoop eventLoop : children) {
            tasks.addAll(eventLoop.shutdownNow());
        }
        return tasks;
    }

    /**
     * 当所有的子节点都进入终结状态时，该方法将被调用
     * 通常用于执行一些清理工作
     */
    protected void terminateHook() {

    }

    // ------------------------------------- 迭代 ----------------------------

    @Nonnull
    @Override
    public EventLoop next() {
        return chooser.next();
    }

    @Nonnull
    @Override
    public EventLoop select(int key) {
        return chooser.select(key);
    }

    @Override
    public int numChildren() {
        return children.length;
    }

    @Nonnull
    @Override
    public Iterator<EventLoop> iterator() {
        return readonlyChildren.iterator();
    }

    @Override
    public void forEach(Consumer<? super EventLoop> action) {
        readonlyChildren.forEach(action);
    }

    @Override
    public Spliterator<EventLoop> spliterator() {
        return readonlyChildren.spliterator();
    }
    //

    /** 子节点终结状态监听器 */
    private class ChildrenTerminateListener implements BiConsumer<Object, Throwable> {

        /** 已关闭的子节点数量 */
        private final AtomicInteger terminatedChildren = new AtomicInteger(0);

        private ChildrenTerminateListener() {

        }

        @Override
        public void accept(Object o, Throwable throwable) {
            if (terminatedChildren.incrementAndGet() == children.length) {
                try {
                    terminateHook();
                } catch (Throwable e) {
                    logger.error("terminateHook caught exception!", e);
                } finally {
                    FutureUtils.completeTerminationFuture(terminationFuture);
                }
            }
        }

    }

}