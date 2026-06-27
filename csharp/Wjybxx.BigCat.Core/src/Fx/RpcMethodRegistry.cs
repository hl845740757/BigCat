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

namespace Wjybxx.BigCat.Fx
{
/// <summary>
/// 默认的Rpc方法注册表实现
///
/// <see cref="RpcMethodInfo"/>通常由主线程进行注册，IO线程查询使用，
/// 为保证线程可见性和安全性，主线程在注册完成之后需调用<see cref="MakeImmutable"/>将registry变更为不可变状态（注册完成），
/// IO线程在启动时可调用<see cref="EnsureImmutable"/>检查registry的状态。
/// (另一种方案是通过线程的启动顺序来保证可见性，同时后续禁止修改。)
/// </summary>
public class RpcMethodRegistry : IRpcMethodRegistry
{
    private readonly Dictionary<int, RpcMethodInfo> methodInfoMap = new(512);
    private readonly Dictionary<int, Delegate> proxyMap = new(512);
    private readonly Dictionary<int, RpcMethodInvoker> invokerMap = new(512);
    private readonly Dictionary<int, bool> disabledMap = new();
    private readonly Dictionary<int, object?> proxyDataMap = new(512);
    private volatile bool mutable = true;

    /// <summary>
    /// 注册rpc方法
    /// </summary>
    /// <param name="methodInfo"></param>
    /// <exception cref="IllegalStateException"></exception>
    public void Register(RpcMethodInfo methodInfo) {
        if (!mutable) {
            throw new IllegalStateException("registry is immutable");
        }
        int methodKey = RpcMethodKey.MethodKey(methodInfo.serviceId, methodInfo.methodId);
        if (!methodInfoMap.TryGetValue(methodKey, out RpcMethodInfo exist)) {
            methodInfoMap[methodKey] = methodInfo;
        } else if (exist != methodInfo) {
            // 同一个方法被重复注入是安全的，主要处理继承来的方法...
            throw new IllegalStateException($"methodKey: {methodInfo.serviceId}-{methodInfo.methodId}");
        }
    }

    /// <summary>
    /// 查询方法信息
    /// </summary>
    /// <param name="serviceId">服务id</param>
    /// <param name="methodId">方法id</param>
    /// <returns>如果方法不存在，则返回null</returns>
    public RpcMethodInfo? GetMethodInfo(int serviceId, int methodId) {
        int methodKey = RpcMethodKey.MethodKey(serviceId, methodId);
        return methodInfoMap.TryGetValue(methodKey, out RpcMethodInfo exist) ? exist : null;
    }

    public void Register<T>(int serviceId, int methodId, RpcMethodProxy<T> proxy) {
        if (proxy == null) throw new ArgumentNullException(nameof(proxy));
        int methodKey = RpcMethodKey.MethodKey(serviceId, methodId);
        if (!proxyMap.TryAdd(methodKey, proxy)) {
            throw new ArgumentException($"methodKey is duplicate, serviceId: {serviceId}, methodId: {methodId}");
        }
        invokerMap[methodKey] = new RpcMethodInvoker<T>(proxy);
    }

    public virtual void SetProxyData(int serviceId, int methodId, object? customData) {
        int methodKey = RpcMethodKey.MethodKey(serviceId, methodId);
        if (customData == null) {
            proxyDataMap.Remove(methodKey);
        } else {
            proxyDataMap[methodKey] = customData;
        }
    }

    public object? GetProxyData(int serviceId, int methodId) {
        int methodKey = RpcMethodKey.MethodKey(serviceId, methodId);
        proxyDataMap.TryGetValue(methodKey, out object? proxyData);
        return proxyData;
    }

    public Delegate? GetProxy(int serviceId, int methodId) {
        int methodKey = RpcMethodKey.MethodKey(serviceId, methodId);
        proxyMap.TryGetValue(methodKey, out Delegate? proxy);
        return proxy;
    }

    public Delegate? RemoveProxy(int serviceId, int methodId) {
        int methodKey = RpcMethodKey.MethodKey(serviceId, methodId);
        proxyMap.Remove(methodKey, out Delegate proxy);
        invokerMap.Remove(methodKey, out _);
        return proxy;
    }

    public RpcMethodInvoker? GetInvoker(int serviceId, int methodId) {
        int methodKey = RpcMethodKey.MethodKey(serviceId, methodId);
        invokerMap.TryGetValue(methodKey, out RpcMethodInvoker? invoker);
        return invoker;
    }

    public void Disable(int serviceId, int methodId) {
        if (methodId == -1) {
            foreach (var pair in proxyMap) {
                if (RpcMethodKey.ServiceIdOfKey(pair.Key) == serviceId) {
                    disabledMap[pair.Key] = true;
                }
            }
        } else {
            int key = RpcMethodKey.MethodKey(serviceId, methodId);
            disabledMap[key] = true;
        }
    }

    public void Enable(int serviceId, int methodId) {
        if (methodId == -1) {
            List<int> keys = new List<int>();
            foreach (var pair in disabledMap) {
                if (RpcMethodKey.ServiceIdOfKey(pair.Key) == serviceId) {
                    keys.Add(pair.Key);
                }
            }
            foreach (int key in keys) {
                disabledMap.Remove(key);
            }
        } else {
            int key = RpcMethodKey.MethodKey(serviceId, methodId);
            disabledMap.Remove(key);
        }
    }

    public bool IsDisabled(int serviceId, int methodId) {
        int key = RpcMethodKey.MethodKey(serviceId, methodId);
        return disabledMap.ContainsKey(key);
    }

    public HashSet<int> Export() {
        var result = new HashSet<int>(proxyMap.Count);
        foreach (int key in proxyMap.Keys) {
            result.Add(RpcMethodKey.ServiceIdOfKey(key));
        }
        return result;
    }

    public void Clear() {
        proxyMap.Clear();
        invokerMap.Clear();
        proxyDataMap.Clear();
    }

    /// <summary>
    /// 当前是否处于可变状态
    /// </summary>
    public bool IsMutable => mutable;

    /**
     * 设置为不可变(主线程注册完毕后调用)
     * 1.设置为不可变后，不再可增删MethodInfo和MethodProxy，切面数据可以动态变更
     * 2.建议执行该方法
     */
    public void MakeImmutable() {
        mutable = true;
    }

    /** 检查是否处于不可变状态(IO线程启动时调用) */
    public void EnsureImmutable() {
        if (mutable) {
            throw new IllegalStateException("registry is mutable");
        }
    }

    /** 检查是否处于可变状态(主线程检测) */
    public void EnsureMutable() {
        if (!mutable) {
            throw new IllegalStateException("registry is immutable");
        }
    }
}
}