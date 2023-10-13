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
     * 服务id
     * <p>
     * 1. serviceId < 0，则表示本地服务
     * 2. serviceId >= 0 表示公共服务
     * 3. 在与客户端通信的服务中，取值范围为 [-32767, 32767]，即2字节内，且可以转正值
     * 4. serviceId 要好好规划，合理的serviceId分配有助于拦截器测试上下文
     */
    int serviceId();

    /**
     * 自定义扩展数据，通常是json或dson格式。
     * 它的主要作用是配置切面数据，用于拦截器。
     */
    @StableName
    String customData() default "";

    /** 是否生成服务端用的{@code Exporter} */
    boolean genExporter() default true;

    /** 是否生成客户端用的{@code Proxy} */
    boolean genProxy() default true;

}