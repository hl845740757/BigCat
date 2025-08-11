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

namespace Wjybxx.BigCat.Fx
{
/// <summary>
/// Rpc方法代理
/// </summary>
public delegate void RpcMethodProxy<T>(ref RpcContext<T> context, object parameter);

/// <summary>
/// Rpc方法调用器--用于解决泛型问题
/// </summary>
public interface RpcMethodInvoker
{
    /// <summary>
    /// 执行方法调用
    ///
    /// 注：不能传入<see cref="RpcRequest"/>对象，否则不能兼容自定义协议。
    /// </summary>
    void Invoke(S2SRpcClient rpcClient, long sessionId, WorkerAddr srcAddr,
                long requestId, int serviceId, int methodId, int invokeType,
                object parameter);
}

/// <summary>
/// Rpc方法调用器--用于解决泛型问题
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class RpcMethodInvoker<T> : RpcMethodInvoker
{
    private readonly RpcMethodProxy<T> proxy;

    public RpcMethodInvoker(RpcMethodProxy<T> proxy) {
        this.proxy = proxy ?? throw new ArgumentNullException(nameof(proxy));
    }

    public void Invoke(S2SRpcClient rpcClient, long sessionId, WorkerAddr srcAddr,
                       long requestId, int serviceId, int methodId, int invokeType,
                       object parameter) {
        RpcContext<T> context = new RpcContext<T>(rpcClient, sessionId, srcAddr, requestId, serviceId, methodId, invokeType);
        proxy.Invoke(ref context, parameter);
    }
}
}