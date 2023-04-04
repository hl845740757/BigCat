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

package cn.wjybxx.bigcat.common.codec.document;

import cn.wjybxx.bigcat.common.annotation.NameIsStable;
import cn.wjybxx.bigcat.common.codec.TypeArgInfo;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.util.Collection;
import java.util.Map;

/**
 * @author wjybxx
 * date 2023/4/3
 */
public interface DocumentWriter extends AutoCloseable {

    // region 基础api

    void writeInt(String name, int value);

    void writeLong(String name, long value);

    void writeFloat(String name, float value);

    void writeDouble(String name, double value);

    void writeBoolean(String name, boolean value);

    void writeString(String name, @Nullable String value);

    /**
     * 向输出流中写入一个字节数组
     */
    void writeBytes(String name, @Nullable byte[] value);

    /**
     * 向输出流中写入一个字节数组，并可以指定偏移量和长度
     *
     * @throws NullPointerException 如果 bytes 为null
     */
    void writeBytes(String name, @Nonnull byte[] bytes, int offset, int length);

    /**
     * 向输出流中写入一个Object
     *
     * @param value       如果对象是{@link Iterable}的子类或{@link DocumentPojoAsArray}的子类或数组，将写为数组格式
     * @param typeArgInfo 对象的类型信息；当对象的运行时类型和声明类型一致时不写入类型信息
     */
    void writeObject(String name, Object value, @Nonnull TypeArgInfo<?> typeArgInfo);

    /**
     * 向输出流中写入一个字段，如果没有对应的简便方法，可以使用该方法
     *
     * @param value 字段的值
     */
    default void writeObject(String name, @Nullable Object value) {
        writeObject(name, value, TypeArgInfo.OBJECT);
    }

    /**
     * 如果存在缓冲区，则刷新缓冲区
     */
    void flush();

    /**
     * 如果存在特殊的资源，应在这里关闭
     */
    @Override
    void close();

    // endregion

    // region 自定义写

    /** 获取当前的上下文类型 */
    DocumentContextType getContextType();

    /**
     * 开始写当前对象
     *
     * @param value       对象的值
     * @param typeArgInfo 对象的类型信息
     * @see #writeStartObject(Object, TypeArgInfo, DocumentContextType)
     */
    default DocumentContextType writeStartObject(Object value, @Nonnull TypeArgInfo<?> typeArgInfo) {
        return writeStartObject(value, typeArgInfo, null);
    }

    /**
     * 开始写当前对象
     *
     * @param value       对象的值
     * @param typeArgInfo 对象的类型信息
     * @param contextType 期望创建的上下文类型；如果未指定，将自动检测value的类型；
     *                    如果要写的value是[数组]或{@link Collection}或{@link DocumentPojoAsArray}将自动按照数组方式写
     */
    DocumentContextType writeStartObject(Object value, @Nonnull TypeArgInfo<?> typeArgInfo, @Nullable DocumentContextType contextType);

    /**
     * 开始写嵌套对象（文档或数组）
     * 如果要写的value是[数组]或{@link Collection}或{@link DocumentPojoAsArray}将自动按照数组方式写
     *
     * @param name        嵌套对象的名字
     * @param value       嵌套对象的值
     * @param typeArgInfo 嵌套对象的类型信息
     * @see #writeStartObject(String, Object, TypeArgInfo, DocumentContextType)
     */
    default DocumentContextType writeStartObject(String name, @Nullable Object value, @Nonnull TypeArgInfo<?> typeArgInfo) {
        return writeStartObject(name, value, typeArgInfo, null);
    }

    /**
     * 开始写嵌套对象（文档或数组）
     * <p>
     * Q：为什么允许{@code value}为null？
     * A：这样我们在上层接口就不需要提供{@code readNull和 writeNull的支持}，用户就可以不关心底层实现
     * <p>
     * 小心：
     * 1.如果value是包装类型，则会写成对象结构，用户需要自行决定 -- 写值可直接调用{@link #writeObject(String, Object)}。
     * 2.你最好只在写特殊容器对象的时候调用该方法。
     *
     * @param name        嵌套对象的名字
     * @param value       嵌套对象的值
     * @param typeArgInfo 嵌套对象的类型信息
     * @param contextType 期望创建的上下文类型；如果未指定，将自动检测value的类型；
     *                    如果要写的value是[数组]或{@link Collection}或{@link DocumentPojoAsArray}将自动按照数组方式写
     * @return 是否是数组格式
     */
    DocumentContextType writeStartObject(String name, @Nullable Object value, @Nonnull TypeArgInfo<?> typeArgInfo,
                                         @Nullable DocumentContextType contextType);

    void writeEndObject();

    /**
     * 获取下一个元素的名字
     * 如果当前是写数组，则返回一个合适的名字，以按照通用接口写数组内元素 -- 用户最好不要对该名字做任何假设。
     * 如果当前是写文档，则抛出上下文错误异常。
     */
    String nextElementName();

    /**
     * 将一个Object类型的key转换为字符串key
     */
    default String encodeName(Object key, TypeArgInfo<?> typeArgInfo) {
        String stringKey = DocumentConverterUtils.encodeName(key, typeArgInfo.declaredType);
        if (stringKey == null) throw new IllegalArgumentException("unsupported keyType " + key.getClass());
        return stringKey;
    }

    // endregion

    // region 便捷方法

    @NameIsStable
    default void writeShort(String name, short value) {
        writeInt(name, value);
    }

    @NameIsStable
    default void writeByte(String name, byte value) {
        writeInt(name, value);
    }

    @NameIsStable
    default void writeChar(String name, char value) {
        writeInt(name, value);
    }

    @NameIsStable
    default void writeCollection(String name, @Nullable Collection<?> collection, TypeArgInfo<?> typeArgInfo) {
        writeObject(name, collection, typeArgInfo);
    }

    @NameIsStable
    default void writeMap(String name, @Nullable Map<?, ?> map, TypeArgInfo<?> typeArgInfo) {
        writeObject(name, map, typeArgInfo);
    }

    // endregion

}