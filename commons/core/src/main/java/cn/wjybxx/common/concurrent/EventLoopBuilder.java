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

import cn.wjybxx.common.concurrent.ext.TimeoutSleepingWaitStrategy;
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

    public abstract EventLoop build();

    public EventLoopGroup getParent() {
        return parent;
    }

    public EventLoopBuilder setParent(EventLoopGroup parent) {
        this.parent = parent;
        return this;
    }

    public ThreadFactory getThreadFactory() {
        return threadFactory;
    }

    public EventLoopBuilder setThreadFactory(ThreadFactory threadFactory) {
        this.threadFactory = threadFactory;
        return this;
    }

    public RejectedExecutionHandler getRejectedExecutionHandler() {
        return rejectedExecutionHandler;
    }

    public EventLoopBuilder setRejectedExecutionHandler(RejectedExecutionHandler rejectedExecutionHandler) {
        this.rejectedExecutionHandler = rejectedExecutionHandler;
        return this;
    }

    //

    public static DisruptorBuilder newDisruptBuilder() {
        return new DisruptorBuilder();
    }

    public static DefaultBuilder newDefaultBuilder() {
        return new DefaultBuilder();
    }
    //

    public static class DefaultBuilder extends EventLoopBuilder {

        private EventLoopAgent<? super AgentEvent> agent;
        private WaitStrategy waitStrategy;
        private int batchSize = 8192;
        private int chunkSize = 1024;
        private int maxPooledChunks = 8;

        //

        @Override
        public DefaultBuilder setParent(EventLoopGroup parent) {
            super.setParent(parent);
            return this;
        }

        @Override
        public DefaultBuilder setThreadFactory(ThreadFactory threadFactory) {
            super.setThreadFactory(threadFactory);
            return this;
        }

        @Override
        public DefaultBuilder setRejectedExecutionHandler(RejectedExecutionHandler rejectedExecutionHandler) {
            super.setRejectedExecutionHandler(rejectedExecutionHandler);
            return this;
        }

        @Override
        public EventLoop build() {
            if (getThreadFactory() == null) {
                setThreadFactory(new DefaultThreadFactory("DisruptorEventLoop"));
            }
            if (waitStrategy == null) {
                waitStrategy = new TimeoutSleepingWaitStrategy();
            }
            return new DefaultEventLoop(this);
        }
        //

        public EventLoopAgent<? super AgentEvent> getAgent() {
            return agent;
        }

        public DefaultBuilder setAgent(EventLoopAgent<? super AgentEvent> agent) {
            this.agent = agent;
            return this;
        }

        public WaitStrategy getWaitStrategy() {
            return waitStrategy;
        }

        public DefaultBuilder setWaitStrategy(WaitStrategy waitStrategy) {
            this.waitStrategy = waitStrategy;
            return this;
        }

        public int getBatchSize() {
            return batchSize;
        }

        public DefaultBuilder setBatchSize(int batchSize) {
            this.batchSize = batchSize;
            return this;
        }

        public int getChunkSize() {
            return chunkSize;
        }

        public DefaultBuilder setChunkSize(int chunkSize) {
            this.chunkSize = chunkSize;
            return this;
        }

        public int getMaxPooledChunks() {
            return maxPooledChunks;
        }

        public DefaultBuilder setMaxPooledChunks(int maxPooledChunks) {
            this.maxPooledChunks = maxPooledChunks;
            return this;
        }
    }

    public static class DisruptorBuilder extends EventLoopBuilder {

        private EventLoopAgent<? super RingBufferEvent> agent;
        private int ringBufferSize = 8192;
        private WaitStrategy waitStrategy;
        private int batchSize = 8192;

        //

        @Override
        public DisruptorBuilder setParent(EventLoopGroup parent) {
            super.setParent(parent);
            return this;
        }

        @Override
        public DisruptorBuilder setRejectedExecutionHandler(RejectedExecutionHandler rejectedExecutionHandler) {
            super.setRejectedExecutionHandler(rejectedExecutionHandler);
            return this;
        }

        @Override
        public DisruptorBuilder setThreadFactory(ThreadFactory threadFactory) {
            super.setThreadFactory(threadFactory);
            return this;
        }

        @Override
        public DisruptorEventLoop build() {
            if (getThreadFactory() == null) {
                setThreadFactory(new DefaultThreadFactory("DisruptorEventLoop"));
            }
            if (waitStrategy == null) {
                waitStrategy = new TimeoutSleepingWaitStrategy();
            }
            return new DisruptorEventLoop(this);
        }

        //

        public EventLoopAgent<? super RingBufferEvent> getAgent() {
            return agent;
        }

        public DisruptorBuilder setAgent(EventLoopAgent<? super RingBufferEvent> agent) {
            this.agent = agent;
            return this;
        }

        public int getRingBufferSize() {
            return ringBufferSize;
        }

        public DisruptorBuilder setRingBufferSize(int ringBufferSize) {
            this.ringBufferSize = ringBufferSize;
            return this;
        }

        public WaitStrategy getWaitStrategy() {
            return waitStrategy;
        }

        public DisruptorBuilder setWaitStrategy(WaitStrategy waitStrategy) {
            this.waitStrategy = waitStrategy;
            return this;
        }

        public int getBatchSize() {
            return batchSize;
        }

        public DisruptorBuilder setBatchSize(int batchSize) {
            this.batchSize = batchSize;
            return this;
        }
    }

}