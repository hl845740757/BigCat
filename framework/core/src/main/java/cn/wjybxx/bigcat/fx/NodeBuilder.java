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

package cn.wjybxx.bigcat.fx;

import cn.wjybxx.common.concurrent.DefaultThreadFactory;
import cn.wjybxx.common.concurrent.EventLoopBuilder;
import cn.wjybxx.common.concurrent.EventLoopChooserFactory;
import cn.wjybxx.common.concurrent.RejectedExecutionHandler;
import cn.wjybxx.common.concurrent.ext.TimeoutSleepingWaitStrategy;
import com.google.inject.Injector;
import com.lmax.disruptor.WaitStrategy;

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

    protected NodeBuilder(EventLoopBuilder delegateBuilder) {
        super(delegateBuilder);
    }

    // region 重写返回值类型
    @Override
    public abstract Node build();

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
    public NodeBuilder addModule(Class<? extends WorkerModule> moduleClazz) {
        super.addModule(moduleClazz);
        return this;
    }

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
            super(EventLoopBuilder.newDefaultBuilder());
        }

        @Override
        public Node build() {
            if (getWaitStrategy() == null) {
                setWaitStrategy(new TimeoutSleepingWaitStrategy());
            }
            if (getThreadFactory() == null) {
                setThreadFactory(new DefaultThreadFactory("Node"));
            }
            return new NodeImpl(this);
        }

        @Override
        public EventLoopBuilder.DefaultBuilder getDelegateBuilder() {
            return (EventLoopBuilder.DefaultBuilder) super.getDelegateBuilder();
        }

        public WaitStrategy getWaitStrategy() {
            return getDelegateBuilder().getWaitStrategy();
        }

        public DefaultNodeBuilder setWaitStrategy(WaitStrategy waitStrategy) {
            getDelegateBuilder().setWaitStrategy(waitStrategy);
            return this;
        }

        public int getBatchSize() {
            return getDelegateBuilder().getBatchSize();
        }

        public DefaultNodeBuilder setBatchSize(int batchSize) {
            getDelegateBuilder().setBatchSize(batchSize);
            return this;
        }

        public int getChunkSize() {
            return getDelegateBuilder().getChunkSize();
        }

        public DefaultNodeBuilder setChunkSize(int chunkSize) {
            getDelegateBuilder().setChunkSize(chunkSize);
            return this;
        }

        public int getMaxPooledChunks() {
            return getDelegateBuilder().getMaxPooledChunks();
        }

        public DefaultNodeBuilder setMaxPooledChunks(int maxPooledChunks) {
            getDelegateBuilder().setMaxPooledChunks(maxPooledChunks);
            return this;
        }
    }
}