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

import cn.wjybxx.base.collection.DefaultIndexedPriorityQueue;
import cn.wjybxx.base.collection.IndexedPriorityQueue;
import cn.wjybxx.base.ex.ErrorCodeException;
import cn.wjybxx.base.ex.NoLogRequiredException;
import cn.wjybxx.base.pool.DefaultObjectPool;
import cn.wjybxx.base.pool.ObjectPool;
import cn.wjybxx.concurrent.*;
import org.apache.commons.lang3.exception.ExceptionUtils;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.ArrayList;
import java.util.List;
import java.util.Objects;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.ExecutionException;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.TimeoutException;

/**
 * 服务器间的{@link RpcClient}实现，是{@link S2SSessionMgr}的门面实现
 *
 * @author wjybxx
 * date - 2023/10/28
 */
public final class S2SRpcClient extends EventLoopModule implements RpcClientImpl, RpcClient,
        IAgentEventHandler<WorkerEvent> {

    private static final Logger logger = LoggerFactory.getLogger(S2SRpcClient.class);

    private Worker worker;
    private WorkerAddr selfAddr;
    private TimeModule timeModule;
    private RpcProxyRegistry proxyRegistry;
    private S2SSessionMgr sessionMgr;
    private RpcSupport rpcSupport;

    /** rpc默认超时时间 */
    private long timeoutMs = 15 * 1000;
    /** 是否允许本地调用共享对象 - 可禁用{@link RpcProtocol#isSharable()} */
    private boolean enableLocalSharing = true;
    /** 本地Session，用于Node内的线程通信 */
    private S2SSession localSession;
    /** 超时信息 -- 所有Session的集中处理 */
    private final IndexedPriorityQueue<RpcRequestStub> stubQueue = new DefaultIndexedPriorityQueue<>(RpcRequestStub::compare);
    /** Stub池 -- 不共享，没必要 */
    private final ObjectPool<RpcRequestStub> stubPool = new DefaultObjectPool<>(RpcRequestStub::new, RpcRequestStub::reset, 100);

    @Override
    public void onReady() {
        this.worker = (Worker) getEntity();
        this.selfAddr = worker.workerAddr();
        this.timeModule = worker.injector().getInstance(TimeModule.class);
        this.proxyRegistry = worker.injector().getInstance(RpcProxyRegistry.class);
        this.sessionMgr = worker.injector().getInstance(S2SSessionMgr.class);
        // Node上的组件
        Node node = worker.node();
        this.rpcSupport = node.injector().getInstance(RpcSupport.class);
        // 创建虚拟Session
        this.localSession = new S2SSession(0, selfAddr.nodeId);
    }

    /** rpc超时时间 */
    public long getTimeoutMs() {
        return timeoutMs;
    }

    public void setTimeoutMs(long timeoutMs) {
        if (timeoutMs < 1) throw new IllegalArgumentException("timeoutMs: " + timeoutMs);
        this.timeoutMs = timeoutMs;
    }

    /** 是否启用本地对象共享 */
    public boolean isEnableLocalSharing() {
        return enableLocalSharing;
    }

    public void setEnableLocalSharing(boolean enableLocalSharing) {
        this.enableLocalSharing = enableLocalSharing;
    }

    /** 注册session */
    public void addSession(long sessionId) {
        rpcSupport.addSession(sessionId, worker);
    }

    /** 删除相关session数据 */
    public void removeSession(long sessionId) {
        rpcSupport.removeSession(sessionId);

        List<RpcRequestStub> list = new ArrayList<>();
        for (RpcRequestStub stub : stubQueue) {
            if (stub.sessionId == sessionId) {
                list.add(stub);
            }
        }
        for (RpcRequestStub stub : list) {
            stub.promise.trySetException(RpcClientException.sessionClosed(stub.destAddr));
            stubQueue.removeTyped(stub);
            stubPool.release(stub);
        }
    }

    private S2SSession getSession(long sessionId) {
        if (sessionId == 0) return localSession;
        return sessionMgr.getSession(sessionId);
    }

    private S2SSession getSessionOfNode(int nodeId) {
        return nodeId == selfAddr.nodeId ? localSession : sessionMgr.getSessionOfNode(nodeId);
    }

    // region sendRequest

    @Override
    public void send(WorkerAddr destAddr, RpcMethodSpec<?> methodSpec) {
        assert worker.inEventLoop();
        S2SSession session = getSessionOfNode(destAddr.nodeId);
        if (session == null) {
            return;
        }
        final RpcRequest request = newRequest(session, destAddr, methodSpec, RpcInvokeType.ONEWAY);
        rpcSupport.sendRequest(request);
    }

    @Override
    public <V> IFuture<V> call(WorkerAddr destAddr, RpcMethodSpec<V> methodSpec) {
        return call(destAddr, methodSpec, 0);
    }

    @Override
    public <V> IFuture<V> call(WorkerAddr destAddr, RpcMethodSpec<V> methodSpec, long timeoutMs) {
        assert worker.inEventLoop();
        final S2SSession session = getSessionOfNode(destAddr.nodeId);
        if (session == null) {
            return Promise.failedPromise(RpcClientException.sessionNotExist(destAddr));
        }
        if (timeoutMs <= 0) {
            timeoutMs = this.timeoutMs;
        }
        final RpcRequest request = newRequest(session, destAddr, methodSpec, RpcInvokeType.CALL);
        final IPromise<V> promise = worker.newPromise(); // 不可在Worker上阻塞
        // 先保留存根再发送
        {
            final RpcRequestStub stub = newStub(request, promise, timeModule.getTime() + timeoutMs);
            session.stubMap.put(stub.requestId, stub);
            stubQueue.add(stub);
        }
        rpcSupport.sendRequest(request); // send以后不可再访问request，可能已被回收
        return promise;
    }

    @Override
    public <V> V syncCall(WorkerAddr destAddr, RpcMethodSpec<V> methodSpec) throws TimeoutException, InterruptedException {
        return syncCall(destAddr, methodSpec, 0);
    }

    @Override
    public <V> V syncCall(WorkerAddr destAddr, RpcMethodSpec<V> methodSpec, long timeoutMs) throws TimeoutException, InterruptedException {
        assert worker.inEventLoop();
        final S2SSession session = getSessionOfNode(destAddr.nodeId);
        if (session == null) {
            throw RpcClientException.sessionNotExist(destAddr);
        }
        if (timeoutMs <= 0) {
            timeoutMs = this.timeoutMs;
        }
        final RpcRequest request = newRequest(session, destAddr, methodSpec, RpcInvokeType.SYNC_CALL);
        final IPromise<RpcResult> promise = new Promise<>(); // 允许阻塞

        final long requestId = request.getRequestId(); // 提前保留requestId
        rpcSupport.addWatcher(session.sessionId, requestId, promise); // 先添加watcher再发送
        rpcSupport.sendRequest(request); // send以后不可再访问request，可能已被回收
        try {
            RpcResult result = promise.get(timeoutMs, TimeUnit.MILLISECONDS);
            if (result.isSucceeded()) {
                @SuppressWarnings("unchecked") V castV = (V) result.getData();
                return castV;
            }
            throw RpcServerException.newServerException(result.getErrorCode(), result.getErrorMsg());
        } catch (ExecutionException ex) {
            throw RpcClientException.unknownException(ex);
        } finally {
            rpcSupport.removeWatcher(session.sessionId, requestId);
        }
    }

    // endregion

    // region sendResponse

    @Override
    public void sendResult(long sessionId, WorkerAddr destAddr,
                           long requestId, int serviceId, int methodId,
                           Object result, boolean sharable) {
        RpcResponse response = newResponse(sessionId, destAddr, requestId, serviceId, methodId,
                result, sharable);
        rpcSupport.sendResponse(response);
    }

    @Override
    public void sendError(long sessionId, WorkerAddr destAddr,
                          long requestId, int serviceId, int methodId,
                          int errorCode, String msg) {
        RpcResponse response = newResponse(sessionId, destAddr, requestId, serviceId, methodId,
                errorCode, msg);
        rpcSupport.sendResponse(response);
    }

    @Override
    public void sendError(long sessionId, WorkerAddr destAddr,
                          long requestId, int serviceId, int methodId,
                          Throwable ex) {
        RpcResult result = toErrorResult(ex);
        RpcResponse response = newResponse(sessionId, destAddr, requestId, serviceId, methodId,
                result.getErrorCode(), result.getErrorMsg());
        rpcSupport.sendResponse(response);
    }

    @Override
    public <V> void sendAsyncResult(long sessionId, WorkerAddr destAddr,
                                    long requestId, int serviceId, int methodId,
                                    IFuture<V> future, boolean sharable) {
        future.onCompletedAsync(worker,
                new RpcFutureListener<>(this, sessionId, destAddr, requestId, serviceId, methodId, sharable),
                TaskOptions.STAGE_TRY_INLINE);
    }

    @Override
    public <V> void sendAsyncResult(long sessionId, WorkerAddr destAddr,
                                    long requestId, int serviceId, int methodId,
                                    CompletableFuture<V> future, boolean sharable) {
        // 暂时认为就在Worker线程吧
        future.whenComplete(new RpcFutureListener<>(this, sessionId, destAddr,
                requestId, serviceId, methodId, sharable));
    }

    // endregion

    // region 生命周期

    @Override
    public void start() {
        // 接收Node派发到Worker的请求和响应，再转换给RpcSupport
        worker.subscribe(FxUtils.TYPE_NODE_WORKER_REQUEST, this);
        worker.subscribe(FxUtils.TYPE_NODE_WORKER_RESPONSE, this);
    }

    @Override
    public void onEvent(long sequence, WorkerEvent event) {
        switch (event.getType()) {
            case FxUtils.TYPE_NODE_WORKER_REQUEST -> onRcvRequestStep3((RpcRequest) event.obj1);
            case FxUtils.TYPE_NODE_WORKER_RESPONSE -> onRcvResponseStep3((RpcResponse) event.obj1);
            default -> throw new AssertionError();
        }
    }

    @Override
    public void update() {
        final long curTime = timeModule.getTime();
        RpcRequestStub stub;
        while ((stub = stubQueue.peek()) != null) {
            if (curTime < stub.deadline) {
                return;
            }
            stubQueue.poll();
            // 从关联Session中删除
            S2SSession session = getSession(stub.sessionId);
            if (session != null) {
                session.stubMap.remove(stub.requestId);
            }
            logger.info("rpc timeout, destAddr {}, requestId {}, serviceId {}, methodId {}",
                    stub.destAddr, stub.requestId, stub.serviceId, stub.methodId);

            stub.promise.trySetException(RpcClientException.timeout(stub.destAddr));
            stubPool.release(stub);
        }
    }

    @Override
    public void stop() {
        localSession.stubMap.clear();
        for (S2SSession session : sessionMgr.getSessionMap().values()) {
            session.stubMap.clear();
        }
        stubQueue.clear();
        stubPool.clear();
    }

    // endregion

    // region rcvRequest

    /** 当前在worker线程 */
    @SuppressWarnings("unchecked")
    <T> void onRcvRequestStep3(RpcRequest request) {
        RpcMethodProxy<T> proxy = (RpcMethodProxy<T>) proxyRegistry.getProxy(request.getServiceId(), request.getMethodId());
        if (proxy == null) {
            reject(request, RpcErrorCodes.SERVER_UNSUPPORTED_INTERFACE);
            return;
        }
        // 拦截测试
        int code = sessionMgr.test(request);
        if (code != 0) {
            reject(request, code);
            return;
        }
        // Request加入池化逻辑后，我们将关键数据拷贝到Context上，使得Request可立即归还
        RpcContextImpl<T> context = new RpcContextImpl<>(this,
                request.getSessionId(), request.getSrcAddr(),
                request.getRequestId(),
                request.getServiceId(), request.getMethodId(),
                request.getInvokeType());
        try {
            proxy.invoke(context, request.getData());
        } catch (Throwable ex) {
            logInvokeException(request, ex);
            // 其实还可以感知一下context是否发送了结果
            if (RpcInvokeType.isCall(request.getInvokeType())) {
                sendError(request.getSessionId(), request.getSrcAddr(), // srcAddr
                        request.getRequestId(), request.getServiceId(), request.getMethodId(),
                        ex);
            }
        }
        RpcRequest.release(request); // 回收
    }

    /** 拒绝客户端请求 -- node和worker的拒绝有差异，地址不同 */
    private void reject(RpcRequest request, int code) {
        logger.warn("reject the request, reason {}, sessionId {}, srcAddr {}, requestId {}, serviceId {}, methodId {}",
                code,
                request.getSessionId(), request.getSrcAddr(),
                request.getRequestId(), request.getServiceId(), request.getMethodId());
        if (RpcInvokeType.isCall(request.getInvokeType())) {
            sendError(request.getSessionId(), request.getSrcAddr(), // srcAddr
                    request.getRequestId(), request.getServiceId(), request.getMethodId(),
                    code, null);
        }
        RpcRequest.release(request);
    }

    /** 记录执行异常 */
    private static void logInvokeException(RpcRequest request, Throwable ex) {
        if (!(ex instanceof NoLogRequiredException)) {
            logger.warn("invoke caught exception, sessionId {}, srcAddr {}, requestId {}, serviceId {}, methodId {}",
                    request.getSessionId(), request.getSrcAddr(),
                    request.getRequestId(), request.getServiceId(), request.getMethodId(),
                    ex);
        }
    }

    // endregion

    // region rcvResponse

    /** 当前在worker线程 */
    void onRcvResponseStep3(RpcResponse response) {
        S2SSession session = getSession(response.getSessionId());
        if (session == null) {
            logResponseTimeout(response);
            RpcResponse.release(response);
            return;
        }
        RpcRequestStub stub = session.stubMap.remove(response.getRequestId());
        if (stub == null) {
            logResponseTimeout(response);
            RpcResponse.release(response);
            return;
        }
        stubQueue.remove(stub);

        @SuppressWarnings("unchecked") IPromise<Object> promise = (IPromise<Object>) stub.promise;
        if (response.isSucceeded()) {
            promise.trySetResult(response.getData());
        } else {
            promise.trySetException(RpcServerException.newServerException(response.getErrorCode(), response.getErrorMsg()));
        }
        stubPool.release(stub); // 回收
        RpcResponse.release(response);
    }

    private static void logResponseTimeout(RpcResponse response) {
        logger.info("rcv rpc response, but request is timeout, sessionId {}, srcAddr {}, requestId {}",
                response.getSessionId(), response.getSrcAddr(), response.getRequestId());
    }

    // endregion

    // region factory

    private <V> RpcRequestStub newStub(RpcRequest request, IPromise<V> promise, long deadline) {
        final RpcRequestStub stub = stubPool.acquire();
        stub.promise = promise;
        stub.deadline = deadline;

        stub.sessionId = request.getSessionId();
        stub.destAddr = request.getDestAddr();
        stub.requestId = request.getRequestId();
        stub.serviceId = request.getServiceId();
        stub.methodId = request.getMethodId();
        return stub;
    }

    private RpcRequest newRequest(S2SSession session, WorkerAddr destAddr, RpcMethodSpec<?> methodSpec, int invokeType) {
        RpcRequest request = RpcRequest.acquire();
        request.setSessionId(session.sessionId);
        request.setSrcAddr(selfAddr);
        request.setDestAddr(destAddr);

        // 本地session使用全局的序号分配器，sessionId + requestId才具有唯一性
        if (session.sessionId == 0) {
            request.setRequestId(rpcSupport.nextRequestId());
        } else {
            request.setRequestId(session.nextRequestId());
        }
        request.setInvokeType(invokeType);
        request.setCreateTime(timeModule.getTime());
        request.setServiceId(methodSpec.getServiceId());
        request.setMethodId(methodSpec.getMethodId());
        request.setData(methodSpec.getParameter());
        request.setSharable(methodSpec.isSharable());

        // 数据可共享的情况下：进程内不序列化；如果需要发送到网络，则延迟到Node序列化
        if (!(enableLocalSharing && request.isSharable()) && request.getData() != null) {
            rpcSupport.encodeParameter(request);
        }
        return request;
    }

    /** 任意线程调用 */
    private RpcResponse newResponse(long sessionId, WorkerAddr destAddr,
                                    long requestId, int serviceId, int methodId,
                                    Object result, boolean sharable) {
        RpcResponse response = RpcResponse.acquire();
        response.setSessionId(sessionId);
        response.setSrcAddr(selfAddr);
        response.setDestAddr(destAddr);

        response.setRequestId(requestId);
        response.setServiceId(serviceId);
        response.setMethodId(methodId);
        response.setSharable(sharable);
        response.setSuccess(result);

        // 数据可共享的情况下：进程内不序列化；如果需要发送到网络，则延迟到Node序列化
        if (!(enableLocalSharing && response.isSharable()) && response.getData() != null) {
            rpcSupport.encodeResult(response);
        }
        return response;
    }

    /** 任意线程调用 */
    private RpcResponse newResponse(long sessionId, WorkerAddr destAddr,
                                    long requestId, int serviceId, int methodId,
                                    int errorCode, String msg) {
        RpcResponse response = RpcResponse.acquire();
        response.setSessionId(sessionId);
        response.setSrcAddr(selfAddr);
        response.setDestAddr(destAddr);

        response.setRequestId(requestId);
        response.setServiceId(serviceId);
        response.setMethodId(methodId);
        response.setFailed(errorCode, msg);
        return response;
    }

    /** 解析异常信息为错误码信息 */
    private static RpcResult toErrorResult(Throwable ex) {
        Objects.requireNonNull(ex, "ex");
        // future对异常进行了封装
        ex = ExecutorUtils.unwrapCompletionException(ex);
        if (ex instanceof ErrorCodeException codeException) {
            return new RpcResult(codeException.getErrorCode(), codeException.getMessage());
        }
        if (ex instanceof RpcException rpcException) {
            return new RpcResult(rpcException.getErrorCode(), rpcException.getMessage());
        }
        return new RpcResult(RpcErrorCodes.SERVER_UNKNOWN_EXCEPTION, ExceptionUtils.getMessage(ex));
    }

    // endregion
}
