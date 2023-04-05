/*
 * Copyright 2023 wjybxx
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

package cn.wjybxx.bigcat.common.rpc;


import cn.wjybxx.bigcat.common.async.FluentFuture;

import java.lang.annotation.ElementType;
import java.lang.annotation.Retention;
import java.lang.annotation.RetentionPolicy;
import java.lang.annotation.Target;

/**
 * 该注解用于注释需要被导出的方法。
 *
 * <h3>代理方法的返回值</h3>
 * 1. 如果方法的返回值为{@link FluentFuture}或{@link java.util.concurrent.Future}，则会捕获{@code Future}的泛型参数作为返回值类型。
 * 2. 当方法返回值为void或泛型参数为通配符{@code ?}时，代理方法的返回值类型为Object（比Void有更好的兼容性）。
 * 3. 其它普通方法，其返回值类型就是代理方法的返回值类型。
 *
 * <h3>获取rpc上下文</h3>
 * 默认并不提供该支持，用户可以在自己的{@link RpcProcessor}将当{@link RpcProcessContext}发布到某个可访问的地方，eg：ThreadLocal。
 *
 * <h3>限制</h3>
 * 1. 方法不能是private - 至少是包级访问权限(让生成的代码可访问) -- 建议用接口定义服务。
 * 2. methodId必须在[0,9999]区间段。
 *
 * <h3>方法参数和返回值类型限制</h3>
 * 1. 不要使用short/byte/char及其包装类型，兼容性较差。
 * 2. 使用{@link java.util.Collection}和{@link java.util.Map}时，只可以使用抽象接口，否则需要定义Bean确定编解码类型。
 * 3. 建议保持参数数量小于等于3个，超过3个请定义Bean；定义Bean可以减少接口的变化频率，以及更好的编解码效率和类型精确度。
 *
 * @author wjybxx
 * date 2023/4/1
 */
@Retention(RetentionPolicy.SOURCE)
@Target(ElementType.METHOD)
public @interface RpcMethod {

    /**
     * 该方法在该类中的唯一id。
     * 注意：取值范围为闭区间[0, 9999]。
     *
     * @return 由该id和serviceId构成唯一索引。
     */
    short methodId();

}