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
import com.google.protobuf.Parser;
import org.apache.commons.lang3.StringUtils;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/4/23
 */
public class DefaultDocumentObjectReader implements DocumentObjectReader {

    private final DefaultDocumentConverter converter;
    private final DsonDocReader reader;

    public DefaultDocumentObjectReader(DefaultDocumentConverter converter, DsonDocReader reader) {
        this.converter = converter;
        this.reader = reader;
    }

    @SuppressWarnings("unchecked")
    @Override
    public <T> T decodeKey(String keyString, Class<T> keyDeclared) {
        if (keyDeclared == String.class || keyDeclared == Object.class) {
            return (T) keyString;
        }
        if (keyDeclared == Integer.class) { // key一定是包装类型
            return (T) Integer.valueOf(keyString);
        }
        if (keyDeclared == Long.class) {
            return (T) Long.valueOf(keyString);
        }
        DocumentPojoCodec<T> pojoCodec = converter.codecRegistry.get(keyDeclared);
        if (pojoCodec == null || !pojoCodec.isDsonEnumCodec()) {
            throw DsonCodecException.unsupportedKeyType(keyDeclared);
        }
        int number = Integer.parseInt(keyString);
        T dsonEnum = pojoCodec.forNumber(number);
        if (dsonEnum == null) {
            throw DsonCodecException.enumAbsent(keyDeclared, number);
        }
        return dsonEnum;
    }

    // region 代理

    @Override
    public void close() {
        reader.close();
    }

    @Override
    public DsonType readDsonType() {
        return reader.readDsonType();
    }

    @Override
    public String readName() {
        if (reader.isAtType()) {
            reader.readDsonType();
        }
        return reader.readName();
    }

    @Override
    public void readName(String name) {
        if (reader.isAtType()) {
            reader.readDsonType();
        }
        reader.readName(name);
    }

    @Override
    @Nonnull
    public DsonType getCurrentDsonType() {
        return reader.getCurrentDsonType();
    }

    @Override
    public String getCurrentName() {
        return reader.getCurrentName();
    }

    @Override
    public void skipName() {
        reader.skipName();
    }

    @Override
    public void skipValue() {
        reader.skipValue();
    }

    @Override
    public void skipToEndOfObject() {
        reader.skipToEndOfObject();
    }

    @Override
    public <T> T readMessage(String name, @Nonnull Parser<T> parser) {
        if (reader.isAtType()) {
            reader.readDsonType();
        }
        return reader.readMessage(name, converter.options.pbBinaryType, parser);
    }

    @Override
    public byte[] readValueAsBytes(String name) {
        if (reader.isAtType()) {
            reader.readDsonType();
        }
        return reader.readValueAsBytes(name);
    }

    // endregion

    // region 简单值

    @Override
    public int readInt(String name) {
        return NumberCodecHelper.readInt(reader, name);
    }

    @Override
    public long readLong(String name) {
        return NumberCodecHelper.readLong(reader, name);
    }

    @Override
    public float readFloat(String name) {
        return NumberCodecHelper.readFloat(reader, name);
    }

    @Override
    public double readDouble(String name) {
        return NumberCodecHelper.readDouble(reader, name);
    }

    @Override
    public boolean readBoolean(String name) {
        return NumberCodecHelper.readBool(reader, name);
    }

    @Override
    public String readString(String name) {
        return NumberCodecHelper.readString(reader, name);
    }

    @Override
    public void readNull(String name) {
        if (reader.isAtType()) {
            reader.readDsonType();
        }
        reader.readNull(name);
    }

    @Override
    public DsonBinary readBinary(String name) {
        return NumberCodecHelper.readBinary(reader, name);
    }

    @Override
    public DsonExtInt32 readExtInt32(String name) {
        return NumberCodecHelper.readExtInt32(reader, name);
    }

    @Override
    public DsonExtInt64 readExtInt64(String name) {
        return NumberCodecHelper.readExtInt64(reader, name);
    }

    @Override
    public DsonExtString readExtString(String name) {
        return NumberCodecHelper.readExtString(reader, name);
    }

    // endregion

    // region object处理

    @Override
    public <T> T readObject(TypeArgInfo<T> typeArgInfo) {
        Objects.requireNonNull(typeArgInfo);
        DsonType dsonType = reader.readDsonType();
        if (reader.getContextType() == DsonContextType.OBJECT) {
            reader.readName();
        }
        return readContainer(typeArgInfo, dsonType);
    }

    @SuppressWarnings("unchecked")
    @Nullable
    @Override
    public <T> T readObject(String name, TypeArgInfo<T> typeArgInfo) {
        Class<T> declaredType = typeArgInfo.declaredType;
        DsonDocReader reader = this.reader;
        // 基础类型不能返回null
        if (declaredType.isPrimitive()) {
            return (T) NumberCodecHelper.readPrimitive(reader, name, declaredType);
        }
        if (declaredType == String.class) {
            return (T) NumberCodecHelper.readString(reader, name);
        }
        if (declaredType == byte[].class) { // binary可以接收定长数据
            return (T) NumberCodecHelper.readBinary(reader, name);
        }
        // 对象类型--需要先读取写入的类型，才可以解码
        DsonType dsonType = NumberCodecHelper.readOrGetDsonType(reader);
        if (reader.getContextType() == DsonContextType.OBJECT) {
            reader.readName(name);
        }
        if (dsonType == DsonType.NULL) {
            return null;
        }
        if (dsonType.isContainer()) {
            // 容器类型只能通过codec解码
            return readContainer(typeArgInfo, dsonType);
        }
        // 考虑包装类型
        Class<?> unboxed = ConverterUtils.unboxIfWrapperType(declaredType);
        if (unboxed.isPrimitive()) {
            return (T) NumberCodecHelper.readPrimitive(reader, name, unboxed);
        }
        // 默认类型转换-声明类型可能是个抽象类型，eg：Number
        return readAsDsonValue(dsonType, name, declaredType);
    }

    private <T> T readContainer(TypeArgInfo<T> typeArgInfo, DsonType dsonType) {
//        DocClassId classId;
//        if (dsonType == DsonType.ARRAY) {
//            classId = reader.prestartArray();
//        } else {
//            classId = reader.prestartObject();
//        }
//        DocumentPojoCodec<? extends T> codec = findObjectDecoder(typeArgInfo, classId);
//        if (codec == null) {
//            throw DsonCodecException.incompatible(typeArgInfo.declaredType, classId);
//        }
//        return codec.readObject(this, typeArgInfo);
        return null;
    }

    private <T> T readAsDsonValue(DsonType dsonType, String name, Class<T> declaredType) {
        final DsonDocReader reader = this.reader;
        Object value = switch (dsonType) {
            case INT32 -> reader.readInt32(name);
            case INT64 -> reader.readInt64(name);
            case FLOAT -> reader.readFloat(name);
            case DOUBLE -> reader.readDouble(name);
            case BOOLEAN -> reader.readBoolean(name);
            case STRING -> reader.readString(name);
            case BINARY -> reader.readBinary(name);
            case EXT_STRING -> reader.readExtString(name);
            case EXT_INT32 -> reader.readExtInt32(name);
            case EXT_INT64 -> reader.readExtInt64(name);
            default -> throw new AssertionError(dsonType); // null和容器都前面测试了
        };
        return declaredType.cast(value);
    }

    @Override
    public void readStartObject(@Nonnull TypeArgInfo<?> typeArgInfo) {
        if (reader.isAtType()) {
            reader.readDsonType();
        }
        reader.readStartObject();
    }

    @Override
    public void readEndObject() {
        reader.skipToEndOfObject();
        reader.readEndObject();
    }

    @Override
    public void readStartArray(@Nonnull TypeArgInfo<?> typeArgInfo) {
        if (reader.isAtType()) {
            reader.readDsonType();
        }
        reader.readStartArray();
    }

    @Override
    public void readEndArray() {
        reader.skipToEndOfObject();
        reader.readEndArray();
    }

    //
    @SuppressWarnings("unchecked")
    private <T> DocumentPojoCodec<? extends T> findObjectDecoder(TypeArgInfo<T> typeArgInfo, String classId) {
        final Class<T> declaredType = typeArgInfo.declaredType;
        final Class<?> encodedType = StringUtils.isBlank(classId) ? null : converter.classIdRegistry.ofId(classId);
        // 尝试按真实类型读 - 概率最大
        if (encodedType != null && declaredType.isAssignableFrom(encodedType)) {
            return (DocumentPojoCodec<? extends T>) converter.codecRegistry.get(encodedType);
        }
        // 尝试按照声明类型读 - 读的时候两者可能是无继承关系的(投影)
        return converter.codecRegistry.get(declaredType);
    }

    // endregion
}