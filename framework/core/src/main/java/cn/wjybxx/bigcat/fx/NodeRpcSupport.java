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

package cn.wjybxx.bigcat.fx;

import cn.wjybxx.common.ThreadUtils;
import cn.wjybxx.common.concurrent.ICompletableFuture;
import cn.wjybxx.common.concurrent.NoLogRequiredException;
import cn.wjybxx.common.concurrent.WatcherMgr;
import cn.wjybxx.common.concurrent.XCompletableFuture;
import cn.wjybxx.common.log.DebugLogLevel;
import cn.wjybxx.common.log.DebugLogUtils;
import cn.wjybxx.common.pb.PBMethodInfo;
import cn.wjybxx.common.pb.PBMethodInfoRegistry;
import cn.wjybxx.common.rpc.*;
import cn.wjybxx.common.time.TimeProvider;
import com.google.protobuf.InvalidProtocolBufferException;
import com.google.protobuf.Message;
import it.unimi.dsi.fastutil.longs.Long2ObjectLinkedOpenHashMap;
import org.apache.commons.lang3.ArrayUtils;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import javax.annotation.Nonnull;
import javax.annotation.concurrent.ThreadSafe;
import java.nio.charset.StandardCharsets;
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
    /** 是否允许本地调用共享对象 */
    private boolean enableLocalShare = true;
    /** 日志配置 */
    private RpcLogConfig logConfig = RpcLogConfig.NONE;
    /** 当前是否可修改配置数据 -- 也可看做是否已启动标记 */
    private volatile boolean mutable = true;

    /**
     * 为request分配id
     * 1.非线程安全，只在node线程访问。
     * 2.这样可保证node发送出去的请求id是有序的。
     */
    private long idSequencer = 0;
    /** 保持插入序很重要 */
    private final Long2ObjectLinkedOpenHashMap<RpcRequestStubImpl> requestStubMap = new Long2ObjectLinkedOpenHashMap<>(100);
    /** 用于支持同步调用 */
    private final Map<Long, WatcherMgr.Watcher<RpcResponse>> watcherMap = new ConcurrentHashMap<>(8);

    private Node node;
    private WorkerAddr selfAddr;
    private RpcSerializer serializer;
    private PBMethodInfoRegistry methodInfoRegistry;
    private NodeRpcSender sender;
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
        this.timeoutMs = timeoutMs;
        return this;
    }

    public boolean isEnableLocalShare() {
        return enableLocalShare;
    }

    public NodeRpcSupport setEnableLocalShare(boolean enableLocalShare) {
        ensureMutable();
        this.enableLocalShare = enableLocalShare;
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
        this.sender = node.injector().getInstance(NodeRpcSender.class);
    }

    @Override
    public void start() {
        if (conId == 0) {
            conId = ThreadLocalRandom.current().nextLong();
        }
        mutable = false;
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
        if (!sender.send(request)) {
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
                    .thenCompose(e -> e) // 这里不能调用composeAsync，composeAsync如果返回的future尚未完成，则会在触发其完成的线程触发业务回调
                    .whenCompleteAsync((v, t) -> {
                    }, worker); // 需要回到worker线程
        } else {
            return w2n_call(worker, request);
        }
    }

    private <V> ICompletableFuture<V> w2n_call(Worker worker, RpcRequest request) {
        fillRequest(request);
        if (logConfig.getSndRequestLogLevel() > DebugLogLevel.NONE) {
            logSndRequest(request);
        }
        if (!isUnicastAddr(request.getDestAddr())) {
            logger.info("rpc multicast call, target " + request.getDestAddr());
        }
        if (!sender.send(request)) {
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
            decodeResult(response);
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
            // 即时删除watcher -- 注意！当前线程可能看不见Node线程分配的requestId，而在node线程下一定可见
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
        if (!isUnicastAddr(request.getDestAddr())) {
            logger.info("rpc multicast syncCall, target " + request.getDestAddr());
        }
        if (!sender.send(request)) {
            logger.info("rpc send failure, target " + request.getDestAddr());

            RpcResponse response = newFailedResponse(request, RpcErrorCodes.LOCAL_ROUTER_EXCEPTION, "Failed to send request");
            watcher.future.complete(response);
        }
        return watcher.future;
    }

    // endregion

    // region rcvRequest

    /** 该方法由IO线程调用(由RpcRouter类调用) */
    public void onRcvRequest(final RpcRequest request) {
        Objects.requireNonNull(request);
        if (!node.inEventLoop()) {
            node.execute(() -> onRcvRequest(request));
            return;
        }
        // 在使用之前需要先反序列化
        if (!decodeParameters(request)) {
            logger.warn("rcv bad request, info: " + request.toSimpleLog());
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
        if (isBroadcastWorkerAddr(request.getDestAddr()) && workerList.size() > 1) {
            // 广播 -- 需要克隆，这里不做额外的优化（可减少一次克隆），因为广播协议整体还是偏少
            encodeParameters(request);
            for (int i = 0; i < workerList.size(); i++) {
                Worker worker = workerList.get(i);
                RpcRequest clonedRequest = clone(request);
                decodeParameters(clonedRequest);
                if (worker != node) { // 只有node.inEventLoop为true
                    worker.execute(() -> onRcvRequestImpl(worker, request));
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
                final RpcResponse response = newFailedResponse(request, code, "");
                sendResponseAndLog(response);
            }
            return;
        }
        // 执行调用
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
                    @SuppressWarnings("unchecked") T castReult = (T) result;
                    context.sendResult(castReult);
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
            final RpcResponse response = newFailedResponse(request, RpcErrorCodes.SERVER_UNSUPPORTED_INTERFACE, "");
            sendResponseAndLog(response);
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

    /** 该方法由IO线程调用(由RpcRouter类调用) */
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
        // 在使用之前需要先反序列化
        decodeResult(response);

        final RpcRequestStubImpl requestStub = requestStubMap.remove(response.getRequestId());
        if (logConfig.getRcvResponseLogLevel() > DebugLogLevel.NONE) {
            logRcvResponse(response, requestStub == null);
        }

        if (requestStub != null) {
            // future的跨线程问题是在call的时候处理的
            @SuppressWarnings("unchecked") CompletableFuture<Object> future = (CompletableFuture<Object>) requestStub.future;
            final int errorCode = response.getErrorCode();
            if (errorCode == 0) {
                future.complete(response.getResult());
            } else {
                future.completeExceptionally(RpcServerException.newServerException(response));
            }
        }
    }

    private void sendResponseAndLog(final RpcResponse response) {
        // 不可共享的数据在主线程序列化
        if (!response.isSharable()) {
            encodeResult(response);
        }
        if (!node.inEventLoop()) {
            node.execute(() -> sendResponseAndLog(response));
            return;
        }
        // 检测可共享的数据是否需要序列化
        if (response.isSharable()) {
            if (enableLocalShare && isLocalUnicastAddr(response.getDestAddr())) {
                response.setSerialized();
                response.setDeserialized();
            } else {
                encodeResult(response);
            }
        }
        if (logConfig.getSndResponseLogLevel() > DebugLogLevel.NONE) {
            logSndResponse(response);
        }
        if (!sender.send(response)) {
            logger.warn("rpc send response failure, dest {}", response.getDestAddr());
        }
    }

    private RpcResponse newFailedResponse(RpcRequest request, int errorCode, String msg) {
        RpcResponse response = new RpcResponse(request, selfAddr, errorCode, List.of(msg));
        response.setSharable(true);
        return response;
    }

    // endregion

    // region request

    /** worker线程调用 -- worker可能是node自身 */
    private RpcRequest newRequest(RpcAddr target, RpcMethodSpec<?> methodSpec, int invokeType) {
//        assert !mutable;
        RpcRequest request = new RpcRequest(conId, selfAddr, target, 0, invokeType, methodSpec);
        // 参数不可共享时在请求线程序列化 -- 不论是否本地
        if (!request.isSharable()) {
            encodeParameters(request);
        }
        return request;
    }

    /** node线程调用，分配请求id和检查序列化 */
    private void fillRequest(RpcRequest request) {
//        assert node.inEventLoop();
        request.setRequestId(++idSequencer);
        // 检测可共享的数据是否需要（延迟）序列化 -- 不可共享的数据已序列化
        if (request.isSharable()) {
            if (enableLocalShare && isLocalUnicastAddr(request.getDestAddr())) {
                request.setSerialized();
                request.setDeserialized();
            } else {
                encodeParameters(request);
            }
        }
    }

    /** 判断是否是单播地址 */
    private boolean isUnicastAddr(RpcAddr addr) {
        if (addr instanceof WorkerAddr workerAddr) {
            return sender.isUnicastAddr(workerAddr);
        }
        return addr == StaticRpcAddr.LOCAL;
    }

    /** 判断是否是本地单播地址 */
    private boolean isLocalUnicastAddr(RpcAddr addr) {
        if (addr instanceof WorkerAddr workerAddr) {
            return selfAddr.equalsIgnoreWorker(workerAddr)
                    && sender.isUnicastWorkerAddr(workerAddr);
        }
        return addr == StaticRpcAddr.LOCAL;
    }

    /** 判断是否是本地广播地址 */
    private boolean isBroadcastWorkerAddr(RpcAddr addr) {
        if (addr instanceof WorkerAddr workerAddr) {
            return sender.isBroadcastWorkerAddr(workerAddr);
        }
        return addr == StaticRpcAddr.LOCAL_BROADCAST;
    }

    /** 克隆rpc请求 -- 参数应尚未解码 */
    private RpcRequest clone(RpcRequest src) {
        // 控制标识(ctl)不会被拷贝
        return new RpcRequest(src.getConId(), src.getSrcAddr(), src.getDestAddr())
                .setRequestId(src.getRequestId())
                .setInvokeType(src.getInvokeType())
                .setServiceId(src.getServiceId())
                .setMethodId(src.getMethodId())
                .setParameters(src.getParameters());
    }
    // endregion

    // region 编解码

    /** 序列化rpc参数 */
    private void encodeParameters(RpcRequest request) {
        if (request.isSerialized()) {
            return;
        }
        request.setSerialized();

        List<Object> parameters = request.listParameters();
        if (sender.isCrossLanguageAddr(request.getDestAddr())) {
            if (parameters.size() == 0) {
                request.setParameters(ArrayUtils.EMPTY_BYTE_ARRAY);
                return;
            }
            Message message = (Message) parameters.get(0);
            if (message == null) { // null参数警告
                logger.info("rpc argument is null, it will be replaced with an empty message, serviceId: {}, {}",
                        request.getServiceId(), request.getMethodId());
                request.setParameters(ArrayUtils.EMPTY_BYTE_ARRAY);
            } else {
                request.setParameters(message.toByteArray());
            }
        } else {
            request.setParameters(serializer.write(parameters));
        }
    }

    /** 反序列化rpc参数 */
    private boolean decodeParameters(RpcRequest request) {
        if (request.isDeserialized()) {
            return true;
        }
        request.setDeserialized();

        byte[] parameters = request.bytesParameters();
        if (sender.isCrossLanguageAddr(request.getSrcAddr())) {
            PBMethodInfo<?, ?> methodInfo = methodInfoRegistry.getParser(request.getServiceId(), request.getMethodId());
            if (methodInfo == null) {
                return false;
            }
            if (methodInfo.argParser == null) {
                // 无参
                request.setParameters(List.of());
                return true;
            }
            try {
                // 单参-(空数组将被解析为空消息)
                Object argument = methodInfo.argParser.parseFrom(parameters);
                request.setParameters(List.of(argument));
                return true;
            } catch (Exception e) {
                logger.info("decode parameters caught exception", e);
                return false; // 外部会打印方法信息
            }
        } else {
            try {
                request.setParameters(serializer.read(parameters));
                return true;
            } catch (Exception e) {
                logger.info("decode parameters caught exception", e);
                return false;
            }
        }
    }

    private void encodeResult(RpcResponse response) {
        if (response.isSerialized()) { // 已序列化，含bytes和null的特殊处理
            return;
        }
        response.setSerialized();

        if (!RpcErrorCodes.isSuccess(response.getErrorCode())) { // 失败结果
            response.setResult(response.getErrorMsg().getBytes(StandardCharsets.UTF_8));
            return;
        }
        if (sender.isCrossLanguageAddr(response.getDestAddr())) {
            if (response.getErrorCode() == RpcErrorCodes.RESULT_NULL) {
                // null结果警告
                if (methodInfoRegistry.getParser(response.getServiceId(), response.getMethodId()) != null) {
                    logger.info("rpc result is null, it will be replaced with an empty message, serviceId: {}, {}",
                            response.getServiceId(), response.getMethodId());
                }
                response.setResult(ArrayUtils.EMPTY_BYTE_ARRAY); // 替换为空数组，然后替换为普通值
                response.setErrorCode(RpcErrorCodes.SUCCESS);
            } else if (response.getErrorCode() == RpcErrorCodes.RESULT_BYTES) {
                // 允许发送bytes以减少拷贝，但打印警告
                logger.info("rpc result is bytes, serviceId: {}, {}",
                        response.getServiceId(), response.getMethodId());

                byte[] rawBytes = (byte[]) response.getResult();
                response.setResult(rawBytes.clone()); // 需要拷贝下来，然后替换为普通值
                response.setErrorCode(RpcErrorCodes.SUCCESS);
            } else {
                Message message = (Message) response.getResult();
                response.setResult(message.toByteArray());
            }
        } else {
            if (response.getErrorCode() == RpcErrorCodes.RESULT_NULL) {
                response.setResult(ArrayUtils.EMPTY_BYTE_ARRAY);
            } else if (response.getErrorCode() == RpcErrorCodes.RESULT_BYTES) {
                byte[] rawBytes = (byte[]) response.getResult();
                response.setResult(rawBytes.clone()); // 需要拷贝下来，保持为bytes
            } else {
                response.setResult(serializer.write(response.getResult()));
            }
        }
    }

    private boolean decodeResult(RpcResponse response) {
        if (response.isDeserialized()) {
            return true;
        }
        response.setDeserialized();

        if (!RpcErrorCodes.isSuccess(response.getErrorCode())) { // 失败结果
            String msg = new String((byte[]) response.getResult(), StandardCharsets.UTF_8);
            response.setResult(msg);
            return true;
        }
        byte[] result = (byte[]) response.getResult();
        if (sender.isCrossLanguageAddr(response.getSrcAddr())) {
            PBMethodInfo<?, ?> methodInfo = methodInfoRegistry.getParser(response.getServiceId(), response.getMethodId());
            if (methodInfo == null) {
                logger.info("receive result, but methodInfo is null, serviceId: {}, methodId: {}",
                        response.getServiceId(), response.getMethodId());
                return false;
            }
            if (methodInfo.resultParser == null) {
                // 无结果
                response.setResult(null);
                return true;
            }
            try {
                // 单结果 -(空数组将被解析为空消息)
                response.setResult(methodInfo.resultParser.parseFrom(result));
                return true;
            } catch (InvalidProtocolBufferException e) {
                logger.info("decode result caught exception", e);
                return false; // 外部会打印方法信息
            }
        } else {
            if (response.getErrorCode() == RpcErrorCodes.RESULT_NULL) {
                response.setErrorCode(0);
                response.setResult(null);
                return true;
            }
            if (response.getErrorCode() == RpcErrorCodes.RESULT_BYTES) {
                response.setErrorCode(0);
                return true;
            }
            try {
                response.setResult(serializer.read(result));
                return true;
            } catch (Exception e) {
                logger.info("decode result caught exception", e);
                return false;
            }
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
            rpcClient.sendResponseAndLog(response);
        }

        @Override
        public void sendError(int errorCode, String msg) {
            if (!RpcErrorCodes.isUserCode(errorCode)) {
                throw new IllegalArgumentException("invalid errorCode: " + errorCode);
            }
            RpcResponse response = new RpcResponse(request, rpcClient.selfAddr);
            response.setFailed(errorCode, msg);
            rpcClient.sendResponseAndLog(response);
        }

        @Override
        public void sendError(Throwable ex) {
            Objects.requireNonNull(ex);
            RpcResponse response = new RpcResponse(request, rpcClient.selfAddr);
            response.setFailed(ex);
            rpcClient.sendResponseAndLog(response);
        }

        @Override
        public void sendEncodedResult(byte[] result) {
            Objects.requireNonNull(result);
            RpcResponse response = new RpcResponse(request, rpcClient.selfAddr);
            response.setSuccess(result);
            // 非基于protobuf通信时，原始类型不能是bytes
            if (!rpcClient.sender.isCrossLanguageAddr(response.getDestAddr())) {
                response.setErrorCode(0);
            }
            rpcClient.sendResponseAndLog(response);
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