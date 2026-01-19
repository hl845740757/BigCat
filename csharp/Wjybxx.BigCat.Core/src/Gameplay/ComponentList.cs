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
using Wjybxx.BigCat.Util;
using Wjybxx.Commons;
using Wjybxx.Commons.Fx;

// ReSharper disable PossibleUnintendedReferenceComparison

namespace Wjybxx.BigCat.Gameplay
{
/// <summary>
/// 组件列表辅助类
/// </summary>
/// <typeparam name="T"></typeparam>
public interface ComponentListHelper<T>
{
    ComponentId GetCid(T element);

    T? GetNext(T element);

    void SetNext(T element, T? next);
}

/// <summary>
/// 定制组件List
/// 
/// 1.增删组件，以及获取Last的情况都很少，因此不需要考虑迭代链表的性能问题。
/// 2.如果为不同的组件分配了相同的CacheIndex，使用Mask时要小心。
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class ComponentList<T> where T : class
{
    private T?[] _elements;
    private readonly ComponentListHelper<T> _helper;
    private GBitSet _elementsMask;

    public ComponentList(ComponentListHelper<T> helper, int capacity = 0) {
        _helper = helper;
        _elements = capacity > 0 ? new T[capacity] : Array.Empty<T>();
    }

    /// <summary>
    /// 组件掩码
    /// </summary>
    public GBitSet Mask => _elementsMask;

    public T? Get(ComponentId cid) {
        T? comp;
        if (cid.cacheIndex < _elements.Length
            && (comp = _elements[cid.cacheIndex]) != null
            && (_helper.GetCid(comp) == cid)) {
            return comp;
        }
        return null;
    }

    public U? Get<U>(ComponentId<U> cid) where U : class {
        T? comp;
        if (cid.cacheIndex < _elements.Length
            && (comp = _elements[cid.cacheIndex]) != null
            && (_helper.GetCid(comp) == cid)) {
            return comp as U;
        }
        return null;
    }

    public T? GetLast(ComponentId cid) {
        T? comp;
        if (cid.cacheIndex < _elements.Length
            && (comp = _elements[cid.cacheIndex]) != null
            && (_helper.GetCid(comp) == cid)) {
            //
            T next;
            while ((next = _helper.GetNext(comp)) != null) {
                comp = next;
            }
            return comp;
        }
        return null;
    }

    public U? GetLast<U>(ComponentId<U> cid) where U : class {
        T? comp;
        if (cid.cacheIndex < _elements.Length
            && (comp = _elements[cid.cacheIndex]) != null
            && (_helper.GetCid(comp) == cid)) {
            //
            T next;
            while ((next = _helper.GetNext(comp)) != null) {
                comp = next;
            }
            return comp as U;
        }
        return null;
    }

    public void Get(ComponentId cid, List<T> result) {
        T? comp;
        if (cid.cacheIndex < _elements.Length
            && (comp = _elements[cid.cacheIndex]) != null
            && (_helper.GetCid(comp) == cid)) {
            //
            result.Add(comp);
            while ((comp = _helper.GetNext(comp)) != null) {
                result.Add(comp);
            }
        }
    }

    public void Get<U>(ComponentId<U> cid, List<U> result) where U : class {
        T? comp;
        if (cid.cacheIndex < _elements.Length
            && (comp = _elements[cid.cacheIndex]) != null
            && (_helper.GetCid(comp) == cid)) {
            //
            result.Add(comp as U);
            while ((comp = _helper.GetNext(comp)) != null) {
                result.Add(comp as U);
            }
        }
    }

    public int Count(ComponentId cid) {
        T? comp;
        if (cid.cacheIndex < _elements.Length
            && (comp = _elements[cid.cacheIndex]) != null
            && (_helper.GetCid(comp) == cid)) {
            //
            int r = 1;
            while ((comp = _helper.GetNext(comp)) != null) {
                r++;
            }
            return r;
        }
        return 0;
    }

    public bool Contains(T component) {
        ComponentId cid = _helper.GetCid(component);
        T? comp;
        if (cid.cacheIndex < _elements.Length
            && (comp = _elements[cid.cacheIndex]) != null
            && (_helper.GetCid(comp) == cid)) { // 组件id相同才能相同
            //
            if (ReferenceEquals(comp, component)) {
                return true;
            }
            while ((comp = _helper.GetNext(comp)) != null) {
                if (ReferenceEquals(comp, component)) {
                    return true;
                }
            }
        }
        return false;
    }

    public void Add(T component, bool addFirst = false) {
        ComponentId cid = _helper.GetCid(component);
        // 新组件和既有组件的cid必须相同
        T? exist;
        if (cid.cacheIndex < _elements.Length
            && (exist = _elements[cid.cacheIndex]) != null
            && (_helper.GetCid(exist) != cid)) {
            throw new InvalidOperationException($"component conflict, exist: {_helper.GetCid(exist)}, comp: {cid}");
        }
        if (cid.cacheIndex > _elements.Length - 1) {
            EnsureCapacity(cid.cacheIndex + 1);
        }
        //
        T? head = _elements[cid.cacheIndex];
        if (head == null) {
            _elements[cid.cacheIndex] = component;
            _elementsMask.Set(cid.cacheIndex);
            return;
        }
        //
        if (addFirst) {
            _helper.SetNext(component, head);
            _elements[cid.cacheIndex] = component;
        } else {
            T next;
            while ((next = _helper.GetNext(head)) != null) {
                head = next;
            }
            _helper.SetNext(head, component);
        }
    }

    public bool Remove(T component) {
        ComponentId cid = _helper.GetCid(component);
        T? comp;
        if (cid.cacheIndex < _elements.Length
            && (comp = _elements[cid.cacheIndex]) != null
            && (_helper.GetCid(comp) == cid)) { // 组件id相同才能相同
            //
            if (ReferenceEquals(comp, component)) { // 删除队首
                T next = _helper.GetNext(comp);
                _helper.SetNext(component, null);
                if (next == null) {
                    _elements[cid.cacheIndex] = null;
                    _elementsMask.Unset(cid.cacheIndex);
                } else {
                    _elements[cid.cacheIndex] = next;
                }
                return true;
            }
            T prev = comp;
            while ((comp = _helper.GetNext(comp)) != null) {
                if (ReferenceEquals(comp, component)) { // 删除中间节点
                    _helper.SetNext(prev, _helper.GetNext(comp));
                    _helper.SetNext(comp, null);
                    return true;
                }
                prev = comp;
            }
        }
        return false;
    }

    public void Clear() {
        Array.Clear(_elements, 0, _elements.Length);
        _elementsMask.Clear();
    }

    #region internal

    private const int MAX_CAPACITY = int.MaxValue - 8;

    public void EnsureCapacity(int minCapacity) {
        int oldCapacity = _elements.Length;
        if (minCapacity <= oldCapacity) {
            return;
        }
        if (minCapacity > MAX_CAPACITY) {
            throw new OutOfMemoryException("Required array length " + minCapacity + " is too large");
        }

        int grow = Math.Max(8, oldCapacity >> 1);
        int newCapacity = MathCommon.Clamp((long)oldCapacity + grow, minCapacity, MAX_CAPACITY);
        _elements = ArrayUtil.CopyOf(_elements, 0, newCapacity);
    }

    #endregion
}
}