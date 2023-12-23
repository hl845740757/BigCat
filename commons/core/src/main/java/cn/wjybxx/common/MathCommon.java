/*
 * Copyright 2023 wjybxx(845740757@qq.com)
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

package cn.wjybxx.common;

/**
 * @author wjybxx
 * date 2023/3/31
 */
@SuppressWarnings("unused")
public class MathCommon {

    public static final int MAX_POWER_OF_TWO = 1 << 30;
    public static final long LONG_MAX_POWER_OF_TWO = 1L << 62;
    public static final float FLOAT_ROUNDING_ERROR = 0.00001f;
    public static final float DOUBLE_ROUNDING_ERROR = 0.000000001f;

    public static final int INT_MASK4 = 0xFF_00_00_00;
    public static final int INT_MASK3 = 0x00_FF_00_00;
    public static final int INT_MASK2 = 0x00_00_FF_00;
    public static final int INT_MASK1 = 0x00_00_00_FF;

    public static final long LONG_MASK8 = 0xFF_00_00_00_00_00_00_00L;
    public static final long LONG_MASK7 = 0x00_FF_00_00_00_00_00_00L;
    public static final long LONG_MASK6 = 0x00_00_FF_00_00_00_00_00L;
    public static final long LONG_MASK5 = 0x00_00_00_FF_00_00_00_00L;
    public static final long LONG_MASK4 = 0x00_00_00_00_FF_00_00_00L;
    public static final long LONG_MASK3 = 0x00_00_00_00_00_FF_00_00L;
    public static final long LONG_MASK2 = 0x00_00_00_00_00_00_FF_00L;
    public static final long LONG_MASK1 = 0x00_00_00_00_00_00_00_FFL;

    protected MathCommon() {
    }

    /** 判断一个值是否是2的整次幂 */
    public static boolean isPowerOfTwo(int x) {
        return x > 0 && (x & (x - 1)) == 0;
    }

    /** 计算num最接近下一个整2次幂；如果自身是2的整次幂，则会返回自身 */
    public static int nextPowerOfTwo(int num) {
        if (num < 1) return 1;
        return 1 << (32 - Integer.numberOfLeadingZeros(num - 1));
    }

    /** 计算num最接近下一个整2次幂；如果自身是2的整次幂，则会返回自身 */
    public static long nextPowerOfTwo(long num) {
        if (num < 1) return 1;
        return 1L << (64 - Long.numberOfLeadingZeros(num - 1));
    }

    /** @return 如果给定参数是【偶数】则返回true */
    public static boolean isEven(final int x) {
        return (x & 1) == 0;
    }

    /** @return 如果给定参数是【奇数】则返回true */
    public static boolean isOdd(final int x) {
        return (x & 1) == 1;
    }

    // region 聚合拆解

    /**
     * 将两个int聚合为long
     *
     * @param higher 高32位
     * @param lower  低32位
     * @return long
     */
    public static long composeIntToLong(int higher, int lower) {
        // 保留b符号扩充以后的低32位
        return ((long) higher << 32) | ((long) lower & 0xFF_FF_FF_FFL);
    }

    public static int higherIntOfLong(long value) {
        return (int) (value >>> 32);
    }

    public static int lowerIntOfLong(long value) {
        return (int) value;
    }

    /**
     * 将两个short聚合为int
     *
     * @param higher 高16位
     * @param lower  低16位
     * @return int
     */
    public static int composeShortToInt(short higher, short lower) {
        // 保留b符号扩充以后的低16位
        return ((int) higher << 16) | ((int) lower & 0xFF_FF);
    }

    public static short higherShortOfInt(int value) {
        return (short) (value >>> 16);
    }

    public static short lowerShortOfInt(int value) {
        return (short) value;
    }

    /** 两个int安全相乘，返回一个long，避免越界；相乘之后再强转可能越界。 */
    public static long multiplyToLong(int a, int b) {
        return ((long) a) * b;
    }

    /** 两个short安全相乘，返回一个int，避免越界；相乘之后再强转可能越界. */
    public static int multiplyToInt(short a, short b) {
        return ((int) a) * b;
    }
    // endregion

    // region clamp
    public static int clamp(int value, int min, int max) {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    public static long clamp(long value, long min, long max) {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    public static float clamp(float value, float min, float max) {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    public static double clamp(double value, double min, double max) {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    public static float clamp01(float value) {
        if (value <= 0f) return 0f;
        if (value >= 1f) return 1f;
        return value;
    }

    public static double clamp01(double value) {
        if (value <= 0d) return 0d;
        if (value >= 1d) return 1d;
        return value;
    }

    // endregion

    // region min,max

    public static int min(int a, int b, int c) {
        if (a > b) a = b;
        if (a > c) a = c;
        return a;
    }

    public static int max(int a, int b, int c) {
        if (a < b) a = b;
        if (a < c) a = c;
        return a;
    }

    public static long min(long a, long b, long c) {
        if (a > b) a = b;
        if (a > c) a = c;
        return a;
    }

    public static long max(long a, long b, long c) {
        if (a < b) a = b;
        if (a < c) a = c;
        return a;
    }

    public static float min(float a, float b, float c) {
        float r = Math.min(a, b);
        return Math.min(r, c);
    }

    public static float max(float a, float b, float c) {
        float r = Math.max(a, b);
        return Math.max(r, c);
    }

    public static double min(double a, double b, double c) {
        double r = Math.min(a, b);
        return Math.min(r, c);
    }

    public static double max(double a, double b, double c) {
        double r = Math.max(a, b);
        return Math.max(r, c);
    }

    public static int min(short a, short b) {
        return a < b ? a : b;
    }

    public static int max(short a, short b) {
        return a > b ? a : b;
    }

    public static int min(short a, short b, short c) {
        if (a > b) a = b;
        if (a > c) a = c;
        return a;
    }

    public static int max(short a, short b, short c) {
        if (a < b) a = b;
        if (a < c) a = c;
        return a;
    }

    // endregion

    // region 比较

    public static boolean isBetween(short value, short min, short max) {
        return value >= min && value <= max;
    }

    public static boolean isBetween(int value, int min, int max) {
        return value >= min && value <= max;
    }

    public static boolean isBetween(long value, long min, long max) {
        return value >= min && value <= max;
    }

    public static boolean isBetween(float value, float min, float max) {
        return value >= min && value <= max; // 这里不使用isEqual比较，避免越界
    }

    public static boolean isBetween(double value, double min, double max) {
        return value >= min && value <= max; // 这里不使用isEqual比较，避免越界
    }
    //

    public static boolean isZero(float value) {
        return Math.abs(value) <= FLOAT_ROUNDING_ERROR;
    }

    public static boolean isZero(float value, float tolerance) {
        return Math.abs(value) <= tolerance;
    }

    public static boolean isEqual(float a, float b) {
        return Math.abs(a - b) <= FLOAT_ROUNDING_ERROR;
    }

    public static boolean isEqual(float a, float b, float tolerance) {
        return Math.abs(a - b) <= tolerance;
    }

    //

    public static boolean isZero(double value) {
        return Math.abs(value) <= DOUBLE_ROUNDING_ERROR;
    }

    public static boolean isZero(double value, double tolerance) {
        return Math.abs(value) <= tolerance;
    }

    public static boolean isEqual(double a, double b) {
        return Math.abs(a - b) <= DOUBLE_ROUNDING_ERROR;
    }

    public static boolean isEqual(double a, double b, double tolerance) {
        return Math.abs(a - b) <= tolerance;
    }

    // endregion

}