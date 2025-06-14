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
using Wjybxx.BigCat.Fx;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Concurrent;
using Wjybxx.Commons.Fx;
using Wjybxx.Commons.Inject;

namespace Wjybxx.BigCat.Unity
{
/// <summary>
/// UnityWorker是指没有自己的线程，由引擎调度的Worker。
///
/// <h3>场景的Update</h3>
/// 场景外的逻辑Module直接挂载在Worker上，而场景内的Module则需要看情况：
/// 1.如果想让场景内代码跑在服务器和本地的结果是一致的，那么我们应当设计一个Module来Update场景，并提供FixedUpdate、LateUpdate等逻辑。
/// 2.如果前后端代码是分离的，那么可以由Unity来驱动场景内的逻辑，我们就无需手动管理帧率。
///
/// <h3>驱动方式</h3>
/// 在Unity下，用户需要实现一个MonoBehavior来驱动该Worker和UI，以及场景的Update（分情况）。
/// UIMgr等需要外部驱动的特殊组件，可以实现为<see cref="EventLoopModule"/>，但将类型标记为<see cref="ComponentKind.Behavior"/>。
/// 
/// PS：该实现和<see cref="WorkerImpl"/>基本相同，只是超类不同。
/// </summary>
public class UnityWorker : UnityEventLoop<WorkerEvent>, Worker
{
    private readonly WorkerAddr workerAddr;
    private readonly IInjector injector;
    private volatile ISet<int> serviceIdSet = ImmutableSet<int>.Empty;
    private readonly WorkerControlData controlData;

    public UnityWorker(UnityWorkerBuilder builder)
        : base(decorate(builder), false) {
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

    private static UnityEventLoopBuilder<WorkerEvent> decorate(UnityWorkerBuilder builder) {
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