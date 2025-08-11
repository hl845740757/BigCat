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
using Wjybxx.Dson.Codec;
using Wjybxx.Dson.Codec.Attributes;

namespace Wjybxx.BigCat.Fx
{
/// <summary>
/// Worker地址
/// PS：建议使用静态方法代替构造函数，提供更友好的API -- 或提供Utils类。
/// </summary>
public readonly struct WorkerAddr : IEquatable<WorkerAddr>
{
    /// <summary>
    /// NodeId，进程Id
    ///
    /// 1.服务器地址应当是一个稳定值，就像主机的hostname。
    /// 2.发送Rpc时通常精确指定
    /// 3.NodeId通常是可编码的，通常包含服务器类型和服务器id。
    /// 4.虽然int类型随扩展性有限，但对于游戏服务器而言足够。
    /// </summary>
    [DsonProperty(Name = "nid")]
    public readonly int nodeId;

    /// <summary>
    /// WorkerId，线程Id
    ///
    /// 1.线程id通常不具有稳定性，用户应尽可能避免线程id
    /// 2.发送Rpc时通常不指定
    /// </summary>
    [DsonProperty(Name = "wid")]
    public readonly string? workerId;

    public WorkerAddr(int nodeId, string? workerId) {
        this.nodeId = nodeId;
        this.workerId = workerId;
    }

    public WorkerAddr(IDsonObjectReader reader) {
        nodeId = reader.ReadInt("nid");
        workerId = reader.ReadString("wid");
    }

    #region equals

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(WorkerAddr other) {
        return nodeId == other.nodeId && workerId == other.workerId;
    }

    public override bool Equals(object? obj) {
        return obj is WorkerAddr other && Equals(other);
    }

    public override int GetHashCode() {
        return (nodeId * 397) ^ (workerId != null ? workerId.GetHashCode() : 0);
    }

    public static bool operator ==(WorkerAddr left, WorkerAddr right) {
        return left.Equals(right);
    }

    public static bool operator !=(WorkerAddr left, WorkerAddr right) {
        return !left.Equals(right);
    }

    #endregion

    public override string ToString() {
        return $"{nameof(nodeId)}: {nodeId}, {nameof(workerId)}: {workerId}";
    }
}
}