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

import it.unimi.dsi.fastutil.ints.*;

import javax.annotation.Nonnull;
import java.util.Objects;

/**
 * 子类可重写{@link #setProxyData(int, int, Object)}提前解析数据
 *
 * @author wjybxx
 * date 2023/4/1
 */
public class DefaultRpcProxyRegistry implements RpcProxyRegistry {

    /**
     * 所有的Rpc请求处理函数, methodKey -> methodProxy
     */
    private final Int2ObjectMap<RpcMethodProxy<?>> proxyMap = new Int2ObjectOpenHashMap<>(512);
    private final Int2ObjectMap<Boolean> disabledMap = new Int2ObjectOpenHashMap<>();
    private final Int2ObjectMap<Object> proxyDataMap = new Int2ObjectOpenHashMap<>(512);

    @Override
    public <T> void register(int serviceId, int methodId, @Nonnull RpcMethodProxy<T> proxy) {
        Objects.requireNonNull(proxy, "proxy");
        final int methodKey = RpcMethodKey.methodKey(serviceId, methodId);
        if (proxyMap.containsKey(methodKey)) {
            throw new IllegalArgumentException("methodKey is duplicate, serviceId: %d, methodId: %d"
                    .formatted(serviceId, methodId));
        }
        proxyMap.put(methodKey, proxy);
    }

    @Override
    public void setProxyData(int serviceId, int methodId, Object customData) {
        final int methodKey = RpcMethodKey.methodKey(serviceId, methodId);
        if (customData == null) {
            proxyDataMap.remove(methodKey);
        } else {
            proxyDataMap.put(methodKey, customData);
        }
    }

    @Override
    public Object getProxyData(int serviceId, int methodId) {
        final int methodKey = RpcMethodKey.methodKey(serviceId, methodId);
        return proxyDataMap.get(methodKey);
    }

    @Override
    public RpcMethodProxy<?> getProxy(int serviceId, int methodId) {
        final int methodKey = RpcMethodKey.methodKey(serviceId, methodId);
        return proxyMap.get(methodKey);
    }

    @Override
    public RpcMethodProxy<?> removeProxy(int serviceId, int methodId) {
        final int methodKey = RpcMethodKey.methodKey(serviceId, methodId);
        return proxyMap.remove(methodKey);
    }

    @Override
    public void disable(int serviceId, int methodId) {
        if (methodId == -1) {
            for (var pair : proxyMap.int2ObjectEntrySet()) {
                if (RpcMethodKey.serviceIdOfKey(pair.getIntKey()) == serviceId) {
                    disabledMap.put(pair.getIntKey(), Boolean.TRUE);
                }
            }
        } else {
            int key = RpcMethodKey.methodKey(serviceId, methodId);
            disabledMap.put(key, Boolean.TRUE);
        }
    }

    @Override
    public void enable(int serviceId, int methodId) {
        if (methodId == -1) {
            IntList keys = new IntArrayList();
            for (var pair : disabledMap.int2ObjectEntrySet()) {
                if (RpcMethodKey.serviceIdOfKey(pair.getIntKey()) == serviceId) {
                    keys.add(pair.getIntKey());
                }
            }
            keys.forEach(disabledMap::remove);
        } else {
            int key = RpcMethodKey.methodKey(serviceId, methodId);
            disabledMap.remove(key);
        }
    }

    @Override
    public boolean isDisabled(int serviceId, int methodId) {
        int key = RpcMethodKey.methodKey(serviceId, methodId);
        return disabledMap.containsKey(key);
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
        proxyDataMap.clear();
    }

}