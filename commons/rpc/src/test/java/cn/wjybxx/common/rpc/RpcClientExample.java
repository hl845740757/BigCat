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

import javax.annotation.Nonnull;
import java.util.HashMap;
import java.util.Map;

/**
 * @author wjybxx
 * date 2023/4/12
 */
@RpcService(serviceId = 2)
public class RpcClientExample implements ExtensibleService {

    private final RpcClient rpcClient;

    public RpcClientExample(RpcClient rpcClient) {
        this.rpcClient = rpcClient;
    }

    @RpcMethod(methodId = 1)
    public void onMessage(String msg) {
        System.out.println(msg);
    }

    // 测试从接口继承的方法
    private final Map<String, Object> extBlackboard = new HashMap<>();

    @Nonnull
    @Override
    public Map<String, Object> getExtBlackboard() {
        return extBlackboard;
    }

    @Override
    public Object execute(@Nonnull String cmd, Object params) throws Exception {
        return null;
    }

    public void test() throws Exception {
        RpcClient rpcClient = this.rpcClient;
        rpcClient.send(SimpleAddr.SERVER, RpcServiceExampleProxy.hello("这是一个通知，不接收结果"));
        rpcClient.call(SimpleAddr.SERVER, RpcServiceExampleProxy.hello("这是一个异步调用，可监听结果"))
                .thenApply(result -> {
                    System.out.println(result);
                    return null;
                });
        String result = rpcClient.syncCall(SimpleAddr.SERVER,
                RpcServiceExampleProxy.helloAsync("这是一个同步调用，远程异步执行"));
        System.out.println(result);
    }

    public void contextTest() {
        RpcClient rpcClient = this.rpcClient;
        rpcClient.send(SimpleAddr.SERVER, RpcServiceExampleProxy.contextHello("这是一个通知，目标函数有Context"));
        rpcClient.call(SimpleAddr.SERVER, RpcServiceExampleProxy.contextHello("这是一个异步调用，目标函数有Context"))
                .thenApply(result -> {
                    System.out.println(result);
                    return null;
                });
    }
}