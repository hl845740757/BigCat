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

package cn.wjybxx.common.codec.document.codecs;

import cn.wjybxx.dson.DsonType;
import cn.wjybxx.common.codec.TypeArgInfo;
import cn.wjybxx.common.codec.document.DocumentObjectReader;
import cn.wjybxx.common.codec.document.DocumentObjectWriter;
import cn.wjybxx.common.codec.document.DocumentPojoCodecImpl;

import javax.annotation.Nonnull;
import java.util.LinkedHashMap;
import java.util.Map;
import java.util.Set;

/**
 * @author wjybxx
 * date - 2023/4/27
 */
@SuppressWarnings("rawtypes")
public class MapAsObjectCodec implements DocumentPojoCodecImpl<Map> {

    @Nonnull
    @Override
    public Class<Map> getEncoderClass() {
        return Map.class;
    }

    @Override
    public boolean isWriteAsArray() {
        return false;
    }

    @Override
    public void writeObject(Map instance, DocumentObjectWriter writer, TypeArgInfo<?> typeArgInfo) {
        TypeArgInfo<?> valueArgInfo = TypeArgInfo.of(typeArgInfo.typeArg2);
        @SuppressWarnings("unchecked") Set<Map.Entry<?, ?>> entrySet = instance.entrySet();
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
    }

    @Override
    public Map readObject(DocumentObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        Map<Object, Object> result;
        if (typeArgInfo.factory != null) {
            result = (Map<Object, Object>) typeArgInfo.factory.get();
        } else {
            result = new LinkedHashMap<>();
        }

        TypeArgInfo<?> valueArgInfo = TypeArgInfo.of(typeArgInfo.typeArg2);
        while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
            String keyString = reader.readName();
            Object key = reader.decodeKey(keyString, typeArgInfo.typeArg1);
            Object value = reader.readObject(keyString, valueArgInfo);
            result.put(key, value);
        }
        return result;
    }
}