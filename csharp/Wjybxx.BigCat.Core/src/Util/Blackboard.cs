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

using System.Collections.Generic;
using Wjybxx.Commons;

namespace Wjybxx.BigCat.Util
{
/// <summary>
/// 基础的的黑板实现
/// </summary>
public class Blackboard
{
    private Blackboard shared; // 共享内存区
    private readonly Dictionary<DataKey, UnionValue> dataMap;

    public Blackboard(int capacity = 0) {
        dataMap = new Dictionary<DataKey, UnionValue>(capacity);
    }

    /// <summary>
    /// 绑定的共享内存
    /// </summary>
    public Blackboard Shared {
        get => shared;
        set => shared = value;
    }

    /// <summary>
    /// 清理数据
    /// </summary>
    public void Clear() => dataMap.Clear();

    /// <summary>
    /// 重置对象（会清理共享黑板引用）
    /// </summary>
    public void Reset() {
        shared = null;
        dataMap.Clear();
    }

    /// <summary>
    /// 字段数量
    /// (本地字段数，与clear对应)
    /// </summary>
    public int Count => dataMap.Count;

    /// <summary>
    /// 是否包含目标字段
    /// </summary>
    public bool ContainsKey(DataKey key) {
        return dataMap.ContainsKey(key) || (shared != null && shared.ContainsKey(key));
    }

    /// <summary>
    /// 本地内存是否包含目标变量
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public bool LocalContainsKey(DataKey key) {
        return dataMap.ContainsKey(key);
    }

    #region 泛型key

    public void Set<T>(DataKey<T> key, T value) {
        if (shared == null || dataMap.ContainsKey(key) || !shared.ContainsKey(key)) {
            // 避免值类型测试null产生装箱
            UnionValue unionValue = (typeof(T).IsValueType || value != null) ? key.Box(value) : UnionValue.Null;
            dataMap[key] = unionValue;
        } else {
            shared.Set(key, value);
        }
    }

    public T Get<T>(DataKey<T> key) {
        if (dataMap.TryGetValue(key, out UnionValue unionValue)) {
            return unionValue.IsNull ? default : key.Unbox(unionValue);
        }
        if (shared != null) {
            return shared.Get<T>(key);
        }
        return default;
    }

    public bool Get<T>(DataKey<T> key, out T value) {
        if (dataMap.TryGetValue(key, out UnionValue unionValue)) {
            value = unionValue.IsNull ? default : key.Unbox(unionValue);
            return true;
        }
        if (shared != null) {
            return shared.Get(key, out value);
        }
        value = default;
        return false;
    }

    public bool Remove<T>(DataKey<T> key) {
        if (dataMap.Remove(key)) {
            return true;
        }
        if (shared != null) {
            return shared.Remove(key);
        }
        return false;
    }

    public bool Remove<T>(DataKey<T> key, out T value) {
        if (dataMap.Remove(key, out UnionValue unionValue)) {
            value = unionValue.IsNull ? default : key.Unbox(unionValue);
            return true;
        }
        if (shared != null) {
            return shared.Remove(key, out value);
        }
        value = default;
        return false;
    }

    #endregion

    #region nullable支持

    public void Set<T>(DataKey<T?> key, T? value) where T : struct {
        if (shared == null || dataMap.ContainsKey(key) || !shared.ContainsKey(key)) {
            UnionValue unionValue = value.HasValue ? key.Box(value.Value) : UnionValue.Null;
            dataMap[key] = unionValue;
        } else {
            shared.Set(key, value);
        }
    }

    public T? Get<T>(DataKey<T?> key) where T : struct {
        if (dataMap.TryGetValue(key, out UnionValue unionValue)) {
            return unionValue.IsNull ? null : key.Unbox(unionValue);
        }
        if (shared != null) {
            return shared.Get(key);
        }
        return null;
    }

    public bool Get<T>(DataKey<T?> key, out T? value) where T : struct {
        if (dataMap.TryGetValue(key, out UnionValue unionValue)) {
            value = unionValue.IsNull ? null : key.Unbox(unionValue);
            return true;
        }
        if (shared != null) {
            return shared.Get(key, out value);
        }
        value = null;
        return false;
    }

    public bool Remove<T>(DataKey<T?> key) where T : struct {
        if (dataMap.Remove(key)) {
            return true;
        }
        if (shared != null) {
            return shared.Remove(key);
        }
        return false;
    }

    public bool Remove<T>(DataKey<T?> key, out T? value) where T : struct {
        if (dataMap.Remove(key, out UnionValue unionValue)) {
            value = unionValue.IsNull ? null : key.Unbox(unionValue);
            return true;
        }
        if (shared != null) {
            return shared.Remove(key, out value);
        }
        value = null;
        return false;
    }

    #endregion

    #region local

    #region 泛型key

    public void LocalSet<T>(DataKey<T> key, T value) {
        // 避免值类型测试null产生装箱
        UnionValue unionValue = (typeof(T).IsValueType || value != null) ? key.Box(value) : UnionValue.Null;
        dataMap[key] = unionValue;
    }

    public T LocalGet<T>(DataKey<T> key) {
        if (dataMap.TryGetValue(key, out UnionValue unionValue)) {
            return unionValue.IsNull ? default : key.Unbox(unionValue);
        }
        return default;
    }

    public bool LocalGet<T>(DataKey<T> key, out T value) {
        if (dataMap.TryGetValue(key, out UnionValue unionValue)) {
            value = unionValue.IsNull ? default : key.Unbox(unionValue);
            return true;
        }
        value = default;
        return false;
    }

    public bool LocalRemove<T>(DataKey<T> key) {
        return dataMap.Remove(key);
    }

    public bool LocalRemove<T>(DataKey<T> key, out T value) {
        if (dataMap.Remove(key, out UnionValue unionValue)) {
            value = unionValue.IsNull ? default : key.Unbox(unionValue);
            return true;
        }
        value = default;
        return false;
    }

    #endregion

    #region nullable支持

    public void LocalSet<T>(DataKey<T?> key, T? value) where T : struct {
        UnionValue unionValue = value.HasValue ? key.Box(value.Value) : UnionValue.Null;
        dataMap[key] = unionValue;
    }

    public T? LocalGet<T>(DataKey<T?> key) where T : struct {
        if (dataMap.TryGetValue(key, out UnionValue unionValue)) {
            return unionValue.IsNull ? null : key.Unbox(unionValue);
        }
        return null;
    }

    public bool LocalGet<T>(DataKey<T?> key, out T? value) where T : struct {
        if (dataMap.TryGetValue(key, out UnionValue unionValue)) {
            value = unionValue.IsNull ? null : key.Unbox(unionValue);
            return true;
        }
        value = null;
        return false;
    }

    public bool LocalRemove<T>(DataKey<T?> key) where T : struct {
        return dataMap.Remove(key);
    }

    public bool LocalRemove<T>(DataKey<T?> key, out T? value) where T : struct {
        if (dataMap.Remove(key, out UnionValue unionValue)) {
            value = unionValue.IsNull ? null : key.Unbox(unionValue);
            return true;
        }
        value = null;
        return false;
    }

    #endregion

    #endregion
}
}