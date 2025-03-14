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

import cn.wjybxx.base.BitFlags;
import cn.wjybxx.base.MathCommon;
import cn.wjybxx.base.ThreadUtils;
import cn.wjybxx.base.ex.NoLogRequiredException;
import cn.wjybxx.base.time.TimeProvider;
import cn.wjybxx.bigcat.pb.ProtobufUtils;
import cn.wjybxx.concurrent.*;
import it.unimi.dsi.fastutil.longs.Long2ObjectLinkedOpenHashMap;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import javax.annotation.Nonnull;
import javax.annotation.concurrent.ThreadSafe;
import java.nio.charset.StandardCharsets;
import java.util.List;
import java.util.Map;
import java.util.Objects;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.TimeUnit;
import java.util.function.BiConsumer;
import java.util.function.Consumer;

/**
 * 1.设置属性应该启动Node之前，运行时不可修改对象的属性
 * 2.所有的线程切换都在该类中，避免代码分散
 * 3.该模块仅负责服务器之间的rpc通信
 *
 * @author wjybxx
 * date - 2023/10/28
 */
@SuppressWarnings("unused")
public final class RpcSupport implements WorkerModule {

    private static final Logger logger = LoggerFactory.getLogger(RpcSupport.class);

    /** 连接id -- 每次启动时应当分配新的id */
    private long conId;
    /** rpc超时时间 */
    private long timeoutMs = 15 * 1000;
    /** 是否启用日志 */
    private boolean enableLog = false;
    /** 当前是否可修改配置数据 -- 也可看做是否已启动标记 */
    private volatile boolean mutable = true;

    /**
     * 为request分配id
     * 1.非线程安全，只在node线程访问。
     * 2.这样可保证node发送出去的请求id是有序的。
     */
    private long sequencer = 0;
    /** 保持插入序很重要 */
    private final Long2ObjectLinkedOpenHashMap<RpcRequestStub> requestStubMap = new Long2ObjectLinkedOpenHashMap<>(100);
    /** 用于支持同步调用 */
    private final Map<Long, WatcherMgr.Watcher<RpcResponse>> watcherMap = new ConcurrentHashMap<>(8);

    private Node node;
    private WorkerAddr selfAddr;
    private RpcMethodRegistry methodRegistry;
    private RpcSerializer serializer;
    private NodeRpcRouter router;
    private TimeProvider timeProvider;

    // region 设置

    public long getConId() {
        return conId;
    }

    public RpcSupport setConId(long conId) {
        ensureMutable();
        this.conId = conId;
        return this;
    }

    public long getTimeoutMs() {
        return timeoutMs;
    }

    public RpcSupport setTimeoutMs(long timeoutMs) {
        ensureMutable();
        this.timeoutMs = Math.max(0, timeoutMs);
        return this;
    }

    public boolean isEnableLog() {
        return enableLog;
    }

    public RpcSupport setEnableLog(boolean enableLog) {
        this.enableLog = enableLog;
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
        this.methodRegistry = node.injector().getInstance(RpcMethodRegistry.class);
        this.router = node.injector().getInstance(NodeRpcRouter.class);
    }

    @Override
    public void start() {
        if (conId == 0) {
            conId = Math.abs(MathCommon.SHARED_RANDOM.nextLong());
        }
        makeImmutable();
    }

    @Override
    public void update() {
        final long curTime = timeProvider.getTime();
        while (requestStubMap.size() > 0) {
            final long requestId = requestStubMap.firstLongKey();
            final RpcRequestStub requestStub = requestStubMap.get(requestId);
            if (curTime < requestStub.deadline) {
                return;
            }
            logger.info("rpc timeout, requestId {}, target {}", requestId, requestStub.getDestAddr());
            requestStubMap.removeFirst();
            requestStub.future.trySetException(RpcClientException.timeout());
            // 删除watcher
            if (requestStub.request.getInvokeType() == RpcInvokeType.SYNC_CALL) {
                watcherMap.remove(requestStub.request.getRequestId());
            }
        }
    }

    @Override
    public void stop() {
        requestStubMap.clear();
        watcherMap.clear();
    }

    // endregion

    // region sendRequest

    public void w2n_send(Worker worker, RpcAddr target, RpcMethodSpec<?> methodSpec) {
        Objects.requireNonNull(worker, "worker");
        Objects.requireNonNull(target, "target");
        Objects.requireNonNull(methodSpec, "methodSpec");

        final RpcRequest request = newRequest(target, methodSpec, RpcInvokeType.ONEWAY);
        if (node.inEventLoop()) {
            sendRequestStep2(worker, request, null);
        } else {
            publishWorkerToNode(worker, request, null);
        }
    }

    public <V> IFuture<V> w2n_call(Worker worker, RpcAddr target, RpcMethodSpec<V> methodSpec) {
        Objects.requireNonNull(worker, "worker");
        Objects.requireNonNull(target, "target");
        Objects.requireNonNull(methodSpec, "methodSpec");

        final RpcRequest request = newRequest(target, methodSpec, RpcInvokeType.CALL);
        final IPromise<V> promise = worker.newPromise();
        if (node.inEventLoop()) {
            sendRequestStep2(worker, request, promise);
        } else {
            publishWorkerToNode(worker, request, promise);
        }
        return promise;
    }

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
        // 检测调用自身上的同步rpc服务
        if (router.isLocalAddr(target)
                && worker.services().contains(methodSpec.getServiceId())) {
            throw new BlockingOperationException("deadlock");
        }
        // 只阻塞发起调用的线程
        RpcRequest request = newRequest(target, methodSpec, RpcInvokeType.SYNC_CALL);
        IPromise<RpcResponse> promise = new Promise<>(); // promise允许阻塞
        try {
            if (node.inEventLoop()) {
                sendRequestStep2(worker, request, promise);
            } else {
                publishWorkerToNode(worker, request, promise);
            }
            RpcResponse response = promise.get(timeoutMs, TimeUnit.MILLISECONDS);
            // 使用之前反序列化 -- 这几行需要重复编码
            RpcMethodInfo<?, ?> methodInfo = methodRegistry.getMethodInfo(request.getServiceId(), request.getMethodId());
            if (response.isBytes() && !decodeResult(response, methodInfo)) {
                response.setFailed(RpcErrorCodes.LOCAL_DESERIALIZE_FAILED, "data error");
            }
            if (enableLog) {
                logRcvResponse(response, false);
            }
            // 返回给用户
            if (response.getErrorCode() == 0) {
                @SuppressWarnings("unchecked") V result = (V) response.getData();
                return result;
            } else {
                throw RpcServerException.newServerException(response);
            }
        } catch (Exception e) {
            ThreadUtils.recoveryInterrupted(e);
            throw RpcClientException.wrapOrRethrow(e);
        }
    }

    /** 收到worker到node的request */
    void sendRequestStep2(Worker worker, RpcRequest request, IPromise<?> promise) {
        // 不检测延迟序列化，以允许进程内共享方法参数对象
        request.setRequestId(++sequencer);
        request.setTime(timeProvider.getTime());

        if (request.getInvokeType() == RpcInvokeType.SYNC_CALL) {
            @SuppressWarnings("unchecked") IPromise<RpcResponse> castPromise = (IPromise<RpcResponse>) promise;
            // 理论上到达这里的时候，可能请求线程已经超时了，暂不处理
            if (enableLog) {
                logSndRequest(request);
            }
            if (!router.send(request)) {
                logger.info("rpc send failure, target " + request.getDestAddr());
                // 同步调用，发送失败时立即失败
                RpcResponse response = newFailedResponse(request, RpcErrorCodes.LOCAL_ROUTER_EXCEPTION, "Failed to send request");
                castPromise.trySetResult(response);
            } else {
                // 理论上send之前添加watcher更安全，但我们的业务并不会在send的时候立即执行rpc请求，因此不会立即完成
                RpcResponseWatcher watcher = new RpcResponseWatcher(conId, request.getRequestId(), castPromise);
                watcherMap.put(request.getRequestId(), watcher);
                // 同步调用也保留存根，确保watcher及时删除
                final long deadline = timeProvider.getTime() + timeoutMs;
                requestStubMap.put(request.getRequestId(), new RpcRequestStub(worker, request, promise, deadline));
            }
        } else {
            if (enableLog) {
                logSndRequest(request);
            }
            if (!router.send(request)) {
                logger.info("rpc send failure, target " + request.getDestAddr());
            }
            // 发送失败不立即失败，保持先请求的先失败
            if (request.getInvokeType() == RpcInvokeType.CALL) {
                final long deadline = timeProvider.getTime() + timeoutMs;
                requestStubMap.put(request.getRequestId(), new RpcRequestStub(worker, request, promise, deadline));
            }
        }
    }

    /** 从worker发布到Node */
    private void publishWorkerToNode(Worker worker, RpcRequest request, IPromise<?> promise) {
        long seq = node.nextSequence();
        if (seq < 0) return; // shutdown
        WorkerEvent event = node.getEvent(seq);
        event.setType(FxUtils.TYPE_WORKER_NODE_REQUEST);
        event.obj1 = worker;
        event.obj2 = request;
        event.obj3 = promise;
        node.publish(seq);
    }

    // endregion

    // region sendResponse

    /** node或worker线程调用 */
    private void sendResponse(RpcResponse response) {
        // 参数可共享的情况下，延迟序列化（分担主线程开销）
        if (!response.isSharable() && !response.isNullOrBytes()) {
            RpcMethodInfo<?, ?> methodInfo = methodRegistry.getMethodInfo(response.getServiceId(), response.getMethodId());
            encodeResult(response, methodInfo);
        }
        if (node.inEventLoop()) {
            sendResponseStep2(response);
        } else {
            long seq = node.nextSequence();
            if (seq < 0) return; // shutdown
            WorkerEvent event = node.getEvent(seq);
            event.setType(FxUtils.TYPE_WORKER_NODE_RESPONSE);
            event.obj1 = response;
            node.publish(seq);
        }
    }

    void sendResponseStep2(RpcResponse response) {
        // 不检测延迟序列化，以允许进程内共享结果对象
        if (enableLog) {
            logSndResponse(response);
        }
        if (!router.send(response)) {
            logger.warn("rpc send response failure, dest {}", response.getDestAddr());
        }
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
        if (node.inEventLoop()) {
            onRcvRequestStep2(request);
        } else {
            long seq = node.nextSequence();
            if (seq < 0) return; // shutdown
            WorkerEvent event = node.getEvent(seq);
            event.setType(FxUtils.TYPE_NET_NODE_REQUEST);
            event.setObj1(request);
            node.publish(seq);
        }
    }

    /** 当前在Node线程 */
    public void onRcvRequestStep2(final RpcRequest request) {
        RpcMethodInfo<?, ?> methodInfo = methodRegistry.getMethodInfo(request.getServiceId(), request.getMethodId());
        if (methodInfo == null) {
            unsupportedInterface(request);
            return;
        }
        // 在使用之前需要先反序列化
        if (request.isBytes() && !decodeParameter(request, methodInfo)) {
            deserializeFailed(request);
            return;
        }
        if (enableLog) {
            logRcvRequest(request);
        }
        // 判断是否对外提供服务
        ServiceInfo serviceInfo = node.serviceInfoMap().get(request.getServiceId());
        if (serviceInfo == null || serviceInfo.workerList.isEmpty()) {
            unsupportedInterface(request);
            return;
        }
        List<Worker> workerList = serviceInfo.workerList;
        if (workerList.size() > 1 && router.isBroadcastWorkerAddr(request.getDestAddr())) {
            // 广播 - 逆序迭代(顺序不应该产生影响)，最后一个worker不拷贝协议
            byte[] bytesParameters = serializer.write(request.getData(), methodInfo.parameterType);
            for (int i = workerList.size() - 1; i >= 0; i--) {
                Worker worker = workerList.get(i);
                RpcRequest clonedRequest = i == 0 ? request : deepCopy(request, bytesParameters, methodInfo);
                if (worker != node) {
                    publishNodeToWorker(worker, request);
                } else {
                    onRcvRequestStep3(worker, clonedRequest);
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
                publishNodeToWorker(worker, request);
            } else {
                onRcvRequestStep3(worker, request);
            }
        }
    }

    private void publishNodeToWorker(Worker worker, RpcRequest request) {
        long seq = worker.nextSequence();
        if (seq < 0) return; // shutdown
        WorkerEvent event = worker.getEvent(seq);
        event.setType(FxUtils.TYPE_NODE_WORKER_REQUEST);
        event.obj1 = worker;
        event.obj2 = request;
        worker.publish(seq);
    }

    /** 当前在worker线程 */
    <T> void onRcvRequestStep3(final Worker worker, RpcRequest request) {
        WorkerCtx workerCtx = worker.workerCtx();
        RpcMethodProxy proxy = workerCtx.rpcRegistry.getProxy(request.getServiceId(), request.getMethodId());
        if (proxy == null) {
            unsupportedInterface(request);
            return;
        }
        // 拦截测试
        int code = workerCtx.rpcInterceptor == null ? 0 : workerCtx.rpcInterceptor.test(request);
        if (code != 0) {
            reject(request, code);
            return;
        }
        // 执行调用
        RpcContextImpl<T> context = new RpcContextImpl<>(request, this);
        if (!RpcInvokeType.isCall(request.getInvokeType())) {
            // Oneway - 不需要结果
            try {
                proxy.invoke(context, request.getData());
            } catch (Throwable e) {
                logInvokeException(request, e);
            }
        } else {
            // Call -- 监听future完成事件
            try {
                final Object result = proxy.invoke(context, request.getData());
                if (context.isManualReturn()) {
                    return; // 用户自行管理结果
                }
                if (result instanceof IFuture<?>) { // 异步获取结果
                    @SuppressWarnings("unchecked") IFuture<T> future = (IFuture<T>) result;
                    future.onCompleted(context, 0);
                } else if (result instanceof CompletableFuture<?>) {
                    @SuppressWarnings("unchecked") CompletableFuture<T> future = (CompletableFuture<T>) result;
                    future.whenComplete(context);
                } else {
                    // 立即得到了结果
                    @SuppressWarnings("unchecked") T castReult = (T) result;
                    context.sendResult(castReult);
                }
            } catch (Throwable e) {
                logInvokeException(request, e);
                context.sendError(e);
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
        if (RpcInvokeType.isCall(request.getInvokeType())) {
            sendResponse(newFailedResponse(request, RpcErrorCodes.SERVER_UNSUPPORTED_INTERFACE, ""));
        }
    }

    /** 请求被拒绝 */
    private void reject(RpcRequest request, int code) {
        logger.warn("request denied, src {}, serviceId={}, methodId={}",
                request.getSrcAddr(), request.getServiceId(), request.getMethodId());
        if (RpcInvokeType.isCall(request.getInvokeType())) {
            sendResponse(newFailedResponse(request, code, ""));
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
        if (node.inEventLoop()) {
            onRcvResponseStep2(response);
        } else {
            long seq = node.nextSequence();
            if (seq < 0) return; // shutdown
            WorkerEvent event = node.getEvent(seq);
            event.setType(FxUtils.TYPE_NET_NODE_RESPONSE);
            event.setObj1(response);
            node.publish(seq);
        }
    }

    /** 当前在Node线程 */
    void onRcvResponseStep2(RpcResponse response) {
        RpcMethodInfo<?, ?> methodInfo = methodRegistry.getMethodInfo(response.getServiceId(), response.getMethodId());
        if (methodInfo == null) {
            return;
        }
        // 使用之前反序列化
        if (response.isBytes() && !decodeResult(response, methodInfo)) {
            response.setFailed(RpcErrorCodes.LOCAL_DESERIALIZE_FAILED, "data error");
        }
        final RpcRequestStub requestStub = requestStubMap.remove(response.getRequestId());
        if (enableLog) {
            logRcvResponse(response, requestStub == null);
        }
        if (requestStub == null) {
            return;
        }
        Worker worker = requestStub.worker;
        @SuppressWarnings("unchecked") IPromise<Object> future = (IPromise<Object>) requestStub.future;
        if (worker.inEventLoop()) {
            onRcvResponseStep3(response, future);
        } else {
            long seq = worker.nextSequence();
            if (seq < 0) return; // shutdown
            WorkerEvent event = worker.getEvent(seq);
            event.setType(FxUtils.TYPE_NODE_WORKER_RESPONSE);
            event.obj1 = response;
            event.obj2 = future;
            worker.publish(seq);
        }
    }

    /** 当前在Worker线程 */
    void onRcvResponseStep3(RpcResponse response, IPromise<Object> promise) {
        final int errorCode = response.getErrorCode();
        if (errorCode == 0) {
            promise.trySetResult(response.getData());
        } else {
            promise.trySetException(RpcServerException.newServerException(response));
        }
    }

    // endregion

    // region factory

    /** worker线程调用 -- worker可能是node自身 */
    private RpcRequest newRequest(RpcAddr target, RpcMethodSpec<?> methodSpec, int invokeType) {
        RpcRequest request = new RpcRequest(conId, selfAddr, target, invokeType, 0, methodSpec);
        // 参数可共享的情况下，延迟序列化（分担主线程开销）(本地调用的情况下，还可以不序列化)
        if (!request.isSharable() && !request.isNullOrBytes()) {
            RpcMethodInfo<?, ?> methodInfo = methodRegistry.getMethodInfo(request.getServiceId(), request.getMethodId());
            encodeParameter(request, methodInfo);
        }
        return request;
    }

    private RpcResponse newFailedResponse(RpcRequest request, int errorCode, String msg) {
        RpcResponse response = new RpcResponse(request);
        response.setFailed(errorCode, msg);
        return response;
    }

    private RpcResponse newFailedResponse(RpcRequest request, Throwable ex) {
        Objects.requireNonNull(ex);
        RpcResponse response = new RpcResponse(request);
        response.setFailed(ex);
        return response;
    }

    /** 深度拷贝rpc请求参数 */
    private RpcRequest deepCopy(RpcRequest src, byte[] bytesParameters, RpcMethodInfo<?, ?> methodInfo) {
        RpcRequest request = new RpcRequest(src.getConId(), src.getSrcAddr(), src.getDestAddr());
        request.setRequestId(src.getRequestId());
        request.setServiceId(src.getServiceId());
        request.setMethodId(src.getMethodId());
        request.setInvokeType(src.getInvokeType());
        request.setData(bytesParameters);
        decodeParameter(request, methodInfo);
        return request;
    }

    // endregion

    // region 编解码

    /** 序列化rpc参数 */
    private void encodeParameter(RpcRequest request, RpcMethodInfo<?, ?> methodInfo) {
        if (request.getData() == null) { // null不序列化
            return;
        }
        byte[] bytes;
        if (methodInfo.parameterParser != null) {
            bytes = ProtobufUtils.toBytes(request.getData());
        } else {
            bytes = serializer.write(request.getData(), methodInfo.parameterType);
        }
        request.setData(bytes);
    }

    /** 反序列化rpc参数 */
    private boolean decodeParameter(RpcRequest request, RpcMethodInfo<?, ?> methodInfo) {
        try {
            byte[] data = (byte[]) request.getData();
            Object parameter;
            if (methodInfo.parameterParser != null) {
                parameter = methodInfo.parameterParser.parseFrom(data);
            } else {
                parameter = serializer.read(data, methodInfo.resultType);
            }
            request.setData(parameter);
            return true;
        } catch (Exception e) {
            logger.info("decode parameters caught exception, serviceId {}, methodId {}",
                    request.getServiceId(), request.getMethodId(), e);
            return false;
        }
    }

    private void encodeResult(RpcResponse response, RpcMethodInfo<?, ?> methodInfo) {
        if (response.getData() == null) { // null不序列化
            return;
        }
        byte[] bytes;
        if (response.isFailed()) {
            bytes = response.getErrorMsg().getBytes(StandardCharsets.UTF_8);
        } else if (methodInfo.resultParser != null) {
            bytes = ProtobufUtils.toBytes(response.getData());
        } else {
            bytes = serializer.write(response.getData(), methodInfo.resultType);
        }
        response.setData(bytes);
    }

    /** 反序列化结果 -- 在使用之前；可顺带进行部分初始化 */
    private boolean decodeResult(RpcResponse response, RpcMethodInfo<?, ?> methodInfo) {
        try {
            byte[] data = (byte[]) response.getData();
            if (response.isFailed()) {
                response.setData(new String(data, StandardCharsets.UTF_8));
                return true;
            }
            Object result;
            if (methodInfo.resultParser != null) {
                result = methodInfo.resultParser.parseFrom(data);
            } else {
                result = serializer.read(data, methodInfo.resultType);
            }
            response.setData(result);
            return true;
        } catch (Exception e) {
            logger.info("decode result caught exception, serviceId {}, methodId {}",
                    response.getServiceId(), response.getMethodId(), e);
            return false;
        }
    }
    // endregion

    // region context

    private static class RpcContextImpl<V> implements RpcContext<V>,
            Consumer<IFuture<V>>,
            BiConsumer<V, Throwable> {

        final RpcRequest request;
        final RpcSupport rpcSupport;
        int options; // 这里不提前构建Response，避免用户重复调用Send时产生异常

        RpcContextImpl(RpcRequest request, RpcSupport rpcSupport) {
            this.request = request;
            this.rpcSupport = rpcSupport;
        }

        @Override
        public long conId() {
            return request.getConId();
        }

        @Override
        public RpcAddr remoteAddr() {
            return request.getSrcAddr();
        }

        @Override
        public boolean isSharable() {
            return BitFlags.isSet(options, MASK_RESULT_SHARABLE);
        }

        @Override
        public void setSharable(boolean sharable) {
            options = BitFlags.set(options, MASK_RESULT_SHARABLE, sharable);
        }

        @Override
        public boolean isManualReturn() {
            return BitFlags.isSet(options, MASK_RESULT_MANUAL);
        }

        @Override
        public void setManualReturn(boolean value) {
            options = BitFlags.set(options, MASK_RESULT_MANUAL, value);
        }

        @Override
        public void sendResult(V result) {
            RpcResponse response = new RpcResponse(request);
            response.setSharable(isSharable());
            response.setSuccess(result);
            rpcSupport.sendResponse(response);
        }

        @Override
        public void sendResult(byte[] result) {
            RpcResponse response = new RpcResponse(request);
            if (result == null || isSharable()) {
                response.setSharable(true);
                response.setSuccess(result);
            } else {
                response.setSuccess(result.clone());
            }
            rpcSupport.sendResponse(response);
        }

        @Override
        public void sendError(int errorCode, String msg) {
            if (!RpcErrorCodes.isUserCode(errorCode)) {
                throw new IllegalArgumentException("invalid errorCode: " + errorCode);
            }
            RpcResponse response = new RpcResponse(request);
            response.setFailed(errorCode, msg);
            rpcSupport.sendResponse(response);
        }

        @Override
        public void sendError(Throwable ex) {
            Objects.requireNonNull(ex);
            RpcResponse response = new RpcResponse(request);
            response.setFailed(ex);
            rpcSupport.sendResponse(response);
        }

        @Override
        public void accept(IFuture<V> future) {
            if (future.isSucceeded()) {
                sendResult(future.resultNow());
            } else {
                sendError(future.exceptionNow(false));
            }
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

    @ThreadSafe
    private static class RpcResponseWatcher implements WatcherMgr.Watcher<RpcResponse> {

        private final long conId;
        private final long requestId;
        private final IPromise<RpcResponse> future;

        private RpcResponseWatcher(long conId, long requestId, IPromise<RpcResponse> future) {
            this.future = future;
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
            future.trySetResult(response);
        }
    }

    private static class RpcRequestStub {

        final Worker worker;
        final RpcRequest request;
        final IPromise<?> future;
        final long deadline;

        RpcRequestStub(Worker worker, RpcRequest request, IPromise<?> future, long deadline) {
            this.worker = worker;
            this.future = future;
            this.deadline = deadline;
            this.request = request;
        }

        public long getDeadline() {
            return deadline;
        }

        public RpcAddr getDestAddr() {
            return request.getDestAddr();
        }

        public RpcRequest getRequest() {
            return request;
        }
    }

    // endregion

    // region debug日志

    private void logSndRequest(RpcRequest request) {
        if (logger.isDebugEnabled()) {
            RpcMethodInfo<?, ?> methodInfo = methodRegistry.getMethodInfo(request.getServiceId(), request.getMethodId());
            logger.debug("snd rpc request, request {}", request.toDetailLog(methodInfo.serviceName, methodInfo.methodName));
        } else if (logger.isInfoEnabled()) {
            logger.info("snd rpc request, request {}", request.toSimpleLog());
        }
    }

    private void logRcvRequest(RpcRequest request) {
        if (logger.isDebugEnabled()) {
            RpcMethodInfo<?, ?> methodInfo = methodRegistry.getMethodInfo(request.getServiceId(), request.getMethodId());
            logger.debug("rcv rpc request, request {}", request.toDetailLog(methodInfo.serviceName, methodInfo.methodName));
        } else if (logger.isInfoEnabled()) {
            logger.info("rcv rpc request, request {}", request.toSimpleLog());
        }
    }

    private void logSndResponse(RpcResponse response) {
        if (logger.isDebugEnabled()) {
            RpcMethodInfo<?, ?> methodInfo = methodRegistry.getMethodInfo(response.getServiceId(), response.getMethodId());
            logger.debug("snd rpc response, response {}", response.toDetailLog(methodInfo.serviceName, methodInfo.methodName));
        } else if (logger.isInfoEnabled()) {
            logger.info("snd rpc response, response {}", response.toSimpleLog());
        }
    }

    private void logRcvResponse(RpcResponse response, boolean timeout) {
        String format = timeout ? "rcv rpc response, but request is timeout, response {}"
                : "rcv rpc response, response {}";
        if (logger.isDebugEnabled()) {
            RpcMethodInfo<?, ?> methodInfo = methodRegistry.getMethodInfo(response.getServiceId(), response.getMethodId());
            logger.debug(format, response.toDetailLog(methodInfo.serviceName, methodInfo.methodName));
        } else if (logger.isInfoEnabled()) {
            logger.info(format, response.toSimpleLog());
        }
    }

    // endregion
}