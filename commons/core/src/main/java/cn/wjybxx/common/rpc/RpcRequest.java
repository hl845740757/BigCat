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


import cn.wjybxx.common.dson.AutoTypeArgs;
import cn.wjybxx.common.dson.binary.BinarySerializable;
import cn.wjybxx.common.log.DebugLogFriendlyObject;

import javax.annotation.Nonnull;

/**
 * rpc请求结构体
 *
 * @author wjybxx
 * date 2023/4/1
 */
@AutoTypeArgs
@BinarySerializable
public class RpcRequest implements DebugLogFriendlyObject {

    /**
     * 客户端进程id - 它与{@link NodeId}是两个维度的东西
     * <p>
     * Q: 它出现的必要性？
     * A: 进程重启可能收到前一个进程的消息，因此需要在请求中记录其关联的进程。
     */
    private long clientProcessId;
    /** 请求方 */
    private NodeId clientNodeId;
    /** 请求id */
    private long requestId;
    /** 是否是单项调用（不需要结果） */
    private boolean oneWay;
    /** 方法描述信息 */
    private RpcMethodSpec<?> rpcMethodSpec;

    public RpcRequest() {
        // 可能的序列化支持
    }

    public RpcRequest(long clientProcessId, NodeId clientNodeId, long requestId, boolean oneWay, RpcMethodSpec<?> rpcMethodSpec) {
        this.requestId = requestId;
        this.oneWay = oneWay;
        this.clientProcessId = clientProcessId;
        this.clientNodeId = clientNodeId;
        this.rpcMethodSpec = rpcMethodSpec;
    }

    public long getRequestId() {
        return requestId;
    }

    public void setRequestId(long requestId) {
        this.requestId = requestId;
    }

    public boolean isOneWay() {
        return oneWay;
    }

    public void setOneWay(boolean oneWay) {
        this.oneWay = oneWay;
    }

    public long getClientProcessId() {
        return clientProcessId;
    }

    public void setClientProcessId(long clientProcessId) {
        this.clientProcessId = clientProcessId;
    }

    public NodeId getClientNodeId() {
        return clientNodeId;
    }

    public void setClientNodeId(NodeId clientNodeId) {
        this.clientNodeId = clientNodeId;
    }

    public RpcMethodSpec<?> getRpcMethodSpec() {
        return rpcMethodSpec;
    }

    public void setRpcMethodSpec(RpcMethodSpec<?> rpcMethodSpec) {
        this.rpcMethodSpec = rpcMethodSpec;
    }

    @Nonnull
    @Override
    public String toSimpleLog() {
        return '{' +
                "clientNodeGuid=" + clientProcessId +
                ", clientNode=" + clientNodeId +
                ", requestGuid=" + requestId +
                ", oneWay=" + oneWay +
                ", methodSpec=" + rpcMethodSpec.toSimpleLog() +
                '}';
    }

    @Nonnull
    @Override
    public String toDetailLog() {
        return '{' +
                "clientNodeGuid=" + clientProcessId +
                ", clientNode=" + clientNodeId +
                ", requestGuid=" + requestId +
                ", oneWay=" + oneWay +
                ", methodSpec=" + rpcMethodSpec.toDetailLog() +
                '}';
    }

    @Override
    public String toString() {
        return "RpcRequest{" +
                "clientNodeGuid=" + clientProcessId +
                ", clientNode=" + clientNodeId +
                ", requestGuid=" + requestId +
                ", oneWay=" + oneWay +
                ", rpcMethodSpec=" + rpcMethodSpec.toSimpleLog() +
                '}';
    }

}