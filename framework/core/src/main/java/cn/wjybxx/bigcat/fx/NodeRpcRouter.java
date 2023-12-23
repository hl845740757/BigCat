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
 * 1.该接口主要用于支持自定义地址解析；查询地址特性的方法可能多线程访问，需要保证【线程安全】。
 * 2.在收到请求时应当调用{@link NodeRpcSupport#onRcvRequest(RpcRequest)}
 * 3.在收到响应时应当调用{@link NodeRpcSupport#onRcvResponse(RpcResponse)}
 * 4.Router和{@link NodeRpcSupport}都是Node上的模块，需要双向绑定。
 *
 * @author wjybxx
 * date - 2023/10/28
 */
public interface NodeRpcRouter extends RpcRouter, WorkerModule {

    /**
     * 1.该方法在Node线程调用
     * 2.如果方法参数或结果是不可共享的，则已在Worker线程序列化；否则由Router决定序列化时机。
     * 3.可通过{@link RpcProtocol#isSerialized()}判断是否已序列化。
     */
    @Override
    boolean send(RpcProtocol protocol);

    /**
     * 测试给定的地址是否是跨语言的rpc节点
     * 1.如果是跨语言的节点通信，方法参数和结果必须是protobuf的消息
     * 2.通常用于客户端和服务器的rpc通信
     */
    boolean isCrossLanguageAddr(RpcAddr addr);

    /**
     * 测试给定地址似乎否是本地地址(进程内地址)
     * 1.当一个进程内有多个Node的时候，需要Router来处理。
     * 2.通常用于判断数据是否可共享 -- ‘本地单播’时可直接传递原始对象。
     * 3.用于本地服务调用优化，避免不必要的序列化和拷贝 -- 比如：调用本地的DB服务，Http服务。
     */
    boolean isLocalAddr(WorkerAddr addr);

    /** 测试给定的地址是否是单播地址，只有每一级都是单播的情况下才可以返回true */
    default boolean isUnicastAddr(WorkerAddr addr) {
        return addr.serverType > 0
                && addr.serverId > 0
                && !("*".equals(addr.workerId));
    }

    /** 测试给定的地址在worker层是否是单播地址 */
    default boolean isUnicastWorkerAddr(WorkerAddr addr) {
        return !("*".equals(addr.workerId));
    }

    /** 测试给定的地址在worker层是否是广播地址 */
    default boolean isBroadcastWorkerAddr(WorkerAddr addr) {
        return "*".equals(addr.workerId);
    }

    //region util

    /** 判断是否是单播地址 */
    default boolean isUnicastAddr(RpcAddr addr) {
        if (addr instanceof WorkerAddr workerAddr) {
            return isUnicastAddr(workerAddr);
        }
        return addr == StaticRpcAddr.LOCAL;
    }

    /** 测试给定的地址在worker层是否是单播地址 */
    default boolean isUnicastWorkerAddr(RpcAddr addr) {
        if (addr instanceof WorkerAddr workerAddr) {
            return isUnicastWorkerAddr(addr);
        }
        return addr == StaticRpcAddr.LOCAL;
    }

    /** 测试给定的地址在worker层是否是广播地址 */
    default boolean isBroadcastWorkerAddr(RpcAddr addr) {
        if (addr instanceof WorkerAddr workerAddr) {
            return isBroadcastWorkerAddr(workerAddr);
        }
        return addr == StaticRpcAddr.LOCAL_BROADCAST;
    }

    /** 测试给定地址似乎否是本地地址(进程内地址) */
    default boolean isLocalAddr(RpcAddr addr) {
        if (addr instanceof WorkerAddr workerAddr) {
            return isLocalAddr(workerAddr);
        }
        return addr instanceof StaticRpcAddr;
    }
    // endregion

}