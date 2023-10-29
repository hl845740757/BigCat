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
import cn.wjybxx.common.concurrent.RejectedExecutionHandler;
import cn.wjybxx.common.concurrent.ext.TimeoutSleepingWaitStrategy;
import cn.wjybxx.common.rpc.RpcRegistry;
import com.google.inject.Injector;
import com.lmax.disruptor.WaitStrategy;

import java.util.ArrayList;
import java.util.List;
import java.util.Objects;
import java.util.concurrent.ThreadFactory;

/**
 * 建议先设置子类属性再设置父类属性，避免过多的重写
 *
 * @author wjybxx
 * date - 2023/10/4
 */
public abstract class WorkerBuilder {

    private String workerId;
    /**
     * Worker上绑定的容器
     * 1. 需要包含{@link MainModule}接口的实例
     * 2. 需要包含{@link RpcRegistry}接口的实例
     */
    private Injector injector;

    /**
     * Worker上挂载的模块类
     * 1. 需要能通过{@link #injector}获取实例
     * 2. 无需包含{@link MainModule}
     * 3. 添加顺序很重要，Worker将按照添加顺序启动所有的Module
     */
    private final List<Class<? extends WorkerModule>> moduleClasses = new ArrayList<>();

    /** 在真正构建时由{@link Node}赋值，同{@link #setParent(Node)} */
    private WorkerCtx workerCtx;
    /** Builder之间不方便继承 */
    private final EventLoopBuilder delegateBuilder;

    protected WorkerBuilder(EventLoopBuilder delegateBuilder) {
        this.delegateBuilder = Objects.requireNonNull(delegateBuilder);
    }

    public EventLoopBuilder getDelegateBuilder() {
        return delegateBuilder;
    }

    //
    public abstract Worker build();

    public Node getParent() {
        return (Node) delegateBuilder.getParent();
    }

    public WorkerBuilder setParent(Node parent) {
        delegateBuilder.setParent(parent);
        return this;
    }

    public ThreadFactory getThreadFactory() {
        return delegateBuilder.getThreadFactory();
    }

    public WorkerBuilder setThreadFactory(ThreadFactory threadFactory) {
        delegateBuilder.setThreadFactory(threadFactory);
        return this;
    }

    public RejectedExecutionHandler getRejectedExecutionHandler() {
        return delegateBuilder.getRejectedExecutionHandler();
    }

    public WorkerBuilder setRejectedExecutionHandler(RejectedExecutionHandler rejectedExecutionHandler) {
        delegateBuilder.setRejectedExecutionHandler(rejectedExecutionHandler);
        return this;
    }

    public WorkerCtx getWorkerCtx() {
        return workerCtx;
    }

    public WorkerBuilder setWorkerCtx(WorkerCtx workerCtx) {
        this.workerCtx = workerCtx;
        return this;
    }

    //

    public String getWorkerId() {
        return workerId;
    }

    public WorkerBuilder setWorkerId(String workerId) {
        this.workerId = workerId;
        return this;
    }

    public Injector getInjector() {
        return injector;
    }

    public WorkerBuilder setInjector(Injector injector) {
        this.injector = injector;
        return this;
    }

    public List<Class<? extends WorkerModule>> getModuleClasses() {
        return moduleClasses;
    }

    public WorkerBuilder addModule(Class<? extends WorkerModule> moduleClazz) {
        Objects.requireNonNull(moduleClazz);
        moduleClasses.add(moduleClazz);
        return this;
    }

    public static DisruptWorkerBuilder newDisruptorWorkerBuilder() {
        return new DisruptWorkerBuilder();
    }

    public static class DisruptWorkerBuilder extends WorkerBuilder {

        private DisruptWorkerBuilder() {
            super(EventLoopBuilder.newDisruptBuilder());
        }

        @Override
        public Worker build() {
            if (getThreadFactory() == null) {
                setThreadFactory(new DefaultThreadFactory("Worker"));
            }
            if (getWaitStrategy() == null) {
                setWaitStrategy(new TimeoutSleepingWaitStrategy());
            }
            return new WorkerImpl(this);
        }

        @Override
        public EventLoopBuilder.DisruptorBuilder getDelegateBuilder() {
            return (EventLoopBuilder.DisruptorBuilder) super.getDelegateBuilder();
        }

        public int getRingBufferSize() {
            return getDelegateBuilder().getRingBufferSize();
        }

        public DisruptWorkerBuilder setRingBufferSize(int ringBufferSize) {
            getDelegateBuilder().setRingBufferSize(ringBufferSize);
            return this;
        }

        public WaitStrategy getWaitStrategy() {
            return getDelegateBuilder().getWaitStrategy();
        }

        public DisruptWorkerBuilder setWaitStrategy(WaitStrategy waitStrategy) {
            getDelegateBuilder().setWaitStrategy(waitStrategy);
            return this;
        }

        public int getBatchSize() {
            return getDelegateBuilder().getBatchSize();
        }

        public DisruptWorkerBuilder setBatchSize(int batchSize) {
            getDelegateBuilder().setBatchSize(batchSize);
            return this;
        }

    }
}