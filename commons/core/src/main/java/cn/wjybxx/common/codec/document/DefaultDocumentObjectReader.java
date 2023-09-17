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
import cn.wjybxx.common.pool.DefaultObjectPool;
import cn.wjybxx.common.pool.ObjectPool;
import cn.wjybxx.dson.*;
import cn.wjybxx.dson.types.ObjectRef;
import cn.wjybxx.dson.types.OffsetTimestamp;
import com.google.protobuf.Parser;
import it.unimi.dsi.fastutil.objects.ObjectLinkedOpenHashSet;
import org.apache.commons.lang3.StringUtils;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.util.Iterator;
import java.util.Objects;
import java.util.Set;

/**
 * 默认实现之所以限定{@link DsonObjectReader}，是因为文档默认情况下用于解析数据库和文本文件，
 * 文档中的字段顺序可能和类定义不同，因此顺序读的容错较低。
 *
 * @author wjybxx
 * date - 2023/4/23
 */
public class DefaultDocumentObjectReader implements DocumentObjectReader {

    private static final ThreadLocal<ObjectPool<ObjectLinkedOpenHashSet<String>>> LOCAL_POOL
            = ThreadLocal.withInitial(() -> new DefaultObjectPool<>(ObjectLinkedOpenHashSet::new, ObjectLinkedOpenHashSet::clear, 2, 16));

    private final DefaultDocumentConverter converter;
    private final DsonObjectReader reader;
    private final ObjectPool<ObjectLinkedOpenHashSet<String>> keySetPool;

    public DefaultDocumentObjectReader(DefaultDocumentConverter converter, DsonObjectReader reader) {
        this.converter = converter;
        this.reader = reader;
        this.keySetPool = LOCAL_POOL.get(); // 缓存下来，技减少查询
    }

    @Override
    public <T> T decodeKey(String keyString, Class<T> keyDeclared) {
        return DocumentConverterUtils.decodeKey(keyString, keyDeclared, converter.codecRegistry);
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
    public String readName() {
        return reader.isAtName() ? reader.readName() : reader.getCurrentName();
    }

    @Override
    public boolean readName(String name) {
        DsonReader reader = this.reader;
        if (reader.getContextType() == DsonContextType.ARRAY) {
            if (name != null) throw new IllegalArgumentException("the name of array element must be null");
            return true;
        }
        if (reader.isAtValue()) {
            if (reader.getCurrentName().equals(name)) {
                return true;
            }
            reader.skipValue();
        }
        // 用户未调用readDsonType，可指定下一个key的值
        if (reader.isAtType()) {
            KeyIterator keyItr = (KeyIterator) reader.attachment();
            if (keyItr.keySet.contains(name)) {
                keyItr.setNext(name);
                reader.readDsonType();
                reader.readName();
                return true;
            }
            return false;
        } else {
            if (reader.getCurrentDsonType() == DsonType.END_OF_OBJECT) {
                return false;
            }
            reader.readName(name);
            return true;
        }
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
        return readName(name) ? reader.readMessage(name, binaryType, parser) : null;
    }

    @Override
    public byte[] readValueAsBytes(String name) {
        return readName(name) ? reader.readValueAsBytes(name) : null;
    }

    // endregion

    // region 简单值

    @Override
    public int readInt(String name) {
        return readName(name) ? CodecHelper.readInt(reader, name) : 0;
    }

    @Override
    public long readLong(String name) {
        return readName(name) ? CodecHelper.readLong(reader, name) : 0;
    }

    @Override
    public float readFloat(String name) {
        return readName(name) ? CodecHelper.readFloat(reader, name) : 0;
    }

    @Override
    public double readDouble(String name) {
        return readName(name) ? CodecHelper.readDouble(reader, name) : 0;
    }

    @Override
    public boolean readBoolean(String name) {
        return readName(name) && CodecHelper.readBool(reader, name);
    }

    @Override
    public String readString(String name) {
        return readName(name) ? CodecHelper.readString(reader, name) : null;
    }

    @Override
    public void readNull(String name) {
        if (readName(name)) {
            CodecHelper.readNull(reader, name);
        }
    }

    @Override
    public DsonBinary readBinary(String name) {
        return readName(name) ? CodecHelper.readBinary(reader, name) : null;
    }

    @Override
    public DsonExtInt32 readExtInt32(String name) {
        return readName(name) ? CodecHelper.readExtInt32(reader, name) : null;
    }

    @Override
    public DsonExtInt64 readExtInt64(String name) {
        return readName(name) ? CodecHelper.readExtInt64(reader, name) : null;
    }

    @Override
    public DsonExtString readExtString(String name) {
        return readName(name) ? CodecHelper.readExtString(reader, name) : null;
    }

    @Override
    public ObjectRef readRef(String name) {
        return readName(name) ? CodecHelper.readRef(reader, name) : null;
    }

    @Override
    public OffsetTimestamp readTimestamp(String name) {
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
    public <T> T readObject(String name, TypeArgInfo<T> typeArgInfo) {
        Class<T> declaredType = typeArgInfo.declaredType;
        if (!readName(name)) {
            return (T) ConverterUtils.getDefaultValue(declaredType);
        }

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

        // 对象类型--需要先读取写入的类型，才可以解码；Object上下文的话，readName已处理
        if (reader.getContextType() == DsonContextType.ARRAY) {
            if (reader.isAtType()) reader.readDsonType();
        }
        DsonType dsonType = reader.getCurrentDsonType();
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
        if (reader.isAtType()) { // 顶层对象适配
            reader.readDsonType();
        }
        reader.readStartObject();

        KeyIterator keyItr = new KeyIterator(reader.getkeySet(), keySetPool.get());
        reader.setKeyItr(keyItr, DsonNull.INSTANCE);
        reader.attach(keyItr);
    }

    @Override
    public void readEndObject() {
        KeyIterator keyItr = (KeyIterator) reader.attach(null); // 需要在readEndObject之前保存下来
        reader.skipToEndOfObject();
        reader.readEndObject();

        keySetPool.free(keyItr.keyQueue);
        keyItr.keyQueue = null;
    }

    @Override
    public void readStartArray(@Nonnull TypeArgInfo<?> typeArgInfo) {
        if (reader.isAtType()) { // 顶层对象适配
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
            reader.readDsonType();
            reader.readStartHeader();
            clsName = reader.readString(DsonHeader.NAMES_CLASS_NAME);
            reader.skipToEndOfObject();
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

    private static class KeyIterator implements Iterator<String> {

        Set<String> keySet;
        ObjectLinkedOpenHashSet<String> keyQueue;

        public KeyIterator(Set<String> keySet, ObjectLinkedOpenHashSet<String> keyQueue) {
            this.keySet = keySet;
            this.keyQueue = keyQueue;
            keyQueue.addAll(keySet);
        }

        public void setNext(String key) {
            Objects.requireNonNull(key);
            keyQueue.addAndMoveToFirst(key);
        }

        @Override
        public boolean hasNext() {
            return keyQueue.size() > 0;
        }

        @Override
        public String next() {
            return keyQueue.removeFirst();
        }
    }
}