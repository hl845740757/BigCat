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

namespace Wjybxx.BigCat.Fx
{
/// <summary>
/// 默认的Rpc注册表实现
/// </summary>
public class DefaultRpcProxyRegistry : RpcProxyRegistry
{
    private readonly Dictionary<int, Delegate> proxyMap = new(512);
    private readonly Dictionary<int, RpcMethodInvoker> invokerMap = new(512);
    private readonly Dictionary<int, bool> disabledMap = new();
    private readonly Dictionary<int, object?> proxyDataMap = new(512);

    public void Register<T>(int serviceId, int methodId, RpcMethodProxy<T> proxy) {
        if (proxy == null) throw new ArgumentNullException(nameof(proxy));
        int methodKey = RpcMethodKey.MethodKey(serviceId, methodId);
        if (proxyMap.ContainsKey(methodKey)) {
            throw new ArgumentException($"methodKey is duplicate, serviceId: {serviceId}, methodId: {methodId}");
        }
        proxyMap[methodKey] = proxy;
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
        return new HashSet<int>(proxyMap.Keys);
    }

    public void Clear() {
        proxyMap.Clear();
        invokerMap.Clear();
        proxyDataMap.Clear();
    }
}
}