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

import java.util.concurrent.CompletionStage;

/**
 * rpc方法代理
 * 用于代码生成工具为{@link RpcMethod}生成对应lambda表达式，以代替反射调用。
 * 当然也可以手写实现。
 *
 * @author wjybxx
 * date 2023/4/1
 */
@FunctionalInterface
public interface RpcMethodProxy {

    /**
     * 执行调用
     *
     * @param context    rpc执行时的一些上下文
     * @param methodSpec 方法的参数
     * @return 方法执行结果，可能情况：1.null 2.{@link CompletionStage} 3.其它结果
     * @throws Exception 由于用户的代码可能存在抛出异常的情况，这里声明异常对lambda更友好
     */
    Object invoke(@StableName RpcContext<?> context,
                  @StableName RpcMethodSpec<?> methodSpec) throws Exception;

}