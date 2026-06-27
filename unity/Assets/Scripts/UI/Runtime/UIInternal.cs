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
using System.Runtime.CompilerServices;
using UnityEngine;
using Wjybxx.BigCat.Gameplay;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Pool;

namespace Wjybxx.BigCat.UI
{
/// <summary>
/// 
/// </summary>
internal static class UIInternal
{
    private static readonly SingleObjectPool<List<Component>> listPool = new SingleObjectPool<List<Component>>(
        ObjectPoolUtil<Component>.listFactory, ObjectPoolUtil<Component>.cleaner);

    /// <summary>
    /// 当前节点自身是否为active状态
    /// </summary>
    public const int MASK_NOT_ACTIVE_SELF = 1;
    /// <summary>
    /// 当前节点及其所有父节点是否都为active状态
    /// </summary>
    public const int MASK_NOT_ACTIVE_IN_HIERARCHY = 1 << 1;
    /// <summary>
    /// 是否处于被聚焦状态
    /// </summary>
    public const int MASK_FOCUS_ON = 1 << 2;
    /// <summary>
    /// 父窗口关闭时不自动关闭
    /// </summary>
    public const int MASK_NO_HANGUP = 1 << 3;
    /// <summary>
    /// 将窗口标记为需要重新绘制
    /// </summary>
    public const int MASK_DIRTY_REPAINT = 1 << 4;
    /// <summary>
    /// 是否需要Update
    /// </summary>
    public const int MASK_NEED_UPDATE = 1 << 5;
    /// <summary>
    /// Node是否处于Showing状态
    /// (理论上其实等价于ActiveInHierarchy)
    /// </summary>
    public const int MASK_SHOWING = 1 << 6;
    /// <summary>
    /// 立即销毁
    /// </summary>
    public const int MASK_DESTROY_IMMEDIATELY = 1 << 7;

    /// <summary>
    /// 组件的方法重写信息
    /// 注：理论上可以合并到ctl控制标记中
    /// </summary>
    private static readonly ConcurrentDictionary<Type, ScriptMethods> overridesCache = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsIntersect(this WindowFeatures self, WindowFeatures other) {
        return (self & other) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsIntersect(this ScriptMethods self, ScriptMethods other) {
        return (self & other) != 0;
    }

    public static ScriptMethods GetOverrideInfo(Type abstractType, Type type) {
        if (overridesCache.TryGetValue(type, out ScriptMethods methods)) {
            return methods;
        }
        methods = ScriptMethods.All;
        if (!IsOverride(abstractType, type, "EarlyUpdate")) methods &= ~ScriptMethods.EarlyUpdate;
        // if (!IsOverride(abstractType, type, "FixedUpdate")) methods &= ~ScriptMethods.FixedUpdate;
        if (!IsOverride(abstractType, type, "Update")) methods &= ~ScriptMethods.Update;
        if (!IsOverride(abstractType, type, "LateUpdate")) methods &= ~ScriptMethods.LateUpdate;
        overridesCache.TryAdd(type, methods);
        return methods;
    }

    /** 是否重写了某个方法 */
    private static bool IsOverride(Type abstractType, Type currentType, string methodName) {
        MethodInfo? methodInfo = currentType.GetMethod(methodName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            Array.Empty<Type>(), Array.Empty<ParameterModifier>());
        if (methodInfo == null) {
            throw new InvalidOperationException($"Method {methodName} not found");
        }
        Type declaringType = methodInfo.DeclaringType!;
        return declaringType != abstractType;
    }

    public static bool IsIntersect<T>(List<T> lhs, List<T> rhs) {
        if (lhs == null || rhs == null) return false;
        List<T> min, max;
        if (lhs.Count < rhs.Count) {
            min = lhs;
            max = rhs;
        } else {
            min = rhs;
            max = lhs;
        }
        for (int index = 0; index < min.Count; index++) {
            T key = min[index];
            if (max.Contains(key)) return true;
        }
        return false;
    }

    #region children

    private static int IndexOf(List<UINode> list, UINode node) {
        // 考虑node上的索引存在差错的情况
        if (list[node.uiIndex] == node) {
            return node.uiIndex;
        }
        for (int i = 0; i < list.Count; i++) {
            if (list[i] == node) {
                return i;
            }
        }
        throw new ArgumentException($"Node {node} not found");
    }

    /// <summary>
    /// 设置子节点的索引
    /// </summary>
    public static bool SetSiblingIndex(List<UINode> children, UINode node, int newIndex,
                                       out int min, out int max) {
        if (newIndex < 0 || newIndex >= children.Count) {
            throw new ArgumentOutOfRangeException(nameof(newIndex), newIndex, null);
        }
        int prevIndex = IndexOf(children, node);
        if (newIndex == prevIndex) {
            min = max = -1;
            return false;
        }
        children.RemoveAt(prevIndex);
        children.Insert(newIndex, node);
        // 区间刷新
        if (newIndex < prevIndex) {
            min = newIndex;
            max = prevIndex;
        } else {
            min = prevIndex;
            max = newIndex;
        }
        RefreshChildrenIndex(children, min, max);
        return true;
    }

    public static void RefreshChildrenIndex(List<UINode> children) {
        for (int idx = 0; idx < children.Count; idx++) {
            children[idx].uiIndex = idx;
        }
    }

    public static void RefreshChildrenIndex(List<UINode> children, int start, int end) {
        if (end >= children.Count) end = children.Count - 1;
        for (int idx = start; idx <= end; idx++) {
            children[idx].uiIndex = idx;
        }
    }

    #endregion

    public static IComparer<WComponent> UpdateOrderComparer => CUpdateOrderComparer.Inst;

    private class CUpdateOrderComparer : IComparer<WComponent>
    {
        public static CUpdateOrderComparer Inst { get; } = new CUpdateOrderComparer();

        public int Compare(WComponent x, WComponent y) {
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

    #region Node

    /// <summary>
    /// 查找Node关联的Controller 
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    public static Controller FindController(UINode node) {
        if (!node) throw new ArgumentNullException(nameof(node));
        List<Component> list = listPool.Acquire();
        node.gameObject.GetComponents(list);
        //
        string nodeName = node.nodeCfg.name;
        try {
            if (string.IsNullOrWhiteSpace(nodeName)) {
                foreach (Component component in list) {
                    if (component is Controller controller
                        && string.IsNullOrWhiteSpace(controller.nodeName)) {
                        return controller;
                    }
                }
            } else {
                foreach (Component component in list) {
                    if (component is Controller controller
                        && nodeName == controller.nodeName) {
                        return controller;
                    }
                }
            }
        }
        finally {
            listPool.Release(list);
        }
        return null;
    }

    public static GameObject FindElement(List<GameObject> elements, string name) {
        foreach (GameObject gameObject in elements) {
            if (gameObject.name == name) return gameObject;
        }
        return null;
    }

    public static UINode FindNode(IReadOnlyList<UINode> nodes, string name) {
        if (nodes == null) return null;
        for (int index = 0; index < nodes.Count; index++) {
            UINode node = nodes[index];
            if (node.nodeCfg.name == name) return node;
        }
        return null;
    }

    public static void FindNodes(IReadOnlyList<UINode> nodes, string name, List<UINode> outList) {
        if (nodes == null) return;
        for (int index = 0; index < nodes.Count; index++) {
            UINode node = nodes[index];
            if (node.nodeCfg.name == name) outList.Add(node);
        }
    }

    #endregion
}
}