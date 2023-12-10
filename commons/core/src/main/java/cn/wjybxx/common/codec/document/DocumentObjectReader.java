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

import cn.wjybxx.common.CollectionUtils;
import cn.wjybxx.common.annotation.StableName;
import cn.wjybxx.common.codec.ConvertOptions;
import cn.wjybxx.common.codec.TypeArgInfo;
import cn.wjybxx.dson.*;
import cn.wjybxx.dson.types.ObjectRef;
import cn.wjybxx.dson.types.OffsetTimestamp;
import com.google.protobuf.Parser;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.util.*;

/**
 * @author wjybxx
 * date 2023/4/3
 */
@SuppressWarnings("unused")
public interface DocumentObjectReader extends AutoCloseable {

    // region 简单值

    int readInt(String name);

    /** 可读取ExtInt64 */
    long readLong(String name);

    float readFloat(String name);

    double readDouble(String name);

    boolean readBoolean(String name);

    /** 可读取ExtString */
    String readString(String name);

    void readNull(String name);

    /** 可读取Binary */
    default byte[] readBytes(String name) {
        DsonBinary binary = readBinary(name);
        return binary == null ? null : binary.getData();
    }

    DsonBinary readBinary(String name);

    DsonExtInt32 readExtInt32(String name);

    DsonExtInt64 readExtInt64(String name);

    DsonExtDouble readExtDouble(String name);

    DsonExtString readExtString(String name);

    ObjectRef readRef(String name);

    OffsetTimestamp readTimestamp(String name);

    // endregion

    // region object封装

    @SuppressWarnings("unchecked")
    @Nullable
    default <T> T readObject(String name) {
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
    <T> T readObject(String name, TypeArgInfo<T> typeArgInfo);

    /** 读顶层对象 */
    <T> T readObject(TypeArgInfo<T> typeArgInfo);

    //

    /** @return 如果存在对应的字段则返回true */
    default boolean readStartObject(String name, @Nonnull TypeArgInfo<?> typeArgInfo) {
        if (readName(name)) {
            readStartObject(typeArgInfo);
            return true;
        }
        return false;
    }

    /** @return 如果存在对应的字段则返回true */
    default boolean readStartArray(String name, @Nonnull TypeArgInfo<?> typeArgInfo) {
        if (readName(name)) {
            readStartArray(typeArgInfo);
            return true;
        }
        return false;
    }

    /** 顶层对象或数组内元素 */
    void readStartObject(@Nonnull TypeArgInfo<?> typeArgInfo);

    void readEndObject();

    /** 顶层对象或数组内元素 */
    void readStartArray(@Nonnull TypeArgInfo<?> typeArgInfo);

    void readEndArray();

    // endregion

    // region 流程

    @Override
    void close();

    ConvertOptions options();

    /** 读取下一个数据的类型 */
    DsonType readDsonType();

    /**
     * 读取下一个值的名字
     * 该方法只能在{@link #readDsonType()}后调用
     */
    String readName();

    /**
     * 读取指定名字的值 -- 可实现随机读
     * 如果尚未调用{@link #readDsonType()}，该方法将尝试跳转到该name所在的字段。
     * 如果已调用{@link #readDsonType()}，则该方法必须与下一个name匹配。
     * 如果reader不支持随机读，当名字不匹配下一个值时将抛出异常。
     * 返回false的情况下，可继续调用该方法或{@link #readDsonType()}读取下一个字段。
     *
     * @return 如果是Object上下午，如果字段存在则返回true，否则返回false；
     * 如果是Array上下文，如果尚未到达数组尾部，则返回true，否则返回false
     */
    boolean readName(String name);

    DsonType getCurrentDsonType();

    String getCurrentName();

    DsonContextType getContextType();

    void skipName();

    void skipValue();

    void skipToEndOfObject();

    <T> T readMessage(String name, int binaryType, @Nonnull Parser<T> parser);

    byte[] readValueAsBytes(String name);

    <T> T decodeKey(String keyString, Class<T> keyDeclared);
    // endregion

    // region 快捷方法

    /**
     * 应当减少 short/byte/char 的使用，尤其应当避免使用其包装类型，使用的越多越难以扩展，越难以支持跨语言等。
     */
    @StableName
    default short readShort(String name) {
        return (short) readInt(name);
    }

    @StableName
    default byte readByte(String name) {
        return (byte) readInt(name);
    }

    @StableName
    default char readChar(String name) {
        return (char) readInt(name);
    }

    @SuppressWarnings("unchecked")
    @Nonnull
    default <E> List<E> readImmutableList(String name, Class<E> elementType) {
        final Collection<E> c = readObject(name, TypeArgInfo.ofCollection(Collection.class, ArrayList::new, elementType));
        return CollectionUtils.toImmutableList(c);
    }

    @SuppressWarnings("unchecked")
    @Nonnull
    default <E> Set<E> readImmutableSet(String name, Class<E> elementType) {
        final Set<E> c = readObject(name, TypeArgInfo.ofCollection(Set.class, HashSet::new, elementType));
        return CollectionUtils.toImmutableSet(c);
    }

    @SuppressWarnings("unchecked")
    @Nonnull
    default <K, V> Map<K, V> readImmutableMap(String name, Class<K> keyType, Class<V> valueType) {
        final Map<K, V> m = readObject(name, TypeArgInfo.ofMap(Map.class, HashMap::new, keyType, valueType));
        return CollectionUtils.toImmutableMap(m);
    }

    // endregion
}