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

import cn.wjybxx.common.ThreadUtils;
import cn.wjybxx.common.async.SameThreadScheduledExecutor;
import cn.wjybxx.common.async.SameThreads;
import cn.wjybxx.common.time.TimeProvider;
import cn.wjybxx.common.time.TimeProviders;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.concurrent.BlockingQueue;
import java.util.concurrent.CountDownLatch;
import java.util.concurrent.LinkedBlockingQueue;
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

    private BlockingQueue<RpcProtocol> serverQueue;
    private BlockingQueue<RpcProtocol> clientQueue;
    private ServerWorker serverWorker;
    private ClientWorker clientWorker;
    private Thread serverThread;
    private Thread clientThread;

    private final AtomicInteger counter = new AtomicInteger();
    private CountDownLatch latch = new CountDownLatch(2);
    private volatile boolean alert;

    void test(int mode) throws InterruptedException {
        serverQueue = new LinkedBlockingQueue<>();
        clientQueue = new LinkedBlockingQueue<>();

        serverThread = new Thread((serverWorker = new ServerWorker()));
        clientThread = new Thread((clientWorker = new ClientWorker(mode)));
        counter.set(0);
        latch = new CountDownLatch(2);
        alert = false;

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
        test(0);
    }

    @Test
    void contextTest() throws InterruptedException {
        test(1);
    }

    // 模拟双端线程
    private class ServerWorker implements Runnable {

        final TimeProvider timeProvider;
        final SameThreadScheduledExecutor executor;
        final DefaultRpcClient rpcClient;

        private ServerWorker() {
            this.timeProvider = TimeProviders.systemTimeProvider();
            this.executor = SameThreads.newScheduledExecutor(timeProvider);

            SimpleAddr role = SimpleAddr.SERVER;
            RpcRegistry registry = new DefaultRpcRegistry();
            rpcClient = new DefaultRpcClient(role.id, role,
                    new TestRpcRouter(), registry,
                    timeProvider, 5 * 1000);
//            rpcClient.setRpcLogConfig(RpcLogConfig.ALL_SIMPLE);
        }

        @Override
        public void run() {
            // 注册服务
            RpcRegistry registry = rpcClient.getRegistry();
            RpcServiceExampleExporter.export(registry, new RpcServiceExample(rpcClient));

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
        final int mode;

        final DefaultRpcClient rpcClient;

        private ClientWorker(int mode) {
            this.timeProvider = TimeProviders.systemTimeProvider();
            this.executor = SameThreads.newScheduledExecutor(timeProvider);
            this.mode = mode;

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
            // 注册服务
            RpcRegistry registry = rpcClient.getRegistry();
            RpcClientExampleExporter.export(registry, new RpcClientExample(rpcClient));

            RpcClientExample rpcUserExample = new RpcClientExample(rpcClient);
            executor.scheduleWithFixedDelay(() -> rpcTest(rpcUserExample), 200, 500);

            // 准备好以后执行countdown
            latch.countDown();
            try {
                while (!alert) {
                    RpcProtocol protocol = clientQueue.poll(10, TimeUnit.MILLISECONDS);
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

        private void rpcTest(RpcClientExample rpcUserExample) {
            try {
                if (mode == 1) {
                    rpcUserExample.contextTest();
                } else {
                    rpcUserExample.test();
                }
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