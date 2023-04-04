/*
 * Copyright 2023 wjybxx
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

package cn.wjybxx.bigcat.common.codec.binary.base;

import cn.wjybxx.bigcat.common.codec.TypeArgInfo;
import cn.wjybxx.bigcat.common.codec.binary.BinaryPojoCodecImpl;
import cn.wjybxx.bigcat.common.codec.binary.BinaryPojoCodecScanIgnore;
import cn.wjybxx.bigcat.common.codec.binary.BinaryReader;
import cn.wjybxx.bigcat.common.codec.binary.BinaryWriter;

import javax.annotation.Nonnull;
import java.util.LinkedHashMap;
import java.util.Map;
import java.util.Set;

/**
 * @author wjybxx
 * date 2023/4/4
 */
@SuppressWarnings("rawtypes")
@BinaryPojoCodecScanIgnore
public class MapCodec implements BinaryPojoCodecImpl<Map> {

    @Nonnull
    @Override
    public Class<Map> getEncoderClass() {
        return Map.class;
    }

    @Override
    public void writeObject(Map instance, BinaryWriter writer, TypeArgInfo<?> typeArgInfo) {
        TypeArgInfo<?> ketArgInfo = TypeArgInfo.of(typeArgInfo.typeArg1);
        TypeArgInfo<?> valueArgInfo = TypeArgInfo.of(typeArgInfo.typeArg2);
        @SuppressWarnings("unchecked") Set<Map.Entry<?, ?>> entrySet = instance.entrySet();
        for (Map.Entry<?, ?> entry : entrySet) {
            writer.writeObject(entry.getKey(), ketArgInfo);
            writer.writeObject(entry.getValue(), valueArgInfo);
        }
    }

    @Override
    public Map<?, ?> readObject(BinaryReader reader, TypeArgInfo<?> typeArgInfo) {
        Map<Object, Object> result;
        if (typeArgInfo.factory != null) {
            result = (Map<Object, Object>) typeArgInfo.factory.get();
        } else {
            result = new LinkedHashMap<>();
        }

        TypeArgInfo<?> ketArgInfo = TypeArgInfo.of(typeArgInfo.typeArg1);
        TypeArgInfo<?> valueArgInfo = TypeArgInfo.of(typeArgInfo.typeArg2);
        while (!reader.isAtEndOfObject()) {
            Object key = reader.readObject(ketArgInfo);
            Object value = reader.readObject(valueArgInfo);
            result.put(key, value);
        }
        return result;
    }
}