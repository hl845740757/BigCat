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

import java.util.List;

/**
 * rpc执行时的上下文接口。
 * 1. 该接口提供了返回结果的方法。
 * 2. 当Rpc方法的第一个参数为该接口时，由用户自行控制结果的返回时机。
 *
 * @author wjybxx
 * date 2023/4/1
 */
public interface RpcContext<V> extends RpcGenericContext {

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
     * @param sharable 是否允许共享
     */
    void sendEncodedResult(byte[] result, boolean sharable);
}