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

package cn.wjybxx.common.dson.binary;

import cn.wjybxx.common.dson.BinClassId;
import cn.wjybxx.common.dson.binary.codecs.*;
import cn.wjybxx.common.dson.codec.ClassIdRegistries;
import cn.wjybxx.common.dson.codec.ClassIdRegistry;
import cn.wjybxx.common.dson.codec.ConverterUtils;

import javax.annotation.Nullable;
import java.util.Collection;
import java.util.List;
import java.util.Map;
import java.util.Set;
import java.util.stream.Collectors;

/**
 * @author wjybxx
 * date 2023/4/1
 */
public class BinaryConverterUtils extends ConverterUtils {

    /** 默认id注册表 */
    private static final ClassIdRegistry<BinClassId> CLASS_ID_REGISTRY;
    /** 默认codec注册表 */
    private static final BinaryCodecRegistry CODEC_REGISTRY;

    static {
        // 基础类型的数组codec用于避免拆装箱，提高性能
        List<Map.Entry<BinaryPojoCodec<?>, BinClassId>> entryList = List.of(
                newCodec(new IntArrayCodec(), 1),
                newCodec(new LongArrayCodec(), 2),
                newCodec(new FloatArrayCodec(), 3),
                newCodec(new DoubleArrayCodec(), 4),
                newCodec(new BooleanArrayCodec(), 5),
                newCodec(new StringArrayCodec(), 6),
                newCodec(new ShortArrayCodec(), 7),
                newCodec(new CharArrayCodec(), 8),

                newCodec(new ObjectArrayCodec(), 11),
                newCodec(new SetCodec(), 12),
                newCodec(new CollectionCodec(), 13),
                newCodec(new MapCodec(), 14)
        );
        final Map<Class<?>, BinClassId> classIdMap = entryList.stream()
                .collect(Collectors.toMap(e -> e.getKey().getEncoderClass(), Map.Entry::getValue));
        CLASS_ID_REGISTRY = ClassIdRegistries.fromClassIdMap(classIdMap);

        Map<Class<?>, BinaryPojoCodec<?>> codecMap = BinaryCodecRegistries.newCodecMap(entryList.stream()
                .map(Map.Entry::getKey)
                .collect(Collectors.toList()));
        CODEC_REGISTRY = new DefaultCodecRegistry(codecMap);
    }

    private static Map.Entry<BinaryPojoCodec<?>, BinClassId> newCodec(BinaryPojoCodecImpl<?> codecImpl, int classId) {
        assert classId > 0 : "classId must be positive";
        return Map.entry(new BinaryPojoCodec<>(codecImpl), new BinClassId(0, classId));
    }

    public static ClassIdRegistry<BinClassId> getDefaultClassIdRegistry() {
        return CLASS_ID_REGISTRY;
    }

    public static BinaryCodecRegistry getDefaultCodecRegistry() {
        return CODEC_REGISTRY;
    }

    @SuppressWarnings({"rawtypes", "unchecked"})
    private static class DefaultCodecRegistry implements BinaryCodecRegistry {

        final Map<Class<?>, BinaryPojoCodec<?>> codecMap;

        final BinaryPojoCodec<Object[]> objectArrayCodec;
        final BinaryPojoCodec<Set> setCodec;
        final BinaryPojoCodec<Collection> collectionCodec;
        final BinaryPojoCodec<Map> mapCodec;

        private DefaultCodecRegistry(Map<Class<?>, BinaryPojoCodec<?>> codecMap) {
            this.codecMap = codecMap;

            this.objectArrayCodec = getCodec(codecMap, Object[].class);
            this.setCodec = getCodec(codecMap, Set.class);
            this.collectionCodec = getCodec(codecMap, Collection.class);
            this.mapCodec = getCodec(codecMap, Map.class);
        }

        private static <T> BinaryPojoCodec<T> getCodec(Map<Class<?>, BinaryPojoCodec<?>> codecMap, Class<T> clazz) {
            BinaryPojoCodec<T> codec = (BinaryPojoCodec<T>) codecMap.get(clazz);
            if (codec == null) throw new IllegalArgumentException(clazz.getName());
            return codec;
        }

        @Nullable
        @Override
        public <T> BinaryPojoCodec<T> get(Class<T> clazz) {
            BinaryPojoCodec<?> codec = codecMap.get(clazz);
            if (codec != null) return (BinaryPojoCodec<T>) codec;

            if (clazz.isArray()) return (BinaryPojoCodec<T>) objectArrayCodec;
            if (Set.class.isAssignableFrom(clazz)) return (BinaryPojoCodec<T>) setCodec;
            if (Collection.class.isAssignableFrom(clazz)) return (BinaryPojoCodec<T>) collectionCodec;
            if (Map.class.isAssignableFrom(clazz)) return (BinaryPojoCodec<T>) mapCodec;
            return null;
        }
    }

}