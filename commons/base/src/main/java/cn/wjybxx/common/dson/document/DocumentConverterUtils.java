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

package cn.wjybxx.common.dson.document;

import cn.wjybxx.common.dson.DocClassId;
import cn.wjybxx.common.dson.codec.ClassIdRegistries;
import cn.wjybxx.common.dson.codec.ClassIdRegistry;
import cn.wjybxx.common.dson.codec.ConverterUtils;
import cn.wjybxx.common.dson.document.codecs.*;
import cn.wjybxx.common.props.IProperties;
import cn.wjybxx.common.props.PropertiesLoader;

import javax.annotation.Nullable;
import java.util.Collection;
import java.util.List;
import java.util.Map;
import java.util.Set;
import java.util.stream.Collectors;

/**
 * @author wjybxx
 * date 2023/4/4
 */
public class DocumentConverterUtils extends ConverterUtils {

    /** 枚举对象持久化时，使用number持久化 */
    public static final String NUMBER_KEY = "number";

    /** 数组下标名字缓存大小 */
    private static final int NAME_CACHE_SIZE;
    /** 下标字符串缓存 */
    private static final String[] arrayElementNameCache;

    /** 默认id注册表 */
    private static final ClassIdRegistry<DocClassId> CLASS_ID_REGISTRY;
    /** 默认codec注册表 */
    private static final DocumentCodecRegistry CODEC_REGISTRY;
    /** Map看做普通Object编码的注册表 */
    private static final DocumentCodecRegistry CODEC_REGISTRY2;

    static {
        IProperties properties = PropertiesLoader.wrapProperties(System.getProperties());
        NAME_CACHE_SIZE = properties.getAsInt("cn.wjybxx.common.codec.document.namecahesize", 200);

        String[] nameCache = arrayElementNameCache = new String[NAME_CACHE_SIZE];
        for (int idx = 0; idx < nameCache.length; idx++) {
            nameCache[idx] = Integer.toString(idx).intern();
        }

        List<Map.Entry<DocumentPojoCodec<?>, DocClassId>> entryList = List.of(
                // 基础类型的数组codec用于避免拆装箱，提高性能
                newCodec(new IntArrayCodec()),
                newCodec(new LongArrayCodec()),
                newCodec(new FloatArrayCodec()),
                newCodec(new DoubleArrayCodec()),
                newCodec(new BooleanArrayCodec()),
                newCodec(new StringArrayCodec()),
                newCodec(new ShortArrayCodec()),
                newCodec(new CharArrayCodec()),

                newCodec(new ObjectArrayCodec()),
                newCodec(new SetCodec()),
                newCodec(new CollectionCodec()),
                newCodec(new MapCodec())
        );

        final Map<Class<?>, DocClassId> classIdMap = entryList.stream()
                .collect(Collectors.toMap(e -> e.getKey().getEncoderClass(), Map.Entry::getValue));
        CLASS_ID_REGISTRY = ClassIdRegistries.fromClassIdMap(classIdMap);

        Map<Class<?>, DocumentPojoCodec<?>> codecMap = DocumentCodecRegistries.newCodecMap(entryList.stream()
                .map(Map.Entry::getKey)
                .collect(Collectors.toList()));
        CODEC_REGISTRY = new DefaultCodecRegistry(codecMap);

        // 替换Map的Codec
        codecMap.put(Map.class, new DocumentPojoCodec<>(new MapAsObjectCodec()));
        CODEC_REGISTRY2 = new DefaultCodecRegistry(codecMap);
    }

    private static Map.Entry<DocumentPojoCodec<?>, DocClassId> newCodec(DocumentPojoCodecImpl<?> codecImpl) {
        return Map.entry(new DocumentPojoCodec<>(codecImpl), DocClassId.of(codecImpl.getTypeName()));
    }

    public static ClassIdRegistry<DocClassId> getDefaultClassIdRegistry() {
        return CLASS_ID_REGISTRY;
    }

    /**
     * @param encodeMapAsObject 是否将map看做普通的Object
     */
    public static DocumentCodecRegistry getDefaultCodecRegistry(boolean encodeMapAsObject) {
        return encodeMapAsObject ? CODEC_REGISTRY2 : CODEC_REGISTRY;
    }

    @SuppressWarnings({"rawtypes", "unchecked"})
    private static class DefaultCodecRegistry implements DocumentCodecRegistry {

        final Map<Class<?>, DocumentPojoCodec<?>> codecMap;

        final DocumentPojoCodec<Object[]> objectArrayCodec;
        final DocumentPojoCodec<Set> setCodec;
        final DocumentPojoCodec<Collection> collectionCodec;
        final DocumentPojoCodec<Map> mapCodec;

        private DefaultCodecRegistry(Map<Class<?>, DocumentPojoCodec<?>> codecMap) {
            this.codecMap = codecMap;

            this.objectArrayCodec = getCodec(codecMap, Object[].class);
            this.setCodec = getCodec(codecMap, Set.class);
            this.collectionCodec = getCodec(codecMap, Collection.class);
            this.mapCodec = getCodec(codecMap, Map.class);
        }

        private static <T> DocumentPojoCodec<T> getCodec(Map<Class<?>, DocumentPojoCodec<?>> codecMap, Class<T> clazz) {
            DocumentPojoCodec<T> codec = (DocumentPojoCodec<T>) codecMap.get(clazz);
            if (codec == null) throw new IllegalArgumentException(clazz.getName());
            return codec;
        }

        @Nullable
        @Override
        public <T> DocumentPojoCodec<T> get(Class<T> clazz) {
            DocumentPojoCodec<?> codec = codecMap.get(clazz);
            if (codec != null) return (DocumentPojoCodec<T>) codec;

            if (clazz.isArray()) return (DocumentPojoCodec<T>) objectArrayCodec;
            if (Set.class.isAssignableFrom(clazz)) return (DocumentPojoCodec<T>) setCodec;
            if (Collection.class.isAssignableFrom(clazz)) return (DocumentPojoCodec<T>) collectionCodec;
            if (Map.class.isAssignableFrom(clazz)) return (DocumentPojoCodec<T>) mapCodec;
            return null;
        }
    }

    // region

    /** 获取数组idx对于的字符串表示 */
    public static String arrayElementName(int idx) {
        if (idx < 0) throw new IllegalArgumentException("invalid idx " + idx);
        if (idx < NAME_CACHE_SIZE) return arrayElementNameCache[idx];
        return Integer.toString(idx);
    }

    // endregion

}