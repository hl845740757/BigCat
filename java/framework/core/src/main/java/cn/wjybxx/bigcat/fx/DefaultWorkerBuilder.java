/*
 * Copyright 2023-2025 wjybxx(845740757@qq.com)
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

import cn.wjybxx.concurrent.DefaultThreadFactory;
import cn.wjybxx.concurrent.EventLoopBuilder;
import cn.wjybxx.concurrent.IEventLoopAgent;
import cn.wjybxx.concurrent.RejectedExecutionHandler;
import cn.wjybxx.disruptor.EventSequencer;
import cn.wjybxx.disruptor.RingBufferEventSequencer;
import cn.wjybxx.disruptor.TimeoutSleepingWaitStrategy;
import cn.wjybxx.disruptor.WaitStrategy;
import com.google.inject.Injector;

import java.util.List;
import java.util.concurrent.ThreadFactory;

/**
 * @author wjybxx
 * date - 2025/4/16
 */
public class DefaultWorkerBuilder extends WorkerBuilder {

    public DefaultWorkerBuilder() {
        super(EventLoopBuilder.newDisruptBuilder());
    }

    @Override
    public Worker build() {
        if (getEventSequencer() == null) {
            setEventSequencer(RingBufferEventSequencer.newMultiProducer(WorkerEvent::new)
                    .setWaitStrategy(TimeoutSleepingWaitStrategy.INSTANCE)
                    .setBufferSize(8 * 1024)
                    .build());
        }
        if (getThreadFactory() == null) {
            setThreadFactory(new DefaultThreadFactory("Worker"));
        }
        return new WorkerImpl(this);
    }

    @Override
    public EventLoopBuilder.DisruptorBuilder<WorkerEvent> getDelegated() {
        return (EventLoopBuilder.DisruptorBuilder<WorkerEvent>) super.getDelegated();
    }

    // region

    @Override
    public DefaultWorkerBuilder setParent(Node parent) {
        super.setParent(parent);
        return this;
    }

    @Override
    public DefaultWorkerBuilder setThreadFactory(ThreadFactory threadFactory) {
        super.setThreadFactory(threadFactory);
        return this;
    }

    @Override
    public DefaultWorkerBuilder setRejectedExecutionHandler(RejectedExecutionHandler rejectedExecutionHandler) {
        super.setRejectedExecutionHandler(rejectedExecutionHandler);
        return this;
    }

    @Override
    public DefaultWorkerBuilder setAgent(IEventLoopAgent<WorkerEvent> agent) {
        super.setAgent(agent);
        return this;
    }

    @Override
    public DefaultWorkerBuilder setControlData(WorkerControlData controlData) {
        super.setControlData(controlData);
        return this;
    }

    @Override
    public DefaultWorkerBuilder setManualClose(Boolean manualClose) {
        super.setManualClose(manualClose);
        return this;
    }

    @Override
    public DefaultWorkerBuilder setWorkerId(String workerId) {
        super.setWorkerId(workerId);
        return this;
    }

    @Override
    public DefaultWorkerBuilder setInjector(Injector injector) {
        super.setInjector(injector);
        return this;
    }

    @Override
    public DefaultWorkerBuilder addModule(Class<?> moduleClazz) {
        super.addModule(moduleClazz);
        return this;
    }

    @Override
    public DefaultWorkerBuilder addService(Class<?> serviceClass) {
        super.addService(serviceClass);
        return this;
    }

    @Override
    public DefaultWorkerBuilder addModules(List<Class<?>> moduleClazz) {
        super.addModules(moduleClazz);
        return this;
    }

    @Override
    public DefaultWorkerBuilder addServices(List<Class<?>> serviceClass) {
        super.addServices(serviceClass);
        return this;
    }

    // endregion

    // region disruptor

    public EventSequencer<? extends WorkerEvent> getEventSequencer() {
        return getDelegated().getEventSequencer();
    }

    public DefaultWorkerBuilder setEventSequencer(EventSequencer<? extends WorkerEvent> eventSequencer) {
        getDelegated().setEventSequencer(eventSequencer);
        return this;
    }

    public WaitStrategy getWaitStrategy() {
        return getDelegated().getWaitStrategy();
    }

    public DefaultWorkerBuilder setWaitStrategy(WaitStrategy waitStrategy) {
        getDelegated().setWaitStrategy(waitStrategy);
        return this;
    }

    // endregion
}