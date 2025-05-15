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
using Wjybxx.Commons.Concurrent;

namespace Wjybxx.BigCat.Fx
{
/// <summary>
/// 1.该接口和<see cref="RpcClient"/>分离，属于关注点分离。
/// 2.这里的接口由<see cref="RpcContext{T}"/>调用，
/// 3.我们这里不再封装额外的方法对象来传输参数，因为用户基本不会手动调用这里的方法。
/// 4.如果返回结果的线程可能不是当前Worker，要小心多线程问题。
/// </summary>
public interface RpcClientImpl
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="sessionId">会话id</param>
    /// <param name="destAddr">目标地址</param>
    /// <param name="requestId">请求id</param>
    /// <param name="serviceId">服务id</param>
    /// <param name="methodId">方法id</param>
    /// <param name="result">结果</param>
    /// <param name="sharable">结果对象是否可共享</param>
    void SendResult(long sessionId, WorkerAddr destAddr,
                    long requestId, int serviceId, int methodId,
                    object? result, bool sharable);

    /// <summary>
    /// 发送异常执行的结果
    /// </summary>
    /// <param name="sessionId">会话id</param>
    /// <param name="destAddr">目标地址</param>
    /// <param name="requestId">请求id</param>
    /// <param name="serviceId">服务id</param>
    /// <param name="methodId">方法id</param>
    /// <param name="errorCode">错误码</param>
    /// <param name="msg">错误消息</param>
    void SendError(long sessionId, WorkerAddr destAddr,
                   long requestId, int serviceId, int methodId,
                   int errorCode, string? msg);

    /// <summary>
    /// 发送异常执行的结果
    /// </summary>
    /// <param name="sessionId">会话id</param>
    /// <param name="destAddr">目标地址</param>
    /// <param name="requestId">请求id</param>
    /// <param name="serviceId">服务id</param>
    /// <param name="methodId">方法id</param>
    /// <param name="ex">异常信息</param>
    void SendError(long sessionId, WorkerAddr destAddr,
                   long requestId, int serviceId, int methodId,
                   Exception ex);

    /// <summary>
    /// 发送异步结果
    /// </summary>
    /// <param name="sessionId">会话id</param>
    /// <param name="destAddr">目标地址</param>
    /// <param name="requestId">请求id</param>
    /// <param name="serviceId">服务id</param>
    /// <param name="methodId">方法id</param>
    /// <param name="future">异步结果</param>
    /// <param name="sharable">是否可共享</param>
    void SendAsyncResult(long sessionId, WorkerAddr destAddr,
                         long requestId, int serviceId, int methodId,
                         IFuture future, bool sharable);

    /// <summary>
    /// 发送异步结果
    /// </summary>
    /// <param name="sessionId">会话id</param>
    /// <param name="destAddr">目标地址</param>
    /// <param name="requestId">请求id</param>
    /// <param name="serviceId">服务id</param>
    /// <param name="methodId">方法id</param>
    /// <param name="future">异步结果</param>
    /// <param name="sharable">是否可共享</param>
    void SendAsyncResult<T>(long sessionId, WorkerAddr destAddr,
                            long requestId, int serviceId, int methodId,
                            ValueFuture<T> future, bool sharable);

    /// <summary>
    /// 发送异步结果
    /// </summary>
    /// <param name="sessionId">会话id</param>
    /// <param name="destAddr">目标地址</param>
    /// <param name="requestId">请求id</param>
    /// <param name="serviceId">服务id</param>
    /// <param name="methodId">方法id</param>
    /// <param name="future">异步结果</param>
    /// <param name="sharable">是否可共享</param>
    void SendAsyncResult(long sessionId, WorkerAddr destAddr,
                         long requestId, int serviceId, int methodId,
                         ValueFuture future, bool sharable);
}
}