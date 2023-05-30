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

package cn.wjybxx.common.dson.document;

import cn.wjybxx.common.dson.*;
import cn.wjybxx.common.dson.codec.ConverterUtils;
import cn.wjybxx.common.dson.io.Chunk;
import com.google.protobuf.MessageLite;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/4/23
 */
public class DefaultDocumentObjectWriter implements DocumentObjectWriter {

    private final DefaultDocumentConverter converter;
    private final DsonDocWriter writer;

    public DefaultDocumentObjectWriter(DefaultDocumentConverter converter, DsonDocWriter writer) {
        this.converter = converter;
        this.writer = writer;
    }

    @Override
    public String encodeKey(Object key) {
        Objects.requireNonNull(key);
        if (key instanceof String str) {
            return str;
        }
        if ((key instanceof Integer) || (key instanceof Long)) {
            return key.toString();
        }
        if (!(key instanceof DsonEnum dsonEnum)) {
            throw DsonCodecException.unsupportedType(key.getClass());
        }
        return Integer.toString(dsonEnum.getNumber());
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
    public void writeName(String name) {
        writer.writeName(name);
    }

    @Override
    public void writeMessage(String name, MessageLite messageLite) {
        if (messageLite == null) {
            writeNull(name);
        } else {
            writer.writeMessage(name, messageLite);
        }
    }

    @Override
    public void writeValueBytes(String name, DsonType dsonType, byte[] data) {
        Objects.requireNonNull(data);
        writer.writeValueBytes(name, dsonType, data);
    }
    // endregion

    // region 简单值

    @Override
    public void writeInt(String name, int value, WireType wireType) {
        writer.writeInt32(name, value, wireType);
    }

    @Override
    public void writeLong(String name, long value, WireType wireType) {
        writer.writeInt64(name, value, wireType);
    }

    @Override
    public void writeFloat(String name, float value) {
        writer.writeFloat(name, value);
    }

    @Override
    public void writeDouble(String name, double value) {
        writer.writeDouble(name, value);
    }

    @Override
    public void writeBoolean(String name, boolean value) {
        writer.writeBoolean(name, value);
    }

    @Override
    public void writeString(String name, @Nullable String value) {
        if (value == null) {
            writeNull(name);
        } else {
            writer.writeString(name, value);
        }
    }

    @Override
    public void writeNull(String name) {
        // 用户已写入name或convert开启了null写入
        if (!writer.isAtName() || converter.options.appendNull) {
            writer.writeNull(name);
        }
    }

    @Override
    public void writeBytes(String name, @Nullable byte[] value) {
        if (value == null) {
            writeNull(name);
        } else {
            writer.writeBinary(name, (byte) 0, value);
        }
    }

    @Override
    public void writeBytes(String name, DsonBinaryType type, @Nonnull Chunk chunk) {
        Objects.requireNonNull(chunk);
        writer.writeBinary(name, type.getValue(), chunk);
    }

    @Override
    public void writeBinary(String name, DsonBinaryType type, byte[] value) {
        if (value == null) {
            writeNull(name);
        } else {
            writer.writeBinary(name, type.getValue(), value);
        }
    }

    @Override
    public void writeBinary(String name, DsonBinary binary) {
        if (binary == null) {
            writeNull(name);
        } else {
            writer.writeBinary(name, binary);
        }
    }

    @Override
    public void writeExtString(String name, DsonExtString value) {
        if (value == null) {
            writeNull(name);
        } else {
            writer.writeExtString(name, value);
        }
    }

    @Override
    public void writeExtString(String name, DsonExtStringType type, String value) {
        if (value == null) {
            writeNull(name);
        } else {
            writer.writeExtString(name, type.getValue(), value);
        }
    }

    @Override
    public void writeExtInt32(String name, DsonExtInt32 value, WireType wireType) {
        if (value == null) {
            writeNull(name);
        } else {
            writer.writeExtInt32(name, value, wireType);
        }
    }

    @Override
    public void writeExtInt32(String name, DsonExtInt32Type type, int value, WireType wireType) {
        Objects.requireNonNull(type);
        writer.writeExtInt32(name, type.getValue(), value, wireType);
    }

    @Override
    public void writeExtInt64(String name, DsonExtInt64 value, WireType wireType) {
        if (value == null) {
            writeNull(name);
        } else {
            writer.writeExtInt64(name, value, wireType);
        }
    }

    @Override
    public void writeExtInt64(String name, DsonExtInt64Type type, long value, WireType wireType) {
        Objects.requireNonNull(type);
        writer.writeExtInt64(name, type.getValue(), value, wireType);
    }

    // endregion

    // region object处理

    @Override
    public <T> void writeObject(T value, TypeArgInfo<?> typeArgInfo) {
        Objects.requireNonNull(value, "value is null");
        DocumentPojoCodec<? super T> codec = findObjectEncoder(value);
        if (codec == null) {
            throw DsonCodecException.unsupportedType(value.getClass());
        }
        codec.writeObject(value, this, typeArgInfo);
    }

    @Override
    public <T> void writeObject(String name, T value, TypeArgInfo<?> typeArgInfo) {
        Objects.requireNonNull(typeArgInfo, "typeArgInfo");
        if (value == null) {
            writeNull(name);
            return;
        }
        // 由于基本类型通常会使用特定的read/write方法，因此最后测试基本类型和包装类型
        DocumentPojoCodec<? super T> codec = findObjectEncoder(value);
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
        writer.writeStartObject(findEncodeClassId(value, typeArgInfo));
    }

    @Override
    public void writeEndObject() {
        writer.writeEndObject();
    }

    @Override
    public void writeStartArray(Object value, TypeArgInfo<?> typeArgInfo) {
        writer.writeStartArray(findEncodeClassId(value, typeArgInfo));
    }

    @Override
    public void writeEndArray() {
        writer.writeEndArray();
    }

    private DocClassId findEncodeClassId(Object value, TypeArgInfo<?> typeArgInfo) {
        final Class<?> encodeClass = ConverterUtils.getEncodeClass(value); // 小心枚举
        if (converter.options.classIdPolicy.test(typeArgInfo.declaredType, encodeClass)) {
            return converter.classIdRegistry.ofType(encodeClass);
        }
        return null;
    }

    @SuppressWarnings("unchecked")
    private <T> DocumentPojoCodec<? super T> findObjectEncoder(T value) {
        final Class<?> encodeClass = ConverterUtils.getEncodeClass(value); // 小心枚举...
        return (DocumentPojoCodec<? super T>) converter.codecRegistry.get(encodeClass);
    }
    // endregion

}