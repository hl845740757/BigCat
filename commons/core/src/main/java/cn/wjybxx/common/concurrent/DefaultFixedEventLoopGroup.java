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

    private final XCompletableFuture<?> terminationFuture = new XCompletableFuture<>(new TerminateFutureContext());

    private final EventLoop[] children;
    private final List<EventLoop> readonlyChildren;
    private final EventLoopChooser chooser;
    private final Runnable terminationHook;

    public DefaultFixedEventLoopGroup(EventLoopGroupBuilder builder) {
        int numberChildren = builder.getNumberChildren();
        if (numberChildren < 1) {
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
            if (eventLoop.parent() != this) throw new IllegalStateException("the parent of child is illegal");
            children[i] = eventLoop;
        }
        readonlyChildren = List.of(children);
        chooser = chooserFactory.newChooser(children.clone()); // 避免错误的实现修改引用
        terminationHook = builder.getTerminationHook();

        // 最后再监听，否则可能状态错误
        final ChildrenTerminateListener terminationListener = new ChildrenTerminateListener();
        for (EventLoop child : children) {
            child.terminationFuture().whenComplete(terminationListener);
        }
    }

    // -------------------------------------  子类生命周期管理 --------------------------------

    @Override
    public ICompletableFuture<?> terminationFuture() {
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
    protected void invokeTerminationHook() {
        if (terminationHook != null) {
            terminationHook.run();
        }
    }

    // ------------------------------------- 迭代 ----------------------------

    @Nonnull
    @Override
    public EventLoop select() {
        return chooser.select();
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
                    invokeTerminationHook();
                } catch (Throwable e) {
                    logger.error("terminateHook caught exception!", e);
                } finally {
                    FutureUtils.completeTerminationFuture(terminationFuture);
                }
            }
        }

    }

}