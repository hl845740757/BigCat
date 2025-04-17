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
using System.Runtime.CompilerServices;

namespace Wjybxx.BigCat.Core
{
/// <summary>
/// rpc方法键工具类
/// </summary>
public static class RpcMethodKey
{
    /// <summary>
    /// 服务id的乘系数
    /// </summary>
    public const int FACTOR = 10000;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int MethodKey(int serviceId, int methodId) {
        if (methodId < 0 || methodId >= FACTOR) {
            throw new ArgumentException("methodId must be between [0, 9999]");
        }
        // 使用乘法更直观，更有规律；负数需要转正数，计算后再转负数
        if (serviceId < 0) {
            return -1 * (Math.Abs(serviceId) * FACTOR + methodId);
        } else {
            return serviceId * FACTOR + methodId;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ServiceIdOfKey(int methodKey) {
        if (methodKey < 0) {
            return -1 * (Math.Abs(methodKey) / FACTOR);
        } else {
            return methodKey / FACTOR;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int MethodIdOfKey(int methodKey) {
        if (methodKey < 0) {
            return Math.Abs(methodKey) % FACTOR;
        }
        return methodKey % FACTOR;
    }
}
}