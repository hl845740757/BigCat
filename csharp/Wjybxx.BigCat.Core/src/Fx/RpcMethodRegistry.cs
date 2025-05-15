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
/// rpc方法信息注册表
/// 1.客户端和服务端的都需要注册。
/// 2.整个Node（或进程）一份。
///
/// {@link RpcMethodInfo}通常由主线程进行注册，IO线程查询使用，
/// 为保证线程可见性和安全性，主线程在注册完成之后需调用<see cref="MakeImmutable"/>将registry变更为不可变状态（注册完成），
/// IO线程在启动时可调用<see cref="EnsureImmutable"/>检查registry的状态。
/// (另一种方案是通过线程的启动顺序来保证可见性，同时后续禁止修改。)
/// </summary>
public sealed class RpcMethodRegistry
{
    private volatile bool mutable = true;
    private readonly Dictionary<int, RpcMethodInfo> methodInfoMap = new(100);

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