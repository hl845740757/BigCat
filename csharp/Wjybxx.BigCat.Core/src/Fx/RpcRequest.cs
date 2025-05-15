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

using Wjybxx.Commons.Pool;

namespace Wjybxx.BigCat.Fx
{
/// <summary>
/// rpc请求结构
/// </summary>
public sealed class RpcRequest : RpcProtocol
{
    /** 请求id */
    private long requestId;
    /** 服务id */
    private int serviceId;
    /** 方法id */
    private int methodId;

    /** 调用类型 - <see cref="RpcInvokeType"/> */
    private int invokeType;
    /** 创建时间 -- 是否序列化到对方，取决于用户 */
    private long createTime;

    public RpcRequest() {
    }

    public RpcRequest(long sessionId, WorkerAddr srcAddr, WorkerAddr destAddr)
        : base(sessionId, srcAddr, destAddr) {
    }

    public long RequestId {
        get => requestId;
        set => requestId = value;
    }
    public int ServiceId {
        get => serviceId;
        set => serviceId = value;
    }
    public int MethodId {
        get => methodId;
        set => methodId = value;
    }
    public int InvokeType {
        get => invokeType;
        set => invokeType = value;
    }
    public long CreateTime {
        get => createTime;
        set => createTime = value;
    }

    public override string ToString() {
        return $"{base.ToString()}," +
               $" {nameof(requestId)}: {requestId}," +
               $" {nameof(serviceId)}: {serviceId}," +
               $" {nameof(methodId)}: {methodId}," +
               $" {nameof(invokeType)}: {invokeType}," +
               $" {nameof(createTime)}: {createTime}";
    }

    #region pool

    protected override void Reset() {
        base.Reset();
        requestId = -1;
        serviceId = 0;
        methodId = 0;

        invokeType = 0;
        createTime = 0;
    }

    private static readonly ConcurrentObjectPool<RpcRequest> POOL = new(
        () => new RpcRequest(), e => e.Reset(), FxUtils.RPC_POOL_SIZE);

    /** 该方法通常由Worker线程调用 */
    public static RpcRequest Acquire() {
        return POOL.Acquire();
    }

    /** 该方法通常由Router调用 */
    public static void Release(RpcRequest request) {
        POOL.Release(request);
    }

    #endregion
}
}