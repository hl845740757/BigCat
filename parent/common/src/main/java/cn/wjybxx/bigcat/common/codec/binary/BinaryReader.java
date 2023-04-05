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

import cn.wjybxx.bigcat.common.CollectionUtils;
import cn.wjybxx.bigcat.common.annotation.NameIsStable;
import cn.wjybxx.bigcat.common.codec.TypeArgInfo;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.util.*;

/**
 * @author wjybxx
 * date 2023/3/31
 */
public interface BinaryReader extends AutoCloseable {

    // region 基础api

    int readInt();

    long readLong();

    float readFloat();

    double readDouble();

    boolean readBoolean();

    /**
     * @apiNote 理论上任何可读取为字节数组的都可以读取为字符串，因此这里支持bytes和Object读取为String
     */
    String readString();

    /**
     * 从输入流中读取一个字节数组
     *
     * @apiNote 理论上任意数据都可以读取为字节数组，但为避免产生过多的依赖，我们只要求Object可转bytes
     */
    byte[] readBytes();

    /**
     * @param out    缓冲区
     * @param offset out的写入偏移
     * @return 读取到的字节数，如果对方写入的是null，默认返回0
     * @see #readBytes()
     */
    int readBytes(byte[] out, int offset);

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
    <T> T readObject(TypeArgInfo<T> typeArgInfo);

    @SuppressWarnings("unchecked")
    @Nullable
    default <T> T readObject() {
        return (T) readObject(TypeArgInfo.OBJECT);
    }

    @Override
    void close();

    // endregion

    // region 自定义读支持

    /**
     * 用于自定义读对象，自动判断是否是嵌套对象
     *
     * @param typeArgInfo 对象的类型细腻
     * @return 写入的对象类型：
     * {@code null} 如果之前写入的是null对象
     * {@link Integer}
     * {@link Long}
     * {@link Float}
     * {@link Double}
     * {@link Boolean}
     * {@link String}
     * {@code byte[]} 之前写入的是字节数组
     * {@code Custom} 写入的是自定义对象类型(没有类型信息的情况下为传入的声明类型)
     */
    @Nullable
    Class<?> readStartObject(@Nonnull TypeArgInfo<?> typeArgInfo);

    /**
     * 读对象结束
     */
    void readEndObject();

    /**
     * 查询是否到达了对象的末尾
     * 注意：不建议写入集合的size，写入size的虽然可以提高性能和内存利用，但写入冗余信息通常是不安全的，也不易扩展
     */
    boolean isAtEndOfObject();

    /**
     * 将对象的剩余部分读取为字节数组
     * <p>
     * Q：这个方法有什么用？？？？
     * A：1.用户可以选择部分数据不解析，读取之后直接写入另一个writer，可避免中间解码过程，
     * 等到达最终端的时候，又正常读取即可，就像这样：
     * <pre>
     * A端：             B端         C端
     * object->bytes  bytes->bytes bytes->object
     * 有多种方式告诉自己的codec是否应该解码，最简单的方式是设置一个可多线程访问的静态属性。
     * </pre>
     * 2.用户也可以选择在应用层进行解析。
     */
    byte[] remainObjectBytes();

    // endregion

    // region 便捷方法

    /**
     * 应当减少 short/byte/char 的使用，尤其应当避免使用其包装类型，使用的越多越难以扩展，越难以支持跨语言等。
     */
    @NameIsStable
    default short readShort() {
        return (short) readInt();
    }

    @NameIsStable
    default byte readByte() {
        return (byte) readInt();
    }

    @NameIsStable
    default char readChar() {
        return (char) readInt(); // int的0并不是char的第一个字'\0'，但并不想对char做过多支持
    }

    @NameIsStable
    @SuppressWarnings({"unchecked", "rawtypes", "unused"})
    default <C extends Collection> C readCollection(TypeArgInfo<?> typeArgInfo) {
        return (C) readObject(typeArgInfo);
    }

    @NameIsStable
    @SuppressWarnings({"unchecked", "rawtypes", "unused"})
    default <M extends Map> M readMap(TypeArgInfo<?> typeArgInfo) {
        return (M) readObject(typeArgInfo);
    }

    @Nonnull
    default <E> List<E> readImmutableList(Class<E> elementType) {
        @SuppressWarnings("unchecked") final Collection<E> c = readObject(TypeArgInfo.ofCollection(Collection.class, ArrayList::new, elementType));
        return CollectionUtils.toImmutableList(c);
    }

    @Nonnull
    default <E> Set<E> readImmutableSet(Class<E> elementType) {
        @SuppressWarnings("unchecked") final Set<E> c = readObject(TypeArgInfo.ofCollection(Set.class, HashSet::new, elementType));
        return CollectionUtils.toImmutableSet(c);
    }

    @Nonnull
    default <K, V> Map<K, V> readImmutableMap(Class<K> keyType, Class<V> valueType) {
        @SuppressWarnings("unchecked") final Map<K, V> m = readObject(TypeArgInfo.ofMap(Map.class, HashMap::new, keyType, valueType));
        return CollectionUtils.toImmutableMap(m);
    }
    // endregion
}