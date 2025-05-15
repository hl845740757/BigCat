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
using System.Runtime.CompilerServices;
using Wjybxx.Commons;
using Wjybxx.Commons.Concurrent;

namespace Wjybxx.BigCat.Fx
{
/// <summary>
/// Rpc执行上下文
///
/// 由于我们会将RpcContext对象暴露给用户，以及监听Future的结果，因此RpcContext无法简单池化；
/// 在C#端我们选择将Context实现为值类型，这样就无需池化。
///
/// 1.结构体较大，当传递给其它方法时应当使用in或ref修饰。
/// 2.泛型参数建议使用object代替void，这样在特殊情况下可以传递结果给请求方。
/// </summary>
public struct RpcContext<T>
{
    private readonly RpcClientImpl rpcClient;
    private readonly long sessionId;
    private readonly WorkerAddr remoteAddr;
    private readonly long requestId;
    private readonly int serviceId;
    private readonly int methodId;
    private readonly int invokeType;
    private int options;

    public RpcContext(RpcClientImpl rpcClient,
                      long sessionId, WorkerAddr remoteAddr,
                      long requestId, int serviceId, int methodId, int invokeType) : this() {
        this.rpcClient = rpcClient ?? throw new ArgumentNullException(nameof(rpcClient));
        this.sessionId = sessionId;
        this.remoteAddr = remoteAddr;
        this.requestId = requestId;
        this.serviceId = serviceId;
        this.methodId = methodId;
        this.invokeType = invokeType;
        this.options = 0;
    }

    /// <summary>
    /// 会话id
    /// 1.服务器与客户端通信时使用该字段
    /// 2.可用于在返回结果前后向目标发送额外的消息
    /// </summary>
    public long SessionId => sessionId;

    /// <summary>
    /// 远端地址
    /// 1.服务器之间通信时使用该字段 -- 它对应的是<see cref="RpcRequest.SrcAddr"/> 
    /// 2.可用于在返回结果前后向目标发送额外的消息
    /// </summary>
    public WorkerAddr RemoteAddr => remoteAddr;

    /// <summary>
    /// 结果对象是否可共享
    /// </summary>
    public bool IsSharable {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (options & MASK_RESULT_SHARABLE) != 0;
        set => options = BitFlags.Set(options, MASK_RESULT_SHARABLE, value);
    }

    /// <summary>
    /// 是否由用户自身返回结果
    /// </summary>
    public bool IsManualReturn {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (options & MASK_RESULT_MANUAL) != 0;
        set => options = BitFlags.Set(options, MASK_RESULT_MANUAL, value);
    }

    /// <summary>
    /// 发送正常结果
    /// </summary>
    /// <param name="result">结果对象，可以是序列化后的Bytes</param>
    public void SendResult(T? result) {
        if (invokeType == RpcInvokeType.ONEWAY) {
            return;
        }
        rpcClient.SendResult(sessionId, remoteAddr,
            requestId, serviceId, methodId,
            result, IsSharable);
    }

    /// <summary>
    /// 发送编码后的结果
    /// </summary>
    /// <param name="result">编码后的结果</param>
    public void SendResult(byte[] result) {
        if (result == null) throw new ArgumentNullException(nameof(result));
        if (invokeType == RpcInvokeType.ONEWAY) {
            return;
        }
        rpcClient.SendResult(sessionId, remoteAddr,
            requestId, serviceId, methodId,
            result, IsSharable);
    }

    /// <summary>
    /// 发送错误结果
    /// </summary>
    /// <param name="code">错误码</param>
    /// <param name="msg">附加消息</param>
    public void SendError(int code, string? msg) {
        if (invokeType == RpcInvokeType.ONEWAY) {
            return;
        }
        rpcClient.SendError(sessionId, remoteAddr,
            requestId, serviceId, methodId,
            code, msg);
    }

    /// <summary>
    /// 发送错误结果
    /// </summary>
    /// <param name="ex">异常信息</param>
    public void SendError(Exception ex) {
        if (invokeType == RpcInvokeType.ONEWAY) {
            return;
        }
        rpcClient.SendError(sessionId, remoteAddr,
            requestId, serviceId, methodId,
            ex);
    }

    /// <summary>
    /// 发送异步结果
    /// </summary>
    /// <param name="future"></param>
    public void SendAsyncResult(IFuture future) {
        if (future == null) throw new ArgumentNullException(nameof(future));
        if (invokeType == RpcInvokeType.ONEWAY) {
            return;
        }
        rpcClient.SendAsyncResult(sessionId, remoteAddr,
            requestId, serviceId, methodId,
            future, IsSharable);
    }

    /// <summary>
    /// 发送异步结果
    /// </summary>
    /// <param name="future"></param>
    public void SendAsyncResult(ValueFuture<T> future) {
        if (invokeType == RpcInvokeType.ONEWAY) {
            return;
        }
        rpcClient.SendAsyncResult(sessionId, remoteAddr,
            requestId, serviceId, methodId,
            future, IsSharable);
    }

    /// <summary>
    /// 发送异步结果
    /// (成功之后返回远程一个null)
    /// </summary>
    /// <param name="future"></param>
    public void SendAsyncResult(ValueFuture future) {
        if (invokeType == RpcInvokeType.ONEWAY) {
            return;
        }
        rpcClient.SendAsyncResult(sessionId, remoteAddr,
            requestId, serviceId, methodId,
            future, IsSharable);
    }

    #region 常量

    /** 返回值可共享 */
    private const int MASK_RESULT_SHARABLE = 1;
    /** 手动返回结果 */
    private const int MASK_RESULT_MANUAL = 1 << 1;

    #endregion
}
}