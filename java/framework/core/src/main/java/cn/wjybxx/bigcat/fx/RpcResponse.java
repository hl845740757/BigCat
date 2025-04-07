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


import cn.wjybxx.base.ex.ErrorCodeException;
import cn.wjybxx.base.pool.ConcurrentObjectPool;
import cn.wjybxx.concurrent.ExecutorUtils;
import org.apache.commons.lang3.exception.ExceptionUtils;

/**
 * rpc响应结构体
 * 注意：该对象不可以共享，结果的可共享性取决于用户。
 *
 * @author wjybxx
 * date 2023/4/1
 */
public final class RpcResponse extends RpcProtocol {

    /** 请求的唯一id */
    private long requestId;
    /** 服务id -- 用于网络线程定位返回值类型，也可以用于校验和日志记录 */
    private int serviceId;
    /** 方法id */
    private int methodId;

    /**
     * 错误码（0表示成功） -- 不使用枚举，以方便用户扩展
     * 如果调用成功，result为对应的结果。
     * 如果调用失败，result为错误信息，固定为字符串类型。
     */
    private int errorCode;

    public RpcResponse() {
        // 序列化支持
    }

    public RpcResponse(long conId, WorkerAddr srcAddr, WorkerAddr destAddr) {
        super(conId, srcAddr, destAddr);
    }

    // region 业务

    public void setSuccess(Object result) {
        this.errorCode = RpcErrorCodes.SUCCESS;
        this.data = result;
    }

    /** 设置为失败，会自动标记为可共享 */
    public void setFailed(int errorCode, String msg) {
        if (errorCode < 1) {
            throw new IllegalArgumentException("errorCode: " + errorCode);
        }
        this.errorCode = errorCode;
        this.data = msg; // msg不为null
        setSharable(true);
    }

    /** 设置为失败，会自动标记为可共享 */
    public void setFailed(Throwable ex) {
        // future对下游任务总是进行了封装
        ex = ExecutorUtils.unwrapCompletionException(ex);
        if (ex instanceof ErrorCodeException codeException) {
            setFailed(codeException.getErrorCode(), codeException.getMessage());
        } else if (ex instanceof RpcException rpcException) {
            setFailed(rpcException.getErrorCode(), rpcException.getMessage());
        } else {
            setFailed(RpcErrorCodes.SERVER_EXCEPTION, ExceptionUtils.getMessage(ex));
        }
    }

    /** 结果转String，只有失败的情况下可调用 */
    public String getErrorMsg() {
        if (errorCode == 0) {
            throw new IllegalStateException("errorCode == 0");
        }
        return (String) data;
    }

    /** 是否成功 */
    public boolean isSucceeded() {
        return errorCode == 0;
    }

    /** 是否失败 */
    public boolean isFailed() {
        return errorCode != 0;
    }

    // endregion

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

    public int getErrorCode() {
        return errorCode;
    }

    public void setErrorCode(int errorCode) {
        this.errorCode = errorCode;
    }

    // endregion

    // region toString

    @Override
    public String toString() {
        return "{" +
                "requestId=" + requestId +
                ", serviceId=" + serviceId +
                ", methodId=" + methodId +
                ", errorCode=" + errorCode +
                ", result=" + dataToString() +
                ", conId=" + conId +
                ", srcAddr=" + srcAddr +
                ", destAddr=" + destAddr +
                '}';
    }
    // endregion

    // region pool

    @Override
    protected void reset() {
        super.reset();
        requestId = -1;
        serviceId = 0;
        methodId = 0;
        errorCode = 0;
    }

    private static final ConcurrentObjectPool<RpcResponse> POOL = new ConcurrentObjectPool<>(
            RpcResponse::new, RpcResponse::reset, FxUtils.RPC_POOL_SIZE);

    /** 该方法通常由Router调用 */
    public static RpcResponse acquire() {
        return POOL.acquire();
    }

    /** 该方法通常由Node线程调用 */
    public static void release(RpcResponse response) {
        POOL.release(response);
    }

    // endregion
}