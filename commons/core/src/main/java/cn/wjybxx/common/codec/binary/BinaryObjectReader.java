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

package cn.wjybxx.common.codec.binary;

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
 * 对{@link DsonLiteReader}的封装，主要提供类型管理和兼容性支持
 * <p>
 * 1.数组内元素 name 传0
 * 2.业务层按照Bean的字段定义顺序读，而不是按照二进制流中的数据顺序读
 * 3.先调用{@link #readName(int)}和先调用{@link #readDsonType()}是不同的，具体可见方法文档说明、
 *
 * @author wjybxx
 * date 2023/3/31
 */
@SuppressWarnings("unused")
public interface BinaryObjectReader extends AutoCloseable {

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

    DsonExtDouble readExtDouble(int name);

    DsonExtString readExtString(int name);

    ObjectRef readRef(int name);

    OffsetTimestamp readTimestamp(int name);

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

    /** @return 如果存在对应的字段则返回true */
    default boolean readStartObject(int name, @Nonnull TypeArgInfo<?> typeArgInfo) {
        if (readName(name)) {
            readStartObject(typeArgInfo);
            return true;
        }
        return false;
    }

    /** @return 如果存在对应的字段则返回true */
    default boolean readStartArray(int name, @Nonnull TypeArgInfo<?> typeArgInfo) {
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

    // region 特殊接口

    @Override
    void close();

    ConvertOptions options();

    /** 读取下一个数据的类型 */
    DsonType readDsonType();

    /**
     * 读取下一个值的名字
     * 该方法只能在{@link #readDsonType()}后调用
     */
    int readName();

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
    boolean readName(int name);

    DsonType getCurrentDsonType();

    int getCurrentName();

    DsonContextType getContextType();

    void skipName();

    void skipValue();

    void skipToEndOfObject();

    <T> T readMessage(int name, int binaryType, @Nonnull Parser<T> parser);

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