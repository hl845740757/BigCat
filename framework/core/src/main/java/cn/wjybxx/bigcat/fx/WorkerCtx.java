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

package cn.wjybxx.bigcat.fx;

import cn.wjybxx.bigcat.rpc.RpcInterceptor;
import cn.wjybxx.bigcat.rpc.RpcRegistry;
import com.google.inject.ConfigurationException;

import javax.annotation.concurrent.NotThreadSafe;

/**
 * node为管理worker，需要保存Worker的一些上下文
 * <p>
 * 1.上下文中的数据只应Node读写，因此不保证对其它线程的可见性
 * 2.该对象仅用于底层Node和Worker交互，用户不应该使用
 *
 * @author wjybxx
 * date - 2023/10/28
 */
@NotThreadSafe
public final class WorkerCtx {

    Worker worker;
    RpcRegistry rpcRegistry;
    RpcInterceptor interceptor;

    public WorkerCtx() {
    }

    void init(Worker worker) {
        this.worker = worker;
        this.rpcRegistry = worker.injector().getInstance(RpcRegistry.class);
        try {
            this.interceptor = worker.injector().getInstance(RpcInterceptor.class);
        } catch (ConfigurationException ignore) {

        }
    }

}