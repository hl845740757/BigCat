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

package cn.wjybxx.common;

import cn.wjybxx.common.async.FluentFuture;
import cn.wjybxx.common.async.SameThreads;
import cn.wjybxx.common.concurrent.FutureUtils;
import cn.wjybxx.common.rpc.ExtensibleService;
import cn.wjybxx.common.rpc.RpcMethod;
import cn.wjybxx.common.rpc.RpcService;

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

    @RpcMethod(methodId = 1)
    public String hello(String msg) {
        return msg;
    }

    /** 测试void返回值 */
    @RpcMethod(methodId = 2)
    public void hello2(String msg) {
    }

    /** 测试参数基本类型和返回值基本类型 */
    @RpcMethod(methodId = 3)
    public int add(int a, int b) {
        return a + b;
    }

    /** 测试参数带泛型 */
    @RpcMethod(methodId = 4)
    public String join(List<String> args) {
        return args.toString();
    }

    /** 测试返回值带泛型 */
    @RpcMethod(methodId = 5)
    public List<String> split(String value) {
        return Arrays.stream(value.split(",")).toList();
    }

    /** 测试异步返回 -- JDK的Future */
    @RpcMethod(methodId = 6)
    public CompletableFuture<String> helloAsync(String msg) {
        return FutureUtils.newSucceededFuture(msg);
    }

    /** 测试异步返回 -- 单线程Future */
    @RpcMethod(methodId = 7)
    public FluentFuture<String> helloAsync2(String msg) {
        return SameThreads.newSucceededFuture(msg);
    }

    // 测试从接口继承的方法
    private Map<String, Object> extBlackboard = new HashMap<>();

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