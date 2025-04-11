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

import cn.wjybxx.base.annotation.Internal;
import cn.wjybxx.concurrent.IDisruptorEventLoop;
import com.google.inject.Injector;
import it.unimi.dsi.fastutil.ints.IntSet;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;

/**
 * Worker表示进程中的一个线程，是业务的执行单元，是模块（Module）和服务(Service)的载体。
 * <p>
 * 1. Worker是Node的内部概念，不直接暴露在网络中.
 * 2. Worker定义了Service命名空间，单个Worker下不支持相同名字的Service。
 * 3. 同Worker上的模块之间直接调用，不同Worker之间的Module通过Rpc交互 —— 因此哪些Module在同一个Worker需要提前规划。
 * 4. 规划上不在同一个Worker时Module，在部署时不应该部署在同一个Worker，否则可能引发死锁等问题。
 * 5. Worker通过Module扩展，Worker为Module提供运行环境。
 * 6. 为保持框架的简单性，我们不支持运行时增删模块 -- 对于游戏服务器而言不必要。
 * 7. 不建议Worker包含和Node相同名字的Service。
 *
 * <h3>主循环 + 事件驱动</h3>
 * 在游戏开发领域，游戏世界需要不停的更新，而通过事件的方式驱动世界更新是复杂且低效的，因此普遍采用轮询的方式更新世界 —— 而这个轮询（循环），在游戏开发中称之为主循环。
 * 在服务器端，为降低压力，主循环的频率通常小于等于30帧/秒，因此完全在主循环中处理所有的逻辑，会导致响应速度较低；因此服务端通常采用 主循环 + 事件驱动 的工作方式。
 * 事件驱动是指：在等待下一次主循环的间隙中，Worker也会处理玩家的输入和处理一些其它的任务。这可以提高服务器对玩家操作的响应速度，也减少了主循环的计算压力，使CPU负载更加均匀。
 *
 * <h3>时序</h3>
 * 1. 启动时，Worker会按照Module的添加顺序启动所有的Module。
 * 2. 循环时，Worker会按照Module的添加顺序执行Module的Update方法。
 * 3. 停止时，Worker会按照启动顺序的逆序执行所有Module的Stop方法。
 *
 * <h3>模块</h3>
 * Module是Worker的组件单元。
 * Module分为两类：Module 和 Service。
 * 模块（Module）是业务逻辑的集成单元，应用由模块（Module）构成。
 * 服务(Service)用于将模块的业务暴露到网络中，因此服务是对外提供服务的基本单位。
 * <p>
 * 1. 不建议Module在构造方法中执行太多逻辑，避免复杂的依赖和环境问题。
 * 2. Module之间的特殊依赖由MainModule解决。
 * 3. 如果Service单导出单个Module的业务，通常由Module直接实现Service接口；否则应由门面类实现Service。
 *
 * @author wjybxx
 * date - 2023/10/4
 */
public interface Worker extends IDisruptorEventLoop<WorkerEvent> {

    /** Worker的Rpc地址 - NodeId和workerId都为有效值 */
    @Nonnull
    WorkerAddr workerAddr();

    /** Worker上绑定的Bean容器 - 用于解决依赖问题 */
    Injector injector();

    /**
     * Worker上绑定的服务id
     * 1.该接口只约定Worker启动后可正确获得，启动之前不保证可见性。
     * 2.通常不建议Worker上包含与Node上同名的服务
     * 3.如果是Node，也仅仅表示Node自身的服务
     */
    IntSet services();

    //-----

    /**
     * 返回node设置的数据。
     * node为管理worker，需要保存Worker的一些上下文。
     * 这些数据只应该node读写，用户不应该访问，不保证对外的可见性。
     */
    @Internal
    WorkerControlData controlData();

    /**
     * Worker绑定的Node
     * Node返回自身
     */
    @Nonnull
    Node node();

    // region 接口适配
    @Nullable
    @Override
    Node parent();

    @Nonnull
    @Override
    default Worker select() {
        return this;
    }

    @Nonnull
    @Override
    default Worker select(int key) {
        return this;
    }

    // endregion

    // region global

    /** 获取当前线程绑定的Worker */
    static Worker currentWorker() {
        return FxUtils.CURRENT_WORKER.get();
    }

    // endregion
}