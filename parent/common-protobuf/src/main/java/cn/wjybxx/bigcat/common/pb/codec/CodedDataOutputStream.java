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

import com.google.protobuf.CodedOutputStream;
import com.google.protobuf.MessageLite;

import java.io.IOException;

/**
 * 对{@link CodedOutputStream}的封装，屏蔽转义一些接口，以及扩展功能。
 *
 * @author wjybxx
 * date 2023/4/1
 */
public abstract class CodedDataOutputStream {

    public static CodedDataOutputStream newInstance(byte[] buffer) {
        return new ArrayCodedDataOutputStream(buffer);
    }

    public static CodedDataOutputStream newInstance(byte[] buffer, int offset, int length) {
        return new ArrayCodedDataOutputStream(buffer, offset, length);
    }

    public final void writeRawByte(int value) throws IOException {
        writeRawByte((byte) value);
    }

    public abstract void writeRawByte(byte value) throws IOException;

    public abstract void writeInt32(int value) throws IOException;

    public abstract void writeInt64(long value) throws IOException;

    public abstract void writeFloat(float value) throws IOException;

    public abstract void writeDouble(double value) throws IOException;

    public abstract void writeBool(boolean value) throws IOException;

    public abstract void writeString(String value) throws IOException;

    public abstract void writeFixed32(int value) throws IOException;

    public abstract void writeFixed64(long value) throws IOException;

    public final void writeRawBytes(byte[] value) throws IOException {
        writeRawBytes(value, 0, value.length);
    }

    public abstract void writeRawBytes(byte[] value, int offset, int length) throws IOException;

    /** 只写入消息的内容，不写入消息的长度 */
    public abstract void writeMessageNoSize(MessageLite value) throws IOException;

    /**
     * 在指定写索引处写如给定值。
     * PS: 这是我们封装protobuf的主要原因。。。
     *
     * @param writerIndex 写索引，与{@link #getTotalBytesWritten()}相关
     * @param value       要写入的值
     * @throws IOException error
     */
    public abstract void setFixedInt32(final int writerIndex, int value) throws IOException;

    /** @return 写入的字节数 */
    public abstract int getTotalBytesWritten();

    /** 刷新缓冲区 */
    public abstract void flush() throws IOException;

    private static class ArrayCodedDataOutputStream extends CodedDataOutputStream {

        private final byte[] buffer;
        private final int offset;
        private final int limit;

        /** 由于{@link CodedOutputStream}不支持回退，我们在提供特殊api的时候就需要创建新的对象 */
        CodedOutputStream codedOutputStream;
        int codedOutputStreamOffset;

        ArrayCodedDataOutputStream(byte[] buffer) {
            this(buffer, 0, buffer.length);
        }

        ArrayCodedDataOutputStream(byte[] buffer, int offset, int length) {
            if (offset >= buffer.length) {
                throw new IllegalArgumentException();
            }
            this.buffer = buffer;
            this.offset = offset;
            this.limit = offset + length;

            this.codedOutputStream = CodedOutputStream.newInstance(buffer, offset, length);
            this.codedOutputStreamOffset = offset;
        }

        @Override
        public void writeRawByte(byte value) throws IOException {
            codedOutputStream.writeRawByte(value);
        }

        @Override
        public void writeInt32(int value) throws IOException {
            codedOutputStream.writeInt32NoTag(value);
        }

        @Override
        public void writeFixed32(int value) throws IOException {
            codedOutputStream.writeFixed32NoTag(value);
        }

        @Override
        public void writeInt64(long value) throws IOException {
            codedOutputStream.writeInt64NoTag(value);
        }

        @Override
        public void writeFixed64(long value) throws IOException {
            codedOutputStream.writeFixed64NoTag(value);
        }

        @Override
        public void writeFloat(float value) throws IOException {
            codedOutputStream.writeFloatNoTag(value);
        }

        @Override
        public void writeDouble(double value) throws IOException {
            codedOutputStream.writeDoubleNoTag(value);
        }

        @Override
        public void writeBool(boolean value) throws IOException {
            codedOutputStream.writeBoolNoTag(value);
        }

        @Override
        public void writeString(String value) throws IOException {
            codedOutputStream.writeStringNoTag(value);
        }

        @Override
        public void writeRawBytes(byte[] value, int offset, int length) throws IOException {
            codedOutputStream.writeRawBytes(value, offset, length);
        }

        @Override
        public void writeMessageNoSize(MessageLite value) throws IOException {
            value.writeTo(codedOutputStream);
        }

        @Override
        public void setFixedInt32(final int writerIndex, int value) throws IOException {
            // 小端编码
            final int tempPos = offset + writerIndex;
            buffer[tempPos] = (byte) value;
            buffer[tempPos + 1] = (byte) (value >>> 8);
            buffer[tempPos + 2] = (byte) (value >>> 16);
            buffer[tempPos + 3] = (byte) (value >>> 24);
        }

        @Override
        public int getTotalBytesWritten() {
            return codedOutputStreamOffset - offset + codedOutputStream.getTotalBytesWritten();
        }

        @Override
        public void flush() throws IOException {
            codedOutputStream.flush();
        }

        @Override
        public String toString() {
            return "ArrayCodedDataOutputStream{" +
                    "arrayLength=" + buffer.length +
                    ", offset=" + offset +
                    ", limit=" + limit +
                    ", codedOutputStreamOffset=" + codedOutputStreamOffset +
                    ", codedOutputStreamTotalBytesWritten=" + codedOutputStream.getTotalBytesWritten() +
                    ", totalBytesWritten=" + getTotalBytesWritten() +
                    '}';
        }
    }

}
