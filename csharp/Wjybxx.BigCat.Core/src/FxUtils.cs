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
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using Wjybxx.Commons;

namespace Wjybxx.BigCat.Core
{
/// <summary>
/// 框架工具类(允许unity项目扩展)
/// </summary>
public static class FxUtils
{
    /// <summary>
    /// 当前线程运行的Worker，Node也会发布到这里
    /// </summary>
    public static readonly ThreadLocal<Worker> CURRENT_WORKER = new();

    /// <summary>
    /// 当前运行中的所有Node -- 用于未来支持单进程下启动多个服务器。
    /// 
    /// 经过反复地思考权衡，允许一个进程内启动多个Node是简单可靠的方式，代价是增加一部分开销 -- 不会太多。
    /// 如果在一个Node内启动多个服务器，虽然资源利用率更高，但编程复杂，尤其对Rpc客户端不友好。
    /// 如果需要查询当前线程的Node，可通过Worker查询。
    ///
    /// C#居然连CopyOnWrite集合都没有...
    /// </summary>
    public static readonly ConcurrentDictionary<Node, bool> CURRENT_NODES = new();
    /// <summary>
    /// Rpc对象池大小
    /// </summary>
    public static readonly int RPC_POOL_SIZE = EnvironmentUtil.GetIntVar("Wjybxx.BigCat.Fx.RpcPoolSize", 1024);

    /** worker发到node的rpc请求 - 发送，包含request，promise,rid */
    public const int TYPE_WORKER_NODE_REQUEST = 1;
    /** worker发到node的rpc响应 - 发送，包含Response */
    public const int TYPE_WORKER_NODE_RESPONSE = 2;

    /** 收到网络层的Request - 接收 */
    public const int TYPE_NET_NODE_REQUEST = 3;
    /** 收到网络层的Response - 接收 */
    public const int TYPE_NET_NODE_RESPONSE = 4;

    /** node发到worker的rpc请求 - 派发请求，包含request */
    public const int TYPE_NODE_WORKER_REQUEST = 5;
    /** node发到worker的rpc结果 - 设置Promise，包含Response */
    public const int TYPE_NODE_WORKER_RESPONSE = 6;

    /// <summary>
    /// 导出服务到
    /// </summary>
    public static void ExportService(WorkerBuilder builder) {
        Injector injector = builder.Injector;
        RpcProxyRegistry registry = injector.GetInstance<RpcProxyRegistry>();
        foreach (Type clazz in builder.ServiceClasses) {
            object instance = injector.GetInstance(clazz);
            ExportService(registry, clazz, instance);
        }
    }

    /** 导出Rpc服务 */
    public static void ExportService(RpcProxyRegistry registry, Type serviceInterface, object serviceImpl) {
        if (!serviceInterface.IsInstanceOfType(serviceImpl)) {
            throw new ArgumentException($"interface: {serviceInterface}, impl: {serviceImpl.GetType()}");
        }
        // public static void export(RpcProxyRegistry registry, RpcServiceExample instance) {}
        // Exporter默认在同命名空间下
        Type exporter = serviceInterface.Assembly.GetType(serviceInterface.FullName + "Exporter");
        if (exporter == null) {
            throw new Exception("Exporter is absent, service:" + serviceInterface);
        }
        try {
            MethodInfo? methodInfo = exporter.GetMethod("Export", BindingFlags.Public | BindingFlags.Static);
            if (methodInfo == null) {
                throw new AssertionError();
            }
            methodInfo.Invoke(null, new[] { registry, serviceImpl });
        }
        catch (Exception e) {
            throw new Exception("service:" + serviceInterface, e);
        }
    }
}
}