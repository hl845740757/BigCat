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

package cn.wjybxx.bigcat.common;

import cn.wjybxx.bigcat.common.async.FluentFuture;
import cn.wjybxx.bigcat.common.async.FutureUtils;
import cn.wjybxx.bigcat.common.async.SameThreadScheduledExecutor;
import cn.wjybxx.bigcat.common.concurrent.WatchableEventQueue;
import cn.wjybxx.bigcat.common.rpc.*;
import cn.wjybxx.bigcat.common.time.TimeProvider;
import cn.wjybxx.bigcat.common.time.TimeProviders;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.List;
import java.util.Objects;
import java.util.concurrent.CountDownLatch;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicInteger;

/**
 * @author wjybxx
 * date 2023/4/5
 */
public class RpcTest {

    private static final Logger logger = LoggerFactory.getLogger("RpcTest");

    private static final short serviceId = 1;
    private static final short helloMethodId = 1;
    private static final short helloAsyncMethodId = 2;

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
        logger.info("succeeded rpc quest " + counter.get());
    }

    // 模拟业务双方
    private static String reverseString(String msg) {
        return new StringBuilder(msg).reverse().toString();
    }

    private static class TestService {

        private final SameThreadScheduledExecutor executor;

        private TestService(SameThreadScheduledExecutor executor) {
            this.executor = executor;
        }

        public String hello(String msg) {
            return reverseString(msg);
        }

        public FluentFuture<String> helloAsync(String msg) {
            return executor.scheduleCall(500, () -> hello(msg));
        }

    }

    private class TestClient {

        final TimeProvider timeProvider;
        final RpcSupportHandler rpcSupportHandler;

        private TestClient(TimeProvider timeProvider, RpcSupportHandler rpcSupportHandler) {
            this.timeProvider = timeProvider;
            this.rpcSupportHandler = rpcSupportHandler;
        }

        void assertResponse(String request, String response) {
            if (Objects.equals(request, reverseString(response))) {
                counter.incrementAndGet();
            } else {
                logger.warn("the request and response are not equal, request {}, response {}", request, response);
            }
        }

        public void syncSayHello() throws InterruptedException {
            {
                String msg1 = "local syncSayHello -- remote helloAsync" + ", time " + timeProvider.getTime();
                String response1 = rpcSupportHandler.syncCall(Role.SERVER, new DefaultRpcMethodSpec<>(
                        serviceId,
                        helloMethodId,
                        List.of(msg1)
                ));
                assertResponse(msg1, response1);
            }
            {
                String msg2 = "local syncSayHello -- remote helloAsync" + ", time " + timeProvider.getTime();
                String response2 = rpcSupportHandler.syncCall(Role.SERVER, new DefaultRpcMethodSpec<>(
                        serviceId,
                        helloAsyncMethodId,
                        List.of(msg2)
                ));
                assertResponse(msg2, response2);
            }
        }

        public void sayHello() {
            {
                String msg1 = "local sayHello -- remote helloAsync" + ", time " + timeProvider.getTime();
                rpcSupportHandler.call(Role.SERVER, new DefaultRpcMethodSpec<String>(
                        serviceId,
                        helloMethodId,
                        List.of(msg1)
                )).thenApply(FutureUtils.toFunction(response -> assertResponse(msg1, response)));
            }
            {
                String msg2 = "local sayHello -- remote helloAsync" + ", time " + timeProvider.getTime();
                rpcSupportHandler.call(Role.SERVER, new DefaultRpcMethodSpec<String>(
                        serviceId,
                        helloAsyncMethodId,
                        List.of(msg2)
                )).thenApply(FutureUtils.toFunction(response -> assertResponse(msg2, response)));
            }
        }
    }
    //

    // 模拟双端线程
    private class ServerWorker implements Runnable {

        final TimeProvider timeProvider;
        final SameThreadScheduledExecutor executor;

        private ServerWorker() {
            this.timeProvider = TimeProviders.systemTimeProvider();
            this.executor = FutureUtils.newScheduledExecutor(timeProvider);
        }

        @Override
        public void run() {
            DefaultRpcProcessor rpcProcessor = new DefaultRpcProcessor();
            Role role = Role.SERVER;
            // 注册服务
            TestService testService = new TestService(executor);
            rpcProcessor.register(serviceId, helloMethodId, (context, methodSpec) -> {
                return testService.hello(methodSpec.getString(0));
            });
            rpcProcessor.register(serviceId, helloAsyncMethodId, (context, methodSpec) -> {
                return testService.helloAsync(methodSpec.getString(0));
            });

            TestRpcRouterReceiver routerReceiver = new TestRpcRouterReceiver();
            RpcSupportHandler rpcSupportHandler = new RpcSupportHandler(role.guid, role, routerReceiver, routerReceiver, rpcProcessor,
                    timeProvider, 5 * 1000);
//            rpcSupportHandler.setRpcLogConfig(RpcLogConfig.ALL_SIMPLE);

            // 准备好以后执行countdown
            latch.countDown();
            try {
                while (!alert) {
                    RpcRequest request = serverQueue.poll(20, TimeUnit.MILLISECONDS);
                    if (request != null) {
                        rpcSupportHandler.onRcvRequest(request);
                    }
                    executor.tick();
                    rpcSupportHandler.tick();
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
            this.executor = FutureUtils.newScheduledExecutor(timeProvider);
        }

        @Override
        public void run() {
            DefaultRpcProcessor rpcProcessor = new DefaultRpcProcessor();
            Role role = Role.CLIENT;

            TestRpcRouterReceiver routerReceiver = new TestRpcRouterReceiver();
            RpcSupportHandler rpcSupportHandler = new RpcSupportHandler(role.guid, role, routerReceiver, routerReceiver, rpcProcessor,
                    timeProvider, 5 * 1000);
//            rpcSupportHandler.setRpcLogConfig(RpcLogConfig.ALL_SIMPLE);

            TestClient testClient = new TestClient(timeProvider, rpcSupportHandler);
            executor.scheduleFixedDelay(200, 300, testClient::sayHello);
            executor.scheduleFixedDelay(200, 500, () -> {
                try {
                    testClient.syncSayHello();
                } catch (InterruptedException e) {
                    ThreadUtils.recoveryInterrupted();
                } catch (RpcClientException e) {
                    if (!alert) {
                        logger.info("client caught exception", e);
                    }
                }
            });

            // 准备好以后执行countdown
            latch.countDown();
            try {
                while (!alert) {
                    RpcResponse response = clientQueue.poll(20, TimeUnit.MILLISECONDS);
                    if (response != null) {
                        rpcSupportHandler.onRcvResponse(response);
                    }
                    executor.tick();
                    rpcSupportHandler.tick();
                }
            } catch (InterruptedException e) {
                // 退出
            }
        }
    }

    // 模拟路由
    private enum Role implements NodeSpec {

        SERVER(1),
        CLIENT(2);

        final long guid;

        Role(long guid) {
            this.guid = guid;
        }
    }

    private class TestRpcRouterReceiver implements RpcRouterHandler, RpcReceiverHandler {

        private TestRpcRouterReceiver() {
        }

        @Override
        public boolean send(NodeSpec target, Object proto) {
            Role targetRole = (Role) target;
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
        public boolean broadcast(ScopeSpec scopeSpec, Object proto) {
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