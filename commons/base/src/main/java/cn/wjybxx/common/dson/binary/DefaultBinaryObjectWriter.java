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

package cn.wjybxx.common.dson.binary;

import cn.wjybxx.common.dson.*;
import cn.wjybxx.common.dson.codec.ClassId;
import cn.wjybxx.common.dson.codec.ConverterUtils;
import cn.wjybxx.common.dson.io.Chunk;
import cn.wjybxx.common.dson.text.ObjectStyle;
import cn.wjybxx.common.dson.text.StringStyle;
import com.google.protobuf.MessageLite;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/4/23
 */
public class DefaultBinaryObjectWriter implements BinaryObjectWriter {

    private final DefaultBinaryConverter converter;
    private final DsonBinWriter writer;

    public DefaultBinaryObjectWriter(DefaultBinaryConverter converter, DsonBinWriter writer) {
        this.converter = converter;
        this.writer = writer;
    }

    // region 代理
    @Override
    public void flush() {
        writer.flush();
    }

    @Override
    public void close() {
        writer.close();
    }

    @Override
    public void writeName(int name) {
        writer.writeName(name);
    }

    @Override
    public void writeMessage(int name, MessageLite messageLite) {
        if (messageLite == null) {
            writeNull(name);
        } else {
            writer.writeMessage(name, converter.options.pbBinaryType, messageLite);
        }
    }

    @Override
    public void writeValueBytes(int name, DsonType dsonType, byte[] data) {
        Objects.requireNonNull(data);
        writer.writeValueBytes(name, dsonType, data);
    }
    // endregion

    // region 简单值

    @Override
    public void writeInt(int name, int value, WireType wireType) {
        writer.writeInt32(name, value, wireType, false);
    }

    @Override
    public void writeLong(int name, long value, WireType wireType) {
        writer.writeInt64(name, value, wireType, false);
    }

    @Override
    public void writeFloat(int name, float value) {
        writer.writeFloat(name, value, false);
    }

    @Override
    public void writeDouble(int name, double value) {
        writer.writeDouble(name, value);
    }

    @Override
    public void writeBoolean(int name, boolean value) {
        writer.writeBoolean(name, value);
    }

    @Override
    public void writeString(int name, @Nullable String value) {
        if (value == null) {
            writeNull(name);
        } else {
            writer.writeString(name, value, StringStyle.AUTO);
        }
    }

    @Override
    public void writeNull(int name) {
        writer.writeNull(name);
    }

    @Override
    public void writeBytes(int name, @Nullable byte[] value) {
        if (value == null) {
            writeNull(name);
        } else {
            writer.writeBinary(name, new DsonBinary(0, value));
        }
    }

    @Override
    public void writeBytes(int name, int type, @Nonnull Chunk chunk) {
        writer.writeBinary(name, type, chunk);
    }

    @Override
    public void writeBinary(int name, int type, byte[] value) {
        if (value == null) {
            writeNull(name);
        } else {
            writer.writeBinary(name, new DsonBinary(type, value));
        }
    }

    @Override
    public void writeBinary(int name, DsonBinary binary) {
        if (binary == null) {
            writeNull(name);
        } else {
            writer.writeBinary(name, binary);
        }
    }

    @Override
    public void writeExtInt32(int name, DsonExtInt32 value, WireType wireType) {
        if (value == null) {
            writeNull(name);
        } else {
            writer.writeExtInt32(name, value, wireType);
        }
    }

    @Override
    public void writeExtInt32(int name, int type, int value, WireType wireType) {
        writer.writeExtInt32(name, new DsonExtInt32(type, value), wireType);
    }

    @Override
    public void writeExtInt64(int name, DsonExtInt64 value, WireType wireType) {
        if (value == null) {
            writeNull(name);
        } else {
            writer.writeExtInt64(name, value, wireType);
        }
    }

    @Override
    public void writeExtInt64(int name, int type, long value, WireType wireType) {
        writer.writeExtInt64(name, new DsonExtInt64(type, value), wireType);
    }

    @Override
    public void writeExtString(int name, DsonExtString value) {
        if (value == null) {
            writeNull(name);
        } else {
            writer.writeExtString(name, value, StringStyle.AUTO);
        }
    }

    @Override
    public void writeExtString(int name, int type, String value) {
        // 这里为Null不安全
        Objects.requireNonNull(value);
        writer.writeExtString(name, new DsonExtString(type, value), StringStyle.AUTO);
    }

    // endregion

    // region object处理

    @Override
    public <T> void writeObject(T value, TypeArgInfo<?> typeArgInfo) {
        Objects.requireNonNull(value, "value is null");
        BinaryPojoCodec<? super T> codec = findObjectEncoder(value);
        if (codec == null) {
            throw DsonCodecException.unsupportedType(value.getClass());
        }
        codec.writeObject(value, this, typeArgInfo);
    }

    @Override
    public <T> void writeObject(int name, T value, TypeArgInfo<?> typeArgInfo) {
        Objects.requireNonNull(typeArgInfo, "typeArgInfo");
        if (value == null) {
            writeNull(name);
            return;
        }
        // 由于基本类型通常会使用特定的read/write方法，因此最后测试基本类型和包装类型
        BinaryPojoCodec<? super T> codec = findObjectEncoder(value);
        if (codec != null) {
            writer.writeName(name);
            codec.writeObject(value, this, typeArgInfo);
            return;
        }

        Class<?> type = value.getClass();
        if (type == Integer.class) {
            writeInt(name, (Integer) value);
            return;
        }
        if (type == Long.class) {
            writeLong(name, (Long) value);
            return;
        }
        if (type == Float.class) {
            writeFloat(name, (Float) value);
            return;
        }
        if (type == Double.class) {
            writeDouble(name, (Double) value);
            return;
        }
        if (type == Boolean.class) {
            writeBoolean(name, (Boolean) value);
            return;
        }
        //
        if (type == String.class) {
            writeString(name, (String) value);
            return;
        }
        if (type == byte[].class) {
            writeBytes(name, (byte[]) value);
        }
        //
        if (type == Short.class) {
            writeShort(name, (Short) value);
            return;
        }
        if (type == Byte.class) {
            writeByte(name, (Byte) value);
            return;
        }
        if (type == Character.class) {
            writeChar(name, (Character) value);
            return;
        }
        throw DsonCodecException.unsupportedType(type);
    }

    @Override
    public void writeStartObject(Object value, TypeArgInfo<?> typeArgInfo) {
        writer.writeStartObject(ObjectStyle.INDENT);
    }

    @Override
    public void writeEndObject() {
        writer.writeEndObject();
    }

    @Override
    public void writeStartArray(Object value, TypeArgInfo<?> typeArgInfo) {
        writer.writeStartArray(ObjectStyle.INDENT);
    }

    @Override
    public void writeEndArray() {
        writer.writeEndArray();
    }

    private ClassId findEncodeClassId(Object value, TypeArgInfo<?> typeArgInfo) {
        final Class<?> encodeClass = ConverterUtils.getEncodeClass(value); // 小心枚举
        if (converter.options.classIdPolicy.test(typeArgInfo.declaredType, encodeClass)) {
            return converter.classIdRegistry.ofType(encodeClass);
        }
        return null;
    }

    @SuppressWarnings("unchecked")
    private <T> BinaryPojoCodec<? super T> findObjectEncoder(T value) {
        final Class<?> encodeClass = ConverterUtils.getEncodeClass(value); // 小心枚举...
        return (BinaryPojoCodec<? super T>) converter.codecRegistry.get(encodeClass);
    }
    // endregion

}