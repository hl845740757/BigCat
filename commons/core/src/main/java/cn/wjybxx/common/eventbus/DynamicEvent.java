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

import javax.annotation.Nonnull;
import javax.annotation.Nullable;

/**
 * 建议项目中的事件都实现该接口，可获得最大的灵活度
 * EventBus对该类型事件的支持：
 * 1.sourceKey会和masterKey构成第一次的派发键，因此{@link #sourceKey()}不会增加派发次数
 * 2.当子键存在时，会联合子键进行第二次派发。
 * <p>
 * 用代码简单表述的话：
 * <pre>{@code
 *      PostKey postKey = new PostKey(event.sourceKey(), event.masterKey());
 *      fire(postKey, event);
 *      if (event.childKey() != null) {
 *          postKey.childKey = event.childKey();
 *          fire(postKey, event);
 *      }
 * }</pre>
 *
 * @author wjybxx
 * date 2023/4/6
 */
public interface DynamicEvent {

    /**
     * 事件源对象的键
     * <p>
     * Q：源键的作用？
     * A：在fastjgame项目并没有这个方法，我在实际的业务中，由于需要监听某个对象的事件，因此又引入了一个键定义。
     * 不过，从实现上讲，该方法并不是必须的，因为用户可以通过将{@code sourceKey}和{@code masterKey}打包为masterKey来实现，
     * 不过，这增加了监听器解析对象id和事件类型的复杂度，并不易用；在上层定义{@code sourceKey}可以很好的让业务保持简单。
     * <p>
     * Q：与子键的区别？
     * A：源键和子键其实都是筛选条件，缩小派发范围；但源键不增加派发次数，子键会增加派发次数。
     * <p>
     * 注意：特定的EventBus实现可能不支持该键相关逻辑 -- 因为多数情况下都不需要该特性。
     */
    default Object sourceKey() {
        return null;
    }

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