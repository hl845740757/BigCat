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

import cn.wjybxx.common.concurrent.RingBufferEvent;

/**
 * 主模块是Worker的策略实现，每个Worker都绑定一个主模块。
 * <p>
 * 1. MainModule的{@link #update()}一定会被每帧调用，且先于其它Module执行。
 * 2. MainModule不可以发布为Service，必定命名冲突。
 *
 * @author wjybxx
 * date - 2023/10/4
 */
public interface MainModule extends WorkerModule {

    /** 用于在Worker启动挂载的所有Module之前解决部分模块之间的特殊依赖问题 */
    default void resolveDependence() {
    }

    // region 主循环和事件

    /** 检查当前是否需要执行主循环 */
    boolean checkMainLoop();

    /** 在每次开始主循环之前调用 */
    void beforeMainLoop();

    /** 在每次主循环结束后调用 */
    void afterMainLoop();

    /** 提交到Worker的事件 */
    void onEvent(RingBufferEvent rawEvent) throws Exception;

    // endregion

    // region Worker生命周期钩子

    /** 在{@link #resolveDependence()}之后调用 */
    void beforeWorkerStart();

    void afterWorkerStart();

    /** 在停止所有Module -- 如果是Node，会在停止Worker之前调用 */
    void beforeWorkerShutdown();

    /** 在Worker停止所有Module之后调用 */
    void afterWorkerShutdown();

    // endregion
}