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
using System.Threading;
using Wjybxx.Commons.Concurrent;
using Wjybxx.Commons.Ex;

namespace Wjybxx.BigCat.Core
{
/// <summary>
/// Rpc客户端异常
/// </summary>
public class RpcClientException : RpcException
{
    public RpcClientException(int errorCode) : base(errorCode) {
    }

    public RpcClientException(int errorCode, string? message) : base(errorCode, message) {
    }

    public RpcClientException(int errorCode, string? message, Exception? innerException) : base(errorCode, message, innerException) {
    }

    /// <summary>
    /// C#端为了性能考虑，暂不打印堆栈
    /// (默认转发给内部异常)
    /// </summary>
    public override string? StackTrace => InnerException?.StackTrace;

    /** 请求超时 */
    public static RpcClientException Timeout(WorkerAddr destAddr) {
        return new RpcClientException(RpcErrorCodes.LOCAL_TIMEOUT);
    }

    /** session不存在 -- 不需要填充堆栈，意义不大 */
    public static RpcClientException SessionNotExist(WorkerAddr destAddr) {
        return new RpcClientException(RpcErrorCodes.LOCAL_SESSION_NOT_EXIST, "destAddr: " + destAddr);
    }

    /** session关闭 -- 不需要填充堆栈，意义不大 */
    public static RpcClientException SessionClosed(WorkerAddr destAddr) {
        return new RpcClientException(RpcErrorCodes.LOCAL_SESSION_CLOSED, "destAddr: " + destAddr);
    }

    public static RpcClientException UnknownException(Exception ex) {
        if (ex is CompletionException) {
            ex = ExecutorUtil.UnwrapCompletionException(ex);
        }
        return new RpcClientException(RpcErrorCodes.LOCAL_UNKNOWN_EXCEPTION, "unknownException", ex);
    }
}
}