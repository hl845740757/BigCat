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


import cn.wjybxx.base.pool.ConcurrentObjectPool;

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
    /** 创建时间 -- 是否序列化到对方，取决于需求；如果需要支持请求超时，由用户拦截 */
    private long createTime;

    public RpcRequest() {
        // 可能的序列化支持
    }

    public RpcRequest(long sessionId, WorkerAddr srcAddr, WorkerAddr destAddr) {
        super(sessionId, srcAddr, destAddr);
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

    public long getCreateTime() {
        return createTime;
    }

    public void setCreateTime(long createTime) {
        this.createTime = createTime;
    }

    // endregion

    // region toString

    @Override
    public String toString() {
        return "{" +
                "requestId=" + requestId +
                ", serviceId=" + serviceId +
                ", methodId=" + methodId +
                ", parameter=" + dataToString() +
                ", invokeType=" + invokeType +
                ", createTime=" + createTime +
                ", sessionId=" + sessionId +
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

        invokeType = 0;
        createTime = 0;
    }

    private static final ConcurrentObjectPool<RpcRequest> POOL = new ConcurrentObjectPool<>(
            RpcRequest::new, RpcRequest::reset, FxUtils.RPC_POOL_SIZE);

    /** 该方法通常由Worker线程调用 */
    public static RpcRequest acquire() {
        return POOL.acquire();
    }

    /** 该方法通常由Router调用 */
    public static void release(RpcRequest request) {
        POOL.release(request);
    }

    // endregion
}