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


import cn.wjybxx.common.concurrent.ICompletableFuture;

import javax.annotation.concurrent.NotThreadSafe;

/**
 * Rpc客户端。
 * 各个模块绑定具体的实现类到该接口；当然，也可以提供更具体的方法。
 *
 * <h3>实现要求</h3>
 * 1. 单向消息(send系列方法)：无论执行成功还是失败，实现必须忽略调用的方法的执行结果(最好不回传结果，而不是仅仅不上报给调用者)。
 * 2. Rpc调用(call系列方法)：如果调用的方法执行成功，则返回对应的结果。如果方法本身没有返回值，则返回null。如果执行失败，则应该返回对应的异常信息（可以是简单信息）。
 * 3. {@code send} {@code call}之间必须满足先发送的先到。<br>
 * 4. 如果架构是单线程的，且消息队列是有界的，{@code syncCall}系列方法要小心死锁问题。
 * 5. 参数合法的情况下，不要抛出{@link RpcException}以外的异常。
 * 6. 如果无法执行请求，则应该返回一个已失败的{@link ICompletableFuture}，且其异常是约定好的。
 *
 * <h3>使用者注意</h3>
 * 1.虽然要求了所有的消息都先发先到。但是先发送的请求不一定先获得结果！对方什么时候返回给你结果是不确定的！
 * 2.同步调用会导致后到的结果被提前处理，因此打乱了时序，请务必清楚。
 *
 * <h3>API设计</h3>
 * 这里的rpc设计并未追求所谓的标准，而更追求实用性。
 * 在传统的RPC中，前后端接口是一致的，我觉得强行屏蔽远程和本地的差异并不好，这会限制使用者的灵活度(就像JAVA语言本身)；
 * 当我们明确告诉用户这就是一个RPC时（其实大家都知道），用户就可以决定如何处理结果，既可以忽略结果，也可以异步执行，也可以同步执行，因此注解生成的Proxy仅仅用于封装参数，而不执行。
 * 至于服务器信息参数(RpcAddr)，则用于用户决定要发送给谁，因为单纯根据服务id定位是不够的，我们经常需要精确的消息投递。
 * 我想过在以后通过protobuf定义rpc服务(修改grpc的语法)，用于客户端和服务器通信，但仍然会保持我们现在的设计，客户端和服务器的接口就是会不一样，Proxy仅用于打包。
 *
 * @author wjybxx
 * date 2023/4/1
 */
@NotThreadSafe
public interface RpcClient {

    /**
     * 发起一个rpc调用，但不接收结果。
     *
     * @param target     远程地址
     * @param methodSpec 要调用的方法信息
     */
    void send(RpcAddr target, RpcMethodSpec<?> methodSpec);

    /**
     * 发起一个rpc调用，可以监听调用结果。
     * 注意：通常禁止在call返回的Future上进行阻塞调用。
     *
     * @param target     远程地址
     * @param methodSpec 要调用的方法信息
     * @return future，可以监听调用结果
     */
    <V> ICompletableFuture<V> call(RpcAddr target, RpcMethodSpec<V> methodSpec);

    /**
     * 执行一个同步rpc调用，当前线程会阻塞到结果返回 -- 使用默认的超时时间。
     *
     * @param target     远程地址
     * @param methodSpec 要调用的方法信息
     * @return 方法返回值
     * @throws RpcException 执行错误时抛出异常
     */
    <V> V syncCall(RpcAddr target, RpcMethodSpec<V> methodSpec);

    /**
     * 执行一个同步rpc调用，当前线程会阻塞到结果返回。
     *
     * @param target     远程地址
     * @param methodSpec 要调用的方法信息
     * @param timeoutMs  超时时间，毫秒
     * @return 执行结果
     * @throws RpcException 执行错误时抛出异常
     */
    <V> V syncCall(RpcAddr target, RpcMethodSpec<V> methodSpec, long timeoutMs);

}