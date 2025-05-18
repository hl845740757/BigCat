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

#nullable enable
using System.Collections.Generic;
using Wjybxx.Commons.Attributes;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Concurrent;

namespace Wjybxx.BigCat.Fx
{
/// <summary>
/// 请求存根
/// 新实现下，Stub会拷贝Request的关键数据，以实现生命周期的分离，使得Request可以简单池化。
/// 该对象仅用于框架层，不可暴露给用户。
/// </summary>
[Internal]
public sealed class RpcRequestStub : IIndexedElement
{
#nullable disable
    public int qIndex = IIndexedElement.IndexNotFound;
    /// <summary>
    /// Call调用的Promise
    /// </summary>
    public ValuePromise<object> promise;
    /// <summary>
    /// <see cref="IValuePromise"/>的rid
    /// </summary>
    public int rid;
    /// <summary>
    /// 超时时间
    /// </summary>
    public long deadline;

    /** 连接id */
    public long sessionId;
    /** 目标地址 */
    public WorkerAddr destAddr;
    /** 请求id */
    public long requestId;
    /** 服务id */
    public int serviceId;
    /** 方法id */
    public int methodId;

    public RpcRequestStub() {
    }

    public int CollectionIndex(object collection) {
        return qIndex;
    }

    public void CollectionIndex(object collection, int index) {
        qIndex = index;
    }

    public void Reset() {
        qIndex = -1;
        promise = null;
        deadline = 0;

        sessionId = 0;
        destAddr = default;
        requestId = -1;
        serviceId = 0;
        methodId = 0;
    }

    private sealed class DefaultComparer : IComparer<RpcRequestStub>
    {
        public int Compare(RpcRequestStub lhs, RpcRequestStub rhs) {
            if (ReferenceEquals(lhs, rhs)) return 0;
            // if (ReferenceEquals(null, rhs)) return 1;
            // if (ReferenceEquals(null, lhs)) return -1;
            int r = lhs!.deadline.CompareTo(rhs!.deadline);
            if (r != 0) return r;
            //
            r = lhs.sessionId.CompareTo(rhs.sessionId);
            if (r != 0) return r;
            //
            return lhs.requestId.CompareTo(rhs.requestId);
        }
    }

    public static IComparer<RpcRequestStub> Comparer { get; } = new DefaultComparer();
}
}