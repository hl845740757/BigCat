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

package cn.wjybxx.bigcat.fx;

import cn.wjybxx.common.rpc.*;

/**
 * Node线程发送Rpc协议的实现
 * 1.该接口主要用于支持自定义地址解析
 * 2.在收到请求时应当调用{@link NodeRpcSupport#onRcvRequest(RpcRequest)}
 * 3.在收到响应时应当调用{@link NodeRpcSupport#onRcvResponse(RpcResponse)}
 *
 * @author wjybxx
 * date - 2023/10/28
 */
public interface NodeRpcSender extends RpcSender {

    /** 该方法一定在Node线程调用 */
    @Override
    boolean send(RpcProtocol proto);

    /**
     * 测试给定的地址是否是单播地址，只有每一级都是单播的情况下才可以返回true
     */
    default boolean isUnicastAddr(WorkerAddr addr) {
        return addr.serverType > 0
                && addr.serverId > 0
                && isUnicastWorkerAddr(addr);
    }

    /**
     * 测试给定的地址在worker层是否是单播地址
     */
    default boolean isUnicastWorkerAddr(WorkerAddr workerAddr) {
        return !("*".equals(workerAddr.workerId));
    }

    /**
     * 测试给定的地址在worker层是否是广播地址
     */
    default boolean isBroadcastWorkerAddr(WorkerAddr workerAddr) {
        return "*".equals(workerAddr.workerId);
    }

    /**
     * 测试给定的地址是否是跨语言的rpc节点
     * 1.如果是跨语言的节点通信，方法参数和结果必须是protobuf的消息
     * 2.通常用于客户端和服务器的rpc通信
     */
    default boolean isCrossLanguageAddr(RpcAddr addr) {
        return false;
    }

}