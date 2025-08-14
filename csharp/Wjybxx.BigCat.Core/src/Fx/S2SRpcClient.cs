#region LICENSE

// Copyright 2025 wjybxx(845740757@qq.com)
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Concurrent;
using Wjybxx.Commons.Ex;
using Wjybxx.Commons.Inject;
using Wjybxx.Commons.Logger;
using Wjybxx.Commons.Pool;

namespace Wjybxx.BigCat.Fx
{
public class S2SRpcClient : EventLoopModule, RpcClient, RpcClientImpl, IAgentEventHandler<WorkerEvent>
{
    private static readonly ILogger logger = LoggerFactory.GetLogger<S2SRpcClient>();
#nullable disable
    private Worker worker;
    private WorkerAddr selfAddr;
    private TimeModule timeModule;
    private RpcProxyRegistry proxyRegistry;
    private S2SSessionMgr sessionMgr;
    private RpcSupport rpcSupport;

    /** rpc默认超时时间 */
    private long timeoutMs = 15 * 1000;
    /** 是否允许本地调用共享对象 */
    private bool enableLocalSharing = true;
    /** 本地Session，用于Node内的线程通信 */
    private S2SSession localSession;
    /** 超时信息 -- 所有Session的集中处理 */
    private readonly IndexedPriorityQueue<RpcRequestStub> stubQueue = new(RpcRequestStub.Comparer);
    /** Stub池 -- 不共享，没必要 */
    private readonly ObjectPool<RpcRequestStub> stubPool = new ObjectPool<RpcRequestStub>(
        () => new RpcRequestStub(), stub => stub.Reset(), 100);

    public override void OnAwake() {
        this.worker = (Worker)Entity;
        this.selfAddr = worker.WorkerAddr;
        this.timeModule = worker.Injector.GetInstance<TimeModule>();
        this.proxyRegistry = worker.Injector.GetInstance<RpcProxyRegistry>();
        this.sessionMgr = worker.Injector.GetInstance<S2SSessionMgr>();
        // Node上的组件
        Node node = worker.Node;
        this.rpcSupport = node.Injector.GetInstance<RpcSupport>();
        // 创建虚拟Session
        this.localSession = new S2SSession(0, selfAddr.nodeId);
    }
#nullable restore

    public long TimeoutMs {
        get => timeoutMs;
        set => timeoutMs = value;
    }
    public bool EnableLocalSharing {
        get => enableLocalSharing;
        set => enableLocalSharing = value;
    }

    /** 注册session */
    public void AddSession(long sessionId) {
        rpcSupport.AddSession(sessionId, worker);
    }

    /** 删除相关session数据 */
    public void RemoveSession(long sessionId) {
        rpcSupport.RemoveSession(sessionId);

        List<RpcRequestStub> list = new List<RpcRequestStub>();
        foreach (RpcRequestStub stub in stubQueue) {
            if (stub.sessionId == sessionId) {
                list.Add(stub);
            }
        }
        foreach (RpcRequestStub stub in list) {
            stub.promise.TrySetException(stub.rid, RpcClientException.SessionClosed(stub.destAddr));
            stubQueue.Remove(stub);
            stubPool.Release(stub);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private S2SSession? GetSession(long sessionId) {
        if (sessionId == 0) return localSession;
        return sessionMgr.GetSession(sessionId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private S2SSession? GetSessionOfNode(int nodeId) {
        return nodeId == selfAddr.nodeId ? localSession : sessionMgr.GetSessionOfNode(nodeId);
    }

    #region sendRequest

    public void Send(WorkerAddr destAddr, RpcMethodSpec methodSpec) {
        Debug.Assert(worker.InEventLoop());
        S2SSession? session = GetSessionOfNode(destAddr.nodeId);
        if (session == null) {
            return;
        }
        RpcRequest request = NewRequest(session, destAddr, in methodSpec, RpcInvokeType.ONEWAY);
        rpcSupport.SendRequest(request);
    }

    public void Send<V>(WorkerAddr destAddr, RpcMethodSpec<V> methodSpec) {
        Debug.Assert(worker.InEventLoop());
        S2SSession? session = GetSessionOfNode(destAddr.nodeId);
        if (session == null) {
            return;
        }
        RpcMethodSpec unwrap = methodSpec.Unwrap();
        RpcRequest request = NewRequest(session, destAddr, in unwrap, RpcInvokeType.ONEWAY);
        rpcSupport.SendRequest(request);
    }

    public ValueFuture<V> Call<V>(WorkerAddr destAddr, RpcMethodSpec<V> methodSpec, long timeoutMs = 0) {
        Debug.Assert(worker.InEventLoop());
        S2SSession? session = GetSessionOfNode(destAddr.nodeId);
        if (session == null) {
            return ValueFuture<V>.FromException(RpcClientException.SessionNotExist(destAddr));
        }
        if (timeoutMs <= 0) {
            timeoutMs = this.timeoutMs;
        }
        RpcMethodSpec unwrap = methodSpec.Unwrap();
        RpcRequest request = NewRequest(session, destAddr, in unwrap, RpcInvokeType.CALL);
        // 注意：我们创建的不是Promise<V>，而是Promise<object>；这是特意加的专项优化，以提高对象池的复用率
        ValuePromise<object> promise = ValuePromise<object>.Acquire(out int rid, worker);
        // 先保留存根再发送
        {
            RpcRequestStub stub = NewStub(request, promise, rid, timeModule.Time + timeoutMs);
            session.stubMap.Add(stub.requestId, stub);
            stubQueue.Add(stub);
        }
        rpcSupport.SendRequest(request); // send以后不可再访问request，可能已被回收
        return ValueFuture<V>.UnsafeCreate(promise, rid);
    }

    public ValueFuture<object> Call(WorkerAddr destAddr, RpcMethodSpec methodSpec, long timeoutMs = 0) {
        Debug.Assert(worker.InEventLoop());
        S2SSession? session = GetSessionOfNode(destAddr.nodeId);
        if (session == null) {
            return ValueFuture<object>.FromException(RpcClientException.SessionNotExist(destAddr));
        }
        if (timeoutMs <= 0) {
            timeoutMs = this.timeoutMs;
        }
        RpcRequest request = NewRequest(session, destAddr, in methodSpec, RpcInvokeType.CALL);
        ValuePromise<object> promise = ValuePromise<object>.Acquire(out int rid, worker);
        // 先保留存根再发送
        {
            RpcRequestStub stub = NewStub(request, promise, rid, timeModule.Time + timeoutMs);
            session.stubMap.Add(stub.requestId, stub);
            stubQueue.Add(stub);
        }
        rpcSupport.SendRequest(request); // send以后不可再访问request，可能已被回收
        return promise.Future;
    }

    public V SyncCall<V>(WorkerAddr destAddr, RpcMethodSpec<V> methodSpec, long timeoutMs = 0) {
        return (V)SyncCall(destAddr, methodSpec.Unwrap(), timeoutMs);
    }

    public object SyncCall(WorkerAddr destAddr, RpcMethodSpec methodSpec, long timeoutMs = 0) {
        Debug.Assert(worker.InEventLoop());
        S2SSession? session = GetSessionOfNode(destAddr.nodeId);
        if (session == null) {
            throw RpcClientException.SessionNotExist(destAddr);
        }
        if (timeoutMs <= 0) {
            timeoutMs = this.timeoutMs;
        }
        RpcRequest request = NewRequest(session, destAddr, methodSpec, RpcInvokeType.SYNC_CALL);
        IPromise<RpcResult> promise = new Promise<RpcResult>(); // 允许阻塞 -- promise没有发布出去

        long requestId = request.RequestId; // 提前保留requestId
        rpcSupport.AddWatcher(session.sessionId, requestId, promise); // 先添加watcher再发送
        rpcSupport.SendRequest(request); // send以后不可再访问request，可能已被回收
        try {
            RpcResult result = promise.Get(TimeSpan.FromMilliseconds(timeoutMs));
            if (result.IsSucceeded) {
                return result.Data!;
            }
            throw RpcServerException.NewServerException(result.ErrorCode, result.ErrorMsg);
        }
        catch (CompletionException ex) {
            throw RpcClientException.UnknownException(ex);
        }
        finally {
            rpcSupport.RemoveWatcher(session.sessionId, requestId);
        }
    }

    #endregion

    #region sendResponse

    public void SendResult(long sessionId, WorkerAddr destAddr,
                           long requestId, int serviceId, int methodId,
                           object? result, bool sharable) {
        RpcResponse response = NewResponse(sessionId, destAddr, requestId, serviceId, methodId,
            result, sharable);
        rpcSupport.SendResponse(response);
    }

    public void SendError(long sessionId, WorkerAddr destAddr,
                          long requestId, int serviceId, int methodId,
                          int errorCode, string? msg) {
        RpcResponse response = NewResponse(sessionId, destAddr, requestId, serviceId, methodId,
            errorCode, msg);
        rpcSupport.SendResponse(response);
    }

    public void SendError(long sessionId, WorkerAddr destAddr,
                          long requestId, int serviceId, int methodId,
                          Exception ex) {
        RpcResult result = ToErrorResult(ex);
        RpcResponse response = NewResponse(sessionId, destAddr, requestId, serviceId, methodId,
            result.ErrorCode, result.ErrorMsg);
        rpcSupport.SendResponse(response);
    }

    public void SendAsyncResult(long sessionId, WorkerAddr destAddr,
                                long requestId, int serviceId, int methodId,
                                IFuture future, bool sharable) {
        SendAsyncResult0(sessionId, destAddr, requestId, serviceId,
            methodId, new ValueFuture(future), sharable).Forget();
    }

    public void SendAsyncResult<T>(long sessionId, WorkerAddr destAddr,
                                   long requestId, int serviceId, int methodId,
                                   ValueFuture<T> future, bool sharable) {
        SendAsyncResult0(sessionId, destAddr, requestId, serviceId,
            methodId, future.Box(), sharable).Forget();
    }

    public void SendAsyncResult(long sessionId, WorkerAddr destAddr,
                                long requestId, int serviceId, int methodId,
                                ValueFuture future, bool sharable) {
        SendAsyncResult0(sessionId, destAddr, requestId, serviceId,
            methodId, future, sharable).Forget();
    }

    /// <summary>
    /// 全部转为ValueFuture类型，可以减少生成的状态机代码，池化对象的利用率也就更好
    /// </summary>
    private async ValueFuture SendAsyncResult0(long sessionId, WorkerAddr destAddr,
                                               long requestId, int serviceId, int methodId,
                                               ValueFuture future, bool sharable) {
        TaskResult r = await future.GetAwaitable(worker, SuppressedTypes.All, TaskOptions.STAGE_TRY_INLINE, true);
        if (r.IsSucceeded) {
            SendResult(sessionId, destAddr, requestId, serviceId, methodId, r.Result, sharable);
        } else {
            SendError(sessionId, destAddr, requestId, serviceId, methodId, r.Exception!);
        }
    }

    #endregion

    #region 生命周期

    public override void Start() {
        // 接收Node派发到Worker的请求和响应，再转换给RpcSupport
        worker.Subscribe(FxUtils.TYPE_NODE_WORKER_REQUEST, this);
        worker.Subscribe(FxUtils.TYPE_NODE_WORKER_RESPONSE, this);
    }

    public void OnEvent(long sequence, ref WorkerEvent evt) {
        switch (evt.Type) {
            case FxUtils.TYPE_NODE_WORKER_REQUEST: {
                OnRcvRequestStep3((RpcRequest)evt.obj1);
                break;
            }
            case FxUtils.TYPE_NODE_WORKER_RESPONSE: {
                OnRcvResponseStep3((RpcResponse)evt.obj1);
                break;
            }
            default: throw new AssertionError();
        }
    }

    public override void Update() {
        long curTime = timeModule.Time;
        RpcRequestStub? stub;
        while (stubQueue.TryPeekHead(out stub)) {
            if (curTime < stub.deadline) {
                return;
            }
            stubQueue.Dequeue();
            // 从关联Session中删除
            S2SSession? session = GetSession(stub.sessionId);
            if (session != null) {
                session.stubMap.Remove(stub.requestId);
            }
            logger.Info("rpc timeout, destAddr {0}, requestId {1}, serviceId {2}, methodId {3}",
                stub.destAddr, stub.requestId, stub.serviceId, stub.methodId);

            stub.promise.TrySetException(stub.rid, RpcClientException.Timeout(stub.destAddr));
            stubPool.Release(stub);
        }
    }

    public override void Stop() {
        localSession.stubMap.Clear();
        foreach (S2SSession session in sessionMgr.SessionMap.Values) {
            session.stubMap.Clear();
        }
        stubQueue.Clear();
        stubPool.Clear();
    }

    #endregion

    #region rcv-request

    public void OnRcvRequestStep3(RpcRequest request) {
        RpcMethodInvoker? invoker = proxyRegistry.GetInvoker(request.ServiceId, request.MethodId);
        if (invoker == null || proxyRegistry.IsDisabled(request.ServiceId, request.MethodId)) {
            Reject(request, RpcErrorCodes.SERVER_UNSUPPORTED_INTERFACE);
            return;
        }
        // 拦截测试
        int code = sessionMgr.Test(request);
        if (code != 0) {
            Reject(request, code);
            return;
        }
        try {
            invoker.Invoke(this, request.SessionId, request.SrcAddr,
                request.RequestId, request.ServiceId, request.MethodId, request.InvokeType,
                request.Data);
        }
        catch (Exception ex) {
            LogInvokeException(request, ex);
            // 其实还可以感知一下context是否发送了结果
            if (RpcInvokeType.IsCall(request.InvokeType)) {
                SendError(request.SessionId, request.SrcAddr,
                    request.RequestId, request.ServiceId, request.MethodId, ex);
            }
        }
        RpcRequest.Release(request); // 回收
    }

    /** 拒绝客户端请求 -- node和worker的拒绝有差异，地址不同 */
    private void Reject(RpcRequest request, int code) {
        logger.Warn("reject the request, reason {0}, sessionId {1}, srcAddr {2}, requestId {3}, serviceId {4}, methodId {5}",
            code,
            request.SessionId, request.SrcAddr,
            request.RequestId, request.ServiceId, request.MethodId);
        if (RpcInvokeType.IsCall(request.InvokeType)) {
            SendError(request.SessionId, request.SrcAddr, // srcAddr
                request.RequestId, request.ServiceId, request.MethodId,
                code, null);
        }
        RpcRequest.Release(request);
    }

    /** 记录执行异常 */
    private static void LogInvokeException(RpcRequest request, Exception ex) {
        if (!(ex is NoLogRequiredException)) {
            logger.Warn(ex, "invoke caught exception, sessionId {0}, srcAddr {1}, requestId {2}, serviceId {3}, methodId {4}",
                request.SessionId, request.SrcAddr,
                request.RequestId, request.ServiceId, request.MethodId);
        }
    }

    #endregion

    #region rcv-response

    public void OnRcvResponseStep3(RpcResponse response) {
        S2SSession? session = GetSession(response.SessionId);
        if (session == null) {
            LogResponseTimeout(response);
            RpcResponse.Release(response);
            return;
        }
        if (!session.stubMap.Remove(response.RequestId, out RpcRequestStub stub)) {
            LogResponseTimeout(response);
            RpcResponse.Release(response);
            return;
        }
        stubQueue.Remove(stub);

        if (response.IsSucceeded) {
            stub.promise.TrySetResult(stub.rid, response.Data);
        } else {
            Exception ex = RpcServerException.NewServerException(response.ErrorCode, response.ErrorMsg);
            stub.promise.TrySetException(stub.rid, ex);
        }
        stubPool.Release(stub); // 回收
        RpcResponse.Release(response);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void LogResponseTimeout(RpcResponse response) {
        logger.Info("rcv rpc response, but request is timeout, sessionId {0}, srcAddr {1}, requestId {2}",
            response.SessionId, response.SrcAddr, response.RequestId);
    }

    #endregion

    #region factory

    private RpcRequestStub NewStub(RpcRequest request, ValuePromise<object> promise, int rid, long deadline) {
        RpcRequestStub stub = stubPool.Acquire();
        stub.deadline = deadline;
        stub.rid = rid;
        stub.promise = promise;

        stub.sessionId = request.SessionId;
        stub.destAddr = request.DestAddr;
        stub.requestId = request.RequestId;
        stub.serviceId = request.ServiceId;
        stub.methodId = request.MethodId;
        return stub;
    }

    private RpcRequest NewRequest(S2SSession session, WorkerAddr destAddr, in RpcMethodSpec methodSpec, int invokeType) {
        RpcRequest request = RpcRequest.Acquire();
        request.SessionId = session.sessionId;
        request.SrcAddr = selfAddr;
        request.DestAddr = destAddr;

        // 本地session使用全局的序号分配器，sessionId + requestId才具有唯一性
        if (session.sessionId == 0) {
            request.RequestId = rpcSupport.NextRequestId();
        } else {
            request.RequestId = session.NextRequestId();
        }
        request.InvokeType = (invokeType);
        request.CreateTime = timeModule.Time;
        request.ServiceId = methodSpec.ServiceId;
        request.MethodId = methodSpec.MethodId;
        request.Data = methodSpec.Parameter;
        request.Sharable = methodSpec.Sharable;

        // 数据可共享的情况下：进程内不序列化；如果需要发送到网络，则延迟到Node序列化
        if (!(enableLocalSharing && request.Sharable) && request.Data != null) {
            rpcSupport.EncodeParameter(request);
        }
        return request;
    }

    /** 任意线程调用 */
    private RpcResponse NewResponse(long sessionId, WorkerAddr destAddr,
                                    long requestId, int serviceId, int methodId,
                                    object? result, bool sharable) {
        RpcResponse response = RpcResponse.Acquire();
        response.SessionId = sessionId;
        response.SrcAddr = selfAddr;
        response.DestAddr = destAddr;

        response.RequestId = requestId;
        response.ServiceId = serviceId;
        response.MethodId = methodId;
        response.Sharable = sharable;
        response.SetSuccess(result);

        // 数据可共享的情况下：进程内不序列化；如果需要发送到网络，则延迟到Node序列化
        if (!(enableLocalSharing && response.Sharable) && response.Data != null) {
            rpcSupport.EncodeResult(response);
        }
        return response;
    }

    /** 任意线程调用 */
    private RpcResponse NewResponse(long sessionId, WorkerAddr destAddr,
                                    long requestId, int serviceId, int methodId,
                                    int errorCode, string? msg) {
        RpcResponse response = RpcResponse.Acquire();
        response.SessionId = sessionId;
        response.SrcAddr = selfAddr;
        response.DestAddr = destAddr;

        response.RequestId = requestId;
        response.ServiceId = serviceId;
        response.MethodId = methodId;
        response.SetFailed(errorCode, msg);
        return response;
    }

    /** 解析异常信息为错误码信息 */
    private static RpcResult ToErrorResult(Exception ex) {
        if (ex == null) throw new ArgumentNullException(nameof(ex));
        // future对异常进行了封装
        ex = ExecutorUtil.UnwrapCompletionException(ex);
        if (ex is ErrorCodeException codeException) {
            return new RpcResult(codeException.ErrorCode, codeException.Message);
        }
        if (ex is RpcException rpcException) {
            return new RpcResult(rpcException.ErrorCode, rpcException.Message);
        }
        return new RpcResult(RpcErrorCodes.SERVER_UNKNOWN_EXCEPTION, ex.Message);
    }

    #endregion
}
}