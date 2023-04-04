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

package cn.wjybxx.bigcat.common.codec.document;

import cn.wjybxx.bigcat.common.annotation.NameIsStable;
import cn.wjybxx.bigcat.common.codec.TypeArgInfo;
import cn.wjybxx.bigcat.common.CollectionUtils;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.util.*;

/**
 * 当年，我用MongoDB的BsonReader和BsonWriter，把我难受的要死。
 * 1.在MongoDB提供的Reader/Writer的api中，由于数组内的元素没有名字，因此读写一个值的方法总有两个版本。
 * 2.此外，读写方法不对称，写Document/Array要名字，读的时候不要。..
 * <p>
 * Q：那我们怎么解决这个问题？怎么让读写数组和读写普通对象一样的api？
 * A: 有多种方式，最简单的就是总是让用户传入外层数组的名字。不过，为了以后可扩展，我还是设计了{@link #nextElementName()}方法，
 * 用户总是通过这个api获得下一个数组元素的名字，就可以像正常对象一样写了。
 * 至于我现在实现成下标的字符串还是固定字符串，用户就不需要关心了。
 *
 * @author wjybxx
 * date 2023/4/3
 */
public interface DocumentReader extends AutoCloseable {

    // region 基础api

    /**
     * 当前读取的文档中是否包含某个字段
     */
    boolean containsKey(String name);

    int readInt(String name);

    long readLong(String name);

    float readFloat(String name);

    double readDouble(String name);

    boolean readBoolean(String name);

    String readString(String name);

    byte[] readBytes(String name);

    int readBytes(String name, byte[] out, int offset);

    /**
     * 从输入流中读取一个对象，该方法支持读取到超类对象和投影
     * 注意：
     * 1. 该方法对于无法精确解析的对象，可能返回一个不兼容的类型。
     * eg：一个字段声明为Object类型，但运行时实际类型为Float，如果读取时传入的参数是Object，那么可能会读取为Double。
     * 2. 目标类型可以与写入类型不一致，甚至无继承关系（投影）；当进行投影时，请确保投影的类(T)存在对应的codec，且只能从首字段顺序连续投影。
     *
     * @param typeArgInfo 期望的目标类型信息；可以与写入的类型不一致
     */
    @Nullable
    <T> T readObject(String name, TypeArgInfo<T> typeArgInfo);

    @SuppressWarnings("unchecked")
    @Nullable
    default <T> T readObject(String name) {
        return (T) readObject(name, TypeArgInfo.OBJECT);
    }

    @Override
    void close();

    // endregion

    // region 自定义读方法

    /** 获取当前上下文 -- 通常应该在调用{@link #readStartObject(String, TypeArgInfo)}后才查询 */
    DocumentContextType getContextType();

    /**
     * 读当前对象开始
     */
    void readStartObject(@Nonnull TypeArgInfo<?> typeArgInfo);

    /**
     * 读嵌套对象开始
     *
     * @param typeArgInfo 当前要读的对象的类型信息
     * @return 写入的对象类型：
     * {@code null} 如果之前写入的是null对象
     * {@link Integer}
     * {@link Long}
     * {@link Float}
     * {@link Double}
     * {@link Boolean}
     * {@link String}
     * {@code byte[]} 之前写入的是字节数组
     * {@link Collection}之前写入的是数组 -- 也可以根据{@link #getContextType()}查询是否是数组
     * {@code Custom} 写入的是自定义对象类型(没有类型信息的情况下为传入的声明类型)
     */
    @Nullable
    Class<?> readStartObject(String name, @Nonnull TypeArgInfo<?> typeArgInfo);

    /**
     * 读对象结束
     */
    void readEndObject();

    /**
     * 获取下一个元素的名字
     * 如果当前是写数组，则返回一个合适的名字，以按照通用接口读数组内元素 -- 用户最好不要对该名字做任何假设。
     * 如果当前读的是一个文档，则返回下一个键值对的key。
     */
    String nextElementName();

    /**
     * 查询是否到达了对象的末尾
     * 注意：不建议写入集合的size，写入size的虽然可以提高性能和内存利用，但写入冗余信息通常是不安全的，也不易扩展
     */
    boolean isAtEndOfObject();

    /**
     * 将一个字符串key解码为对应的object
     * 至少应该支持{@link Integer}{@link Long}{@link String}
     */
    default <T> T decodeName(String stringKey, TypeArgInfo<T> typeArgInfo) {
        T key = DocumentConverterUtils.decodeName(stringKey, typeArgInfo.declaredType);
        if (key == null) throw new IllegalArgumentException("unsupported keyType " + typeArgInfo.declaredType);
        return key;
    }
    // endregion


    // region 便捷方法

    default short readShort(String name) {
        return (short) readInt(name);
    }

    default byte readByte(String name) {
        return (byte) readInt(name);
    }

    default char readChar(String name) {
        return (char) readInt(name);
    }

    @NameIsStable
    @SuppressWarnings({"unchecked", "rawtypes"})
    default <C extends Collection> C readCollection(String name, TypeArgInfo<?> typeArgInfo) {
        return (C) readObject(name, typeArgInfo);
    }

    @NameIsStable
    @SuppressWarnings({"unchecked", "rawtypes"})
    default <M extends Map> M readMap(String name, TypeArgInfo<?> typeArgInfo) {
        return (M) readObject(name, typeArgInfo);
    }

    @Nonnull
    default <E> List<E> readImmutableList(String name, Class<E> elementType) {
        @SuppressWarnings("unchecked") final Collection<E> c = readObject(name, TypeArgInfo.ofCollection(Collection.class, ArrayList::new, elementType));
        return CollectionUtils.toImmutableList(c);
    }

    @Nonnull
    default <E> Set<E> readImmutableSet(String name, Class<E> elementType) {
        @SuppressWarnings("unchecked") final Set<E> c = readObject(name, TypeArgInfo.ofCollection(Set.class, HashSet::new, elementType));
        return CollectionUtils.toImmutableSet(c);
    }

    @Nonnull
    default <K, V> Map<K, V> readImmutableMap(String name, Class<K> keyType, Class<V> valueType) {
        @SuppressWarnings("unchecked") final Map<K, V> m = readObject(name, TypeArgInfo.ofMap(Map.class, HashMap::new, keyType, valueType));
        return CollectionUtils.toImmutableMap(m);
    }

    // 具有默认值的方法
    default int readInt(String name, int def) {
        if (containsKey(name)) {
            return readInt(name);
        }
        return def;
    }

    default long readLong(String name, long def) {
        if (containsKey(name)) {
            return readLong(name);
        }
        return def;
    }

    default float readFloat(String name, float def) {
        if (containsKey(name)) {
            return readLong(name);
        }
        return def;
    }

    default double readDouble(String name, double def) {
        if (containsKey(name)) {
            return readLong(name);
        }
        return def;
    }

    default boolean readBoolean(String name, boolean def) {
        if (containsKey(name)) {
            return readBoolean(name);
        }
        return def;
    }

    default String readString(String name, String def) {
        if (containsKey(name)) {
            return readString(name);
        }
        return def;
    }

    default short readShort(String name, short def) {
        if (containsKey(name)) {
            return (short) readInt(name);
        }
        return def;
    }

    default byte readByte(String name, byte def) {
        if (containsKey(name)) {
            return (byte) readInt(name);
        }
        return def;
    }

    default char readChar(String name, char def) {
        if (containsKey(name)) {
            return (char) readInt(name);
        }
        return def;
    }

    // endregion
}