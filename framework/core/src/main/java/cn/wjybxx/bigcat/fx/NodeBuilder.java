/*
 * Copyright 2023-2024 wjybxx(845740757@qq.com)
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

package cn.wjybxx.bigcat.fx;

import cn.wjybxx.concurrent.*;
import cn.wjybxx.concurrent.EventLoopBuilder.DisruptorBuilder;
import cn.wjybxx.disruptor.EventSequencer;
import cn.wjybxx.disruptor.MpUnboundedEventSequencer;
import cn.wjybxx.disruptor.WaitStrategy;
import com.google.inject.Injector;

import java.util.List;
import java.util.concurrent.ThreadFactory;

/**
 * 建议先设置子类属性再设置父类属性，避免过多的重写
 *
 * @author wjybxx
 * date - 2023/10/4
 */
public abstract class NodeBuilder extends WorkerBuilder {

    private int numberChildren = 1;
    private WorkerFactory workerFactory;
    private EventLoopChooserFactory chooserFactory;
    private WorkerAddr nodeAddr;

    protected NodeBuilder(EventLoopBuilder<RingBufferEvent> delegateBuilder) {
        super(delegateBuilder);
    }

    @Override
    public abstract Node build();

    // region

    @Override
    public NodeBuilder setParent(Node parent) {
        super.setParent(parent);
        return this;
    }

    @Override
    public NodeBuilder setThreadFactory(ThreadFactory threadFactory) {
        super.setThreadFactory(threadFactory);
        return this;
    }

    @Override
    public NodeBuilder setRejectedExecutionHandler(RejectedExecutionHandler rejectedExecutionHandler) {
        super.setRejectedExecutionHandler(rejectedExecutionHandler);
        return this;
    }

    @Override
    public WorkerBuilder setWorkerCtx(WorkerCtx workerCtx) {
        super.setWorkerCtx(workerCtx);
        return this;
    }

    @Override
    public NodeBuilder setWorkerId(String workerId) {
        super.setWorkerId(workerId);
        return this;
    }

    @Override
    public NodeBuilder setInjector(Injector injector) {
        super.setInjector(injector);
        return this;
    }

    @Override
    public NodeBuilder addModule(Class<?> moduleClazz) {
        super.addModule(moduleClazz);
        return this;
    }

    @Override
    public NodeBuilder addService(Class<?> serviceClass) {
        super.addService(serviceClass);
        return this;
    }

    @Override
    public NodeBuilder addModules(List<Class<?>> moduleClazz) {
        super.addModules(moduleClazz);
        return this;
    }

    @Override
    public NodeBuilder addServices(List<Class<?>> serviceClass) {
        super.addServices(serviceClass);
        return this;
    }

    // endregion

    // region
    public int getNumberChildren() {
        return numberChildren;
    }

    public NodeBuilder setNumberChildren(int numberChildren) {
        this.numberChildren = numberChildren;
        return this;
    }

    public WorkerFactory getWorkerFactory() {
        return workerFactory;
    }

    public NodeBuilder setWorkerFactory(WorkerFactory workerFactory) {
        this.workerFactory = workerFactory;
        return this;
    }

    public EventLoopChooserFactory getChooserFactory() {
        return chooserFactory;
    }

    public NodeBuilder setChooserFactory(EventLoopChooserFactory chooserFactory) {
        this.chooserFactory = chooserFactory;
        return this;
    }

    public WorkerAddr getNodeAddr() {
        return nodeAddr;
    }

    public NodeBuilder setNodeAddr(WorkerAddr nodeAddr) {
        this.nodeAddr = nodeAddr;
        return this;
    }
    // endregion

    public static DefaultNodeBuilder newDefaultNodeBuilder() {
        return new DefaultNodeBuilder();
    }

    public static class DefaultNodeBuilder extends NodeBuilder {

        private DefaultNodeBuilder() {
            super(EventLoopBuilder.newDisruptBuilder());
        }

        @Override
        public Node build() {
            if (getEventSequencer() == null) {
                setEventSequencer(MpUnboundedEventSequencer.newBuilder(RingBufferEvent::new)
                        .setChunkSize(1024)
                        .setMaxPooledChunks(8)
                        .build());
            }
            if (getThreadFactory() == null) {
                setThreadFactory(new DefaultThreadFactory("Node"));
            }
            if (getWorkerFactory() == null) {
                setWorkerFactory((parent, ctx, index) -> {
                    return WorkerBuilder.newDisruptorWorkerBuilder()
                            .setParent(parent)
                            .setWorkerCtx(ctx)
                            .setWorkerId("Worker-" + index)
                            .build();
                });
            }
            return new NodeImpl(this);
        }

        @Override
        public DisruptorBuilder<RingBufferEvent> getDelegated() {
            return (DisruptorBuilder<RingBufferEvent>) super.getDelegated();
        }

        @Override
        public DefaultNodeBuilder setBatchSize(int batchSize) {
            getDelegated().setBatchSize(batchSize);
            return this;
        }

        // region disruptor

        public EventSequencer<? extends RingBufferEvent> getEventSequencer() {
            return getDelegated().getEventSequencer();
        }

        public NodeBuilder setEventSequencer(EventSequencer<? extends RingBufferEvent> eventSequencer) {
            getDelegated().setEventSequencer(eventSequencer);
            return this;
        }

        public WaitStrategy getWaitStrategy() {
            return getDelegated().getWaitStrategy();
        }

        public DefaultNodeBuilder setWaitStrategy(WaitStrategy waitStrategy) {
            getDelegated().setWaitStrategy(waitStrategy);
            return this;
        }

        public boolean isCleanBufferOnExit() {
            return getDelegated().isCleanBufferOnExit();
        }

        public DefaultNodeBuilder setCleanBufferOnExit(boolean cleanBufferOnExit) {
            getDelegated().setCleanBufferOnExit(cleanBufferOnExit);
            return this;
        }

        // endregion

    }
}