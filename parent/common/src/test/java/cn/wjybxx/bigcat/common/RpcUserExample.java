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

import cn.wjybxx.bigcat.common.rpc.RpcClient;

/**
 * @author wjybxx
 * date 2023/4/13
 */
public class RpcUserExample {

    private final RpcClient rpcClient;

    public RpcUserExample(RpcClient rpcClient) {
        this.rpcClient = rpcClient;
    }

    public RpcClient getRpcClient() {
        return rpcClient;
    }

    public void rpcTest() throws Exception {
        RpcClient rpcClient = getRpcClient();
        rpcClient.send(SimpleNodeSpec.SERVER, RpcServiceExampleProxy.hello("这是一个通知，不接收结果"));
        rpcClient.call(SimpleNodeSpec.SERVER, RpcServiceExampleProxy.hello("这是一个异步调用，可监听结果"))
                .thenApply(result -> {
                    System.out.println(result);
                    return null;
                });

        String result = rpcClient.syncCall(SimpleNodeSpec.SERVER,
                RpcServiceExampleProxy.helloAsync2("这是一个同步调用，远程异步执行"));
        System.out.println(result);
    }

}