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

import cn.wjybxx.base.annotation.Internal;
import cn.wjybxx.concurrent.IPromise;

/**
 * 请求存根
 * 新实现下，Stub会拷贝Request的关键数据，以实现生命周期的分离，使得Request可以简单池化。
 * 该对象仅用于框架层，不可暴露给用户。
 */
@Internal
public final class RpcRequestStub {

    public Worker worker;
    public IPromise<?> promise;
    public long deadline;

    /** 连接id */
    public long conId;
    /** 目标地址 */
    public WorkerAddr destAddr;
    /** 请求id */
    public long requestId;
    /** 服务id */
    public int serviceId;
    /** 方法id */
    public int methodId;
    /** 调用方式 */
    public int invokeType;

    public RpcRequestStub() {
    }

    public void reset() {
        worker = null;
        promise = null;
        deadline = 0;

        conId = 0;
        destAddr = null;
        requestId = -1;
        serviceId = 0;
        methodId = 0;
        invokeType = 0;
    }

}