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

import cn.wjybxx.common.FunctionUtils;
import cn.wjybxx.common.ThreadUtils;
import cn.wjybxx.common.async.SameThreadScheduledExecutor;
import cn.wjybxx.common.async.SameThreads;
import cn.wjybxx.common.time.TimeProvider;
import cn.wjybxx.common.time.TimeProviders;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.List;
import java.util.Objects;
import java.util.concurrent.*;
import java.util.concurrent.atomic.AtomicInteger;

/**
 * 手动绑定方法调试（项目中不建议使用）
 * 仅用于验证rpc的正确性
 *
 * @author wjybxx
 * date 2023/4/5
 */
public class RpcTest1 {

    private static final Logger logger = LoggerFactory.getLogger("RpcTest1");

    private static final short serviceId = 1;
    private static final short helloMethodId = 1;
    private static final short helloAsyncMethodId = 2;

    private BlockingQueue<RpcProtocol> serverQueue;
    private BlockingQueue<RpcProtocol> clientQueue;
    private ServerWorker serverWorker;
    private ClientWorker clientWorker;

    private Thread serverThread;
    private Thread clientThread;

    private final AtomicInteger counter = new AtomicInteger();
    private CountDownLatch latch = new CountDownLatch(2);
    private volatile boolean alert;

    void test() throws InterruptedException {
        serverQueue = new LinkedBlockingQueue<>();
        clientQueue = new LinkedBlockingQueue<>();

        serverThread = new Thread((serverWorker = new ServerWorker()));
        clientThread = new Thread((clientWorker = new ClientWorker()));
        latch = new CountDownLatch(2);

        serverThread.start();
        clientThread.start();
        latch.await();

        ThreadUtils.sleepQuietly(5 * 1000);
        alert = true;
        serverThread.interrupt();
        clientThread.interrupt();

        serverThread.join();
        clientThread.join();

        Assertions.assertTrue(counter.get() > 0, "succeeded count is zero");
        logger.info("succeeded rpc request " + counter.get());
    }

    @Test
    void basicTest() throws InterruptedException {
        test();
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

        public CompletableFuture<String> helloAsync(String msg) {
            CompletableFuture<String> future = new CompletableFuture<>();
            SameThreads.setFuture(future, executor.scheduleCall(() -> hello(msg), 500));
            return future;
        }

    }

    private class TestClient {

        final TimeProvider timeProvider;
        final RpcClient rpcClient;

        private TestClient(TimeProvider timeProvider, RpcClient rpcClient) {
            this.timeProvider = timeProvider;
            this.rpcClient = rpcClient;
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
                String response1 = rpcClient.syncCall(SimpleAddr.SERVER, new RpcMethodSpec<>(
                        serviceId,
                        helloMethodId,
                        List.of(msg1)
                ));
                assertResponse(msg1, response1);
            }
            {
                String msg2 = "local syncSayHello -- remote helloAsync" + ", time " + timeProvider.getTime();
                String response2 = rpcClient.syncCall(SimpleAddr.SERVER, new RpcMethodSpec<>(
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
                rpcClient.call(SimpleAddr.SERVER, new RpcMethodSpec<String>(
                        serviceId,
                        helloMethodId,
                        List.of(msg1)
                )).thenApply(FunctionUtils.toFunction(response -> assertResponse(msg1, response)));
            }
            {
                String msg2 = "local sayHello -- remote helloAsync" + ", time " + timeProvider.getTime();
                rpcClient.call(SimpleAddr.SERVER, new RpcMethodSpec<String>(
                        serviceId,
                        helloAsyncMethodId,
                        List.of(msg2)
                )).thenApply(FunctionUtils.toFunction(response -> assertResponse(msg2, response)));
            }
        }
    }
    //

    // 模拟双端线程
    private class ServerWorker implements Runnable {

        final TimeProvider timeProvider;
        final SameThreadScheduledExecutor executor;
        final DefaultRpcClient rpcClient;

        private ServerWorker() {
            this.timeProvider = TimeProviders.systemTimeProvider();
            this.executor = SameThreads.newScheduledExecutor(timeProvider);

            SimpleAddr role = SimpleAddr.SERVER;
            rpcClient = new DefaultRpcClient(role.id, role,
                    new TestRpcRouter(), new DefaultRpcRegistry(),
                    timeProvider, 5 * 1000);
//            rpcClient.setRpcLogConfig(RpcLogConfig.ALL_SIMPLE);
        }

        @Override
        public void run() {
            // 注册服务
            RpcRegistry registry = rpcClient.getRegistry();
            TestService testService = new TestService(executor);
            registry.register(serviceId, helloMethodId, (context, methodSpec) -> {
                return testService.hello(methodSpec.getString(0));
            });
            registry.register(serviceId, helloAsyncMethodId, (context, methodSpec) -> {
                return testService.helloAsync(methodSpec.getString(0));
            });

            // 准备好以后执行countdown
            latch.countDown();
            try {
                while (!alert) {
                    RpcProtocol protocol = serverQueue.poll(20, TimeUnit.MILLISECONDS);
                    if (protocol != null) {
                        rpcClient.onRcvProtocol(protocol);
                    }
                    executor.tick();
                    rpcClient.tick();
                }
            } catch (InterruptedException e) {
                // 退出
            }
        }
    }

    private class ClientWorker implements Runnable {

        final TimeProvider timeProvider;
        final SameThreadScheduledExecutor executor;
        final DefaultRpcClient rpcClient;

        private ClientWorker() {
            this.timeProvider = TimeProviders.systemTimeProvider();
            this.executor = SameThreads.newScheduledExecutor(timeProvider);

            SimpleAddr role = SimpleAddr.CLIENT;
            rpcClient = new DefaultRpcClient(role.id, role,
                    new TestRpcRouter(), new DefaultRpcRegistry(),
                    timeProvider, 5 * 1000);
//            rpcClient.setRpcLogConfig(RpcLogConfig.ALL_SIMPLE);
        }

        public boolean checkWatcher(RpcResponse response) {
            return rpcClient.checkWatcher(response);
        }

        @Override
        public void run() {
            TestClient testClient = new TestClient(timeProvider, rpcClient);
            executor.scheduleWithFixedDelay(testClient::sayHello, 200, 300);
            executor.scheduleWithFixedDelay(() -> {
                try {
                    testClient.syncSayHello();
                } catch (InterruptedException e) {
                    ThreadUtils.recoveryInterrupted();
                } catch (RpcClientException e) {
                    if (!alert) {
                        logger.info("client caught exception", e);
                    }
                }
            }, 200, 500);

            // 准备好以后执行countdown
            latch.countDown();
            try {
                while (!alert) {
                    RpcProtocol protocol = clientQueue.poll(20, TimeUnit.MILLISECONDS);
                    if (protocol != null) {
                        rpcClient.onRcvProtocol(protocol);
                    }
                    executor.tick();
                    rpcClient.tick();
                }
            } catch (InterruptedException e) {
                // 退出
            }
        }

    }

    // 模拟路由
    private class TestRpcRouter implements RpcRouter {

        private TestRpcRouter() {
        }

        @Override
        public boolean send(RpcProtocol protocol) {
            SimpleAddr targetRole = (SimpleAddr) protocol.getDestAddr();
            switch (targetRole) {
                case CLIENT -> {
                    if (protocol instanceof RpcResponse response) {
                        return clientWorker.checkWatcher(response) || clientQueue.offer(response);
                    }
                    return clientQueue.offer(protocol);
                }
                case SERVER -> {
                    return serverQueue.offer(protocol);
                }
            }
            return false;
        }

    }

}