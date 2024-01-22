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

import cn.wjybxx.bigcat.rpc.RpcAddr;
import cn.wjybxx.bigcat.rpc.RpcClient;
import cn.wjybxx.bigcat.rpc.RpcMethodSpec;
import cn.wjybxx.concurrent.IFuture;

import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/10/28
 */
public class WorkerRpcClient implements RpcClient, WorkerModule {

    private Worker worker;
    private NodeRpcSupport rpcSupport;

    @Override
    public void inject(Worker worker) {
        this.worker = Objects.requireNonNull(worker);
        Node node;
        if (worker instanceof Node) {
            node = (Node) worker;
        } else {
            node = Objects.requireNonNull(worker.parent());
        }
        this.rpcSupport = node.injector().getInstance(NodeRpcSupport.class);
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

}
