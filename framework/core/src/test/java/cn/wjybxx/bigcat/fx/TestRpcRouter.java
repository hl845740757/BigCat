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

import java.util.ArrayDeque;
import java.util.Objects;

/**
 * @author wjybxx
 * date - 2025/3/14
 */
public class TestRpcRouter extends AbstractRpcRouter {

    private final ArrayDeque<RpcProtocol> protocolQueue = new ArrayDeque<>();

    @Override
    public void inject(Worker worker) {
        super.inject(worker);
//        rpcSupport.setEnableLog(true);
    }

    @Override
    public void start() {
    }

    @Override
    public void update() throws Exception {
        RpcProtocol protocol;
        while ((protocol = protocolQueue.poll()) != null) {
            if (protocol instanceof RpcRequest request) {
                rpcSupport.onRcvRequest(request);
            } else if (protocol instanceof RpcResponse response) {
                rpcSupport.onRcvResponse(response);
            }
        }
    }

    @Override
    public void stop() {
        protocolQueue.clear();
    }

    @Override
    public boolean send(RpcProtocol protocol) {
        assert node.inEventLoop() : "node.inEventLoop()";
        Objects.requireNonNull(protocol);
        // 这里不执行序列化，但如果已序列化，则进行反序列化
        if (protocol.isBytes()) {
            if (protocol instanceof RpcRequest request) {
                RpcMethodInfo<?, ?> methodInfo = methodRegistry.getMethodInfo(request.getServiceId(), request.getMethodId());
                Object parameter = serializer.read(request.getBytes(), methodInfo.parameterType);
                request.setData(parameter);
            } else if (protocol instanceof RpcResponse response) {
                RpcMethodInfo<?, ?> methodInfo = methodRegistry.getMethodInfo(response.getServiceId(), response.getMethodId());
                Object result = serializer.read(response.getBytes(), methodInfo.resultType);
                response.setData(result);
            }
        }
        protocolQueue.offer(protocol);
        return true;
    }

}