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
using System.Collections.Generic;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Concurrent;

namespace Wjybxx.BigCat.Fx
{
public abstract class NodeBuilder : WorkerBuilder
{
#nullable disable
    private int numberChildren = 1;
    private WorkerFactory workerFactory;
    private EventLoopChooserFactory chooserFactory;

    /** 服务器节点id */
    private int nodeId;
    /// <summary>
    /// Rpc接口所在的包，用于生成<see cref="IRpcMethodRegistry"/>
    /// 注：通过Type定位指定的程序集和命名空间，以避免手动逐个添加。
    /// </summary>
    private readonly HashSet<Type> rpcPackages = new();

    protected NodeBuilder(EventLoopBuilder<WorkerEvent> delegated)
        : base(delegated) {
        WorkerId = "Node";
    }

#if NET6_0_OR_GREATER
    public abstract override INode Build();
#endif

    public int NumberChildren {
        get => numberChildren;
        set => numberChildren = value;
    }
    public WorkerFactory WorkerFactory {
        get => workerFactory;
        set => workerFactory = value;
    }
    public EventLoopChooserFactory ChooserFactory {
        get => chooserFactory;
        set => chooserFactory = value;
    }
    public int NodeId {
        get => nodeId;
        set => nodeId = value;
    }

    public HashSet<Type> RpcPackages => rpcPackages;

    /// <summary>
    /// 添加一个Rpc包
    /// </summary>
    /// <param name="pkg"></param>
    /// <returns></returns>
    public NodeBuilder AddRpcPackage(Type pkg) {
        rpcPackages.Add(pkg);
        return this;
    }

    public NodeBuilder AddRpcPackages(List<Type> packages) {
        rpcPackages.AddAll(packages);
        return this;
    }
}
}