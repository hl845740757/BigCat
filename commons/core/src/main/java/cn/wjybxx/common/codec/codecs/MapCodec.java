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

package cn.wjybxx.common.codec.codecs;

import cn.wjybxx.common.codec.ConverterUtils;
import cn.wjybxx.common.codec.PojoCodecImpl;
import cn.wjybxx.common.codec.TypeArgInfo;
import cn.wjybxx.common.codec.binary.BinaryObjectReader;
import cn.wjybxx.common.codec.binary.BinaryObjectWriter;
import cn.wjybxx.common.codec.binary.BinaryPojoCodecScanIgnore;
import cn.wjybxx.common.codec.document.DocumentObjectReader;
import cn.wjybxx.common.codec.document.DocumentObjectWriter;
import cn.wjybxx.common.codec.document.DocumentPojoCodecScanIgnore;
import cn.wjybxx.dson.DsonType;
import cn.wjybxx.dson.text.ObjectStyle;

import javax.annotation.Nonnull;
import java.util.Map;
import java.util.Objects;
import java.util.Set;
import java.util.function.Supplier;

/**
 * @author wjybxx
 * date 2023/4/4
 */
@SuppressWarnings("rawtypes")
@BinaryPojoCodecScanIgnore
@DocumentPojoCodecScanIgnore
public class MapCodec<T extends Map> implements PojoCodecImpl<T> {

    final Class<T> clazz;
    final Supplier<? extends T> factory;

    public MapCodec(Class<T> clazz, Supplier<? extends T> factory) {
        this.clazz = Objects.requireNonNull(clazz);
        this.factory = factory;
    }

    @Nonnull
    @Override
    public Class<T> getEncoderClass() {
        return clazz;
    }

    @Override
    public boolean autoStartEnd() {
        return false;
    }

    @Override
    public void writeObject(BinaryObjectWriter writer, T instance, TypeArgInfo<?> typeArgInfo) {
        TypeArgInfo<?> ketArgInfo = TypeArgInfo.of(typeArgInfo.typeArg1);
        TypeArgInfo<?> valueArgInfo = TypeArgInfo.of(typeArgInfo.typeArg2);

        writer.writeStartArray(instance, typeArgInfo);
        @SuppressWarnings("unchecked") Set<Map.Entry<?, ?>> entrySet = instance.entrySet();
        for (Map.Entry<?, ?> entry : entrySet) {
            writer.writeObject(0, entry.getKey(), ketArgInfo);
            writer.writeObject(0, entry.getValue(), valueArgInfo);
        }
        writer.writeEndArray();
    }

    @Override
    public T readObject(BinaryObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        Map<Object, Object> result = ConverterUtils.newMap(typeArgInfo, factory);
        TypeArgInfo<?> ketArgInfo = TypeArgInfo.of(typeArgInfo.typeArg1);
        TypeArgInfo<?> valueArgInfo = TypeArgInfo.of(typeArgInfo.typeArg2);

        reader.readStartArray(typeArgInfo);
        while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
            Object key = reader.readObject(0, ketArgInfo);
            Object value = reader.readObject(0, valueArgInfo);
            result.put(key, value);
        }
        reader.readEndArray();
        return clazz.cast(result);
    }

    @Override
    public void writeObject(DocumentObjectWriter writer, T instance, TypeArgInfo<?> typeArgInfo, ObjectStyle style) {
        TypeArgInfo<?> valueArgInfo = TypeArgInfo.of(typeArgInfo.typeArg2);
        @SuppressWarnings("unchecked") Set<Map.Entry<?, ?>> entrySet = instance.entrySet();

        if (writer.options().encodeMapAsObject) {
            writer.writeStartObject(instance, typeArgInfo, style);
            for (Map.Entry<?, ?> entry : entrySet) {
                String keyString = writer.encodeKey(entry.getKey());
                Object value = entry.getValue();
                if (value == null) {
                    // map写为普通的Object的时候，必须要写入Null，否则containsKey会异常；要强制写入Null必须先写入Name
                    writer.writeName(keyString);
                    writer.writeNull(keyString);
                } else {
                    writer.writeObject(keyString, value, valueArgInfo, null);
                }
            }
            writer.writeEndObject();
        } else {
            TypeArgInfo<?> ketArgInfo = TypeArgInfo.of(typeArgInfo.typeArg1);
            writer.writeStartArray(instance, typeArgInfo, style);
            for (Map.Entry<?, ?> entry : entrySet) {
                writer.writeObject(null, entry.getKey(), ketArgInfo, null);
                writer.writeObject(null, entry.getValue(), valueArgInfo, null);
            }
            writer.writeEndArray();
        }
    }

    @Override
    public T readObject(DocumentObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        Map<Object, Object> result = ConverterUtils.newMap(typeArgInfo, factory);
        TypeArgInfo<?> valueArgInfo = TypeArgInfo.of(typeArgInfo.typeArg2);

        if (reader.options().encodeMapAsObject) {
            reader.readStartObject(typeArgInfo);
            while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
                String keyString = reader.readName();
                Object key = reader.decodeKey(keyString, typeArgInfo.typeArg1);
                Object value = reader.readObject(keyString, valueArgInfo);
                result.put(key, value);
            }
            reader.readEndObject();
        } else {
            TypeArgInfo<?> ketArgInfo = TypeArgInfo.of(typeArgInfo.typeArg1);
            reader.readStartArray(typeArgInfo);
            while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
                Object key = reader.readObject(null, ketArgInfo);
                Object value = reader.readObject(null, valueArgInfo);
                result.put(key, value);
            }
            reader.readEndArray();
        }
        return clazz.cast(result);
    }

}