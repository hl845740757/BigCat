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

import cn.wjybxx.base.annotation.StableName;
import cn.wjybxx.concurrent.IFuture;

import java.util.concurrent.CompletableFuture;

/**
 * rpc执行时的上下文接口。
 * <p>
 * ps: 由于我们会将RpcContext对象暴露给用户，以及监听Future的结果，因此RpcContext无法简单池化；
 * 由于Java端并不支持值类型，因此我们保留抽象，以方便未来扩展。
 *
 * @author wjybxx
 * date 2023/4/1
 */
public interface RpcContext<V> {

    /**
     * 连接id
     * 1.服务器与客户端通信时使用该字段
     * 2.可用于在返回结果前后向目标发送额外的消息
     */
    long conId();

    /**
     * 远端地址
     * 1.服务器之间通信时使用该字段
     * 2.可用于在返回结果前后向目标发送额外的消息 -- 它对应的是{@link RpcRequest#srcAddr}
     */
    WorkerAddr remoteAddr();

    // region config

    /** 当前返回值是否可共享 */
    boolean isSharable();

    /** 设置返回值是否可共享标记 */
    @StableName
    void setSharable(boolean sharable);

    /** 是否用户手动返回结果 */
    boolean isManualReturn();

    /** 设置是否用户手动返回结果 */
    @StableName
    void setManualReturn(boolean value);

    // endregion

    // region result

    /**
     * 发送正确结果
     * 2.可通过{@link #setSharable(boolean)}设置结果是否可共享
     */
    void sendResult(V result);

    /**
     * 发送已编码的正确结果，避免中途解码
     * 1.基于protobuf通信时，即为protobuf消息的序列化结果 -- 跨语言时限定pb通信
     * 2.非pb通信时，需要等同{@link RpcSerializer}的序列化结果
     * 3.可通过{@link #setSharable(boolean)}设置结果是否可共享
     *
     * @param result 编码后的结果，不可为null
     */
    void sendResult(byte[] result);

    /** 发送错误结果 */
    void sendError(int errorCode, String msg);

    /** 发送错误结果 */
    void sendError(Throwable ex);

    /** 发送异步结果 */
    void sendAsyncResult(IFuture<V> future);

    /** 发送异步结果 */
    void sendAsyncResult(CompletableFuture<V> future);

    // endregion
}