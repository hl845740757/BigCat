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
import it.unimi.dsi.fastutil.ints.IntSet;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;

/**
 * Rpc方法代理注册表
 *
 * @author wjybxx
 * date 2023/4/1
 */
public interface RpcRegistry {

    /**
     * 注册一个rpc请求处理函数
     *
     * @param serviceId 服务id
     * @param methodId  方法id
     * @param proxy     代理方法
     * @throws IllegalArgumentException 如果已存在对应的proxy，则抛出异常
     */
    @StableName
    void register(int serviceId, int methodId, @Nonnull RpcMethodProxy proxy);

    /**
     * 设置代理的切面数据
     * 由于一个方法只能由一个proxy，因此切面数据可以独立注册
     *
     * @param serviceId  服务id
     * @param methodId   方法id
     * @param customData 自定义切面数据；也可用于指示是否可覆盖；若为null则表示删除
     */
    @StableName
    void setProxyData(int serviceId, int methodId, String customData);

    /**
     * 注册一个rpc请求处理函数
     *
     * @param serviceId  服务id
     * @param methodId   方法id
     * @param proxy      代理方法
     * @param customData 自定义切面数据；也可用于指示是否可覆盖
     */
    @StableName
    default void register(int serviceId, int methodId, @Nonnull RpcMethodProxy proxy,
                          @Nullable String customData) {
        register(serviceId, methodId, proxy);
        setProxyData(serviceId, methodId, customData);
    }

    /**
     * 查询方法绑定的Proxy
     *
     * @param serviceId 服务id
     * @param methodId  方法id
     * @return 如果不存在，则返回null
     */
    RpcMethodProxy getProxy(int serviceId, int methodId);

    /**
     * 删除指定方法的Proxy
     * 在删除后可重新注册，通常用于覆盖特定方法的proxy
     *
     * @param serviceId 服务id
     * @param methodId  方法id
     * @return 如果不存在，则返回null
     */
    RpcMethodProxy removeProxy(int serviceId, int methodId);

    /**
     * 由于{@link #getProxy(int, int)}可能返回默认的proxy，因此不能根据是否为null判断是否存在服务
     */
    boolean hasProxy(int serviceId, int methodId);

    /** 设置默认的proxy */
    void setDefaultProxy(RpcMethodProxy defaultProxy);

    RpcMethodProxy getDefaultProxy();

    /**
     * 导出注册表中包含的服务
     *
     * @return 注册的所有服务的id
     */
    IntSet export();

    /**
     * 清理注册表
     * 因为{@link #register(int, int, RpcMethodProxy)}会捕获太多对象，
     * 当不再使用{@link RpcRegistry}时，执行该方法可释放{@link RpcMethodProxy}捕获的对象。
     */
    void clear();

}