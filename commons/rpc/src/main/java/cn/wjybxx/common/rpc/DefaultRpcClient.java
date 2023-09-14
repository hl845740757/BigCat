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


import cn.wjybxx.common.concurrent.*;
import cn.wjybxx.common.ex.ErrorCodeException;
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
public class DefaultRpcClient implements RpcClient {

    private static final Logger logger = LoggerFactory.getLogger(DefaultRpcClient.class);
    private static final int INIT_REQUEST_GUID = 0;

    private long sequencer = INIT_REQUEST_GUID;
    private final Long2ObjectLinkedOpenHashMap<DefaultRpcRequestStub> requestStubMap = new Long2ObjectLinkedOpenHashMap<>(500);
    private final WatcherMgr<RpcResponse> watcherMgr = new SimpleWatcherMgr<>();

    private final long conId;
    private final RpcAddr selfRpcAddr;
    private final RpcSender sender;
    private final RpcRegistry registry;
    private final TimeProvider timeProvider;
    private final long timeoutMs;
    /** 日志级别 */
    private RpcLogConfig rpcLogConfig = RpcLogConfig.NONE;

    /**
     * @param conId        连接id
     * @param selfRpcAddr  当前服务器的描述信息
     * @param sender       路由实现
     * @param registry     rpc调用派发实现
     * @param timeProvider 用于获取当前时间
     * @param timeoutMs    rpc超时时间
     */
    public DefaultRpcClient(long conId, RpcAddr selfRpcAddr,
                            RpcSender sender, RpcRegistry registry,
                            TimeProvider timeProvider, long timeoutMs) {
        this.conId = conId;
        this.selfRpcAddr = Objects.requireNonNull(selfRpcAddr);
        this.sender = Objects.requireNonNull(sender);
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

    public RpcRegistry getRegistry() {
        return registry;
    }

    /**
     * 获取请求的存根信息（用于debug等）
     */
    @Nullable
    public RpcRequestStub getRequestStub(long requestId) {
        return requestStubMap.get(requestId);
    }

    // region 发送

    /**
     * 发送一个消息给远程
     *
     * @param target     远程节点信息
     * @param methodSpec 要调用的方法信息
     */
    @Override
    public void send(RpcAddr target, RpcMethodSpec<?> methodSpec) {
        Objects.requireNonNull(target);
        Objects.requireNonNull(methodSpec);

        final long requestId = ++sequencer;
        final RpcRequest request = new RpcRequest(conId, selfRpcAddr, target,
                requestId, RpcInvokeType.ONEWAY, methodSpec);

        if (rpcLogConfig.getSndRequestLogLevel() > DebugLogLevel.NONE) {
            logSndRequest(request);
        }

        if (!sender.send(request)) {
            logger.warn("rpc router send failure, target " + target);
        }
    }

    /**
     * 广播一个消息给
     *
     * @param target     远程节点信息
     * @param methodSpec 要调用的方法信息
     */
    @Override
    public void broadcast(RpcAddr target, RpcMethodSpec<?> methodSpec) {
        Objects.requireNonNull(target);
        Objects.requireNonNull(methodSpec);

        final long requestId = ++sequencer;
        final RpcRequest request = new RpcRequest(conId, selfRpcAddr, target,
                requestId, RpcInvokeType.BROADCAST, methodSpec);

        if (rpcLogConfig.getSndRequestLogLevel() > DebugLogLevel.NONE) {
            logBroadcastRequest(request);
        }

        if (!sender.send(request)) {
            logger.warn("rpc router broadcast failure, scope " + target);
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
    public <V> ICompletableFuture<V> call(RpcAddr target, RpcMethodSpec<V> methodSpec) {
        Objects.requireNonNull(target);
        Objects.requireNonNull(methodSpec);

        final long requestId = ++sequencer;
        final RpcRequest request = new RpcRequest(conId, selfRpcAddr, target,
                requestId, RpcInvokeType.CALL, methodSpec);

        if (rpcLogConfig.getSndRequestLogLevel() > DebugLogLevel.NONE) {
            logSndRequest(request);
        }

        // 执行发送(routerHandler的实现很关键)
        if (!sender.send(request)) {
            logger.warn("rpc router call failure, target " + target);
        }
        // 保留存根
        final long deadline = timeProvider.getTime() + timeoutMs;
        final ICompletableFuture<V> promise = FutureUtils.newPromise();
        final DefaultRpcRequestStub requestStub = new DefaultRpcRequestStub(promise, deadline, target, request);
        requestStubMap.put(requestId, requestStub);
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
    public <V> V syncCall(RpcAddr target, RpcMethodSpec<V> methodSpec) throws InterruptedException {
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
    public <V> V syncCall(RpcAddr target, RpcMethodSpec<V> methodSpec, long timeoutMs) throws InterruptedException {
        Objects.requireNonNull(target);
        Objects.requireNonNull(methodSpec);

        final long requestId = ++sequencer;
        final RpcRequest request = new RpcRequest(conId, selfRpcAddr, target,
                requestId, RpcInvokeType.SYNC_CALL, methodSpec);

        if (rpcLogConfig.getSndRequestLogLevel() > DebugLogLevel.NONE) {
            logSndRequest(request);
        }

        // 必须先watch再发送，否则可能丢失信号
        RpcResponseWatcher watcher = new RpcResponseWatcher(conId, requestId);
        watcherMgr.watch(watcher);
        try {
            // 执行发送(routerHandler的实现很关键)
            if (!sender.send(request)) {
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
            watcherMgr.cancelWatch(watcher); // 及时取消watcher
        }
    }

    public void onRcvProtocol(RpcProtocol protocol) {
        if (protocol instanceof RpcRequest request) {
            onRcvRequest(request);
        } else if (protocol instanceof RpcResponse response) {
            onRcvResponse(response);
        } else {
            throw new IllegalArgumentException("invalid protocol " + protocol);
        }
    }

    /**
     * 接收到一个rpc请求
     */
    public <T> void onRcvRequest(RpcRequest request) {
        Objects.requireNonNull(request);
        if (rpcLogConfig.getRcvRequestLogLevel() > DebugLogLevel.NONE) {
            logRcvRequest(request);
        }

        RpcMethodProxy proxy = registry.getProxy(request.getServiceId(), request.getMethodId());
        if (proxy == null) {
            logger.warn("rcv unknown request, node {}, serviceId={}, methodId={}",
                    request.getSrcAddr(), request.getServiceId(), request.getMethodId());
            if (RpcInvokeType.isCall(request.getInvokeType())) {
                final RpcResponse response = RpcResponse.newFailedResponse(request, selfRpcAddr, RpcErrorCodes.SERVER_UNSUPPORTED_INTERFACE, "");
                sendResponseAndLog(response);
            }
            return;
        }

        RpcMethodSpec<?> methodSpec = new RpcMethodSpec<>(request.getServiceId(), request.getMethodId(), request.getParameters());
        DefaultRpcContext<T> context = new DefaultRpcContext<>(request, new XCompletableFuture<>());
        if (RpcInvokeType.isMessage(request.getInvokeType())) {
            // 单向消息 - 不需要结果
            try {
                proxy.invoke(context, methodSpec);
                context.future().complete(null);
            } catch (Exception e) {
                context.future().completeExceptionally(e);
                throw wrapException(request, e);
            }
        } else {
            // rpc -- 监听future完成事件
            context.future().whenComplete(new FutureListener<>(request, this));
            try {
                final Object result = proxy.invoke(context, methodSpec);
                if (result == context) {
                    return; // 用户使用了context返回结果
                }
                if (result instanceof CompletableFuture) {
                    @SuppressWarnings("unchecked") CompletableFuture<T> future = (CompletableFuture<T>) result;
                    FutureUtils.setFuture(context.future(), future);
                } else {
                    // 立即得到了结果
                    @SuppressWarnings("unchecked") T castResult = (T) result;
                    context.future().complete(castResult);
                }
            } catch (Exception e) {
                context.future().completeExceptionally(e);
                throw wrapException(request, e);
            }
        }
    }

    /**
     * 接收到一个rpc调用结果，该方法由主线程调用。
     */
    public void onRcvResponse(RpcResponse response) {
        Objects.requireNonNull(response);
        if (response.getConId() != conId) {
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
            } else if (RpcErrorCodes.isUserCode(errorCode)) {
                requestStub.rpcPromise.completeExceptionally(new ErrorCodeException(errorCode, response.getErrorMsg()));
            } else {
                requestStub.rpcPromise.completeExceptionally(RpcServerException.failed(errorCode, response.getErrorMsg()));
            }
        }
    }

    /**
     * 检测是否有Watcher在等待该结果，该方法由IO线程调用。
     * <p>
     * 用户在收到一个{@link RpcResponse}小时时，应当先调用该方法检测是否存在阻塞的线程。
     * 如果该方法返回true，则消息已被消费无需放入队列；否则需要将消息放入队列，然后主线程调用{@link #onRcvResponse(RpcResponse)}
     */
    public boolean checkWatcher(RpcResponse response) {
        return watcherMgr.onEvent(response);
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
            final long requestId = requestStubMap.firstLongKey();
            final DefaultRpcRequestStub requestStub = requestStubMap.get(requestId);
            if (curTime < requestStub.deadline) {
                return;
            }

            logger.warn("rpc timeout, requestId {}, target {}", requestId, requestStub.target);
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

    private void sendResponseAndLog(RpcResponse response) {
        if (rpcLogConfig.getSndResponseLogLevel() > DebugLogLevel.NONE) {
            logSndResponse(response);
        }

        if (!sender.send(response)) {
            logger.warn("rpc send response failure, dest {}", response.getDestAddr());
        }
    }

    private static RuntimeException wrapException(RpcRequest request, Exception e) {
        String msg = String.format("invoke caught exception, node %s, serviceId=%d, methodId=%d",
                request.getSrcAddr(), request.getServiceId(), request.getMethodId());
        return new RuntimeException(msg, e);
    }

    private <V> RpcResponse newSucceedResponse(RpcRequest request, V result) {
        return RpcResponse.newSucceedResponse(request, selfRpcAddr, result);
    }

    private RpcResponse newFailedResponse(RpcRequest request, Throwable e) {
        e = FutureUtils.unwrapCompletionException(e);
        if (e instanceof ErrorCodeException codeException) {
            return RpcResponse.newFailedResponse(request, selfRpcAddr, codeException.getErrorCode(), codeException.getMessage());
        } else {
            return RpcResponse.newFailedResponse(request, selfRpcAddr, RpcErrorCodes.SERVER_EXCEPTION, ExceptionUtils.getMessage(e));
        }
    }

    @ThreadSafe
    private static class RpcResponseWatcher implements WatcherMgr.Watcher<RpcResponse> {

        private final CompletableFuture<RpcResponse> future = new CompletableFuture<>();
        private final long conId;
        private final long requestId;

        private RpcResponseWatcher(long conId, long requestId) {
            this.conId = conId;
            this.requestId = requestId;
        }

        @Override
        public boolean test(@Nonnull RpcResponse response) {
            return response.getConId() == conId
                    && response.getRequestId() == requestId;
        }

        @Override
        public void onEvent(@Nonnull RpcResponse response) {
            future.complete(response);
        }
    }

    private static class FutureListener<V> implements BiConsumer<V, Throwable> {

        final RpcRequest request;
        final DefaultRpcClient rpcClient;

        FutureListener(RpcRequest request, DefaultRpcClient rpcClient) {
            this.request = request;
            this.rpcClient = rpcClient;
        }

        @Override
        public void accept(V v, Throwable throwable) {
            final RpcResponse response;
            if (throwable != null) {
                response = rpcClient.newFailedResponse(request, throwable);
            } else {
                response = rpcClient.newSucceedResponse(request, v);
            }
            rpcClient.sendResponseAndLog(response);
        }

    }

    private static class DefaultRpcRequestStub implements RpcRequestStub {

        final ICompletableFuture<?> rpcPromise;
        final long deadline;
        final RpcAddr target;
        final RpcRequest request;

        DefaultRpcRequestStub(ICompletableFuture<?> rpcPromise, long deadline, RpcAddr target, RpcRequest request) {
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
        public RpcAddr getTargetNode() {
            return target;
        }

        @Override
        public RpcRequest getRequest() {
            return request;
        }
    }

    // endregion

    // region debug日志

    private void logSndRequest(RpcRequest request) {
        logger.info("snd rpc request, target {}, request {}",
                request.getDestAddr(),
                DebugLogUtils.logOf(rpcLogConfig.getSndRequestLogLevel(), request));
    }

    private void logBroadcastRequest(RpcRequest request) {
        logger.info("broadcast rpc request, target {}, request {}",
                request.getDestAddr(),
                DebugLogUtils.logOf(rpcLogConfig.getSndRequestLogLevel(), request));
    }

    private void logSndResponse(RpcResponse response) {
        logger.info("snd rpc response, from {}, response {}",
                response.getDestAddr(),
                DebugLogUtils.logOf(rpcLogConfig.getSndResponseLogLevel(), response));
    }

    private void logRcvRequest(RpcRequest request) {
        logger.info("rcv rpc request, request {}",
                DebugLogUtils.logOf(rpcLogConfig.getRcvRequestLogLevel(), request));
    }

    private void logRcvResponse(RpcResponse response, DefaultRpcRequestStub requestStub) {
        if (null == requestStub) {
            logger.info("rcv rpc response, but request is timeout, response {}",
                    DebugLogUtils.logOf(rpcLogConfig.getRcvResponseLogLevel(), response));
        } else {
            logger.info("rcv rpc response, requestStub {}, response {}",
                    DebugLogUtils.logOf(rpcLogConfig.getRcvResponseLogLevel(), requestStub),
                    DebugLogUtils.logOf(rpcLogConfig.getRcvResponseLogLevel(), response));
        }
    }

    private void logRcvResponse(RpcResponse response, RpcRequest request) {
        logger.info("rcv rpc response, request {}, response {}",
                DebugLogUtils.logOf(rpcLogConfig.getRcvResponseLogLevel(), request),
                DebugLogUtils.logOf(rpcLogConfig.getRcvResponseLogLevel(), response));
    }

    // endregion
}