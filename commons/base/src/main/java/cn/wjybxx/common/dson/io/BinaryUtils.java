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

package cn.wjybxx.common.dson.io;

import cn.wjybxx.common.annotation.Internal;

import java.nio.Buffer;

/**
 * 修改自netty的HeapByteBufUtil
 *
 * @author wjybxx
 * date 2023/3/31
 */
@Internal
public class BinaryUtils {

    private BinaryUtils() {
    }

    /**
     * @param offset 数据的起始索引
     * @param length 数据的长度
     */
    public static void checkBuffer(byte[] buffer, int offset, int length) {
        if ((offset | length | (buffer.length - (offset + length))) < 0) {
            throw new IllegalArgumentException(
                    String.format(
                            "Array range is invalid. Buffer.length=%d, offset=%d, length=%d",
                            buffer.length, offset, length));
        }
    }

    /** JDK9+的代码跑在JDK8上的兼容问题 */
    public static void position(Buffer byteBuffer, int newOffset) {
        byteBuffer.position(newOffset);
    }

    public static int toUint8(byte value) {
        return (value & 0XFF);
    }

    /**
     * @param lower  读取的第一个字节
     * @param higher 读取的第二个字节
     */
    public static int toUint16(byte lower, byte higher) {
        return (lower & 0XFF)
                | (higher << 8);
    }

    public static int toUint16(int value) {
        return (value & 0XFFFF);
    }

    // region 大端编码

    public static byte getByte(byte[] buffer, int index) {
        return buffer[index];
    }

    public static void setByte(byte[] buffer, int index, int value) {
        buffer[index] = (byte) value;
    }

    public static void setShort(byte[] buffer, int index, int value) {
        buffer[index] = (byte) (value >>> 8);
        buffer[index + 1] = (byte) value;
    }

    public static short getShort(byte[] buffer, int index) {
        return (short) ((buffer[index] << 8)
                | (buffer[index + 1] & 0xff));
    }

    public static void setInt(byte[] buffer, int index, int value) {
        buffer[index] = (byte) (value >>> 24);
        buffer[index + 1] = (byte) (value >>> 16);
        buffer[index + 2] = (byte) (value >>> 8);
        buffer[index + 3] = (byte) value;
    }

    public static int getInt(byte[] buffer, int index) {
        return (((buffer[index] & 0xff) << 24)
                | ((buffer[index + 1] & 0xff) << 16)
                | ((buffer[index + 2] & 0xff) << 8)
                | ((buffer[index + 3] & 0xff)));
    }

    public static void setLong(byte[] buffer, int index, long value) {
        buffer[index] = (byte) (value >>> 56);
        buffer[index + 1] = (byte) (value >>> 48);
        buffer[index + 2] = (byte) (value >>> 40);
        buffer[index + 3] = (byte) (value >>> 32);
        buffer[index + 4] = (byte) (value >>> 24);
        buffer[index + 5] = (byte) (value >>> 16);
        buffer[index + 6] = (byte) (value >>> 8);
        buffer[index + 7] = (byte) value;
    }

    public static long getLong(byte[] buffer, int index) {
        return (((buffer[index] & 0xffL) << 56)
                | ((buffer[index + 1] & 0xffL) << 48)
                | ((buffer[index + 2] & 0xffL) << 40)
                | ((buffer[index + 3] & 0xffL) << 32)
                | ((buffer[index + 4] & 0xffL) << 24)
                | ((buffer[index + 5] & 0xffL) << 16)
                | ((buffer[index + 6] & 0xffL) << 8)
                | ((buffer[index + 7] & 0xffL)));
    }
    // endregion

    // region 小端编码

    public static void setShortLE(byte[] buffer, int index, int value) {
        buffer[index] = (byte) value;
        buffer[index + 1] = (byte) (value >>> 8);
    }

    public static short getShortLE(byte[] buffer, int index) {
        return (short) ((buffer[index] & 0xff)
                | (buffer[index + 1] << 8));
    }

    public static void setIntLE(byte[] buffer, int index, int value) {
        buffer[index] = (byte) value;
        buffer[index + 1] = (byte) (value >>> 8);
        buffer[index + 2] = (byte) (value >>> 16);
        buffer[index + 3] = (byte) (value >>> 24);
    }

    public static int getIntLE(byte[] buffer, int index) {
        return (((buffer[index] & 0xff))
                | ((buffer[index + 1] & 0xff) << 8)
                | ((buffer[index + 2] & 0xff) << 16)
                | ((buffer[index + 3] & 0xff) << 24));
    }

    public static void setLongLE(byte[] buffer, int index, long value) {
        buffer[index] = (byte) value;
        buffer[index + 1] = (byte) (value >>> 8);
        buffer[index + 2] = (byte) (value >>> 16);
        buffer[index + 3] = (byte) (value >>> 24);
        buffer[index + 4] = (byte) (value >>> 32);
        buffer[index + 5] = (byte) (value >>> 40);
        buffer[index + 6] = (byte) (value >>> 48);
        buffer[index + 7] = (byte) (value >>> 56);
    }

    public static long getLongLE(byte[] buffer, int index) {
        return (((buffer[index] & 0xffL))
                | ((buffer[index + 1] & 0xffL) << 8)
                | ((buffer[index + 2] & 0xffL) << 16)
                | ((buffer[index + 3] & 0xffL) << 24)
                | ((buffer[index + 4] & 0xffL) << 32)
                | ((buffer[index + 5] & 0xffL) << 40)
                | ((buffer[index + 6] & 0xffL) << 48)
                | ((buffer[index + 7] & 0xffL) << 56));
    }

    // endregion
}