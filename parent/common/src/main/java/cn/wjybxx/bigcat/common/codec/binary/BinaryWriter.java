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

import cn.wjybxx.bigcat.common.annotation.NameIsStable;
import cn.wjybxx.bigcat.common.codec.TypeArgInfo;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.util.Collection;
import java.util.Map;

/**
 * @author wjybxx
 * date 2023/3/31
 */
public interface BinaryWriter extends AutoCloseable {

    // region 基础api

    void writeInt(int value);

    void writeLong(long value);

    void writeFloat(float value);

    void writeDouble(double value);

    void writeBoolean(boolean value);

    void writeString(@Nullable String value);

    /**
     * 向输出流中写入一个字节数组
     */
    void writeBytes(@Nullable byte[] value);

    /**
     * 向输出流中写入一个字节数组，并可以指定偏移量和长度
     *
     * @throws NullPointerException 如果 bytes 为null
     */
    void writeBytes(@Nonnull byte[] bytes, int offset, int length);

    /**
     * 向输出流中写入一个Object
     *
     * @param typeArgInfo 对象的类型信息；当对象的运行时类型和声明类型一致时不写入类型信息
     */
    void writeObject(Object value, @Nonnull TypeArgInfo<?> typeArgInfo);

    /**
     * 向输出流中写入一个字段，如果没有对应的简便方法，可以使用该方法
     *
     * @param value 字段的值
     */
    default void writeObject(@Nullable Object value) {
        writeObject(value, TypeArgInfo.OBJECT);
    }

    /**
     * 如果存在缓冲区，则刷新缓冲区
     */
    void flush();

    @Override
    void close();

    // endregion

    // region 自定义写支持

    /**
     * Q：该方法的作用？
     * A：自定义读写可实现一些有用的效果：WriteObjectBytes，WriteReplace...
     * 用户切换{@code typeArgInfo}就可以发起写替换，前提是禁用{@link BinaryPojoCodecImpl#autoStartEnd()}
     * <p>
     * Q：为什么没有{@code writStartArray}方法？
     * A：因为我们在二进制编码中不写入字段名字或number，因此数组和普通对象是一样的。
     * <p>
     * Q：为什么允许{@code value}为null？
     * A：这样我们在上层接口就不需要提供{@code readNull和 writeNull的支持}，用户就可以不关心底层实现
     * <p>
     * 小心：
     * 1.如果value是包装类型，则会写成对象结构，用户需要自行决定 -- 写值可直接调用{@link #writeObject(Object)}。
     * 2.你最好只在写特殊容器对象的时候调用该方法。
     */
    void writeStartObject(@Nullable Object value, @Nonnull TypeArgInfo<?> typeArgInfo);

    void writeEndObject();

    /**
     * 以原始格式写入字节数组，该字节数组会直接拼接在当前writer后面，而不添加任何标记（长度也不记录）
     * <p>
     * Q：该方法有什么用？？？
     * A：{@link BinaryReader#remainObjectBytes()}
     *
     * @throws NullPointerException rawBytes不可以为空
     */
    void writeObjectBytes(@Nonnull byte[] objectBytes);

    // endregion

    // region 便捷方法

    /**
     * 应当减少 short/byte/char 的使用，尤其应当避免使用其包装类型，使用的越多越难以扩展，越难以支持跨语言等。
     */
    default void writeShort(short value) {
        writeInt(value);
    }

    default void writeByte(byte value) {
        writeInt(value);
    }

    default void writeChar(char value) {
        writeInt(value);
    }

    @NameIsStable
    default void writeCollection(@Nullable Collection<?> collection, TypeArgInfo<?> typeArgInfo) {
        writeObject(collection, typeArgInfo);
    }

    @NameIsStable
    default void writeMap(@Nullable Map<?, ?> map, TypeArgInfo<?> typeArgInfo) {
        writeObject(map, typeArgInfo);
    }

    // endregion

}