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

package cn.wjybxx.common.dson.binary;

import cn.wjybxx.common.CollectionUtils;
import cn.wjybxx.common.annotation.StableName;
import cn.wjybxx.common.dson.*;
import cn.wjybxx.common.dson.codec.ClassId;
import com.google.protobuf.Parser;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.util.*;

/**
 * 对{@link DsonBinReader}的封装，主要提供类型管理和兼容性支持
 * Dson的元素数据是严格读写，业务层通常不需要如此；
 * Dson的类型信息是{@link ClassId}，业务层其实面对的{@link Class}
 * 不过，暂时还是不打算提供随机读取支持，会大幅增加开销。
 * <p>
 * 1.代理api可参考{@link DsonBinReader}的文档
 * 2.顶层对象和数组内元素{@literal name}传0
 *
 * @author wjybxx
 * date 2023/3/31
 */
@SuppressWarnings("unused")
public interface BinaryObjectReader extends AutoCloseable {

    @Override
    void close();

    // region 简单值

    /** 可读取ExtInt32 */
    int readInt(int name);

    /** 可读取ExtInt64 */
    long readLong(int name);

    float readFloat(int name);

    double readDouble(int name);

    boolean readBoolean(int name);

    /** 可读取ExtString */
    String readString(int name);

    void readNull(int name);

    /** 可读取Binary */
    default byte[] readBytes(int name) {
        DsonBinary binary = readBinary(name);
        return binary == null ? null : binary.getData();
    }

    DsonBinary readBinary(int name);

    DsonExtInt32 readExtInt32(int name);

    DsonExtInt64 readExtInt64(int name);

    DsonExtString readExtString(int name);

    // endregion

    // region object封装

    @SuppressWarnings("unchecked")
    @Nullable
    default <T> T readObject(int name) {
        return (T) readObject(name, TypeArgInfo.OBJECT);
    }

    /**
     * 从输入流中读取一个对象
     * 注意：
     * 1. 该方法对于无法精确解析的对象，可能返回一个不兼容的类型。
     * 2. 目标类型可以与写入类型不一致，甚至无继承关系，只要数据格式兼容即可。
     *
     * @param typeArgInfo 期望的目标类型信息；可以与写入的类型不一致，
     */
    @Nullable
    <T> T readObject(int name, TypeArgInfo<T> typeArgInfo);

    /** 读顶层对象 */
    <T> T readObject(TypeArgInfo<T> typeArgInfo);

    //

    default void readStartObject(int name, @Nonnull TypeArgInfo<?> typeArgInfo) {
        readName(name);
        readStartObject(typeArgInfo);
    }

    default void readStartArray(int name, @Nonnull TypeArgInfo<?> typeArgInfo) {
        readName(name);
        readStartArray(typeArgInfo);
    }

    /** 顶层对象或数组内元素 */
    void readStartObject(@Nonnull TypeArgInfo<?> typeArgInfo);

    void readEndObject();

    /** 顶层对象或数组内元素 */
    void readStartArray(@Nonnull TypeArgInfo<?> typeArgInfo);

    void readEndArray();

    // endregion

    // region 代理

    /**
     * 读取下一个数据类型
     *
     * @see DsonBinReader#readDsonType()
     */
    DsonType readDsonType();

    int readName();

    void readName(int name);

    DsonType getCurrentDsonType();

    int getCurrentName();

    void skipName();

    void skipValue();

    void skipToEndOfObject();

    /**
     * 注意:
     * 该方法和{@link #readObject(int, TypeArgInfo)}并不相同，该方法只能从Binary类型中读取一个Message，
     * 而{@link #readObject(int, TypeArgInfo)}解码的是Object类型的Message对象。
     */
    <T> T readMessage(int name, @Nonnull Parser<T> parser);

    byte[] readValueAsBytes(int name);

    // endregion

    // region 快捷方法

    /**
     * 应当减少 short/byte/char 的使用，尤其应当避免使用其包装类型，使用的越多越难以扩展，越难以支持跨语言等。
     */
    @StableName
    default short readShort(int name) {
        return (short) readInt(name);
    }

    @StableName
    default byte readByte(int name) {
        return (byte) readInt(name);
    }

    @StableName
    default char readChar(int name) {
        return (char) readInt(name);
    }

    @SuppressWarnings("unchecked")
    @Nonnull
    default <E> List<E> readImmutableList(int name, Class<E> elementType) {
        final Collection<E> c = readObject(name, TypeArgInfo.ofCollection(Collection.class, ArrayList::new, elementType));
        return CollectionUtils.toImmutableList(c);
    }

    @SuppressWarnings("unchecked")
    @Nonnull
    default <E> Set<E> readImmutableSet(int name, Class<E> elementType) {
        final Set<E> c = readObject(name, TypeArgInfo.ofCollection(Set.class, HashSet::new, elementType));
        return CollectionUtils.toImmutableSet(c);
    }

    @SuppressWarnings("unchecked")
    @Nonnull
    default <K, V> Map<K, V> readImmutableMap(int name, Class<K> keyType, Class<V> valueType) {
        final Map<K, V> m = readObject(name, TypeArgInfo.ofMap(Map.class, HashMap::new, keyType, valueType));
        return CollectionUtils.toImmutableMap(m);
    }
    // endregion

}