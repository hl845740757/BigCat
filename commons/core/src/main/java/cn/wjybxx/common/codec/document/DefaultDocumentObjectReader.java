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

import cn.wjybxx.common.codec.*;
import cn.wjybxx.dson.*;
import cn.wjybxx.dson.types.ObjectRef;
import cn.wjybxx.dson.types.OffsetTimestamp;
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
    private final DsonReader reader;

    public DefaultDocumentObjectReader(DefaultDocumentConverter converter, DsonReader reader) {
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
        if (pojoCodec == null || !pojoCodec.isEnumLiteCodec()) {
            throw DsonCodecException.unsupportedKeyType(keyDeclared);
        }
        int number = Integer.parseInt(keyString);
        T enumLite = pojoCodec.forNumber(number);
        if (enumLite == null) {
            throw DsonCodecException.enumAbsent(keyDeclared, number);
        }
        return enumLite;
    }

    // region 代理

    @Override
    public void close() {
        reader.close();
    }

    @Override
    public ConvertOptions options() {
        return converter.options;
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
    public DsonContextType getContextType() {
        return reader.getContextType();
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
    public <T> T readMessage(String name, int binaryType, @Nonnull Parser<T> parser) {
        if (reader.isAtType()) {
            reader.readDsonType();
        }
        return reader.readMessage(name, binaryType, parser);
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
        return CodecHelper.readInt(reader, name);
    }

    @Override
    public long readLong(String name) {
        return CodecHelper.readLong(reader, name);
    }

    @Override
    public float readFloat(String name) {
        return CodecHelper.readFloat(reader, name);
    }

    @Override
    public double readDouble(String name) {
        return CodecHelper.readDouble(reader, name);
    }

    @Override
    public boolean readBoolean(String name) {
        return CodecHelper.readBool(reader, name);
    }

    @Override
    public String readString(String name) {
        return CodecHelper.readString(reader, name);
    }

    @Override
    public void readNull(String name) {
        CodecHelper.readNull(reader, name);
    }

    @Override
    public DsonBinary readBinary(String name) {
        return CodecHelper.readBinary(reader, name);
    }

    @Override
    public DsonExtInt32 readExtInt32(String name) {
        return CodecHelper.readExtInt32(reader, name);
    }

    @Override
    public DsonExtInt64 readExtInt64(String name) {
        return CodecHelper.readExtInt64(reader, name);
    }

    @Override
    public DsonExtString readExtString(String name) {
        return CodecHelper.readExtString(reader, name);
    }

    @Override
    public ObjectRef readRef(String name) {
        return CodecHelper.readRef(reader, name);
    }

    @Override
    public OffsetTimestamp readTimestamp(String name) {
        return CodecHelper.readTimestamp(reader, name);
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
        DsonReader reader = this.reader;
        // 基础类型不能返回null
        if (declaredType.isPrimitive()) {
            return (T) CodecHelper.readPrimitive(reader, name, declaredType);
        }
        if (declaredType == String.class) {
            return (T) CodecHelper.readString(reader, name);
        }
        if (declaredType == byte[].class) { // binary可以接收定长数据
            return (T) CodecHelper.readBinary(reader, name);
        }
        // 对象类型--需要先读取写入的类型，才可以解码
        DsonType dsonType = CodecHelper.readOrGetDsonType(reader);
        if (reader.isAtName()) {
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
            return (T) CodecHelper.readPrimitive(reader, name, unboxed);
        }
        // 默认类型转换-声明类型可能是个抽象类型，eg：Number
        if (DsonValue.class.isAssignableFrom(declaredType)) {
            return declaredType.cast(Dsons.readDsonValue(reader));
        }
        return declaredType.cast(CodecHelper.readValue(reader, dsonType, name));
    }

    private <T> T readContainer(TypeArgInfo<T> typeArgInfo, DsonType dsonType) {
        String classId = readClassId(dsonType);
        DocumentPojoCodec<? extends T> codec = findObjectDecoder(typeArgInfo, classId);
        if (codec == null) {
            throw DsonCodecException.incompatible(typeArgInfo.declaredType, classId);
        }
        return codec.readObject(this, typeArgInfo);
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

    private String readClassId(DsonType dsonType) {
        DsonReader reader = this.reader;
        if (dsonType == DsonType.OBJECT) {
            reader.readStartObject();
        } else {
            reader.readStartArray();
        }
        String clsName;
        DsonType nextDsonType = reader.peekDsonType();
        if (nextDsonType == DsonType.HEADER) {
            if (reader.readDsonType() != DsonType.HEADER) {
                throw new DsonCodecException();
            }
            reader.readStartHeader();
            clsName = reader.readString(DsonHeader.NAMES_CLASS_NAME);
            if (reader.readDsonType() != DsonType.END_OF_OBJECT) {
                throw new DsonCodecException();
            }
            reader.readEndHeader();
        } else {
            clsName = "";
        }
        reader.backToWaitStart();
        return clsName;
    }

    @SuppressWarnings("unchecked")
    private <T> DocumentPojoCodec<? extends T> findObjectDecoder(TypeArgInfo<T> typeArgInfo, String classId) {
        final Class<T> declaredType = typeArgInfo.declaredType;
        if (!StringUtils.isBlank(classId)) {
            TypeMeta<String> typeMeta = converter.typeMetaRegistry.ofId(classId);
            if (typeMeta != null && declaredType.isAssignableFrom(typeMeta.clazz)) {
                // 尝试按真实类型读
                return (DocumentPojoCodec<? extends T>) converter.codecRegistry.get(typeMeta.clazz);
            }
        }
        // 尝试按照声明类型读 - 读的时候两者可能是无继承关系的(投影)
        return converter.codecRegistry.get(declaredType);
    }

    // endregion
}