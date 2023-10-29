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

package cn.wjybxx.common.rpc;


import cn.wjybxx.common.annotation.StableName;
import cn.wjybxx.common.codec.TypeMeta;

import java.lang.annotation.ElementType;
import java.lang.annotation.Retention;
import java.lang.annotation.RetentionPolicy;
import java.lang.annotation.Target;
import java.util.Collection;
import java.util.Map;
import java.util.concurrent.CompletionStage;

/**
 * 该注解用于注释需要被导出的方法。
 *
 * <h3>代理方法的返回值</h3>
 * 1. 如果方法第一个参数为{@link RpcContext}，则返回值类型必须声明为void，代理会捕获{@link RpcContext}的泛型参数作为返回值类型。
 * 2. 如果方法的返回值为{@link CompletionStage}，则会捕获{@link CompletionStage}的泛型参数作为返回值类型。
 * 3. 其它普通方法，其返回值类型就是代理方法的返回值类型（基本类型和void会被装箱）。
 *
 * <h3>Context</h3>
 * Context有助于实现复杂的消息交互，允许在返回结果前后向对方发送额外的消息，这在与客户端通信的过程中非常有用。
 * 1. 如果需要Ctx，必须将{@link RpcContext}定义为方法的第一个参数。
 * 2. Context不会导出给客户端的Proxy，也不会计数
 * 3. 如果只想获得远程信息，而不想自行管理方法的返回时机，可将Ctx声明为{@link RpcGenericContext}
 * 4. 关于context的用法可查看测试用例(RpcTest2)
 *
 * <h3>限制</h3>
 * 1. 方法不能是private - 至少是包级访问权限(让生成的代码可访问) -- 建议用接口定义服务。
 * 2. methodId必须在[0,9999]区间段。
 * 3. Future和Context的泛型参数不能使用通配符{@code ?}
 * 4. 方法参数不可超过5个，否则需要定义Bean -- Context不计数
 * 5. 避免使用short/byte/char及其包装类型，兼容性较差。
 * 6. 使用集合和Map时，如果使用{@link Collection}和{@link Map}以外的类型，请确保实现类存在{@link TypeMeta}
 *
 * @author wjybxx
 * date 2023/4/1
 */
@Retention(RetentionPolicy.CLASS)
@Target(ElementType.METHOD)
public @interface RpcMethod {

    /**
     * 该方法在该类中的唯一id。
     * 注意：取值范围为闭区间[0, 9999]。
     *
     * @return 由该id和serviceId构成唯一索引。
     */
    int methodId();

    /**
     * 方法参数是否可共享
     * 当方法参数可共享时，序列化会延迟到IO线程 —— 理论上可做到进程内rpc不序列化。
     * <p>
     * 1.该属性用于配置默认值，以免所有调用者都需要调用{@link RpcMethodSpec#setSharable(boolean)}设置共享属性。
     * 2.如果方法参数仅限：基本类型 + 包装类型 + String，则默认会设置为true。
     */
    boolean argSharable() default false;

    /**
     * 方法返回值是否可共享
     * 当返回值可共享时，序列化会延迟到IO线程
     * <p>
     * 1. 该属性用于配置默认值，以免方法的实现者调用{@link RpcGenericContext#setSharable(boolean)}.
     * 2. 如果方法返回值为：基本类型 + 包装类型 + String，则默认会设置为true。
     *
     * @see #argSharable()
     */
    boolean resultSharable() default false;

    /**
     * 自定义扩展数据，通常是json或dson格式。
     * 它的主要作用是配置切面数据，用于拦截器。比如：某些消息只能在玩家在场景的时候处理。
     */
    @StableName
    String customData() default "";

}