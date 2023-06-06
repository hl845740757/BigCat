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

package cn.wjybxx.common.dson.document;

import cn.wjybxx.common.dson.*;
import cn.wjybxx.common.dson.codec.ClassIdRegistries;
import cn.wjybxx.common.dson.codec.ClassIdRegistry;
import cn.wjybxx.common.dson.codec.ConverterUtils;
import cn.wjybxx.common.dson.document.codecs.*;
import cn.wjybxx.common.props.IProperties;
import cn.wjybxx.common.props.PropertiesLoader;

import javax.annotation.Nullable;
import java.util.*;
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
    private static final ClassIdRegistry<String> CLASS_ID_REGISTRY;
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

        List<Map.Entry<DocumentPojoCodec<?>, String>> entryList = List.of(
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

        final Map<Class<?>, String> classIdMap = entryList.stream()
                .collect(Collectors.toMap(e -> e.getKey().getEncoderClass(), Map.Entry::getValue));
        CLASS_ID_REGISTRY = ClassIdRegistries.fromClassIdMap(classIdMap);

        Map<Class<?>, DocumentPojoCodec<?>> codecMap = DocumentCodecRegistries.newCodecMap(entryList.stream()
                .map(Map.Entry::getKey)
                .collect(Collectors.toList()));
        CODEC_REGISTRY = new DefaultCodecRegistry(codecMap);

        // 替换Map的Codec，需要先拷贝
        codecMap = new IdentityHashMap<>(codecMap);
        codecMap.put(Map.class, new DocumentPojoCodec<>(new MapAsObjectCodec()));
        CODEC_REGISTRY2 = new DefaultCodecRegistry(codecMap);
    }

    private static Map.Entry<DocumentPojoCodec<?>, String> newCodec(DocumentPojoCodecImpl<?> codecImpl) {
        return Map.entry(new DocumentPojoCodec<>(codecImpl), codecImpl.getTypeName());
    }

    public static ClassIdRegistry<String> getDefaultClassIdRegistry() {
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

    /** 获取数组idx对于的字符串表示 */
    public static String arrayElementName(int idx) {
        if (idx < 0) throw new IllegalArgumentException("invalid idx " + idx);
        if (idx < NAME_CACHE_SIZE) return arrayElementNameCache[idx];
        return Integer.toString(idx);
    }

    // region 直接读取为DsonObject

    /** @return 如果到达文件尾部，则返回null */
    public static DsonValue readTopDsonValue(DsonDocReader reader) {
        DsonType dsonType = reader.readDsonType();
        if (dsonType == DsonType.END_OF_OBJECT) {
            return null;
        }
        if (dsonType == DsonType.OBJECT) {
            return readObject(reader);
        } else {
            return readArray(reader);
        }
    }

    /** 外部需要先readName */
    private static MutableDsonObject<String> readObject(DsonDocReader reader) {
        DsonType dsonType;
        String name;
        DsonValue value;

        MutableDsonObject<String> dsonObject = new MutableDsonObject<>();
        reader.readStartObject();
        while ((dsonType = reader.readDsonType()) != DsonType.END_OF_OBJECT) {
            if (dsonType == DsonType.HEADER) {
                readHeader(reader, dsonObject.getHeader());
            } else {
                name = reader.readName();
                value = readAsDsonValue(reader, dsonType, name);
                dsonObject.put(name, value);
            }
        }
        reader.readEndObject();
        return dsonObject;
    }

    private static void readHeader(DsonDocReader reader, DsonHeader<String> header) {
        DsonType dsonType;
        String name;
        DsonValue value;
        reader.readStartHeader();
        while ((dsonType = reader.readDsonType()) != DsonType.END_OF_OBJECT) {
            name = reader.readName();
            value = readAsDsonValue(reader, dsonType, name);
            header.put(name, value);
        }
        reader.readEndHeader();
    }

    /** 外部需要先readName */
    private static MutableDsonArray<String> readArray(DsonDocReader reader) {
        DsonType dsonType;
        DsonValue value;

        MutableDsonArray<String> dsonArray = new MutableDsonArray<>(8);
        reader.readStartArray();
        while ((dsonType = reader.readDsonType()) != DsonType.END_OF_OBJECT) {
            if (dsonType == DsonType.HEADER) {
                readHeader(reader, dsonArray.getHeader());
            } else {
                value = readAsDsonValue(reader, dsonType, null);
                dsonArray.add(value);
            }
        }
        reader.readEndArray();
        return dsonArray;
    }

    private static DsonValue readAsDsonValue(DsonDocReader reader, DsonType dsonType, String name) {
        return switch (dsonType) {
            case INT32 -> new DsonInt32(reader.readInt32(name));
            case INT64 -> new DsonInt64(reader.readInt64(name));
            case FLOAT -> new DsonFloat(reader.readFloat(name));
            case DOUBLE -> new DsonDouble(reader.readDouble(name));
            case BOOLEAN -> new DsonBool(reader.readBoolean(name));
            case STRING -> new DsonString(reader.readString(name));
            case BINARY -> reader.readBinary(name);
            case EXT_STRING -> reader.readExtString(name);
            case EXT_INT32 -> reader.readExtInt32(name);
            case EXT_INT64 -> reader.readExtInt64(name);
            case REFERENCE -> new DsonObjectRef(reader.readRef(name));
            case NULL -> {
                reader.readNull(name);
                yield DsonNull.INSTANCE;
            }
            case HEADER -> {
                MutableDsonHeader<String> header = new MutableDsonHeader<>();
                readHeader(reader, header);
                yield header;
            }
            case OBJECT -> readObject(reader);
            case ARRAY -> readArray(reader);
            case END_OF_OBJECT -> throw new AssertionError();
        };
    }
    // endregion

}