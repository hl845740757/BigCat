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


import cn.wjybxx.base.annotation.StableName;
import cn.wjybxx.concurrent.IFuture;

import java.lang.annotation.ElementType;
import java.lang.annotation.Retention;
import java.lang.annotation.RetentionPolicy;
import java.lang.annotation.Target;
import java.util.concurrent.CompletableFuture;

/**
 * 该注解用于注释需要被导出的方法。
 *
 * <h3>限制</h3>
 * 1.方法不能是private - 至少是包级访问权限(让生成的代码可访问) -- 建议用接口定义服务。
 * 2.除去Context参数，方法最多可有1个参数 - 跨语言兼容，且编解码效率更好。
 * 3.方法参数和结果不能是基本类型，也不能是List和字典，必须是普通结构
 * 4.方法如果没有返回值，且引入了Context参数，建议将发现参数声明为object
 * 5.Future和Context的泛型参数不能使用通配符{@code ?}
 *
 * <h3>代理方法的返回值</h3>
 * 1.如果方法的返回值为{@link IFuture}或{@link CompletableFuture}，则会捕获Future的泛型参数作为返回值类型。
 * 2.如果方法的返回值为void，但第一个参数为{@link RpcContext}，工具会捕获{@link RpcContext}的泛型参数作为返回值类型。
 * 3.其它普通方法，其返回值类型就是代理方法的返回值类型（void会被装箱）。
 *
 * <h3>Context</h3>
 * Context有助于实现复杂的消息交互，允许在返回结果前后向对方发送额外的消息，这在与客户端通信的过程中非常有用。
 * 1.如果需要Ctx，必须将{@link RpcContext}定义为方法的第一个参数。
 * 2.Context不会导出给客户端的Proxy。
 * 3.需要自行管理结果的返回时机时，需要设置{@link #manualReturn()}
 * 4.关于context的用法可查看测试用例(NodeTest类)
 *
 * <h3>保留策略</h3>
 * 最新修改为{@link RetentionPolicy#RUNTIME}，是为了方便反射获取方法信息。
 *
 * @author wjybxx
 * date 2023/4/1
 */
@Retention(RetentionPolicy.RUNTIME)
@Target(ElementType.METHOD)
public @interface RpcMethod {

    /**
     * 该方法在该类中的唯一id。
     * <p>
     * 注意：
     * 1.取值范围为闭区间[1, 9999]。
     * 2.由该id和serviceId构成唯一索引
     */
    int methodId();

    /**
     * 方法参数是否可共享
     * 当方法参数可共享时，序列化会延迟到IO线程 —— 理论上可做到进程内rpc不序列化。
     * <p>
     * 1.该属性用于配置默认值，减少用户调用{@link RpcMethodSpec#setSharable(boolean)}设置共享属性。
     * 2.主要用于避免本地Rpc调用时的序列化过程
     */
    boolean argSharable() default false;

    /**
     * 方法返回值是否可共享
     * 当返回值可共享时，序列化会延迟到IO线程
     * <p>
     * 1.该属性用于配置默认值，减少用户调用{@link RpcContext#setSharable(boolean)}.
     * 2.主要用于避免本地Rpc调用时的序列化过程
     *
     * @see #argSharable()
     */
    boolean resultSharable() default false;

    /**
     * 是否由用户手动返回结果
     * <p>
     * 1.该属性用于配置默认值，减少用户调用{@link RpcContext#setManualReturn(boolean)}
     * 2.如果用户手动返回结果，方法参数第一个必须是{@link RpcContext}
     * 3.方法的直接返回值为{@code void}时才需要设置
     */
    boolean manualReturn() default false;

    /**
     * 是否启用建造者模式(主要为protobuf服务)
     * 如果为true，apt生成客户端代码时会生成一个参数为Builder的重载方法。
     *
     * <h3>规范</h3>
     * 1. Message必须是方法的最后一个参数。
     * 2. Builder必须是Message的内部类。
     *
     * <p>
     * Q：生成重载方法的作用？
     * A：protobuf在java端的实现是builder模式，如果总是在主线程构建message，可能有一丢丢开销；如果将build延迟到IO线程，则可以提高主线程性能；
     * 注意，广播消息总是应该主线程build，否则会产生多线程问题。
     * <p>
     * Q：如何关闭？
     * A：通过{@code PBParserOptions}类关闭，或通过拦截器关闭部分服务的设置。
     */
    boolean builderPattern() default false;

    /**
     * 自定义扩展数据，通常是json或dson格式。
     * 它的主要作用是配置切面数据，用于拦截器。比如：发包频率限制等。
     */
    @StableName
    String customData() default "";

}