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
using System.Reflection;

namespace Wjybxx.BigCat.Gameplay
{
internal static class ComponentUtil
{
    /// <summary>
    /// 当前节点自身是否为active状态
    /// active和enable其实是一码事，只是作用于GameObject时使用Active更符合语义，作用于脚本时Enable更符合语义。
    /// </summary>
    public const int MASK_NOT_ACTIVE_SELF = 1;
    /// <summary>
    /// 当前节点及其所有父节点是否都为active状态
    /// </summary>
    public const int MASK_NOT_ACTIVE_IN_HIERARCHY = 1 << 1;

    /// <summary>
    /// 组件的重写信息
    /// </summary>
    private static readonly ConcurrentDictionary<Type, ScriptMethods> overridesCache = new();

    public static bool IsIntersect(this ScriptMethods self, ScriptMethods other) {
        return (self & other) != 0;
    }

    public static ScriptMethods GetOverrideInfo(Type abstractType, Type type) {
        if (overridesCache.TryGetValue(type, out ScriptMethods methods)) {
            return methods;
        }
        methods = ScriptMethods.All;
        if (!IsOverride(abstractType, type, "EarlyUpdate")) methods &= ~ScriptMethods.EarlyUpdate;
        if (!IsOverride(abstractType, type, "FixedUpdate")) methods &= ~ScriptMethods.FixedUpdate;
        if (!IsOverride(abstractType, type, "Update")) methods &= ~ScriptMethods.Update;
        if (!IsOverride(abstractType, type, "LateUpdate")) methods &= ~ScriptMethods.LateUpdate;
        overridesCache.TryAdd(type, methods);
        return methods;
    }

    /** 是否重写了某个方法 */
    private static bool IsOverride(Type abstractType, Type currentType, string methodName) {
        MethodInfo? methodInfo = currentType.GetMethod(methodName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (methodInfo == null) {
            throw new InvalidOperationException($"Method {methodName} not found");
        }
        Type declaringType = methodInfo.DeclaringType!;
        return declaringType != abstractType;
    }

    public static IComparer<SComponent> UpdateOrderComparer => CUpdateOrderComparer.Inst;

    private class CUpdateOrderComparer : IComparer<SComponent>
    {
        public static CUpdateOrderComparer Inst { get; } = new CUpdateOrderComparer();

        public int Compare(SComponent x, SComponent y) {
            // ReSharper disable PossibleNullReferenceException
            int lhs = x.Cid.updateOrder;
            int rhs = y.Cid.updateOrder;
            // -1排后面
            if (lhs == -1 || rhs == -1) {
                if (lhs == -1) return 1;
                if (rhs == -1) return -1;
                return 0;
            }
            return lhs.CompareTo(rhs);
        }
    }
}
}