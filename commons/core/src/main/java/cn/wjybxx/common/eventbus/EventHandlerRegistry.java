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

package cn.wjybxx.common.eventbus;

import javax.annotation.Nullable;

/**
 * 事件处理器注册表
 *
 * @author wjybxx
 * date 2023/4/6
 */
public interface EventHandlerRegistry {

    /**
     * @param masterKey  主事件key，当监听特定事件源对象的事件时，请外部封装为{@link MasterKeyX}
     * @param childKey   子事件key -- 为null或空集合时表示只监听masterKey；如果为非空集合，则表示要监听所有的子事件。
     * @param handler    事件处理器
     * @param customData 传递给EventBus的自定义数据，用于实现切面等功能 -- 在实践中通常是Json
     */
    <T> void registerX(Object masterKey, @Nullable Object childKey, EventHandler<T> handler, @Nullable Object customData);

    /**
     * 删除一个事件处理器。
     * 注意：如果是lambda表达式生成的引用，则必须引用相等，即你必须保存事件处理器的引用。
     *
     * @param masterKey 主事件key，取消指定对象的事件时，请外部封装为{@link MasterKeyX}
     * @param childKey  子事件key
     * @param handler   要删除的处理器
     */
    void unregisterX(Object masterKey, @Nullable Object childKey, EventHandler<?> handler);

    /***
     * 判断handler是否在对应的事件监听列表表
     *
     * @param childKey 注意，这里不支持集合
     */
    boolean hasListener(Object masterKey, @Nullable Object childKey, EventHandler<?> handler);

    /**
     * 清理注册表，释放内存
     */
    void clear();

    // region 辅助方法
    // 默认参数和通过泛型提示Handler接收的事件类型

    default <T> void registerX(Object masterKey, @Nullable Object childKey, EventHandler<T> handler) {
        registerX(masterKey, childKey, handler, null);
    }

    default <T> void register(Class<T> eventType, EventHandler<? super T> handler) {
        registerX(eventType, null, handler, null);
    }

    default <T> void register(Class<T> eventType, EventHandler<? super T> handler, Object customData) {
        registerX(eventType, null, handler, customData);
    }

    default <T> void register(Class<T> eventType, @Nullable Object childKey, EventHandler<? super T> handler) {
        registerX(eventType, childKey, handler, null);
    }

    default <T> void register(Class<T> eventType, @Nullable Object childKey, EventHandler<? super T> handler, Object customData) {
        registerX(eventType, childKey, handler, customData);
    }

    default void unregister(Class<?> eventType, EventHandler<?> handler) {
        unregisterX(eventType, null, handler);
    }

    default void unregister(Class<?> eventType, Object childKey, EventHandler<?> handler) {
        unregisterX(eventType, childKey, handler);
    }

    // endregion

}