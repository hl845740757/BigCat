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
import cn.wjybxx.disruptor.EventSequencer;
import cn.wjybxx.disruptor.MpUnboundedEventSequencer;
import cn.wjybxx.disruptor.TimeoutSleepingWaitStrategy;
import cn.wjybxx.disruptor.WaitStrategy;

/**
 * @author wjybxx
 * date - 2025/4/17
 */
public class DefaultNodeBuilder extends NodeBuilder {

    DefaultNodeBuilder() {
        super(EventLoopBuilder.newDisruptBuilder());
    }

    @Override
    public Node build() {
        if (getEventSequencer() == null) {
            setEventSequencer(MpUnboundedEventSequencer.newBuilder(WorkerEvent::new)
                    .setWaitStrategy(TimeoutSleepingWaitStrategy.INSTANCE)
                    .setChunkSize(1024)
                    .setMaxPooledChunks(8)
                    .build());
        }
        if (getWorkerFactory() == null) {
            setWorkerFactory((parent, index, controlData) -> {
                return WorkerBuilder.newDisruptorWorkerBuilder()
                        .setParent(parent)
                        .setControlData(controlData)
                        .setWorkerId("Worker-" + index)
                        .build();
            });
        }
        if (getThreadFactory() == null) {
            setThreadFactory(new DefaultThreadFactory("Node"));
        }
        return new NodeImpl(this);
    }

    @Override
    public EventLoopBuilder.DisruptorBuilder<WorkerEvent> getDelegated() {
        return (EventLoopBuilder.DisruptorBuilder<WorkerEvent>) super.getDelegated();
    }

    @Override
    public DefaultNodeBuilder setBatchSize(int batchSize) {
        getDelegated().setBatchSize(batchSize);
        return this;
    }

    // region disruptor

    public EventSequencer<? extends WorkerEvent> getEventSequencer() {
        return getDelegated().getEventSequencer();
    }

    public NodeBuilder setEventSequencer(EventSequencer<? extends WorkerEvent> eventSequencer) {
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

    // endregion

}