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
using Wjybxx.Commons;
using Wjybxx.Dson.Codec.Attributes;

namespace Wjybxx.BigCat.Util
{
/// <summary>
/// 固定64位的BitSet
/// </summary>
[DsonSerializable]
public sealed class BitSet64
{
    /// <summary>
    /// Bit集长度
    /// </summary>
    public const int LENGTH = 64;

    [DsonIgnore(false)]
    [DsonProperty(Getter = "Bits", Setter = "Bits")]
    private long _lowBits;

    public BitSet64() {

    }

    public BitSet64(BitSet64 src) {
        this._lowBits = src._lowBits;
    }

    public bool this[int index] {
        get => Get(index);
        set => Set(index, value);
    }

    public bool Get(int bitIndex) {
        CheckIndex(bitIndex);
        return (_lowBits & (1L << bitIndex)) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(int index, bool value) {
        if (value) {
            Set(index);
        } else {
            Unset(index);
        }
    }

    public void Set(int bitIndex) {
        CheckIndex(bitIndex);
        _lowBits |= (1L << bitIndex);
    }

    public void Unset(int bitIndex) {
        CheckIndex(bitIndex);
        _lowBits &= ~(1L << bitIndex);
    }

    public void Clear() {
        _lowBits = 0;
    }

    /// <summary>
    /// 所有的bit信息
    /// </summary>
    public long Bits {
        get => _lowBits;
        set => _lowBits = value;
    }
    /// <summary>
    /// 比特集中的1数量
    /// </summary>
    public int BitCount => MathCommon.BitCount(_lowBits);
    /// <summary>
    /// 比特集中的0数量
    /// </summary>
    public int ZeroCount => LENGTH - MathCommon.BitCount(_lowBits);
    /// <summary>
    /// 是否为空集
    /// </summary>
    public bool IsEmpty => _lowBits == 0;

    /// <summary>
    /// 与运算
    /// </summary>
    /// <param name="other"></param>
    public void And(BitSet64 other) {
        this._lowBits &= other._lowBits;
    }

    /// <summary>
    /// 或运算
    /// </summary>
    /// <param name="other"></param>
    public void Or(BitSet64 other) {
        this._lowBits |= other._lowBits;
    }

    /// <summary>
    /// 异或
    /// </summary>
    /// <param name="other"></param>
    public void Xor(BitSet64 other) {
        this._lowBits ^= other._lowBits;
    }

    /// <summary>
    /// 与非（即清除）
    /// </summary>
    /// <param name="other"></param>
    public void AndNot(BitSet64 other) {
        this._lowBits &= ~other._lowBits;
    }

    /// <summary>
    /// 是否相交
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Intersect(BitSet64 other) {
        return (this._lowBits & other._lowBits) != 0;
    }

    /// <summary>
    /// 内容取反
    /// </summary>
    public void Not() {
        this._lowBits = ~this._lowBits;
    }

    /// <summary>
    /// 拷贝数据
    /// </summary>
    /// <returns></returns>
    public BitSet64 Copy() {
        return new BitSet64(this);
    }

    #region equals

    private bool Equals(BitSet64 other) {
        return _lowBits == other._lowBits;
    }

    public override bool Equals(object obj) {
        return ReferenceEquals(this, obj) || obj is BitSet64 other && Equals(other);
    }

    public override int GetHashCode() {
        return _lowBits.GetHashCode();
    }

    #endregion

    public override string ToString() {
        return _lowBits.ToString("X");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CheckIndex(int index) {
        if (index < 0 || index >= LENGTH) {
            throw new IndexOutOfRangeException($"length: {LENGTH}, index {index}");
        }
    }
}
}