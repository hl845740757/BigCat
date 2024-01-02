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
import cn.wjybxx.base.ex.NoLogRequiredException;
import cn.wjybxx.base.time.TimeProvider;
import cn.wjybxx.bigcat.pb.PBMethodInfo;
import cn.wjybxx.bigcat.pb.PBMethodInfoRegistry;
import cn.wjybxx.bigcat.rpc.*;
import cn.wjybxx.common.concurrent.ICompletableFuture;
import cn.wjybxx.common.concurrent.WatcherMgr;
import cn.wjybxx.common.concurrent.XCompletableFuture;
import cn.wjybxx.common.log.DebugLogLevel;
import cn.wjybxx.common.log.DebugLogUtils;
import it.unimi.dsi.fastutil.longs.Long2ObjectLinkedOpenHashMap;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import javax.annotation.Nonnull;
import javax.annotation.concurrent.ThreadSafe;
import java.util.List;
import java.util.Map;
import java.util.Objects;
import java.util.concurrent.*;
import java.util.function.BiConsumer;

/**
 * 1.设置属性应该启动Node之前，运行时不可修改对象的属性
 * 2.所有的线程切换都在该类中，避免代码分散
 *
 * @author wjybxx
 * date - 2023/10/28
 */
@SuppressWarnings("unused")
public class NodeRpcSupport implements WorkerModule {

    private static final Logger logger = LoggerFactory.getLogger(NodeRpcSupport.class);

    /** 连接id -- 每次启动时应当分配新的id */
    private long conId;
    /** rpc超时时间 */
    private long timeoutMs = 15 * 1000;
    /** 日志配置 */
    private RpcLogConfig logConfig = RpcLogConfig.NONE;
    /** 是否检查pb模式下null参数和结果 */
    private boolean enableNullCheck;
    /** 当前是否可修改配置数据 -- 也可看做是否已启动标记 */
    private volatile boolean mutable = true;

    /**
     * 为request分配id
     * 1.非线程安全，只在node线程访问。
     * 2.这样可保证node发送出去的请求id是有序的。
     */
    private long sequencer = 0;
    /** 保持插入序很重要 */
    private final Long2ObjectLinkedOpenHashMap<RpcRequestStubImpl> requestStubMap = new Long2ObjectLinkedOpenHashMap<>(100);
    /** 用于支持同步调用 */
    private final Map<Long, WatcherMgr.Watcher<RpcResponse>> watcherMap = new ConcurrentHashMap<>(8);

    private Node node;
    private WorkerAddr selfAddr;
    private PBMethodInfoRegistry methodInfoRegistry;
    private RpcSerializer serializer;
    private NodeRpcRouter router;
    private TimeProvider timeProvider;

    // region 设置

    public long getConId() {
        return conId;
    }

    public NodeRpcSupport setConId(long conId) {
        ensureMutable();
        this.conId = conId;
        return this;
    }

    public long getTimeoutMs() {
        return timeoutMs;
    }

    public NodeRpcSupport setTimeoutMs(long timeoutMs) {
        ensureMutable();
        this.timeoutMs = Math.max(0, timeoutMs);
        return this;
    }

    public RpcLogConfig getLogConfig() {
        return logConfig;
    }

    public NodeRpcSupport setLogConfig(RpcLogConfig logConfig) {
        ensureMutable();
        this.logConfig = Objects.requireNonNull(logConfig);
        return this;
    }

    public boolean isEnableNullCheck() {
        return enableNullCheck;
    }

    public NodeRpcSupport setEnableNullCheck(boolean enableNullCheck) {
        ensureMutable();
        this.enableNullCheck = enableNullCheck;
        return this;
    }

    private void makeImmutable() {
        mutable = false;
    }

    private void ensureMutable() {
        if (!mutable) {
            throw new IllegalStateException("node is started");
        }
    }
    // endregion

    // region 流程

    @Override
    public void inject(Worker worker) {
        if (!(worker instanceof Node node)) {
            throw new IllegalStateException();
        }
        this.node = node;
        this.selfAddr = node.nodeAddr();
        this.timeProvider = node.injector().getInstance(TimeProvider.class);
        this.serializer = node.injector().getInstance(RpcSerializer.class);
        this.methodInfoRegistry = node.injector().getInstance(PBMethodInfoRegistry.class);
        this.router = node.injector().getInstance(NodeRpcRouter.class);
    }

    @Override
    public void start() {
        if (conId == 0) {
            conId = ThreadLocalRandom.current().nextLong();
        }
        makeImmutable();
    }

    @Override
    public void update() {
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

    @Override
    public void stop() {
        requestStubMap.clear();
        watcherMap.clear();
    }

    // endregion

    // region send

    public void w2n_send(Worker worker, RpcAddr target, RpcMethodSpec<?> methodSpec) {
        Objects.requireNonNull(worker, "worker");
        Objects.requireNonNull(target, "target");
        Objects.requireNonNull(methodSpec, "methodSpec");

        final RpcRequest request = newRequest(target, methodSpec, RpcInvokeType.ONEWAY);
        if (!node.inEventLoop()) {
            node.execute(() -> w2n_send(worker, request));
        } else {
            w2n_send(worker, request);
        }
    }

    private void w2n_send(Worker worker, RpcRequest request) {
        fillRequest(request);
        if (logConfig.getSndRequestLogLevel() > DebugLogLevel.NONE) {
            logSndRequest(request);
        }
        if (!router.send(request)) {
            logger.info("rpc send failure, target " + request.getDestAddr());
        }
    }
    // endregion

    // region call

    @SuppressWarnings("unchecked")
    public <V> ICompletableFuture<V> w2n_call(Worker worker, RpcAddr target, RpcMethodSpec<V> methodSpec) {
        Objects.requireNonNull(worker, "worker");
        Objects.requireNonNull(target, "target");
        Objects.requireNonNull(methodSpec, "methodSpec");

        final RpcRequest request = newRequest(target, methodSpec, RpcInvokeType.CALL);
        if (!node.inEventLoop()) {
            return (ICompletableFuture<V>) node.submit(() -> w2n_call(worker, request))
                    .thenCompose(e -> e) // 这里不能调用composeAsync，如果返回的future尚未完成，则会在触发其完成的线程触发业务回调
                    .whenCompleteAsync((v, t) -> {}, worker); // 需要回到worker线程
        } else {
            return w2n_call(worker, request);
        }
    }

    private <V> ICompletableFuture<V> w2n_call(Worker worker, RpcRequest request) {
        fillRequest(request);
        if (logConfig.getSndRequestLogLevel() > DebugLogLevel.NONE) {
            logSndRequest(request);
        }
        if (!router.isUnicastAddr(request.getDestAddr())) {
            logger.info("rpc multicast call, target " + request.getDestAddr());
        }
        if (!router.send(request)) {
            logger.info("rpc send failure, target " + request.getDestAddr());
        }

        // 保留存根
        final long deadline = timeProvider.getTime() + timeoutMs;
        final ICompletableFuture<V> promise = node.newPromise(); // 不可在node上阻塞
        final RpcRequestStubImpl requestStub = new RpcRequestStubImpl(request, deadline, promise);
        requestStubMap.put(request.getRequestId(), requestStub);
        return promise;
    }

    // endregion

    // region syncCall

    public <V> V w2n_syncCall(Worker worker, RpcAddr target, RpcMethodSpec<V> methodSpec) {
        return w2n_syncCall(worker, target, methodSpec, timeoutMs);
    }

    public <V> V w2n_syncCall(Worker worker, RpcAddr target, RpcMethodSpec<V> methodSpec, long timeoutMs) {
        Objects.requireNonNull(worker, "worker");
        Objects.requireNonNull(target, "target");
        Objects.requireNonNull(methodSpec, "methodSpec");
        if (timeoutMs <= 0) {
            timeoutMs = this.timeoutMs;
        }
        // 只阻塞发起调用的线程 -- 注意！这里尚无requestId
        RpcRequest request = newRequest(target, methodSpec, RpcInvokeType.SYNC_CALL);
        RpcResponse response = null;
        try {
            if (!node.inEventLoop()) {
                response = node.submit(() -> w2n_syncCall(worker, request))
                        .thenCompose(e -> e)
                        .toCompletableFuture()
                        .get(timeoutMs, TimeUnit.MILLISECONDS);
            } else {
                response = w2n_syncCall(worker, request)
                        .toCompletableFuture()
                        .get(timeoutMs, TimeUnit.MILLISECONDS);
            }
            // 使用之前反序列化
            if (!response.isDeserialized() && !decodeResult(response)) {
                response.setFailed(RpcErrorCodes.LOCAL_DESERIALIZE_FAILED, "data error");
            }
            if (logConfig.getRcvResponseLogLevel() > DebugLogLevel.NONE) {
                logRcvResponse(response, false);
            }

            if (response.getErrorCode() == 0) {
                @SuppressWarnings("unchecked") V result = (V) response.getResult();
                return result;
            } else {
                throw RpcServerException.newServerException(response);
            }
        } catch (Exception e) {
            ThreadUtils.recoveryInterrupted(e);
            throw RpcClientException.wrapOrRethrow(e);
        } finally {
            // 及时删除watcher -- 注意！当前线程可能看不见Node线程分配的requestId，而在node线程下一定可见
            if (response == null) {
                if (request.getRequestId() > 0) {
                    watcherMap.remove(request.getRequestId());
                } else {
                    node.execute(() -> watcherMap.remove(request.getRequestId()));
                }
            }
        }
    }

    private ICompletableFuture<RpcResponse> w2n_syncCall(Worker worker, RpcRequest request) {
        // 理论上到达这里的时候，可能请求线程已经超时了，暂不处理
        fillRequest(request);

        // 必须先watch再发送，否则可能丢失信号
        RpcResponseWatcher watcher = new RpcResponseWatcher(conId, request.getRequestId());
        watcherMap.put(request.getRequestId(), watcher);

        if (logConfig.getSndRequestLogLevel() > DebugLogLevel.NONE) {
            logSndRequest(request);
        }
        if (!router.isUnicastAddr(request.getDestAddr())) {
            logger.info("rpc multicast syncCall, target " + request.getDestAddr());
        }
        if (!router.send(request)) {
            logger.info("rpc send failure, target " + request.getDestAddr());

            RpcResponse response = newFailedResponse(request, RpcErrorCodes.LOCAL_ROUTER_EXCEPTION, "Failed to send request");
            watcher.future.complete(response);
        }
        return watcher.future;
    }

    // endregion

    // region rcvRequest

    /**
     * 通知Support模块收到一个Rpc请求
     * 1.该方法由IO线程调用 -- 即RpcRouter类调用。
     * 2.如果外部未反序列化请求参数，则在Node线程自动反序列化。
     * 3.如果request可能发给多个Node，应在外部拷贝
     */
    public void onRcvRequest(final RpcRequest request) {
        Objects.requireNonNull(request);
        if (!node.inEventLoop()) {
            node.execute(() -> onRcvRequest(request));
            return;
        }
        // 在使用之前需要先反序列化
        if (!request.isDeserialized() && !decodeParameters(request)) {
            deserializeFailed(request);
            return;
        }
        if (logConfig.getRcvRequestLogLevel() > DebugLogLevel.NONE) {
            logRcvRequest(request);
        }

        ServiceInfo serviceInfo = node.serviceInfoMap().get(request.getServiceId());
        if (serviceInfo == null || serviceInfo.workerList.isEmpty()) {
            unsupportedInterface(request);
            return;
        }
        List<Worker> workerList = serviceInfo.workerList;
        if (router.isBroadcastWorkerAddr(request.getDestAddr()) && workerList.size() > 1) {
            // 广播 - 逆序迭代(顺序不应该产生影响)，最后一个worker不拷贝协议
            byte[] bytesParameters = serializer.write(request.getParameters());
            for (int i = workerList.size() - 1; i >= 0; i--) {
                Worker worker = workerList.get(i);
                RpcRequest clonedRequest = i == 0 ? request : deepCopy(request, bytesParameters);
                if (worker != node) {
                    worker.execute(() -> onRcvRequestImpl(worker, clonedRequest));
                } else {
                    onRcvRequestImpl(worker, clonedRequest);
                }
            }
        } else {
            // 单播 - 选择一个worker，多worker时hash保证路由的一致性
            Worker worker;
            if (workerList.size() == 1) {
                worker = workerList.get(0);
            } else {
                int idx = request.getSrcAddr().hashCode() % workerList.size();
                worker = workerList.get(idx);
            }
            if (worker != node) {
                worker.execute(() -> onRcvRequestImpl(worker, request));
            } else {
                onRcvRequestImpl(worker, request);
            }
        }
    }

    private <T> void onRcvRequestImpl(final Worker worker, RpcRequest request) {
        WorkerCtx workerCtx = worker.workerCtx();
        RpcMethodProxy proxy = workerCtx.rpcRegistry.getProxy(request.getServiceId(), request.getMethodId());
        if (proxy == null) {
            unsupportedInterface(request);
            return;
        }
        // 拦截测试
        int code = workerCtx.interceptor == null ? 0 : workerCtx.interceptor.test(request);
        if (code != 0) {
            if (RpcInvokeType.isCall(request.getInvokeType())) {
                sendResponse(newFailedResponse(request, code, ""));
            }
            return;
        }
        // 执行调用
        RpcMethodSpec<T> methodSpec = new RpcMethodSpec<>(request.getServiceId(), request.getMethodId(), request.listParameters());
        RpcContextImpl<T> context = new RpcContextImpl<>(request, this);
        if (!RpcInvokeType.isCall(request.getInvokeType())) {
            // Oneway - 不需要结果
            try {
                proxy.invoke(context, methodSpec);
            } catch (Throwable e) {
                logInvokeException(request, e);
            }
        } else {
            // Call -- 监听future完成事件
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
                    @SuppressWarnings("unchecked") T castReult = (T) result;
                    context.sendResult(castReult);
                }
            } catch (Throwable e) {
                context.sendError(e);
                logInvokeException(request, e);
            }
        }
    }

    /** 反序列化失败 */
    private void deserializeFailed(RpcRequest request) {
        if (logger.isInfoEnabled()) {
            logger.info("deserialize request failed, request: " + request.toSimpleLog());
        }
        if (RpcInvokeType.isCall(request.getInvokeType())) {
            sendResponse(newFailedResponse(request, RpcErrorCodes.SERVER_DESERIALIZE_FAILED, ""));
        }
    }

    /** 服务不存在 */
    private void unsupportedInterface(RpcRequest request) {
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

    // endregion

    // region rcvResponse

    /**
     * 通知Support模块收到一个Rpc响应
     * 1.该方法由IO线程调用 -- 即RpcRouter类调用。
     * 2.如果外部未反序列化结果，则在Node线程自动反序列化
     */
    public void onRcvResponse(RpcResponse response) {
        Objects.requireNonNull(response);
        if (response.getConId() != conId) {
            // 收到旧进程的rpc响应，常见于使用MQ通信的服务器
            logger.info("rcv old process rpc response, remote {}", response.getSrcAddr());
            return;
        }
        // watcher需要在IO线程测试
        WatcherMgr.Watcher<RpcResponse> watcher = watcherMap.remove(response.getRequestId());
        if (watcher != null) { // 同步调用结果
            watcher.onEvent(response);
            return;
        }
        if (!node.inEventLoop()) {
            node.execute(() -> onRcvResponseImpl(response));
        } else {
            onRcvResponseImpl(response);
        }
    }

    private void onRcvResponseImpl(RpcResponse response) {
        // 使用之前反序列化
        if (!response.isDeserialized() && !decodeResult(response)) {
            response.setFailed(RpcErrorCodes.LOCAL_DESERIALIZE_FAILED, "data error");
        }
        final RpcRequestStubImpl requestStub = requestStubMap.remove(response.getRequestId());
        if (logConfig.getRcvResponseLogLevel() > DebugLogLevel.NONE) {
            logRcvResponse(response, requestStub == null);
        }
        if (requestStub == null) {
            return;
        }
        // future的跨线程问题是在call的时候处理的
        @SuppressWarnings("unchecked") CompletableFuture<Object> future = (CompletableFuture<Object>) requestStub.future;
        final int errorCode = response.getErrorCode();
        if (errorCode == 0) {
            future.complete(response.getResult());
        } else {
            future.completeExceptionally(RpcServerException.newServerException(response));
        }
    }

    // endregion

    // region factory

    /** worker线程调用 -- worker可能是node自身 */
    private RpcRequest newRequest(RpcAddr target, RpcMethodSpec<?> methodSpec, int invokeType) {
        RpcRequest request = new RpcRequest(conId, selfAddr, target, invokeType, 0, methodSpec);
        if (enableNullCheck && router.isCrossLanguageAddr(target)) {
            checkArgumentNull(request);
        }
        // 参数可共享的情况下，延迟序列化（分担主线程开销）
        if (!request.isSharable() && !request.isSerialized()) {
            encodeParameters(request);
            request.setSerialized();
        }
        return request;
    }

    /**
     * 填充Request的数据
     * 1.在node线程调用，分配请求id等
     * 2.在Node线程分配id才能保证id连续和递增
     */
    private void fillRequest(RpcRequest request) {
//        assert node.inEventLoop();
        request.setRequestId(++sequencer);
    }

    /** node或worker线程调用 */
    private void sendResponse(final RpcResponse response) {
        if (enableNullCheck && router.isCrossLanguageAddr(response.getDestAddr())) {
            checkResultNull(response);
        }
        // 参数可共享的情况下，延迟序列化（分担主线程开销）
        if (!response.isSharable() && !response.isSerialized()) {
            encodeResult(response);
            response.setSerialized();
        }
        if (!node.inEventLoop()) {
            node.execute(() -> sendResponseImpl(response));
        } else {
            sendResponseImpl(response);
        }
    }

    private void sendResponseImpl(RpcResponse response) {
        if (logConfig.getSndResponseLogLevel() > DebugLogLevel.NONE) {
            logSndResponse(response);
        }
        if (!router.send(response)) {
            logger.warn("rpc send response failure, dest {}", response.getDestAddr());
        }
    }

    private void checkArgumentNull(RpcRequest request) {
        // null参数警告
        PBMethodInfo<?, ?> methodInfo = methodInfoRegistry.getMethodInfo(request.getServiceId(), request.getMethodId());
        List<Object> parameters = request.listParameters();
        if (methodInfo.argType != null && parameters.isEmpty() || parameters.get(0) == null) {
            logger.info("rpc argument is null, it will be replaced with an empty message, serviceId: {}, {}",
                    request.getServiceId(), request.getMethodId());
        }
    }

    private void checkResultNull(RpcResponse response) {
        if (response.isSerialized() || !response.isSuccess()) { // 用户可直接发送编码后的结果
            return;
        }
        // null结果警告
        PBMethodInfo<?, ?> methodInfo = methodInfoRegistry.getMethodInfo(response.getServiceId(), response.getMethodId());
        if (methodInfo.resultType != null && response.getResult() == null) {
            logger.info("rpc result is null, it will be replaced with an empty message, serviceId: {}, {}",
                    response.getServiceId(), response.getMethodId());
        }
    }

    private RpcResponse newFailedResponse(RpcRequest request, int errorCode, String msg) {
        RpcResponse response = new RpcResponse(request, selfAddr);
        response.setFailed(errorCode, msg);
        response.setSharable(true);
        return response;
    }

    private RpcResponse newFailedResponse(RpcRequest request, Throwable ex) {
        Objects.requireNonNull(ex);
        RpcResponse response = new RpcResponse(request, selfAddr);
        response.setSharable(true);
        response.setFailed(ex);
        return response;
    }

    /** 深度拷贝rpc请求参数 */
    private RpcRequest deepCopy(RpcRequest src, byte[] bytesParameters) {
        RpcRequest request = new RpcRequest(src.getConId(), src.getSrcAddr(), src.getDestAddr())
                .setRequestId(src.getRequestId())
                .setInvokeType(src.getInvokeType())
                .setServiceId(src.getServiceId())
                .setMethodId(src.getMethodId())
                .setParameters(bytesParameters);
        decodeParameters(request);
        request.setDeserialized();
        return request;
    }

    // endregion

    // region 编解码

    /** 序列化rpc参数 */
    private void encodeParameters(RpcRequest request) {
        if (router.isCrossLanguageAddr(request.getDestAddr())) {
            methodInfoRegistry.encodeParameters(request);
        } else {
            Object parameters = request.getParameters();
            assert parameters instanceof List;
            request.setParameters(serializer.write(parameters));
        }
    }

    /** 反序列化rpc参数 -- 在使用之前；可顺带进行部分初始化 */
    private boolean decodeParameters(RpcRequest request) {
        if (router.isCrossLanguageAddr(request.getSrcAddr())) {
            return methodInfoRegistry.decodeParameters(request);
        }
        try {
            Object parameters = serializer.read(request.bytesParameters());
            request.setParameters(parameters);
            return true;
        } catch (Exception e) {
            logger.info("decode parameters caught exception, serviceId {}, methodId {}",
                    request.getServiceId(), request.getMethodId(), e);
            return false;
        }
    }

    private void encodeResult(RpcResponse response) {
        if (router.isCrossLanguageAddr(response.getDestAddr())) {
            methodInfoRegistry.encodeResult(response);
        } else {
            Object results = response.getResults();
            assert results instanceof List;
            response.setResults(serializer.write(results));
        }
    }

    /** 反序列化结果 -- 在使用之前；可顺带进行部分初始化 */
    private boolean decodeResult(RpcResponse response) {
        if (router.isCrossLanguageAddr(response.getSrcAddr())) {
            return methodInfoRegistry.decodeResult(response);
        }
        try {
            Object results = serializer.read(response.bytesResults());
            response.setResults(results);
            return true;
        } catch (Exception e) {
            logger.info("decode result caught exception, serviceId {}, methodId {}",
                    response.getServiceId(), response.getMethodId(), e);
            return false;
        }
    }
    // endregion

    // region

    @ThreadSafe
    private static class RpcResponseWatcher implements WatcherMgr.Watcher<RpcResponse> {

        private final ICompletableFuture<RpcResponse> future = new XCompletableFuture<>();
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
        final NodeRpcSupport rpcClient;
        boolean sharable = false;

        RpcContextImpl(RpcRequest request, NodeRpcSupport rpcClient) {
            this.request = request;
            this.rpcClient = rpcClient;
        }

        @Override
        public RpcRequest request() {
            return request;
        }

        @Override
        public RpcAddr remoteAddr() {
            return request.getSrcAddr();
        }

        @Override
        public RpcAddr localAddr() {
            return request.getDestAddr();
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
            if (sharable) {
                response.setSuccess(result);
            } else {
                response.setSuccess(result.clone());
            }
            response.setSharable(sharable);
            response.setSerialized();
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
        // 旧时的response中不包含serverId和methodId，因此参数有传stub和request
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