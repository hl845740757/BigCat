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
import it.unimi.dsi.fastutil.ints.IntOpenHashSet;
import it.unimi.dsi.fastutil.ints.IntSet;

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

    @Override
    public void register(int serviceId, int methodId, @Nonnull RpcMethodProxy proxy) {
        final int methodKey = RpcMethodKey.methodKey(serviceId, methodId);
        if (proxyMap.containsKey(methodKey)) {
            throw new IllegalArgumentException("methodKey is duplicate, serviceId: %d, methodId: %d"
                    .formatted(serviceId, methodId));
        }
        proxyMap.put(methodKey, proxy);
    }

    @Override
    public void setProxyData(int serviceId, int methodId, String customData) {

    }

    @Override
    public RpcMethodProxy getProxy(int serviceId, int methodId) {
        final int methodKey = RpcMethodKey.methodKey(serviceId, methodId);
        return proxyMap.get(methodKey);
    }

    @Override
    public RpcMethodProxy removeProxy(int serviceId, int methodId) {
        final int methodKey = RpcMethodKey.methodKey(serviceId, methodId);
        return proxyMap.remove(methodKey);
    }

    @Override
    public boolean hasProxy(int serviceId, int methodId) {
        final int methodKey = RpcMethodKey.methodKey(serviceId, methodId);
        return proxyMap.containsKey(methodKey);
    }

    @Override
    public void setDefaultProxy(RpcMethodProxy defaultProxy) {
        proxyMap.defaultReturnValue(defaultProxy);
    }

    @Override
    public RpcMethodProxy getDefaultProxy() {
        return proxyMap.defaultReturnValue();
    }

    @Override
    public IntSet export() {
        IntOpenHashSet result = new IntOpenHashSet(10);
        proxyMap.keySet().intStream()
                .map(RpcMethodKey::serviceIdOfKey)
                .forEach(result::add);
        return result;
    }

    public void clear() {
        proxyMap.clear();
    }

}