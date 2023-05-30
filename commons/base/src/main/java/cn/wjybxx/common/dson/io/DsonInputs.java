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
import com.google.protobuf.CodedInputStream;
import com.google.protobuf.ExtensionRegistryLite;
import com.google.protobuf.InvalidProtocolBufferException;
import com.google.protobuf.Parser;

import javax.annotation.Nonnull;
import java.io.IOException;
import java.nio.ByteBuffer;
import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/4/22
 */
public class DsonInputs {

    /**
     * 缓存有助于性能
     */
    private static final ExtensionRegistryLite EMPTY_REGISTRY = ExtensionRegistryLite.getEmptyRegistry();

    public static DsonInput newInstance(@Nonnull byte[] buffer) {
        return new ArrayInput(buffer);
    }

    public static DsonInput newInstance(byte[] buffer, int offset, int length) {
        return new ArrayInput(buffer, offset, length);
    }

    public static DsonInput newInstance(ByteBuffer byteBuffer) {
        return new ByteBufferInput(byteBuffer);
    }

    static abstract class CodedInput implements DsonInput {

        /** 由于{@link CodedInputStream}不支持回退和直接设置位置，因此我们调整位置时需要创建新的对象 */
        CodedInputStream codedInputStream;
        int codedInputStreamOffset;

        @Override
        public byte readRawByte() {
            try {
                return codedInputStream.readRawByte();
            } catch (IOException e) {
                throw DsonCodecException.wrap(e);
            }
        }

        @Override
        public int readInt32() {
            try {
                return codedInputStream.readInt32();
            } catch (IOException e) {
                throw DsonCodecException.wrap(e);
            }
        }

        @Override
        public int readUint32() {
            try {
                return codedInputStream.readUInt32();
            } catch (IOException e) {
                throw DsonCodecException.wrap(e);
            }
        }

        @Override
        public int readSint32() {
            try {
                return codedInputStream.readSInt32();
            } catch (IOException e) {
                throw DsonCodecException.wrap(e);
            }
        }

        @Override
        public int readFixed32() {
            try {
                return codedInputStream.readFixed32();
            } catch (IOException e) {
                throw DsonCodecException.wrap(e);
            }
        }

        @Override
        public long readInt64() {
            try {
                return codedInputStream.readInt64();
            } catch (IOException e) {
                throw DsonCodecException.wrap(e);
            }
        }

        @Override
        public long readUint64() {
            try {
                return codedInputStream.readUInt64();
            } catch (IOException e) {
                throw DsonCodecException.wrap(e);
            }
        }

        @Override
        public long readSint64() {
            try {
                return codedInputStream.readSInt64();
            } catch (IOException e) {
                throw DsonCodecException.wrap(e);
            }
        }

        @Override
        public long readFixed64() {
            try {
                return codedInputStream.readFixed64();
            } catch (IOException e) {
                throw DsonCodecException.wrap(e);
            }
        }

        @Override
        public float readFloat() {
            try {
                return codedInputStream.readFloat();
            } catch (IOException e) {
                throw DsonCodecException.wrap(e);
            }
        }

        @Override
        public double readDouble() {
            try {
                return codedInputStream.readDouble();
            } catch (IOException e) {
                throw DsonCodecException.wrap(e);
            }
        }

        @Override
        public boolean readBool() {
            try {
                return codedInputStream.readBool();
            } catch (IOException e) {
                throw DsonCodecException.wrap(e);
            }
        }

        @Override
        public String readString() {
            try {
                return codedInputStream.readString();
            } catch (IOException e) {
                throw DsonCodecException.wrap(e);
            }
        }

        @Override
        public byte[] readRawBytes(int size) {
            try {
                return codedInputStream.readRawBytes(size);
            } catch (IOException e) {
                throw DsonCodecException.wrap(e);
            }
        }

        @Override
        public void skipRawBytes(int n) {
            try {
                codedInputStream.skipRawBytes(n);
            } catch (IOException e) {
                throw DsonCodecException.wrap(e);
            }
        }

        @Override
        public <T> T readMessageNoSize(@Nonnull Parser<T> parser) {
            try {
                return parser.parseFrom(codedInputStream, EMPTY_REGISTRY);
            } catch (InvalidProtocolBufferException e) {
                throw DsonCodecException.wrap(e);
            }
        }
        //

        @Override
        public int pushLimit(int byteLimit) {
            try {
                return codedInputStream.pushLimit(byteLimit);
            } catch (InvalidProtocolBufferException e) {
                throw DsonCodecException.wrap(e);
            }
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
        public boolean isAtEnd() {
            try {
                return codedInputStream.isAtEnd();
            } catch (IOException e) {
                throw DsonCodecException.wrap(e);
            }
        }

        @Override
        public void close() {

        }

    }

    static class ArrayInput extends CodedInput {

        final byte[] buffer;
        final int offset;
        final int limit;

        ArrayInput(byte[] buffer) {
            this(buffer, 0, buffer.length);
        }

        ArrayInput(byte[] buffer, int offset, int length) {
            this.buffer = buffer;
            this.offset = offset;
            this.limit = offset + length;

            this.codedInputStream = CodedInputStream.newInstance(buffer, offset, length);
            this.codedInputStreamOffset = offset;
        }

        @Override
        public int position() {
            return (codedInputStreamOffset - offset) + codedInputStream.getTotalBytesRead();
        }

        @Override
        public void setPosition(int readerIndex) {
            Objects.checkIndex(readerIndex, limit - offset);
            if (readerIndex == position()) {
                return;
            }
            int newOffset = offset + readerIndex;
            codedInputStream = CodedInputStream.newInstance(buffer, newOffset, limit - readerIndex);
            codedInputStreamOffset = newOffset;
        }

        @Override
        public int getFixed32(int readerIndex) {
            int newOffset = offset + readerIndex;
            BinaryUtils.checkBuffer(buffer, newOffset, 4);
            return BinaryUtils.getIntLE(buffer, newOffset);
        }
    }

    static class ByteBufferInput extends CodedInput {

        private final ByteBuffer byteBuffer;
        private final int offset;

        public ByteBufferInput(ByteBuffer byteBuffer) {
            this.byteBuffer = byteBuffer;
            this.offset = byteBuffer.position();

            this.codedInputStream = CodedInputStream.newInstance(byteBuffer);
            this.codedInputStreamOffset = offset;
        }

        @Override
        public int position() {
            return (codedInputStreamOffset - offset) + codedInputStream.getTotalBytesRead();
        }

        @Override
        public void setPosition(int readerIndex) {
            Objects.checkIndex(readerIndex, byteBuffer.limit() - offset);
            if (position() == readerIndex) {
                return;
            }
            codedInputStream.resetSizeCounter();

            int newOffset = offset + readerIndex;
            BinaryUtils.position(byteBuffer, newOffset);
            codedInputStream = CodedInputStream.newInstance(byteBuffer);
            codedInputStreamOffset = newOffset;
        }

        @Override
        public int getFixed32(int readerIndex) {
            int newOffset = offset + readerIndex;
            return byteBuffer.getInt(newOffset);
        }

    }
}