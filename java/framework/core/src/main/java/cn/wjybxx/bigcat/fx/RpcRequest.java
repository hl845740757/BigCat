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


import cn.wjybxx.base.ArrayUtils;

import javax.annotation.Nonnull;

/**
 * rpc请求结构体
 *
 * @author wjybxx
 * date 2023/4/1
 */
public final class RpcRequest extends RpcProtocol {

    /** 请求id */
    private long requestId;
    /** 服务id */
    private int serviceId;
    /** 方法id */
    private int methodId;

    /** 调用类型 - {@link RpcInvokeType} */
    private int invokeType;
    /** 创建时间 -- 是否序列化到对方，取决于用户 */
    private long time;

    public RpcRequest() {
        // 可能的序列化支持
    }

    public RpcRequest(long conId, RpcAddr srcAddr, RpcAddr destAddr) {
        super(conId, srcAddr, destAddr);
    }

    public RpcRequest(long conId, RpcAddr srcAddr, RpcAddr destAddr,
                      RpcMethodSpec<?> methodSpec, int invokeType) {
        super(conId, srcAddr, destAddr);
        if (methodSpec.getParameter() == null) {
            methodSpec.setParameter(ArrayUtils.EMPTY_BYTE_ARRAY);
            methodSpec.setSharable(true);
        }
        this.invokeType = invokeType;
        this.serviceId = methodSpec.getServiceId();
        this.methodId = methodSpec.getMethodId();
        this.data = methodSpec.getParameter();
        // 缓存标记
        if (methodSpec.isSharable()) {
            setSharable(true);
        }
    }

    // region getter/setter

    public long getRequestId() {
        return requestId;
    }

    public void setRequestId(long requestId) {
        this.requestId = requestId;
    }

    public int getServiceId() {
        return serviceId;
    }

    public void setServiceId(int serviceId) {
        this.serviceId = serviceId;
    }

    public int getMethodId() {
        return methodId;
    }

    public void setMethodId(int methodId) {
        this.methodId = methodId;
    }

    public int getInvokeType() {
        return invokeType;
    }

    public void setInvokeType(int invokeType) {
        this.invokeType = invokeType;
    }

    public long getTime() {
        return time;
    }

    public void setTime(long time) {
        this.time = time;
    }

    // endregion

    @Nonnull
    public String toSimpleLog() {
        return "{" +
                "requestId=" + requestId +
                ", invokeType=" + invokeType +
                ", serviceId=" + serviceId +
                ", methodId=" + methodId +
                ", conId=" + conId +
                ", srcAddr=" + srcAddr +
                ", destAddr=" + destAddr +
                '}';
    }

    @Nonnull
    public String toDetailLog(String serviceName, String methodName) {
        return "RpcRequest{" +
                "requestId=" + requestId +
                ", invokeType=" + invokeType +
                ", serviceId=" + serviceName +
                ", methodId=" + methodName +
                ", data=" + dataToString() +
                ", conId=" + conId +
                ", srcAddr=" + srcAddr +
                ", destAddr=" + destAddr +
                '}';
    }

    @Override
    public String toString() {
        return "RpcRequest{" +
                "requestId=" + requestId +
                ", invokeType=" + invokeType +
                ", serviceId=" + serviceId +
                ", methodId=" + methodId +
                ", data=" + dataToString() +
                ", conId=" + conId +
                ", srcAddr=" + srcAddr +
                ", destAddr=" + destAddr +
                '}';
    }
}