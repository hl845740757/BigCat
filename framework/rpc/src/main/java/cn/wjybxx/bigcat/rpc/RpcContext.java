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

package cn.wjybxx.bigcat.rpc;

import cn.wjybxx.base.annotation.StableName;

import java.util.List;

/**
 * rpc执行时的上下文接口。
 * <p>
 * ps: 不能直接通过{@link RpcContext}发送结果，否则可能导致用户的封装失效，
 * 需要走统一出口{@link RpcClient}发包。
 *
 * @author wjybxx
 * date 2023/4/1
 */
public interface RpcContext<V> {

    /**
     * @return 返回调用的详细信息
     */
    RpcRequest request();

    /**
     * 远端地址
     * 1.可用于在返回结果前后向目标发送额外的消息 -- 它对应的是{@link RpcRequest#srcAddr}
     * 2.本地进行模拟时，可以赋值{@link #localAddr()}
     */
    default RpcAddr remoteAddr() {
        return request().srcAddr;
    }

    /**
     * 本地地址
     * 可用于校验 -- 对应{@link RpcRequest#destAddr}
     */
    default RpcAddr localAddr() {
        return request().destAddr;
    }

    // region config

    /** 当前返回值是否可共享 */
    boolean isSharable();

    /** 设置返回值是否可共享标记 -- 不论是否托管返回时机，都可以设置 */
    @StableName
    void setSharable(boolean sharable);

    /** 是否用户手动返回结果 */
    boolean isManualReturn();

    /** 设置是否用户手动返回结果 */
    @StableName
    void setManualReturn(boolean value);

    // endregion

    // region result

    /** 发送正确结果 */
    void sendResult(V result);

    /** 发送错误结果 */
    void sendError(int errorCode, String msg);

    /** 发送错误结果 */
    void sendError(Throwable ex);

    /**
     * 发送已编码的正确结果，避免中途解码
     * 1.基于protobuf通信时，即为protobuf消息的序列化结果
     * 2.非pb通信时，应当使用{@link List}封装一层，再使用{@link RpcSerializer}序列化，否则无法还原
     * 3.由于类型的不同，需要独立指定是否可共享
     *
     * @param result   编码后的结果，不可为null
     * @param sharable 是否允许共享 -- result是否为只读
     */
    void sendEncodedResult(byte[] result, boolean sharable);

    // endregion

    // region 常量

    /** 返回值可共享 */
    int MASK_RESULT_SHARABLE = 1;
    /** 手动返回结果 */
    int MASK_RESULT_MANUAL = 1 << 1;

    // endregion
}