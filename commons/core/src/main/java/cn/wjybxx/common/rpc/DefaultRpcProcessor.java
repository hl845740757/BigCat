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
import javax.annotation.Nullable;

/**
 * @author wjybxx
 * date 2023/4/1
 */
public class DefaultRpcProcessor extends DefaultRpcRegistry implements RpcProcessor {

    public DefaultRpcProcessor() {

    }

    @Override
    public Object process(RpcRequest request) throws Exception {
        @Nullable RpcMethodSpec<?> methodSpec = request.getRpcMethodSpec();
        if (null == methodSpec) {
            throw new IllegalArgumentException(request.getClientNodeId() + " send null methodSpec");
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
        final RpcMethodProxy methodProxy = getProxy(rpcMethodSpec.getServiceId(), rpcMethodSpec.getMethodId());
        if (null == methodProxy) {
            final String msg = String.format("rcv unknown request, node %s, serviceId=%d methodId=%d",
                    context.request().getClientNodeId(), rpcMethodSpec.getServiceId(), rpcMethodSpec.getMethodId());
            throw new RuntimeException(msg);
        }

        try {
            return methodProxy.invoke(context, rpcMethodSpec);
        } catch (Exception e) {
            // 不打印参数的详细信息，开销太大
            final String msg = String.format("invoke caught exception, node %s, serviceId=%d methodId=%d",
                    context.request().getClientNodeId(), rpcMethodSpec.getServiceId(), rpcMethodSpec.getMethodId());
            throw new RuntimeException(msg, e);
        }
    }

    /**
     * 如果rpc描述信息不是{@link DefaultRpcMethodSpec}对象，那么需要自己实现分发操作
     */
    protected Object process0(RpcRequest request, RpcMethodSpec<?> methodSpec) {
        final String msg = String.format("unknown requestType, node %s, requestType=%s",
                request.getClientNodeId(), methodSpec.getClass().getName());
        throw new UnsupportedOperationException(msg);
    }

}