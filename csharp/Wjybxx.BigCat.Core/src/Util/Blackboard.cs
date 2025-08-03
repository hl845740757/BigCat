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

namespace Wjybxx.BigCat.UI
{
/// <summary>
/// 基础的的黑板实现
/// </summary>
public sealed class Blackboard
{
    private readonly Dictionary<DataKey, UnionValue> dataMap;

    public Blackboard() {
        dataMap = new Dictionary<DataKey, UnionValue>();
    }

    public Blackboard(int capacity, IEqualityComparer<DataKey> comparer = null) {
        dataMap = new Dictionary<DataKey, UnionValue>(capacity, comparer);
    }

    public int Count => dataMap.Count;

    public bool ContainsKey(DataKey key) => dataMap.ContainsKey(key);

    public void Clear() => dataMap.Clear();

    #region 泛型key

    private static bool IsNullableType(Type type) {
        if (!type.IsValueType) return true;
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
    }

    public void Set<T>(DataKey<T> key, T value) {
        // 避免值类型测试null产生装箱
        UnionValue unionValue = (IsNullableType(typeof(T)) && value == null) ? UnionValue.Null : key.Box(value);
        dataMap[key] = unionValue;
    }

    public T Get<T>(DataKey<T> key) {
        if (dataMap.TryGetValue(key, out UnionValue unionValue)) {
            return unionValue.IsNull ? default : key.Unbox(unionValue);
        }
        return default;
    }

    public bool Get<T>(DataKey<T> key, out T value) {
        if (dataMap.TryGetValue(key, out UnionValue unionValue)) {
            value = unionValue.IsNull ? default : key.Unbox(unionValue);
            return true;
        }
        value = default;
        return false;
    }

    public bool Remove<T>(DataKey<T> key) {
        return dataMap.Remove(key);
    }

    public bool Remove<T>(DataKey<T> key, out T value) {
        if (dataMap.Remove(key, out UnionValue unionValue)) {
            value = unionValue.IsNull ? default : key.Unbox(unionValue);
            return true;
        }
        value = default;
        return false;
    }

    #endregion

    #region nullable支持

    public void Set<T>(DataKey<T?> key, T? value) where T : struct {
        UnionValue unionValue = value.HasValue ? key.Box(value.Value) : UnionValue.Null;
        dataMap[key] = unionValue;
    }

    public T? Get<T>(DataKey<T?> key) where T : struct {
        if (dataMap.TryGetValue(key, out UnionValue unionValue)) {
            return unionValue.IsNull ? null : key.Unbox(unionValue);
        }
        return null;
    }

    public bool Get<T>(DataKey<T?> key, out T? value) where T : struct {
        if (dataMap.TryGetValue(key, out UnionValue unionValue)) {
            value = unionValue.IsNull ? null : key.Unbox(unionValue);
            return true;
        }
        value = null;
        return false;
    }

    public bool Remove<T>(DataKey<T?> key) where T : struct {
        return dataMap.Remove(key);
    }

    public bool Remove<T>(DataKey<T?> key, out T? value) where T : struct {
        if (dataMap.Remove(key, out UnionValue unionValue)) {
            value = unionValue.IsNull ? null : key.Unbox(unionValue);
            return true;
        }
        value = null;
        return false;
    }

    #endregion
}
}