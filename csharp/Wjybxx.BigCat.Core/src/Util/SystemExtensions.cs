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
using System.Runtime.CompilerServices; // unity环境下依赖

namespace System
{
/// <summary>
/// 系统类扩展
///
/// 1.主要给Unity用
/// 2.特意命名System命名空间
/// </summary>
public static class SystemExtensions
{
#if UNITY_2021_3_OR_NEWER
    public static void EnsureCapacity<T>(this List<T> list, int capacity) {
        if (list.Capacity >= capacity) {
            return;
        }
        if (capacity <= 4) {
            list.Capacity = 4;
            return;
        }
        int newCapacity = list.Capacity + list.Capacity / 2;
        list.Capacity = Math.Max(newCapacity, capacity);
    }

    /// <summary>
    /// 对象是否处于存活状态
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CheckObject(UnityEngine.Object obj) {
        return obj;
    }
#endif
}
}