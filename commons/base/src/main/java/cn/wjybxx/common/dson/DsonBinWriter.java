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

package cn.wjybxx.common.dson;

import cn.wjybxx.common.dson.io.Chunk;
import com.google.protobuf.MessageLite;

/**
 * 1.写数组普通元素的时候，{@code name}传0，写嵌套对象时使用无name参数的写方法
 * （实在不想定义太多的方法）
 *
 * @author wjybxx
 * date - 2023/4/20
 */
@SuppressWarnings("unused")
public interface DsonBinWriter extends AutoCloseable {

    void flush();

    @Override
    void close();

    /** 当前是否处于等待写入name的状态 */
    boolean isAtName();

    /**
     * 编码的时候，用户总是习惯 name和value 同时写入，
     * 但在写Array或Object成员的时候，不能同时完成，需要先写入number再开始写值
     */
    void writeName(int name);

    // region 简单值
    void writeInt32(int name, int value, WireType wireType);

    void writeInt64(int name, long value, WireType wireType);

    void writeFloat(int name, float value);

    void writeDouble(int name, double value);

    void writeBoolean(int name, boolean value);

    void writeString(int name, String value);

    void writeNull(int name);

    void writeBinary(int name, DsonBinary dsonBinary);

    void writeBinary(int name, byte type, byte[] data);

    /** @param chunk 写入chunk的length区域 */
    void writeBinary(int name, byte type, Chunk chunk);

    void writeExtString(int name, DsonExtString value);

    void writeExtString(int name, byte type, String value);

    void writeExtInt32(int name, DsonExtInt32 value, WireType wireType);

    void writeExtInt32(int name, byte type, int value, WireType wireType);

    void writeExtInt64(int name, DsonExtInt64 value, WireType wireType);

    void writeExtInt64(int name, byte type, long value, WireType wireType);

    // endregion

    // region 容器

    /**
     * 开始写一个数组
     * 1.数组内元素没有名字，因此传0即可
     *
     * <pre>{@code
     *      writer.writeStartArray(name, DocClassId.OBJECT);
     *      for (String coderName: coderNames) {
     *          writer.writeString(0, coderName);
     *      }
     *      writer.writeEndArray();
     * }</pre>
     */
    default void writeStartArray(int name, BinClassId classId) {
        writeName(name);
        writeStartArray(classId);
    }

    void writeStartArray(BinClassId classId);

    void writeEndArray();

    /**
     * 开始写一个普通对象
     * <pre>{@code
     *      writer.writeStartObject(name);
     *      writer.writeString("name", "wjybxx")
     *      writer.writeInt32("age", 28)
     *      writer.writeEndObject();
     * }</pre>
     */
    default void writeStartObject(int name, BinClassId classId) {
        writeName(name);
        writeStartObject(classId);
    }

    void writeStartObject(BinClassId classId);

    void writeEndObject();
    // endregion

    // region 特殊支持

    /**
     * Message最终会写为Binary，子类型为{@link DsonBinaryType#PROTOBUF_MESSAGE}
     */
    void writeMessage(int name, MessageLite messageLite);

    /**
     * 直接写入一个已编码的字节数组
     * 1.请确保合法性
     * 2.支持的类型与读方法相同
     *
     * @param data {@link DsonBinReader#readValueAsBytes(int)}读取的数据
     */
    void writeValueBytes(int name, DsonType type, byte[] data);

    void attachContext(Object value);

    Object attachContext();

    /** 查询当前是否是数组上下文 */
    boolean isArrayContext();

    /** 查询当前是否是Object上下文 */
    boolean isObjectContext();

    // endregion

}