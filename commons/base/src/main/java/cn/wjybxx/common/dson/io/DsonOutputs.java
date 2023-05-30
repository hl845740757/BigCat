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

import cn.wjybxx.common.dson.DsonCodecException;
import com.google.protobuf.CodedOutputStream;
import com.google.protobuf.MessageLite;

import java.io.IOException;
import java.nio.ByteBuffer;
import java.nio.ByteOrder;
import java.util.Objects;

/**
 * java的受检异常太恼火了，应当减少使用，对于正确性也没太大的帮助，但对接口的破坏太凶了。
 *
 * @author wjybxx
 * date - 2023/4/22
 */
public class DsonOutputs {

    public static DsonOutput newInstance(byte[] buffer) {
        return new ArrayOutput(buffer);
    }

    public static DsonOutput newInstance(byte[] buffer, int offset, int length) {
        return new ArrayOutput(buffer, offset, length);
    }

    public static DsonOutput newInstance(ByteBuffer byteBuffer) {
        return new ByteBufferOutput(byteBuffer);
    }

    static abstract class CodedOutput implements DsonOutput {

        /** 由于{@link CodedOutputStream}不支持回退和直接设置位置，因此我们调整位置时需要创建新的对象 */
        CodedOutputStream codedOutputStream;
        int codedOutputStreamOffset;

        @Override
        public void writeRawByte(int value) {
            try {
                codedOutputStream.writeRawByte((byte) value);
            } catch (IOException e) {
                throw DsonCodecException.wrap(e);
            }
        }

        @Override
        public void writeRawByte(byte value) {
            try {
                codedOutputStream.writeRawByte(value);
            } catch (IOException e) {
                throw DsonCodecException.wrap(e);
            }
        }

        @Override
        public void writeInt32(int value) {
            try {
                codedOutputStream.writeInt32NoTag(value);
            } catch (IOException e) {
                throw DsonCodecException.wrap(e);
            }
        }

        @Override
        public void writeUint32(int value) {
            try {
                codedOutputStream.writeUInt32NoTag(value);
            } catch (IOException e) {
                throw DsonCodecException.wrap(e);
            }
        }

        @Override
        public void writeSint32(int value) {
            try {
                codedOutputStream.writeSInt32NoTag(value);
            } catch (IOException e) {
                throw DsonCodecException.wrap(e);
            }
        }

        @Override
        public void writeFixed32(int value) {
            try {
                codedOutputStream.writeFixed32NoTag(value);
            } catch (IOException e) {
                throw DsonCodecException.wrap(e);
            }
        }
        //

        @Override
        public void writeInt64(long value) {
            try {
                codedOutputStream.writeInt64NoTag(value);
            } catch (IOException e) {
                throw DsonCodecException.wrap(e);
            }
        }

        @Override
        public void writeUint64(long value) {
            try {
                codedOutputStream.writeUInt64NoTag(value);
            } catch (IOException e) {
                throw DsonCodecException.wrap(e);
            }
        }

        @Override
        public void writeSint64(long value) {
            try {
                codedOutputStream.writeSInt64NoTag(value);
            } catch (IOException e) {
                throw DsonCodecException.wrap(e);
            }
        }

        @Override
        public void writeFixed64(long value) {
            try {
                codedOutputStream.writeFixed64NoTag(value);
            } catch (IOException e) {
                throw DsonCodecException.wrap(e);
            }
        }
        //

        @Override
        public void writeFloat(float value) {
            try {
                codedOutputStream.writeFloatNoTag(value);
            } catch (IOException e) {
                throw DsonCodecException.wrap(e);
            }
        }

        @Override
        public void writeDouble(double value) {
            try {
                codedOutputStream.writeDoubleNoTag(value);
            } catch (IOException e) {
                throw DsonCodecException.wrap(e);
            }
        }

        @Override
        public void writeBool(boolean value) {
            try {
                codedOutputStream.writeBoolNoTag(value);
            } catch (IOException e) {
                throw DsonCodecException.wrap(e);
            }
        }

        @Override
        public void writeString(String value) {
            try {
                codedOutputStream.writeStringNoTag(value);
            } catch (IOException e) {
                throw DsonCodecException.wrap(e);
            }
        }

        @Override
        public void writeRawBytes(byte[] value) {
            try {
                codedOutputStream.writeRawBytes(value, 0, value.length);
            } catch (IOException e) {
                throw DsonCodecException.wrap(e);
            }
        }

        @Override
        public void writeRawBytes(byte[] value, int offset, int length) {
            try {
                codedOutputStream.writeRawBytes(value, offset, length);
            } catch (IOException e) {
                throw DsonCodecException.wrap(e);
            }
        }

        @Override
        public void writeMessageNoSize(MessageLite value) {
            try {
                value.writeTo(codedOutputStream);
            } catch (IOException e) {
                throw DsonCodecException.wrap(e);
            }
        }
        //

        @Override
        public void flush() {
            try {
                codedOutputStream.flush();
            } catch (IOException e) {
                throw DsonCodecException.wrap(e);
            }
        }

        @Override
        public void close() {

        }
    }

    static class ArrayOutput extends CodedOutput {

        private final byte[] buffer;
        private final int offset;
        private final int limit;

        ArrayOutput(byte[] buffer) {
            this(buffer, 0, buffer.length);
        }

        ArrayOutput(byte[] buffer, int offset, int length) {
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
        public int position() {
            return (codedOutputStreamOffset - offset) + codedOutputStream.getTotalBytesWritten();
        }

        @Override
        public void setPosition(int writerIndex) {
            Objects.checkIndex(writerIndex, limit - offset);
            if (position() == writerIndex) {
                return;
            }
            try {
                codedOutputStream.flush();

                int newOffset = offset + writerIndex;
                codedOutputStream = CodedOutputStream.newInstance(buffer, newOffset, limit - newOffset);
                codedOutputStreamOffset = newOffset;
            } catch (IOException e) {
                throw DsonCodecException.wrap(e);
            }
        }

        @Override
        public void setFixedInt32(final int writerIndex, int value) {
            Objects.checkIndex(writerIndex, limit - offset);
            int newOffset = offset + writerIndex;
            BinaryUtils.checkBuffer(buffer, newOffset, 4);
            BinaryUtils.setIntLE(buffer, newOffset, value); // 保持和writeFixed32一致
        }

        @Override
        public String toString() {
            return "ArrayOutput{" +
                    "arrayLength=" + buffer.length +
                    ", offset=" + offset +
                    ", limit=" + limit +
                    ", codedOutputStreamOffset=" + codedOutputStreamOffset +
                    ", codedOutputStreamTotalBytesWritten=" + codedOutputStream.getTotalBytesWritten() +
                    ", totalBytesWritten=" + position() +
                    '}';
        }
    }

    static class ByteBufferOutput extends CodedOutput {

        private final ByteBuffer byteBuffer;
        private final int offset;

        ByteBufferOutput(ByteBuffer byteBuffer) {
            this.byteBuffer = byteBuffer.duplicate().order(ByteOrder.LITTLE_ENDIAN);
            this.offset = byteBuffer.position();

            this.codedOutputStream = CodedOutputStream.newInstance(byteBuffer);
            this.codedOutputStreamOffset = offset;
        }

        @Override
        public int position() {
            return (codedOutputStreamOffset - offset) + codedOutputStream.getTotalBytesWritten();
        }

        @Override
        public void setPosition(int writerIndex) {
            Objects.checkIndex(writerIndex, byteBuffer.limit() - offset);
            if (position() == writerIndex) {
                return;
            }
            try {
                codedOutputStream.flush();

                int newOffset = offset + writerIndex;
                BinaryUtils.position(byteBuffer, newOffset);
                codedOutputStream = CodedOutputStream.newInstance(byteBuffer);
                codedOutputStreamOffset = newOffset;
            } catch (IOException e) {
                throw DsonCodecException.wrap(e);
            }
        }

        @Override
        public void setFixedInt32(int writerIndex, int value) {
            int newOffset = offset + writerIndex;
            byteBuffer.putInt(newOffset, value);
        }
    }
}