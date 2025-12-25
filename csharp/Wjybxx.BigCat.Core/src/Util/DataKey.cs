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

namespace Wjybxx.BigCat.Util
{
/// <summary>
/// 接口用于处理泛型
/// 
/// 1.非泛型接口用于装箱，避免用户传入奇怪的对象。
/// 2.也用于提供装箱的拆装箱接口
/// </summary>
public interface DataKey
{
    string Name { get; }
    Type DataType { get; }

    object Unbox(in UnionValue boxedValue);

    UnionValue Box(object value);
}

/// <summary>
/// 数据键抽象，搭配<see cref="UnionValue"/>使用。
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class DataKey<T> : DataKey
{
    private readonly string _name;
    private readonly int _hash;

    protected DataKey(string name) {
        // 其实可以根据name + 泛型参数分配唯一id
        _name = name;
        _hash = name.GetHashCode() * 31 + typeof(T).GetHashCode();
    }

    public string Name => _name;
    public Type DataType => typeof(T);

    public abstract T Unbox(in UnionValue boxedValue);

    public abstract UnionValue Box(T value);

    public override bool Equals(object obj) {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        // string的equals默认没有先测试引用，而我们的Key多数情况下是常量对象，因此先测试引用
        if (obj is DataKey<T> other) {
            return ReferenceEquals(_name, other._name) || _name == other._name;
        }
        return false;
    }

    public override int GetHashCode() {
        return _hash;
    }

    public override string ToString() {
        return $"{nameof(Name)}: {_name}, {nameof(DataType)}: {DataType}";
    }

    object DataKey.Unbox(in UnionValue boxedValue) {
        return Unbox(in boxedValue);
    }

    UnionValue DataKey.Box(object value) {
        return Box((T)value);
    }
}
}