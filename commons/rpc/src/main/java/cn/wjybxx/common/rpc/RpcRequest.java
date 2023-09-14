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


import cn.wjybxx.common.codec.AutoSchema;
import cn.wjybxx.common.codec.binary.BinarySerializable;
import cn.wjybxx.common.log.DebugLogFriendlyObject;

import javax.annotation.Nonnull;
import java.util.List;

/**
 * rpc请求结构体
 *
 * @author wjybxx
 * date 2023/4/1
 */
@AutoSchema
@BinarySerializable
public class RpcRequest extends RpcProtocol implements DebugLogFriendlyObject {

    /** 请求id */
    private long requestId;
    /** 调用类型 */
    private int invokeType;

    /** 服务id */
    private int serviceId;
    /** 方法id */
    private int methodId;
    /** 方法参数 */
    private List<Object> parameters;

    public RpcRequest() {
        // 可能的序列化支持
    }

    public RpcRequest(long conId, RpcAddr srcAddr, RpcAddr destAddr) {
        super(conId, srcAddr, destAddr);
    }

    public RpcRequest(long conId, RpcAddr srcAddr, RpcAddr destAddr,
                      long requestId, int invokeType, RpcMethodSpec<?> methodSpec) {
        super(conId, srcAddr, destAddr);
        this.requestId = requestId;
        this.invokeType = invokeType;
        this.serviceId = methodSpec.getServiceId();
        this.methodId = methodSpec.getMethodId();
        this.parameters = methodSpec.getParameters();
    }

    //

    public long getRequestId() {
        return requestId;
    }

    public RpcRequest setRequestId(long requestId) {
        this.requestId = requestId;
        return this;
    }

    public int getInvokeType() {
        return invokeType;
    }

    public RpcRequest setInvokeType(int invokeType) {
        this.invokeType = invokeType;
        return this;
    }

    public int getServiceId() {
        return serviceId;
    }

    public RpcRequest setServiceId(int serviceId) {
        this.serviceId = serviceId;
        return this;
    }

    public int getMethodId() {
        return methodId;
    }

    public RpcRequest setMethodId(int methodId) {
        this.methodId = methodId;
        return this;
    }

    public List<Object> getParameters() {
        return parameters;
    }

    public RpcRequest setParameters(List<Object> parameters) {
        this.parameters = parameters;
        return this;
    }

    //

    @Nonnull
    @Override
    public String toSimpleLog() {
        return "{" +
                "requestId=" + requestId +
                ", invokeType=" + invokeType +
                ", serviceId=" + serviceId +
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
        return "RpcRequest{" +
                "requestId=" + requestId +
                ", invokeType=" + invokeType +
                "serviceId=" + serviceId +
                ", methodId=" + methodId +
                ", parameters=" + parameters +
                ", conId=" + conId +
                ", srcAddr=" + srcAddr +
                ", destAddr=" + destAddr +
                '}';
    }
}