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

import javax.annotation.Nonnull;
import javax.annotation.Nullable;

/**
 * 动态事件类型
 * <p>
 * 1.普通事件默认使用事件的Class作为主键，动态事件支持指定事件的主键
 * 2.当子键存在时，会联合子键进行第二次派发。
 * <p>
 * 建议项目中的事件都实现该接口，可获得最大的灵活度
 *
 * @author wjybxx
 * date 2023/4/6
 */
public interface DynamicEvent {

    /**
     * 事件的主键
     * 如果不考虑兼容性的话，通常是对象的运行时类型。
     * 在需要与客户端通信或策划配置的地方，通常使用枚举或int值
     */
    @Nonnull
    default Object masterKey() {
        return getClass();
    }

    /**
     * 事件的子键
     * 通常是一个枚举值或int值
     * <p>
     * 子键表达一个筛选条件，用于缩小派发范围，避免监听器都挂载在主键上；
     * 如果子键不为null，在派发事件时将联合主键进行第二次派发。
     */
    @Nullable
    default Object childKey() {
        return null;
    }

}