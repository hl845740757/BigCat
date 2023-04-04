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

package cn.wjybxx.bigcat.common.codec.binary;

import cn.wjybxx.bigcat.common.codec.TypeArgInfo;

import javax.annotation.Nonnull;

/**
 * 默认只编码有效部分
 * 未来倒是可以考虑提供是否完整写入的属性，现在，你也可以重写codec实现，在注册到{@link BinaryConverter}之前替换默认实现。
 *
 * @author wjybxx
 * date 2023/4/2
 */
public class Chunk {

    private final byte[] buffer;
    private int offset;
    private int length;
    private int used;

    public Chunk(byte[] buffer) {
        this(buffer, 0, buffer.length);
    }

    /**
     * @param offset 有效部分的起始偏移量
     * @param length 有效部分的长度
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
            throw new IllegalArgumentException();
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

    private static class Codec implements BinaryPojoCodecImpl<Chunk> {

        @Nonnull
        @Override
        public Class<Chunk> getEncoderClass() {
            return Chunk.class;
        }

        @Override
        public void writeObject(Chunk instance, BinaryWriter writer, TypeArgInfo<?> typeArgInfo) {
            writer.writeBytes(instance.getBuffer(), instance.getOffset(), instance.getLength());
            writer.writeInt(instance.getUsed());
        }

        @Override
        public Chunk readObject(BinaryReader reader, TypeArgInfo<?> typeArgInfo) {
            Chunk chunk = new Chunk(reader.readBytes());
            chunk.setUsed(reader.readInt());
            return chunk;
        }

    }

}