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
 * 1. 如果方法的返回值为{@link CompletionStage}，则会捕获{@code Stage}的泛型参数作为返回值类型。
 * 2. 当方法返回值为void时，代理方法的返回值类型为Object（比Void有更好的兼容性）。
 * 3. 其它普通方法，其返回值类型就是代理方法的返回值类型。
 *
 * <h3>Context</h3>
 * Context有助于实现复杂的消息交互，允许在返回结果前后向对方发送额外的消息，这在与客户端通信的过程中非常有用。
 * 1. 如果需要Ctx，必须将{@link RpcContext}定义为方法的第一个参数。
 * 2. Context不会导出给客户端的Proxy，也不会计数
 * 3. 关于context的用法可查看测试用例(RpcTest2)
 *
 * <h3>限制</h3>
 * 1. 方法不能是private - 至少是包级访问权限(让生成的代码可访问) -- 建议用接口定义服务。
 * 2. methodId必须在[0,9999]区间段。
 * 3. 异步方法的Future不能使用通配符{@code ?}
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

}