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

import javax.annotation.Nonnull;

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
     */
    void register(short serviceId, short methodId, @Nonnull RpcMethodProxy proxy);

    /** 受信任的注册方法，可用于覆盖之前绑定的代理方法 */
    void trustedRegister(short serviceId, short methodId, @Nonnull RpcMethodProxy proxy);

    /**
     * 查询方法绑定的Proxy
     *
     * @param serviceId 服务id
     * @param methodId  方法id
     * @return 如果不存在，则返回null
     */
    RpcMethodProxy getProxy(short serviceId, short methodId);

    /**
     * 清理注册表
     * 因为{@link #register(short, short, RpcMethodProxy)}会捕获太多对象，
     * 当不再使用{@link RpcRegistry}时，执行该方法可释放{@link RpcMethodProxy}捕获的对象。
     */
    void clear();

}