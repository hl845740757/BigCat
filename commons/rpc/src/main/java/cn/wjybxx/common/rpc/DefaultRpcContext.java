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

import cn.wjybxx.common.concurrent.XCompletableFuture;
import cn.wjybxx.common.ex.ErrorCodeException;

/**
 * @author wjybxx
 * date 2023/4/1
 */
public class DefaultRpcContext<V> implements RpcContext<V> {

    private final RpcRequest request;
    private final XCompletableFuture<V> future;

    public DefaultRpcContext(RpcRequest request, XCompletableFuture<V> future) {
        this.request = request;
        this.future = future;
    }

    @Override
    public RpcRequest request() {
        return request;
    }

    @Override
    public RpcAddr remoteAddr() {
        return request.srcAddr;
    }

    @Override
    public RpcAddr localAddr() {
        return request.destAddr;
    }

    @Override
    public void sendResult(V msg) {
        future.complete(msg);
    }

    @Override
    public void sendError(int errorCode, String msg) {
        if (errorCode == 0) throw new IllegalArgumentException();
        future.completeExceptionally(new ErrorCodeException(errorCode, msg));
    }

    @Override
    public XCompletableFuture<V> future() {
        return future;
    }
}