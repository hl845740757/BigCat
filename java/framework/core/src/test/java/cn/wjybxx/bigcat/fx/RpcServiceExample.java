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

import cn.wjybxx.concurrent.EventLoopModule;
import cn.wjybxx.concurrent.ExecutorUtils;
import cn.wjybxx.concurrent.IFuture;
import com.google.inject.Inject;

import javax.annotation.Nonnull;
import java.util.HashMap;
import java.util.Map;

/**
 * @author wjybxx
 * date 2023/4/12
 */
@RpcService(serviceId = 11)
public class RpcServiceExample extends EventLoopModule implements ExtensibleService {

    @Inject
    private RpcClient rpcClient;

    @RpcMethod(methodId = 1, argSharable = true, resultSharable = true)
    public Response echo(Request request) {
        return new Response()
                .setString(request.getString1());
    }

    @RpcMethod(methodId = 2, argSharable = true, resultSharable = true, customData = "{interval : 500}")
    public Response hello(Request request) {
        return new Response()
                .setString(request.getString1());
    }

    /** 测试异步返回 */
    @RpcMethod(methodId = 3, argSharable = true, resultSharable = true)
    public IFuture<Response> helloAsync(Request request) {
        Response response = new Response()
                .setString(request.getString1());
        return ExecutorUtils.completedFuture(response);
    }

    /** 测试void返回值 */
    @RpcMethod(methodId = 4, argSharable = true, resultSharable = true)
    public void hello2(Request request) {
    }

    /** 测试参数带泛型 */
    @RpcMethod(methodId = 5, argSharable = true, resultSharable = true)
    public Response join(Request request) {
        String result = String.join(",", request.getStringList());
        return new Response()
                .setString(result);
    }

    /** 测试context的代码生成 -- 参数和结果都不设置为可共享的，测试反序列化 */
    @RpcMethod(methodId = 6)
    public void contextHello(RpcContext<Response> rpcContext, Request request) {
        rpcClient.send(rpcContext.remoteAddr(), RpcClientExampleProxy.onMessage(Request.ofString("context -- before")));
        {
            Response response = new Response()
                    .setString(request.getString1());
            rpcContext.sendResult(response);
        }
        rpcClient.send(rpcContext.remoteAddr(), RpcClientExampleProxy.onMessage(Request.ofString("context -- end\n")));
    }

    /** 测试context的代码生成 */
    @RpcMethod(methodId = 7, argSharable = true, resultSharable = true)
    public Response requestHello(RpcContext<Response> rpcContext, Request request) {
        return new Response()
                .setString(request.getString1());
    }

    // 测试从接口继承的方法
    private final Map<String, Object> extBlackboard = new HashMap<>();

    @Nonnull
    @Override
    public Map<String, Object> getExtBlackboard() {
        return extBlackboard;
    }

    @Override
    public ExecuteResult execute(ExecuteRequest request) {
        return null;
    }
}