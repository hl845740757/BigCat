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
import javax.annotation.Nullable;

/**
 * @author wjybxx
 * date 2023/4/1
 */
public class DefaultRpcProcessor implements RpcMethodProxyRegistry, RpcProcessor {

    /**
     * 所有的Rpc请求处理函数, methodKey -> methodProxy
     */
    private final Int2ObjectMap<RpcMethodProxy> proxyMap = new Int2ObjectOpenHashMap<>(512);

    public DefaultRpcProcessor() {

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

    private static int calMethodKey(short serviceId, short methodId) {
        // 使用乘法更直观，更有规律
        return serviceId * 10000 + methodId;
    }

    public void clear() {
        proxyMap.clear();
    }

    @Override
    public Object process(RpcRequest request) throws Exception {
        @Nullable RpcMethodSpec<?> methodSpec = request.getRpcMethodSpec();
        if (null == methodSpec) {
            throw new IllegalArgumentException(request.getClientNode() + " send null methodSpec");
        }

        if (methodSpec instanceof DefaultRpcMethodSpec) {
            return processImpl(newContext(request), (DefaultRpcMethodSpec<?>) methodSpec);
        } else {
            return process0(request, methodSpec);
        }
    }

    protected RpcProcessContext newContext(RpcRequest request) {
        return new DefaultRpcProcessContext(request);
    }

    protected Object processImpl(@Nonnull RpcProcessContext context, @Nonnull DefaultRpcMethodSpec<?> rpcMethodSpec) {
        final int methodKey = calMethodKey(rpcMethodSpec.getServiceId(), rpcMethodSpec.getMethodId());
        final RpcMethodProxy methodProxy = proxyMap.get(methodKey);
        if (null == methodProxy) {
            final String msg = String.format("rcv unknown request, node %s, serviceId=%d methodId=%d",
                    context.request().getClientNode(), rpcMethodSpec.getServiceId(), rpcMethodSpec.getMethodId());
            throw new RuntimeException(msg);
        }

        try {
            return methodProxy.invoke(context, rpcMethodSpec);
        } catch (Exception e) {
            // 不打印参数的详细信息，开销太大
            final String msg = String.format("invoke caught exception, node %s, serviceId=%d methodId=%d",
                    context.request().getClientNode(), rpcMethodSpec.getServiceId(), rpcMethodSpec.getMethodId());
            throw new RuntimeException(msg, e);
        }
    }

    /**
     * 如果rpc描述信息不是{@link DefaultRpcMethodSpec}对象，那么需要自己实现分发操作
     */
    protected Object process0(RpcRequest request, RpcMethodSpec<?> methodSpec) {
        final String msg = String.format("unknown requestType, node %s, requestType=%s",
                request.getClientNode(), methodSpec.getClass().getName());
        throw new UnsupportedOperationException(msg);
    }

}