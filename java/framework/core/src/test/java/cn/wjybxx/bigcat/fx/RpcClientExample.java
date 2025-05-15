/*
 * Copyright 2023-2024 wjybxx(845740757@qq.com)
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

import cn.wjybxx.base.MathCommon;
import cn.wjybxx.base.time.Regulator;
import cn.wjybxx.base.time.TimeHelper;
import cn.wjybxx.concurrent.EventLoopModule;
import com.google.inject.Inject;
import org.junit.jupiter.api.Assertions;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import javax.annotation.Nonnull;
import java.util.HashMap;
import java.util.Map;
import java.util.concurrent.TimeoutException;

/**
 * @author wjybxx
 * date 2023/4/12
 */
@RpcService(serviceId = 12)
public class RpcClientExample extends EventLoopModule implements ExtensibleService {

    private static final Logger logger = LoggerFactory.getLogger(RpcClientExample.class);

    /** worker */
    private Worker worker;
    /** 定时器 */
    private final Regulator regulator = Regulator.newFixedDelay(1, 50);

    @Inject
    private RpcClient rpcClient;
    @Inject
    private TimeModule timeModule;
    // 测试从接口继承的方法
    private final Map<String, Object> extBlackboard = new HashMap<>();
    // 目标地址 -- 本地
    private WorkerAddr serverAddr;

    public RpcClientExample() {
    }

    @RpcMethod(methodId = 1)
    public void onMessage(Request request) {
        System.out.println(request.getString1());
    }

    @Nonnull
    @Override
    public Map<String, Object> getExtBlackboard() {
        return extBlackboard;
    }

    @Override
    public ExecuteResult execute(ExecuteRequest request) {
        return new ExecuteResult();
    }

    // region logic

    @Override
    public void resolveDependence() {
        this.worker = (Worker) getEntity();
        this.serverAddr = worker.node().nodeAddr();
    }

    @Override
    public void start() {
        regulator.restart(timeModule.getTime());
    }

    @Override
    public void stop() {
        System.out.println("triggerCount: " + regulator.getCount());
    }

    @Override
    public void update() throws TimeoutException {
        if (!regulator.isReady(timeModule.getTime())) {
            return;
        }
        int seed = MathCommon.SHARED_RANDOM.nextInt(4);
        switch (seed) {
            case 0 -> testOneway();
            case 1 -> testAsyncCall();
            case 2 -> testSyncCall();
            case 3 -> testContext();
        }
    }

    // endregion

    private void testOneway() {
        String msg = createMessage("这是一个通知，不接收结果");
        rpcClient.send(serverAddr, RpcServiceExampleProxy.hello(Request.ofString(msg)));
    }

    private void testAsyncCall() {
        String msg = createMessage("这是一个异步调用，可监听结果");
        rpcClient.call(serverAddr, RpcServiceExampleProxy.hello(Request.ofString(msg)))
                .thenApply((ctx, result) -> {
                    // 启用本地共享的情况下应当是同一个字符串
                    Assertions.assertSame(msg, result.getString());
                    Assertions.assertTrue(worker.inEventLoop(), "worker.inEventLoop");
                    System.out.println("callResult: " + result.getString());
                    System.out.println();
                    return null;
                });
    }

    private void testSyncCall() {
        try {
            String msg = createMessage("这是一个同步调用，远程异步执行");
            Response result = rpcClient.syncCall(serverAddr, RpcServiceExampleProxy.helloAsync(Request.ofString(msg)));
            System.out.println("syncResult: " + result.getString());
            System.out.println();
        } catch (InterruptedException ignore) {
            logger.info("syncCall interrupted");
        } catch (TimeoutException ex) {
            logger.info("syncCall timeout", ex);
        }
    }

    private void testContext() {
        String msg = createMessage("这是一个异步调用，目标函数有Context");
        rpcClient.call(serverAddr, RpcServiceExampleProxy.contextHello(Request.ofString(msg)))
                .thenApply((ctx, result) -> {
                    System.out.println(result.getString());
                    return null;
                });
    }

    private String createMessage(String msg) {
        String timeString = TimeHelper.SYSTEM.formatTime(regulator.getLastUpdateTime());
        return "time: " + timeString + " # " + msg;
    }
}