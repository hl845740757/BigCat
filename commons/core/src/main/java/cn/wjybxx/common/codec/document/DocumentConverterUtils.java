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

import cn.wjybxx.common.codec.ConverterUtils;
import cn.wjybxx.common.codec.TypeMeta;
import cn.wjybxx.common.codec.TypeMetaRegistries;
import cn.wjybxx.common.codec.TypeMetaRegistry;
import cn.wjybxx.common.codec.codecs.*;
import cn.wjybxx.common.pb.ProtobufUtils;
import cn.wjybxx.common.props.PropertiesUtils;
import cn.wjybxx.dson.text.ObjectStyle;
import com.google.protobuf.MessageLite;
import com.google.protobuf.ProtocolMessageEnum;

import javax.annotation.Nullable;
import java.lang.invoke.MethodHandles;
import java.lang.reflect.Constructor;
import java.util.*;
import java.util.concurrent.ConcurrentHashMap;
import java.util.function.Supplier;

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

    /** 类型id注册表 */
    private static final TypeMetaRegistry TYPE_META_REGISTRY;
    /** 默认codec注册表 */
    private static final DocumentCodecRegistry CODEC_REGISTRY;

    static {
        Properties properties = System.getProperties();
        NAME_CACHE_SIZE = PropertiesUtils.getInt(properties, "cn.wjybxx.common.codec.document.namecachesize", 200);

        String[] nameCache = arrayElementNameCache = new String[NAME_CACHE_SIZE];
        for (int idx = 0; idx < nameCache.length; idx++) {
            nameCache[idx] = Integer.toString(idx).intern();
        }

        List<DocumentPojoCodec<?>> entryList = List.of(
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
                newCodec(new CollectionCodec<>(Collection.class, null)),
                newCodec(new MapCodec<>(Map.class, null)),

                // 常用具体类型集合 -- 不含默认解码类型的超类
                newCodec(new CollectionCodec<>(LinkedList.class, LinkedList::new)),
                newCodec(new CollectionCodec<>(ArrayDeque.class, ArrayDeque::new)),
                newCodec(new MapCodec<>(IdentityHashMap.class, IdentityHashMap::new)),
                newCodec(new MapCodec<>(ConcurrentHashMap.class, ConcurrentHashMap::new))
        );

        Map<Class<?>, DocumentPojoCodec<?>> codecMap = DocumentCodecRegistries.newCodecMap(entryList);
        CODEC_REGISTRY = new DefaultCodecRegistry(codecMap);

        TYPE_META_REGISTRY = TypeMetaRegistries.fromMetas(
                entryOfClass(int[].class),
                entryOfClass(long[].class),
                entryOfClass(float[].class),
                entryOfClass(double[].class),
                entryOfClass(boolean[].class),
                entryOfClass(String[].class),
                entryOfClass(short[].class),
                entryOfClass(char[].class),

                entryOfClass(Object[].class),
                entryOfClass(Collection.class),
                entryOfClass(Map.class),

                // 常用具体类型集合
                entryOfClass(LinkedList.class),
                entryOfClass(ArrayDeque.class),
                entryOfClass(IdentityHashMap.class),
                entryOfClass(ConcurrentHashMap.class)
        );
    }

    private static TypeMeta entryOfClass(Class<?> clazz) {
        return TypeMeta.of(clazz, ObjectStyle.INDENT, clazz.getSimpleName());
    }

    private static <T> DocumentPojoCodec<T> newCodec(DocumentPojoCodecImpl<T> codecImpl) {
        return new DocumentPojoCodec<>(codecImpl);
    }

    public static DocumentCodecRegistry getDefaultCodecRegistry() {
        return CODEC_REGISTRY;
    }

    public static TypeMetaRegistry getDefaultTypeMetaRegistry() {
        return TYPE_META_REGISTRY;
    }

    @SuppressWarnings({"rawtypes", "unchecked"})
    private static class DefaultCodecRegistry implements DocumentCodecRegistry {

        final Map<Class<?>, DocumentPojoCodec<?>> codecMap;

        final DocumentPojoCodec<Object[]> objectArrayCodec;
        final DocumentPojoCodec<Collection> collectionCodec;
        final DocumentPojoCodec<Map> mapCodec;

        private DefaultCodecRegistry(Map<Class<?>, DocumentPojoCodec<?>> codecMap) {
            this.codecMap = codecMap;

            this.objectArrayCodec = getCodec(codecMap, Object[].class);
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
            if (Collection.class.isAssignableFrom(clazz)) return (DocumentPojoCodec<T>) collectionCodec;
            if (Map.class.isAssignableFrom(clazz)) return (DocumentPojoCodec<T>) mapCodec;
            return null;
        }
    }

    /** 获取数组idx对于的字符串表示 */
    public static String arrayElementName(int idx) {
        if (idx < 0) throw new IllegalArgumentException("invalid idx " + idx);
        if (idx < NAME_CACHE_SIZE) return arrayElementNameCache[idx];
        return Integer.toString(idx);
    }

    // region 特殊类型支持：protobuf,集合,map

    public static List<? extends DocumentPojoCodecImpl<?>> scanProtobuf(final Set<Class<?>> allProtoBufClasses) {
        final List<DocumentPojoCodecImpl<?>> allPojoCodecList = new ArrayList<>(allProtoBufClasses.size());
        // 解析parser
        for (Class<?> clazz : allProtoBufClasses) {
            allPojoCodecList.add(createProtobufCodec(clazz));
        }
        return allPojoCodecList;
    }

    @SuppressWarnings("unchecked")
    public static DocumentPojoCodecImpl<?> createProtobufCodec(Class<?> clazz) {
        // protoBuf消息
        if (MessageLite.class.isAssignableFrom(clazz)) {
            return createMessageCodec((Class<? extends MessageLite>) clazz);
        }
        if (ProtocolMessageEnum.class.isAssignableFrom(clazz)) {
            return createMessageEnumCodec((Class<? extends ProtocolMessageEnum>) clazz);
        }
        throw new IllegalArgumentException("Unsupported class " + clazz);
    }

    public static <T extends MessageLite> MessageCodec<T> createMessageCodec(Class<T> messageClazz) {
        final var enumLiteMap = ProtobufUtils.findParser(messageClazz);
        return new MessageCodec<>(messageClazz, enumLiteMap);
    }

    public static <T extends ProtocolMessageEnum> MessageEnumCodec<T> createMessageEnumCodec(Class<T> messageClazz) {
        final var enumLiteMap = ProtobufUtils.findMapper(messageClazz);
        return new MessageEnumCodec<>(messageClazz, enumLiteMap);
    }

    /** @param lookup 外部缓存实例，避免每次创建的开销 */
    public static <T extends Collection<?>> CollectionCodec<T> createCollectionCodec(MethodHandles.Lookup lookup, Class<T> clazz) throws Throwable {
        Constructor<T> constructor = clazz.getConstructor();
        Supplier<T> factory = noArgConstructorToSupplier(lookup, constructor);
        return new CollectionCodec<>(clazz, factory);
    }

    public static <T extends Map<?, ?>> MapCodec<T> createMapCodec(MethodHandles.Lookup lookup, Class<T> clazz) throws Throwable {
        Constructor<T> constructor = clazz.getConstructor();
        Supplier<T> factory = noArgConstructorToSupplier(lookup, constructor);
        return new MapCodec<>(clazz, factory);
    }

    // endregion
}