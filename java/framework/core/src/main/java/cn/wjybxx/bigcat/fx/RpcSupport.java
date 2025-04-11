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

import cn.wjybxx.base.ObjectUtils;
import cn.wjybxx.bigcat.pb.ProtobufUtils;
import cn.wjybxx.concurrent.EventLoopModule;
import cn.wjybxx.concurrent.IAgentEventHandler;
import cn.wjybxx.concurrent.IPromise;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.List;
import java.util.Objects;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.ConcurrentMap;
import java.util.concurrent.atomic.AtomicLong;

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
public final class RpcSupport extends EventLoopModule implements IAgentEventHandler<WorkerEvent> {

    private static final Logger logger = LoggerFactory.getLogger(RpcSupport.class);

    /** 是否启用日志 -- 允许运行时调整 */
    private volatile boolean enableLog;
    /** 用于Worker间Rpc调用时分配RequestId */
    private final AtomicLong sequencer = new AtomicLong();
    /** 用于判断请求和响应向哪里派发 -- session不存在时按服务查找Worker */
    private final ConcurrentMap<Long, Worker> session2WorkerMap = new ConcurrentHashMap<>(8);
    /** 用于支持同步调用 -- finally快中删除 */
    private final ConcurrentMap<Key, IPromise<RpcResult>> watcherMap = new ConcurrentHashMap<>(8);

    private Node node;
    private WorkerAddr nodeAddr; // 不包含workerId
    private RpcSerializer serializer;
    private RpcMethodRegistry methodRegistry;
    private RpcRouter router;

    // region 设置

    public boolean isEnableLog() {
        return enableLog;
    }

    public void setEnableLog(boolean enableLog) {
        this.enableLog = enableLog; // log允许运行时调整
    }

    // endregion

    // region 生命周期

    @Override
    public void resolveDependence() {
        this.node = (Node) getEntity();
        this.nodeAddr = node.nodeAddr();

        this.serializer = node.injector().getInstance(RpcSerializer.class);
        this.methodRegistry = node.injector().getInstance(RpcMethodRegistry.class);
        this.router = node.injector().getInstance(RpcRouter.class);
    }

    @Override
    public void start() {
        // net到node的请求和响应
        node.subscribe(FxUtils.TYPE_NET_NODE_REQUEST, this);
        node.subscribe(FxUtils.TYPE_NET_NODE_RESPONSE, this);
        // worker到node的请求和响应
        node.subscribe(FxUtils.TYPE_WORKER_NODE_REQUEST, this);
        node.subscribe(FxUtils.TYPE_WORKER_NODE_RESPONSE, this);
    }

    @Override
    public void onEvent(long sequence, WorkerEvent event) {
        switch (event.getType()) {
            case FxUtils.TYPE_NET_NODE_REQUEST -> onRcvRequestStep2((RpcRequest) event.obj1);
            case FxUtils.TYPE_NET_NODE_RESPONSE -> onRcvResponseStep2((RpcResponse) event.obj1);
            case FxUtils.TYPE_WORKER_NODE_REQUEST ->
                    sendRequestStep2((RpcRequest) event.obj1, (IPromise<?>) event.obj2);
            case FxUtils.TYPE_WORKER_NODE_RESPONSE -> sendResponseStep2((RpcResponse) event.obj1);
            default -> throw new AssertionError();
        }
    }

    @Override
    public void stop() {
        session2WorkerMap.clear();
        watcherMap.clear();
    }

    // endregion

    // region support

    /** 分配下一个请求id */
    public long nextRequestId() {
        return sequencer.incrementAndGet();
    }

    /** 注册session */
    public void addSession(long sessionId, Worker worker) {
        Worker exist = session2WorkerMap.putIfAbsent(sessionId, worker);
        if (exist != null) {
            throw new IllegalArgumentException("sessionId: " + sessionId);
        }
    }

    /** 删除session */
    public void removeSession(long sessionId) {
        session2WorkerMap.remove(sessionId);
    }

    /** 添加watcher */
    public void addWatcher(long sessionId, long requestId, IPromise<RpcResult> promise) {
        Objects.requireNonNull(promise);
        Key key = new Key(sessionId, requestId);
        IPromise<RpcResult> exist = watcherMap.putIfAbsent(key, promise);
        if (exist != null) {
            throw new IllegalArgumentException("sessionId: %d, requestId: %d".formatted(sessionId, requestId));
        }
    }

    /** 删除watcher */
    public void removeWatcher(long sessionId, long requestId) {
        Key key = new Key(sessionId, requestId);
        watcherMap.remove(key);
    }

    // endregion

    // region sendRequest

    /** worker请求发送rpc请求 */
    public void sendRequest(RpcRequest request, IPromise<?> promise) {
        if (node.inEventLoop()) {
            sendRequestStep2(request, promise);
        } else {
            long seq = node.nextSequence();
            if (seq < 0) return; // shutdown
            WorkerEvent event = node.getEvent(seq);
            event.setType(FxUtils.TYPE_WORKER_NODE_REQUEST);
            event.obj1 = request;
            event.obj2 = promise;
            node.publish(seq);
        }
    }

    /** 当前在node线程 */
    private void sendRequestStep2(RpcRequest request, IPromise<?> promise) {
        if (enableLog) {
            logger.info("snd rpc request {}", request);
        }
        if (request.getDestAddr().nodeId == nodeAddr.nodeId) {
            // 进程内包 -- 数据已序列化，或是可共享的；直接触发接收
            onRcvRequest(request);
        } else {
            // 网络包 -- router负责回收
            router.send(request);
        }
    }
    // endregion

    // region sendResponse

    /** node或worker线程调用 */
    public void sendResponse(RpcResponse response) {
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

    /** 当前在node线程 */
    private void sendResponseStep2(RpcResponse response) {
        if (enableLog) {
            logger.info("snd rpc response {}", response);
        }
        if (response.getDestAddr().nodeId == nodeAddr.nodeId) {
            // 进程内包 -- 数据已序列化，或是可共享的；直接触发接收
            onRcvResponse(response);
        } else {
            // 网络包 -- router负责回收
            router.send(response);
        }
    }

    // endregion

    // region rcvRequest

    /**
     * 通知Support模块收到一个Rpc请求
     * 1.该方法由IO线程调用 -- 即RpcRouter类调用。
     * 2.如果外部未反序列化请求参数，则在Node线程自动反序列化。
     * 3.如果request可能发给多个Node，应在外部拷贝 -- {@link #deepCopy(RpcRequest, int)}
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
    private void onRcvRequestStep2(final RpcRequest request) {
        // 在使用之前需要先反序列化
        if (request.isBytes() && !decodeParameter(request)) {
            reject(request, RpcErrorCodes.SERVER_DESERIALIZE_FAILED);
            return;
        }
        // 判断是否对外提供服务
        ServiceInfo serviceInfo = node.serviceInfoMap().get(request.getServiceId());
        if (serviceInfo == null || serviceInfo.workerList.isEmpty()) {
            reject(request, RpcErrorCodes.SERVER_UNSUPPORTED_INTERFACE);
            return;
        }
        // 优先按照SessionId查找，其次按workerId查找，最后随机分配
        Worker worker = session2WorkerMap.get(request.getSessionId());
        if (worker != null) {
            onRcvRequestStep2(worker, request);
            return;
        }
        String destWorkerId = request.getDestAddr().workerId;
        List<Worker> workerList = serviceInfo.workerList;
        if (!ObjectUtils.isBlank(destWorkerId)) {
            if (destWorkerId.equals("*")) {
                // 广播 -- 顺序不应该产生影响
                List<RpcRequest> clonedRequests = deepCopy(request, workerList.size());
                for (int idx = 0; idx < workerList.size(); idx++) {
                    worker = workerList.get(idx);
                    RpcRequest clonedRequest = clonedRequests.get(idx);
                    onRcvRequestStep2(worker, clonedRequest);
                }
            } else {
                // 指定worker -- 理论上还可进行复杂的模式匹配，以后再说
                worker = findWorker(workerList, destWorkerId);
                if (worker == null) {
                    reject(request, RpcErrorCodes.SERVER_WORKER_NOT_EXIST);
                    return;
                }
                onRcvRequestStep2(worker, request);
            }
        } else {
            // 随机分配 -- 多worker时根据源地址nodeId保证路由的一致性
            if (workerList.size() == 1) {
                worker = workerList.get(0);
            } else {
                int idx = Math.abs(request.getSrcAddr().nodeId) % workerList.size();
                worker = workerList.get(idx);
            }
            onRcvRequestStep2(worker, request);
        }
    }

    private Worker findWorker(List<Worker> workerList, String workerId) {
        for (int idx = 0; idx < workerList.size(); idx++) {
            Worker worker = workerList.get(idx);
            if (workerId.equals(worker.workerAddr().workerId)) {
                return worker;
            }
        }
        return null;
    }

    private void onRcvRequestStep2(Worker worker, RpcRequest request) {
        if (worker == node) {
            worker.controlData().rpcClient.onRcvRequestStep3(request);
        } else {
            long seq = worker.nextSequence();
            if (seq < 0) return; // shutdown
            WorkerEvent event = worker.getEvent(seq);
            event.setType(FxUtils.TYPE_NODE_WORKER_REQUEST);
            event.obj1 = request;
            worker.publish(seq);
        }
    }

    /** 拒绝客户端请求 -- node节点的拒绝和worker拒绝逻辑有差异，地址不同 */
    private void reject(RpcRequest request, int code) {
        logger.warn("reject the request, reason {}, sessionId {}, srcAddr {}, serviceId {}, methodId {}",
                code,
                request.getSessionId(), request.getSrcAddr(),
                request.getServiceId(), request.getMethodId());
        if (RpcInvokeType.isCall(request.getInvokeType())) {
            sendError(request.getSessionId(), request.getSrcAddr(), // srcAddr
                    request.getRequestId(), request.getServiceId(), request.getMethodId(),
                    code, null);
        }
        RpcRequest.release(request);
    }

    private void sendError(long sessionId, WorkerAddr destAddr,
                           long requestId, int serviceId, int methodId,
                           int errorCode, String msg) {
        RpcResponse response = RpcResponse.acquire();
        response.setSessionId(sessionId);
        response.setSrcAddr(nodeAddr); // 进程内调用的话，可识别是被谁拒绝
        response.setDestAddr(destAddr);

        response.setRequestId(requestId);
        response.setServiceId(serviceId);
        response.setMethodId(methodId);

        response.setFailed(errorCode, msg);
        sendResponse(response);
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
        // 不重复打印旧进程的Rpc响应
        if (enableLog) {
            logger.info("rcv rpc response {}", response);
        }
        // watcher需要在IO线程测试
        IPromise<RpcResult> watcher = watcherMap.remove(new Key(response.getSessionId(), response.getRequestId()));
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
        // 使用之前反序列化
        if (response.isBytes() && !decodeResult(response)) {
            response.setFailed(RpcErrorCodes.LOCAL_DESERIALIZE_FAILED, "data error");
        }
        // 优先根据sessionId查询Worker，其次按workerId查找
        Worker worker = session2WorkerMap.get(response.getSessionId());
        if (worker == null && response.getDestAddr().workerId != null) {
            worker = node.findWorker(response.getDestAddr().workerId);
        }
        if (worker == null) {
            logger.info("rcv old process rpc response, remote {}", response.getSrcAddr());
            RpcResponse.release(response);
            return;
        }
        if (worker == node) {
            worker.controlData().rpcClient.onRcvResponseStep3(response);
        } else {
            long seq = worker.nextSequence();
            if (seq < 0) return; // shutdown
            WorkerEvent event = worker.getEvent(seq);
            event.setType(FxUtils.TYPE_NODE_WORKER_RESPONSE);
            event.obj1 = response;
            worker.publish(seq);
        }
    }

    // endregion

    // region 编解码

    /** 序列化rpc参数 */
    public void encodeParameter(RpcRequest request) {
        if (request.getData() == null) { // null不序列化
            return;
        }
        RpcMethodInfo<?, ?> methodInfo = methodRegistry.getMethodInfo(request.getServiceId(), request.getMethodId());
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
    public boolean decodeParameter(RpcRequest request) {
        RpcMethodInfo<?, ?> methodInfo = methodRegistry.getMethodInfo(request.getServiceId(), request.getMethodId());
        byte[] data = (byte[]) request.getData();
        try {
            Object parameter;
            if (methodInfo.parameterType == null) {
                parameter = null; // 对方将void序列化为了空字节数组
            } else if (methodInfo.parameterParser != null) {
                parameter = methodInfo.parameterParser.parseFrom(data);
            } else {
                parameter = serializer.read(data, methodInfo.parameterType);
            }
            request.setData(parameter);
            return true;
        } catch (Exception ex) {
            logger.info("decode parameters caught exception, serviceId {}, methodId {}",
                    request.getServiceId(), request.getMethodId(), ex);
            return false;
        }
    }

    /** 序列化结果 */
    public void encodeResult(RpcResponse response) {
        if (response.getData() == null) { // null不序列化
            return;
        }
        RpcMethodInfo<?, ?> methodInfo = methodRegistry.getMethodInfo(response.getServiceId(), response.getMethodId());
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

    /** 反序列化结果 */
    public boolean decodeResult(RpcResponse response) {
        RpcMethodInfo<?, ?> methodInfo = methodRegistry.getMethodInfo(response.getServiceId(), response.getMethodId());
        byte[] data = (byte[]) response.getData();
        try {
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

    /** 深度拷贝rpc请求 -- src会包含在结果中返回 */
    public List<RpcRequest> deepCopy(RpcRequest src, int count) {
        List<RpcRequest> list = new ArrayList<>(count);
        list.add(src);
        if (count == 1) {
            return list;
        }
        RpcMethodInfo<?, ?> methodInfo = methodRegistry.getMethodInfo(src.getServiceId(), src.getMethodId());
        byte[] bytesParameters = serializer.write(src.getData(), methodInfo.parameterType);
        for (int idx = 1; idx < count; idx++) {
            RpcRequest request = RpcRequest.acquire();
            request.setSessionId(src.getSessionId());
            request.setSrcAddr(src.getSrcAddr());
            request.setDestAddr(src.getDestAddr());

            request.setRequestId(src.getRequestId());
            request.setServiceId(src.getServiceId());
            request.setMethodId(src.getMethodId());
            request.setInvokeType(src.getInvokeType());

            request.setData(bytesParameters);
            decodeParameter(request);
            list.add(request);
        }
        return list;
    }
    // endregion

    private static class Key {

        public final long sessionId;
        public final long requestId;

        public Key(long sessionId, long requestId) {
            this.sessionId = sessionId;
            this.requestId = requestId;
        }

        @Override
        public boolean equals(Object o) {
            if (this == o) return true;
            if (o == null || getClass() != o.getClass()) return false;

            Key key = (Key) o;
            return sessionId == key.sessionId && requestId == key.requestId;
        }

        @Override
        public int hashCode() {
            int result = Long.hashCode(sessionId);
            result = 31 * result + Long.hashCode(requestId);
            return result;
        }

        @Override
        public String toString() {
            return "Key{" +
                    "sessionId=" + sessionId +
                    ", requestId=" + requestId +
                    '}';
        }
    }
}