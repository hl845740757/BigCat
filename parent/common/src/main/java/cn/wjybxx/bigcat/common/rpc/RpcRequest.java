/*
 * Copyright 2023 wjybxx
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

package cn.wjybxx.bigcat.common.rpc;


import cn.wjybxx.bigcat.common.codec.binary.BinarySerializable;
import cn.wjybxx.bigcat.common.log.DebugLogFriendlyObject;

import javax.annotation.Nonnull;

/**
 * rpc请求结构体
 *
 * @author wjybxx
 * date 2023/4/1
 */
@BinarySerializable
public class RpcRequest implements DebugLogFriendlyObject {

    /**
     * 请求方的唯一标识 - 它与{@link NodeSpec}是两个维度的东西
     * <p>
     * Q: 它出现的必要性？
     * A: 进程重启可能收到前一个进程的消息，因此需要在请求中记录其关联的进程。
     */
    private long clientNodeGuid;
    /** 请求方 */
    private NodeSpec clientNode;
    /** 请求的唯一id */
    private long requestGuid;
    /** 是否是单项调用（不需要结果） */
    private boolean oneWay;
    /** 方法描述信息 */
    private RpcMethodSpec<?> rpcMethodSpec;

    public RpcRequest() {
        // 可能的序列化支持
    }

    public RpcRequest(long clientNodeGuid, NodeSpec clientNode, long requestGuid, boolean oneWay, RpcMethodSpec<?> rpcMethodSpec) {
        this.requestGuid = requestGuid;
        this.oneWay = oneWay;
        this.clientNodeGuid = clientNodeGuid;
        this.clientNode = clientNode;
        this.rpcMethodSpec = rpcMethodSpec;
    }

    public long getRequestGuid() {
        return requestGuid;
    }

    public void setRequestGuid(long requestGuid) {
        this.requestGuid = requestGuid;
    }

    public boolean isOneWay() {
        return oneWay;
    }

    public void setOneWay(boolean oneWay) {
        this.oneWay = oneWay;
    }

    public long getClientNodeGuid() {
        return clientNodeGuid;
    }

    public void setClientNodeGuid(long clientNodeGuid) {
        this.clientNodeGuid = clientNodeGuid;
    }

    public NodeSpec getClientNode() {
        return clientNode;
    }

    public void setClientNode(NodeSpec clientNode) {
        this.clientNode = clientNode;
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
                "clientNodeGuid=" + clientNodeGuid +
                ", clientNode=" + clientNode +
                ", requestGuid=" + requestGuid +
                ", oneWay=" + oneWay +
                ", methodSpec=" + rpcMethodSpec.toSimpleLog() +
                '}';
    }

    @Nonnull
    @Override
    public String toDetailLog() {
        return '{' +
                "clientNodeGuid=" + clientNodeGuid +
                ", clientNode=" + clientNode +
                ", requestGuid=" + requestGuid +
                ", oneWay=" + oneWay +
                ", methodSpec=" + rpcMethodSpec.toDetailLog() +
                '}';
    }

    @Override
    public String toString() {
        return "RpcRequest{" +
                "clientNodeGuid=" + clientNodeGuid +
                ", clientNode=" + clientNode +
                ", requestGuid=" + requestGuid +
                ", oneWay=" + oneWay +
                ", rpcMethodSpec=" + rpcMethodSpec.toSimpleLog() +
                '}';
    }

}