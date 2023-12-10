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

import cn.wjybxx.common.codec.ConvertOptions;
import cn.wjybxx.common.codec.TypeArgInfo;
import cn.wjybxx.dson.*;
import cn.wjybxx.dson.io.DsonChunk;
import cn.wjybxx.dson.text.INumberStyle;
import cn.wjybxx.dson.text.ObjectStyle;
import cn.wjybxx.dson.text.StringStyle;
import com.google.protobuf.MessageLite;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;

/**
 * 对{@link DsonLiteWriter}的封装，主要提供类型管理和兼容性支持
 * <p>
 * 1.对于对象类型，如果value为null，将自动调用{@link #writeNull(int)}
 * 2.数组内元素name传0
 * 3.业务层必须按照Bean的字段定义顺序写
 *
 * @author wjybxx
 * date 2023/3/31
 */
@SuppressWarnings("unused")
public interface BinaryObjectWriter extends AutoCloseable {

    // region 基础api

    void writeInt(int name, int value, WireType wireType);

    void writeLong(int name, long value, WireType wireType);

    void writeFloat(int name, float value);

    void writeDouble(int name, double value);

    void writeBoolean(int name, boolean value);

    void writeString(int name, String value);

    void writeNull(int name);

    //
    void writeBytes(int name, byte[] value);

    void writeBytes(int name, int type, byte[] value);

    void writeBytes(int name, int type, @Nonnull DsonChunk chunk);

    void writeBinary(int name, DsonBinary binary);

    //
    void writeExtInt32(int name, DsonExtInt32 value, WireType wireType);

    void writeExtInt32(int name, int type, int value, WireType wireType);

    void writeExtInt64(int name, DsonExtInt64 value, WireType wireType);

    void writeExtInt64(int name, int type, long value, WireType wireType);

    void writeExtDouble(int name, DsonExtDouble value);

    void writeExtDouble(int name, int type, double value);

    void writeExtString(int name, DsonExtString value);

    void writeExtString(int name, int type, @Nullable String value);

    // endregion

    // region object封装

    default <T> void writeObject(int name, T value) {
        writeObject(name, value, TypeArgInfo.OBJECT);
    }

    /** 写嵌套对象 */
    <T> void writeObject(int name, T value, TypeArgInfo<?> typeArgInfo);

    /** 写顶层对象 */
    <T> void writeObject(T value, TypeArgInfo<?> typeArgInfo);

    /** 顶层对象或数组内元素 */
    void writeStartObject(@Nonnull Object value, TypeArgInfo<?> typeArgInfo);

    void writeEndObject();

    /** 顶层对象或数组内元素 */
    void writeStartArray(@Nonnull Object value, TypeArgInfo<?> typeArgInfo);

    void writeEndArray();

    // endregion

    // region 特殊接口

    void flush();

    @Override
    void close();

    ConvertOptions options();

    void writeName(int name);

    void writeMessage(int name, int binaryType, MessageLite messageLite);

    void writeValueBytes(int name, DsonType dsonType, byte[] data);

    // endregion

    // region 便捷方法

    default void writeInt(int name, int value) {
        writeInt(name, value, WireType.VARINT);
    }

    default void writeLong(int name, long value) {
        writeLong(name, value, WireType.VARINT);
    }

    default void writeExtInt32(int name, int type, int value) {
        writeExtInt32(name, type, value, WireType.VARINT);
    }

    default void writeExtInt64(int name, int type, int value) {
        writeExtInt64(name, type, value, WireType.VARINT);
    }

    /**
     * 应当减少 short/byte/char 的使用，尤其应当避免使用其包装类型，使用的越多越难以扩展，越难以支持跨语言等。
     */
    default void writeShort(int name, short value) {
        writeInt(name, value, WireType.VARINT);
    }

    default void writeShort(int name, short value, WireType wireType) {
        writeInt(name, value, wireType);
    }

    default void writeByte(int name, byte value) {
        writeInt(name, value, WireType.VARINT);
    }

    default void writeByte(int name, byte value, WireType ignore) {
        writeInt(name, value, WireType.VARINT);
    }

    default void writeChar(int name, char value) {
        writeInt(name, value, WireType.UINT);
    }

    default void writeChar(int name, char value, WireType ignore) {
        writeInt(name, value, WireType.UINT);
    }

    default void writeStartObject(int name, Object value, TypeArgInfo<?> typeArgInfo) {
        writeName(name);
        writeStartObject(value, typeArgInfo);
    }

    default void writeStartArray(int name, Object value, TypeArgInfo<?> typeArgInfo) {
        writeName(name);
        writeStartArray(value, typeArgInfo);
    }

    // endregion

    // region 生成代码的适配方法

    default void writeInt(int name, int value, WireType wireType, INumberStyle ignore) {
        writeInt(name, value, wireType);
    }

    default void writeLong(int name, long value, WireType wireType, INumberStyle ignore) {
        writeLong(name, value, wireType);
    }

    default void writeFloat(int name, float value, INumberStyle ignore) {
        writeFloat(name, value);
    }

    default void writeDouble(int name, double value, INumberStyle ignore) {
        writeDouble(name, value);
    }

    default void writeString(int name, String value, StringStyle ignore) {
        writeString(name, value);
    }

    default void writeExtInt32(int name, int type, int value, WireType wireType, INumberStyle ignore) {
        writeExtInt32(name, type, value, wireType);
    }

    default void writeExtInt64(int name, int type, long value, WireType wireType, INumberStyle ignore) {
        writeExtInt64(name, type, value, wireType);
    }

    default void writeExtDouble(int name, int type, double value, INumberStyle ignore) {
        writeExtDouble(name, type, value);
    }

    default void writeExtString(int name, int type, String value, StringStyle ignore) {
        writeExtString(name, type, value);
    }

    default <T> void writeObject(int name, T value, TypeArgInfo<?> typeArgInfo, ObjectStyle ignore) {
        writeObject(name, value, typeArgInfo);
    }

    // endregion
}