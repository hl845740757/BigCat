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
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Wjybxx.BigCat.Util
{
/// <summary>
/// 基于Int64的十进制定点数。
/// <para>真实值等于<see cref="RawValue"/>除以<see cref="Scale"/>，固定保留4位小数。</para>
/// </summary>
public readonly struct Fixed64 : IEquatable<Fixed64>, IComparable<Fixed64>
{
    /// <summary>
    /// 缩放因子，即定点数1.0的原始值。
    /// </summary>
    public const long Scale = 10_000L;

    /// <summary>
    /// 零值。
    /// </summary>
    public static readonly Fixed64 Zero = new Fixed64(0);
    /// <summary>
    /// 一值。
    /// </summary>
    public static readonly Fixed64 One = new Fixed64(Scale);
    /// <summary>
    /// 最小值，与最大值互为相反数。
    /// </summary>
    public static readonly Fixed64 MinValue = new Fixed64(long.MinValue + 1);
    /// <summary>
    /// 最大值。
    /// </summary>
    public static readonly Fixed64 MaxValue = new Fixed64(long.MaxValue);

    /// <summary>
    /// 定点数的原始整数值。
    /// </summary>
    public readonly long RawValue;

    private Fixed64(long rawValue) {
        if (rawValue == long.MinValue) throw new OverflowException();
        RawValue = rawValue;
    }

    /// <summary>
    /// 根据原始值创建定点数。
    /// </summary>
    /// <exception cref="OverflowException">rawValue为long.MinValue，超出可表示范围。</exception>
    public static Fixed64 FromRaw(long rawValue) {
        return new Fixed64(rawValue);
    }

    /// <summary>
    /// 根据整数创建定点数。
    /// </summary>
    /// <exception cref="OverflowException">结果超出可表示范围。</exception>
    public static Fixed64 FromInt64(long value) {
        return new Fixed64(checked(value * Scale));
    }

    /// <summary>
    /// 转换为整数，小数部分向零截断。
    /// </summary>
    public long ToInt64() {
        return RawValue / Scale;
    }

    /// <summary>
    /// 返回较小值。
    /// </summary>
    public static Fixed64 Min(Fixed64 x, Fixed64 y) {
        return x.RawValue <= y.RawValue ? x : y;
    }

    /// <summary>
    /// 返回较大值。
    /// </summary>
    public static Fixed64 Max(Fixed64 x, Fixed64 y) {
        return x.RawValue >= y.RawValue ? x : y;
    }

    /// <summary>
    /// 将值限制在指定区间内。
    /// </summary>
    /// <exception cref="ArgumentException">min大于max。</exception>
    public static Fixed64 Clamp(Fixed64 value, Fixed64 min, Fixed64 max) {
        if (min > max) throw new ArgumentException("min不能大于max");
        if (value < min) return min;
        return value > max ? max : value;
    }

    /// <summary>
    /// 计算平方根，结果向下取整到最接近的定点数。
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">value为负数。</exception>
    public static Fixed64 Sqrt(Fixed64 value) {
        if (value.RawValue < 0) throw new ArgumentOutOfRangeException(nameof(value), "不能计算负数的平方根");
        if (value.RawValue == 0) return Zero;

        UInt128Parts number = UInt128Parts.Multiply((ulong)value.RawValue, (ulong)Scale);
        ulong low = 0;
        // sqrt(long.MaxValue * Scale)小于2^40
        ulong high = 1UL << 40;
        while (low < high) {
            ulong mid = low + (high - low + 1) / 2;
            UInt128Parts square = UInt128Parts.Multiply(mid, mid);
            if (square.CompareTo(number) <= 0) {
                low = mid;
            } else {
                high = mid - 1;
            }
        }
        return new Fixed64((long)low);
    }

    public static Fixed64 operator +(Fixed64 value) {
        return value;
    }

    public static Fixed64 operator -(Fixed64 value) {
        return new Fixed64(-value.RawValue); // MinValue和MaxValue对称
    }

    /// <exception cref="OverflowException">结果超出可表示范围。</exception>
    public static Fixed64 operator +(Fixed64 left, Fixed64 right) {
        return new Fixed64(checked(left.RawValue + right.RawValue));
    }

    /// <exception cref="OverflowException">结果超出可表示范围。</exception>
    public static Fixed64 operator -(Fixed64 left, Fixed64 right) {
        return new Fixed64(checked(left.RawValue - right.RawValue));
    }

    /// <summary>
    /// 计算两个定点数的乘积，小数部分向零截断。
    /// </summary>
    /// <exception cref="OverflowException">结果超出可表示范围。</exception>
    public static Fixed64 operator *(Fixed64 left, Fixed64 right) {
        if (left.RawValue == 0 || right.RawValue == 0) return Zero;
        bool negative = (left.RawValue < 0) != (right.RawValue < 0);
        ulong x = GetMagnitude(left.RawValue);
        ulong y = GetMagnitude(right.RawValue);
        // 常用数值无需构造完整的128位乘积。
        ulong quotient = (x | y) <= uint.MaxValue
            ? x * y / (ulong)Scale
            : UInt128Parts.DivideByScale(UInt128Parts.Multiply(x, y));
        return new Fixed64(FromMagnitude(quotient, negative));
    }

    /// <summary>
    /// 计算两个定点数的商，小数部分向零截断。
    /// </summary>
    /// <exception cref="DivideByZeroException">right为零。</exception>
    /// <exception cref="OverflowException">结果超出可表示范围。</exception>
    public static Fixed64 operator /(Fixed64 left, Fixed64 right) {
        if (right.RawValue == 0) throw new DivideByZeroException();
        if (left.RawValue == 0) return Zero;
        bool negative = (left.RawValue < 0) != (right.RawValue < 0);
        ulong x = GetMagnitude(left.RawValue);
        ulong divisor = GetMagnitude(right.RawValue);
        ulong quotient = x <= ulong.MaxValue / (ulong)Scale
            ? x * (ulong)Scale / divisor
            : UInt128Parts.Divide(UInt128Parts.Multiply(x, (ulong)Scale), divisor);
        return new Fixed64(FromMagnitude(quotient, negative));
    }

    public bool Equals(Fixed64 other) {
        return RawValue == other.RawValue;
    }

    public int CompareTo(Fixed64 other) {
        return RawValue.CompareTo(other.RawValue);
    }

    public override bool Equals(object? obj) {
        return obj is Fixed64 other && Equals(other);
    }

    public override int GetHashCode() {
        return RawValue.GetHashCode();
    }

    public static bool operator ==(Fixed64 left, Fixed64 right) {
        return left.RawValue == right.RawValue;
    }

    public static bool operator !=(Fixed64 left, Fixed64 right) {
        return left.RawValue != right.RawValue;
    }

    public static bool operator <(Fixed64 left, Fixed64 right) {
        return left.RawValue < right.RawValue;
    }

    public static bool operator <=(Fixed64 left, Fixed64 right) {
        return left.RawValue <= right.RawValue;
    }

    public static bool operator >(Fixed64 left, Fixed64 right) {
        return left.RawValue > right.RawValue;
    }

    public static bool operator >=(Fixed64 left, Fixed64 right) {
        return left.RawValue >= right.RawValue;
    }

    public override string ToString() {
        ulong magnitude = GetMagnitude(RawValue);
        ulong integer = magnitude / (ulong)Scale;
        ulong fraction = magnitude % (ulong)Scale;
        string value = integer.ToString(CultureInfo.InvariantCulture) + "."
                       + fraction.ToString("D4", CultureInfo.InvariantCulture);
        return RawValue < 0 ? "-" + value : value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong GetMagnitude(long value) {
        return (ulong)Math.Abs(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long FromMagnitude(ulong value, bool negative) {
        if (value > long.MaxValue) throw new OverflowException();
        return negative ? -(long)value : (long)value;
    }

    /// <summary>
    /// 仅用于乘除和开方中间计算的无符号128位整数。
    /// </summary>
    private readonly struct UInt128Parts : IComparable<UInt128Parts>
    {
        private const ulong Mask32 = uint.MaxValue;

        public readonly ulong High;
        public readonly ulong Low;

        public UInt128Parts(ulong high, ulong low) {
            High = high;
            Low = low;
        }

        public int CompareTo(UInt128Parts other) {
            int r = High.CompareTo(other.High);
            return r != 0 ? r : Low.CompareTo(other.Low);
        }

        /// <summary>
        /// 计算两个无符号64位整数的完整乘积。
        /// </summary>
        public static UInt128Parts Multiply(ulong x, ulong y) {
            ulong x0 = x & Mask32;
            ulong x1 = x >> 32;
            ulong y0 = y & Mask32;
            ulong y1 = y >> 32;

            ulong w0 = x0 * y0;
            ulong t = x1 * y0 + (w0 >> 32);
            ulong w1 = t & Mask32;
            ulong w2 = t >> 32;
            w1 += x0 * y1;

            ulong high = x1 * y1 + w2 + (w1 >> 32);
            ulong low = (w1 << 32) | (w0 & Mask32);
            return new UInt128Parts(high, low);
        }

        /// <summary>
        /// 计算无符号128位整数除以缩放因子的64位商。
        /// </summary>
        public static ulong DivideByScale(UInt128Parts dividend) {
            if (dividend.High == 0) return dividend.Low / (ulong)Scale;
            if (dividend.High >= (ulong)Scale) throw new OverflowException();

            // 高半部小于除数，最高两个商位必为零，只需计算低两个32位商位。
            ulong partial = (dividend.High << 32) | (dividend.Low >> 32);
            ulong q1 = partial / (ulong)Scale;
            ulong remainder = partial - q1 * (ulong)Scale;
            partial = (remainder << 32) | (dividend.Low & Mask32);
            ulong q0 = partial / (ulong)Scale;
            return (q1 << 32) | q0;
        }

        /// <summary>
        /// 计算缩放后的原始值幅值除以非零除数的64位商，超出范围时抛出异常。
        /// </summary>
        public static ulong Divide(UInt128Parts dividend, ulong divisor) {
            if (dividend.High >= divisor) throw new OverflowException();
            if (divisor <= Mask32) {
                ulong partial = (dividend.High << 32) | (dividend.Low >> 32);
                ulong q1 = partial / divisor;
                ulong remainder = partial - q1 * divisor;
                partial = (remainder << 32) | (dividend.Low & Mask32);
                ulong q0 = partial / divisor;
                return (q1 << 32) | q0;
            }

            // Knuth除法的双商位特化：归一化后每个商位至多修正两次。
            int shift = LeadingZeroCount((uint)(divisor >> 32));
            ulong normalizedDivisor = divisor << shift;
            ulong high = shift == 0
                ? dividend.High
                : (dividend.High << shift) | (dividend.Low >> (64 - shift));
            ulong low = dividend.Low << shift;
            ulong divisorHigh = normalizedDivisor >> 32;
            ulong divisorLow = normalizedDivisor & Mask32;

            ulong quotientHigh = 0;
            if (high >= divisorHigh) {
                quotientHigh = DivideDigit(high, low >> 32, divisorHigh, divisorLow);
            }
            // 数学余数小于除数；中间的96位乘减按模2^64计算即可得到完整余数。
            ulong middle = unchecked((high << 32) + (low >> 32) - quotientHigh * normalizedDivisor);
            ulong quotientLow = DivideDigit(middle, low & Mask32, divisorHigh, divisorLow);
            return (quotientHigh << 32) | quotientLow;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong DivideDigit(ulong high, ulong low, ulong divisorHigh, ulong divisorLow) {
            const ulong Radix = 1UL << 32;
            ulong quotient = high / divisorHigh;
            ulong remainder = high - quotient * divisorHigh;
            // 初估可能为2^32甚至2^32+1，不能提前转换为uint。
            while (quotient >= Radix || quotient * divisorLow > (remainder << 32) + low) {
                quotient--;
                remainder += divisorHigh;
                if (remainder >= Radix) break;
            }
            return quotient;
        }

        /// <summary>
        /// 计算非零32位整数的前导零数，兼容Unity的基础类库。
        /// </summary>
        private static int LeadingZeroCount(uint value) {
            int count = 0;
            if (value < 0x10000) { count += 16; value <<= 16; }
            if (value < 0x1000000) { count += 8; value <<= 8; }
            if (value < 0x10000000) { count += 4; value <<= 4; }
            if (value < 0x40000000) { count += 2; value <<= 2; }
            if (value < 0x80000000) count++;
            return count;
        }
    }
}

}