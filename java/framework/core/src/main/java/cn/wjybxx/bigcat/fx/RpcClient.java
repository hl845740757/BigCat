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


import cn.wjybxx.base.ex.ErrorCodeException;
import cn.wjybxx.concurrent.IFuture;

import javax.annotation.concurrent.NotThreadSafe;
import java.util.concurrent.TimeoutException;

/**
 * Rpc客户端。
 * RpcClient是{@code SessionMgr}的门面，提供更易于使用的API。
 *
 * <h3>实现要求</h3>
 * 1.单向消息(send系列方法)：无论执行成功还是失败，实现必须忽略调用的方法的执行结果(最好不回传结果，而不是仅仅不上报给调用者)。
 * 2.Rpc调用(call系列方法)：如果调用的方法执行成功，则返回对应的结果。如果方法本身没有返回值，则返回null。如果执行失败，则应该返回对应的异常信息（可以是简单信息）。
 * 3.{@code send} {@code call}之间必须满足先发送的先到。<br>
 * 4.如果架构是单线程的，且消息队列是有界的，{@code syncCall}系列方法要小心死锁问题。
 * 5.参数合法的情况下，不要抛出{@link RpcException}以外的异常。
 * 6.如果无法执行请求，则应该返回一个已失败的{@link IFuture}，且其异常是约定好的。
 * 7.{@link RpcMethodSpec}是临时参数对象，不可保留引用。
 *
 * <h3>使用者注意</h3>
 * 1.虽然要求了所有的消息都先发先到。但是先发送的请求不一定先获得结果！对方什么时候返回给你结果是不确定的！
 * 2.同步调用会导致后到的结果被提前处理，因此打乱了时序，请务必清楚。
 * 3.和玩家通信时建议使用定制的Client实现(不应该提供同步调用接口，目标地址使用conId)
 *
 * @author wjybxx
 * date 2023/4/1
 */
@NotThreadSafe
public interface RpcClient {

    /**
     * 发起一个rpc调用，但不接收结果。
     *
     * @param destAddr   目标地址
     * @param methodSpec 要调用的方法信息
     */
    void send(WorkerAddr destAddr, RpcMethodSpec<?> methodSpec);

    /**
     * 发起一个rpc调用，可以监听调用结果。
     * 注意：
     * 1.禁止在call返回的Future上进行阻塞调用。
     * 2.可能立即成功或失败，用户可显式测试返回的Future是否已完成。
     *
     * @param destAddr   目标地址
     * @param methodSpec 要调用的方法信息
     * @return future，可以监听调用结果
     */
    <V> IFuture<V> call(WorkerAddr destAddr, RpcMethodSpec<V> methodSpec);

    /**
     * 发起一个rpc调用，可以监听调用结果。
     * 注意：
     * 1.禁止在call返回的Future上进行阻塞调用。
     * 2.可能立即成功或失败，用户可显式测试返回的Future是否已完成。
     *
     * @param destAddr   目标地址
     * @param methodSpec 要调用的方法信息
     * @param timeoutMs  超时时间
     * @return future，可以监听调用结果
     */
    <V> IFuture<V> call(WorkerAddr destAddr, RpcMethodSpec<V> methodSpec, long timeoutMs);

    /**
     * 执行一个同步rpc调用，当前线程会阻塞到结果返回 -- 使用默认的超时时间。
     *
     * @param destAddr   目标地址
     * @param methodSpec 要调用的方法信息
     * @return 方法返回值
     * @throws TimeoutException     等待超时
     * @throws InterruptedException 线程被中断
     * @throws ErrorCodeException   逻辑层异常
     * @throws RpcException         Rpc异常
     */
    <V> V syncCall(WorkerAddr destAddr, RpcMethodSpec<V> methodSpec) throws TimeoutException, InterruptedException;

    /**
     * 执行一个同步rpc调用，当前线程会阻塞到结果返回。
     *
     * @param destAddr   远程地址
     * @param methodSpec 要调用的方法信息
     * @param timeoutMs  超时时间，毫秒
     * @return 执行结果
     * @throws TimeoutException     等待超时
     * @throws InterruptedException 线程被中断
     * @throws ErrorCodeException   逻辑层异常
     * @throws RpcException         Rpc异常
     */
    <V> V syncCall(WorkerAddr destAddr, RpcMethodSpec<V> methodSpec, long timeoutMs) throws TimeoutException, InterruptedException;

}