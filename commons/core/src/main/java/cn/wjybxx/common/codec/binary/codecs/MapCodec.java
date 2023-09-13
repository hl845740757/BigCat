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

package cn.wjybxx.common.codec.binary.codecs;

import cn.wjybxx.common.codec.ConverterUtils;
import cn.wjybxx.common.codec.TypeArgInfo;
import cn.wjybxx.common.codec.binary.BinaryObjectReader;
import cn.wjybxx.common.codec.binary.BinaryObjectWriter;
import cn.wjybxx.common.codec.binary.BinaryPojoCodecImpl;
import cn.wjybxx.common.codec.binary.BinaryPojoCodecScanIgnore;
import cn.wjybxx.dson.DsonType;

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
public class MapCodec<T extends Map> implements BinaryPojoCodecImpl<T> {

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
    public void writeObject(T instance, BinaryObjectWriter writer, TypeArgInfo<?> typeArgInfo) {
        TypeArgInfo<?> ketArgInfo = TypeArgInfo.of(typeArgInfo.typeArg1);
        TypeArgInfo<?> valueArgInfo = TypeArgInfo.of(typeArgInfo.typeArg2);
        @SuppressWarnings("unchecked") Set<Map.Entry<?, ?>> entrySet = instance.entrySet();
        for (Map.Entry<?, ?> entry : entrySet) {
            writer.writeObject(0, entry.getKey(), ketArgInfo);
            writer.writeObject(0, entry.getValue(), valueArgInfo);
        }
    }

    @Override
    public T readObject(BinaryObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        Map<Object, Object> result = ConverterUtils.newMap(typeArgInfo, factory);
        TypeArgInfo<?> ketArgInfo = TypeArgInfo.of(typeArgInfo.typeArg1);
        TypeArgInfo<?> valueArgInfo = TypeArgInfo.of(typeArgInfo.typeArg2);
        while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
            Object key = reader.readObject(0, ketArgInfo);
            Object value = reader.readObject(0, valueArgInfo);
            result.put(key, value);
        }
        return clazz.cast(result);
    }

}