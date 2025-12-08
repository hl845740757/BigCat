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
using System.Runtime.InteropServices;

namespace Wjybxx.BigCat.Util
{
/// <summary>
/// 黑板的Value，最大支持3个Double值，即Vector3
///
/// 1.不算对象头，内存消耗：40个字节。
/// 2.double和long值的内存是重叠的，使用时需小心。
/// 3.对于普通数字类型，建议总是使用double而非细分类型。
/// </summary>
[StructLayout(LayoutKind.Explicit)]
public struct UnionValue : IEquatable<UnionValue>
{
    /// <summary>
    /// 默认的无效值
    /// </summary>
    public static UnionValue Undefine => new UnionValue();
    /// <summary>
    /// 默认的Null值
    /// </summary>
    public static UnionValue Null => new UnionValue(-1);

    /// <summary>
    /// 0表示value无效
    ///
    /// PS：
    /// 1.Typ其实不重要，因为我们是根据Key取值的，用户可以总是赋值为1，Type主要用于Debug视图。
    /// 2.用户使用时应当使用正数，负数留给框架使用。
    /// </summary>
    [FieldOffset(0)] public readonly int type;
    [FieldOffset(4)] public int val;
    [FieldOffset(4)] public float fVal;
    /// <summary>
    /// double
    /// </summary>
    [FieldOffset(8)] public double dv1;
    [FieldOffset(16)] public double dv2;
    [FieldOffset(24)] public double dv3;
    /// <summary>
    /// long
    /// </summary>
    [FieldOffset(8)] public long lv1;
    [FieldOffset(16)] public long lv2;
    [FieldOffset(24)] public long lv3;
    /// <summary>
    /// 额外的引用类型
    /// </summary>
    [FieldOffset(32)] public object? obj1;

    public UnionValue(int type) : this() {
        if (type == 0) throw new ArgumentException("type cannot be 0"); // 0表示undefine
        this.type = type;
    }

    /// <summary>
    /// 是否是无效值
    /// </summary>
    public bool IsUndefine => type == 0;
    /// <summary>
    /// 是否是Null值
    /// (引用类型和可空值类型都会走到这里)
    /// </summary>
    public bool IsNull => type == -1;

    public bool Equals(UnionValue other) {
        if (type <= 0) {
            return type == other.type;
        }
        // double和long内存重叠，比较long部分即可
        return type == other.type
               && val == other.val
               && lv1 == other.lv1 && lv2 == other.lv2 && lv3 == other.lv3
               && Equals(obj1, other.obj1);
    }

    public override bool Equals(object? obj) {
        return obj is UnionValue other && Equals(other);
    }

    public override int GetHashCode() {
        int hashCode = type.GetHashCode();
        if (type <= 0) {
            return hashCode;
        }
        hashCode = (hashCode * 397) ^ val.GetHashCode();
        hashCode = (hashCode * 397) ^ lv1.GetHashCode();
        hashCode = (hashCode * 397) ^ lv2.GetHashCode();
        hashCode = (hashCode * 397) ^ lv3.GetHashCode();
        if (obj1 != null) hashCode = (hashCode * 397) ^ obj1.GetHashCode();
        return hashCode;
    }

    public static bool operator ==(in UnionValue left, in UnionValue right) {
        return left.Equals(right);
    }

    public static bool operator !=(in UnionValue left, in UnionValue right) {
        return !left.Equals(right);
    }
}
}