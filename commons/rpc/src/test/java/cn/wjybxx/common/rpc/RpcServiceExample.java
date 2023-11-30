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

import cn.wjybxx.common.concurrent.FutureUtils;

import javax.annotation.Nonnull;
import java.util.Arrays;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.concurrent.CompletableFuture;

/**
 * @author wjybxx
 * date 2023/4/12
 */
@RpcService(serviceId = 1)
public class RpcServiceExample implements ExtensibleService {

    private final RpcClient rpcClient;

    public RpcServiceExample(RpcClient rpcClient) {
        this.rpcClient = rpcClient;
    }

    @RpcMethod(methodId = 1, customData = "{interval : 500}")
    public String hello(String msg) {
        return msg;
    }

    /** 测试异步返回 */
    @RpcMethod(methodId = 2)
    public CompletableFuture<String> helloAsync(String msg) {
        return FutureUtils.newSucceededFuture(msg);
    }

    /** 测试void返回值 */
    @RpcMethod(methodId = 3)
    public void hello2(String msg) {
    }

    /** 测试参数基本类型和返回值基本类型 */
    @RpcMethod(methodId = 4)
    public int add(int a, int b) {
        return a + b;
    }

    /** 测试参数带泛型 */
    @RpcMethod(methodId = 5)
    public String join(List<String> args) {
        return args.toString();
    }

    /** 测试返回值带泛型 */
    @RpcMethod(methodId = 6)
    public List<String> split(String value) {
        return Arrays.stream(value.split(",")).toList();
    }

    /** 测试context的代码生成 */
    @RpcMethod(methodId = 7)
    public void contextHello(RpcContext<String> rpcContext, String msg) {
        rpcClient.send(rpcContext.remoteAddr(), RpcClientExampleProxy.onMessage("context -- before"));
        rpcContext.sendResult(msg);
        rpcClient.send(rpcContext.remoteAddr(), RpcClientExampleProxy.onMessage("context -- end\n"));
    }

    /** 测试context的代码生成 */
    @RpcMethod(methodId = 8)
    public String requestHello(RpcGenericContext ctx, String msg) {
        return msg;
    }

    /** 测试不可变 */
    @RpcMethod(methodId = 11)
    public Long box_add(Integer a, Long b) {
        if (a == null && b == null) return null;
        if (a == null) a = 0;
        if (b == null) b = 0L;
        return a.longValue() + b;
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
}