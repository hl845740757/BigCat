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
using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;

namespace Wjybxx.BigCat.Tests
{
/// <summary>
/// 测试泛型参数装箱问题
///
/// 结论：
/// 1.<see cref="EqualityComparer{T}"/>会调用用户实现的Equals和GetHashCode方法。
/// 2.当用户实现了<see cref="IEquatable{T}"/>的时候，优先调用<see cref="IEquatable{T}"/>接口的Equals方法。
/// </summary>
public class GenericEqualsTest
{
    [Test]
    public void TestCustomKey() {
        EqualityComparer<Key> comparer = EqualityComparer<Key>.Default;
        Key k1 = new Key(1);
        Key k2 = new Key(2);
        int _ = comparer.GetHashCode(k1);
        bool __ = comparer.Equals(k1, k2);
    }

    [Test]
    public void TestInt32() {
        EqualityComparer<int> comparer = EqualityComparer<int>.Default;
        int hash = comparer.GetHashCode(int.MinValue);
        bool __ = comparer.Equals(int.MinValue, int.MaxValue);
    }

    [Test]
    public void TestFloat() {
        EqualityComparer<float> comparer = EqualityComparer<float>.Default;
        int hash = comparer.GetHashCode(float.MinValue);
        bool __ = comparer.Equals(float.MinValue, float.MaxValue);
    }

    /// <summary>
    /// 测试没有泛型约束的情况下是否会产生装箱
    /// </summary>
    [Test]
    public void TestCustomKey2() {
        Key k1 = new Key(1);
        Key k2 = new Key(2);
        TestGenericEquals(k1, k2);
    }

    private static void TestGenericEquals<T>(T key1, T key2) where T : IEquatable<T> {
        int hash1 = key1.GetHashCode();
        int hash2 = key2.GetHashCode();
        bool eq = key1.Equals(key2);
    }

    private class GenericWrapper<T>
    {
        public T value;

        public GenericWrapper(T value) {
            this.value = value;
        }
    }


    private readonly struct Key : IEquatable<Key>
    {
        public readonly int val;

        public Key(int val) {
            this.val = val;
        }

        public bool Equals(Key other) {
            return val == other.val;
        }

        public override bool Equals(object? obj) {
            return obj is Key key && key.val == val;
        }

        public override int GetHashCode() {
            return val;
        }

        public override string ToString() {
            return val.ToString();
        }
    }
}
}