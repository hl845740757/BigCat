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

import cn.wjybxx.bigcat.common.concurrent.ext.TimeoutSleepingWaitStrategy;
import com.lmax.disruptor.WaitStrategy;

import java.util.concurrent.ThreadFactory;

/**
 * @author wjybxx
 * date 2023/4/11
 */
public abstract class EventLoopBuilder {

    private EventLoopGroup parent;
    private RejectedExecutionHandler rejectedExecutionHandler = RejectedExecutionHandlers.abort();
    private ThreadFactory threadFactory;

    public EventLoopGroup getParent() {
        return parent;
    }

    public EventLoopBuilder setParent(EventLoopGroup parent) {
        this.parent = parent;
        return this;
    }

    public RejectedExecutionHandler getRejectedExecutionHandler() {
        return rejectedExecutionHandler;
    }

    public EventLoopBuilder setRejectedExecutionHandler(RejectedExecutionHandler rejectedExecutionHandler) {
        this.rejectedExecutionHandler = rejectedExecutionHandler;
        return this;
    }

    public ThreadFactory getThreadFactory() {
        return threadFactory;
    }

    public EventLoopBuilder setThreadFactory(ThreadFactory threadFactory) {
        this.threadFactory = threadFactory;
        return this;
    }

    public abstract EventLoop build();

    //

    public static DisruptBuilder newDisruptBuilder() {
        return new DisruptBuilder();
    }
    //

    public static class DisruptBuilder extends EventLoopBuilder {

        private EventLoopAgent<? super RingBufferEvent> agent;
        private int ringBufferSize = 8192;
        private WaitStrategy waitStrategy = new TimeoutSleepingWaitStrategy();
        private int batchSize = 8192;

        //

        @Override
        public DisruptBuilder setParent(EventLoopGroup parent) {
            return (DisruptBuilder) super.setParent(parent);
        }

        @Override
        public DisruptBuilder setRejectedExecutionHandler(RejectedExecutionHandler rejectedExecutionHandler) {
            return (DisruptBuilder) super.setRejectedExecutionHandler(rejectedExecutionHandler);
        }

        @Override
        public DisruptBuilder setThreadFactory(ThreadFactory threadFactory) {
            return (DisruptBuilder) super.setThreadFactory(threadFactory);
        }

        @Override
        public DisruptorEventLoop build() {
            return new DisruptorEventLoop(this);
        }

        //

        public EventLoopAgent<? super RingBufferEvent> getAgent() {
            return agent;
        }

        public DisruptBuilder setAgent(EventLoopAgent<? super RingBufferEvent> agent) {
            this.agent = agent;
            return this;
        }

        public int getRingBufferSize() {
            return ringBufferSize;
        }

        public DisruptBuilder setRingBufferSize(int ringBufferSize) {
            this.ringBufferSize = ringBufferSize;
            return this;
        }

        public WaitStrategy getWaitStrategy() {
            return waitStrategy;
        }

        public DisruptBuilder setWaitStrategy(WaitStrategy waitStrategy) {
            this.waitStrategy = waitStrategy;
            return this;
        }

        public int getBatchSize() {
            return batchSize;
        }

        public DisruptBuilder setBatchSize(int batchSize) {
            this.batchSize = batchSize;
            return this;
        }
    }

}