using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Wjybxx.BigCat.Fx;
using Wjybxx.BigCat.Gameplay;
using Wjybxx.BigCat.Tests;
using Wjybxx.BigCat.Unity;
using Wjybxx.Commons.Concurrent;
using Wjybxx.Commons.Inject;

namespace Wjybxx.BigCat.Launcher
{
/// <summary>
/// 
/// </summary>
public class GameLauncher : MonoBehaviour
{
    /// <summary>
    /// 挂载的模块
    /// 由于不能直接配置Type，因此我们配置Type的全限定名
    /// </summary>
    public List<string> moduleClasses = new();

    [NonSerialized] private Node node;
    [NonSerialized] private UnityWorker worker;
    [NonSerialized] private SceneMgr sceneMgr;

    private void Awake() {
        var nodeBuilder = new DefaultNodeBuilder()
        {
            NodeId = NodeId.MakeNodeId(1, 1),
            WorkerId = "Node",
            Injector = InjectorExtensions.CreateInjector(new NodeInjectorConfig()),
            // 初始化模块
            ModuleClasses =
            {
                typeof(RpcClient),
                typeof(RpcSupport),
                typeof(TestRpcRouter)
            },
            // Rpc接口包
            RpcPackages =
            {
                typeof(TestRpcRouter)
            },
            NumberChildren = 2,
            WorkerFactory = (parent, index, controlData) => {
                // 1号Worker在Unity线程，2号worker是后台线程
                // 初始化Worker，1号worker是client，2号是server，否则无法支持同步调用
                WorkerBuilder workerBuilder;
                if (index == 0) {
                    workerBuilder = new UnityWorkerBuilder(Thread.CurrentThread)
                    {
                        ManualClose = true, // 不由Node管理生命周期
                        WorkerId = "UnityWorker",
                        Parent = parent,
                        ControlData = controlData,
                        Injector = InjectorExtensions.CreateInjector(new WorkerInjectorConfig()),
                        ModuleClasses = { typeof(RpcClient) }
                    };
                } else {
                    workerBuilder = new DefaultWorkerBuilder()
                    {
                        WorkerId = "Worker-" + index,
                        Parent = parent,
                        ControlData = controlData,
                        Injector = InjectorExtensions.CreateInjector(new WorkerInjectorConfig()),
                        ModuleClasses = { typeof(RpcClient) }
                    };
                }
                // 初始化rpc服务
                if (index == 0) {
                    workerBuilder.ModuleClasses.Add(typeof(RpcClientExample));
                    workerBuilder.ServiceClasses.Add(typeof(RpcClientExample));
                } else {
                    workerBuilder.ModuleClasses.Add(typeof(RpcServiceExample));
                    workerBuilder.ServiceClasses.Add(typeof(RpcServiceExample));
                }
                // 覆盖默认值
                workerBuilder.Agent = workerBuilder.Injector.GetInstance<IEventLoopAgent<WorkerEvent>>();
                return workerBuilder.Build();
            },
        };
        nodeBuilder.Agent = nodeBuilder.Injector.GetInstance<IEventLoopAgent<WorkerEvent>>();
        node = (Node)nodeBuilder.Build();
        worker = (UnityWorker)node.MainWorker;

        // 需要先启动Worker否则Join会死锁
        worker.Internal_Start();
        node.Start().Join();
        
        sceneMgr = worker.Injector.GetInstance<SceneMgr>();
        SceneMgr.Inst = sceneMgr;
    }

    private void FixedUpdate() {
        sceneMgr.FixedUpdate(Time.fixedDeltaTime);
    }

    private void Update() {
        worker.Internal_Update();
        sceneMgr.BeginOfFrame(Time.unscaledDeltaTime);
        sceneMgr.EarlyUpdate();
        sceneMgr.Update();
    }

    private void LateUpdate() {
        sceneMgr.LateUpdate();
        sceneMgr.EndOfFrame();
    }

    private void OnDestroy() {
        worker.Internal_Stop();
    }

    private class NodeInjectorConfig : IInjectModule
    {
        public void Configure(IInjectBinder binder) {
            binder.Bind<DefaultMainModule>(InjectScope.Singleton, typeof(IEventLoopAgent<WorkerEvent>));
            binder.Bind<TimeModule>();
            binder.Bind<S2SRpcClient, RpcClient>();
            binder.Bind<DefaultRpcProxyRegistry, RpcProxyRegistry>();
            binder.Bind<S2SSessionMgr>(); // 具体项目需要绑定子类

            // RPC组件
            binder.Bind<RpcSupport>();
            binder.Bind<TestRpcSerializer, RpcSerializer>();
            binder.Bind<RpcMethodRegistry>();
            binder.Bind<TestRpcRouter>(InjectScope.Singleton, typeof(TestRpcRouter), typeof(RpcRouter)); // 具体子类也被引用
        }
    }

    private class WorkerInjectorConfig : IInjectModule
    {
        public void Configure(IInjectBinder binder) {
            binder.Bind<DefaultMainModule>(InjectScope.Singleton, typeof(IEventLoopAgent<WorkerEvent>));
            binder.Bind<TimeModule>();
            binder.Bind<S2SRpcClient, RpcClient>();
            binder.Bind<DefaultRpcProxyRegistry, RpcProxyRegistry>();
            binder.Bind<S2SSessionMgr>(); // 具体项目需要绑定子类

            binder.Bind<RpcClientExample>(); // worker1
            binder.Bind<RpcServiceExample>(); // worker2
            
            binder.Bind<SceneMgr>(); // 场景管理器
        }
    }
}
}