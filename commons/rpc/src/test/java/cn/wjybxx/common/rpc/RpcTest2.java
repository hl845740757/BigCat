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

package cn.wjybxx.common.rpc;

import cn.wjybxx.common.rpc.RpcServiceExampleExporter;
import cn.wjybxx.common.ThreadUtils;
import cn.wjybxx.common.async.SameThreadScheduledExecutor;
import cn.wjybxx.common.async.SameThreads;
import cn.wjybxx.common.concurrent.WatchableEventQueue;
import cn.wjybxx.common.time.TimeProvider;
import cn.wjybxx.common.time.TimeProviders;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.concurrent.CountDownLatch;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicInteger;

/**
 * 使用注解处理器生成的代码进行调试
 *
 * @author wjybxx
 * date 2023/4/5
 */
public class RpcTest2 {

    private static final Logger logger = LoggerFactory.getLogger("RpcTest2");

    private SimpleEventQueue<RpcRequest> serverQueue;
    private SimpleEventQueue<RpcResponse> clientQueue;

    private Thread serverThread;
    private Thread clientThread;

    private final AtomicInteger counter = new AtomicInteger();
    private CountDownLatch latch = new CountDownLatch(2);
    private volatile boolean alert;

    @BeforeEach
    void setUp() throws InterruptedException {
        serverQueue = new SimpleEventQueue<>();
        clientQueue = new SimpleEventQueue<>();

        latch = new CountDownLatch(2);
        serverThread = new Thread(new ServerWorker());
        clientThread = new Thread(new ClientWorker());

        serverThread.start();
        clientThread.start();
        latch.await();
    }

    @Test
    void waitTimeout() throws InterruptedException {
        ThreadUtils.sleepQuietly(5 * 1000);
        alert = true;
        serverThread.interrupt();
        clientThread.interrupt();

        serverThread.join();
        clientThread.join();

        Assertions.assertTrue(counter.get() > 0, "succeeded count is zero");
        logger.info("succeeded rpc request " + counter.get());
    }

    // 模拟双端线程
    private class ServerWorker implements Runnable {

        final TimeProvider timeProvider;
        final SameThreadScheduledExecutor executor;

        private ServerWorker() {
            this.timeProvider = TimeProviders.systemTimeProvider();
            this.executor = SameThreads.newScheduledExecutor(timeProvider);
        }

        @Override
        public void run() {
            RpcRegistry registry = new DefaultRpcRegistry();
            SimpleNodeId role = SimpleNodeId.SERVER;
            // 注册服务
            RpcServiceExampleExporter.export(registry, new RpcServiceExample());

            TestRpcRouterReceiver routerReceiver = new TestRpcRouterReceiver();
            RpcClientImpl rpcClientImpl = new RpcClientImpl(role.id, role, routerReceiver, routerReceiver, registry,
                    timeProvider, 5 * 1000);
//            rpcSupportHandler.setRpcLogConfig(RpcLogConfig.ALL_SIMPLE);

            // 准备好以后执行countdown
            latch.countDown();
            try {
                while (!alert) {
                    RpcRequest request = serverQueue.poll(20, TimeUnit.MILLISECONDS);
                    if (request != null) {
                        rpcClientImpl.onRcvRequest(request);
                    }
                    executor.tick();
                    rpcClientImpl.tick();
                }
            } catch (InterruptedException e) {
                // 退出
            }
        }
    }

    private class ClientWorker implements Runnable {

        final TimeProvider timeProvider;
        final SameThreadScheduledExecutor executor;

        private ClientWorker() {
            this.timeProvider = TimeProviders.systemTimeProvider();
            this.executor = SameThreads.newScheduledExecutor(timeProvider);
        }

        @Override
        public void run() {
            RpcRegistry registry = new DefaultRpcRegistry();
            SimpleNodeId role = SimpleNodeId.CLIENT;

            TestRpcRouterReceiver routerReceiver = new TestRpcRouterReceiver();
            RpcClientImpl rpcClientImpl = new RpcClientImpl(role.id, role, routerReceiver, routerReceiver, registry,
                    timeProvider, 5 * 1000);
//            rpcSupportHandler.setRpcLogConfig(RpcLogConfig.ALL_SIMPLE);

            RpcUserExample rpcUserExample = new RpcUserExample(rpcClientImpl);
            executor.scheduleWithFixedDelay(() -> rpcTest(rpcUserExample), 200, 500);

            // 准备好以后执行countdown
            latch.countDown();
            try {
                while (!alert) {
                    RpcResponse response = clientQueue.poll(20, TimeUnit.MILLISECONDS);
                    if (response != null) {
                        rpcClientImpl.onRcvResponse(response);
                    }
                    executor.tick();
                    rpcClientImpl.tick();
                }
            } catch (InterruptedException e) {
                // 退出
            }
        }

        private void rpcTest(RpcUserExample rpcUserExample) {
            try {
                rpcUserExample.rpcTest();
                counter.incrementAndGet();
            } catch (RpcClientException e) {
                if (!alert) {
                    logger.info("client caught exception", e);
                }
            } catch (InterruptedException e) {
                ThreadUtils.recoveryInterrupted();
            } catch (Exception e) {
                logger.info("client caught exception", e);
            }
        }
    }

    // 模拟路由

    private class TestRpcRouterReceiver implements RpcSender, RpcReceiver {

        private TestRpcRouterReceiver() {
        }

        @Override
        public boolean send(NodeId target, Object proto) {
            SimpleNodeId targetRole = (SimpleNodeId) target;
            switch (targetRole) {
                case CLIENT -> {
                    return clientQueue.offer((RpcResponse) proto);
                }
                case SERVER -> {
                    return serverQueue.offer((RpcRequest) proto);
                }
            }
            return false;
        }

        @Override
        public boolean broadcast(NodeScope nodeScope, Object proto) {
            throw new AssertionError(); // 不测试该实现
        }

        @Override
        public void watch(WatchableEventQueue.Watcher<? super RpcResponse> watcher) {
            clientQueue.watch(watcher);
        }

        @Override
        public void cancelWatch(WatchableEventQueue.Watcher<? super RpcResponse> watcher) {
            clientQueue.cancelWatch(watcher);
        }
    }

}