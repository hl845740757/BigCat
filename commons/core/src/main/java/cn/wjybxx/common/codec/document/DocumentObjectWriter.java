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

import cn.wjybxx.common.codec.ConvertOptions;
import cn.wjybxx.common.codec.TypeArgInfo;
import cn.wjybxx.dson.*;
import cn.wjybxx.dson.io.DsonChunk;
import cn.wjybxx.dson.text.*;
import cn.wjybxx.dson.types.ObjectRef;
import cn.wjybxx.dson.types.OffsetTimestamp;
import com.google.protobuf.MessageLite;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;

/**
 * 如果用户期望强制写入null，需要先调用{@link DocumentObjectWriter#writeName(String)}，
 * 再调用{@link DocumentObjectWriter#writeNull(String)}
 *
 * @author wjybxx
 * date 2023/4/3
 */
@SuppressWarnings("unused")
public interface DocumentObjectWriter extends AutoCloseable {

    // region 基础api

    void writeInt(String name, int value, WireType wireType, INumberStyle style);

    void writeLong(String name, long value, WireType wireType, INumberStyle style);

    void writeFloat(String name, float value, INumberStyle style);

    void writeDouble(String name, double value, INumberStyle style);

    void writeBoolean(String name, boolean value);

    void writeString(String name, String value, StringStyle style);

    void writeNull(String name);

    //
    void writeBytes(String name, byte[] value);

    void writeBytes(String name, int type, byte[] value);

    void writeBytes(String name, int type, @Nonnull DsonChunk chunk);

    void writeBinary(String name, DsonBinary binary);

    //
    void writeExtInt32(String name, DsonExtInt32 value, WireType wireType, INumberStyle style);

    void writeExtInt32(String name, int type, int value, WireType wireType, INumberStyle style);

    void writeExtInt64(String name, DsonExtInt64 value, WireType wireType, INumberStyle style);

    void writeExtInt64(String name, int type, long value, WireType wireType, INumberStyle style);

    void writeExtDouble(String name, DsonExtDouble value, INumberStyle style);

    void writeExtDouble(String name, int type, double value, INumberStyle style);

    void writeExtString(String name, DsonExtString value, StringStyle style);

    void writeExtString(String name, int type, @Nullable String value, StringStyle style);

    void writeRef(String name, ObjectRef ref);

    void writeTimestamp(String name, OffsetTimestamp timestamp);

    // endregion

    // region object封装

    default <T> void writeObject(String name, T value) {
        writeObject(name, value, TypeArgInfo.OBJECT, null);
    }

    default <T> void writeObject(String name, T value, TypeArgInfo<?> typeArgInfo) {
        writeObject(name, value, typeArgInfo, null);
    }

    /**
     * 写嵌套对象
     *
     * @param typeArgInfo 对象的类型参数信息
     * @param style       对象的编码风格，如果为null则使用目标类型Codec的默认格式
     */
    <T> void writeObject(String name, T value, TypeArgInfo<?> typeArgInfo, @Nullable IStyle style);

    /** 写顶层对象 */
    <T> void writeObject(T value, TypeArgInfo<?> typeArgInfo, @Nullable ObjectStyle style);

    /** 顶层对象或数组内元素 */
    void writeStartObject(@Nonnull Object value, TypeArgInfo<?> typeArgInfo, ObjectStyle style);

    void writeEndObject();

    /** 顶层对象或数组内元素 */
    void writeStartArray(@Nonnull Object value, TypeArgInfo<?> typeArgInfo, ObjectStyle style);

    void writeEndArray();

    // endregion

    // region 特殊接口

    void flush();

    @Override
    void close();

    ConvertOptions options();

    void writeName(String name);

    void writeMessage(String name, int binaryType, MessageLite messageLite);

    void writeValueBytes(String name, DsonType dsonType, byte[] data);

    String encodeKey(Object key);

    // endregion

    // region 便捷方法

    default void writeInt(String name, int value) {
        writeInt(name, value, WireType.VARINT, NumberStyle.SIMPLE);
    }

    default void writeInt(String name, int value, WireType wireType) {
        writeInt(name, value, wireType, NumberStyle.SIMPLE);
    }

    default void writeLong(String name, long value) {
        writeLong(name, value, WireType.VARINT, NumberStyle.SIMPLE);
    }

    default void writeLong(String name, long value, WireType wireType) {
        writeLong(name, value, wireType, NumberStyle.SIMPLE);
    }

    default void writeString(String name, String value) {
        writeString(name, value, StringStyle.AUTO);
    }

    default void writeExtInt32(String name, int type, int value, WireType wireType) {
        writeExtInt32(name, type, value, wireType, NumberStyle.SIMPLE);
    }

    default void writeExtInt64(String name, int type, long value, WireType wireType) {
        writeExtInt64(name, type, value, wireType, NumberStyle.SIMPLE);
    }

    default void writeExtDouble(String name, int type, double value) {
        writeExtDouble(name, type, value, NumberStyle.SIMPLE);
    }

    default void writeExtString(String name, int type, String value) {
        writeExtString(name, type, value, StringStyle.AUTO);
    }

    /**
     * 应当减少 short/byte/char 的使用，尤其应当避免使用其包装类型，使用的越多越难以扩展，越难以支持跨语言等。
     */
    default void writeShort(String name, short value) {
        writeInt(name, value, WireType.VARINT, NumberStyle.SIMPLE);
    }

    default void writeShort(String name, short value, WireType wireType, INumberStyle style) {
        writeInt(name, value, wireType, style);
    }

    default void writeByte(String name, byte value) {
        writeInt(name, value, WireType.VARINT, NumberStyle.SIMPLE);
    }

    default void writeByte(String name, byte value, WireType wireType, INumberStyle style) {
        writeInt(name, value, WireType.VARINT, style);
    }

    default void writeChar(String name, char value) {
        writeInt(name, value, WireType.UINT, NumberStyle.SIMPLE);
    }

    /** @apiNote 保持签名以确保生成的代码可正确调用 */
    default void writeChar(String name, char value, WireType ignore, INumberStyle style) {
        writeInt(name, value, WireType.UINT, style);
    }

    default void writeStartObject(String name, Object value, TypeArgInfo<?> typeArgInfo) {
        writeName(name);
        writeStartObject(value, typeArgInfo, ObjectStyle.INDENT);
    }

    default void writeStartObject(String name, Object value, TypeArgInfo<?> typeArgInfo, ObjectStyle style) {
        writeName(name);
        writeStartObject(value, typeArgInfo, style);
    }

    default void writeStartObject(Object value, TypeArgInfo<?> typeArgInfo) {
        writeStartObject(value, typeArgInfo, ObjectStyle.INDENT);
    }

    default void writeStartArray(String name, Object value, TypeArgInfo<?> typeArgInfo) {
        writeName(name);
        writeStartArray(value, typeArgInfo, ObjectStyle.INDENT);
    }

    default void writeStartArray(String name, Object value, TypeArgInfo<?> typeArgInfo, ObjectStyle style) {
        writeName(name);
        writeStartArray(value, typeArgInfo, style);
    }

    default void writeStartArray(Object value, TypeArgInfo<?> typeArgInfo) {
        writeStartArray(value, typeArgInfo, ObjectStyle.INDENT);
    }

    // endregion

}