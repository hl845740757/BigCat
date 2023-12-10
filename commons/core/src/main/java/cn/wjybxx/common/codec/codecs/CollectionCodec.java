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
import java.util.Collection;
import java.util.Objects;
import java.util.function.Supplier;

/**
 * @author wjybxx
 * date 2023/4/4
 */
@SuppressWarnings("rawtypes")
@BinaryPojoCodecScanIgnore
@DocumentPojoCodecScanIgnore
public class CollectionCodec<T extends Collection> implements PojoCodecImpl<T> {

    final Class<T> clazz;
    final Supplier<? extends T> factory;

    public CollectionCodec(Class<T> clazz, Supplier<? extends T> factory) {
        this.clazz = Objects.requireNonNull(clazz);
        this.factory = factory;
    }

    @Nonnull
    @Override
    public Class<T> getEncoderClass() {
        return clazz;
    }

    @Override
    public void writeObject(BinaryObjectWriter writer, T instance, TypeArgInfo<?> typeArgInfo) {
        TypeArgInfo<?> componentArgInfo = TypeArgInfo.of(typeArgInfo.typeArg1);
        for (Object e : instance) {
            writer.writeObject(0, e, componentArgInfo);
        }
    }

    @Override
    public T readObject(BinaryObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        Collection<Object> result = ConverterUtils.newCollection(typeArgInfo, factory);
        TypeArgInfo<?> componentArgInfo = TypeArgInfo.of(typeArgInfo.typeArg1);
        while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
            result.add(reader.readObject(0, componentArgInfo));
        }
        return clazz.cast(result);
    }

    @Override
    public void writeObject(DocumentObjectWriter writer, T instance, TypeArgInfo<?> typeArgInfo, ObjectStyle style) {
        TypeArgInfo<?> componentArgInfo = TypeArgInfo.of(typeArgInfo.typeArg1);
        for (Object e : instance) {
            writer.writeObject(null, e, componentArgInfo, null);
        }
    }

    @Override
    public T readObject(DocumentObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        Collection<Object> result = ConverterUtils.newCollection(typeArgInfo, factory);
        TypeArgInfo<?> componentArgInfo = TypeArgInfo.of(typeArgInfo.typeArg1);
        while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
            result.add(reader.readObject(null, componentArgInfo));
        }
        return clazz.cast(result);
    }
}