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

import cn.wjybxx.common.ThreadUtils;
import cn.wjybxx.common.concurrent.AgentEvent;
import cn.wjybxx.common.concurrent.RingBufferEvent;
import cn.wjybxx.common.rpc.DefaultRpcRegistry;
import cn.wjybxx.common.rpc.RpcRegistry;
import com.google.inject.*;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

/**
 * @author wjybxx
 * date - 2023/10/5
 */
public class NodeTest {

    private static final Logger logger = LoggerFactory.getLogger(NodeTest.class);

    Node node;

    @BeforeEach
    void setUp() {
        node = NodeBuilder.newDefaultNodeBuilder()
                .setInjector(createInjector())
                .setNodeAddr(StaticRpcAddr.LOCAL)
                .setWorkerId("Node")
                .setWorkerFactory((parent, index) -> {
                    return WorkerBuilder.newDisruptorWorkerBuilder()
                            .setWorkerId("Worker-" + index)
                            .setParent(parent)
                            .setInjector(createInjector())
                            .build();
                })
                .build();
    }

    @Test
    void test() {
        node.start().join();
        ThreadUtils.sleepQuietly(5000);

        node.shutdown();
        node.terminationFuture().join();
    }

    @Test
    void testFireEvent() {
        node.start().join();

        for (int idx = 0; idx < 10; idx++) {
            RingBufferEvent event = new RingBufferEvent();
            event.setType(1);
            event.intVal1 = idx;

            node.execute(event);
            ThreadUtils.sleepQuietly(10);
        }

        node.shutdown();
        node.terminationFuture().join();
    }

    private static Injector createInjector() {
        return Guice.createInjector(new AbstractModule() {
            @Override
            protected void configure() {
                super.configure();
                bind(MainModule.class).to(TestMainModule.class).in(Singleton.class);
                bind(RpcRegistry.class).to(DefaultRpcRegistry.class).in(Singleton.class);
                bind(TimeModule.class).in(Singleton.class);
            }
        });
    }

    private static class TestMainModule implements MainModule {

        private Worker worker;
        @Inject
        private TimeModule timeModule;

        @Override
        public void inject(Worker worker) {
            this.worker = worker;
        }

        @Override
        public void start() {
            timeModule.start(System.currentTimeMillis());
        }

        @Override
        public boolean checkMainLoop() {
            return System.currentTimeMillis() - timeModule.getTime() >= 10;
        }

        @Override
        public void beforeMainLoop() {
            timeModule.update(System.currentTimeMillis());
        }

        @Override
        public void afterMainLoop() {

        }

        @Override
        public void onEvent(AgentEvent rawEvent) {
            RingBufferEvent event = (RingBufferEvent) rawEvent;
            logger.info("eventType: {}, index: {}", event.getType(), event.intVal1);
        }

        @Override
        public void beforeWorkerStart() {
            logger.info("beforeWorkerStart: " + worker.workerId());
        }

        @Override
        public void afterWorkerStart() {
            logger.info("afterWorkerStart: " + worker.workerId());
        }

        @Override
        public void beforeWorkerShutdown() {
            logger.info("beforeWorkerShutdown: " + worker.workerId());
        }

        @Override
        public void afterWorkerShutdown() {
            logger.info("afterWorkerShutdown: " + worker.workerId());
        }
    }
}