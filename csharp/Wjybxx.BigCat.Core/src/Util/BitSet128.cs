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
/// 固定128位的BitSet -- 可以满足绝大多数场景
/// </summary>
[DsonSerializable]
public sealed class BitSet128
{
    /// <summary>
    /// Bit集长度
    /// </summary>
    public const int LENGTH = 64 * 2;

    private long _lowBits;
    private long _highBits;

    public BitSet128() {

    }

    public BitSet128(BitSet128 src) {
        this._lowBits = src._lowBits;
        this._highBits = src._highBits;
    }

    public bool this[int index] {
        get => Get(index);
        set => Set(index, value);
    }

    public bool Get(int bitIndex) {
        CheckIndex(bitIndex);
        if (bitIndex > 63) {
            return (_highBits & 1L << (bitIndex - 64)) != 0;
        } else {
            return (_lowBits & (1L << bitIndex)) != 0;
        }
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
        if (bitIndex > 63) {
            _highBits |= (1L << (bitIndex - 64));
        } else {
            _lowBits |= (1L << bitIndex);
        }
    }

    public void Unset(int bitIndex) {
        CheckIndex(bitIndex);
        if (bitIndex > 63) {
            _highBits &= ~(1L << (bitIndex - 64));
        } else {
            _lowBits &= ~(1L << bitIndex);
        }
    }

    public void Clear() {
        _lowBits = 0;
        _highBits = 0;
    }

    /// <summary>
    /// 低位Bit信息
    /// </summary>
    public long LowBits {
        get => _lowBits;
        set => _lowBits = value;
    }
    /// <summary>
    /// 高位Bit信息
    /// </summary>
    public long HighBits {
        get => _highBits;
        set => _highBits = value;
    }
    /// <summary>
    /// 比特集中的1数量
    /// </summary>
    public int BitCount => MathCommon.BitCount(_lowBits) + MathCommon.BitCount(_highBits);
    /// <summary>
    /// 比特集中的0数量
    /// </summary>
    public int ZeroCount => LENGTH - MathCommon.BitCount(_lowBits) - MathCommon.BitCount(_highBits);
    /// <summary>
    /// 是否为空集
    /// </summary>
    public bool IsEmpty => _lowBits == 0 && _highBits == 0;

    /// <summary>
    /// 与运算
    /// </summary>
    /// <param name="other"></param>
    public void And(BitSet128 other) {
        this._lowBits &= other._lowBits;
        this._highBits &= other._highBits;
    }

    /// <summary>
    /// 或运算
    /// </summary>
    /// <param name="other"></param>
    public void Or(BitSet128 other) {
        this._lowBits |= other._lowBits;
        this._highBits |= other._highBits;
    }

    /// <summary>
    /// 异或
    /// </summary>
    /// <param name="other"></param>
    public void Xor(BitSet128 other) {
        this._lowBits ^= other._lowBits;
        this._highBits ^= other._highBits;
    }

    /// <summary>
    /// 与非（即清除）
    /// </summary>
    /// <param name="other"></param>
    public void AndNot(BitSet128 other) {
        this._lowBits &= ~other._lowBits;
        this._highBits &= ~other._highBits;
    }

    /// <summary>
    /// 是否相交
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Intersect(BitSet128 other) {
        return (this._lowBits & other._lowBits) != 0
               || (this._highBits & other._highBits) != 0;
    }

    /// <summary>
    /// 内容取反
    /// </summary>
    public void Not() {
        this._lowBits = ~this._lowBits;
        this._highBits = ~this._highBits;
    }

    /// <summary>
    /// 拷贝数据
    /// </summary>
    /// <returns></returns>
    public BitSet128 Copy() {
        return new BitSet128(this);
    }

    #region equals

    private bool Equals(BitSet128 other) {
        return _lowBits == other._lowBits && _highBits == other._highBits;
    }

    public override bool Equals(object obj) {
        return ReferenceEquals(this, obj) || obj is BitSet128 other && Equals(other);
    }

    public override int GetHashCode() {
        return (_lowBits.GetHashCode() * 397) ^ _highBits.GetHashCode();
    }

    #endregion

    public override string ToString() {
        // 高位在前
        return $" {nameof(_highBits)}: {_highBits.ToString("X")}, {nameof(_lowBits)}: {_lowBits.ToString("X")}";
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CheckIndex(int index) {
        if (index < 0 || index >= LENGTH) {
            throw new IndexOutOfRangeException($"length: {LENGTH}, index {index}");
        }
    }
}
}