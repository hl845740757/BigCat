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

package cn.wjybxx.bigcat.eventbus;

/**
 * 基础的EventBus接口。
 * （不适用战斗系统这类追求性能的场景，使用养成系统等更重视可读性的场景）
 *
 * <h3>chileKey时序问题</h3>
 * childKey的目的在于监听器分流，可以减少不必要的广播；
 * 但提前分流会导致不同的时序，如果时序很重要，用户应当不使用子键监听，然后再处理事件的时候测试子键的相等性。
 *
 * <h3>其它约定</h3>
 * 1.post接口显式接收子键，是为了避免对事件类型进行任何的假设。
 * 2.childKey仍需要包含在事件信息中，监听器可能需要这部分数据。
 *
 * @author wjybxx
 * date 2023/4/6
 */
public interface EventBus {

    // region register

    /**
     * @param masterKey 主事件key
     * @param handler   事件处理器
     */
    <T> void register(Class<T> masterKey, EventHandler<? super T> handler);

    /**
     * @param masterKey 主事件key
     * @param childKey  子事件key
     * @param handler   事件处理器
     */
    <T> void register(Class<T> masterKey, int childKey, EventHandler<? super T> handler);

    /**
     * @param masterKey 主事件key
     * @param childKey  子事件key
     * @param handler   事件处理器
     */
    <T> void register(Class<T> masterKey, Object childKey, EventHandler<? super T> handler);

    // endregion

    // region unregister

    /**
     * @param masterKey 主事件key
     * @param handler   事件处理器
     */
    <T> void unregister(Class<T> masterKey, EventHandler<? super T> handler);

    /**
     * @param masterKey 主事件key
     * @param childKey  子事件key
     * @param handler   事件处理器
     */
    <T> void unregister(Class<T> masterKey, int childKey, EventHandler<? super T> handler);

    /**
     * @param masterKey 主事件key
     * @param childKey  子事件key
     * @param handler   事件处理器
     */
    <T> void unregister(Class<T> masterKey, Object childKey, EventHandler<? super T> handler);

    // endregion

    // region post

    /** 派发事件 */
    void post(Object event);

    /**
     * 派发事件
     * (事件支持一个int类型的子键)
     *
     * @param childKey 事件子键
     */
    void post(Object event, int childKey);

    /**
     * 派发事件
     * (事件支持一个引用类型的子键)
     *
     * @param childKey 事件子键
     */
    void post(Object event, Object childKey);

    // endregion

    // region other

    /**
     * 清理注册表，释放内存
     */
    void clear();

    // endregion

}