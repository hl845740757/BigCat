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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Wjybxx.Commons;
using Wjybxx.Commons.Concurrent;
using Wjybxx.Commons.Inject;
using Wjybxx.Commons.Logger;

namespace Wjybxx.BigCat.Core
{
/// <summary>
/// Rpc支持模块
/// 
/// 1.设置属性应该启动Node之前，运行时不可修改对象的属性
/// 2.所有的线程切换都在该类中，避免代码分散
/// 3.该模块仅负责服务器之间的rpc通信
/// </summary>
public class RpcSupport : EventLoopModule, IAgentEventHandler<WorkerEvent>
{
    private static readonly ILogger logger = LoggerFactory.GetLogger(typeof(RpcSupport));
#nullable disable

    /** 是否启用日志 -- 允许运行时调整 */
    private volatile bool enableLog;
    /** 用于为Worker间的Rpc分配RequestId -- 原子更新 */
    private long sequencer = 0;
    /** 用于支持同步调用 -- sessionId到worker的映射 */
    private readonly ConcurrentDictionary<long, Worker> session2WorkerMap = new();
    /** 用于支持同步调用 -- finally块中删除 */
    private readonly ConcurrentDictionary<Key, IPromise<RpcResult>> watcherMap = new();

    private Node node;
    private WorkerAddr nodeAddr;
    private RpcSerializer serializer;
    private RpcMethodRegistry methodRegistry;
    private RpcRouter router;
#nullable enable

    #region 设置

    public bool EnableLog {
        get => enableLog;
        set => enableLog = value;
    }

    #endregion

    #region 生命周期

    public override void ResolveDependence() {
        this.node = (Node)Entity;
        this.nodeAddr = node.NodeAddr;

        this.serializer = node.Injector.GetInstance<RpcSerializer>();
        this.methodRegistry = node.Injector.GetInstance<RpcMethodRegistry>();
        this.router = node.Injector.GetInstance<RpcRouter>();
    }

    public override void Start() {
        // net到node的请求和响应
        node.Subscribe(FxUtils.TYPE_NET_NODE_REQUEST, this);
        node.Subscribe(FxUtils.TYPE_NET_NODE_RESPONSE, this);
        // worker到node的请求和响应
        node.Subscribe(FxUtils.TYPE_WORKER_NODE_REQUEST, this);
        node.Subscribe(FxUtils.TYPE_WORKER_NODE_RESPONSE, this);
    }

    public void OnEvent(long sequence, ref WorkerEvent evt) {
        switch (evt.Type) {
            case FxUtils.TYPE_NET_NODE_REQUEST: {
                OnRcvRequestStep2((RpcRequest)evt.obj1);
                break;
            }
            case FxUtils.TYPE_NET_NODE_RESPONSE: {
                OnRcvResponseStep2((RpcResponse)evt.obj1);
                break;
            }
            case FxUtils.TYPE_WORKER_NODE_REQUEST: {
                SendRequestStep2((RpcRequest)evt.obj1);
                break;
            }
            case FxUtils.TYPE_WORKER_NODE_RESPONSE: {
                SendResponseStep2((RpcResponse)evt.obj1);
                break;
            }
            default: throw new AssertionError();
        }
    }

    public override void Stop() {
        session2WorkerMap.Clear();
        watcherMap.Clear();
    }

    #endregion

    #region support

    /// <summary>
    /// 该方法可能被Worker并发调用
    /// </summary>
    /// <returns></returns>
    public long NextRequestId() {
        return Interlocked.Increment(ref sequencer);
    }

    /// <summary>
    /// 注册session
    /// </summary>
    /// <param name="sessionId">sessionId</param>
    /// <param name="worker">session所在的线程</param>
    /// <exception cref="ArgumentException"></exception>
    public void AddSession(long sessionId, Worker worker) {
        if (!session2WorkerMap.TryAdd(sessionId, worker)) {
            throw new ArgumentException("sessionId: " + sessionId);
        }
    }

    /// <summary>
    /// 删除session
    /// </summary>
    /// <param name="sessionId"></param>
    public void RemoveSession(long sessionId) {
        // ConcurrentDictionary没有Remove方法 -- Remove是扩展方法
        session2WorkerMap.TryRemove(sessionId, out _);
    }

    /// <summary>
    /// 添加watcher
    /// </summary>
    /// <param name="sessionId">会话id</param>
    /// <param name="requestId">请求id</param>
    /// <param name="promise">关联的Promise</param>
    /// <exception cref="ArgumentException"></exception>
    public void AddWatcher(long sessionId, long requestId, IPromise<RpcResult> promise) {
        if (promise == null) throw new ArgumentNullException(nameof(promise));
        Key key = new Key(sessionId, requestId);
        if (!watcherMap.TryAdd(key, promise)) {
            throw new ArgumentException($"sessionId: {sessionId}, requestId: {requestId}");
        }
    }

    /// <summary>
    /// 删除watcher
    /// </summary>
    /// <param name="sessionId"></param>
    /// <param name="requestId"></param>
    public void RemoveWatcher(long sessionId, long requestId) {
        Key key = new Key(sessionId, requestId);
        watcherMap.TryRemove(key, out _);
    }

    #endregion

    #region send-request

    /** worker请求发送rpc请求 */
    public void SendRequest(RpcRequest request) {
        if (node.InEventLoop()) {
            SendRequestStep2(request);
        } else {
            long seq = node.NextSequence();
            if (seq < 0) return; // shutdown
            WorkerEvent evt = default; // 发布事件到node时使用聚合发布
            evt.Type = FxUtils.TYPE_WORKER_NODE_REQUEST;
            evt.obj1 = request;
            node.Publish(seq, in evt);
        }
    }


    /** 收到worker到node的request */
    private void SendRequestStep2(RpcRequest request) {
        if (enableLog) {
            logger.Info("snd rpc request {0}", request);
        }
        if (request.DestAddr.nodeId == nodeAddr.nodeId) {
            // 进程内包 -- 数据已序列化，或是可共享的；直接触发接收
            OnRcvRequest(request);
        } else {
            // 网络包 -- router负责回收
            router.Send(request);
        }
    }

    #endregion

    #region send-response

    /** node或worker线程调用 */
    public void SendResponse(RpcResponse response) {
        if (node.InEventLoop()) {
            SendResponseStep2(response);
        } else {
            long seq = node.NextSequence();
            if (seq < 0) return; // shutdown
            WorkerEvent evt = default; // 发布事件到node时使用聚合发布
            evt.Type = FxUtils.TYPE_WORKER_NODE_RESPONSE;
            evt.obj1 = response;
            node.Publish(seq, in evt);
        }
    }

    /** 当前在node线程 */
    private void SendResponseStep2(RpcResponse response) {
        if (enableLog) {
            logger.Info("snd rpc response {0}", response);
        }
        if (response.DestAddr.nodeId == nodeAddr.nodeId) {
            // 进程内包 -- 数据已序列化，或是可共享的；直接触发接收
            OnRcvResponse(response);
        } else {
            // 网络包 -- router负责回收
            router.Send(response);
        }
    }

    #endregion

    #region rcv-request

    /**
     * 通知Support模块收到一个Rpc请求
     * 1.该方法由IO线程调用 -- 即RpcRouter类调用。
     * 2.如果外部未反序列化请求参数，则在Node线程自动反序列化。
     * 3.如果request可能发给多个Node，应在外部拷贝 -- {@link #deepCopy(RpcRequest, int)}
     */
    public void OnRcvRequest(RpcRequest request) {
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (enableLog) {
            logger.Info("rcv rpc request {0}", request);
        }
        if (node.InEventLoop()) {
            OnRcvRequestStep2(request);
        } else {
            long seq = node.NextSequence();
            if (seq < 0) return; // shutdown
            WorkerEvent evt = default; // 发布事件到node时使用聚合发布
            evt.Type = FxUtils.TYPE_NET_NODE_REQUEST;
            evt.obj1 = request;
            node.Publish(seq, in evt);
        }
    }

    /** 当前在Node线程 */
    private void OnRcvRequestStep2(RpcRequest request) {
        // 在使用之前需要先反序列化
        if (request.IsBytes && !DecodeParameter(request)) {
            Reject(request, RpcErrorCodes.SERVER_DESERIALIZE_FAILED);
            return;
        }
        // 判断是否对外提供服务
        if (!node.ServiceInfoMap.TryGetValue(request.ServiceId, out ServiceInfo? serviceInfo)
            || serviceInfo.workerList.Count == 0) {
            Reject(request, RpcErrorCodes.SERVER_UNSUPPORTED_INTERFACE);
            return;
        }
        // 优先按照SessionId查找，其次按workerId查找，最后随机分配
        if (session2WorkerMap.TryGetValue(request.SessionId, out Worker? worker)) {
            OnRcvRequestStep2(worker, request);
            return;
        }
        string? destWorkerId = request.DestAddr.workerId;
        IList<Worker> workerList = serviceInfo.workerList;
        if (!string.IsNullOrWhiteSpace(destWorkerId)) {
            if (destWorkerId == "*") {
                // 广播 -- 顺序不应该产生影响
                List<RpcRequest> clonedRequests = DeepCopy(request, workerList.Count);
                for (int idx = 0; idx < workerList.Count; idx++) {
                    worker = workerList[idx];
                    RpcRequest clonedRequest = clonedRequests[idx];
                    OnRcvRequestStep2(worker, clonedRequest);
                }
            } else {
                // 指定worker -- 理论上还可进行复杂的模式匹配，以后再说
                worker = FindWorker(workerList, destWorkerId);
                if (worker == null) {
                    Reject(request, RpcErrorCodes.SERVER_WORKER_NOT_EXIST);
                    return;
                }
                OnRcvRequestStep2(worker, request);
            }
        } else {
            // 随机分配 -- 多worker时根据源地址nodeId保证路由的一致性
            if (workerList.Count == 1) {
                worker = workerList[0];
            } else {
                int idx = Math.Abs(request.SrcAddr.nodeId) % workerList.Count;
                worker = workerList[idx];
            }
            OnRcvRequestStep2(worker, request);
        }
    }

    private Worker? FindWorker(IList<Worker> workerList, string workerId) {
        for (int idx = 0; idx < workerList.Count; idx++) {
            Worker worker = workerList[idx];
            if (workerId == worker.WorkerAddr.workerId) {
                return worker;
            }
        }
        return null;
    }

    private void OnRcvRequestStep2(Worker worker, RpcRequest request) {
        if (worker == node) {
            worker.ControlData.rpcClient.OnRcvRequestStep3(request);
        } else {
            long seq = worker.NextSequence();
            if (seq < 0) return; // shutdown
            ref WorkerEvent evt = ref worker.GetEventRef(seq);
            evt.Type = FxUtils.TYPE_NODE_WORKER_REQUEST;
            evt.obj1 = request;
            worker.Publish(seq);
        }
    }

    /** 拒绝客户端请求 -- node节点的拒绝和worker拒绝逻辑有差异，地址不同 */
    private void Reject(RpcRequest request, int code) {
        logger.Warn("reject the request, reason {0}, sessionId {1}, srcAddr {2}, serviceId {3}, methodId {4}",
            code, request.SessionId, request.SrcAddr, request.ServiceId, request.MethodId);
        //
        if (RpcInvokeType.IsCall(request.InvokeType)) {
            SendError(request.SessionId, request.SrcAddr, // srcAddr
                request.RequestId, request.ServiceId, request.MethodId,
                code, null);
        }
        RpcRequest.Release(request);
    }

    private void SendError(long sessionId, WorkerAddr destAddr,
                           long requestId, int serviceId, int methodId,
                           int errorCode, string? msg) {
        RpcResponse response = RpcResponse.Acquire();
        response.SessionId = (sessionId);
        response.SrcAddr = nodeAddr; // 进程内调用的话，可识别是被谁拒绝
        response.DestAddr = destAddr;

        response.RequestId = requestId;
        response.ServiceId = serviceId;
        response.MethodId = methodId;

        response.SetFailed(errorCode, msg);
        SendResponse(response);
    }

    #endregion

    #region rcv-response

    /**
     * 通知Support模块收到一个Rpc响应
     * 1.该方法由IO线程调用 -- 即RpcRouter类调用。
     * 2.如果外部未反序列化结果，则在Node线程自动反序列化
     */
    public void OnRcvResponse(RpcResponse response) {
        if (response == null) throw new ArgumentNullException(nameof(response));
        // 不重复打印旧进程的Rpc响应
        if (enableLog) {
            logger.Info("rcv rpc response {0}", response);
        }
        // watcher需要在IO线程测试
        Key key = new Key(response.SessionId, response.RequestId);
        if (watcherMap.TryRemove(key, out IPromise<RpcResult>? watcher)) { // 同步调用结果
            RpcResult result = new RpcResult(response.ErrorCode, response.Data);
            watcher.TrySetResult(result);
            return;
        }
        if (node.InEventLoop()) {
            OnRcvResponseStep2(response);
        } else {
            long seq = node.NextSequence();
            if (seq < 0) return; // shutdown
            WorkerEvent evt = default; // 发布事件到node时使用聚合发布
            evt.Type = FxUtils.TYPE_NET_NODE_RESPONSE;
            evt.obj1 = response;
            node.Publish(seq, in evt);
        }
    }

    /** 当前在Node线程 */
    private void OnRcvResponseStep2(RpcResponse response) {
        // 使用之前反序列化
        if (response.IsBytes && !DecodeResult(response)) {
            response.SetFailed(RpcErrorCodes.LOCAL_DESERIALIZE_FAILED, "data error");
        }
        // 优先根据sessionId查询Worker，其次按workerId查找
        session2WorkerMap.TryGetValue(response.SessionId, out Worker? worker);
        if (worker == null && response.DestAddr.workerId != null) {
            worker = node.FindWorker(response.DestAddr.workerId);
        }
        if (worker == null) {
            logger.Info("rcv old process rpc response, remote {0}", response.SrcAddr);
            RpcResponse.Release(response);
            return;
        }
        if (worker == node) {
            worker.ControlData.rpcClient.OnRcvResponseStep3(response);
        } else {
            long seq = worker.NextSequence();
            if (seq < 0) return; // shutdown
            ref WorkerEvent evt = ref worker.GetEventRef(seq);
            evt.Type = FxUtils.TYPE_NODE_WORKER_RESPONSE;
            evt.obj1 = response;
            worker.Publish(seq);
        }
    }

    #endregion

    #region 编解码

    /** 序列化rpc参数 */
    public void EncodeParameter(RpcRequest request) {
        if (request.Data == null) { // null不序列化
            return;
        }
        RpcMethodInfo methodInfo = methodRegistry.GetMethodInfo(request.ServiceId, request.MethodId)!;
        byte[] bytes;
        if (request.Data is byte[] codedBytes) {
            bytes = ArrayUtil.CopyOf(codedBytes);
        } else if (methodInfo.parameterParser != null) {
            bytes = ProtobufUtils.ToBytes(request.Data);
        } else {
            bytes = serializer.Write(request.Data, methodInfo.parameterType);
        }
        request.Data = bytes;
    }

    /** 反序列化rpc参数 */
    public bool DecodeParameter(RpcRequest request) {
        RpcMethodInfo methodInfo = methodRegistry.GetMethodInfo(request.ServiceId, request.MethodId)!;
        byte[] data = (byte[])request.Data;
        try {
            object? parameter;
            if (methodInfo.parameterType == null) {
                parameter = null; // 对方将void序列化为了空字节数组
            } else if (methodInfo.parameterParser != null) {
                parameter = methodInfo.parameterParser.ParseFrom(data);
            } else {
                parameter = serializer.Read(data, methodInfo.parameterType);
            }
            request.Data = parameter;
            return true;
        }
        catch (Exception ex) {
            logger.Info(ex, "decode parameters caught exception, serviceId {0}, methodId {1}",
                request.ServiceId, request.MethodId);
            return false;
        }
    }

    /** 序列化结果 */
    public void EncodeResult(RpcResponse response) {
        if (response.Data == null) { // null不序列化
            return;
        }
        RpcMethodInfo methodInfo = methodRegistry.GetMethodInfo(response.ServiceId, response.MethodId)!;
        byte[] bytes;
        if (response.Data is byte[] codedBytes) {
            bytes = ArrayUtil.CopyOf(codedBytes);
        } else if (response.IsFailed) {
            bytes = ObjectUtil.GetUtf8Bytes(response.ErrorMsg!);
        } else if (methodInfo.resultParser != null) {
            bytes = ProtobufUtils.ToBytes(response.Data);
        } else {
            bytes = serializer.Write(response.Data, methodInfo.resultType);
        }
        response.Data = bytes;
    }

    /** 反序列化结果 */
    public bool DecodeResult(RpcResponse response) {
        RpcMethodInfo methodInfo = methodRegistry.GetMethodInfo(response.ServiceId, response.MethodId)!;
        byte[] data = (byte[])response.Data;
        try {
            if (response.IsFailed) {
                response.Data = data.Length > 0 ? ObjectUtil.GetUtf8String(data) : "";
                return true;
            }
            object? result;
            if (methodInfo.resultType == null) {
                result = null; // 对方将void序列化为了空字节数组
            } else if (methodInfo.resultParser != null) {
                result = methodInfo.resultParser.ParseFrom(data);
            } else {
                result = serializer.Read(data, methodInfo.resultType);
            }
            response.Data = result;
            return true;
        }
        catch (Exception ex) {
            logger.Info(ex, "decode result caught exception, serviceId {0}, methodId {1}",
                response.ServiceId, response.MethodId);
            return false;
        }
    }


    /** 深度拷贝rpc请求参数 -- src会包含在结果中返回；暂不池化List，广播很少 */
    private List<RpcRequest> DeepCopy(RpcRequest src, int count) {
        List<RpcRequest> list = new List<RpcRequest>(count);
        list.Add(src);
        if (count == 1) {
            return list;
        }
        RpcMethodInfo? methodInfo = methodRegistry.GetMethodInfo(src.ServiceId, src.MethodId);
        Debug.Assert(methodInfo != null);
        byte[] bytesParameters = serializer.Write(src.Data, methodInfo.parameterType);
        for (int idx = 1; idx < count; idx++) {
            RpcRequest request = RpcRequest.Acquire();
            request.SessionId = src.SessionId;
            request.SrcAddr = src.SrcAddr;
            request.DestAddr = src.DestAddr;

            request.RequestId = src.RequestId;
            request.ServiceId = src.ServiceId;
            request.MethodId = src.MethodId;
            request.InvokeType = src.InvokeType;

            request.Data = bytesParameters;
            DecodeParameter(request);
            list.Add(request);
        }
        return list;
    }

    #endregion

    private readonly struct Key : IEquatable<Key>
    {
        public readonly long sessionId;
        public readonly long requestId;

        public Key(long sessionId, long requestId) {
            this.sessionId = sessionId;
            this.requestId = requestId;
        }

        public bool Equals(Key other) {
            return sessionId == other.sessionId && requestId == other.requestId;
        }

        public override bool Equals(object? obj) {
            return obj is Key other && Equals(other);
        }

        public override int GetHashCode() {
            unchecked {
                return (sessionId.GetHashCode() * 397) ^ requestId.GetHashCode();
            }
        }

        public static bool operator ==(Key left, Key right) {
            return left.Equals(right);
        }

        public static bool operator !=(Key left, Key right) {
            return !left.Equals(right);
        }

        public override string ToString() {
            return $"{nameof(sessionId)}: {sessionId}, {nameof(requestId)}: {requestId}";
        }
    }
}
}