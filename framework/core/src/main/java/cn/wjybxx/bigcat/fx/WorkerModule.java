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

/**
 * WorkerModule是Worker的组件单元。
 * WorkerModule分为两类：Module 和 Service。
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
public interface WorkerModule {

    /** Worker会在启动所有的模块之前调用该方法，各模块可以在这里解决特殊的依赖问题 */
    default void inject(Worker worker) {

    }

    /** Worker会在启动时执行所有模块的Start方法 */
    default void start() {

    }

    /**
     * Worker每帧会调用调用所有模块的Update方法
     * 注：只有重写了该方法的类才会被每帧调用。
     */
    default void update() {

    }

    /** Worker在停止时会调用所有模块的Stop方法 */
    default void stop() {

    }

}