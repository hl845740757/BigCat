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
using System.Runtime.CompilerServices;
using Wjybxx.Commons.Attributes;

namespace Wjybxx.BigCat.Fx
{
/// <summary>
/// Rpc方法注册表
/// (每个Worker一个)
/// </summary>
public interface IRpcMethodRegistry
{
    /// <summary>
    /// 注册一个RPC方法信息
    /// (通常只需要Node线程注册)
    /// </summary>
    /// <param name="methodInfo"></param>
    void Register(RpcMethodInfo methodInfo);

    /// <summary>
    /// 获取Rpc方法信息
    /// </summary>
    /// <param name="serviceId">服务id</param>
    /// <param name="methodId">方法id</param>
    /// <returns></returns>
    RpcMethodInfo GetMethodInfo(int serviceId, int methodId);

    /// <summary>
    /// 注册一个rpc方法代理
    /// 重复添加时会抛出异常
    /// </summary>
    /// <param name="serviceId">服务id</param>
    /// <param name="methodId">方法id</param>
    /// <param name="proxy">代理方法</param>
    [StableName]
    void Register<T>(int serviceId, int methodId, RpcMethodProxy<T> proxy);

    /// <summary>
    /// 设置代理的切面数据
    /// 由于一个方法只能由一个proxy，因此切面数据可以独立注册
    /// </summary>
    /// <param name="serviceId">服务id</param>
    /// <param name="methodId">方法id</param>
    /// <param name="customData">自定义切面数据</param>
    [StableName]
    void SetProxyData(int serviceId, int methodId, object? customData);

    /// <summary>
    /// 获取设置的代理数据 -- 切面数据
    /// </summary>
    /// <param name="serviceId">服务id</param>
    /// <param name="methodId">方法id</param>
    /// <returns></returns>
    object? GetProxyData(int serviceId, int methodId);

    /// <summary>
    /// 注册一个rpc方法代理
    /// </summary>
    /// <param name="serviceId">服务id</param>
    /// <param name="methodId">方法id</param>
    /// <param name="proxy">代理方法</param>
    /// <param name="customData"></param>
    [StableName]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void Register<T>(int serviceId, int methodId, RpcMethodProxy<T> proxy,
                     object? customData) {
        Register(serviceId, methodId, proxy);
        SetProxyData(serviceId, methodId, customData);
    }

    /// <summary>
    /// 查询方法绑定的Proxy
    /// </summary>
    /// <param name="serviceId">服务id</param>
    /// <param name="methodId">方法id</param>
    /// <returns>如果不存在，则返回默认的proxy</returns>
    Delegate? GetProxy(int serviceId, int methodId);

    /// <summary>
    /// 删除指定方法的Proxy
    /// 在删除后可重新注册，通常用于覆盖特定方法的proxy
    /// </summary>
    /// <param name="serviceId">服务id</param>
    /// <param name="methodId">方法id</param>
    /// <returns></returns>
    Delegate? RemoveProxy(int serviceId, int methodId);

    /// <summary>
    /// 获取Rpc调用器
    /// C#端特殊支持，用于避免装箱
    /// </summary>
    /// <param name="serviceId"></param>
    /// <param name="methodId"></param>
    /// <returns></returns>
    RpcMethodInvoker? GetInvoker(int serviceId, int methodId);

    /// <summary>
    /// 临时禁用服务（用于线上临时关闭功能）
    /// </summary>
    /// <param name="serviceId">服务id</param>
    /// <param name="methodId">方法id，-1表示全部</param>
    void Disable(int serviceId, int methodId);

    /// <summary>
    /// 临时启用服务
    /// </summary>
    /// <param name="serviceId">服务id</param>
    /// <param name="methodId">方法id，-1表示全部</param>
    void Enable(int serviceId, int methodId);

    /// <summary>
    /// 查询方法是否被禁用
    /// </summary>
    /// <param name="serviceId"></param>
    /// <param name="methodId"></param>
    /// <returns></returns>
    bool IsDisabled(int serviceId, int methodId);

    /// <summary>
    /// 导出注册表中包含的服务
    /// </summary>
    /// <returns>注册的所有服务的id</returns>
    HashSet<int> Export();

    /// <summary>
    /// 清理注册表
    /// 当不再使用<see cref="IRpcMethodRegistry"/>时，执行该方法可释放<see cref="RpcMethodProxy{T}"/>捕获的对象。
    /// </summary>
    void Clear();
}
}