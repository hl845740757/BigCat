/*
 * Copyright 2023-2024 wjybxx(845740757@qq.com)
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

import it.unimi.dsi.fastutil.ints.IntArrayList;

/**
 * @author wjybxx
 * date - 2023/4/17
 */
public class Bits {

    //-- mask

    public static boolean isSet(int flags, int mask) {
        return (flags & mask) == mask;
    }

    public static boolean isSet(long flags, long mask) {
        return (flags & mask) == mask;
    }

    public static boolean isAnySet(int flags, int mask) {
        return (flags & mask) != 0;
    }

    public static boolean isAnySet(long flags, long mask) {
        return (flags & mask) != 0;
    }

    public static boolean isNotSet(int flags, int mask) {
        return (flags & mask) == 0;
    }

    public static boolean isNotSet(long flags, long mask) {
        return (flags & mask) == 0;
    }

    public static int set(int flags, int mask, boolean enable) {
        return enable ? (flags | mask) : (flags & ~mask);
    }

    public static long set(long flags, long mask, boolean enable) {
        return enable ? (flags | mask) : (flags & ~mask);
    }

    //-- index

    public static boolean isSetAt(int flags, int idx) {
        return isSet(flags, 1 << idx);
    }

    public static boolean isSetAt(long flags, int idx) {
        return isSet(flags, 1L << idx);
    }

    public static boolean isNotSetAt(int flags, int idx) {
        return isNotSet(flags, 1 << idx);
    }

    public static boolean isNotSetAt(long flags, int idx) {
        return isNotSet(flags, 1L << idx);
    }

    public static int setAt(int flags, int idx, boolean enable) {
        return set(flags, 1 << idx, enable);
    }

    public static long setAt(long flags, int idx, boolean enable) {
        return set(flags, 1L << idx, enable);
    }

    // endregion

    /** @param indexArray bit位为1的元素下标的数组 */
    public static long indexArray2Bits(int[] indexArray) {
        long r = 0;
        for (int idx : indexArray) {
            r |= (1L << idx);
        }
        return r;
    }

    /** @return bit位为1的元素下标的数组 */
    public static int[] bits2IndexArray(long bits) {
        IntArrayList list = new IntArrayList(8);
        for (int idx = 0; idx < 64; idx++) {
            if ((bits & (1L << idx)) != 0) {
                list.add(idx);
            }
        }
        return list.toIntArray();
    }

    /** @return bit位为1的元素下标的数组 */
    public static int[] bits2IndexArray(int bits) {
        IntArrayList list = new IntArrayList(8);
        for (int idx = 0; idx < 32; idx++) {
            if ((bits & (1 << idx)) != 0) {
                list.add(idx);
            }
        }
        return list.toIntArray();
    }

}