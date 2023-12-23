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

import cn.wjybxx.common.concurrent.EventLoop;
import cn.wjybxx.common.concurrent.FutureUtils;
import cn.wjybxx.common.time.TimeProvider;
import it.unimi.dsi.fastutil.ints.Int2IntMap;
import it.unimi.dsi.fastutil.ints.Int2IntOpenHashMap;
import it.unimi.dsi.fastutil.ints.Int2ObjectMap;
import it.unimi.dsi.fastutil.ints.IntSet;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.util.Iterator;
import java.util.List;
import java.util.concurrent.CopyOnWriteArraySet;

/**
 * Node表示分布式中的一个节点，是分布式架构下的成员 -- 也就是游戏架构中“服”的概念。
 * Node是一个IO线程，主要负责线程间和分布式进程间的Rpc通信。
 * Node是特殊的Worker，也支持挂载模块和服务，它挂载的模块称之为路由模块，它挂载的服务称之为路由服务。
 * Node是Worker的管理者，也是Worker在网络中的门面。
 * <p>
 * 1. 同Worker一样，Node也通过挂载模块（Module）扩展，
 * 2. Node的业务应当保持简单，勿在Node上挂在非IO模块。
 * 3. 为保持架构的简单性，我们不支持Node在运行时添加Worker.
 *
 * <h3>服务导出</h3>
 * 1. 当暴露服务到网络时，只能暴露服务支持的并发数，而不能暴露服务关联的Worker。
 * 2. Rpc客户端不能指定服务由哪个Worker处理 -- 避免不必要的依赖。
 *
 * <h3>建议</h3>
 * 1. Node上的{@link TimeProvider}最好支持多线程读，可参考{@link FutureUtils#newTimeProvider(EventLoop, long)}
 *
 * @author wjybxx
 * date - 2023/10/4
 */
public interface Node extends Worker {

    /**
     * 当前运行中的所有Node -- 用于未来支持单进程下启动多个服务器。
     * <p>
     * 经过反复地思考权衡，允许一个进程内启动多个Node是简单可靠的方式，代价是增加一部分开销 -- 不会太多。
     * 如果在一个Node内启动多个服务器，虽然资源利用率更高，但编程复杂，尤其对Rpc客户端不友好。
     * 如果需要查询当前线程的Node，可通过Worker查询。
     */
    CopyOnWriteArraySet<Node> CURRENT_NODES = new CopyOnWriteArraySet<>();

    /** 节点地址（不包含workerId） -- 用于Rpc通信；公司地址 */
    WorkerAddr nodeAddr();

    /** 直接绑定在Node上的模块 */
    @Override
    List<WorkerModule> modules();

    /** 直接绑定在Node上的服务 */
    @Override
    IntSet services();

    /** 服务id -> 存在对应服务的Worker -- 限本地使用 */
    Int2ObjectMap<ServiceInfo> serviceInfoMap();

    // region worker管理

    /** Node挂载的所有Worker */
    Iterator<Worker> workers();

    /** Node挂载的第一个Worker */
    Worker mainWorker();

    Worker nextWorker();

    Worker selectWorker(int key);

    /** 根据Worker的名字查找Worker，不存在则返回null */
    Worker findWorker(String workerId);

    // endregion

    // region 接口适配

    /** Node总是返回自己 */
    @Nonnull
    @Override
    default Node node() {
        return this;
    }

    /** Node没有Parent */
    @Nullable
    @Override
    default Node parent() {
        return null;
    }

    @Nonnull
    @Override
    default Node select() {
        return this;
    }

    @Nonnull
    @Override
    default Node select(int key) {
        return this;
    }
    // endregion

    // region util

    /** 获取所有的服务计数 -- 用于暴露到网络 */
    default Int2IntMap serviceCountMap() {
        Int2ObjectMap<ServiceInfo> servicedInfoMap = serviceInfoMap();
        Int2IntOpenHashMap result = new Int2IntOpenHashMap(servicedInfoMap.size());
        servicedInfoMap.values().forEach(serviceInfo -> {
            result.put(serviceInfo.serviceId, serviceInfo.workerList.size());
        });
        return result;
    }

    /** 获取指定服务的数量（并发数） */
    default int serviceCount(int serviceId) {
        ServiceInfo serviceInfo = serviceInfoMap().get(serviceId);
        return serviceInfo == null ? 0 : serviceInfo.workerList.size();
    }

    // endregion
}