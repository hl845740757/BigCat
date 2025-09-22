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
using Wjybxx.Commons.Inject;

namespace Wjybxx.BigCat.Fx
{
public abstract class WorkerBuilder
{
#nullable disable
    private string workerId;
    //
    /// <summary>
    /// Worker上绑定的容器，需要包含：
    /// <see cref="IEventLoopAgent{T}"/>、<see cref="TimeModule"/>
    /// <see cref="RpcClient"/>、<see cref="RpcMethodRegistry"/>、
    /// <see cref="S2SSessionMgr"/>
    ///
    /// 如果是Node，则还需要包含：
    /// <see cref="RpcSupport"/>、<see cref="RpcRouter"/>、<see cref="RpcSerializer"/>、
    /// </summary>
    private IInjector injector;

    /// <summary>
    /// Worker上挂载的模块类
    /// 1.需要能通过<see cref="injector"/>获取实例
    /// 2.添加顺序很重要，Worker将按照添加顺序启动所有的Module
    /// 3.实现类必须是<see cref="EventLoopModule"/>的子类（注入的接口则不一定）
    /// </summary>
    private readonly List<Type> moduleClasses = new();
    /// <summary>
    /// Worker上挂载的服务类
    /// 1.服务接口的实例必须在容器中存在
    /// 2.服务会自动导出
    /// </summary>
    private readonly List<Type> serviceClasses = new();

    /// <summary>
    /// Worker的控制数据
    /// 在真正构建时由<see cref="Node"/>赋值，用户需要设置到parent上
    /// </summary>
    private WorkerControlData controlData;
    /// <summary>
    /// 是否手动关闭Worker -- 如果未赋值，则取决于添加到Node时是否已启动 
    /// </summary>
    private bool? manualClose;
    /// <summary>
    /// 最终的EventLoop构建器
    /// Builder之间不方便继承
    /// </summary>
    protected readonly EventLoopBuilder<WorkerEvent> delegated;

    public WorkerBuilder(EventLoopBuilder<WorkerEvent> delegated) {
        this.delegated = delegated ?? throw new ArgumentNullException(nameof(delegated));
    }

    public EventLoopBuilder<WorkerEvent> Delegated => delegated;

    public abstract Worker Build();

    #region worker

    public string WorkerId {
        get => workerId;
        set => workerId = value;
    }

    public IInjector Injector {
        get => injector;
        set => injector = value;
    }

    public WorkerControlData ControlData {
        get => controlData;
        set => controlData = value;
    }

    public bool? ManualClose {
        get => manualClose;
        set => manualClose = value;
    }

    public List<Type> ModuleClasses => moduleClasses;

    public List<Type> ServiceClasses => serviceClasses;

    public void AddModule(Type moduleClazz) {
        moduleClasses.Add(moduleClazz);
    }

    public WorkerBuilder AddModules(IEnumerable<Type> moduleClazz) {
        moduleClasses.AddRange(moduleClazz);
        return this;
    }

    public WorkerBuilder AddService(Type serviceClass) {
        serviceClasses.Add(serviceClass);
        return this;
    }

    public WorkerBuilder AddServices(IEnumerable<Type> serviceClass) {
        serviceClasses.AddRange(serviceClass);
        return this;
    }

    #endregion

    #region delegated

    public Node Parent {
        get => (Node)delegated.Parent;
        set => delegated.Parent = value;
    }
    public int Index {
        get => delegated.Index;
        set => delegated.Index = value;
    }
    public RejectedExecutionHandler RejectedExecutionHandler {
        get => delegated.RejectedExecutionHandler;
        set => delegated.RejectedExecutionHandler = value;
    }
    public ThreadFactory ThreadFactory {
        get => delegated.ThreadFactory;
        set => delegated.ThreadFactory = value;
    }
    public IEventLoopAgent<WorkerEvent> Agent {
        get => delegated.Agent;
        set => delegated.Agent = value;
    }
    public int BatchSize {
        get => delegated.BatchSize;
        set => delegated.BatchSize = value;
    }

    #endregion
}
}