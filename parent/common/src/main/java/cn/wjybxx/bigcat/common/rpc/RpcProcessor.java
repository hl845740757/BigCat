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

package cn.wjybxx.bigcat.common.rpc;

import cn.wjybxx.bigcat.common.async.FluentFuture;
import cn.wjybxx.bigcat.common.async.SameThreads;

import javax.annotation.concurrent.NotThreadSafe;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.Executor;

/**
 * 用于rpc服务端处理请求。
 *
 * @author wjybxx
 * date 2023/4/1
 */
@NotThreadSafe
public interface RpcProcessor {

    /**
     * 处理一个rpc请求
     * <p>
     * 注意：接口默认只约定支持{@link FluentFuture}以确保逻辑在当前线程执行。
     * 如果你想支持其它类型的Future，比如JDK的{@link CompletableFuture}和Netty的Future，请在上层进行转换，同时确保回调执行在当前线程。
     * 工具方法：{@link SameThreads#fromJDKFuture(CompletableFuture, Executor)}
     *
     * @param request rpc请求信息
     * @return 方法执行结果，可能情况：1.null 2.{@link FluentFuture} 3.其它结果
     * @throws Exception 异常情况请抛出异常，暂不支持使用特殊的返回值表达失败。
     */
    Object process(RpcRequest request) throws Exception;

}