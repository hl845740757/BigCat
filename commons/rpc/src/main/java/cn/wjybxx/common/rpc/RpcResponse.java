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

/**
 * rpc响应结构体
 *
 * @author wjybxx
 * date 2023/4/1
 */
@AutoSchema
@BinarySerializable
public class RpcResponse extends RpcProtocol implements DebugLogFriendlyObject {

    /** 请求的唯一id */
    private long requestId;
    /** 服务id -- 定位返回值类型，也可以用于校验 */
    private int serviceId;
    /** 方法id */
    private int methodId;
    /** 错误码（0表示成功） -- 不使用枚举，以方便用户扩展 */
    private int errorCode;
    /**
     * 如果调用成功，该值表示对应的结果。
     * 如果调用失败，该值为对应的错误信息，为{@link String}类型。
     */
    private Object result;

    public RpcResponse() {
        // 序列化支持
    }

    public RpcResponse(long conId, RpcAddr srcAddr, RpcAddr destAddr) {
        super(conId, srcAddr, destAddr);
    }

    private RpcResponse(RpcRequest request, RpcAddr selfAddr, int errorCode, Object result) {
        super(request.getConId(), selfAddr, request.getSrcAddr());
        this.requestId = request.getRequestId();
        this.serviceId = request.getServiceId();
        this.methodId = request.getMethodId();
        this.errorCode = errorCode;
        this.result = result;
    }

    public static RpcResponse newSucceedResponse(RpcRequest request, RpcAddr selfAddr, Object result) {
        return new RpcResponse(request, selfAddr, 0, result);
    }

    public static RpcResponse newFailedResponse(RpcRequest request, RpcAddr selfAddr, int errorCode, String msg) {
        if (errorCode == 0) throw new IllegalArgumentException("invalid errorCode " + errorCode);
        return new RpcResponse(request, selfAddr, errorCode, msg);
    }

    public boolean isSuccess() {
        return errorCode == 0;
    }

    public String getErrorMsg() {
        if (errorCode == 0) {
            throw new IllegalStateException("errorCode == 0");
        }
        return (String) result;
    }
    //

    public long getRequestId() {
        return requestId;
    }

    public RpcResponse setRequestId(long requestId) {
        this.requestId = requestId;
        return this;
    }

    public int getServiceId() {
        return serviceId;
    }

    public RpcResponse setServiceId(int serviceId) {
        this.serviceId = serviceId;
        return this;
    }

    public int getMethodId() {
        return methodId;
    }

    public RpcResponse setMethodId(int methodId) {
        this.methodId = methodId;
        return this;
    }

    public int getErrorCode() {
        return errorCode;
    }

    public RpcResponse setErrorCode(int errorCode) {
        this.errorCode = errorCode;
        return this;
    }

    public Object getResult() {
        return result;
    }

    public RpcResponse setResult(Object result) {
        this.result = result;
        return this;
    }

    @Nonnull
    @Override
    public String toSimpleLog() {
        return "{" +
                "requestId=" + requestId +
                ", serviceId=" + serviceId +
                ", methodId=" + methodId +
                ", errorCode=" + errorCode +
                ", result=" + result +
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
        return "RpcResponse{" +
                "requestId=" + requestId +
                ", serviceId=" + serviceId +
                ", methodId=" + methodId +
                ", errorCode=" + errorCode +
                ", result=" + result +
                ", conId=" + conId +
                ", srcAddr=" + srcAddr +
                ", destAddr=" + destAddr +
                '}';
    }
}