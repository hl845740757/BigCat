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

import cn.wjybxx.base.Preconditions;
import cn.wjybxx.base.time.TimeProvider;
import cn.wjybxx.bigcat.pb.PBMethodInfoRegistry;
import cn.wjybxx.bigcat.rpc.RpcClient;
import cn.wjybxx.bigcat.rpc.RpcRegistry;
import cn.wjybxx.bigcat.rpc.RpcRouter;
import cn.wjybxx.bigcat.rpc.RpcSerializer;
import cn.wjybxx.concurrent.DefaultThreadFactory;
import cn.wjybxx.concurrent.EventLoopBuilder;
import cn.wjybxx.concurrent.EventLoopBuilder.DisruptorBuilder;
import cn.wjybxx.concurrent.RejectedExecutionHandler;
import cn.wjybxx.concurrent.RingBufferEvent;
import cn.wjybxx.disruptor.EventSequencer;
import cn.wjybxx.disruptor.RingBufferEventSequencer;
import cn.wjybxx.disruptor.WaitStrategy;
import com.google.inject.Injector;

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
     * Worker上绑定的容器，需要包含：
     * {@link MainModule}、{@link TimeModule}
     * {@link RpcClient}、{@link RpcRegistry}、
     * <p>
     * 如果是Node，则还需要包含：
     * {@link NodeRpcSupport}、{@link RpcRouter}、{@link RpcSerializer}、
     * {@link PBMethodInfoRegistry}、{@link TimeProvider}
     */
    private Injector injector;

    /**
     * Worker上挂载的模块类
     * 1. 需要能通过{@link #injector}获取实例
     * 2. 无需包含{@link MainModule}
     * 3. 添加顺序很重要，Worker将按照添加顺序启动所有的Module
     * 4. 实现类必须是{@link WorkerModule}的子类（注入的接口则不一定）
     */
    private final List<Class<?>> moduleClasses = new ArrayList<>();
    /**
     * Worker上挂载的服务类
     * 1.服务接口的实例必须在容器中存在
     * 2.
     */
    private final List<Class<?>> serviceClasses = new ArrayList<>();

    /** 在真正构建时由{@link Node}赋值，同{@link #setParent(Node)} */
    private WorkerCtx workerCtx;
    /** Builder之间不方便继承 */
    protected final EventLoopBuilder<RingBufferEvent> delegated;

    protected WorkerBuilder(EventLoopBuilder<RingBufferEvent> delegated) {
        this.delegated = Objects.requireNonNull(delegated);
    }

    public EventLoopBuilder<RingBufferEvent> getDelegated() {
        return delegated;
    }

    //
    public abstract Worker build();

    public Node getParent() {
        return (Node) delegated.getParent();
    }

    public WorkerBuilder setParent(Node parent) {
        delegated.setParent(parent);
        return this;
    }

    public ThreadFactory getThreadFactory() {
        return delegated.getThreadFactory();
    }

    public WorkerBuilder setThreadFactory(ThreadFactory threadFactory) {
        delegated.setThreadFactory(threadFactory);
        return this;
    }

    public int getBatchSize() {
        return delegated.getBatchSize();
    }

    public WorkerBuilder setBatchSize(int batchSize) {
        delegated.setBatchSize(batchSize);
        return this;
    }

    public RejectedExecutionHandler getRejectedExecutionHandler() {
        return delegated.getRejectedExecutionHandler();
    }

    public WorkerBuilder setRejectedExecutionHandler(RejectedExecutionHandler rejectedExecutionHandler) {
        delegated.setRejectedExecutionHandler(rejectedExecutionHandler);
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

    public List<Class<?>> getModuleClasses() {
        return moduleClasses;
    }

    public WorkerBuilder addModule(Class<?> moduleClazz) {
        Objects.requireNonNull(moduleClazz);
        moduleClasses.add(moduleClazz);
        return this;
    }

    public WorkerBuilder addModules(List<Class<?>> moduleClazz) {
        Preconditions.checkNullElements(moduleClazz);
        moduleClasses.addAll(moduleClazz);
        return this;
    }

    public List<Class<?>> getServiceClasses() {
        return serviceClasses;
    }

    public WorkerBuilder addService(Class<?> serviceClass) {
        Objects.requireNonNull(serviceClass);
        serviceClasses.add(serviceClass);
        return this;
    }

    public WorkerBuilder addServices(List<Class<?>> serviceClass) {
        Preconditions.checkNullElements(serviceClass);
        serviceClasses.addAll(serviceClass);
        return this;
    }

    //

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
            if (getEventSequencer() == null) {
                setEventSequencer(RingBufferEventSequencer.newMultiProducer(RingBufferEvent::new)
                        .setBufferSize(8 * 1024)
                        .build());
            }
            return new WorkerImpl(this);
        }

        @Override
        public DisruptorBuilder<RingBufferEvent> getDelegated() {
            return (DisruptorBuilder<RingBufferEvent>) super.getDelegated();
        }

        // region

        @Override
        public DisruptWorkerBuilder setParent(Node parent) {
            super.setParent(parent);
            return this;
        }

        @Override
        public DisruptWorkerBuilder setThreadFactory(ThreadFactory threadFactory) {
            super.setThreadFactory(threadFactory);
            return this;
        }

        @Override
        public DisruptWorkerBuilder setRejectedExecutionHandler(RejectedExecutionHandler rejectedExecutionHandler) {
            super.setRejectedExecutionHandler(rejectedExecutionHandler);
            return this;
        }

        @Override
        public DisruptWorkerBuilder setWorkerCtx(WorkerCtx workerCtx) {
            super.setWorkerCtx(workerCtx);
            return this;
        }

        @Override
        public DisruptWorkerBuilder setWorkerId(String workerId) {
            super.setWorkerId(workerId);
            return this;
        }

        @Override
        public DisruptWorkerBuilder setInjector(Injector injector) {
            super.setInjector(injector);
            return this;
        }

        @Override
        public DisruptWorkerBuilder addModule(Class<?> moduleClazz) {
            super.addModule(moduleClazz);
            return this;
        }

        @Override
        public DisruptWorkerBuilder addService(Class<?> serviceClass) {
            super.addService(serviceClass);
            return this;
        }

        @Override
        public DisruptWorkerBuilder addModules(List<Class<?>> moduleClazz) {
            super.addModules(moduleClazz);
            return this;
        }

        @Override
        public DisruptWorkerBuilder addServices(List<Class<?>> serviceClass) {
            super.addServices(serviceClass);
            return this;
        }

        // endregion

        // region disruptor

        public EventSequencer<? extends RingBufferEvent> getEventSequencer() {
            return getDelegated().getEventSequencer();
        }

        public DisruptWorkerBuilder setEventSequencer(EventSequencer<? extends RingBufferEvent> eventSequencer) {
            getDelegated().setEventSequencer(eventSequencer);
            return this;
        }

        public WaitStrategy getWaitStrategy() {
            return getDelegated().getWaitStrategy();
        }

        public DisruptWorkerBuilder setWaitStrategy(WaitStrategy waitStrategy) {
            getDelegated().setWaitStrategy(waitStrategy);
            return this;
        }

        public boolean isCleanBufferOnExit() {
            return getDelegated().isCleanBufferOnExit();
        }

        public DisruptWorkerBuilder setCleanBufferOnExit(boolean cleanBufferOnExit) {
            getDelegated().setCleanBufferOnExit(cleanBufferOnExit);
            return this;
        }

        // endregion
    }
}