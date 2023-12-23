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
import cn.wjybxx.common.concurrent.*;
import cn.wjybxx.common.ex.NoLogRequiredException;
import cn.wjybxx.common.log.DebugLogLevel;
import cn.wjybxx.common.log.DebugLogUtils;
import cn.wjybxx.common.time.TimeProvider;
import it.unimi.dsi.fastutil.longs.Long2ObjectLinkedOpenHashMap;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import javax.annotation.concurrent.NotThreadSafe;
import javax.annotation.concurrent.ThreadSafe;
import java.util.Objects;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.CompletionStage;
import java.util.concurrent.TimeUnit;
import java.util.function.BiConsumer;

/**
 * 默认的{@link RpcClient}实现，但仍建议你进行代理封装
 * 默认实现不支持延迟序列化特性，是否延迟有{@link RpcRouter}决定
 *
 * @author wjybxx
 * date 2023/4/1
 */
@NotThreadSafe
public class DefaultRpcClient implements RpcClient {

    private static final Logger logger = LoggerFactory.getLogger(DefaultRpcClient.class);

    private long sequencer = 0;
    private final Long2ObjectLinkedOpenHashMap<RpcRequestStubImpl> requestStubMap = new Long2ObjectLinkedOpenHashMap<>(500);
    private final WatcherMgr<RpcResponse> watcherMgr = new SimpleWatcherMgr<>();

    private final long conId;
    private final RpcAddr selfAddr;
    private final RpcRouter router;
    private final RpcRegistry registry;
    private final TimeProvider timeProvider;
    private final long timeoutMs;

    /** 日志级别 */
    private RpcLogConfig logConfig = RpcLogConfig.NONE;
    /** 拦截测试 */
    private RpcInterceptor interceptor;

    /**
     * @param conId        连接id
     * @param selfAddr     当前服务器的描述信息
     * @param router       路由实现
     * @param registry     rpc调用派发实现
     * @param timeProvider 用于获取当前时间
     * @param timeoutMs    rpc超时时间
     */
    public DefaultRpcClient(long conId, RpcAddr selfAddr,
                            RpcRouter router, RpcRegistry registry,
                            TimeProvider timeProvider, long timeoutMs) {
        this.conId = conId;
        this.selfAddr = Objects.requireNonNull(selfAddr);
        this.router = Objects.requireNonNull(router);
        this.registry = Objects.requireNonNull(registry);
        this.timeProvider = Objects.requireNonNull(timeProvider);
        this.timeoutMs = timeoutMs;
    }

    public RpcLogConfig getLogConfig() {
        return logConfig;
    }

    public DefaultRpcClient setLogConfig(RpcLogConfig logConfig) {
        this.logConfig = Objects.requireNonNullElse(logConfig, RpcLogConfig.NONE);
        return this;
    }

    public RpcInterceptor getInterceptor() {
        return interceptor;
    }

    public DefaultRpcClient setInterceptor(RpcInterceptor interceptor) {
        this.interceptor = interceptor;
        return this;
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

    // region 流程

    /**
     * 服务器需要每帧调用该方法，以检测超时等
     */
    public void tick() {
        // 并不需要检查全部的rpc请求，只要第一个未超时，即可停止。
        // 因为我们没打算支持每个请求单独设立超时时间，且我们的map是保持插入序的，因此先发送的请求一定先超时
        final long curTime = timeProvider.getTime();
        while (requestStubMap.size() > 0) {
            final long requestId = requestStubMap.firstLongKey();
            final RpcRequestStubImpl requestStub = requestStubMap.get(requestId);
            if (curTime < requestStub.deadline) {
                return;
            }

            logger.info("rpc timeout, requestId {}, target {}", requestId, requestStub.getDestAddr());
            requestStubMap.removeFirst();
            requestStub.future.completeExceptionally(RpcClientException.timeout());
        }
    }

    /**
     * 清除所有的rpc调用（慎重调用）
     */
    public void clear() {
        requestStubMap.clear();
    }
    // endregion

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
        final RpcRequest request = new RpcRequest(conId, selfAddr, target,
                RpcInvokeType.ONEWAY, requestId, methodSpec);

        if (logConfig.getSndRequestLogLevel() > DebugLogLevel.NONE) {
            logSndRequest(request);
        }
        if (!router.send(request)) {
            logger.info("rpc router send failure, target " + target);
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
        final RpcRequest request = new RpcRequest(conId, selfAddr, target,
                RpcInvokeType.CALL, requestId, methodSpec);

        if (logConfig.getSndRequestLogLevel() > DebugLogLevel.NONE) {
            logSndRequest(request);
        }
        if (!router.send(request)) {
            logger.info("rpc router call failure, target " + target);
        }

        // 保留存根
        final long deadline = timeProvider.getTime() + timeoutMs;
        final ICompletableFuture<V> promise = FutureUtils.newPromise();
        final RpcRequestStubImpl requestStub = new RpcRequestStubImpl(request, deadline, promise);
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
    public <V> V syncCall(RpcAddr target, RpcMethodSpec<V> methodSpec) {
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
    public <V> V syncCall(RpcAddr target, RpcMethodSpec<V> methodSpec, long timeoutMs) {
        Objects.requireNonNull(target);
        Objects.requireNonNull(methodSpec);

        final long requestId = ++sequencer;
        final RpcRequest request = new RpcRequest(conId, selfAddr, target,
                RpcInvokeType.SYNC_CALL, requestId, methodSpec);

        if (logConfig.getSndRequestLogLevel() > DebugLogLevel.NONE) {
            logSndRequest(request);
        }

        // 必须先watch再发送，否则可能丢失信号
        RpcResponseWatcher watcher = new RpcResponseWatcher(conId, requestId);
        watcherMgr.watch(watcher);
        try {
            // 执行发送(router的实现很关键)
            if (!router.send(request)) {
                logger.info("rpc router call failure, target " + target);
                throw RpcClientException.sendFailed(target);
            }

            RpcResponse response = watcher.future.get(timeoutMs, TimeUnit.MILLISECONDS);
            if (logConfig.getRcvResponseLogLevel() > DebugLogLevel.NONE) {
                logRcvResponse(response, false);
            }

            if (response.getErrorCode() == 0) {
                @SuppressWarnings("unchecked") final V result = (V) response.getResult();
                return result;
            } else {
                throw RpcServerException.newServerException(response);
            }
        } catch (Exception e) {
            ThreadUtils.recoveryInterrupted(e);
            throw RpcClientException.wrapOrRethrow(e);
        } finally {
            watcherMgr.cancelWatch(watcher); // 及时取消watcher
        }
    }
    // endregion

    // region 收包

    public void onRcvProtocol(RpcProtocol protocol) {
        if (protocol instanceof RpcRequest request) {
            onRcvRequest(request);
        } else {
            onRcvResponse((RpcResponse) protocol);
        }
    }

    /**
     * 接收到一个rpc请求
     */
    public <T> void onRcvRequest(RpcRequest request) {
        Objects.requireNonNull(request);
        if (logConfig.getRcvRequestLogLevel() > DebugLogLevel.NONE) {
            logRcvRequest(request);
        }

        RpcMethodProxy proxy = registry.getProxy(request.getServiceId(), request.getMethodId());
        if (proxy == null) {
            unsupportedInterface(request);
            return;
        }
        // 拦截器测试
        int code = interceptor == null ? 0 : interceptor.test(request);
        if (code != 0) {
            if (RpcInvokeType.isCall(request.getInvokeType())) {
                sendResponse(newFailedResponse(request, code, ""));
            }
            return;
        }

        RpcMethodSpec<?> methodSpec = new RpcMethodSpec<>(request.getServiceId(), request.getMethodId(), request.listParameters());
        RpcContextImpl<T> context = new RpcContextImpl<>(request, this);
        if (!RpcInvokeType.isCall(request.getInvokeType())) {
            // 单向消息 - 不需要结果
            try {
                proxy.invoke(context, methodSpec);
            } catch (Throwable e) {
                logInvokeException(request, e);
            }
        } else {
            // rpc -- 监听future完成事件
            try {
                final Object result = proxy.invoke(context, methodSpec);
                if (result == context) {
                    return; // 用户使用了context返回结果
                }
                if (result instanceof CompletionStage<?>) { // 异步获取结果
                    @SuppressWarnings("unchecked") CompletionStage<T> future = (CompletionStage<T>) result;
                    future.whenComplete(context);
                } else {
                    // 立即得到了结果
                    @SuppressWarnings("unchecked") T castResult = (T) result;
                    context.sendResult(castResult);
                }
            } catch (Throwable e) {
                context.sendError(e);
                logInvokeException(request, e);
            }
        }
    }

    private void unsupportedInterface(RpcRequest request) {
        // 不存在的服务
        logger.warn("unsupported interface, src {}, serviceId={}, methodId={}",
                request.getSrcAddr(), request.getServiceId(), request.getMethodId());

        // 需要返回结果
        if (RpcInvokeType.isCall(request.getInvokeType())) {
            sendResponse(newFailedResponse(request, RpcErrorCodes.SERVER_UNSUPPORTED_INTERFACE, ""));
        }
    }

    private static void logInvokeException(RpcRequest request, Throwable e) {
        if (!(e instanceof NoLogRequiredException)) {
            logger.warn("invoke caught exception, src {}, serviceId={}, methodId={}",
                    request.getSrcAddr(), request.getServiceId(), request.getMethodId(), e);
        }
    }

    private void sendResponse(RpcResponse response) {
        if (logConfig.getSndResponseLogLevel() > DebugLogLevel.NONE) {
            logSndResponse(response);
        }
        if (!router.send(response)) {
            logger.warn("rpc send response failure, dest {}", response.getDestAddr());
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

        final RpcRequestStubImpl requestStub = requestStubMap.remove(response.getRequestId());
        if (logConfig.getRcvResponseLogLevel() > DebugLogLevel.NONE) {
            logRcvResponse(response, requestStub == null);
        }
        if (requestStub == null) {
            return;
        }
        final int errorCode = response.getErrorCode();
        @SuppressWarnings("unchecked") final ICompletableFuture<Object> promise = (ICompletableFuture<Object>) requestStub.future;
        if (errorCode == 0) {
            promise.complete(response.getResult());
        } else {
            promise.completeExceptionally(RpcServerException.newServerException(response));
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

    // region internal

    private RpcResponse newFailedResponse(RpcRequest request, int errorCode, String msg) {
        assert errorCode > 0;
        RpcResponse response = new RpcResponse(request, selfAddr);
        response.setFailed(errorCode, msg);
        return response;
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

    private static class RpcContextImpl<V> implements RpcContext<V>, BiConsumer<V, Throwable> {

        final RpcRequest request;
        final DefaultRpcClient rpcClient;
        boolean sharable = false;

        RpcContextImpl(RpcRequest request, DefaultRpcClient rpcClient) {
            this.request = request;
            this.rpcClient = rpcClient;
        }

        @Override
        public RpcRequest request() {
            return request;
        }

        @Override
        public RpcAddr remoteAddr() {
            return request.srcAddr;
        }

        @Override
        public RpcAddr localAddr() {
            return request.destAddr;
        }

        @Override
        public boolean isSharable() {
            return sharable;
        }

        @Override
        public void setSharable(boolean sharable) {
            this.sharable = sharable;
        }

        @Override
        public void sendResult(V result) {
            RpcResponse response = new RpcResponse(request, rpcClient.selfAddr);
            response.setSharable(sharable);
            response.setSuccess(result);
            rpcClient.sendResponse(response);
        }

        @Override
        public void sendError(int errorCode, String msg) {
            if (!RpcErrorCodes.isUserCode(errorCode)) {
                throw new IllegalArgumentException("invalid errorCode: " + errorCode);
            }
            RpcResponse response = new RpcResponse(request, rpcClient.selfAddr);
            response.setSharable(true);
            response.setFailed(errorCode, msg);
            rpcClient.sendResponse(response);
        }

        @Override
        public void sendError(Throwable ex) {
            Objects.requireNonNull(ex);
            RpcResponse response = new RpcResponse(request, rpcClient.selfAddr);
            response.setSharable(true);
            response.setFailed(ex);
            rpcClient.sendResponse(response);
        }

        @Override
        public void sendEncodedResult(byte[] result, boolean sharable) {
            Objects.requireNonNull(result);
            RpcResponse response = new RpcResponse(request, rpcClient.selfAddr);
            response.setSharable(sharable);
            response.setSuccess(result);
            rpcClient.sendResponse(response);
        }

        @Override
        public void accept(V v, Throwable throwable) {
            if (throwable == null) {
                sendResult(v);
            } else {
                sendError(throwable);
            }
        }

    }

    private static class RpcRequestStubImpl implements RpcRequestStub {

        final RpcRequest request;
        final long deadline;
        final ICompletableFuture<?> future;

        RpcRequestStubImpl(RpcRequest request, long deadline, ICompletableFuture<?> future) {
            this.future = future;
            this.deadline = deadline;
            this.request = request;
        }

        @Override
        public long getDeadline() {
            return deadline;
        }

        @Override
        public RpcAddr getDestAddr() {
            return request.getDestAddr();
        }

        @Override
        public RpcRequest getRequest() {
            return request;
        }
    }

    // endregion

    // region debug日志

    private void logSndRequest(RpcRequest request) {
        logger.info("snd rpc request, request {}",
                DebugLogUtils.logOf(logConfig.getSndRequestLogLevel(), request));
    }

    private void logSndResponse(RpcResponse response) {
        logger.info("snd rpc response, response {}",
                DebugLogUtils.logOf(logConfig.getSndResponseLogLevel(), response));
    }

    private void logRcvRequest(RpcRequest request) {
        logger.info("rcv rpc request, request {}",
                DebugLogUtils.logOf(logConfig.getRcvRequestLogLevel(), request));
    }

    private void logRcvResponse(RpcResponse response, boolean timeout) {
        if (timeout) {
            logger.info("rcv rpc response, but request is timeout, response {}",
                    DebugLogUtils.logOf(logConfig.getRcvResponseLogLevel(), response));
        } else {
            logger.info("rcv rpc response, response {}",
                    DebugLogUtils.logOf(logConfig.getRcvResponseLogLevel(), response));
        }
    }

    // endregion
}