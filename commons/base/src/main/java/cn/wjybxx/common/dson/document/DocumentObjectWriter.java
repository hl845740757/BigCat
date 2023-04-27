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

import cn.wjybxx.common.dson.*;
import cn.wjybxx.common.dson.io.Chunk;
import com.google.protobuf.MessageLite;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;

/**
 * 在文档编码中，默认情况下Writer不会写入自定义Object的Null字段，
 * 如果用户期望强制写入null，需要先调用{@link #writeName(String)}，再调用{@link #writeNull(String)}，
 * 这样我们就能知道用户确实需要写入该字段。
 *
 * @author wjybxx
 * date 2023/4/3
 */
@SuppressWarnings("unused")
public interface DocumentObjectWriter extends AutoCloseable {

    String encodeKey(Object key);

    void flush();

    @Override
    void close();

    // region 基础api

    default void writeInt(String name, int value) {
        writeInt(name, value, WireType.VARINT);
    }

    default void writeLong(String name, long value) {
        writeLong(name, value, WireType.VARINT);
    }

    void writeInt(String name, int value, WireType wireType);

    void writeLong(String name, long value, WireType wireType);

    void writeFloat(String name, float value);

    void writeDouble(String name, double value);

    void writeBoolean(String name, boolean value);

    void writeString(String name, @Nullable String value);

    void writeNull(String name);

    //
    void writeBytes(String name, @Nullable byte[] value);

    void writeBytes(String name, DsonBinaryType type, @Nonnull Chunk chunk);

    void writeBinary(String name, DsonBinaryType type, byte[] value);

    void writeBinary(String name, DsonBinary binary);

    //
    void writeExtString(String name, DsonExtString value);

    void writeExtString(String name, DsonExtStringType type, String value);

    void writeExtInt32(String name, DsonExtInt32 value, WireType wireType);

    void writeExtInt32(String name, DsonExtInt32Type type, int value, WireType wireType);

    void writeExtInt64(String name, DsonExtInt64 value, WireType wireType);

    void writeExtInt64(String name, DsonExtInt64Type type, long value, WireType wireType);

    default void writeExtInt32(String name, DsonExtInt32Type type, int value) {
        writeExtInt32(name, type, value, WireType.VARINT);
    }

    default void writeExtInt64(String name, DsonExtInt64Type type, int value) {
        writeExtInt64(name, type, value, WireType.VARINT);
    }

    // endregion

    // region object封装

    <T> void writeObject(String name, T value, TypeArgInfo<?> typeArgInfo);

    default <T> void writeObject(String name, T value) {
        writeObject(name, value, TypeArgInfo.OBJECT);
    }

    /** 写顶层对象 */
    <T> void writeObject(T value, TypeArgInfo<?> typeArgInfo);

    default void writeStartObject(String name, Object value, TypeArgInfo<?> typeArgInfo) {
        writeName(name);
        writeStartObject(value, typeArgInfo);
    }

    void writeStartObject(Object value, TypeArgInfo<?> typeArgInfo);

    void writeEndObject();

    default void writeStartArray(String name, Object value, TypeArgInfo<?> typeArgInfo) {
        writeName(name);
        writeStartArray(value, typeArgInfo);
    }

    void writeStartArray(Object value, TypeArgInfo<?> typeArgInfo);

    void writeEndArray();

    // endregion

    // region 代理

    void writeName(String name);

    /** @see DsonBinWriter#writeValueBytes(int, DsonType, byte[]) */
    void writeValueBytes(String name, DsonType dsonType, byte[] data);

    /**
     * 注意：
     * 该方法和{@link #writeObject(String, Object)}并不相同，直接调用该方法，message将写为一个普通的Binary，
     * 而{@link #writeObject(String, Object)}会查找Message的Codec从而写为一个包含Binary字段的Object。
     */
    void writeMessage(String name, MessageLite messageLite);

    // endregion

    // region 便捷方法

    /**
     * 应当减少 short/byte/char 的使用，尤其应当避免使用其包装类型，使用的越多越难以扩展，越难以支持跨语言等。
     */
    default void writeShort(String name, short value) {
        writeInt(name, value, WireType.VARINT);
    }

    default void writeByte(String name, byte value) {
        writeInt(name, value, WireType.VARINT);
    }

    default void writeChar(String name, char value) {
        writeInt(name, value, WireType.UINT);
    }

    default void writeShort(String name, short value, WireType wireType) {
        writeInt(name, value, wireType);
    }

    default void writeByte(String name, byte value, WireType wireType) {
        writeInt(name, value, wireType);
    }

    default void writeChar(String name, char value, WireType ignore) {
        writeInt(name, value, WireType.UINT);
    }

    // endregion

}