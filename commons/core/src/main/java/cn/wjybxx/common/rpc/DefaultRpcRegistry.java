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

import it.unimi.dsi.fastutil.ints.Int2ObjectMap;
import it.unimi.dsi.fastutil.ints.Int2ObjectOpenHashMap;

import javax.annotation.Nonnull;

/**
 * @author wjybxx
 * date 2023/4/1
 */
public class DefaultRpcRegistry implements RpcRegistry {

    /**
     * 所有的Rpc请求处理函数, methodKey -> methodProxy
     */
    private final Int2ObjectMap<RpcMethodProxy> proxyMap = new Int2ObjectOpenHashMap<>(512);

    private static int calMethodKey(short serviceId, short methodId) {
        // 使用乘法更直观，更有规律
        return serviceId * 10000 + methodId;
    }

    @Override
    public void register(short serviceId, short methodId, @Nonnull RpcMethodProxy proxy) {
        // rpc请求id不可以重复
        final int methodKey = calMethodKey(serviceId, methodId);
        if (proxyMap.containsKey(methodKey)) {
            throw new IllegalArgumentException("methodKey " + methodKey + " is already registered!");
        }
        proxyMap.put(methodKey, proxy);
    }

    @Override
    public void trustedRegister(short serviceId, short methodId, @Nonnull RpcMethodProxy proxy) {
        final int methodKey = calMethodKey(serviceId, methodId);
        proxyMap.put(methodKey, proxy);
    }

    @Override
    public RpcMethodProxy getProxy(short serviceId, short methodId) {
        final int methodKey = calMethodKey(serviceId, methodId);
        return proxyMap.get(methodKey);
    }

    public void clear() {
        proxyMap.clear();
    }

}