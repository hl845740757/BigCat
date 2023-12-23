/*
 *  Copyright 2023 wjybxx
 *
 *  Licensed under the Apache License, Version 2.0 (the "License");
 *  you may not use this file except in compliance with the License.
 *  You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 *  Unless required by applicable law or agreed to iBn writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 */

package cn.wjybxx.bigcat.fx;

import cn.wjybxx.common.rpc.RpcAddr;
import cn.wjybxx.common.rpc.RpcSerializer;

import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/12/22
 */
public abstract class AbstractRpcRouter implements NodeRpcRouter {

    protected Node node;
    protected NodeRpcSupport rpcSupport;
    protected RpcSerializer serializer;

    /** 是否允许本地调用共享对象 */
    protected boolean enableLocalShare = true;

    // region 设置

    public boolean isEnableLocalShare() {
        return enableLocalShare;
    }

    public AbstractRpcRouter setEnableLocalShare(boolean enableLocalShare) {
        this.enableLocalShare = enableLocalShare;
        return this;
    }

    // endregion

    @Override
    public void inject(Worker worker) {
        node = (Node) Objects.requireNonNull(worker);
        rpcSupport = worker.injector().getInstance(NodeRpcSupport.class);
        serializer = worker.injector().getInstance(RpcSerializer.class);
    }

    @Override
    public boolean isCrossLanguageAddr(RpcAddr addr) {
        return addr.getClass() == PlayerAddr.class;
    }

    @Override
    public boolean isLocalAddr(WorkerAddr addr) {
        return node.nodeAddr().equalsIgnoreWorker(addr);
    }

}