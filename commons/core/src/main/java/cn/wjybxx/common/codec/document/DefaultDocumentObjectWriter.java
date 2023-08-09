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
import cn.wjybxx.common.codec.ConvertOptions;
import cn.wjybxx.common.codec.ConverterUtils;
import cn.wjybxx.common.codec.DsonCodecException;
import cn.wjybxx.common.codec.TypeArgInfo;
import cn.wjybxx.dson.*;
import cn.wjybxx.dson.io.Chunk;
import cn.wjybxx.dson.text.INumberStyle;
import cn.wjybxx.dson.text.IStyle;
import cn.wjybxx.dson.text.ObjectStyle;
import cn.wjybxx.dson.text.StringStyle;
import cn.wjybxx.dson.types.ObjectRef;
import cn.wjybxx.dson.types.OffsetTimestamp;

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
    public DsonWriter dsonWriter() {
        return writer;
    }

    @Override
    public boolean isAtName() {
        return writer.isAtName();
    }

    @Override
    public void writeName(String name) {
        writer.writeName(name);
    }

    @Override
    public void writeValueBytes(String name, DsonType dsonType, byte[] data) {
        Objects.requireNonNull(data);
        writer.writeValueBytes(name, dsonType, data);
    }
    // endregion

    // region 简单值

    @Override
    public void writeInt(String name, int value, WireType wireType, INumberStyle style) {
        writer.writeInt32(name, value, wireType, style);
    }

    @Override
    public void writeLong(String name, long value, WireType wireType, INumberStyle style) {
        writer.writeInt64(name, value, wireType, style);
    }

    @Override
    public void writeFloat(String name, float value, INumberStyle style) {
        writer.writeFloat(name, value, style);
    }

    @Override
    public void writeDouble(String name, double value, INumberStyle style) {
        writer.writeDouble(name, value, style);
    }

    @Override
    public void writeBoolean(String name, boolean value) {
        writer.writeBoolean(name, value);
    }

    @Override
    public void writeString(String name, @Nullable String value, StringStyle style) {
        if (value == null) {
            writeNull(name);
        } else {
            writer.writeString(name, value, StringStyle.AUTO);
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
    public void writeBytes(String name, int type, @Nonnull Chunk chunk) {
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
        codec.writeObject(value, this, typeArgInfo, findObjectStyle(value, typeArgInfo, style));
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
            codec.writeObject(value, this, typeArgInfo, findObjectStyle(value, typeArgInfo, style));
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
    public void writeStartObject(Object value, TypeArgInfo<?> typeArgInfo, ObjectStyle style) {
        writer.writeStartObject(style);
        writeClassId(value, typeArgInfo);
    }

    @Override
    public void writeEndObject() {
        writer.writeEndObject();
    }

    @Override
    public void writeStartArray(Object value, TypeArgInfo<?> typeArgInfo, ObjectStyle style) {
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
        return ObjectStyle.INDENT;
    }

    private void writeClassId(Object value, TypeArgInfo<?> typeArgInfo) {
        final Class<?> encodeClass = ConverterUtils.getEncodeClass(value); // 小心枚举
        if (!converter.options.classIdPolicy.test(typeArgInfo.declaredType, encodeClass)) {
            return;
        }
        String classId = converter.classIdRegistry.ofType(encodeClass);
        if (classId != null) {
            writer.writeStartHeader(ObjectStyle.FLOW);
            writer.writeString(DsonHeader.NAMES_CLASS_NAME, classId, StringStyle.UNQUOTE);
            writer.writeEndHeader();
        }
    }

    @SuppressWarnings("unchecked")
    private <T> DocumentPojoCodec<? super T> findObjectEncoder(T value) {
        final Class<?> encodeClass = ConverterUtils.getEncodeClass(value); // 小心枚举...
        return (DocumentPojoCodec<? super T>) converter.codecRegistry.get(encodeClass);
    }
    // endregion

}