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

package cn.wjybxx.common.codec.binary;

import cn.wjybxx.common.codec.*;
import cn.wjybxx.dson.*;
import cn.wjybxx.dson.types.ObjectRef;
import cn.wjybxx.dson.types.OffsetTimestamp;
import com.google.protobuf.Parser;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.util.List;
import java.util.Map;
import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/4/23
 */
public class DefaultBinaryObjectReader implements BinaryObjectReader {

    private final DefaultBinaryConverter converter;
    private final DsonLiteReader reader;

    public DefaultBinaryObjectReader(DefaultBinaryConverter converter, DsonLiteReader reader) {
        this.converter = converter;
        this.reader = reader;
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
        return reader.isAtType() ? reader.readDsonType() : reader.getCurrentDsonType();
    }

    @Override
    public int readName() {
        return reader.isAtName() ? reader.readName() : reader.getCurrentName();
    }

    @Override
    public boolean readName(int name) {
        DsonLiteReader reader = this.reader;
        if (reader.getContextType() == DsonContextType.ARRAY) {
            if (name != 0) throw new IllegalArgumentException("the name of array element must be 0");
            if (reader.isAtValue()) {
                return true;
            }
            if (reader.isAtType()) {
                return reader.readDsonType() != DsonType.END_OF_OBJECT;
            }
            return reader.getCurrentDsonType() != DsonType.END_OF_OBJECT;
        }
        // 判断上次读取的name是否有效
        if (reader.isAtValue()) {
            if (reader.getCurrentName() == name) {
                return true;
            }
            if (FieldNumber.compare(reader.getCurrentName(), name) > 0) {
                return false;
            }
            reader.skipValue();
        }
        if (reader.isAtType()) {
            // 尚可尝试读取
            while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
                if (FieldNumber.compare(reader.readName(), name) >= 0) {
                    break;
                }
                reader.skipValue();
            }
            if (reader.getCurrentDsonType() == DsonType.END_OF_OBJECT) {
                return false;
            }
            return reader.getCurrentName() == name;
        } else {
            // 当前已到达尾部
            if (reader.getCurrentDsonType() == DsonType.END_OF_OBJECT) {
                return false;
            }
            reader.readName(name); // 不匹配会产生异常
            return true;
        }
    }

    @Override
    @Nonnull
    public DsonType getCurrentDsonType() {
        return reader.getCurrentDsonType();
    }

    @Override
    public int getCurrentName() {
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
    public <T> T readMessage(int name, int binaryType, @Nonnull Parser<T> parser) {
        return readName(name) ? reader.readMessage(name, binaryType, parser) : null;
    }

    @Override
    public byte[] readValueAsBytes(int name) {
        return readName(name) ? reader.readValueAsBytes(name) : null;
    }

    // endregion

    // region 简单值

    @Override
    public int readInt(int name) {
        return readName(name) ? CodecHelper.readInt(reader, name) : 0;
    }

    @Override
    public long readLong(int name) {
        return readName(name) ? CodecHelper.readLong(reader, name) : 0;
    }

    @Override
    public float readFloat(int name) {
        return readName(name) ? CodecHelper.readFloat(reader, name) : 0;
    }

    @Override
    public double readDouble(int name) {
        return readName(name) ? CodecHelper.readDouble(reader, name) : 0;
    }

    @Override
    public boolean readBoolean(int name) {
        return readName(name) && CodecHelper.readBool(reader, name);
    }

    @Override
    public String readString(int name) {
        return readName(name) ? CodecHelper.readString(reader, name) : null;
    }

    @Override
    public void readNull(int name) {
        if (readName(name)) {
            CodecHelper.readNull(reader, name);
        }
    }

    @Override
    public DsonBinary readBinary(int name) {
        return readName(name) ? CodecHelper.readBinary(reader, name) : null;
    }

    @Override
    public DsonExtInt32 readExtInt32(int name) {
        return readName(name) ? CodecHelper.readExtInt32(reader, name) : null;
    }

    @Override
    public DsonExtInt64 readExtInt64(int name) {
        return readName(name) ? CodecHelper.readExtInt64(reader, name) : null;
    }

    @Override
    public DsonExtDouble readExtDouble(int name) {
        return readName(name) ? CodecHelper.readExtDouble(reader, name) : null;
    }

    @Override
    public DsonExtString readExtString(int name) {
        return readName(name) ? CodecHelper.readExtString(reader, name) : null;
    }

    @Override
    public ObjectRef readRef(int name) {
        return readName(name) ? CodecHelper.readRef(reader, name) : null;
    }

    @Override
    public OffsetTimestamp readTimestamp(int name) {
        return readName(name) ? CodecHelper.readTimestamp(reader, name) : null;
    }

    // endregion

    // region object处理

    @Override
    public <T> T readObject(TypeArgInfo<T> typeArgInfo) {
        Objects.requireNonNull(typeArgInfo);
        DsonType dsonType = reader.readDsonType();
        return readContainer(typeArgInfo, dsonType);
    }

    @SuppressWarnings("unchecked")
    @Nullable
    @Override
    public <T> T readObject(int name, TypeArgInfo<T> typeArgInfo) {
        Class<T> declaredType = typeArgInfo.declaredType;
        if (!readName(name)) {
            return (T) ConverterUtils.getDefaultValue(declaredType);
        }

        DsonLiteReader reader = this.reader;
        // 基础类型不能返回null
        if (declaredType.isPrimitive()) {
            return (T) CodecHelper.readPrimitive(reader, name, declaredType);
        }
        if (declaredType == String.class) {
            return (T) CodecHelper.readString(reader, name);
        }
        if (declaredType == byte[].class) {
            return (T) CodecHelper.readBinary(reader, name);
        }

        // 对象类型--需要先读取写入的类型，才可以解码；Object上下文的话，readName已处理
        if (reader.getContextType() == DsonContextType.ARRAY) {
            if (reader.isAtType()) reader.readDsonType();
        }
        DsonType dsonType = reader.getCurrentDsonType();
        if (dsonType == DsonType.NULL) {
            return null;
        }
        if (dsonType.isContainer()) { // 容器类型只能通过codec解码
            return readContainer(typeArgInfo, dsonType);
        }
        // 考虑包装类型
        Class<?> unboxed = ConverterUtils.unboxIfWrapperType(declaredType);
        if (unboxed.isPrimitive()) {
            return (T) CodecHelper.readPrimitive(reader, name, unboxed);
        }
        // 默认类型转换-声明类型可能是个抽象类型，eg：Number
        if (DsonValue.class.isAssignableFrom(declaredType)) {
            return declaredType.cast(DsonLites.readDsonValue(reader));
        }
        return declaredType.cast(CodecHelper.readValue(reader, dsonType, name));
    }

    private <T> T readContainer(TypeArgInfo<T> typeArgInfo, DsonType dsonType) {
        ClassId classId = readClassId(dsonType);
        BinaryPojoCodec<? extends T> codec = findObjectDecoder(typeArgInfo, dsonType, classId);
        if (codec == null) {
            throw DsonCodecException.incompatible(typeArgInfo.declaredType, classId);
        }
        return codec.readObject(this, typeArgInfo);
    }

    @Override
    public void readStartObject(@Nonnull TypeArgInfo<?> typeArgInfo) {
        if (reader.isAtType()) { // 顶层对象适配
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
        if (reader.isAtType()) {  // 顶层对象适配
            reader.readDsonType();
        }
        reader.readStartArray();
    }

    @Override
    public void readEndArray() {
        reader.skipToEndOfObject();
        reader.readEndArray();
    }

    private ClassId readClassId(DsonType dsonType) {
        DsonLiteReader reader = this.reader;
        if (dsonType == DsonType.OBJECT) {
            reader.readStartObject();
        } else {
            reader.readStartArray();
        }
        ClassId classId;
        DsonType nextDsonType = reader.peekDsonType();
        if (nextDsonType == DsonType.HEADER) {
            reader.readDsonType();
            reader.readStartHeader();
            long classGuid = reader.readInt64(DsonHeader.NUMBERS_CLASS_ID);
            classId = converter.options.classIdConverter.toClassId(classGuid);
            reader.skipToEndOfObject();
            reader.readEndHeader();
        } else {
            classId = ClassId.OBJECT;
        }
        reader.backToWaitStart();
        return classId;
    }

    @SuppressWarnings("unchecked")
    private <T> BinaryPojoCodec<? extends T> findObjectDecoder(TypeArgInfo<T> typeArgInfo, DsonType dsonType, ClassId classId) {
        final Class<T> declaredType = typeArgInfo.declaredType;
        if (!classId.isObjectClassId()) {
            TypeMeta typeMeta = converter.typeMetaRegistry.ofId(classId);
            if (typeMeta != null && declaredType.isAssignableFrom(typeMeta.clazz)) {
                // 尝试按真实类型读
                return (BinaryPojoCodec<? extends T>) converter.codecRegistry.get(typeMeta.clazz);
            }
        }
        if (declaredType == Object.class) {
            if (dsonType == DsonType.ARRAY) {
                return (BinaryPojoCodec<? extends T>) converter.codecRegistry.get(List.class);
            } else {
                return (BinaryPojoCodec<? extends T>) converter.codecRegistry.get(Map.class);
            }
        }
        // 尝试按照声明类型读 - 读的时候两者可能是无继承关系的(投影)
        return converter.codecRegistry.get(declaredType);
    }

    // endregion
}