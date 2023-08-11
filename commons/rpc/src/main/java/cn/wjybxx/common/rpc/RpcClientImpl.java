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


import cn.wjybxx.common.ThreadUtils;
import cn.wjybxx.common.concurrent.FutureUtils;
import cn.wjybxx.common.concurrent.ICompletableFuture;
import cn.wjybxx.common.concurrent.WatchableEventQueue;
import cn.wjybxx.common.log.DebugLogLevel;
import cn.wjybxx.common.log.DebugLogUtils;
import cn.wjybxx.common.time.TimeProvider;
import it.unimi.dsi.fastutil.longs.Long2ObjectLinkedOpenHashMap;
import org.apache.commons.lang3.exception.ExceptionUtils;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import javax.annotation.concurrent.NotThreadSafe;
import javax.annotation.concurrent.ThreadSafe;
import java.util.Objects;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.ExecutionException;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.TimeoutException;
import java.util.function.BiConsumer;

/**
 * 默认的{@link RpcClient}实现，但仍建议你进行代理封装
 *
 * @author wjybxx
 * date 2023/4/1
 */
@NotThreadSafe
public class RpcClientImpl implements RpcClient {

    private static final Logger logger = LoggerFactory.getLogger(RpcClientImpl.class);
    private static final int INIT_REQUEST_GUID = 0;

    private long requestGuidSequencer = INIT_REQUEST_GUID;
    private final Long2ObjectLinkedOpenHashMap<DefaultRpcRequestStub> requestStubMap = new Long2ObjectLinkedOpenHashMap<>(500);

    private final long processGuid;
    private final NodeId selfNodeId;

    private final RpcSender sender;
    private final RpcReceiver receiver;
    private final RpcRegistry registry;

    private final TimeProvider timeProvider;
    private final long timeoutMs;
    /** 日志级别 */
    private RpcLogConfig rpcLogConfig = RpcLogConfig.NONE;

    /**
     * @param processGuid  当前服务器的进程唯一id（它与{@link NodeId}是两个不同维度的东西）
     * @param selfNodeId   当前服务器的描述信息
     * @param sender       路由实现
     * @param receiver     用于主动接收消息
     * @param registry     rpc调用派发实现
     * @param timeProvider 用于获取当前时间
     * @param timeoutMs    rpc超时时间
     */
    public RpcClientImpl(long processGuid, NodeId selfNodeId,
                         RpcSender sender, RpcReceiver receiver, RpcRegistry registry,
                         TimeProvider timeProvider, long timeoutMs) {
        this.processGuid = processGuid;
        this.selfNodeId = Objects.requireNonNull(selfNodeId);
        this.sender = Objects.requireNonNull(sender);
        this.receiver = Objects.requireNonNull(receiver);
        this.registry = Objects.requireNonNull(registry);
        this.timeProvider = Objects.requireNonNull(timeProvider);
        this.timeoutMs = timeoutMs;
    }

    public RpcLogConfig getRpcLogConfig() {
        return rpcLogConfig;
    }

    public void setRpcLogConfig(RpcLogConfig rpcLogConfig) {
        this.rpcLogConfig = Objects.requireNonNullElse(rpcLogConfig, RpcLogConfig.NONE);
    }

    /**
     * 获取请求的存根信息（用于debug等）
     */
    @Nullable
    public RpcRequestStub getRequestStub(long requestGuid) {
        return requestStubMap.get(requestGuid);
    }

    // region 发送

    /**
     * 发送一个消息给远程
     *
     * @param target     远程节点信息
     * @param methodSpec 要调用的方法信息
     */
    @Override
    public void send(NodeId target, RpcMethodSpec<?> methodSpec) {
        Objects.requireNonNull(target);
        Objects.requireNonNull(methodSpec);

        final long requestGuid = ++requestGuidSequencer;
        final RpcRequest request = new RpcRequest(processGuid, selfNodeId, requestGuid, true, methodSpec);

        if (rpcLogConfig.getSndRequestLogLevel() > DebugLogLevel.NONE) {
            logSndRequest(target, request);
        }

        if (!sender.send(target, request)) {
            logger.warn("rpc router send failure, target " + target);
        }
    }

    /**
     * 广播一个消息给
     *
     * @param scope      远程节点信息
     * @param methodSpec 要调用的方法信息
     */
    @Override
    public void broadcast(NodeScope scope, RpcMethodSpec<?> methodSpec) {
        Objects.requireNonNull(scope);
        Objects.requireNonNull(methodSpec);

        final long requestGuid = ++requestGuidSequencer;
        final RpcRequest request = new RpcRequest(processGuid, selfNodeId, requestGuid, true, methodSpec);

        if (rpcLogConfig.getSndRequestLogLevel() > DebugLogLevel.NONE) {
            logBroadcastRequest(scope, request);
        }

        if (!sender.broadcast(scope, request)) {
            logger.warn("rpc router broadcast failure, scope " + scope);
        }
    }

    /**
     * 发起一个rpc调用，可以监听调用结果。
     *
     * @param target     远程节点信息
     * @param methodSpec 要调用的方法信息
     * @return future，可以监听调用结果
     */
    @Override
    public <V> ICompletableFuture<V> call(NodeId target, RpcMethodSpec<V> methodSpec) {
        Objects.requireNonNull(target);
        Objects.requireNonNull(methodSpec);

        final long requestGuid = ++requestGuidSequencer;
        final RpcRequest request = new RpcRequest(processGuid, selfNodeId, requestGuid, false, methodSpec);

        if (rpcLogConfig.getSndRequestLogLevel() > DebugLogLevel.NONE) {
            logSndRequest(target, request);
        }

        // 执行发送(routerHandler的实现很关键)
        if (!sender.send(target, request)) {
            logger.warn("rpc router call failure, target " + target);
        }
        // 保留存根
        final long deadline = timeProvider.getTime() + timeoutMs;
        final ICompletableFuture<V> promise = FutureUtils.newPromise();
        final DefaultRpcRequestStub requestStub = new DefaultRpcRequestStub(promise, deadline, target, request);
        requestStubMap.put(requestGuid, requestStub);
        return promise;
    }

    /**
     * 发起一个同步rpc调用，阻塞到得到结果或超时
     *
     * @param target     远程节点信息
     * @param methodSpec 要调用的方法信息
     * @return rpc调用结果
     */
    @Override
    public <V> V syncCall(NodeId target, RpcMethodSpec<V> methodSpec) throws InterruptedException {
        return syncCall(target, methodSpec, timeoutMs);
    }

    /**
     * 发起一个同步rpc调用，阻塞到得到结果或超时
     *
     * @param target     远程节点信息
     * @param methodSpec 要调用的方法信息
     * @param timeoutMs  超时时间 - 毫秒
     * @return rpc调用结果
     */
    @Override
    public <V> V syncCall(NodeId target, RpcMethodSpec<V> methodSpec, long timeoutMs) throws InterruptedException {
        Objects.requireNonNull(target);
        Objects.requireNonNull(methodSpec);

        final long requestGuid = ++requestGuidSequencer;
        final RpcRequest request = new RpcRequest(processGuid, selfNodeId, requestGuid, false, methodSpec);

        if (rpcLogConfig.getSndRequestLogLevel() > DebugLogLevel.NONE) {
            logSndRequest(target, request);
        }

        // 必须先watch再发送，否则可能丢失信号
        RpcResponseWatcher watcher = new RpcResponseWatcher(processGuid, requestGuid);
        receiver.watch(watcher);
        try {
            // 执行发送(routerHandler的实现很关键)
            if (!sender.send(target, request)) {
                logger.warn("rpc router call failure, target " + target);
                throw RpcClientException.routeFailed(target);
            }

            RpcResponse response = watcher.future.get(timeoutMs, TimeUnit.MILLISECONDS);
            if (rpcLogConfig.getRcvResponseLogLevel() > DebugLogLevel.NONE) {
                logRcvResponse(response, request);
            }

            final int errorCode = response.getErrorCode();
            if (errorCode == 0) {
                @SuppressWarnings("unchecked") final V result = (V) response.getResult();
                return result;
            } else {
                throw RpcServerException.failed(errorCode, response.getErrorMsg());
            }
        } catch (TimeoutException e) {
            throw RpcClientException.blockingTimeout(e);
        } catch (ExecutionException e) {
            throw RpcClientException.executionException(e);
        } finally {
            receiver.cancelWatch(watcher); // 及时取消watcher
        }
    }

    /**
     * 接收到一个rpc请求
     */
    public void onRcvRequest(RpcRequest request) {
        Objects.requireNonNull(request);
        if (rpcLogConfig.getRcvRequestLogLevel() > DebugLogLevel.NONE) {
            logRcvRequest(request);
        }

        RpcMethodSpec<?> methodSpec = request.getRpcMethodSpec();
        RpcMethodProxy proxy = registry.getProxy(methodSpec.getServiceId(), methodSpec.getMethodId());
        if (proxy == null) {
            logger.warn("rcv unknown request, node {}, serviceId={}, methodId={}",
                    request.getClientNodeId(), methodSpec.getServiceId(), methodSpec.getMethodId());
            if (!request.isOneWay()) {
                RpcResponse response = RpcResponse.newFailedResponse(request.getClientProcessId(), request.getRequestId(), RpcErrorCodes.SERVER_UNSUPPORTED_INTERFACE, "");
                sendResponseAndLog(response, request.getClientNodeId());
            }
            return;
        }

        DefaultRpcProcessContext context = new DefaultRpcProcessContext(request);
        if (request.isOneWay()) {
            // 单向消息 - 不需要结果
            try {
                proxy.invoke(context, methodSpec);
            } catch (Exception e) {
                throw wrapException(request, methodSpec, e);
            }
        } else {
            // rpc
            try {
                final Object result = proxy.invoke(context, methodSpec);
                if (result instanceof CompletableFuture<?> future) {
                    // 未完成，需要监听结果
                    future.whenComplete(new FutureListener<>(request, this));
                } else {
                    // 立即得到了结果
                    final RpcResponse response = RpcResponse.newSucceedResponse(request.getClientProcessId(), request.getRequestId(), result);
                    sendResponseAndLog(response, request.getClientNodeId());
                }
            } catch (Exception e) {
                // 出现异常，立即返回失败
                final RpcResponse response = RpcResponse.newFailedResponse(request.getClientProcessId(), request.getRequestId(),
                        RpcErrorCodes.SERVER_EXCEPTION, ExceptionUtils.getMessage(e));
                sendResponseAndLog(response, request.getClientNodeId());
                // 抛出异常
                throw wrapException(request, methodSpec, e);
            }
        }
    }

    private void sendResponseAndLog(RpcResponse response, NodeId from) {
        if (rpcLogConfig.getSndResponseLogLevel() > DebugLogLevel.NONE) {
            logSndResponse(from, response);
        }

        if (!sender.send(from, response)) {
            logger.warn("rpc send response failure, from {}", from);
        }
    }

    private static RuntimeException wrapException(RpcRequest request, RpcMethodSpec<?> methodSpec, Exception e) {
        String msg = String.format("invoke caught exception, node %s, serviceId=%d, methodId=%d",
                request.getClientNodeId(), methodSpec.getServiceId(), methodSpec.getMethodId());
        return new RuntimeException(msg, e);
    }

    /**
     * 接收到一个rpc调用结果
     */
    public void onRcvResponse(RpcResponse response) {
        Objects.requireNonNull(response);
        if (response.getClientProcessId() != processGuid) {
            // 不是我发起的请求的响应 - 避免造成错误的响应
            logger.info("rcv old process rpc response");
            return;
        }

        final DefaultRpcRequestStub requestStub = requestStubMap.remove(response.getRequestId());
        if (rpcLogConfig.getRcvResponseLogLevel() > DebugLogLevel.NONE) {
            logRcvResponse(response, requestStub);
        }

        if (requestStub != null) {
            final int errorCode = response.getErrorCode();
            if (errorCode == 0) {
                @SuppressWarnings("unchecked") final ICompletableFuture<Object> promise = (ICompletableFuture<Object>) requestStub.rpcPromise;
                promise.complete(response.getResult());
            } else {
                requestStub.rpcPromise.completeExceptionally(RpcServerException.failed(errorCode, response.getErrorMsg()));
            }
        }
    }

    // endregion

    //

    /**
     * 服务器需要每帧调用该方法，以检测超时等
     */
    public void tick() {
        // 并不需要检查全部的rpc请求，只要第一个未超时，即可停止。
        // 因为我们没打算支持每个请求单独设立超时时间，且我们的map是保持插入序的，因此先发送的请求一定先超时
        final long curTime = timeProvider.getTime();
        while (requestStubMap.size() > 0) {
            final long requestGuid = requestStubMap.firstLongKey();
            final DefaultRpcRequestStub requestStub = requestStubMap.get(requestGuid);
            if (curTime < requestStub.deadline) {
                return;
            }

            logger.warn("rpc timeout, requestGuid {}, target {}", requestGuid, requestStub.target);
            requestStubMap.removeFirst();
            requestStub.rpcPromise.completeExceptionally(RpcClientException.timeout());
        }
    }

    /**
     * 清除所有的rpc调用（慎重调用）
     */
    public void clear() {
        requestStubMap.clear();
    }

    //

    // region 内部实现

    @ThreadSafe
    private static class RpcResponseWatcher implements WatchableEventQueue.Watcher<RpcResponse> {

        private final CompletableFuture<RpcResponse> future = new CompletableFuture<>();
        private final long processGuid;
        private final long requestId;

        private RpcResponseWatcher(long processGuid, long requestId) {
            this.processGuid = processGuid;
            this.requestId = requestId;
        }

        @Override
        public boolean test(@Nonnull RpcResponse response) {
            return response.getClientProcessId() == processGuid
                    && response.getRequestId() == requestId;
        }

        @Override
        public void onEvent(@Nonnull RpcResponse response) {
            future.complete(response);
        }
    }

    private static class FutureListener<V> implements BiConsumer<V, Throwable> {

        final long fromProcessGuid;
        final NodeId from;
        final long requestGuid;
        final RpcClientImpl rpcClientImpl;

        FutureListener(RpcRequest request, RpcClientImpl rpcClientImpl) {
            // Q: 为什么不直接持有{@link RpcRequest}对象？
            // A: 会造成内存泄漏！会持有本不需要的{@link RpcMethodSpec}对象。
            this.fromProcessGuid = request.getClientProcessId();
            this.requestGuid = request.getRequestId();
            this.from = request.getClientNodeId();
            this.rpcClientImpl = rpcClientImpl;
        }

        @Override
        public void accept(V v, Throwable throwable) {
            final RpcResponse response;
            if (throwable != null) {
                // 异常信息已由上游打印，这里只打印rpc信息
                logger.warn("rpc execute caught exception, fromProcessGuid: {}, from: {}, requestGuid: {}", fromProcessGuid, from, requestGuid);
                ThreadUtils.recoveryInterrupted(throwable);
                response = RpcResponse.newFailedResponse(fromProcessGuid, requestGuid, RpcErrorCodes.SERVER_EXCEPTION, ExceptionUtils.getMessage(throwable));
            } else {
                response = RpcResponse.newSucceedResponse(fromProcessGuid, requestGuid, v);
            }
            rpcClientImpl.sendResponseAndLog(response, from);
        }
    }

    private static class DefaultRpcRequestStub implements RpcRequestStub {

        final ICompletableFuture<?> rpcPromise;
        final long deadline;
        final NodeId target;
        final RpcRequest request;

        DefaultRpcRequestStub(ICompletableFuture<?> rpcPromise, long deadline, NodeId target, RpcRequest request) {
            this.rpcPromise = rpcPromise;
            this.deadline = deadline;
            this.target = target;
            this.request = request;
        }

        @Override
        public long getDeadline() {
            return deadline;
        }

        @Override
        public NodeId getTargetNode() {
            return target;
        }

        @Override
        public RpcRequest getRequest() {
            return request;
        }
    }

    // endregion

    // region debug日志

    private void logSndRequest(NodeId target, RpcRequest request) {
        logger.info("snd rpc request, target {}, request {}", target, DebugLogUtils.logOf(rpcLogConfig.getSndRequestLogLevel(), request));
    }

    private void logSndResponse(NodeId from, RpcResponse rpcResponse) {
        logger.info("snd rpc response, from {}, response {}", from, DebugLogUtils.logOf(rpcLogConfig.getSndResponseLogLevel(), rpcResponse));
    }

    private void logBroadcastRequest(NodeScope scope, RpcRequest request) {
        logger.info("broadcast rpc request, scope {}, request {}", scope, DebugLogUtils.logOf(rpcLogConfig.getSndRequestLogLevel(), request));
    }

    private void logRcvRequest(RpcRequest request) {
        logger.info("rcv rpc request, request {}", DebugLogUtils.logOf(rpcLogConfig.getRcvRequestLogLevel(), request));
    }

    private void logRcvResponse(RpcResponse rpcResponse, DefaultRpcRequestStub requestStub) {
        if (null == requestStub) {
            logger.info("rcv rpc response, but request is timeout, response {}",
                    DebugLogUtils.logOf(rpcLogConfig.getRcvResponseLogLevel(), rpcResponse));
        } else {
            logger.info("rcv rpc response, requestStub {}, response {}",
                    DebugLogUtils.logOf(rpcLogConfig.getRcvResponseLogLevel(), requestStub),
                    DebugLogUtils.logOf(rpcLogConfig.getRcvResponseLogLevel(), rpcResponse));
        }
    }

    private void logRcvResponse(RpcResponse rpcResponse, RpcRequest request) {
        logger.info("rcv rpc response, request {}, response {}",
                DebugLogUtils.logOf(rpcLogConfig.getRcvResponseLogLevel(), request),
                DebugLogUtils.logOf(rpcLogConfig.getRcvResponseLogLevel(), rpcResponse));
    }

    // endregion
}