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
using System.Numerics;
using NUnit.Framework;
using Wjybxx.BigCat.Util;

namespace Wjybxx.BigCat.Tests.Util
{
public class Fixed64Test
{
    [Test]
    public void TestFactoryAndCompare() {
        Assert.That(Fixed64.Scale, Is.EqualTo(10_000));
        Fixed64 value = Fixed64.FromRaw(12_345);
        Assert.That(value.RawValue, Is.EqualTo(12_345));
        Assert.That(value.ToInt64(), Is.EqualTo(1));
        Assert.That(Fixed64.FromInt64(-12).RawValue, Is.EqualTo(-120_000));
        Assert.That(Fixed64.One.RawValue, Is.EqualTo(Fixed64.Scale));
        Assert.That(Fixed64.FromRaw(-12_345).ToString(), Is.EqualTo("-1.2345"));
        Assert.That(Fixed64.MinValue.ToString(), Is.EqualTo("-922337203685477.5807"));
        Assert.That(Fixed64.MinValue.RawValue, Is.EqualTo(long.MinValue + 1));
        Assert.That(Math.Abs(Fixed64.MinValue.RawValue), Is.EqualTo(Fixed64.MaxValue.RawValue));
        Assert.Throws<OverflowException>(() => Fixed64.FromRaw(long.MinValue));

        Fixed64 left = Fixed64.FromRaw(-1);
        Fixed64 right = Fixed64.FromRaw(1);
        Assert.That(left, Is.LessThan(right));
        Assert.That(left == right, Is.False);
        Assert.That(left != right, Is.True);
        Assert.That(left.CompareTo(right), Is.LessThan(0));
        Assert.That(left.Equals(Fixed64.FromRaw(-1)), Is.True);
        Assert.That(left.GetHashCode(), Is.EqualTo(Fixed64.FromRaw(-1).GetHashCode()));

        Assert.Throws<OverflowException>(() => Fixed64.FromInt64(long.MaxValue / Fixed64.Scale + 1));
    }

    [Test]
    public void TestMinMaxClamp() {
        Fixed64 min = Fixed64.FromRaw(-100);
        Fixed64 mid = Fixed64.FromRaw(20);
        Fixed64 max = Fixed64.FromRaw(100);
        Assert.That(Fixed64.Min(min, max), Is.EqualTo(min));
        Assert.That(Fixed64.Max(min, max), Is.EqualTo(max));
        Assert.That(Fixed64.Clamp(min, min, max), Is.EqualTo(min));
        Assert.That(Fixed64.Clamp(mid, min, max), Is.EqualTo(mid));
        Assert.That(Fixed64.Clamp(max, min, max), Is.EqualTo(max));
        Assert.Throws<ArgumentException>(() => Fixed64.Clamp(mid, max, min));
    }

    [Test]
    public void TestAddSubtractAndNegate() {
        Fixed64 left = Fixed64.FromRaw(12_345);
        Fixed64 right = Fixed64.FromRaw(-2_345);
        Assert.That((left + right).RawValue, Is.EqualTo(10_000));
        Assert.That((left - right).RawValue, Is.EqualTo(14_690));
        Assert.That((-left).RawValue, Is.EqualTo(-12_345));
        Assert.That((+left).RawValue, Is.EqualTo(12_345));

        Assert.Throws<OverflowException>(() => _ = Fixed64.MaxValue + Fixed64.FromRaw(1));
        Assert.Throws<OverflowException>(() => _ = Fixed64.MinValue - Fixed64.FromRaw(1));
        Assert.Throws<OverflowException>(() => _ = Fixed64.MinValue + Fixed64.FromRaw(-1));
        Assert.That(-Fixed64.MinValue, Is.EqualTo(Fixed64.MaxValue));
        Assert.That(-Fixed64.MaxValue, Is.EqualTo(Fixed64.MinValue));
    }

    [Test]
    public void TestMultiplyAndDivide() {
        Assert.That((Fixed64.FromRaw(12_500) * Fixed64.FromRaw(18_000)).RawValue, Is.EqualTo(22_500));
        Assert.That((Fixed64.One / Fixed64.FromRaw(30_000)).RawValue, Is.EqualTo(3_333));
        Assert.That((Fixed64.One / Fixed64.FromRaw(-30_000)).RawValue, Is.EqualTo(-3_333));
        Assert.That((Fixed64.FromRaw(-10_000) / Fixed64.FromRaw(30_000)).RawValue, Is.EqualTo(-3_333));
        Assert.That((Fixed64.FromRaw(-10_000) / Fixed64.FromRaw(-30_000)).RawValue, Is.EqualTo(3_333));
        Assert.That((Fixed64.FromRaw(-1) * Fixed64.FromRaw(5_000)).RawValue, Is.EqualTo(-1));
        Assert.That((Fixed64.FromRaw(-3) * Fixed64.FromRaw(5_000)).RawValue, Is.EqualTo(-2));
        Assert.That((Fixed64.MaxValue * Fixed64.FromRaw(1)).RawValue, Is.EqualTo(long.MaxValue / Fixed64.Scale + 1));
        Assert.That((Fixed64.MinValue * Fixed64.FromRaw(1)).RawValue, Is.EqualTo(long.MinValue / Fixed64.Scale - 1));
        Assert.That((Fixed64.FromRaw(4_294_967_296L) * Fixed64.FromRaw(4_294_967_296L)).RawValue,
            Is.EqualTo(1_844_674_407_370_955L));
        Assert.Throws<DivideByZeroException>(() => _ = Fixed64.One / Fixed64.Zero);

        Fixed64 large = Fixed64.FromRaw(long.MaxValue);
        Assert.That((large * Fixed64.FromRaw(1)).RawValue, Is.EqualTo(long.MaxValue / Fixed64.Scale + 1));
        Assert.That((Fixed64.MaxValue / Fixed64.One).RawValue, Is.EqualTo(long.MaxValue));
        Assert.That((Fixed64.MaxValue * Fixed64.One).RawValue, Is.EqualTo(long.MaxValue));
        Assert.Throws<OverflowException>(() => _ = Fixed64.MaxValue * Fixed64.FromRaw(Fixed64.Scale + 1));
        Assert.Throws<OverflowException>(() => _ = Fixed64.MaxValue / Fixed64.FromRaw(1));
        Assert.Throws<DivideByZeroException>(() => _ = Fixed64.Zero / Fixed64.Zero);
    }

    [Test]
    public void TestMultiplyAndDivideByOracle() {
        long[] boundaryValues =
        {
            long.MinValue + 1, long.MinValue + 2, -1_000_000_000_000L, -10_000L, -1, 0, 1, 3, 9_999, 10_000,
            1_000_000_000_000L, long.MaxValue - 1, long.MaxValue
        };
        foreach (long left in boundaryValues) {
            foreach (long right in boundaryValues) {
                AssertOperation(left, right);
            }
        }

        Random random = new Random(20260914);
        byte[] bytes = new byte[8];
        for (int i = 0; i < 10_000; i++) {
            random.NextBytes(bytes);
            long left = BitConverter.ToInt64(bytes, 0);
            random.NextBytes(bytes);
            long right = BitConverter.ToInt64(bytes, 0);
            if (left == long.MinValue || right == long.MinValue) continue;
            AssertOperation(left, right);
        }
    }

    [Test]
    public void TestMultiplyMagnitudeBoundaries() {
        const long b = 1L << 32;
        long[] values = { b - 1, b, b + 1, 2 * b - 1, 2 * b, 2 * b + 1 };
        // 同时覆盖32位幅值边界及乘积落在2^64两侧的路径。
        foreach (long left in values) {
            foreach (long right in values) {
                AssertSigns(left, right, AssertMultiply);
            }
        }
        AssertSigns(long.MaxValue, Fixed64.Scale - 1, AssertMultiply);
        AssertSigns(long.MaxValue, Fixed64.Scale, AssertMultiply);
        AssertSigns(long.MaxValue, Fixed64.Scale + 1, AssertMultiply);
    }

    [TestCase(1L, 5_000L, 0L, 1L, 1L)]
    [TestCase(4_294_970_001L, 5_000L, 2_147_055_503L, 2_147_485_001L, 2_147_914_498L)]
    [TestCase(5_000_000_001L, 5_000_005_000L, 2_500_002_500_000_000L, 2_500_002_500_500_001L, 2_500_002_501_000_001L)]
    public void TestMultiplyRounding(long leftRaw, long midpoint, long below, long tie, long above) {
        // 覆盖快路径、宽乘法高半部为零及非零，余数分别为4999、5000和5001。
        long[] expectedValues = { below, tie, above };
        for (int offset = -1; offset <= 1; offset++) {
            long expected = expectedValues[offset + 1];
            AssertSigns(leftRaw, midpoint + offset, (left, right) => {
                long signedExpected = (left < 0) != (right < 0) ? -expected : expected;
                Assert.That((Fixed64.FromRaw(left) * Fixed64.FromRaw(right)).RawValue,
                    Is.EqualTo(signedExpected), $"multiply({left}, {right})");
                AssertMultiply(left, right);
            });
        }
    }

    [Test]
    public void TestMultiplyRoundingOverflow() {
        const long leftRaw = 9_222_449_791_875_588_249L;
        const long rightRaw = 10_001;
        // 截断商恰为MaxValue，余数8249触发进位溢出。
        AssertSigns(leftRaw, rightRaw, (left, right) => {
            Assert.Throws<OverflowException>(() => _ = Fixed64.FromRaw(left) * Fixed64.FromRaw(right));
            AssertMultiply(left, right);
        });
        AssertSigns(leftRaw - 1, rightRaw, (left, right) => {
            Fixed64 expected = (left < 0) != (right < 0) ? Fixed64.MinValue : Fixed64.MaxValue;
            Assert.That(Fixed64.FromRaw(left) * Fixed64.FromRaw(right), Is.EqualTo(expected));
            AssertMultiply(left, right);
        });
    }

    [Test]
    public void TestDivideMagnitudeBoundaries() {
        const long b = 1L << 32;
        const long threshold = (long)(ulong.MaxValue / 10_000UL);
        long[] dividends = { 0, 1, threshold - 1, threshold, threshold + 1, long.MaxValue };
        long[] divisors = { 1, 9_999, 10_000, b - 1, b, b + 1, (1L << 45) - 1, 1L << 45, (1L << 45) + 1, long.MaxValue };
        // 被除数缩放前后的64位边界，以及单字除法和宽除法的分支边界。
        foreach (long dividend in dividends) {
            foreach (long divisor in divisors) {
                AssertSigns(dividend, divisor, AssertDivide);
            }
        }
    }

    [TestCase(2L, 40_000L, 0L)]
    [TestCase(2_000_000_000_000_002L, 40_000L, 500_000_000_000_000L)]
    [TestCase(2_251_799_813_947_392L, 5_242_880_000L, 4_294_967_296L)]
    [TestCase(2_000_400_000_000_000_000L, 8_000_000_000_000_000_000L, 2_500L)]
    public void TestDivideRounding(long midpoint, long divisor, long truncated) {
        // 覆盖快路径、宽分子单字除法及Knuth归一化左移31位和1位。
        for (int offset = -1; offset <= 1; offset++) {
            long expected = offset < 0 ? truncated : truncated + 1;
            AssertSigns(midpoint + offset, divisor, (left, right) => {
                long signedExpected = (left < 0) != (right < 0) ? -expected : expected;
                Assert.That((Fixed64.FromRaw(left) / Fixed64.FromRaw(right)).RawValue,
                    Is.EqualTo(signedExpected), $"divide({left}, {right})");
                AssertDivide(left, right);
            });
        }
    }

    [Test]
    public void TestDivideRoundingOverflow() {
        const long leftRaw = 9_222_449_699_651_090_330L;
        const long divisor = 9_999;
        // 截断商恰为MaxValue，但余数超过半数，进位后越界。
        AssertSigns(leftRaw, divisor, (left, right) => {
            Assert.Throws<OverflowException>(() => _ = Fixed64.FromRaw(left) / Fixed64.FromRaw(right));
            AssertDivide(left, right);
        });
        // 相邻输入进位后恰好到达合法幅值上界。
        AssertSigns(leftRaw - 1, divisor, (left, right) => {
            Fixed64 expected = (left < 0) != (right < 0) ? Fixed64.MinValue : Fixed64.MaxValue;
            Assert.That(Fixed64.FromRaw(left) / Fixed64.FromRaw(right), Is.EqualTo(expected));
            AssertDivide(left, right);
        });
    }

    [Test]
    public void TestDivideNormalization() {
        // 合法除数高32位归一化的左移量遍历1..31；幅值2^63已被排除。
        for (int s = 0; s <= 31; s++) {
            long divisor = long.MaxValue >> s;
            AssertSigns(long.MaxValue, divisor, AssertDivide);
            AssertSigns(long.MaxValue, divisor - 1, AssertDivide);
            if (s != 0) {
                AssertSigns(long.MaxValue, divisor + 1, AssertDivide);
            }
        }
    }

    [Test]
    public void TestDivideQuotientCorrection() {
        const long b = 1L << 32;
        // 商恰好落在目标值下方，触发估商过大时的修正；构造过程不能先溢出。
        long[] divisors = { b + 1, 2 * b + 3 };
        foreach (long divisor in divisors) {
            long dividend = (long)(((BigInteger)divisor * b - 1) / Fixed64.Scale);
            AssertSigns(dividend, divisor, AssertDivide);
            dividend = (long)(((BigInteger)divisor * (b - 3) - 1) / Fixed64.Scale);
            AssertSigns(dividend, divisor, AssertDivide);
        }
    }

    [Test]
    public void TestMinValueSignBoundaries() {
        long minRaw = Fixed64.MinValue.RawValue;
        long[] values = { minRaw, minRaw + 1, -10_001, -10_000, -9_999, -1, 0, 1, 9_999, 10_000, 10_001, long.MaxValue };
        foreach (long value in values) {
            AssertOperation(minRaw, value);
            AssertOperation(value, minRaw);
        }
        Assert.That(Fixed64.MinValue * Fixed64.One, Is.EqualTo(Fixed64.MinValue));
        Assert.That(Fixed64.MinValue / Fixed64.One, Is.EqualTo(Fixed64.MinValue));
        Assert.That(Fixed64.MinValue * -Fixed64.One, Is.EqualTo(Fixed64.MaxValue));
        Assert.That(Fixed64.MinValue / -Fixed64.One, Is.EqualTo(Fixed64.MaxValue));
        // 运算结果恰好为long.MinValue时，Int64本身未溢出，但已超出定点数范围。
        Fixed64 halfMin = Fixed64.FromRaw(long.MinValue / 2);
        Assert.Throws<OverflowException>(() => _ = halfMin * Fixed64.FromInt64(2));
        Assert.Throws<OverflowException>(() => _ = halfMin / Fixed64.FromRaw(Fixed64.Scale / 2));
    }

    [Test]
    public void TestStratifiedRandomOperations() {
        const long b = 1L << 32;
        const long threshold = (long)(ulong.MaxValue / 10_000UL);
        Random random = new Random(20260915);
        byte[] bytes = new byte[8];
        for (int i = 0; i < 1_024; i++) {
            long left = NextMagnitude(random, bytes);
            long right = NextMagnitude(random, bytes);
            // 避免全范围随机乘法几乎全部溢出：窄乘法、必经宽路径的合法乘法、全幅值乘小数。
            AssertSigns(left % b, right % b, AssertMultiply);
            AssertSigns(b + left % ((1L << 38) - b), b + right % ((1L << 38) - b), AssertMultiply);
            AssertSigns(left, right % (Fixed64.Scale + 1), AssertMultiply);
            // 在合法乘积上界附近采样，兼顾宽路径的合法值和相邻溢出值。
            long wideLeft = b + left % (long.MaxValue - b);
            long maxRight = (long)((BigInteger)long.MaxValue * Fixed64.Scale / wideLeft);
            AssertSigns(wideLeft, maxRight, AssertMultiply);
            AssertSigns(wideLeft, maxRight + 1, AssertMultiply);

            long wideDividend = threshold + 1 + left % (long.MaxValue - threshold);
            // 缩放能落入64位，以及宽被除数下三种除数宽度；每层均覆盖四种符号。
            AssertSigns(left % (threshold + 1), 1 + right % long.MaxValue, AssertDivide);
            AssertSigns(wideDividend, 1 + right % (b - 1), AssertDivide);
            AssertSigns(wideDividend, b + right % ((1L << 45) - b), AssertDivide);
            AssertSigns(wideDividend, (1L << 45) + right % (long.MaxValue - (1L << 45)), AssertDivide);
            // 小商和小除数同时采样，补充舍入至零与结果溢出的情况。
            AssertSigns(left % Fixed64.Scale, b + right % (long.MaxValue - b), AssertDivide);
            AssertSigns(wideDividend, 1 + right % Fixed64.Scale, AssertDivide);
        }
    }

    [Test]
    public void TestSqrt() {
        Assert.That(Fixed64.Sqrt(Fixed64.Zero), Is.EqualTo(Fixed64.Zero));
        Assert.That(Fixed64.Sqrt(Fixed64.FromRaw(10_000)).RawValue, Is.EqualTo(10_000));
        Assert.That(Fixed64.Sqrt(Fixed64.FromRaw(40_000)).RawValue, Is.EqualTo(20_000));
        Assert.That(Fixed64.Sqrt(Fixed64.FromRaw(22_500)).RawValue, Is.EqualTo(15_000));
        Assert.That(Fixed64.Sqrt(Fixed64.FromRaw(20_000)).RawValue, Is.EqualTo(14_142));
        Assert.That(Fixed64.Sqrt(Fixed64.FromRaw(1)).RawValue, Is.EqualTo(100));
        Assert.Throws<ArgumentOutOfRangeException>(() => Fixed64.Sqrt(Fixed64.FromRaw(-1)));

        long[] values = { 0, 1, 2, 3, 9_999, 10_000, 20_000, 22_500, 40_000, long.MaxValue };
        foreach (long rawValue in values) {
            AssertSqrt(rawValue);
        }
        Random random = new Random(20260916);
        byte[] bytes = new byte[8];
        for (int i = 0; i < 2_048; i++) {
            long rawValue = NextMagnitude(random, bytes);
            AssertSqrt(rawValue);
            AssertSqrt(rawValue % (1L << 32));
        }
    }

    private static void AssertSqrt(long rawValue) {
        long root = Fixed64.Sqrt(Fixed64.FromRaw(rawValue)).RawValue;
        BigInteger number = (BigInteger)rawValue * Fixed64.Scale;
        BigInteger nextRoot = (BigInteger)root + 1;
        Assert.That(root, Is.GreaterThanOrEqualTo(0), $"sqrt({rawValue})");
        Assert.That((BigInteger)root * root, Is.LessThanOrEqualTo(number), $"sqrt({rawValue})下界");
        Assert.That(nextRoot * nextRoot, Is.GreaterThan(number), $"sqrt({rawValue})上界");
    }

    private static long NextMagnitude(Random random, byte[] bytes) {
        random.NextBytes(bytes);
        return BitConverter.ToInt64(bytes, 0) & long.MaxValue;
    }

    /// <summary>
    /// 两个非负幅值的四种符号组合，MinValue由单独用例覆盖。
    /// </summary>
    private static void AssertSigns(long left, long right, Action<long, long> assertion) {
        assertion(left, right);
        assertion(left, -right);
        assertion(-left, right);
        assertion(-left, -right);
    }

    private static void AssertOperation(long leftRaw, long rightRaw) {
        AssertMultiply(leftRaw, rightRaw);
        AssertDivide(leftRaw, rightRaw);
    }

    /// <summary>
    /// 先以无限精度运算并按半数远离零舍入，仅最终结果越界时才应抛出异常。
    /// </summary>
    private static void AssertMultiply(long leftRaw, long rightRaw) {
        string message = $"multiply({leftRaw}, {rightRaw})";
        BigInteger product = BigInteger.DivRem((BigInteger)leftRaw * rightRaw, Fixed64.Scale, out BigInteger remainder);
        if (BigInteger.Abs(remainder) * 2 >= Fixed64.Scale) {
            product += (leftRaw < 0) != (rightRaw < 0) ? -1 : 1;
        }
        if (product > long.MinValue && product <= long.MaxValue) {
            Assert.That((Fixed64.FromRaw(leftRaw) * Fixed64.FromRaw(rightRaw)).RawValue, Is.EqualTo((long)product), message);
        } else {
            Assert.Throws<OverflowException>(() => _ = Fixed64.FromRaw(leftRaw) * Fixed64.FromRaw(rightRaw), message);
        }
    }

    /// <summary>
    /// 缩放与除法全部使用无限精度整数，独立校验结果及除零、溢出异常。
    /// </summary>
    private static void AssertDivide(long leftRaw, long rightRaw) {
        string message = $"divide({leftRaw}, {rightRaw})";
        if (rightRaw == 0) {
            Assert.Throws<DivideByZeroException>(() => _ = Fixed64.FromRaw(leftRaw) / Fixed64.FromRaw(rightRaw), message);
            return;
        }
        BigInteger quotient = BigInteger.DivRem((BigInteger)leftRaw * Fixed64.Scale, rightRaw, out BigInteger remainder);
        if (BigInteger.Abs(remainder) * 2 >= BigInteger.Abs(rightRaw)) {
            quotient += (leftRaw < 0) != (rightRaw < 0) ? -1 : 1;
        }
        if (quotient > long.MinValue && quotient <= long.MaxValue) {
            Assert.That((Fixed64.FromRaw(leftRaw) / Fixed64.FromRaw(rightRaw)).RawValue, Is.EqualTo((long)quotient), message);
        } else {
            Assert.Throws<OverflowException>(() => _ = Fixed64.FromRaw(leftRaw) / Fixed64.FromRaw(rightRaw), message);
        }
    }
}
}