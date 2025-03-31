/*
 *  Copyright 2023-2024 wjybxx
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

import cn.wjybxx.concurrent.EventLoopModule;

/**
 * @author wjybxx
 * date - 2023/12/22
 */
public abstract class AbstractRpcRouter extends EventLoopModule implements RpcRouter {

    protected Node node;
    protected RpcSupport rpcSupport;
    protected RpcSerializer serializer;
    protected RpcMethodRegistry methodRegistry;

    /** 是否允许本地调用共享对象 - 可禁用{@link RpcProtocol#isSharable()} */
    protected boolean enableLocalShare = true;

    // region 设置

    /** 是否允许本地调用共享对象 */
    public boolean isEnableLocalShare() {
        return enableLocalShare;
    }

    public void setEnableLocalShare(boolean enableLocalShare) {
        this.enableLocalShare = enableLocalShare;
    }

    // endregion

    // region addr

    @Override
    public void resolveDependence() {
        node = (Node) getEntity();
        rpcSupport = node.injector().getInstance(RpcSupport.class);
        serializer = node.injector().getInstance(RpcSerializer.class);
        methodRegistry = node.injector().getInstance(RpcMethodRegistry.class);
    }

    /** 测试给定地址似乎否是本地地址(进程内地址) */
    @Override
    public boolean isLocalAddr(RpcAddr addr) {
        if (addr instanceof WorkerAddr workerAddr) {
            return node.nodeAddr().equalsIgnoreWorker(workerAddr);
        }
        return false;
    }

    /** 判断是否是单播地址 */
    @Override
    public boolean isUnicastAddr(RpcAddr addr) {
        if (addr instanceof WorkerAddr workerAddr) {
            return workerAddr.serverType > 0
                    && workerAddr.serverId > 0
                    && !("*".equals(workerAddr.workerId));
        }
        return false;
    }

    /** 测试给定的地址在worker层是否是单播地址 */
    @Override
    public boolean isWorkerUnicastAddr(RpcAddr addr) {
        if (addr instanceof WorkerAddr workerAddr) {
            return !("*".equals(workerAddr.workerId));
        }
        return false;
    }

    /** 测试给定的地址在worker层是否是广播地址 */
    @Override
    public boolean isWorkerBroadcastAddr(RpcAddr addr) {
        if (addr instanceof WorkerAddr workerAddr) {
            return "*".equals(workerAddr.workerId);
        }
        return false;
    }

    // endregion
}