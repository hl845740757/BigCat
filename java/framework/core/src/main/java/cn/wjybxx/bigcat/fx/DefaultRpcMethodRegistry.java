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
 * <p>
 * {@link RpcMethodInfo}通常由主线程进行注册，IO线程查询使用，
 * 为保证线程可见性和安全性，主线程在注册完成之后需调用{@link #makeImmutable()}将registry变更为不可变状态（注册完成），
 * IO线程在启动时可调用{@link #ensureImmutable()}检查registry的状态。
 * (另一种方案是通过线程的启动顺序来保证可见性，同时后续禁止修改。)
 *
 * @author wjybxx
 * date 2023/4/1
 */
public class DefaultRpcMethodRegistry implements RpcMethodRegistry {

    private final Int2ObjectMap<RpcMethodInfo> methodInfoMap = new Int2ObjectOpenHashMap<>(512);
    private final Int2ObjectMap<RpcMethodProxy<?>> proxyMap = new Int2ObjectOpenHashMap<>(512);
    private final Int2ObjectMap<Boolean> disabledMap = new Int2ObjectOpenHashMap<>();
    private final Int2ObjectMap<Object> proxyDataMap = new Int2ObjectOpenHashMap<>(512);
    private volatile boolean mutable = true;

    /** 注册rpc方法 */
    @Override
    public void register(RpcMethodInfo methodInfo) {
        if (!mutable) {
            throw new IllegalStateException("registry is immutable");
        }
        int methodKey = RpcMethodKey.methodKey(methodInfo.serviceId, methodInfo.methodId);
        RpcMethodInfo exist = methodInfoMap.get(methodKey);
        if (exist == null) {
            methodInfoMap.put(methodKey, methodInfo);
        } else if (!exist.equals(methodInfo)) {
            // 同一个方法被重复注入是安全的，主要处理继承来的方法...
            throw new IllegalStateException("methodKey: %d-%d".formatted(methodInfo.serviceId, methodInfo.methodId));
        }
    }

    /** @return 如果方法不存在，则返回null */
    @Override
    public RpcMethodInfo getMethodInfo(int serviceId, int methodId) {
        int methodKey = RpcMethodKey.methodKey(serviceId, methodId);
        return methodInfoMap.get(methodKey);
    }

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

    /** 当前是否处于可变状态 */
    public boolean isMutable() {
        return mutable;
    }

    /**
     * 设置为不可变(主线程注册完毕后调用)
     * 1.设置为不可变后，不再可增删MethodInfo和MethodProxy，切面数据可以动态变更
     * 2.建议执行该方法
     */
    public void makeImmutable() {
        mutable = true;
    }

    /** 检查是否处于不可变状态(IO线程启动时调用) */
    public void ensureImmutable() {
        if (mutable) {
            throw new IllegalStateException("registry is mutable");
        }
    }

    /** 检查是否处于可变状态(主线程检测) */
    public void ensureMutable() {
        if (!mutable) {
            throw new IllegalStateException("registry is immutable");
        }
    }
}