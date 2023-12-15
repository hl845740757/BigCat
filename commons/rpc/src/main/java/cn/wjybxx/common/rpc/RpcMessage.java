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

import javax.annotation.Nonnull;
import java.util.List;

/**
 * 单向消息
 * 1. 该类只用于表明协议结构，实际不会被使用和传输，实际使用和传输的是{@link RpcRequest}.
 * 2. 该结构可以省去8或6字节的requestId开销。
 * 3. 在服务器与服务器通信之间不会被使用，主要用于节省客户端与服务器之间的通信开销。
 *
 * @author wjybxx
 * date - 2023/9/11
 */
@SuppressWarnings("unused")
public final class RpcMessage extends RpcProtocol {

    /** 服务id */
    private int serviceId;
    /** 方法id */
    private int methodId;
    /** 方法参数 */
    private List<Object> parameters;

    public RpcMessage() {
    }

    public RpcMessage(long conId, RpcAddr srcAddr, RpcAddr destAddr) {
        super(conId, srcAddr, destAddr);
    }

    public RpcMessage(long conId, RpcAddr srcAddr, RpcAddr destAddr, RpcMethodSpec<?> methodSpec) {
        super(conId, srcAddr, destAddr);
        this.serviceId = methodSpec.getServiceId();
        this.methodId = methodSpec.getMethodId();
        this.parameters = methodSpec.getParameters();
        setSharable(methodSpec.isSharable());
    }

    public int getServiceId() {
        return serviceId;
    }

    public RpcMessage setServiceId(int serviceId) {
        this.serviceId = serviceId;
        return this;
    }

    public int getMethodId() {
        return methodId;
    }

    public RpcMessage setMethodId(int methodId) {
        this.methodId = methodId;
        return this;
    }

    public List<Object> getParameters() {
        return parameters;
    }

    public RpcMessage setParameters(List<Object> parameters) {
        this.parameters = parameters;
        return this;
    }

    //
    @Nonnull
    @Override
    public String toSimpleLog() {
        return "{" +
                "serviceId=" + serviceId +
                ", methodId=" + methodId +
                ", parameterCount=" + parameters.size() +
                ", conId=" + conId +
                ", srcAddr=" + srcAddr +
                ", destAddr=" + destAddr +
                '}';
    }

    @Nonnull
    @Override
    public String toDetailLog() {
        return toString();
    }

    @Override
    public String toString() {
        return "RpcMessage{" +
                "serviceId=" + serviceId +
                ", methodId=" + methodId +
                ", parameters=" + parameters +
                ", conId=" + conId +
                ", srcAddr=" + srcAddr +
                ", destAddr=" + destAddr +
                '}';
    }
}