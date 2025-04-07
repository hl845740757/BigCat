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

import cn.wjybxx.base.MathCommon;
import cn.wjybxx.base.ObjectUtils;
import cn.wjybxx.base.ThreadUtils;
import cn.wjybxx.base.ex.NoLogRequiredException;
import cn.wjybxx.base.pool.DefaultObjectPool;
import cn.wjybxx.base.pool.ObjectPool;
import cn.wjybxx.base.time.TimeProvider;
import cn.wjybxx.bigcat.pb.ProtobufUtils;
import cn.wjybxx.concurrent.*;
import it.unimi.dsi.fastutil.longs.Long2ObjectLinkedOpenHashMap;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.nio.charset.StandardCharsets;
import java.util.List;
import java.util.Map;
import java.util.Objects;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.TimeUnit;

/**
 * Rpc支持模块
 * 1.设置属性应该启动Node之前，运行时不可修改对象的属性
 * 2.所有的线程切换都在该类中，避免代码分散
 * 3.该模块仅负责服务器之间的rpc通信
 *
 * @author wjybxx
 * date - 2023/10/28
 */
@SuppressWarnings("unused")
public final class NodeRpcSupport extends EventLoopModule implements RpcSupport, IAgentEventHandler<WorkerEvent> {

    private static final Logger logger = LoggerFactory.getLogger(NodeRpcSupport.class);

    /** 连接id -- 每次启动时应当分配新的id */
    private long conId;
    /** rpc超时时间 */
    private long timeoutMs = 15 * 1000;
    /** 是否启用日志 -- 允许运行时调整；暂非volatile，但安全 */
    private boolean enableLog;
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
    private final Map<Long, IPromise<RpcResult>> watcherMap = new ConcurrentHashMap<>(8);
    /** 服务器发起请求的数量不多 */
    private final ObjectPool<RpcRequestStub> stubPool = new DefaultObjectPool<>(RpcRequestStub::new, RpcRequestStub::reset, 100);

    private Node node;
    private WorkerAddr selfAddr;
    private RpcMethodRegistry methodRegistry;
    private RpcSerializer serializer;
    private RpcRouter router;
    private TimeProvider timeProvider;

    // region 设置

    public long getConId() {
        return conId;
    }

    public void setConId(long conId) {
        ensureMutable();
        this.conId = conId;
    }

    public long getTimeoutMs() {
        return timeoutMs;
    }

    public void setTimeoutMs(long timeoutMs) {
        ensureMutable();
        this.timeoutMs = Math.max(0, timeoutMs);
    }

    public boolean isEnableLog() {
        return enableLog;
    }

    public void setEnableLog(boolean enableLog) {
        this.enableLog = enableLog; // log允许运行时调整
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

    // region 生命周期

    @Override
    public void resolveDependence() {
        this.node = (Node) getEntity();
        this.selfAddr = node.nodeAddr();
        this.timeProvider = node.injector().getInstance(TimeProvider.class);
        this.serializer = node.injector().getInstance(RpcSerializer.class);
        this.methodRegistry = node.injector().getInstance(RpcMethodRegistry.class);
        this.router = node.injector().getInstance(RpcRouter.class);
    }

    @Override
    public void start() {
        if (conId == 0) {
            conId = Math.abs(MathCommon.SHARED_RANDOM.nextLong());
        }
        makeImmutable();
        subscribeEvents();
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
            requestStubMap.removeFirst();
            logger.info("rpc timeout, requestId {}, target {}, serviceId {}, methodId {}",
                    requestId, requestStub.destAddr,
                    requestStub.serviceId, requestStub.methodId);

            if (requestStub.invokeType == RpcInvokeType.SYNC_CALL) {
                watcherMap.remove(requestStub.requestId); // 这里不能操作Promise
            } else {
                requestStub.promise.trySetException(RpcClientException.timeout());
            }
            stubPool.release(requestStub);
        }
    }

    @Override
    public void stop() {
        requestStubMap.clear();
        watcherMap.clear();
        stubPool.clear();
    }

    private void subscribeEvents() {
        // net到node的请求和响应
        node.subscribe(FxUtils.TYPE_NET_NODE_REQUEST, this);
        node.subscribe(FxUtils.TYPE_NET_NODE_RESPONSE, this);
        // worker到node的请求和响应
        node.subscribe(FxUtils.TYPE_WORKER_NODE_REQUEST, this);
        node.subscribe(FxUtils.TYPE_WORKER_NODE_RESPONSE, this);
    }

    @Override
    public void onEvent(long sequence, WorkerEvent event) throws Exception {
        switch (event.getType()) {
            case FxUtils.TYPE_NET_NODE_REQUEST -> {
                onRcvRequestStep2((RpcRequest) event.obj1);
            }
            case FxUtils.TYPE_NET_NODE_RESPONSE -> {
                onRcvResponseStep2((RpcResponse) event.obj1);
            }
            case FxUtils.TYPE_WORKER_NODE_REQUEST -> {
                sendRequestStep2((Worker) event.obj1, (RpcRequest) event.obj2, (IPromise<?>) event.obj3);
            }
            case FxUtils.TYPE_WORKER_NODE_RESPONSE -> {
                sendResponseStep2((RpcResponse) event.obj1);
            }
            default -> throw new AssertionError();
        }
    }

    // endregion

    // region sendRequest

    public void w2n_send(Worker worker, WorkerAddr target, RpcMethodSpec<?> methodSpec) {
        Objects.requireNonNull(target, "target");
        Objects.requireNonNull(methodSpec, "methodSpec");

        final RpcRequest request = newRequest(target, methodSpec, RpcInvokeType.ONEWAY);
        if (worker == node) {
            sendRequestStep2(worker, request, null);
        } else {
            publishWorkerToNode(worker, request, null);
        }
    }

    public <V> IFuture<V> w2n_call(Worker worker, WorkerAddr target, RpcMethodSpec<V> methodSpec) {
        Objects.requireNonNull(target, "target");
        Objects.requireNonNull(methodSpec, "methodSpec");

        final RpcRequest request = newRequest(target, methodSpec, RpcInvokeType.CALL);
        final IPromise<V> promise = worker.newPromise();
        if (worker == node) {
            sendRequestStep2(worker, request, promise);
        } else {
            publishWorkerToNode(worker, request, promise);
        }
        return promise;
    }

    public <V> V w2n_syncCall(Worker worker, WorkerAddr target, RpcMethodSpec<V> methodSpec) {
        return w2n_syncCall(worker, target, methodSpec, timeoutMs);
    }

    public <V> V w2n_syncCall(Worker worker, WorkerAddr target, RpcMethodSpec<V> methodSpec, long timeoutMs) {
        Objects.requireNonNull(target, "target");
        Objects.requireNonNull(methodSpec, "methodSpec");
        if (timeoutMs <= 0) {
            timeoutMs = this.timeoutMs;
        }
        // 检测同步调用自身的rpc服务
        if (target.nodeId == selfAddr.nodeId && worker.services().contains(methodSpec.getServiceId())) {
            throw new BlockingOperationException();
        }
        // 只阻塞发起调用的线程
        RpcRequest request = newRequest(target, methodSpec, RpcInvokeType.SYNC_CALL);
        IPromise<RpcResult> promise = new Promise<>();
        try {
            if (worker == node) {
                sendRequestStep2(worker, request, promise);
            } else {
                publishWorkerToNode(worker, request, promise);
            }
            RpcResult result = promise.get(timeoutMs, TimeUnit.MILLISECONDS);
            if (result.isSucceeded()) {
                @SuppressWarnings("unchecked") V castV = (V) result.getData();
                return castV;
            }
            throw RpcServerException.newServerException(result.getErrorCode(), result.getErrorMsg());
        } catch (Exception e) {
            ThreadUtils.recoveryInterrupted(e);
            throw RpcClientException.wrapException(e);
        }
    }

    /** 收到worker到node的request */
    private void sendRequestStep2(Worker worker, RpcRequest request, IPromise<?> promise) {
        request.setRequestId(++sequencer);
        request.setCreateTime(timeProvider.getTime());

        // Request加入池化逻辑后，在调用send后不可再访问，必要数据需要提前拷贝 -- 我们拷贝到Stub上
        RpcRequestStub stub = stubPool.acquire();
        initStub(stub, worker, request, promise);
        // 发送请求
        if (enableLog) {
            logger.info("snd rpc request {}", request);
        }
        final boolean sendFailed = !router.send(request);
        if (sendFailed) {
            logger.info("rpc send failed, target {}", stub.destAddr);
        }
        if (stub.invokeType == RpcInvokeType.SYNC_CALL) {
            @SuppressWarnings("unchecked") IPromise<RpcResult> castPromise = (IPromise<RpcResult>) promise;
            if (sendFailed) {
                castPromise.trySetResult(new RpcResult(RpcErrorCodes.LOCAL_ROUTER_EXCEPTION, "Failed to send request"));
            } else {
                // 理论上send之前添加watcher更安全，但我们的业务并不会在send的时候立即执行rpc请求，因此不会立即完成
                // 同步调用也保留存根，确保watcher及时删除
                watcherMap.put(stub.requestId, castPromise);
                requestStubMap.put(stub.requestId, stub);
            }
        } else if (stub.invokeType == RpcInvokeType.CALL) {
            // 发送失败不立即失败，保持先请求的先失败
            requestStubMap.put(stub.requestId, stub);
        } else {
            // 单向通知，回收临时的Stub对象
            stubPool.release(stub);
        }
    }

    /** 拷贝必要数据到stub */
    private void initStub(RpcRequestStub stub, Worker worker, RpcRequest request, IPromise<?> promise) {
        stub.worker = worker;
        stub.promise = promise;
        stub.deadline = timeProvider.getTime() + timeoutMs;
        stub.conId = request.getConId();
        stub.destAddr = request.getDestAddr();
        stub.requestId = request.getRequestId();
        stub.serviceId = request.getServiceId();
        stub.methodId = request.getMethodId();
        stub.invokeType = request.getInvokeType();
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

    @Override
    public void sendResult(long conId, WorkerAddr destAddr,
                           long requestId, int serviceId, int methodId,
                           Object result, boolean sharable) {
        RpcResponse response = newResponse(conId, destAddr, requestId, serviceId, methodId);
        response.setSuccess(result);
        response.setSharable(sharable);
        sendResponse(response);
    }

    @Override
    public void sendError(long conId, WorkerAddr destAddr,
                          long requestId, int serviceId, int methodId,
                          int errorCode, String msg) {
        RpcResponse response = newResponse(conId, destAddr, requestId, serviceId, methodId);
        response.setFailed(errorCode, msg);
        sendResponse(response);
    }

    @Override
    public void sendError(long conId, WorkerAddr destAddr,
                          long requestId, int serviceId, int methodId,
                          Throwable ex) {
        Objects.requireNonNull(ex);
        RpcResponse response = newResponse(conId, destAddr, requestId, serviceId, methodId);
        response.setFailed(ex);
        sendResponse(response);
    }

    /** node或worker线程调用 */
    private void sendResponse(RpcResponse response) {
        // 数据不可共享的情况下立即序列化，否则在Node线程序列化（分担主线程开销）(本地调用的情况下，还可以不序列化)
        if (!response.isSharable() && response.getData() != null) {
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

    private void sendResponseStep2(RpcResponse response) {
        if (enableLog) {
            logger.info("snd rpc response {}", response);
        }
        // 不检测延迟序列化，以允许进程内共享结果对象
        if (!router.send(response)) {
            logger.info("rpc send response failure, dest {}", response.getDestAddr());
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
        if (enableLog) {
            logger.info("rcv rpc request {}", request);
        }
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
            reject(request, RpcErrorCodes.SERVER_UNSUPPORTED_INTERFACE);
            return;
        }
        // 在使用之前需要先反序列化
        if (request.isBytes() && !decodeParameter(request, methodInfo)) {
            reject(request, RpcErrorCodes.SERVER_DESERIALIZE_FAILED);
            return;
        }
        // 判断是否对外提供服务
        ServiceInfo serviceInfo = node.serviceInfoMap().get(request.getServiceId());
        if (serviceInfo == null || serviceInfo.workerList.isEmpty()) {
            reject(request, RpcErrorCodes.SERVER_UNSUPPORTED_INTERFACE);
            return;
        }
        List<Worker> workerList = serviceInfo.workerList;
        if (workerList.size() == 1 || ObjectUtils.isBlank(request.getDestAddr().workerId)) {
            // 单播 - 选择一个worker，多worker时hash保证路由的一致性
            Worker worker;
            if (workerList.size() == 1) {
                worker = workerList.get(0);
            } else {
                int idx = request.getSrcAddr().hashCode() % workerList.size();
                worker = workerList.get(idx);
            }
            if (worker == node) {
                onRcvRequestStep3(worker, request);
            } else {
                publishNodeToWorker(worker, request);
            }
        } else {
            // 广播 - 逆序迭代(顺序不应该产生影响)，最后一个worker不拷贝协议
            // 理论上可让router选择执行请求的worker，暂不支持，以后再说
            byte[] bytesParameters = serializer.write(request.getData(), methodInfo.parameterType);
            for (int i = workerList.size() - 1; i >= 0; i--) {
                Worker worker = workerList.get(i);
                RpcRequest clonedRequest = i == 0 ? request : deepCopy(request, bytesParameters, methodInfo);
                if (worker == node) {
                    onRcvRequestStep3(worker, clonedRequest);
                } else {
                    publishNodeToWorker(worker, request);
                }
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
    @SuppressWarnings("unchecked")
    <T> void onRcvRequestStep3(final Worker worker, RpcRequest request) {
        WorkerControlData controlData = worker.controlData();
        RpcMethodProxy<T> proxy = (RpcMethodProxy<T>) controlData.rpcRegistry.getProxy(request.getServiceId(), request.getMethodId());
        if (proxy == null) {
            reject(request, RpcErrorCodes.SERVER_UNSUPPORTED_INTERFACE);
            return;
        }
        // 拦截测试
        int code = controlData.rpcInterceptor == null ? 0 : controlData.rpcInterceptor.test(request);
        if (code != 0) {
            reject(request, code);
            return;
        }
        // Request加入池化逻辑后，我们将关键数据拷贝到Context上，使得Request可立即归还
        RpcContextImpl<T> context = new RpcContextImpl<>(this,
                request.getConId(), request.getSrcAddr(),
                request.getRequestId(),
                request.getServiceId(), request.getMethodId(),
                request.getInvokeType());
        try {
            proxy.invoke(context, request.getData());
        } catch (Throwable e) {
            logInvokeException(request, e);
            context.sendError(e);
        }
        RpcRequest.release(request);
    }

    /** 拒绝客户端请求 */
    private void reject(RpcRequest request, int code) {
        logger.warn("reject the request, reason={}, conId={}, srcAddr={}, serviceId={}, methodId={}",
                code,
                request.getConId(), request.getSrcAddr(),
                request.getServiceId(), request.getMethodId());
        if (RpcInvokeType.isCall(request.getInvokeType())) {
            sendError(request.getConId(), request.getSrcAddr(), // srcAddr
                    request.getRequestId(), request.getServiceId(), request.getMethodId(),
                    code, null);
        }
        RpcRequest.release(request);
    }

    /** 记录执行异常 */
    private static void logInvokeException(RpcRequest request, Throwable ex) {
        if (!(ex instanceof NoLogRequiredException)) {
            logger.warn("invoke caught exception, conId={}, srcAddr={}, serviceId={}, methodId={}",
                    request.getConId(), request.getSrcAddr(),
                    request.getServiceId(), request.getMethodId(),
                    ex);
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
            RpcResponse.release(response);
            return;
        }
        // 不重复打印旧进程的Rpc响应
        if (enableLog) {
            logger.info("rcv rpc response {}", response);
        }
        // watcher需要在IO线程测试
        IPromise<RpcResult> watcher = watcherMap.remove(response.getRequestId());
        if (watcher != null) { // 同步调用结果
            RpcResult result = new RpcResult(response.getErrorCode(), response.getData());
            watcher.trySetResult(result);
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
    private void onRcvResponseStep2(RpcResponse response) {
        RpcMethodInfo<?, ?> methodInfo = methodRegistry.getMethodInfo(response.getServiceId(), response.getMethodId());
        // 使用之前反序列化
        if (response.isBytes() && !decodeResult(response, methodInfo)) {
            response.setFailed(RpcErrorCodes.LOCAL_DESERIALIZE_FAILED, "data error");
        }
        final RpcRequestStub requestStub = requestStubMap.remove(response.getRequestId());
        if (requestStub == null) {
            logger.info("rcv rpc response, but request is timeout, requestId {}", response.getRequestId());
            RpcResponse.release(response);
            return;
        }
        Worker worker = requestStub.worker;
        @SuppressWarnings("unchecked") IPromise<Object> promise = (IPromise<Object>) requestStub.promise;
        if (worker.inEventLoop()) {
            onRcvResponseStep3(promise, response.getErrorCode(), response.getData());
        } else {
            long seq = worker.nextSequence();
            if (seq < 0) return; // shutdown
            WorkerEvent event = worker.getEvent(seq);
            event.setType(FxUtils.TYPE_NODE_WORKER_RESPONSE);
            event.intVal1 = response.getErrorCode();
            event.obj1 = promise;
            event.obj2 = response.getData();
            worker.publish(seq);
        }
        // 归还到池
        stubPool.release(requestStub);
        RpcResponse.release(response);
    }

    /** 当前在Worker线程 */
    void onRcvResponseStep3(IPromise<Object> promise, int errorCode, Object result) {
        if (errorCode == 0) {
            promise.trySetResult(result);
        } else {
            promise.trySetException(RpcServerException.newServerException(errorCode, (String) result));
        }
    }

    // endregion

    // region factory

    /** worker线程调用 -- worker可能是node自身 */
    private RpcRequest newRequest(WorkerAddr target, RpcMethodSpec<?> methodSpec, int invokeType) {
        RpcRequest request = RpcRequest.acquire();
        request.setConId(conId);
        request.setSrcAddr(selfAddr);
        request.setDestAddr(target);

        // 在node线程分配请求id；这里不立即赋值创建时间，存在可见性问题
        request.setInvokeType(invokeType);
        request.setServiceId(methodSpec.getServiceId());
        request.setMethodId(methodSpec.getMethodId());
        request.setData(methodSpec.getParameter());
        request.setSharable(methodSpec.isSharable());

        // 数据不可共享的情况下立即序列化，否则在Node线程序列化（分担主线程开销）(本地调用的情况下，还可以不序列化)
        if (!request.isSharable() && request.getData() != null) {
            RpcMethodInfo<?, ?> methodInfo = methodRegistry.getMethodInfo(request.getServiceId(), request.getMethodId());
            encodeParameter(request, methodInfo);
        }
        return request;
    }

    /** 任意线程调用 */
    private RpcResponse newResponse(long conId, WorkerAddr destAddr,
                                    long requestId, int serviceId, int methodId) {
        RpcResponse response = RpcResponse.acquire();
        response.setConId(conId);
        response.setSrcAddr(selfAddr);
        response.setDestAddr(destAddr);

        response.setRequestId(requestId);
        response.setServiceId(serviceId);
        response.setMethodId(methodId);
        return response;
    }

    /** 深度拷贝rpc请求参数 */
    private RpcRequest deepCopy(RpcRequest src, byte[] bytesParameters, RpcMethodInfo<?, ?> methodInfo) {
        RpcRequest request = RpcRequest.acquire();
        request.setConId(src.getConId());
        request.setSrcAddr(src.getSrcAddr());
        request.setDestAddr(src.getDestAddr());

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
        if (request.getData() instanceof byte[] codedBytes) {
            bytes = codedBytes.clone();
        } else if (methodInfo.parameterParser != null) {
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
            if (methodInfo.parameterType == null) {
                parameter = null; // 对方将void序列化为了空字节数组
            } else if (methodInfo.parameterParser != null) {
                parameter = methodInfo.parameterParser.parseFrom(data);
            } else {
                parameter = serializer.read(data, methodInfo.resultType);
            }
            request.setData(parameter);
            return true;
        } catch (Exception ex) {
            logger.info("decode parameters caught exception, serviceId {}, methodId {}",
                    request.getServiceId(), request.getMethodId(), ex);
            return false;
        }
    }

    private void encodeResult(RpcResponse response, RpcMethodInfo<?, ?> methodInfo) {
        if (response.getData() == null) { // null不序列化
            return;
        }
        byte[] bytes;
        if (response.getData() instanceof byte[] codedBytes) {
            bytes = codedBytes.clone();
        } else if (response.isFailed()) {
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
                response.setData(data.length > 0 ? new String(data, StandardCharsets.UTF_8) : "");
                return true;
            }
            Object result;
            if (methodInfo.resultType == null) {
                result = null; // 对方将void序列化为了空字节数组
            } else if (methodInfo.resultParser != null) {
                result = methodInfo.resultParser.parseFrom(data);
            } else {
                result = serializer.read(data, methodInfo.resultType);
            }
            response.setData(result);
            return true;
        } catch (Exception ex) {
            logger.info("decode result caught exception, serviceId {}, methodId {}",
                    response.getServiceId(), response.getMethodId(), ex);
            return false;
        }
    }
    // endregion
}