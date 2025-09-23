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
/// 
/// </summary>
public abstract class RpcProtocol
{
#nullable disable
    /// <summary>
    /// 连接id -- 服务器间通信时，condId由连接的发起方生成即可
    /// </summary>
    protected long sessionId;
    /// <summary>
    /// 发送方地址 -- 服务器通信使用
    /// </summary>
    protected WorkerAddr srcAddr;
    /// <summary>
    /// 接收方地址 -- 服务器通信使用
    /// </summary>
    protected WorkerAddr destAddr;

    /// <summary>
    /// 方法参数或结果
    /// 
    /// 1.可能情况：null,string,bytes,结构
    /// 2.bytes 表示已经序列化；TODO 改为Bytebuf
    /// 3.null 表示无参数和结果，或是用户的参数和结果为null；在写入最终协议时需要区分
    /// 4.string 表示错误信息
    /// 5.结构 表示正常的参数和结果
    /// </summary>
    protected object data;
    /// <summary>
    /// 方法参数或结果是否可共享
    /// </summary>
    [NonSerialized] protected bool sharable;

    protected RpcProtocol() {
    }

    protected RpcProtocol(long sessionId, WorkerAddr srcAddr, WorkerAddr destAddr) {
        this.sessionId = sessionId;
        this.srcAddr = srcAddr;
        this.destAddr = destAddr;
    }

    #region internal

    /// <summary>
    /// 数据部分是否是null或bytes -- 不需要序列化或已经序列化
    /// </summary>
    public bool IsNullOrBytes => data == null || data is byte[];

    /// <summary>
    /// 数据部分是否是bytes -- 需要反序列化
    /// </summary>
    public bool IsBytes => data is byte[];

    /** data转bytes返回 */
    public byte[] GetBytes() {
        return (byte[])data;
    }

    protected virtual void Reset() {
        sessionId = 0;
        srcAddr = default;
        destAddr = default;
        data = null;
        sharable = false;
    }

    #endregion

    public long SessionId {
        get => sessionId;
        set => sessionId = value;
    }
    public WorkerAddr SrcAddr {
        get => srcAddr;
        set => srcAddr = value;
    }
    public WorkerAddr DestAddr {
        get => destAddr;
        set => destAddr = value;
    }
    public object Data {
        get => data;
        set => data = value;
    }
    public bool Sharable {
        get => sharable;
        set => sharable = value;
    }

    protected string DataToString() {
        if (data == null) {
            return "null";
        }
        return data.ToString()!;
    }

    public override string ToString() {
        return $"{nameof(sessionId)}: {sessionId}," +
               $" {nameof(srcAddr)}: {srcAddr}," +
               $" {nameof(destAddr)}: {destAddr}," +
               $" {nameof(data)}: {data}," +
               $" {nameof(sharable)}: {sharable}";
    }
}
}