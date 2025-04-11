/*
 * Copyright 2023-2025 wjybxx(845740757@qq.com)
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

import cn.wjybxx.concurrent.IFuture;

import java.util.concurrent.CompletableFuture;

/**
 * 1.该接口和{@link RpcClient}分离，属于关注点分离。
 * 2.这里的接口由{@link RpcContext}调用，
 * 3.我们这里不再封装额外的方法对象来传输参数，因为用户基本不会手动调用这里的方法。
 * 4.如果返回结果的线程可能不是当前Worker，要小心多线程问题。
 *
 * @author wjybxx
 * date - 2025/4/7
 */
public interface RpcClientImpl extends RpcClient {

    // region result

    /**
     * 发送正确执行的结果
     *
     * @param sessionId 会话id
     * @param destAddr  目标地址
     * @param requestId 请求id
     * @param serviceId 服务id
     * @param methodId  方法id
     * @param result    结果
     * @param sharable  结果对象是否可共享
     */
    void sendResult(long sessionId, WorkerAddr destAddr,
                    long requestId, int serviceId, int methodId,
                    Object result, boolean sharable);

    /**
     * 发送异常执行的结果
     *
     * @param sessionId 会话id
     * @param destAddr  目标地址
     * @param requestId 请求id
     * @param serviceId 服务id
     * @param methodId  方法id
     * @param errorCode 错误码
     * @param msg       错误消息
     */
    void sendError(long sessionId, WorkerAddr destAddr,
                   long requestId, int serviceId, int methodId,
                   int errorCode, String msg);

    /**
     * 发送异常执行的结果
     *
     * @param sessionId 会话id
     * @param destAddr  目标地址
     * @param requestId 请求id
     * @param serviceId 服务id
     * @param methodId  方法id
     * @param ex        异常信息
     */
    void sendError(long sessionId, WorkerAddr destAddr,
                   long requestId, int serviceId, int methodId,
                   Throwable ex);

    /**
     * 发送异步结果
     *
     * @param sessionId 会话id
     * @param destAddr  目标地址
     * @param requestId 请求id
     * @param serviceId 服务id
     * @param methodId  方法id
     * @param future    异步结果
     * @param sharable  结果是否可共享
     */
    <V> void sendAsyncResult(long sessionId, WorkerAddr destAddr,
                             long requestId, int serviceId, int methodId,
                             IFuture<V> future, boolean sharable);

    /**
     * 发送异步结果
     *
     * @param sessionId 会话id
     * @param destAddr  目标地址
     * @param requestId 请求id
     * @param serviceId 服务id
     * @param methodId  方法id
     * @param future    异步结果
     * @param sharable  结果是否可共享
     */
    <V> void sendAsyncResult(long sessionId, WorkerAddr destAddr,
                             long requestId, int serviceId, int methodId,
                             CompletableFuture<V> future, boolean sharable);

    // endregion
}