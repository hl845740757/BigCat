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

import cn.wjybxx.common.annotation.Internal;
import cn.wjybxx.common.concurrent.EventLoop;
import com.google.inject.Injector;
import it.unimi.dsi.fastutil.ints.IntSet;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.util.List;

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
 * @author wjybxx
 * date - 2023/10/4
 */
public interface Worker extends EventLoop {

    /** 用于获取当前Worker -- Node也会设置该变量 */
    ThreadLocal<Worker> CURRENT_WORKER = new ThreadLocal<>();

    /**
     * Worker的id —— 员工编号
     * 为线程分配数字id不易使用，我们使用字符串类型；在实际的rpc通信中，我们极少指定目标的workerId
     */
    String workerId();

    /** Worker上绑定的Bean容器 */
    Injector injector();

    /** Worker绑定的主模块 */
    MainModule mainModule();

    /**
     * Worker上绑定模块 -- 由于包含{@link MainModule}，因此一定不为空。
     * 该接口只约定Worker启动后可正确获得，其它时候不保证可见性。
     */
    List<WorkerModule> modules();

    /**
     * Worker上绑定的服务id
     * 1.该接口只约定Worker启动后可正确获得，启动之前不保证可见性。
     * 2.通常不建议Worker上包含与Node上同名的服务
     */
    IntSet services();

    //

    /** 返回node设置的上下文 */
    @Internal
    WorkerCtx workerCtx();

    /** Node返回自身，Worker返回从属的Node */
    @Nonnull
    Node node();

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

}