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

import cn.wjybxx.common.annotation.Beta;
import com.google.protobuf.Parser;

import javax.annotation.Nonnull;

/**
 * @author wjybxx
 * date - 2023/4/20
 */
@SuppressWarnings("unused")
public interface DsonDocReader extends AutoCloseable {

    @Override
    void close();

    /**
     * 是否到达当前Array/Object的末尾；
     * 1.该查询不会产生状态切换
     * 2.如果该方法返回true，接下来的{@link #readDsonType()}必将返回{@link DsonType#END_OF_OBJECT}
     * <p>
     * 循环的基本写法：
     * <pre>{@code
     *  while(!isAtEndOfObject()) {
     *     readDsonType();
     *     readName();
     *     readValue();
     *  }
     * }</pre>
     */
    boolean isAtEndOfObject();

    /** 当前是否处于应该读取type状态 */
    boolean isAtType();

    /**
     * 读取下一个值的类型
     * 如果到达对象末尾，则返回{@link DsonType#END_OF_OBJECT}
     * <p>
     * 循环的基本写法：
     * <pre>{@code
     *  DsonType dsonType;
     *  while((dsonType = readDsonType()) != DsonType.END_OF_OBJECT) {
     *      readName();
     *      readValue();
     *  }
     * }</pre>
     */
    DsonType readDsonType();

    /**
     * 读取下一个值的name
     */
    String readName();

    /**
     * 读取下一个值的name
     * 如果下一个name不等于期望的值，则抛出异常
     */
    void readName(String name);

    /**
     * 获取当前的数据类型
     * 1.该值在调用任意的读方法后将变化
     * 2.如果尚未执行过{@link #readDsonType()}则抛出异常
     */
    @Nonnull
    DsonType getCurrentDsonType();

    /**
     * 后去当前的字段编号
     * 1.该值在调用任意的读方法后将变化
     * 2.只有在读取值状态下才可访问
     *
     * @return 当前字段的name
     * @throws DsonCodecException 如果当前不是对象上下文
     */
    String getCurrentName();

    /**
     * 获取当前容器对象关联的class信息
     * 1.该值在readStart/readEnd期间保持不变
     * 2.如果尚未执行readStart方法，则抛出异常。
     */
    @Nonnull
    DocClassId getCurrentClassId();

    // region 简单值

    int readInt32(String name);

    long readInt64(String name);

    float readFloat(String name);

    double readDouble(String name);

    boolean readBoolean(String name);

    String readString(String name);

    void readNull(String name);

    DsonBinary readBinary(String name);

    DsonExtString readExtString(String name);

    DsonExtInt32 readExtInt32(String name);

    DsonExtInt64 readExtInt64(String name);

    // endregion

    // region 容器

    /***
     * @return 当前对象的classId;如果之前未写入，则返回默认的ClassId
     */
    @Nonnull
    default DocClassId readStartArray(String name) {
        readName(name);
        return readStartArray();
    }

    /***
     * @return 当前对象的classId;如果之前未写入，则返回默认的ClassId
     */
    @Nonnull
    DocClassId readStartArray();

    void readEndArray();

    /***
     * @return 当前对象的classId;如果之前未写入，则返回默认的ClassId
     */
    @Nonnull
    default DocClassId readStartObject(String name) {
        readName(name);
        return readStartObject();
    }

    /***
     * @return 当前对象的classId;如果之前未写入，则返回默认的ClassId
     */
    @Nonnull
    DocClassId readStartObject();

    void readEndObject();

    // endregion

    // region 特殊支持

    /**
     * 该方法将使reader处于一个等待{@link #readStartArray()}调用的状态
     * 1.该方法会导致reader的上下文切换，会提前进入读数组上下文。
     * 2.会提前读取length和classId信息，{@link #getCurrentClassId()}将返回最新的对象信息
     * 3.该状态下任何读值方法都将触发状态异常错误.
     * 4.需要先读取字段的name，避免更多的重载方法。
     * <p>
     * 该方法的目的是先预读一部分数据，以确定如何解析后面的数据。
     */
    DocClassId prestartArray();

    /**
     * 该方法将使reader处于一个等待{@link #readStartObject()}调用的状态
     *
     * @see #prestartArray()
     */
    DocClassId prestartObject();

    /**
     * 如果当前是数组上下文，则不产生影响；
     * 如果当前是Object上下文，且处于读取Name状态则跳过name，否则抛出状态异常
     */
    void skipName();

    /**
     * 如果当前不处于读值状态则抛出状态异常
     */
    void skipValue();

    /**
     * 跳过Array或Object的剩余内容
     * 调用该方法后，{@link #getCurrentDsonType()}将返回{@link DsonType#END_OF_OBJECT}
     */
    void skipToEndOfObject();

    /**
     * 查看value的概要信息
     * 1.如果当前不处于读值状态则抛出状态异常
     * 2.注意{@link DsonType#EXT_STRING}的length属性问题
     */
    DsonValueSummary peekValueSummary();

    /**
     * 读取一个protobuf消息
     * 只有当前数据是Binary的时候才合法
     */
    <T> T readMessage(String name, @Nonnull Parser<T> parser);

    /**
     * 将value的值读取为字节数组
     * 1.支持类型：String、Binary、Array、Object
     * 2.返回的bytes中去除了value的length信息，
     * 3.该方法主要用于避免中间编解码过程，eg：
     * <pre>
     * A端：             B端         C端
     * object->bytes  bytes->bytes bytes->object
     * </pre>
     */
    byte[] readValueAsBytes(String name);

    /**
     * 添加一个数据到Context上
     * 通过attach的方式，可避免建立相同深度的上下文
     */
    void attachContext(Object value);

    /** 返回添加到Context上的值 */
    Object attachContext();

    /** 查询当前是否是数组上下文 */
    boolean isArrayContext();

    /** 查询当前是否是Object上下文 */
    boolean isObjectContext();

    @Beta
    DsonReaderGuide whatShouldIDo();
}