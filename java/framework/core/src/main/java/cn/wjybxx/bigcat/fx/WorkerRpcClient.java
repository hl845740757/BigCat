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

import cn.wjybxx.concurrent.EventLoopModule;
import cn.wjybxx.concurrent.IAgentEventHandler;
import cn.wjybxx.concurrent.IFuture;
import cn.wjybxx.concurrent.IPromise;

import java.util.Objects;

/**
 * worker端的RpcClient仅仅将请求转发至Node，线程切换问题全部由Node处理。
 *
 * @author wjybxx
 * date - 2023/10/28
 */
public class WorkerRpcClient extends EventLoopModule implements RpcClient, IAgentEventHandler<WorkerEvent> {

    private Worker worker;
    private RpcSupport rpcSupport;

    @Override
    public void resolveDependence() {
        this.worker = (Worker) getEntity();
        Node node;
        if (worker instanceof Node) {
            node = (Node) worker;
        } else {
            node = Objects.requireNonNull(worker.parent());
        }
        this.rpcSupport = node.injector().getInstance(RpcSupport.class);
    }

    @Override
    public void send(RpcAddr target, RpcMethodSpec<?> methodSpec) {
        rpcSupport.w2n_send(worker, target, methodSpec);
    }

    @Override
    public <V> IFuture<V> call(RpcAddr target, RpcMethodSpec<V> methodSpec) {
        return rpcSupport.w2n_call(worker, target, methodSpec);
    }

    @Override
    public <V> V syncCall(RpcAddr target, RpcMethodSpec<V> methodSpec) {
        return rpcSupport.w2n_syncCall(worker, target, methodSpec);
    }

    @Override
    public <V> V syncCall(RpcAddr target, RpcMethodSpec<V> methodSpec, long timeoutMs) {
        return rpcSupport.w2n_syncCall(worker, target, methodSpec, timeoutMs);
    }

    // region rpc支持

    @Override
    public void start() {
        // 接收Node派发到Worker的请求和响应，再转换给RpcSupport
        worker.subscribe(FxUtils.TYPE_NODE_WORKER_REQUEST, this);
        worker.subscribe(FxUtils.TYPE_NODE_WORKER_RESPONSE, this);
    }

    @Override
    public void onEvent(long sequence, WorkerEvent event) throws Exception {
        switch (event.getType()) {
            case FxUtils.TYPE_NODE_WORKER_REQUEST -> {
                rpcSupport.onRcvRequestStep3((Worker) event.obj1, (RpcRequest) event.obj2);
            }
            case FxUtils.TYPE_NODE_WORKER_RESPONSE -> {
                @SuppressWarnings("unchecked") IPromise<Object> promise = (IPromise<Object>) event.obj2;
                rpcSupport.onRcvResponseStep3((RpcResponse) event.obj1, promise);
            }
            default -> {
                throw new AssertionError();
            }
        }
    }

    // endregion
}
