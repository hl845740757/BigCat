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

/**
 * 二进制块
 * 默认编码length对应的部分
 *
 * @author wjybxx
 * date 2023/4/2
 */
public final class Chunk {

    private final byte[] buffer;
    /** 有效区域的起始偏移 */
    private int offset;
    /** 有效区域的长度 */
    private int length;
    /** 有效区域已使用长度 */
    private int used;

    public Chunk(byte[] buffer) {
        this(buffer, 0, buffer.length);
    }

    /**
     * @param offset 有效区域的起始偏移
     * @param length 有效区域的长度
     */
    public Chunk(byte[] buffer, int offset, int length) {
        BinaryUtils.checkBuffer(buffer, offset, length);
        this.buffer = buffer;
        this.offset = offset;
        this.length = length;
        this.used = 0;
    }

    /**
     * 重新设置块的有效载荷部分
     *
     * @param offset 有效部分的起始偏移量
     * @param length 有效部分的长度
     */
    public void setOffsetLength(int offset, int length) {
        BinaryUtils.checkBuffer(buffer, offset, length);
        this.offset = offset;
        this.length = length;
    }

    /** 设置已使用的块大小 */
    public void setUsed(int used) {
        if (used > length) {
            throw new IllegalArgumentException(String.format("used %d, length %d", used, length));
        }
        this.used = used;
    }
    //

    public byte[] getBuffer() {
        return buffer;
    }

    public int getOffset() {
        return offset;
    }

    public int getLength() {
        return length;
    }

    public int getUsed() {
        return used;
    }

}