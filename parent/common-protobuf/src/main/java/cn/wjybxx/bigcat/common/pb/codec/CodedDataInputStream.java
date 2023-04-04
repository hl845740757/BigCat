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

package cn.wjybxx.bigcat.common.pb.codec;

import com.google.protobuf.*;

import javax.annotation.Nonnull;
import java.io.IOException;

/**
 * {@link CodedInputStream}的封装，屏蔽转义一些接口。
 *
 * @author wjybxx
 * date 2023/4/1
 */
public abstract class CodedDataInputStream {

    /**
     * 缓存有助于性能
     */
    private static final ExtensionRegistryLite EMPTY_REGISTRY = ExtensionRegistryLite.getEmptyRegistry();

    public static CodedDataInputStream newInstance(@Nonnull byte[] buffer) {
        return new ArrayCodedDataInputStream(buffer);
    }

    public static CodedDataInputStream newInstance(byte[] buffer, int offset, int length) {
        return new ArrayCodedDataInputStream(buffer, offset, length);
    }

    public abstract byte readRawByte() throws IOException;

    public abstract int readInt32() throws IOException;

    public abstract long readInt64() throws IOException;

    public abstract float readFloat() throws IOException;

    public abstract double readDouble() throws IOException;

    public abstract boolean readBool() throws IOException;

    public abstract String readString() throws IOException;

    public abstract int readFixed32() throws IOException;

    public abstract long readFixed64() throws IOException;

    public abstract byte[] readRawBytes(int size) throws IOException;

    public abstract void readRawBytes(int size, byte[] out, int offset) throws IOException;

    /**
     * 读取一个protoBuf消息，内容部分没有size
     */
    public abstract <T> T readMessageNoSize(@Nonnull Parser<T> parser) throws IOException;

    /**
     * @return 已读取的字节数
     */
    public abstract int getTotalBytesRead();

    /**
     * 限制接下来可读取的字节数
     *
     * @param byteLimit 可用字节数
     * @return oldLimit 前一次设置的限制点
     */
    public abstract int pushLimit(int byteLimit) throws InvalidProtocolBufferException;

    public abstract void popLimit(int oldLimit);

    public abstract int getBytesUntilLimit();

    public abstract void skipRawBytes(int n) throws IOException;

    /**
     * @return 如果达到了当前限制，则返回true
     */
    public abstract boolean isAtEnd() throws IOException;

    private static class ArrayCodedDataInputStream extends CodedDataInputStream {

        final byte[] buffer;
        final int offset;
        final int limit;

        /** 由于{@link CodedOutputStream}不支持回退，我们在提供特殊api的时候就需要创建新的对象 */
        CodedInputStream codedInputStream;
        int codedInputStreamOffset;

        ArrayCodedDataInputStream(byte[] buffer) {
            this(buffer, 0, buffer.length);
        }

        ArrayCodedDataInputStream(byte[] buffer, int offset, int length) {
            this.buffer = buffer;
            this.offset = offset;
            this.limit = offset + length;

            this.codedInputStream = CodedInputStream.newInstance(buffer, offset, length);
            this.codedInputStreamOffset = offset;
        }

        @Override
        public byte readRawByte() throws IOException {
            return codedInputStream.readRawByte();
        }

        @Override
        public int readInt32() throws IOException {
            return codedInputStream.readInt32();
        }

        @Override
        public long readInt64() throws IOException {
            return codedInputStream.readInt64();
        }

        @Override
        public float readFloat() throws IOException {
            return codedInputStream.readFloat();
        }

        @Override
        public double readDouble() throws IOException {
            return codedInputStream.readDouble();
        }

        @Override
        public boolean readBool() throws IOException {
            return codedInputStream.readBool();
        }

        @Override
        public String readString() throws IOException {
            return codedInputStream.readString();
        }

        @Override
        public int readFixed32() throws IOException {
            return codedInputStream.readFixed32();
        }

        @Override
        public long readFixed64() throws IOException {
            return codedInputStream.readFixed64();
        }

        @Override
        public byte[] readRawBytes(int size) throws IOException {
            return codedInputStream.readRawBytes(size);
        }

        @Override
        public void readRawBytes(int size, byte[] out, int offset) throws IOException {
            final int totalBytesRead = getTotalBytesRead();
            codedInputStream.skipRawBytes(size);
            System.arraycopy(buffer, totalBytesRead, out, offset, size);
        }

        @Override
        public <T> T readMessageNoSize(@Nonnull Parser<T> parser) throws IOException {
            return parser.parseFrom(codedInputStream, EMPTY_REGISTRY);
        }

        @Override
        public int getTotalBytesRead() {
            return codedInputStream.getTotalBytesRead();
        }

        @Override
        public int pushLimit(int byteLimit) throws InvalidProtocolBufferException {
            return codedInputStream.pushLimit(byteLimit);
        }

        @Override
        public void popLimit(int oldLimit) {
            codedInputStream.popLimit(oldLimit);
        }

        @Override
        public int getBytesUntilLimit() {
            return codedInputStream.getBytesUntilLimit();
        }

        @Override
        public void skipRawBytes(int n) throws IOException {
            codedInputStream.skipRawBytes(n);
        }

        @Override
        public boolean isAtEnd() throws IOException {
            return codedInputStream.isAtEnd();
        }

    }

}