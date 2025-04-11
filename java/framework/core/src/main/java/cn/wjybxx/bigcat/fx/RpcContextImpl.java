/*
 * Copyright 2023-2025 wjybxx(845740757@qq.com)
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

import cn.wjybxx.base.BitFlags;
import cn.wjybxx.concurrent.IFuture;

import java.util.concurrent.CompletableFuture;

/**
 * {@link RpcContext}的默认实现
 *
 * @param <V>
 * @author wjybxx
 * date 2023/4/1
 */
public final class RpcContextImpl<V> implements RpcContext<V> {

    final RpcClientImpl rpcClient;
    final long conId;
    final WorkerAddr remoteAddr;
    final long requestId;
    final int serviceId;
    final int methodId;
    final int invokeType;
    private int options;

    public RpcContextImpl(RpcClientImpl rpcClient,
                          long conId, WorkerAddr remoteAddr,
                          long requestId, int serviceId, int methodId,
                          int invokeType) {
        this.rpcClient = rpcClient;
        this.conId = conId;
        this.remoteAddr = remoteAddr;
        this.requestId = requestId;
        this.serviceId = serviceId;
        this.methodId = methodId;
        this.invokeType = invokeType;
    }

    @Override
    public long sessionId() {
        return conId;
    }

    @Override
    public WorkerAddr remoteAddr() {
        return remoteAddr;
    }

    @Override
    public boolean isSharable() {
        return (options & MASK_RESULT_SHARABLE) != 0;
    }

    @Override
    public void setSharable(boolean sharable) {
        options = BitFlags.set(options, MASK_RESULT_SHARABLE, sharable);
    }

    @Override
    public boolean isManualReturn() {
        return (options & MASK_RESULT_MANUAL) != 0;
    }

    @Override
    public void setManualReturn(boolean value) {
        options = BitFlags.set(options, MASK_RESULT_MANUAL, value);
    }

    @Override
    public void sendResult(V result) {
        if (invokeType == RpcInvokeType.ONEWAY) {
            return;
        }
        rpcClient.sendResult(conId, remoteAddr,
                requestId, serviceId, methodId,
                result, isSharable());
    }

    @Override
    public void sendResult(byte[] result) {
        if (invokeType == RpcInvokeType.ONEWAY) {
            return;
        }
        rpcClient.sendResult(conId, remoteAddr,
                requestId, serviceId, methodId,
                result, isSharable());
    }

    @Override
    public void sendError(int errorCode, String msg) {
        if (invokeType == RpcInvokeType.ONEWAY) {
            return;
        }
        rpcClient.sendError(conId, remoteAddr,
                requestId, serviceId, methodId,
                errorCode, msg);
    }

    @Override
    public void sendError(Throwable ex) {
        if (invokeType == RpcInvokeType.ONEWAY) {
            return;
        }
        rpcClient.sendError(conId, remoteAddr,
                requestId, serviceId, methodId,
                ex);
    }

    @Override
    public void sendAsyncResult(IFuture<V> future) {
        if (invokeType == RpcInvokeType.ONEWAY) {
            return;
        }
        rpcClient.sendAsyncResult(conId, remoteAddr,
                requestId, serviceId, methodId,
                future, isSharable());
    }

    @Override
    public void sendAsyncResult(CompletableFuture<V> future) {
        if (invokeType == RpcInvokeType.ONEWAY) {
            return;
        }
        rpcClient.sendAsyncResult(conId, remoteAddr,
                requestId, serviceId, methodId,
                future, isSharable());
    }

    // region 常量
    /** 返回值可共享 */
    private static final int MASK_RESULT_SHARABLE = 1;
    /** 手动返回结果 */
    private static final int MASK_RESULT_MANUAL = 1 << 1;
    // endregion
}