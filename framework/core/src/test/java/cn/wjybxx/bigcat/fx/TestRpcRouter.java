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

import cn.wjybxx.base.ThreadUtils;

import java.util.Objects;
import java.util.concurrent.ConcurrentLinkedQueue;

/**
 * @author wjybxx
 * date - 2025/3/14
 */
public class TestRpcRouter extends AbstractRpcRouter {

    private final ConcurrentLinkedQueue<RpcProtocol> protocolQueue = new ConcurrentLinkedQueue<>();
    private volatile boolean shuttingDown;
    private Thread thread;

    @Override
    public void start() {
        thread = new Thread(this::subThreadLoop);
        thread.setName("RpcSender");
        thread.setDaemon(true);
        thread.start();
    }

    @Override
    public void stop() {
        shuttingDown = true;
        thread.interrupt();
    }

    @Override
    public boolean send(RpcProtocol protocol) {
        Objects.requireNonNull(protocol);
        // 这里不执行序列化，但如果已序列化，则进行反序列化
        if (protocol.isBytes()) {
            if (protocol instanceof RpcRequest request) {
                RpcMethodInfo<?, ?> methodInfo = methodRegistry.getMethodInfo(request.getServiceId(), request.getMethodId());
                byte[] bytesParameters = request.getBytes();
                request.setData(serializer.read(bytesParameters, methodInfo.parameterType));
            } else if (protocol instanceof RpcResponse response) {
                RpcMethodInfo<?, ?> methodInfo = methodRegistry.getMethodInfo(response.getServiceId(), response.getMethodId());
                byte[] bytesResults = response.getBytes();
                response.setData(serializer.read(bytesResults, methodInfo.resultType));
            }
        }
        protocolQueue.offer(protocol);
        return true;
    }


    // 该方法为子线程循环，不能在主线程，否则无法支持同步rpc调用
    private void subThreadLoop() {
        RpcProtocol protocol;
        while (!shuttingDown) {
            protocol = protocolQueue.poll();
            if (protocol == null) {
                ThreadUtils.sleepQuietly(1);
                continue;
            }
            if (protocol instanceof RpcRequest request) {
                rpcSupport.onRcvRequest(request);
            } else if (protocol instanceof RpcResponse response) {
                rpcSupport.onRcvResponse(response);
            }
        }
    }

}