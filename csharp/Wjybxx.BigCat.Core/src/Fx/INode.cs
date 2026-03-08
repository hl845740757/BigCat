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
using Wjybxx.Commons.Concurrent;

namespace Wjybxx.BigCat.Fx
{
/// <summary>
/// Node表示分布式中的一个节点，是分布式架构下的成员 -- 也就是游戏架构中“服”的概念。
/// Node是一个IO线程，主要负责线程间和分布式进程间的Rpc通信。
/// Node是特殊的Worker，也支持挂载模块和服务，它挂载的模块称之为路由模块，它挂载的服务称之为路由服务。
/// Node是Worker的管理者，也是Worker在网络中的门面。
///
/// <h3>Select接口</h3>
/// <see cref="IExecutor.Execute(Action, int)"/>提交的任务都直接进入Node的任务队列，
/// 如果想提交到Worker，可通过<see cref="IEventLoopGroup.Select"/>提交到指定的Worker。
/// 
/// <h3>模块管理</h3>
/// 1.同Worker一样，Node也通过挂载模块（Module）扩展，
/// 2.Node的业务应当保持简单，勿在Node上挂在非IO模块。
/// 3.为保持架构的简单性，我们不支持Node在运行时添加Worker.
/// 4.Node不可以同步调用Worker上的服务，否则会导致死锁（超时）。
///
/// <h3>服务导出</h3>
/// 1. 当暴露服务到网络时，只能暴露服务支持的并发数，而不能暴露服务关联的Worker。
/// 2. Rpc客户端不能指定服务由哪个Worker处理 -- 避免不必要的依赖。
/// </summary>
public interface INode : IWorker
{
    /// <summary>
    /// node的Rpc地址 -- workerId为null；
    /// </summary>
    WorkerAddr NodeAddr { get; }

    /// <summary>
    /// 服务id -> 存在对应服务的Worker -- 限本地使用
    /// </summary>
    /// <returns></returns>
    IDictionary<int, ServiceInfo> ServiceInfoMap { get; }

    #region worker管理

    /** Node挂载的所有Worker */
    IList<IWorker> Workers { get; }

    /** Node挂载的第一个Worker */
    IWorker MainWorker { get; }

    /** 根据Worker的名字查找Worker，不存在则返回null */
    IWorker? FindWorker(string workerId);

    #endregion

    #region 接口适配

    /// <summary>
    /// Node总是返回自己
    /// </summary>
    INode IWorker.Node => this;

    /// <summary>
    /// Node没有Parent
    /// </summary>
    IEventLoopGroup? IEventLoop.Parent => null;

    #endregion

    #region global

#nullable disable

    /// <summary>
    /// 获取当前线程关联的Node
    ///
    /// 注：正常业务不应该通过该字段获取引用。
    /// </summary>
    public static INode CurrentNode {
        get {
            IWorker worker = CurrentWorker;
            return worker != null ? worker.Node : null;
        }
    }

    /// <summary>
    /// 当前进程上所有的Node
    /// </summary>
    public static List<INode> CurrentNodes => new(FxUtils.CURRENT_NODES.Keys);

    #endregion
}
}