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

package cn.wjybxx.bigcat.common.codec.binary;

import cn.wjybxx.bigcat.common.codec.EntityConverterUtils;
import cn.wjybxx.bigcat.common.codec.binary.base.*;

import javax.annotation.Nullable;
import java.util.*;
import java.util.stream.Collectors;

/**
 * @author wjybxx
 * date 2023/4/1
 */
public class BinaryConverterUtils extends EntityConverterUtils {

    private static final BinaryPojoCodec<Object[]> OBJECT_ARRAY_CODEC = newCodec(new ObjectArrayCodec(), 11);
    @SuppressWarnings("rawtypes")
    private static final BinaryPojoCodec<Set> SET_CODEC = newCodec(new SetCodec(), 12);
    @SuppressWarnings("rawtypes")
    private static final BinaryPojoCodec<Collection> COLLECTION_CODEC = newCodec(new CollectionCodec(), 13);
    @SuppressWarnings("rawtypes")
    private static final BinaryPojoCodec<Map> MAP_CODEC = newCodec(new MapCodec(), 14);

    public static final List<BinaryPojoCodec<?>> DEFAULT_CODECS = List.of(
            // 基础类型的数组codec用于避免拆装箱，提高性能
            newCodec(new IntArrayCodec(), 1),
            newCodec(new LongArrayCodec(), 2),
            newCodec(new FloatArrayCodec(), 3),
            newCodec(new DoubleArrayCodec(), 4),
            newCodec(new BooleanArrayCodec(), 5),
            newCodec(new StringArrayCodec(), 6),
            newCodec(new ShortArrayCodec(), 7),
            newCodec(new CharArrayCodec(), 8),

            OBJECT_ARRAY_CODEC,
            SET_CODEC,
            COLLECTION_CODEC,
            MAP_CODEC
    );

    /** 默认id注册表 */
    private static final TypeIdRegistry TYPE_ID_REGISTRY = TypeIdRegistries.fromTypeIdMap(
            DEFAULT_CODECS.stream()
                    .collect(Collectors.toMap(BinaryPojoCodec::getEncoderClass, BinaryPojoCodec::getWrapedTypeId))
    );
    /** 默认codec注册表 */
    private static final BinaryCodecRegistry CODEC_REGISTRY = BinaryCodecRegistries.fromPojoCodecs(DEFAULT_CODECS);

    private static <T> BinaryPojoCodec<T> newCodec(BinaryPojoCodecImpl<T> codecImpl, int classId) {
        assert classId > 0 : "classId must be positive";
        return new BinaryPojoCodec<>(codecImpl, classId);
    }

    /** 获取默认命名空间下的类型 */
    public static Class<?> classOfId(long typeId) {
        return TYPE_ID_REGISTRY.checkedOfId(typeId);
    }

    /**
     * 获取默认的解码器
     * 如果为Null，外部自行决定如何处理
     * <p>
     * Q: extends 是不是不安全？
     * A：是不安全的，必须是数据格式兼容的子类，超类肯定不行。
     *
     * @param declaredType 对象的声明类型
     */
    @Nullable
    @SuppressWarnings("unchecked")
    public static <T> BinaryPojoCodec<? extends T> getDefaultDecoder(Class<T> declaredType) {
        BinaryPojoCodec<T> codec = CODEC_REGISTRY.get(declaredType);
        if (codec != null) {
            return codec;
        }
        if (declaredType.isArray()) {
            assert !declaredType.getComponentType().isPrimitive() : "primitiveTypeArray cant get here";
            return (BinaryPojoCodec<? extends T>) OBJECT_ARRAY_CODEC;
        }
        if (declaredType.isAssignableFrom(LinkedHashSet.class)) {
            return (BinaryPojoCodec<? extends T>) SET_CODEC;
        }
        if (declaredType.isAssignableFrom(ArrayList.class)) {
            return (BinaryPojoCodec<? extends T>) COLLECTION_CODEC;
        }
        if (declaredType.isAssignableFrom(LinkedHashMap.class)) {
            return (BinaryPojoCodec<? extends T>) MAP_CODEC;
        }
        return null;
    }

    /**
     * 获取默认的编码器
     * 如果为Null，外部自行决定如何处理
     *
     * @param runtimeType 对象的运行时类型
     */
    @Nullable
    @SuppressWarnings("unchecked")
    public static <T> BinaryPojoCodec<? super T> getDefaultEncoder(Class<T> runtimeType) {
        BinaryPojoCodec<T> codec = CODEC_REGISTRY.get(runtimeType);
        if (codec != null) {
            return codec;
        }
        if (runtimeType.isArray()) {
            assert !runtimeType.getComponentType().isPrimitive() : "primitiveTypeArray cant get here";
            return (BinaryPojoCodec<? super T>) OBJECT_ARRAY_CODEC;
        }
        if (Set.class.isAssignableFrom(runtimeType)) {
            return (BinaryPojoCodec<? super T>) SET_CODEC;
        }
        if (Collection.class.isAssignableFrom(runtimeType)) {
            return (BinaryPojoCodec<? super T>) COLLECTION_CODEC;
        }
        if (Map.class.isAssignableFrom(runtimeType)) {
            return (BinaryPojoCodec<? super T>) MAP_CODEC;
        }
        return null;
    }

}