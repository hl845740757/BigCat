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
using System.Runtime.CompilerServices;
using Wjybxx.Commons;
using Wjybxx.Dson;
using Wjybxx.Dson.Codec;
using Wjybxx.Dson.Codec.Codecs;

namespace Wjybxx.BigCat.Util
{
/// <summary>
/// 虚拟的Enum，特殊场景下配合EnumSet使用
/// </summary>
public enum MockEnum : int
{
}

/// <summary>
/// 枚举比特集
///
/// 注：
/// 1.虽然内存中为long数组，但序列化为int数组格式；其目的是与编辑器导出数据对齐 —— 编辑器仅支持32位枚举。
/// 2.枚举类型需要为int32类型，其hashcode即为其number
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class EnumSet<T> where T : struct, Enum
{
    private long[] _values;

    public EnumSet() : this(128) {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="bitCount">期望的bit数</param>
    public EnumSet(int bitCount = 128) {
        if (bitCount < 0 || bitCount > MAX_COUNT) {
            throw new ArgumentException(nameof(bitCount));
        }
        _values = new long[WordCount(bitCount)];
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

    #endregion

    #region int-api

    public bool Get(int index) {
        if (index < 0 || index >= MAX_COUNT) {
            throw new IndexOutOfRangeException();
        }
        int wordIndex = WordIndex(index);
        return wordIndex < _values.Length
               && (_values[wordIndex] & 1L << index) != 0;
    }

    public void Set(int index) {
        if (index < 0 || index >= MAX_COUNT) {
            throw new IndexOutOfRangeException();
        }
        int wordIndex = WordIndex(index);
        if (wordIndex >= _values.Length) {
            Array.Resize(ref _values, wordIndex + 1);
        }
        _values[wordIndex] |= 1L << index;
    }

    public void Unset(int index) {
        if (index < 0 || index >= MAX_COUNT) {
            throw new IndexOutOfRangeException();
        }
        int wordIndex = WordIndex(index);
        if (wordIndex >= _values.Length) {
            return;
        }
        _values[wordIndex] &= ~(1L << index);
    }

    public void Set(int index, bool val) {
        if (val) {
            Set(index);
        } else {
            Unset(index);
        }
    }

    #endregion

    /// <summary>
    /// 清理所有bit
    /// </summary>
    public void Clear() {
        Array.Clear(_values, 0, _values.Length);
    }

    /// <summary>
    /// 比特集中的1数量
    /// </summary>
    public int BitCount {
        get {
            int r = 0;
            foreach (long element in _values) {
                r += MathCommon.BitCount(element);
            }
            return r;
        }
    }
    /// <summary>
    /// 比特集中的0数量
    /// </summary>
    public int ZeroCount {
        get {
            int capacity = _values.Length << ADDRESS_BITS_PER_WORD;
            return capacity - BitCount;
        }
    }
    /// <summary>
    /// 是否为空集
    /// </summary>
    public bool IsEmpty {
        get {
            foreach (long element in _values) {
                if (element != 0) return false;
            }
            return true;
        }
    }

    /// <summary>
    /// 与运算
    /// </summary>
    /// <param name="other"></param>
    public void And(EnumSet<T> other) {
        CheckLength(other);
        int minLen = Math.Min(_values.Length, other._values.Length);
        for (int i = 0; i < minLen; i++) {
            _values[i] &= other._values[i];
        }
    }

    /// <summary>
    /// 或运算
    /// </summary>
    /// <param name="other"></param>
    public void Or(EnumSet<T> other) {
        CheckLength(other);
        int minLen = Math.Min(_values.Length, other._values.Length);
        for (int i = 0; i < minLen; i++) {
            _values[i] |= other._values[i];
        }
    }

    /// <summary>
    /// 异或
    /// </summary>
    /// <param name="other"></param>
    public void Xor(EnumSet<T> other) {
        CheckLength(other);
        int minLen = Math.Min(_values.Length, other._values.Length);
        for (int i = 0; i < minLen; i++) {
            _values[i] ^= other._values[i];
        }
    }

    /// <summary>
    /// 与非（即清除）
    /// </summary>
    /// <param name="other"></param>
    public void AndNot(EnumSet<T> other) {
        CheckLength(other);
        int minLen = Math.Min(_values.Length, other._values.Length);
        for (int i = 0; i < minLen; i++) {
            _values[i] &= ~other._values[i];
        }
    }

    /// <summary>
    /// 是否相交
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Intersect(EnumSet<T> other) {
        int minLen = Math.Min(_values.Length, other._values.Length);
        for (int i = 0; i < minLen; i++) {
            if ((_values[i] & other._values[i]) != 0) {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 内容取反
    /// </summary>
    public void Not() {
        for (int idx = 0; idx < _values.Length; idx++) {
            _values[idx] = ~_values[idx];
        }
    }

    private void CheckLength(EnumSet<T> other) {
        int minLen = other._values.Length;
        if (_values.Length < minLen) {
            Array.Resize(ref _values, minLen);
        }
    }

    #region 序列化

    internal static EnumSet<T> NewInstance(IDsonObjectReader reader) {
        DsonType firstDsonType = reader.ReadDsonType();
        if (firstDsonType == DsonType.EndOfObject) {
            return new EnumSet<T>();
        }
        // 单值字符串数组 [A, B, C]
        if (firstDsonType == DsonType.String) {
            DsonCodecImpl<T> enumCodec = reader.GetInlinableCodec<T>();
            if (enumCodec == null) throw new AssertionError();
            //
            EnumSet<T> result = new EnumSet<T>();
            result.Set(enumCodec.DecodeKey(reader.ReadString()));
            while ((reader.ReadDsonType()) != DsonType.EndOfObject) {
                result.Set(enumCodec.DecodeKey(reader.ReadString()));
            }
            return result;
        }
        // flags数组格式
        List<int> tempList = new List<int>(8);
        tempList.Add(reader.ReadInt());
        while ((reader.ReadDsonType()) != DsonType.EndOfObject) {
            tempList.Add(reader.ReadInt());
        }
        int wordCount = tempList.Count;
        EnumSet<T> enumSet = new EnumSet<T>(wordCount * 32);
        for (int idx = 0; idx < wordCount; idx += 2) {
            long low = tempList[idx];
            long high = idx + 1 < wordCount ? tempList[idx + 1] : 0;
            enumSet._values[idx / 2] = (high << 32) | low;
        }
        return enumSet;
    }

    public static EnumSet<T> NewInstance(int[] bitArray) {
        int wordCount = bitArray.Length;
        EnumSet<T> enumSet = new EnumSet<T>(wordCount * 32);
        for (int idx = 0; idx < wordCount; idx += 2) {
            long low = bitArray[idx];
            long high = idx + 1 < wordCount ? bitArray[idx + 1] : 0;
            enumSet._values[idx / 2] = (high << 32) | low;
        }
        return enumSet;
    }

    internal void WriteObject(IDsonObjectWriter writer) {
        const SerializeFeatures fixedHex = SerializeFeatures.NumberFixed | SerializeFeatures.NumberHex;
        foreach (long element in _values) {
            int low = (int)element;
            int high = (int)(element >> 32);
            writer.WriteInt(low, fixedHex);
            writer.WriteInt(high, fixedHex);
        }
    }

    public int[] ToIntArray() {
        int[] result = new int[_values.Length * 2];
        for (int index = 0; index < _values.Length; index++) {
            long element = _values[index];
            int low = (int)element;
            int high = (int)(element >> 32);
            result[index * 2] = low;
            result[index * 2 + 1] = high;
        }
        return result;
    }

    #endregion

    #region internal

    private const int MAX_COUNT = 1024;
    private const int ADDRESS_BITS_PER_WORD = 6;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int WordIndex(int index) {
        return index >> ADDRESS_BITS_PER_WORD;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int WordCount(int bitCount) {
        return (bitCount >> ADDRESS_BITS_PER_WORD) + 1;
    }

    #endregion
}
}