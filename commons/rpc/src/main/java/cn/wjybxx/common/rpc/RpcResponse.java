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


import cn.wjybxx.common.ObjectUtils;
import cn.wjybxx.common.codec.FieldImpl;
import cn.wjybxx.common.codec.binary.BinaryObjectReader;
import cn.wjybxx.common.codec.binary.BinaryObjectWriter;
import cn.wjybxx.common.codec.binary.BinarySerializable;
import cn.wjybxx.common.concurrent.FutureUtils;
import cn.wjybxx.common.ex.ErrorCodeException;
import cn.wjybxx.common.log.DebugLogFriendlyObject;
import org.apache.commons.lang3.exception.ExceptionUtils;

import javax.annotation.Nonnull;
import java.util.List;

/**
 * rpc响应结构体
 *
 * @author wjybxx
 * date 2023/4/1
 */
@BinarySerializable
public final class RpcResponse extends RpcProtocol implements DebugLogFriendlyObject {

    /** 请求的唯一id */
    private long requestId;
    /** 服务id -- 定位返回值类型，也可以用于校验 */
    private int serviceId;
    /** 方法id */
    private int methodId;
    /**
     * 错误码（0表示成功） -- 不使用枚举，以方便用户扩展
     * 如果调用成功，result为对应的结果。
     * 如果调用失败，result为错误信息，固定为字符串类型。
     */
    private int errorCode;
    /**
     * 方法结果
     * 1.正确设值的情况下不为null，为{@link byte[]}或{@link List}
     * 2.如果为bytes，表示已经序列化
     * 3.如果为List，表示尚未序列化；兼容无返回值和返回null的情况，也有利于扩展
     */
    @FieldImpl(writeProxy = "writeResults", readProxy = "readResults")
    private Object result;

    public RpcResponse() {
        // 序列化支持
    }

    public RpcResponse(long conId, RpcAddr srcAddr, RpcAddr destAddr) {
        super(conId, srcAddr, destAddr);
    }

    public RpcResponse(RpcRequest request, RpcAddr selfAddr) {
        super(request.getConId(), selfAddr, request.getSrcAddr());
        this.requestId = request.getRequestId();
        this.serviceId = request.getServiceId();
        this.methodId = request.getMethodId();
    }

    public RpcResponse(RpcRequest request, RpcAddr selfAddr, int errorCode, Object result) {
        super(request.getConId(), selfAddr, request.getSrcAddr());
        this.requestId = request.getRequestId();
        this.serviceId = request.getServiceId();
        this.methodId = request.getMethodId();
        this.errorCode = errorCode;
        this.result = result;
    }

    // region 业务

    public void setSuccess(Object result) {
        if (result == null) {
            this.errorCode = RpcErrorCodes.RESULT_NULL;
        } else if (result.getClass() == byte[].class) {
            this.errorCode = RpcErrorCodes.RESULT_BYTES;
        } else {
            this.errorCode = RpcErrorCodes.SUCCESS;
        }
        this.result = result;
    }

    public void setFailed(int errorCode, String msg) {
        assert !RpcErrorCodes.isSuccess(errorCode);
        this.errorCode = errorCode;
        this.result = ObjectUtils.nullToDef(msg, "");
        this.setSharable(true); // 字符串总是可共享
    }

    public void setFailed(Throwable ex) {
        ex = FutureUtils.unwrapCompletionException(ex);
        if (ex instanceof ErrorCodeException codeException) {
            setFailed(codeException.getErrorCode(), codeException.getMessage());
        } else if (ex instanceof RpcException rpcException) {
            setFailed(rpcException.getErrorCode(), rpcException.getMessage());
        } else {
            setFailed(RpcErrorCodes.SERVER_EXCEPTION, ExceptionUtils.getMessage(ex));
        }
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
    // endregion

    // region getter/setter
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
    // endregion

    @Nonnull
    @Override
    public String toSimpleLog() {
        return "{" +
                "requestId=" + requestId +
                ", serviceId=" + serviceId +
                ", methodId=" + methodId +
                ", errorCode=" + errorCode +
                ", results=" + result +
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
                ", results=" + result +
                ", conId=" + conId +
                ", srcAddr=" + srcAddr +
                ", destAddr=" + destAddr +
                '}';
    }

    // region 序列化优化
    // 1.自动处理延迟序列化问题

    public void writeResults(BinaryObjectWriter writer, int name) {
        if (errorCode == RpcErrorCodes.RESULT_NULL) {
            // null不写
        } else if (errorCode == RpcErrorCodes.RESULT_BYTES) {
            writer.writeBytes(name, (byte[]) result);
        } else {
            writer.writeObject(name, result);
        }
    }

    public void readResults(BinaryObjectReader reader, int name) {
        if (errorCode == RpcErrorCodes.RESULT_NULL) {
            // null不读
        } else if (errorCode == RpcErrorCodes.RESULT_BYTES) {
            result = reader.readBytes(name);
        } else {
            result = reader.readObject(name);
        }
    }

    // endregion
}