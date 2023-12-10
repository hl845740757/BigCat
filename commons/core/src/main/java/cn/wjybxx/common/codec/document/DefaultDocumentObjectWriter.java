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

package cn.wjybxx.common.codec.document;

import cn.wjybxx.common.EnumLite;
import cn.wjybxx.common.codec.*;
import cn.wjybxx.dson.*;
import cn.wjybxx.dson.io.DsonChunk;
import cn.wjybxx.dson.text.INumberStyle;
import cn.wjybxx.dson.text.IStyle;
import cn.wjybxx.dson.text.ObjectStyle;
import cn.wjybxx.dson.text.StringStyle;
import cn.wjybxx.dson.types.ObjectRef;
import cn.wjybxx.dson.types.OffsetTimestamp;
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
    private final DsonWriter writer;

    public DefaultDocumentObjectWriter(DefaultDocumentConverter converter, DsonWriter writer) {
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
    public ConvertOptions options() {
        return converter.options;
    }

    @Override
    public void writeName(String name) {
        writer.writeName(name);
    }

    @Override
    public void writeMessage(String name, int binaryType, MessageLite messageLite) {
        Objects.requireNonNull(messageLite);
        writer.writeMessage(name, binaryType, messageLite);
    }

    @Override
    public void writeValueBytes(String name, DsonType dsonType, byte[] data) {
        Objects.requireNonNull(data);
        writer.writeValueBytes(name, dsonType, data);
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
        if (!(key instanceof EnumLite enumLite)) {
            throw DsonCodecException.unsupportedType(key.getClass());
        }
        return Integer.toString(enumLite.getNumber());
    }
    // endregion

    // region 简单值

    @Override
    public void writeInt(String name, int value, WireType wireType, INumberStyle style) {
        if (value != 0 || (!writer.isAtName() || converter.options.appendDef)) {
            writer.writeInt32(name, value, wireType, style);
        }
    }

    @Override
    public void writeLong(String name, long value, WireType wireType, INumberStyle style) {
        if (value != 0 || (!writer.isAtName() || converter.options.appendDef)) {
            writer.writeInt64(name, value, wireType, style);
        }
    }

    @Override
    public void writeFloat(String name, float value, INumberStyle style) {
        if (value != 0 || (!writer.isAtName() || converter.options.appendDef)) {
            writer.writeFloat(name, value, style);
        }
    }

    @Override
    public void writeDouble(String name, double value, INumberStyle style) {
        if (value != 0 || (!writer.isAtName() || converter.options.appendDef)) {
            writer.writeDouble(name, value, style);
        }
    }

    @Override
    public void writeBoolean(String name, boolean value) {
        if (value || (!writer.isAtName() || converter.options.appendDef)) {
            writer.writeBoolean(name, value);
        }
    }

    @Override
    public void writeString(String name, @Nullable String value, StringStyle style) {
        if (value == null) {
            writeNull(name);
        } else {
            writer.writeString(name, value, style);
        }
    }

    @Override
    public void writeNull(String name) {
        // 用户已写入name或convert开启了null写入
        if (!writer.isAtName() || options().appendNull) {
            writer.writeNull(name);
        }
    }

    @Override
    public void writeBytes(String name, byte[] value) {
        if (value == null) {
            writeNull(name);
        } else {
            writer.writeBinary(name, new DsonBinary(0, value));
        }
    }

    @Override
    public void writeBytes(String name, int type, byte[] value) {
        if (value == null) {
            writeNull(name);
        } else {
            writer.writeBinary(name, new DsonBinary(type, value));
        }
    }

    @Override
    public void writeBytes(String name, int type, @Nonnull DsonChunk chunk) {
        writer.writeBinary(name, type, chunk);
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
    public void writeExtInt32(String name, DsonExtInt32 value, WireType wireType, INumberStyle style) {
        if (value == null) {
            writeNull(name);
        } else {
            writer.writeExtInt32(name, value, wireType, style);
        }
    }

    @Override
    public void writeExtInt32(String name, int type, int value, WireType wireType, INumberStyle style) {
        writer.writeExtInt32(name, new DsonExtInt32(type, value), wireType, style);
    }

    @Override
    public void writeExtInt64(String name, DsonExtInt64 value, WireType wireType, INumberStyle style) {
        if (value == null) {
            writeNull(name);
        } else {
            writer.writeExtInt64(name, value, wireType, style);
        }
    }

    @Override
    public void writeExtInt64(String name, int type, long value, WireType wireType, INumberStyle style) {
        writer.writeExtInt64(name, new DsonExtInt64(type, value), wireType, style);
    }

    @Override
    public void writeExtDouble(String name, DsonExtDouble value, INumberStyle style) {
        if (value == null) {
            writeNull(name);
        } else {
            writer.writeExtDouble(name, value, style);
        }
    }

    @Override
    public void writeExtDouble(String name, int type, double value, INumberStyle style) {
        writer.writeExtDouble(name, new DsonExtDouble(type, value), style);
    }

    @Override
    public void writeExtString(String name, DsonExtString value, StringStyle style) {
        if (value == null) {
            writeNull(name);
        } else {
            writer.writeExtString(name, value, style);
        }
    }

    @Override
    public void writeExtString(String name, int type, String value, StringStyle style) {
        writer.writeExtString(name, new DsonExtString(type, value), style);
    }

    @Override
    public void writeRef(String name, ObjectRef ref) {
        if (ref == null) {
            writeNull(name);
        } else {
            writer.writeRef(name, ref);
        }
    }

    @Override
    public void writeTimestamp(String name, OffsetTimestamp timestamp) {
        if (timestamp == null) {
            writeNull(name);
        } else {
            writer.writeTimestamp(name, timestamp);
        }
    }

    // endregion

    // region object处理

    @Override
    public <T> void writeObject(T value, TypeArgInfo<?> typeArgInfo, ObjectStyle style) {
        Objects.requireNonNull(value, "value is null");
        DocumentPojoCodec<? super T> codec = findObjectEncoder(value);
        if (codec == null) {
            throw DsonCodecException.unsupportedType(value.getClass());
        }
        codec.writeObject(this, value, typeArgInfo, findObjectStyle(value, typeArgInfo, style));
    }

    @Override
    public <T> void writeObject(String name, T value, TypeArgInfo<?> typeArgInfo, IStyle style) {
        Objects.requireNonNull(typeArgInfo, "typeArgInfo");
        if (value == null) {
            writeNull(name);
            return;
        }
        // 由于基本类型通常会使用特定的read/write方法，因此最后测试基本类型和包装类型
        DocumentPojoCodec<? super T> codec = findObjectEncoder(value);
        if (codec != null) {
            writer.writeName(name);
            codec.writeObject(this, value, typeArgInfo, findObjectStyle(value, typeArgInfo, style));
            return;
        }

        Class<?> type = value.getClass();
        if (type == Integer.class) {
            writeInt(name, (Integer) value, WireType.VARINT, ConverterUtils.castNumberStyle(style));
            return;
        }
        if (type == Long.class) {
            writeLong(name, (Long) value, WireType.VARINT, ConverterUtils.castNumberStyle(style));
            return;
        }
        if (type == Float.class) {
            writeFloat(name, (Float) value, ConverterUtils.castNumberStyle(style));
            return;
        }
        if (type == Double.class) {
            writeDouble(name, (Double) value, ConverterUtils.castNumberStyle(style));
            return;
        }
        if (type == Boolean.class) {
            writeBoolean(name, (Boolean) value);
            return;
        }
        //
        if (type == String.class) {
            writeString(name, (String) value, ConverterUtils.castStringStyle(style));
            return;
        }
        if (type == byte[].class) {
            writeBytes(name, 0, (byte[]) value);
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
        if (value instanceof DsonValue dsonValue) {
            Dsons.writeDsonValue(writer, dsonValue, name);
            return;
        }
        throw DsonCodecException.unsupportedType(type);
    }

    @Override
    public void writeStartObject(@Nonnull Object value, TypeArgInfo<?> typeArgInfo, ObjectStyle style) {
        writer.writeStartObject(style);
        writeClassId(value, typeArgInfo);
    }

    @Override
    public void writeEndObject() {
        writer.writeEndObject();
    }

    @Override
    public void writeStartArray(@Nonnull Object value, TypeArgInfo<?> typeArgInfo, ObjectStyle style) {
        writer.writeStartArray(style);
        writeClassId(value, typeArgInfo);
    }

    @Override
    public void writeEndArray() {
        writer.writeEndArray();
    }

    private ObjectStyle findObjectStyle(Object value, TypeArgInfo<?> typeArgInfo, IStyle style) {
        if (style instanceof ObjectStyle objectStyle) {
            return objectStyle;
        }
        final Class<?> encodeClass = ConverterUtils.getEncodeClass(value); // 小心枚举...
        TypeMeta typeMeta = converter.typeMetaRegistry.ofType(encodeClass);
        if (typeMeta != null) {
            return typeMeta.style;
        }
        return ObjectStyle.INDENT;
    }

    private void writeClassId(Object value, TypeArgInfo<?> typeArgInfo) {
        final Class<?> encodeClass = ConverterUtils.getEncodeClass(value); // 小心枚举
        if (!converter.options.classIdPolicy.test(typeArgInfo.declaredType, encodeClass)) {
            return;
        }
        TypeMeta typeMeta = converter.typeMetaRegistry.ofType(encodeClass);
        if (typeMeta != null && typeMeta.classNames.size() > 0) {
            writer.writeSimpleHeader(typeMeta.mainClassName());
        }
    }

    @SuppressWarnings("unchecked")
    private <T> DocumentPojoCodec<? super T> findObjectEncoder(T value) {
        final Class<?> encodeClass = ConverterUtils.getEncodeClass(value); // 小心枚举...
        return (DocumentPojoCodec<? super T>) converter.codecRegistry.get(encodeClass);
    }
    // endregion

}