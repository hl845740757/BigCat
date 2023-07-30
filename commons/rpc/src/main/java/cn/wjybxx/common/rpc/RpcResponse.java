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


import cn.wjybxx.common.log.DebugLogFriendlyObject;
import cn.wjybxx.common.codec.AutoSchema;
import cn.wjybxx.common.codec.binary.BinarySerializable;

import javax.annotation.Nonnull;

/**
 * rpc响应结构体
 *
 * @author wjybxx
 * date 2023/4/1
 */
@AutoSchema
@BinarySerializable
public class RpcResponse implements DebugLogFriendlyObject {

    /** 请求方进程id */
    private long clientProcessId;
    /** 请求的唯一id */
    private long requestId;
    /**
     * 错误码（0表示成功）
     * 没使用枚举，是为了方便用户扩展
     */
    private int errorCode;
    /**
     * 如果调用成功，该值表示对应的结果。
     * 如果调用失败，该值为对应的错误信息，为{@link String}类型。
     */
    private Object result;

    public RpcResponse() {
        // 序列化支持
    }

    private RpcResponse(long clientProcessId, long requestId, int errorCode, Object result) {
        this.clientProcessId = clientProcessId;
        this.requestId = requestId;
        this.errorCode = errorCode;
        this.result = result;
    }

    public static RpcResponse newSucceedResponse(long clientNodeGuid, long requestGuid, Object result) {
        return new RpcResponse(clientNodeGuid, requestGuid, 0, result);
    }

    public static RpcResponse newFailedResponse(long clientNodeGuid, long requestGuid, int errorCode, String msg) {
        return new RpcResponse(clientNodeGuid, requestGuid, errorCode, msg);
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

    public long getClientProcessId() {
        return clientProcessId;
    }

    public void setClientProcessId(long clientProcessId) {
        this.clientProcessId = clientProcessId;
    }

    public long getRequestId() {
        return requestId;
    }

    public void setRequestId(long requestId) {
        this.requestId = requestId;
    }

    public int getErrorCode() {
        return errorCode;
    }

    public void setErrorCode(int errorCode) {
        this.errorCode = errorCode;
    }

    public Object getResult() {
        return result;
    }

    public void setResult(Object result) {
        this.result = result;
    }

    @Nonnull
    @Override
    public String toSimpleLog() {
        return "{" +
                "clientNodeGuid=" + clientProcessId +
                ", requestGuid=" + requestId +
                ", errorCode=" + errorCode +
                '}';
    }

    @Nonnull
    @Override
    public String toDetailLog() {
        return "{" +
                "clientNodeGuid=" + clientProcessId +
                ", requestGuid=" + requestId +
                ", errorCode=" + errorCode +
                ", result=" + result +
                '}';
    }

    @Override
    public String toString() {
        return "RpcResponse{" +
                "clientNodeGuid=" + clientProcessId +
                ", requestGuid=" + requestId +
                ", errorCode=" + errorCode +
                ", result=" + result +
                '}';
    }

}