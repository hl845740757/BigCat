#region LICENSE

// Copyright 2026 wjybxx(845740757@qq.com)
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
using Wjybxx.Dson;
using Wjybxx.Dson.Codec;
using Wjybxx.Dson.Codec.Attributes;

namespace Wjybxx.BigCat.Util
{
/// <summary>
/// 轻量级枚举集
/// </summary>
/// <typeparam name="T"></typeparam>
public class EnumSet64<T> where T : struct, Enum
{
    private const int LENGTH = 64;

    [DsonIgnore(false)]
    [DsonProperty(Getter = "Bits", Setter = "Bits")]
    private long _bits;

    public EnumSet64() {
    }

    public EnumSet64(EnumSet64<T> src) {
        this._bits = src._bits;
    }

    public bool this[T key] {
        get => Get(key.GetHashCode());
        set => Set(key.GetHashCode(), value);
    }
    public bool this[int key] {
        get => Get(key);
        set => Set(key, value);
    }

    #region enum-api

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Get(T key) => Get(key.GetHashCode());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(T key) => Set(key.GetHashCode());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Unset(T key) => Unset(key.GetHashCode());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(T key, bool val) {
        if (val) {
            Set(key.GetHashCode());
        } else {
            Unset(key.GetHashCode());
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(params T[] array) {
        foreach (T e in array) {
            Set(e.GetHashCode());
        }
    }

    #endregion

    #region int-api

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CheckIndex(int index) {
        if (index < 0 || index >= LENGTH) {
            throw new IndexOutOfRangeException($"length: {LENGTH}, index {index}");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Get(int index) {
        CheckIndex(index);
        return (_bits & (1L << index)) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(int index, bool value) {
        if (value) {
            Set(index);
        } else {
            Unset(index);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(int index) {
        CheckIndex(index);
        _bits |= (1L << index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Unset(int index) {
        CheckIndex(index);
        _bits &= ~(1L << index);
    }

    #endregion

    /// <summary>
    /// 所有的bit信息
    /// </summary>
    public long Bits {
        get => _bits;
        set => _bits = value;
    }
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
    /// 清空数据
    /// </summary>
    public void Clear() {
        _bits = 0;
    }

    /// <summary>
    /// 与运算
    /// </summary>
    /// <param name="other"></param>
    public void And(EnumSet64<T> other) {
        this._bits &= other._bits;
    }

    /// <summary>
    /// 或运算
    /// </summary>
    /// <param name="other"></param>
    public void Or(EnumSet64<T> other) {
        this._bits |= other._bits;
    }

    /// <summary>
    /// 异或
    /// </summary>
    /// <param name="other"></param>
    public void Xor(EnumSet64<T> other) {
        this._bits ^= other._bits;
    }

    /// <summary>
    /// 与非（即清除）
    /// </summary>
    /// <param name="other"></param>
    public void AndNot(EnumSet64<T> other) {
        this._bits &= ~other._bits;
    }

    /// <summary>
    /// 是否相交
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Intersect(EnumSet64<T> other) {
        return (this._bits & other._bits) != 0;
    }

    /// <summary>
    /// 是否相交
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Intersect(long other) {
        return (this._bits & other) != 0;
    }

    /// <summary>
    /// 内容取反
    /// </summary>
    public void Not() {
        this._bits = ~this._bits;
    }

    /// <summary>
    /// 拷贝数据
    /// </summary>
    /// <returns></returns>
    public EnumSet64<T> Copy() {
        return new EnumSet64<T>(this);
    }

    public override string ToString() {
        return _bits.ToString("X");
    }

    #region 序列化

    internal static EnumSet64<T> NewInstance(IDsonObjectReader reader) {
        DsonType firstDsonType = reader.ReadDsonType();
        if (firstDsonType == DsonType.EndOfObject) {
            return new EnumSet64<T>();
        }
        // 单值字符串数组 [A, B, C]
        if (firstDsonType == DsonType.String) {
            DsonCodecImpl<T> enumCodec = reader.GetInlinableCodec<T>();
            if (enumCodec == null) throw new AssertionError();
            //
            EnumSet64<T> result = new EnumSet64<T>();
            result.Set(enumCodec.DecodeKey(reader.ReadString()));
            while ((reader.ReadDsonType()) != DsonType.EndOfObject) {
                result.Set(enumCodec.DecodeKey(reader.ReadString()));
            }
            return result;
        }
        // 双int值数组 [A, B]
        long lowBits = reader.ReadInt();
        long highBits = reader.ReadInt();
        return new EnumSet64<T>()
        {
            Bits = highBits << 32 | lowBits
        };
    }

    internal void WriteObject(IDsonObjectWriter writer, SerializeFeatures features) {
        if ((features & SerializeFeatures.EnumAsString) != 0) {
            // 序列化为字符串数组，暂时暴力迭代 - 主要用于导出编辑器兼容数据
            DsonCodecImpl<T> keyCodec = writer.GetInlinableCodec<T>()!;
            for (int index = 0, end = 64; index < end; index++) {
                if (Get(index)) {
                    keyCodec.WriteObject(writer, keyCodec.ToObject(index), typeof(T), SerializeFeatures.EnumAsString);
                }
            }
            return;
        }
        const SerializeFeatures fixedHex = SerializeFeatures.NumberFixed | SerializeFeatures.NumberHex;
        int low = (int)_bits;
        int high = (int)(_bits >> 32);
        writer.WriteInt(low, fixedHex);
        writer.WriteInt(high, fixedHex);
    }

    #endregion
}
}