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

import java.lang.annotation.ElementType;
import java.lang.annotation.Retention;
import java.lang.annotation.RetentionPolicy;
import java.lang.annotation.Target;

/**
 * 该注解表示该类是可以对外提供服务，只有拥有该标记，才会为该类生成对应的代理工具类。
 * 生成的代理类名: {@code xxxProxy} {@code xxxExporter}
 * 其中：
 * Proxy用于客户端创建{@link RpcMethodSpec}，即：打包参数。
 * Exporter用于服务端暴露接口，向{@link RpcRegistry}中注册暴露的方法。
 * 生成的文件会添加一个指向源文件的引用，方便你通过引用查找生成的文件
 * <p>
 * 注意事项:
 * 1. service需要定义在公共模块，因为它为其它模块提供服务。
 * 2. 建议接口命名遵循统一的规范，比如{@code XXXService}。
 *
 * @author wjybxx
 * date 2023/4/1
 */
@Retention(RetentionPolicy.SOURCE)
@Target(ElementType.TYPE)
public @interface RpcService {

    /**
     * 该类对应的serviceId
     *
     * @return short
     */
    short serviceId();

}