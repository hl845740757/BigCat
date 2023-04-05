/*
 * Copyright 2023 wjybxx
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

package cn.wjybxx.bigcat.common.rpc;


import cn.wjybxx.bigcat.common.ThreadUtils;
import cn.wjybxx.bigcat.common.async.FluentFuture;
import cn.wjybxx.bigcat.common.async.FluentPromise;
import cn.wjybxx.bigcat.common.async.FutureUtils;
import cn.wjybxx.bigcat.common.concurrent.WatchableEventQueue;
import cn.wjybxx.bigcat.common.log.DebugLogLevel;
import cn.wjybxx.bigcat.common.log.DebugLogUtils;
import cn.wjybxx.bigcat.common.time.TimeProvider;
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
 * 提供Rpc调用支持的handler。
 *
 * @author wjybxx
 * date 2023/4/1
 */
@NotThreadSafe
public class RpcSupportHandler {

    private static final Logger logger = LoggerFactory.getLogger(RpcSupportHandler.class);
    private static final int INIT_REQUEST_GUID = 0;

    private long requestGuidSequencer = INIT_REQUEST_GUID;
    private final Long2ObjectLinkedOpenHashMap<DefaultRpcRequestStub> requestStubMap = new Long2ObjectLinkedOpenHashMap<>(500);

    private final long processGuid;
    private final NodeSpec selfNodeSpec;

    private final RpcRouterHandler rpcRouterHandler;
    private final RpcReceiverHandler rpcReceiverHandler;
    private final RpcProcessor rpcProcessor;

    private final TimeProvider timeProvider;
    private final long timeoutMs;

    /** 异步调用时，发送消息失败是否立即触发调用失败，如果为true，对应用来讲，会导致future的完成时间不可控 */
    private boolean immediateFailureWhenSendFailed = false;
    /** 日志级别 */
    private RpcLogConfig rpcLogConfig = RpcLogConfig.NONE;

    /**
     * @param processGuid        当前服务器的进程唯一id（它与{@link NodeSpec}是两个不同维度的东西）
     * @param selfNodeSpec       当前服务器的描述信息
     * @param rpcRouterHandler   路由实现
     * @param rpcReceiverHandler 用于主动接收消息
     * @param rpcProcessor       rpc调用派发实现
     * @param timeProvider       用于获取当前时间
     * @param timeoutMs          rpc超时时间
     */
    public RpcSupportHandler(long processGuid, NodeSpec selfNodeSpec,
                             RpcRouterHandler rpcRouterHandler, RpcReceiverHandler rpcReceiverHandler, RpcProcessor rpcProcessor,
                             TimeProvider timeProvider, long timeoutMs) {
        this.processGuid = processGuid;
        this.selfNodeSpec = Objects.requireNonNull(selfNodeSpec);
        this.rpcRouterHandler = Objects.requireNonNull(rpcRouterHandler);
        this.rpcReceiverHandler = Objects.requireNonNull(rpcReceiverHandler);
        this.rpcProcessor = Objects.requireNonNull(rpcProcessor);
        this.timeProvider = Objects.requireNonNull(timeProvider);
        this.timeoutMs = timeoutMs;
    }

    public boolean isImmediateFailureWhenSendFailed() {
        return immediateFailureWhenSendFailed;
    }

    /**
     * 如果为true，则发送失败时立即失败，future上的回调会立即执行。
     * 注意：如果设置为true，则会有一定的时序影响，会产生后发送的先失败这么个情况。
     */
    public void setImmediateFailureWhenSendFailed(boolean immediateFailureWhenSendFailed) {
        this.immediateFailureWhenSendFailed = immediateFailureWhenSendFailed;
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
    public void send(NodeSpec target, RpcMethodSpec<?> methodSpec) {
        Objects.requireNonNull(target);
        Objects.requireNonNull(methodSpec);

        final long requestGuid = ++requestGuidSequencer;
        final RpcRequest request = new RpcRequest(processGuid, selfNodeSpec, requestGuid, true, methodSpec);

        if (rpcLogConfig.getSndRequestLogLevel() > DebugLogLevel.NONE) {
            logSndRequestLog(target, request);
        }

        if (!rpcRouterHandler.send(target, request)) {
            logger.warn("rpc router send failure, target " + target);
        }
    }

    /**
     * 广播一个消息给
     *
     * @param scope      远程节点信息
     * @param methodSpec 要调用的方法信息
     */
    public void broadcast(ScopeSpec scope, RpcMethodSpec<?> methodSpec) {
        Objects.requireNonNull(scope);
        Objects.requireNonNull(methodSpec);

        final long requestGuid = ++requestGuidSequencer;
        final RpcRequest request = new RpcRequest(processGuid, selfNodeSpec, requestGuid, true, methodSpec);

        if (rpcLogConfig.getSndRequestLogLevel() > DebugLogLevel.NONE) {
            logBroadcastRequestLog(scope, request);
        }

        if (!rpcRouterHandler.broadcast(scope, request)) {
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
    public <V> FluentFuture<V> call(NodeSpec target, RpcMethodSpec<V> methodSpec) {
        Objects.requireNonNull(target);
        Objects.requireNonNull(methodSpec);

        final long requestGuid = ++requestGuidSequencer;
        final RpcRequest request = new RpcRequest(processGuid, selfNodeSpec, requestGuid, false, methodSpec);

        if (rpcLogConfig.getSndRequestLogLevel() > DebugLogLevel.NONE) {
            logSndRequestLog(target, request);
        }

        // 执行发送(routerHandler的实现很关键)
        if (!rpcRouterHandler.send(target, request)) {
            logger.warn("rpc router call failure, target " + target);
            if (immediateFailureWhenSendFailed) {
                return FutureUtils.newFailedFuture(RpcClientException.routeFailed(target));
            }
        }

        // 记录调用信息
        final FluentPromise<V> promise = FutureUtils.newPromise();
        final long deadline = timeProvider.getTime() + timeoutMs;
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
    public <V> V syncCall(NodeSpec target, RpcMethodSpec<V> methodSpec) throws InterruptedException {
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
    public <V> V syncCall(NodeSpec target, RpcMethodSpec<V> methodSpec, long timeoutMs) throws InterruptedException {
        Objects.requireNonNull(target);
        Objects.requireNonNull(methodSpec);

        final long requestGuid = ++requestGuidSequencer;
        final RpcRequest request = new RpcRequest(processGuid, selfNodeSpec, requestGuid, false, methodSpec);

        if (rpcLogConfig.getSndRequestLogLevel() > DebugLogLevel.NONE) {
            logSndRequestLog(target, request);
        }

        // 必须先watch再发送，否则可能丢失信号
        RpcResponseWatcher watcher = new RpcResponseWatcher(processGuid, requestGuid);
        rpcReceiverHandler.watch(watcher);

        // 执行发送(routerHandler的实现很关键)
        if (!rpcRouterHandler.send(target, request)) {
            logger.warn("rpc router call failure, target " + target);
            throw RpcClientException.routeFailed(target);
        }
        try {
            RpcResponse response = watcher.future.get(timeoutMs, TimeUnit.MILLISECONDS);
            if (rpcLogConfig.getRcvResponseLogLevel() > DebugLogLevel.NONE) {
                logRcvResponseLog(response, request);
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
        }
    }

    /**
     * 接收到一个rpc请求
     */
    public void onRcvRequest(RpcRequest request) {
        Objects.requireNonNull(request);
        if (rpcLogConfig.getRcvRequestLogLevel() > DebugLogLevel.NONE) {
            logRcvRequestLog(request);
        }

        final DefaultRpcProcessContext context = new DefaultRpcProcessContext(request);
        if (request.isOneWay()) {
            // 单向消息 - 不需要结果
            try {
                rpcProcessor.process(context);
            } catch (Exception e) {
                ExceptionUtils.rethrow(e);
            }
        } else {
            // rpc
            try {
                final Object result = rpcProcessor.process(context);
                if (result instanceof FluentFuture<?> future) {
                    // 当前任务不能立即完成
                    future.addListener(new FutureListener<>(request, this));
                } else {
                    // 立即得到了结果
                    final RpcResponse response = RpcResponse.newSucceedResponse(request.getClientNodeGuid(), request.getRequestGuid(), result);
                    sendResponseAndLog(response, request.getClientNode());
                }
            } catch (Exception e) {
                // 出现异常，立即返回失败
                final RpcResponse response = RpcResponse.newFailedResponse(request.getClientNodeGuid(), request.getRequestGuid(),
                        RpcErrorCodes.SERVER_EXCEPTION, ExceptionUtils.getMessage(e));
                sendResponseAndLog(response, request.getClientNode());
                // 抛出异常
                ExceptionUtils.rethrow(e);
            }
        }
    }

    private void sendResponseAndLog(RpcResponse response, NodeSpec from) {
        if (rpcLogConfig.getSndResponseLogLevel() > DebugLogLevel.NONE) {
            logSndResponseLog(from, response);
        }

        if (!rpcRouterHandler.send(from, response)) {
            logger.warn("rpc send response failure, from {}", from);
        }
    }

    /**
     * 接收到一个rpc调用结果
     */
    public void onRcvResponse(RpcResponse response) {
        Objects.requireNonNull(response);
        if (response.getClientNodeGuid() != processGuid) {
            // 不是我发起的请求的响应 - 避免造成错误的响应
            logger.info("rcv old process rpc response");
            return;
        }

        final DefaultRpcRequestStub requestStub = requestStubMap.remove(response.getRequestGuid());
        if (rpcLogConfig.getRcvResponseLogLevel() > DebugLogLevel.NONE) {
            logRcvResponseLog(response, requestStub);
        }

        if (requestStub != null) {
            final int errorCode = response.getErrorCode();
            if (errorCode == 0) {
                @SuppressWarnings("unchecked") final FluentPromise<Object> promise = (FluentPromise<Object>) requestStub.rpcPromise;
                promise.trySuccess(response.getResult());
            } else {
                requestStub.rpcPromise.tryFailure(RpcServerException.failed(errorCode, response.getErrorMsg()));
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
            requestStub.rpcPromise.tryFailure(RpcClientException.timeout());
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
        private final long reqGuid;

        private RpcResponseWatcher(long processGuid, long reqGuid) {
            this.processGuid = processGuid;
            this.reqGuid = reqGuid;
        }

        @Override
        public boolean test(@Nonnull RpcResponse response) {
            return response.getClientNodeGuid() == processGuid
                    && response.getRequestGuid() == reqGuid;
        }

        @Override
        public void onEvent(@Nonnull RpcResponse response) {
            future.complete(response);
        }
    }

    private static class FutureListener<V> implements BiConsumer<V, Throwable> {

        final long fromProcessGuid;
        final NodeSpec from;
        final long requestGuid;
        final RpcSupportHandler rpcSupportHandler;

        FutureListener(RpcRequest request, RpcSupportHandler rpcSupportHandler) {
            // Q: 为什么不直接持有{@link RpcRequest}对象？
            // A: 会造成内存泄漏！会持有本不需要的{@link RpcMethodSpec}对象。
            this.fromProcessGuid = request.getClientNodeGuid();
            this.requestGuid = request.getRequestGuid();
            this.from = request.getClientNode();
            this.rpcSupportHandler = rpcSupportHandler;
        }

        @Override
        public void accept(V v, Throwable throwable) {
            final RpcResponse response;
            if (throwable != null) {
                // 这里由于不能抛出异常（会被捕获），因此需要记录日志(记录日志比立即返回结果更重要)
                logger.warn("rpc execute caught exception, fromProcessGuid: {}, from: {}, requestGuid: {}", fromProcessGuid, from, requestGuid, throwable);
                ThreadUtils.recoveryInterrupted(throwable);
                response = RpcResponse.newFailedResponse(fromProcessGuid, requestGuid, RpcErrorCodes.SERVER_EXCEPTION, ExceptionUtils.getMessage(throwable));
            } else {
                response = RpcResponse.newSucceedResponse(fromProcessGuid, requestGuid, v);
            }
            rpcSupportHandler.sendResponseAndLog(response, from);
        }
    }

    private static class DefaultRpcRequestStub implements RpcRequestStub {

        final FluentPromise<?> rpcPromise;
        final long deadline;
        final NodeSpec target;
        final RpcRequest request;

        DefaultRpcRequestStub(FluentPromise<?> rpcPromise, long deadline, NodeSpec target, RpcRequest request) {
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
        public NodeSpec getTargetNode() {
            return target;
        }

        @Override
        public RpcRequest getRequest() {
            return request;
        }
    }

    // endregion

    // region debug日志

    private void logSndRequestLog(NodeSpec target, RpcRequest request) {
        logger.info("snd rpc request, target {}, request {}", target, DebugLogUtils.logOf(rpcLogConfig.getSndRequestLogLevel(), request));
    }

    private void logSndResponseLog(NodeSpec from, RpcResponse rpcResponse) {
        logger.info("snd rpc response, from {}, response {}", from, DebugLogUtils.logOf(rpcLogConfig.getSndResponseLogLevel(), rpcResponse));
    }

    private void logBroadcastRequestLog(ScopeSpec scope, RpcRequest request) {
        logger.info("broadcast rpc request, scope {}, request {}", scope, DebugLogUtils.logOf(rpcLogConfig.getSndRequestLogLevel(), request));
    }

    private void logRcvRequestLog(RpcRequest request) {
        logger.info("rcv rpc request, request {}", DebugLogUtils.logOf(rpcLogConfig.getRcvRequestLogLevel(), request));
    }

    private void logRcvResponseLog(RpcResponse rpcResponse, DefaultRpcRequestStub requestStub) {
        if (null == requestStub) {
            logger.info("rcv rpc response, but request is timeout, response {}",
                    DebugLogUtils.logOf(rpcLogConfig.getRcvResponseLogLevel(), rpcResponse));
        } else {
            logger.info("rcv rpc response, requestStub {}, response {}",
                    DebugLogUtils.logOf(rpcLogConfig.getRcvResponseLogLevel(), requestStub),
                    DebugLogUtils.logOf(rpcLogConfig.getRcvResponseLogLevel(), rpcResponse));
        }
    }

    private void logRcvResponseLog(RpcResponse rpcResponse, RpcRequest request) {
        logger.info("rcv rpc response, request {}, response {}",
                DebugLogUtils.logOf(rpcLogConfig.getRcvResponseLogLevel(), request),
                DebugLogUtils.logOf(rpcLogConfig.getRcvResponseLogLevel(), rpcResponse));
    }

    // endregion
}