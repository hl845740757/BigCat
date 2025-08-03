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

namespace Wjybxx.BigCat.Util
{
/// <summary>
/// 固定64位的BitSet
/// </summary>
public sealed class BitSet64
{
    /// <summary>
    /// Bit集长度
    /// </summary>
    public const int LENGTH = 64;

    private long _bits;

    public BitSet64() {

    }

    public BitSet64(BitSet64 src) {
        this._bits = src._bits;
    }

    public bool Get(int bitIndex) {
        CheckIndex(bitIndex);
        return (_bits & (1L << bitIndex)) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(int index, bool value) {
        if (value) {
            Set(index);
        } else {
            Clear(index);
        }
    }

    public void Set(int bitIndex) {
        CheckIndex(bitIndex);
        _bits |= (1L << bitIndex);
    }

    public void Clear(int bitIndex) {
        CheckIndex(bitIndex);
        _bits &= ~(1L << bitIndex);
    }

    public void Clear() {
        _bits = 0;
    }

    /// <summary>
    /// 所有的bit信息
    /// </summary>
    public long Bits => _bits;
    /// <summary>
    /// 比特集中的1数量
    /// </summary>
    public int BitCount => MathCommon.BitCount(_bits);
    /// <summary>
    /// 比特集中的0数量
    /// </summary>
    public int ZeroCount => LENGTH - MathCommon.BitCount(_bits);
    /// <summary>
    /// 是否为空集
    /// </summary>
    public bool IsEmpty => _bits == 0;

    /// <summary>
    /// 与运算
    /// </summary>
    /// <param name="other"></param>
    public void And(BitSet64 other) {
        this._bits &= other._bits;
    }

    /// <summary>
    /// 或运算
    /// </summary>
    /// <param name="other"></param>
    public void Or(BitSet64 other) {
        this._bits |= other._bits;
    }

    /// <summary>
    /// 异或
    /// </summary>
    /// <param name="other"></param>
    public void Xor(BitSet64 other) {
        this._bits ^= other._bits;
    }

    /// <summary>
    /// 与非
    /// </summary>
    /// <param name="other"></param>
    public void AndNot(BitSet64 other) {
        this._bits &= ~other._bits;
    }

    /// <summary>
    /// 是否相交
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool IsIntersect(BitSet64 other) {
        return (this._bits & other._bits) != 0;
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
        return _bits == other._bits;
    }

    public override bool Equals(object obj) {
        return ReferenceEquals(this, obj) || obj is BitSet64 other && Equals(other);
    }

    public override int GetHashCode() {
        return _bits.GetHashCode();
    }

    #endregion

    public override string ToString() {
        return _bits.ToString("X");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CheckIndex(int index) {
        if (index < 0 || index >= LENGTH) {
            throw new IndexOutOfRangeException($"length: {LENGTH}, index {index}");
        }
    }
}
}