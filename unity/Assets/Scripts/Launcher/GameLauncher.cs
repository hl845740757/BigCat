using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using Wjybxx.BigCat.Co;
using Wjybxx.BigCat.Fx;
using Wjybxx.BigCat.Gameplay;
using Wjybxx.BigCat.MVC;
using Wjybxx.BigCat.Tests;
using Wjybxx.BigCat.UI;
using Wjybxx.BigCat.Unity;
using Wjybxx.Commons.Concurrent;
using Wjybxx.Commons.Inject;
using Wjybxx.Disruptor;

namespace Wjybxx.BigCat.Launcher
{
/// <summary>
/// 
/// </summary>
public class GameLauncher : MonoBehaviour
{
    [NonSerialized] private Node node;
    [NonSerialized] private UnityWorker worker;
    [NonSerialized] private SceneMgr sceneMgr;

    [NonSerialized] private GameObject uiRoot;
    [NonSerialized] private WindowMgr windowMgr;
    [NonSerialized] private int lastFrame;

    private void Awake() {
        var nodeBuilder = new DefaultNodeBuilder()
        {
            NodeId = NodeId.MakeNodeId(1, 1),
            WorkerId = "Node",
            WaitStrategy = new TimeoutSleepingWaitStrategy(5, 2, 5),
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
                        WaitStrategy = new TimeoutSleepingWaitStrategy(5, 2, 5),
                        Injector = InjectorExtensions.CreateInjector(new WorkerInjectorConfig()),
                        ModuleClasses = { typeof(RpcClient) }
                    };
                }
                // 初始化rpc服务
                // if (index == 0) {
                //     workerBuilder.ModuleClasses.Add(typeof(RpcClientExample));
                //     workerBuilder.ServiceClasses.Add(typeof(RpcClientExample));
                // } else {
                //     workerBuilder.ModuleClasses.Add(typeof(RpcServiceExample));
                //     workerBuilder.ServiceClasses.Add(typeof(RpcServiceExample));
                // }
                // 覆盖默认值
                workerBuilder.Agent = workerBuilder.Injector.GetInstance<IEventLoopAgent<WorkerEvent>>();
                return workerBuilder.Build();
            },
        };
        nodeBuilder.Agent = nodeBuilder.Injector.GetInstance<IEventLoopAgent<WorkerEvent>>();
        node = (Node)nodeBuilder.Build();
        worker = (UnityWorker)node.MainWorker;
        // 发布Worker引用
        WorkerHolder workerHolder = worker.Injector.GetInstance<WorkerHolder>();
        workerHolder.Worker = worker;

        // 需要先启动Worker否则Join会死锁
        worker.Internal_Start();
        node.Start().Join();

        // 初始化场景管理器
        sceneMgr = worker.Injector.GetInstance<SceneMgr>();
        SceneMgr.Inst = sceneMgr;

        // 初始化UI管理器 - 配置类以后需要集中管理
        uiRoot = gameObject.scene.GetRootGameObjects().First(e => e.name == "UIRoot");
        WindowMgrCfg windowMgrCfg = new WindowMgrCfg
        {
            canvas = uiRoot.GetComponent<Canvas>()
        };
        windowMgr = new WindowMgr(workerHolder, windowMgrCfg);
        WindowMgr.Inst = windowMgr;
    }

    private void Start() {
        // 打开测试UI
        Transform child = uiRoot.transform.Find("LoginWindow");
        if (child) {
            windowMgr.Open("LoginWindow", child.gameObject, new WindowOpenArgs());
        }
    }

    private void FixedUpdate() {
        // UI和Scene的Update混合与否都可以
        if (lastFrame < Time.frameCount) {
            lastFrame = Time.frameCount;
            //
            sceneMgr.BeginOfFrame(Time.unscaledDeltaTime);
            windowMgr.BeginOfFrame(Time.unscaledDeltaTime);
            sceneMgr.EarlyUpdate();
            windowMgr.EarlyUpdate();
        }
        sceneMgr.FixedUpdate(Time.fixedDeltaTime);
    }

    private void Update() {
        worker.Internal_Update();
        //
        sceneMgr.Update();
        windowMgr.Update();
    }

    private void LateUpdate() {
        sceneMgr.LateUpdate();
        windowMgr.LateUpdate();
        //
        sceneMgr.EndOfFrame();
        windowMgr.EndOfFrame();
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
            binder.Bind<DefaultRpcProxyRegistry, RpcMethodRegistry>();
            binder.Bind<S2SSessionMgr>(); // 具体项目需要绑定子类

            // RPC组件
            binder.Bind<RpcSupport>();
            binder.Bind<TestRpcSerializer, RpcSerializer>();
            binder.Bind<TestRpcRouter>(InjectScope.Singleton, typeof(TestRpcRouter), typeof(RpcRouter)); // 具体子类也被引用
        }
    }

    private class WorkerInjectorConfig : IInjectModule
    {
        public void Configure(IInjectBinder binder) {
            binder.Bind<DefaultMainModule>(InjectScope.Singleton, typeof(IEventLoopAgent<WorkerEvent>));
            binder.Bind<TimeModule>();
            binder.Bind<S2SRpcClient, RpcClient>();
            binder.Bind<DefaultRpcProxyRegistry, RpcMethodRegistry>();
            binder.Bind<S2SSessionMgr>(); // 具体项目需要绑定子类

            binder.Bind<RpcClientExample>(); // worker1
            binder.Bind<RpcServiceExample>(); // worker2

            binder.Bind<WorkerHolder>(); // 发布Worker引用
            binder.Bind<SceneMgrCfg>(); // 场景管理器配置
            binder.Bind<SceneMgr>(); // 场景管理器
        }
    }
}
}