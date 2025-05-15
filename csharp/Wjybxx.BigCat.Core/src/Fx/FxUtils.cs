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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Wjybxx.Commons;
using Wjybxx.Commons.Concurrent;
using Wjybxx.Commons.Inject;

namespace Wjybxx.BigCat.Fx
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
        IInjector injector = builder.Injector;
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

    /** 导出Rpc方法信息 */
    public static void ExportMethodInfo(NodeBuilder builder) {
        RpcMethodRegistry registry = builder.Injector.GetInstance<RpcMethodRegistry>();
        foreach (Type pkg in builder.RpcPackages) {
            List<TypeInfo> rpcInterfaces = pkg.Assembly.DefinedTypes
                .Where(e => e.Namespace == pkg.Namespace)
                .Where(e => e.IsDefined(typeof(RpcServiceAttribute)))
                .ToList();
            foreach (TypeInfo serviceInterface in rpcInterfaces) {
                ExportMethodInfo(registry, serviceInterface);
            }
        }
    }

    public static void ExportMethodInfo(RpcMethodRegistry registry, TypeInfo serviceInterface) {
        RpcServiceAttribute serviceAnno = serviceInterface.GetCustomAttribute<RpcServiceAttribute>();
        if (serviceAnno == null) {
            throw new ArgumentException("target is not RpcService: " + serviceInterface);
        }
        try {
            MethodInfo[] methods = serviceInterface.GetMethods(); // 全部的public方法
            foreach (MethodInfo method in methods) {
                RpcMethodAttribute methodAnno = method.GetCustomAttribute<RpcMethodAttribute>();
                if (methodAnno == null) {
                    continue;
                }
                // 获取RpcContext的类型和方法参数类型
                Type ctxType;
                Type pType;
                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length > 0 && IsRpcContextType(parameters[0].ParameterType)) {
                    ctxType = parameters[0].ParameterType;
                    pType = parameters.Length > 1 ? parameters[1].ParameterType : null;
                } else {
                    ctxType = null;
                    pType = parameters.Length > 0 ? parameters[0].ParameterType : null;
                }
                // 返回值类型可能在Future和RpcContext的泛型参数中
                Type rType;
                if (ctxType != null) {
                    rType = ctxType.GenericTypeArguments[0]; // TypeArguments是类型实参
                } else {
                    rType = method.ReturnType;
                    if (IsFutureType(rType)) { // 声明类型可能是无泛型的Future
                        rType = rType.IsGenericType ? rType.GenericTypeArguments[0] : null;
                    }
                }
                // 注册方法
                RpcMethodInfo methodInfo = new RpcMethodInfo(
                    serviceInterface.Name, method.Name,
                    serviceAnno.ServiceId, methodAnno.MethodId,
                    pType, rType);
                registry.Register(methodInfo);
            }
        }
        catch (Exception e) {
            throw new Exception("service:" + serviceInterface.FullName, e);
        }
    }

    private static bool IsRpcContextType(Type type) {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(RpcContext<>);
    }

    private static bool IsFutureType(Type type) {
        if (!type.IsGenericType) {
            return type == typeof(ValueFuture) || type == typeof(Task)
                                               || type.GetInterface(typeof(IFuture).FullName!) != null;
        }
        type = type.GetGenericTypeDefinition();
        return type == typeof(ValueFuture<>) || type == typeof(Task<>)
                                             || type.GetInterface(typeof(IFuture<>).FullName!) != null;
    }
}
}