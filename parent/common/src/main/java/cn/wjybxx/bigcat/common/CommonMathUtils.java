/*
 * Copyright 2023 wjybxx
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

package cn.wjybxx.bigcat.common;

import cn.wjybxx.bigcat.common.box.IntTuple2;
import cn.wjybxx.bigcat.common.box.ShortTuple2;
import com.google.common.math.IntMath;

/**
 * @author wjybxx
 * date 2023/3/31
 */
public class CommonMathUtils {

    protected CommonMathUtils() {
    }

    /**
     * 返回大于或等于x的2的最小次幂。
     */
    public static int ceilingPowerOfTwo(int x) {
        return IntMath.ceilingPowerOfTwo(x);
    }

    /**
     * 判断一个值是否是2的整次幂
     */
    public static boolean isPowerOfTwo(int value) {
        return IntMath.isPowerOfTwo(value);
    }

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

    public static IntTuple2 decomposeLongToInt(long value) {
        return new IntTuple2(higherIntOfLong(value), lowerIntOfLong(value));
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

    public static ShortTuple2 decomposeIntToShort(int value) {
        return new ShortTuple2(higherShortOfInt(value), lowerShortOfInt(value));
    }

    /**
     * 两个int安全相乘，返回一个long，避免越界；
     * 相乘之后再强转可能越界。
     *
     * @param a int
     * @param b int
     * @return long
     */
    public static long safeMultiplyToLong(int a, int b) {
        return ((long) a) * b;
    }

    /**
     * 两个short安全相乘，返回一个int，避免越界；
     * 相乘之后再强转可能越界。
     *
     * @param a short
     * @param b short
     * @return integer
     */
    public static int safeMultiplyToInt(short a, short b) {
        return ((int) a) * b;
    }

    public static int clamp(int v, int min, int max) {
        if (v < min) return min;
        if (v > max) return max;
        return v;
    }

    public static long clamp(long v, long min, long max) {
        if (v < min) return min;
        if (v > max) return max;
        return v;
    }

    public static int setEnable(int flags, int mask, boolean enable) {
        if (enable) {
            flags |= mask;
        } else {
            flags &= ~mask;
        }
        return flags;
    }

    public static long setEnable(long flags, long mask, boolean enable) {
        if (enable) {
            flags |= mask;
        } else {
            flags &= ~mask;
        }
        return flags;
    }

}