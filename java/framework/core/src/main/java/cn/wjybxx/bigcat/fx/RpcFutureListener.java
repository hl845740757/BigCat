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

import cn.wjybxx.concurrent.IFuture;

import java.util.function.BiConsumer;
import java.util.function.Consumer;

/**
 * 这个对象是可池化的，但我们暂不池化
 *
 * @author wjybxx
 * date - 2025/4/9
 */
public final class RpcFutureListener<V> implements
        Consumer<IFuture<V>>,
        BiConsumer<V, Throwable> {

    final RpcClientImpl sessionMgr;
    final long sessionId;
    final WorkerAddr remoteAddr;
    final long requestId;
    final int serviceId;
    final int methodId;
    final boolean sharable;

    public RpcFutureListener(RpcClientImpl sessionMgr,
                             long sessionId, WorkerAddr remoteAddr,
                             long requestId, int serviceId, int methodId,
                             boolean sharable) {
        this.sessionMgr = sessionMgr;
        this.sessionId = sessionId;
        this.remoteAddr = remoteAddr;
        this.requestId = requestId;
        this.serviceId = serviceId;
        this.methodId = methodId;
        this.sharable = sharable;
    }

    @Override
    public void accept(IFuture<V> future) {
        if (future.isSucceeded()) {
            sessionMgr.sendResult(sessionId, remoteAddr, requestId, serviceId, methodId, future.resultNow(), sharable);
        } else {
            sessionMgr.sendError(sessionId, remoteAddr, requestId, serviceId, methodId, future.exceptionNow(false));
        }
    }

    @Override
    public void accept(V result, Throwable ex) {
        if (ex == null) {
            sessionMgr.sendResult(sessionId, remoteAddr, requestId, serviceId, methodId, result, sharable);
        } else {
            sessionMgr.sendError(sessionId, remoteAddr, requestId, serviceId, methodId, ex);
        }
    }

}