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

/**
 * 泛型化子键事件
 * 该接口约定子键必须是{@link Class}，这样注解处理器就可以在编译时捕获泛型参数类型，从而生成事件绑定方法，十分适合协议绑定这样的场景。
 * <p>
 * eg：让你的协议事件实现该类型，那么就可以通过泛型参数声明定义的协议，就可以避免不安全的类型转换。
 *
 * @author wjybxx
 * date 2023/4/6
 */
public interface GenericEvent<T> extends DynamicEvent {

    /** @implNote 实现类通常需要强制类型转换，{@code xxx.class} 是没有泛型信息的 */
    @Nonnull
    @Override
    Class<T> childKey();

}