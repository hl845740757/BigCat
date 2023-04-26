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

import cn.wjybxx.common.dson.ConverterUtils;
import cn.wjybxx.common.dson.document.codecs.*;
import org.apache.commons.lang3.math.NumberUtils;

import javax.annotation.Nullable;
import java.util.Collection;
import java.util.List;
import java.util.Map;
import java.util.Set;

/**
 * @author wjybxx
 * date 2023/4/4
 */
public class DocumentConverterUtils extends ConverterUtils {

    /** 枚举对象持久化时，使用number持久化 */
    public static final String NUMBER_KEY = "number";

    /** 数组下标名字缓存大小 */
    private static final int NAME_CACHE_SIZE = NumberUtils.toInt(
            System.getProperty("cn.wjybxx.common.codec.document.namecahesize"), 200);
    /** 下标字符串缓存 */
    private static final String[] arrayElementNameCache = new String[NAME_CACHE_SIZE];

    private static final DocumentPojoCodec<Object[]> OBJECT_ARRAY_CODEC = newCodec(new ObjectArrayCodec());
    @SuppressWarnings("rawtypes")
    private static final DocumentPojoCodec<Set> SET_CODEC = newCodec(new SetCodec());
    @SuppressWarnings("rawtypes")
    private static final DocumentPojoCodec<Collection> COLLECTION_CODEC = newCodec(new CollectionCodec());
    @SuppressWarnings("rawtypes")
    private static final DocumentPojoCodec<Map> MAP_CODEC = newCodec(new MapCodec());

    /** 默认name注册表 */
//    private static final TypeNameRegistry TYPE_NAME_REGISTRY;

    /** 默认codec注册表 */
//    private static final DocumentCodecRegistry CODEC_REGISTRY;

    static {
        String[] nameCache = arrayElementNameCache;
        for (int idx = 0; idx < nameCache.length; idx++) {
            nameCache[idx] = Integer.toString(idx).intern();
        }

        List<DocumentPojoCodec<?>> DEFAULT_CODECS = List.of(
                // 基础类型的数组codec用于避免拆装箱，提高性能
                newCodec(new IntArrayCodec()),
                newCodec(new LongArrayCodec()),
                newCodec(new FloatArrayCodec()),
                newCodec(new DoubleArrayCodec()),
                newCodec(new BooleanArrayCodec()),
                newCodec(new StringArrayCodec()),
                newCodec(new ShortArrayCodec()),
                newCodec(new CharArrayCodec()),

                OBJECT_ARRAY_CODEC,
                SET_CODEC,
                COLLECTION_CODEC,
                MAP_CODEC
        );
//        TYPE_NAME_REGISTRY = TypeNameRegistries.fromTypeNameMap(DEFAULT_CODECS.stream()
//                .collect(Collectors.toMap(DocumentPojoCodec::getEncoderClass, DocumentPojoCodec::getTypeName)));
//        CODEC_REGISTRY = DocumentCodecRegistries.fromPojoCodecs(DEFAULT_CODECS);
    }

    private static <T> DocumentPojoCodec<T> newCodec(DocumentPojoCodecImpl<T> codecImpl) {
        return new DocumentPojoCodec<>(codecImpl);
    }

    /** 获取数组idx对于的字符串表示 */
    public static String arrayElementName(int idx) {
        if (idx < 0) throw new IllegalArgumentException("invalid idx " + idx);
        if (idx < NAME_CACHE_SIZE) return arrayElementNameCache[idx];
        return Integer.toString(idx);
    }

    /** @return 如果返回null，外部自行处理 */
    @Nullable
    public static String encodeName(Object objKey, Class<?> declaredType) {
        if (objKey instanceof String) {
            return (String) objKey;
        }
        Class<?> keyClass = objKey.getClass(); // 基本类型到这已是包装对象
        if (keyClass == Integer.class || keyClass == Long.class || keyClass == Short.class) {
            return objKey.toString(); // 整数
        }
        if (objKey instanceof Enum<?>) { // 枚举
            return objKey.toString();
        }
        return null;
    }

    /** @return 如果返回null，外部自行处理 */
    @SuppressWarnings({"unchecked", "rawtypes"})
    @Nullable
    public static <T> T decodeName(String stringKey, Class<T> declaredType) {
        if (declaredType == String.class || declaredType == Object.class) {
            return (T) stringKey;
        }
        Class<?> keyClass = boxIfPrimitiveType(declaredType);
        if (keyClass == Integer.class) {
            return (T) Integer.valueOf(stringKey);
        }
        if (keyClass == Long.class) {
            return (T) Long.valueOf(stringKey);
        }
        if (keyClass == Short.class) {
            return (T) Short.valueOf(stringKey);
        }
        if (keyClass.isEnum()) {
            return (T) Enum.valueOf((Class<Enum>) keyClass, stringKey);
        }
        return null;
    }

}