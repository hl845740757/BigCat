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

/**
 * rpc执行时的上下文。
 * 定义该接口，方便扩展，比如添加接收到请求时的时间等信息。
 *
 * @author wjybxx
 * date 2023/4/1
 */
public interface RpcContext<V> {

    /**
     * @return 返回调用的详细信息
     */
    RpcRequest request();

    /** 发送正确结果 */
    void sendResult(V msg);

    /** 发送错误结果 */
    void sendError(int errorCode, String msg);

    /**
     * 远端地址
     * 可用于在返回结果前后向目标发送额外的消息
     */
    RpcAddr remoteAddr();

    /**
     * 本次调用远端是否需要结果
     */
    boolean isRequireResult();

}