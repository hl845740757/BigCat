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

using System.Threading;
using NUnit.Framework;
using Wjybxx.BigCat.Fx;
using Wjybxx.Commons.Concurrent;
using Wjybxx.Commons.Inject;

namespace Wjybxx.BigCat.Tests
{
/// <summary>
/// 
/// </summary>
public class NodeTest
{
#nullable disable
    private static INode node;
#nullable restore

    [OneTimeSetUp]
    public void SetUp() {
        var nodeBuilder = new DefaultNodeBuilder()
        {
            NodeId = NodeId.MakeNodeId(1, 1),
            WorkerId = "Node",
            Injector = InjectorExtensions.CreateInjector(new NodeInjectorConfig()),
            // 初始化模块
            ModuleClasses =
            {
                typeof(IRpcClient),
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
                // 初始化Worker，1号worker是client，2号是server，否则无法支持同步调用
                DefaultWorkerBuilder workerBuilder = new DefaultWorkerBuilder()
                {
                    WorkerId = "Worker-" + index,
                    Parent = parent,
                    ControlData = controlData,
                    Injector = InjectorExtensions.CreateInjector(new WorkerInjectorConfig()),
                    ModuleClasses = { typeof(IRpcClient) }
                };
                // 初始化rpc服务
                if (index == 0) {
                    workerBuilder.ModuleClasses.Add(typeof(RpcClientExample));
                    workerBuilder.ServiceClasses.Add(typeof(RpcClientExample));
                } else {
                    workerBuilder.ModuleClasses.Add(typeof(RpcServiceExample));
                    workerBuilder.ServiceClasses.Add(typeof(RpcServiceExample));
                }
                return workerBuilder.Build();
            },
        };
        node = nodeBuilder.Build();
        node.Start().Join();
    }

    [OneTimeTearDown]
    public void TearDown() {
        if (node != null) {
            node.ShutdownNow();
            node.TerminationFuture.Join();
        }
    }

    [Test]
    public void Test() {
        // 查看日志
        Thread.Sleep(5 * 1000);
        node.Shutdown();
    }

    private class NodeInjectorConfig : IInjectModule
    {
        public void Configure(IInjectBinder binder) {
            binder.Bind<DefaultMainModule>(InjectScope.Singleton, typeof(IMainModule));
            binder.Bind<TimeModule>();
            binder.Bind<S2SRpcClient, IRpcClient>();
            binder.Bind<RpcMethodRegistry, IRpcMethodRegistry>();
            binder.Bind<S2SSessionMgr>(); // 具体项目需要绑定子类

            // RPC组件
            binder.Bind<RpcSupport>();
            binder.Bind<TestRpcSerializer, IRpcSerializer>();
            binder.Bind<TestRpcRouter>(InjectScope.Singleton, typeof(TestRpcRouter), typeof(IRpcRouter)); // 具体子类也被引用
        }
    }

    private class WorkerInjectorConfig : IInjectModule
    {
        public void Configure(IInjectBinder binder) {
            binder.Bind<DefaultMainModule>(InjectScope.Singleton, typeof(IMainModule));
            binder.Bind<TimeModule>();
            binder.Bind<S2SRpcClient, IRpcClient>();
            binder.Bind<RpcMethodRegistry, IRpcMethodRegistry>();
            binder.Bind<S2SSessionMgr>(); // 具体项目需要绑定子类

            binder.Bind<RpcClientExample>(); // worker1
            binder.Bind<RpcServiceExample>(); // worker2
        }
    }
}
}