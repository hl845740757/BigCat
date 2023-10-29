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
import cn.wjybxx.common.rpc.*;
import cn.wjybxx.common.time.Regulator;
import cn.wjybxx.common.time.TimeProvider;
import com.google.inject.*;
import org.junit.jupiter.api.AfterAll;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.BeforeAll;
import org.junit.jupiter.api.Test;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Objects;
import java.util.concurrent.ConcurrentLinkedQueue;

/**
 * @author wjybxx
 * date - 2023/10/5
 */
public class NodeTest {

    private static final Logger logger = LoggerFactory.getLogger(NodeTest.class);
    private static Node node;

    @BeforeAll
    static void setUp() {
        node = NodeBuilder.newDefaultNodeBuilder()
                .setNodeAddr(new WorkerAddr(1, 1))
                .setWorkerId("Node")
                // 初始化模块
                .setInjector(createNodeInjector())
                .addModule(WorkerRpcClient.class)
                .addModule(NodeRpcSupport.class)
                .addModule(TestRpcSender.class)
                // 初始化Worker
                .setWorkerFactory((parent, workerCtx, index) -> {
                    return WorkerBuilder.newDisruptorWorkerBuilder()
                            .setWorkerId("Worker-" + index)
                            .setParent(parent)
                            .setWorkerCtx(workerCtx)
                            // 初始化模块
                            .setInjector(createWorkerInjector())
                            .addModule(WorkerRpcClient.class)
                            .addModule(TestWorkerModule.class)
                            .build();
                })
                .build();

        node.start().join();
    }

    @AfterAll
    static void tearDown() {
        if (node != null) {
            node.shutdownNow();
            node.terminationFuture().join();
        }
    }

    @Test
    void test() {
        // 查看日志
        ThreadUtils.sleepQuietly(5000);
    }

    @Test
    void testFireEvent() {
        for (int idx = 0; idx < 10; idx++) {
            RingBufferEvent event = new RingBufferEvent();
            event.setType(1);
            event.intVal1 = idx;

            node.execute(event);
            ThreadUtils.sleepQuietly(10);
        }
    }

    private static Injector createNodeInjector() {
        return Guice.createInjector(new AbstractModule() {
            @Override
            protected void configure() {
                super.configure();
                binder().requireExplicitBindings(); // 获取未显式绑定的实例时抛出异常，避免获取到错误的实例

                bind(MainModule.class).to(TestMainModule.class).in(Singleton.class);
                bind(RpcClient.class).to(WorkerRpcClient.class).in(Singleton.class);
                bind(WorkerRpcClient.class).in(Singleton.class);
                bind(RpcRegistry.class).to(DefaultRpcRegistry.class).in(Singleton.class);
                bind(TimeProvider.class).to(TimeModule.class).in(Singleton.class); // 部分地方依赖的是TimeProvider
                bind(TimeModule.class).in(Singleton.class);

                bind(NodeRpcSupport.class).in(Singleton.class);
                bind(RpcSerializer.class).to(TestRpcSerializer.class).in(Singleton.class);

                // 记得以前超类绑定到子类时指定Singleton，子类不需要单独声明Singleton，现在怎么不行了....
                // 子类如果不单独绑定，则会创建一个新的实例，各种bug...
                bind(NodeRpcSender.class).to(TestRpcSender.class).in(Singleton.class);
                bind(TestRpcSender.class).in(Singleton.class);
            }
        });
    }

    private static Injector createWorkerInjector() {
        return Guice.createInjector(new AbstractModule() {
            @Override
            protected void configure() {
                super.configure();
                binder().requireExplicitBindings();

                bind(MainModule.class).to(TestMainModule.class).in(Singleton.class);
                bind(RpcClient.class).to(WorkerRpcClient.class).in(Singleton.class);
                bind(WorkerRpcClient.class).in(Singleton.class);
                bind(RpcRegistry.class).to(DefaultRpcRegistry.class).in(Singleton.class);
                bind(TimeProvider.class).to(TimeModule.class).in(Singleton.class);
                bind(TimeModule.class).in(Singleton.class);

                bind(TestWorkerModule.class).in(Singleton.class);
            }
        });
    }

    private static class TestWorkerModule implements WorkerModule {

        final Regulator regulator = Regulator.newFixedDelay(1, 100);
        Worker worker;
        NodeRpcSupport rpcSupport;

        @Inject
        RpcRegistry registry;
        @Inject
        TimeModule timeModule;
        @Inject
        RpcClient rpcClient;

        @Override
        public void inject(Worker worker) {
            this.worker = worker;
            this.rpcSupport = Objects.requireNonNull(worker.parent()).injector().getInstance(NodeRpcSupport.class);
        }

        @Override
        public void start() {
            regulator.restart(timeModule.getTime());
            RpcServiceExampleExporter.export(registry, new RpcServiceExample());
        }

        @Override
        public void update() {
            if (regulator.isReady(timeModule.getTime())) {
                String msg = "time: " + regulator.getLastUpdateTime();
                rpcClient.call(StaticRpcAddr.LOCAL, RpcServiceExampleProxy.echo(msg))
                        .thenAccept(result -> {
                            if (rpcSupport.isEnableLocalShare()) { // 启用本地共享的情况下应当是同一个字符串
                                Assertions.assertSame(msg, result);
                            } else {
                                Assertions.assertEquals(msg, result);
                            }
                            Assertions.assertTrue(worker.inEventLoop(), "worker.inEventLoop");
                            logger.info("rcv echo " + result);
                        });
            }
        }

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
        public void onEvent(AgentEvent rawEvent) throws Exception {
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

    private static class TestRpcSender implements NodeRpcSender, WorkerModule {

        private final ConcurrentLinkedQueue<RpcProtocol> protocolQueue = new ConcurrentLinkedQueue<>();
        private volatile boolean shuttingDown;
        private Thread thread;

        @Inject
        private NodeRpcSupport rpcSupport;

        @Override
        public void start() {
            thread = new Thread(this::subThreadLoop);
            thread.setName("RpcSender");
            thread.setDaemon(true);
            thread.start();
        }

        @Override
        public boolean send(RpcProtocol proto) {
            Objects.requireNonNull(proto);
            protocolQueue.offer(proto);
            return true;
        }

        @Override
        public void stop() {
            shuttingDown = true;
            thread.interrupt();
        }

        // 该方法由子线程调用
        private void onProtocol(RpcProtocol protocol) {
            if (protocol instanceof RpcRequest request) {
                rpcSupport.onRcvRequest(request);
            } else if (protocol instanceof RpcResponse response) {
                rpcSupport.onRcvResponse(response);
            }
        }

        // 该方法为子线程循环，不能在主线程，否则无法支持同步rpc调用
        private void subThreadLoop() {
            RpcProtocol protocol;
            while (!shuttingDown) {
                protocol = protocolQueue.poll();
                if (protocol == null) {
                    ThreadUtils.sleepQuietly(1);
                    continue;
                }
                onProtocol(protocol);
            }
        }

    }

}