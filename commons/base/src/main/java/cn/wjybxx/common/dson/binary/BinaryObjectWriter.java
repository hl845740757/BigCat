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

package cn.wjybxx.common.dson.binary;

import cn.wjybxx.common.dson.*;
import cn.wjybxx.common.dson.io.Chunk;
import com.google.protobuf.MessageLite;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;

/**
 * 对{@link DsonBinWriter}的封装，主要提供类型管理和兼容性支持
 * 1.对于对象类型，如果value为null，将自动调用{@link #writeNull(int)}
 * 2.
 *
 * @author wjybxx
 * date 2023/3/31
 */
@SuppressWarnings("unused")
public interface BinaryObjectWriter extends AutoCloseable {

    void flush();

    @Override
    void close();

    // region 基础api

    default void writeInt(int name, int value) {
        writeInt(name, value, WireType.VARINT);
    }

    default void writeLong(int name, long value) {
        writeLong(name, value, WireType.VARINT);
    }

    void writeInt(int name, int value, WireType wireType);

    void writeLong(int name, long value, WireType wireType);

    void writeFloat(int name, float value);

    void writeDouble(int name, double value);

    void writeBoolean(int name, boolean value);

    void writeString(int name, @Nullable String value);

    void writeNull(int name);

    //
    void writeBytes(int name, @Nullable byte[] value);

    void writeBytes(int name, DsonBinaryType type, @Nonnull Chunk chunk);

    void writeBinary(int name, DsonBinaryType type, byte[] value);

    void writeBinary(int name, DsonBinary binary);

    //
    void writeExtString(int name, DsonExtString value);

    void writeExtString(int name, DsonExtStringType type, String value);

    void writeExtInt32(int name, DsonExtInt32 value, WireType wireType);

    void writeExtInt32(int name, DsonExtInt32Type type, int value, WireType wireType);

    void writeExtInt64(int name, DsonExtInt64 value, WireType wireType);

    void writeExtInt64(int name, DsonExtInt64Type type, long value, WireType wireType);

    default void writeExtInt32(int name, DsonExtInt32Type type, int value) {
        writeExtInt32(name, type, value, WireType.VARINT);
    }

    default void writeExtInt64(int name, DsonExtInt64Type type, int value) {
        writeExtInt64(name, type, value, WireType.VARINT);
    }

    // endregion

    // region object封装

    <T> void writeObject(int name, T value, TypeArgInfo<?> typeArgInfo);

    default <T> void writeObject(int name, T value) {
        writeObject(name, value, TypeArgInfo.OBJECT);
    }
    //

    /** 写顶层对象 */
    <T> void writeObject(T value, TypeArgInfo<?> typeArgInfo);

    default void writeStartObject(int name, Object value, TypeArgInfo<?> typeArgInfo) {
        writeName(name);
        writeStartObject(value, typeArgInfo);
    }

    default void writeStartArray(int name, Object value, TypeArgInfo<?> typeArgInfo) {
        writeName(name);
        writeStartArray(value, typeArgInfo);
    }

    /** 顶层对象或数组内元素 */
    void writeStartObject(Object value, TypeArgInfo<?> typeArgInfo);

    void writeEndObject();

    /** 顶层对象或数组内元素 */
    void writeStartArray(Object value, TypeArgInfo<?> typeArgInfo);

    void writeEndArray();

    // endregion

    // region 代理

    void writeName(int name);

    /**
     * 注意：
     * 该方法和{@link #writeObject(int, Object)}并不相同，直接调用该方法，message将写为一个普通的Binary，
     * 而{@link #writeObject(int, Object)}会查找Message的Codec从而写为一个包含Binary字段的Object。
     */
    void writeMessage(int name, MessageLite messageLite);

    /** @see DsonBinWriter#writeValueBytes(int, DsonType, byte[]) */
    void writeValueBytes(int name, DsonType dsonType, byte[] data);

    // endregion

    // region 便捷方法

    /**
     * 应当减少 short/byte/char 的使用，尤其应当避免使用其包装类型，使用的越多越难以扩展，越难以支持跨语言等。
     */
    default void writeShort(int name, short value) {
        writeInt(name, value, WireType.VARINT);
    }

    default void writeByte(int name, byte value) {
        writeInt(name, value, WireType.VARINT);
    }

    default void writeChar(int name, char value) {
        writeInt(name, value, WireType.UINT);
    }

    default void writeShort(int name, short value, WireType wireType) {
        writeInt(name, value, wireType);
    }

    default void writeByte(int name, byte value, WireType wireType) {
        writeInt(name, value, wireType);
    }

    default void writeChar(int name, char value, WireType ignore) {
        writeInt(name, value, WireType.UINT);
    }

    // endregion

}