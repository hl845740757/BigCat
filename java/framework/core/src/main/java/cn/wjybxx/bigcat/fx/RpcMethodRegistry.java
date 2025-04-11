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

import it.unimi.dsi.fastutil.ints.Int2ObjectMap;
import it.unimi.dsi.fastutil.ints.Int2ObjectOpenHashMap;

/**
 * rpc方法信息注册表
 * 1.客户端和服务端的方法都需要注册。
 * 2.整个Node（或进程）一份。
 * 3.禁止服务id重复，一个确定的服务id，对应的服务是确定的。
 * <p>
 * {@link RpcMethodInfo}通常由主线程进行注册，IO线程查询使用，
 * 为保证线程可见性和安全性，主线程在注册完成之后需调用{@link #makeImmutable()}将registry变更为不可变状态（注册完成），
 * IO线程在启动时可调用{@link #ensureImmutable()}检查registry的状态。
 * (另一种方案是通过线程的启动顺序来保证可见性，同时后续禁止修改。)
 *
 * @author wjybxx
 * date - 2023/10/12
 */
public final class RpcMethodRegistry {

    private volatile boolean mutable = true;
    private final Int2ObjectMap<RpcMethodInfo<?, ?>> methodInfoMap = new Int2ObjectOpenHashMap<>(100);

    /** 注册rpc方法 */
    public void register(RpcMethodInfo<?, ?> methodInfo) {
        if (!mutable) {
            throw new IllegalStateException("registry is immutable");
        }
        int methodKey = RpcMethodKey.methodKey(methodInfo.serviceId, methodInfo.methodId);
        RpcMethodInfo<?, ?> exist = methodInfoMap.get(methodKey);
        if (exist == null) {
            methodInfoMap.put(methodKey, methodInfo);
        } else if (!exist.equals(methodInfo)) {
            // 同一个方法被重复注入是安全的，主要处理继承来的方法...
            throw new IllegalStateException("methodKey: %d-%d".formatted(methodInfo.serviceId, methodInfo.methodId));
        }
    }

    /** @return 如果方法不存在，则返回null */
    public RpcMethodInfo<?, ?> getMethodInfo(int serviceId, int methodId) {
        int methodKey = RpcMethodKey.methodKey(serviceId, methodId);
        return methodInfoMap.get(methodKey);
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