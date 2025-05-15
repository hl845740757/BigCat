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
using Wjybxx.Commons.Ex;
using Wjybxx.Commons.Pool;

namespace Wjybxx.BigCat.Fx
{
/// <summary>
/// 
/// </summary>
public sealed class RpcResponse : RpcProtocol
{
    /** 请求的唯一id */
    private long requestId;
    /** 服务id -- 用于网络线程定位返回值类型，也可以用于校验和日志记录 */
    private int serviceId;
    /** 方法id */
    private int methodId;

    /**
     * 错误码（0表示成功） -- 不使用枚举，以方便用户扩展
     * 如果调用成功，result为对应的结果。
     * 如果调用失败，result为错误信息，固定为字符串类型。
     */
    private int errorCode;

    public RpcResponse() {
    }

    public RpcResponse(long conId, WorkerAddr srcAddr, WorkerAddr destAddr)
        : base(conId, srcAddr, destAddr) {
    }

    #region logic

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetSuccess(object? result) {
        errorCode = RpcErrorCodes.SUCCESS;
        data = result;
    }

    /** 设置为失败，会自动标记为可共享 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetFailed(int errorCode, string? msg) {
        if (errorCode < 1) {
            throw new ArgumentException("errorCode: " + errorCode);
        }
        this.errorCode = errorCode;
        this.data = msg;
        sharable = true;
    }

    /** 结果转String，只有失败的情况下可调用 */
    public string? ErrorMsg {
        get {
            if (errorCode == 0) {
                throw new IllegalStateException("errorCode == 0");
            }
            return (string)data;
        }
    }

    /** 是否成功 */
    public bool IsSucceeded => errorCode == 0;

    /** 是否失败 */
    public bool IsFailed => errorCode != 0;

    #endregion

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
    public int ErrorCode {
        get => errorCode;
        set => errorCode = value;
    }

    public override string ToString() {
        return $"{base.ToString()}," +
               $" {nameof(requestId)}: {requestId}," +
               $" {nameof(serviceId)}: {serviceId}," +
               $" {nameof(methodId)}: {methodId}," +
               $" {nameof(errorCode)}: {errorCode}";
    }

    #region pool

    protected override void Reset() {
        base.Reset();
        requestId = -1;
        serviceId = 0;
        methodId = 0;
        errorCode = 0;
    }

    private static readonly ConcurrentObjectPool<RpcResponse> POOL = new(
        () => new RpcResponse(), e => e.Reset(), FxUtils.RPC_POOL_SIZE);

    /** 该方法通常由Router调用 */
    public static RpcResponse Acquire() {
        return POOL.Acquire();
    }

    /** 该方法通常由Worker线程调用 */
    public static void Release(RpcResponse response) {
        POOL.Release(response);
    }

    #endregion
}
}