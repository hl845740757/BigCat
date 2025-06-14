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
using Wjybxx.Commons.Inject;

namespace Wjybxx.BigCat.Fx
{
/// <summary>
/// 基础的Worker实现
/// </summary>
public sealed class WorkerImpl : DisruptorEventLoop<WorkerEvent>, Worker
{
    private readonly WorkerAddr workerAddr;
    private readonly IInjector injector;
    private volatile ISet<int> serviceIdSet = ImmutableSet<int>.Empty;
    private readonly WorkerControlData controlData;

    public WorkerImpl(DefaultWorkerBuilder builder) : base(decorate(builder), false) {
        int nodeId = builder.Parent.NodeAddr.nodeId;
        string workerId = builder.WorkerId ?? throw new NullReferenceException("workerId");
        this.workerAddr = new WorkerAddr(nodeId, workerId);
        this.injector = builder.Injector ?? throw new NullReferenceException("injector");
        this.controlData = builder.ControlData;
        // 导出Rpc服务 -- 先注册到Registry但不对外发布
        FxUtils.ExportService(builder);

        // 构造完成后再初始化模块
        agent.Inject(this, ConsumerId);
    }

    private static DisruptorEventLoopBuilder<WorkerEvent> decorate(DefaultWorkerBuilder builder) {
        FxUtils.CreateModules(builder);
        if (builder.Agent == null) {
            builder.Agent = builder.Injector.GetInstance<IEventLoopAgent<WorkerEvent>>();
        }
        return builder.Delegated;
    }

    private void SetServiceIdSet(ICollection<int> serviceIdSet) {
        this.serviceIdSet = ImmutableSet<int>.CreateRange(serviceIdSet);
    }

    public WorkerAddr WorkerAddr => workerAddr;
    public IInjector Injector => injector;
    public ISet<int> Services => serviceIdSet;

    public WorkerControlData ControlData => controlData;
    public Node Node => (Node)base.Parent!;

#if NET6_0_OR_GREATER
    public override Node? Parent => (Node)base.Parent;

    public override Worker Select() {
        return this;
    }

    public override Worker Select(int key) {
        return this;
    }
#endif

    #region 生命周期

    protected override void OnStart() {
        FxUtils.CURRENT_WORKER.Value = this;

        agent.BeforeEventLoopStart();
        StartModules();
        ExportServices();
        agent.AfterEventLoopStart();
    }

    protected override void OnShutdown() {
        try {
            SetServiceIdSet(Array.Empty<int>());
            base.OnShutdown();
        }
        finally {
            FxUtils.CURRENT_WORKER.Value = null;
        }
    }

    private void ExportServices() {
        RpcProxyRegistry registry = injector.GetInstance<RpcProxyRegistry>();
        SetServiceIdSet(registry.Export());
    }

    #endregion
}
}