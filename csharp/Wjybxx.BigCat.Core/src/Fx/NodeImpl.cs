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
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Concurrent;
using Wjybxx.Commons.Inject;

namespace Wjybxx.BigCat.Fx
{
/// <summary>
/// 默认的Node实现
/// </summary>
public class NodeImpl : DisruptorEventLoop<WorkerEvent>, Node
{
    private readonly IInjector injector;
    private readonly WorkerAddr workerAddr;
    private readonly WorkerAddr nodeAddr;

    private readonly Worker[] children;
    private readonly ImmutableList<Worker> readonlyChildren;
    private readonly IEventLoopChooser chooser;
    private readonly WorkerControlData controlData = new WorkerControlData();

    /** Node自身的服务信息 */
    private volatile ISet<int> serviceIdSet = ImmutableLinkedHastSet<int>.Empty;
    /** Node+Worker的服务信息 */
    private volatile IDictionary<int, ServiceInfo> serviceInfoMap = ImmutableLinkedDictionary<int, ServiceInfo>.Empty;

    public NodeImpl(DefaultNodeBuilder builder)
        : base(Decorate(builder)) {
        string workerId = builder.WorkerId ?? throw new NullReferenceException("workerId");
        int nodeId = builder.NodeId;
        this.workerAddr = new WorkerAddr(nodeId, workerId);
        this.nodeAddr = new WorkerAddr(nodeId, null);
        this.injector = builder.Injector ?? throw new NullReferenceException("injector");
        // 导出Rpc服务 -- 先注册到Registry但不对外发布
        FxUtils.ExportService(builder);
        FxUtils.ExportMethodInfo(builder);

        int numberChildren = builder.NumberChildren;
        if (numberChildren < 1) {
            throw new ArgumentException("numberChildren must greater than 0");
        }
        WorkerFactory workerFactory = builder.WorkerFactory;
        if (workerFactory == null) {
            throw new NullReferenceException("workerFactory");
        }
        EventLoopChooserFactory chooserFactory = builder.ChooserFactory;
        if (chooserFactory == null) {
            chooserFactory = new EventLoopChooserFactory();
        }
        children = new Worker[numberChildren];
        for (int idx = 0; idx < numberChildren; idx++) {
            WorkerControlData controlData = new WorkerControlData();
            Worker eventLoop = workerFactory(this, idx, controlData);
            if (eventLoop.Parent != this) throw new IllegalStateException("the parent of worker is illegal");
            if (eventLoop.ControlData != controlData)
                throw new IllegalStateException("the controlData of worker is illegal");
            if (builder.ManualClose != null) controlData.manualClose = builder.ManualClose;
            children[idx] = eventLoop;
        }
        readonlyChildren = children.ToImmutableList2();
        chooser = chooserFactory.NewChooser(children);
    }

    private static DisruptorEventLoopBuilder<WorkerEvent> Decorate(DefaultNodeBuilder builder) {
        if (builder.Agent == null) {
            builder.Agent = builder.Injector.GetInstance<IEventLoopAgent<WorkerEvent>>();
        }
        return builder.Delegated;
    }

    private void SetServiceIdSet(ICollection<int> serviceIdSet) {
        this.serviceIdSet = ImmutableLinkedHastSet<int>.CreateRange(serviceIdSet);
    }

    private void SetServiceInfoMap(Dictionary<int, ServiceInfo> serviceInfoMap) {
        Dictionary<int, ServiceInfo> tempMap = new(serviceInfoMap.Count);
        foreach (ServiceInfo serviceInfo in serviceInfoMap.Values) {
            tempMap[serviceInfo.serviceId] = serviceInfo.ToImmutable(); // 转不可变
        }
        this.serviceInfoMap = tempMap.ToImmutableLinkedDictionary();
    }

    // -----------------------
    public IInjector Injector => injector;
    public WorkerAddr WorkerAddr => workerAddr;
    public WorkerAddr NodeAddr => nodeAddr;
    public ISet<int> Services => serviceIdSet;
    public IDictionary<int, ServiceInfo> ServiceInfoMap => serviceInfoMap;

    public IList<Worker> Workers => readonlyChildren;
    public Worker MainWorker => children[0];

    public Worker? FindWorker(string workerId) {
        foreach (Worker worker in children) {
            if (workerId == worker.WorkerAddr.workerId) {
                return worker;
            }
        }
        // 可能是查找自己
        if (workerId == workerAddr.workerId) {
            return this;
        }
        return null;
    }

    // -----------------
    public WorkerControlData ControlData => controlData;
    public Node Node => this;
    public override IEventLoopGroup? Parent => null;

#if NET6_0_OR_GREATER
    public override Worker Select() {
        return (Worker)chooser.Select();
    }

    public override Worker Select(int key) {
        return (Worker)chooser.Select(key);
    }
#else
    public override IEventLoop Select() {
        return chooser.Select();
    }

    public override IEventLoop Select(int key) {
        return chooser.Select(key);
    }
#endif

    #region 生命流程

    protected override void OnStart() {
        FxUtils.CURRENT_WORKER.Value = (this);
        FxUtils.CURRENT_NODES[this] = true;
        InitControlData();

        agent.BeforeEventLoopStart();
        StartModules(); // 先启动自己的模块和服务，Worker可能需要使用
        ExportServices(Array.Empty<Worker>());

        StartWorkers();
        ExportServices(readonlyChildren); // 再重新导出所有的服务
        agent.AfterEventLoopStart();
    }

    protected override void OnShutdown() {
        agent.BeforeEventLoopShutdown();
        try {
            StopWorkers(); // 先停止worker，再停止自己的模块和服务
            SetServiceIdSet(Array.Empty<int>());
            SetServiceInfoMap(new Dictionary<int, ServiceInfo>());

            StopModules();
            DestroyModules();
            agent.AfterEventLoopShutdown();
        }
        finally {
            FxUtils.CURRENT_WORKER.Value = null;
            FxUtils.CURRENT_NODES.TryRemove(this, out bool _);
        }
    }

    private void InitControlData() {
        controlData.Init(this, false);
        foreach (Worker worker in children) {
            worker.ControlData.Init(worker, worker.State == EventLoopState.Unstarted);
        }
    }

    private void ExportServices(IList<Worker> workers) {
        // Node自身的服务
        HashSet<int> nodeServiceIdSet = injector.GetInstance<RpcProxyRegistry>().Export();
        SetServiceIdSet(nodeServiceIdSet);

        Dictionary<int, ServiceInfo> serviceInfoMap = new();
        // Node自身的服务
        foreach (int serviceId in nodeServiceIdSet) {
            serviceInfoMap[serviceId] = new ServiceInfo(serviceId, ImmutableList<Worker>.Create(this));
        }
        // 添加Worker上的服务 -- Worker不可包含Node同名服务
        foreach (Worker worker in workers) {
            foreach (int serviceId in worker.Services) {
                if (nodeServiceIdSet.Contains(serviceId)) {
                    throw new IllegalStateException("The service in the worker conflicts with the service in the node, id " + serviceId);
                }
                if (!serviceInfoMap.TryGetValue(serviceId, out ServiceInfo serviceInfo)) {
                    serviceInfo = new ServiceInfo(serviceId, new List<Worker>(2));
                    serviceInfoMap[serviceId] = serviceInfo;
                }
                serviceInfo.AddWorker(worker);
            }
        }
    }

    private void StartWorkers() {
        FutureCombiner combiner = ExecutorUtil.NewCombiner();
        foreach (Worker child in children) {
            combiner.Add(child.Start());
        }
        combiner.SelectAll().Join();
    }

    private void StopWorkers() {
        FutureCombiner combiner = ExecutorUtil.NewCombiner();
        // 逆序关闭 -- 可能存在时序依赖
        for (int i = children.Length - 1; i >= 0; i--) {
            Worker child = children[i];
            if (child.ControlData.IsManualClose) continue;
            child.Shutdown();
            combiner.Add(child.TerminationFuture);
        }
        IPromise<object> aggregateFuture = combiner.SelectAll(true);
        if (aggregateFuture.AwaitUninterruptibly(TimeSpan.FromMinutes(1))) {
            return;
        }
        // 进入快速关闭阶段
        for (int i = children.Length - 1; i >= 0; i--) {
            Worker child = children[i];
            if (child.ControlData.IsManualClose) continue;
            child.ShutdownNow();
        }
        aggregateFuture.Join();
    }

    #endregion
}
}