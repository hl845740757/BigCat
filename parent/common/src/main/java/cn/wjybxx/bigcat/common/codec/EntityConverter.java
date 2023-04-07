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

package cn.wjybxx.bigcat.common.codec;

import javax.annotation.Nonnull;

/**
 * @author wjybxx
 * date 2023/3/31
 */
public interface EntityConverter<T> {

    /**
     * @param typeArgInfo 用于支持集合和Map的特殊写入
     */
    @Nonnull
    T write(Object value, @Nonnull TypeArgInfo<?> typeArgInfo);

    /**
     * 如果在写入对象的时候指明了声明类型，即调用了{@link #write(Object, Class)}，则应该调用该方法解码。
     *
     * @param source      数据源
     * @param typeArgInfo 投影的对象
     */
    <U> U read(T source, TypeArgInfo<U> typeArgInfo);

    @Nonnull
    default T write(Object value) {
        return write(value, TypeArgInfo.OBJECT);
    }

    default Object read(@Nonnull T source) {
        return read(source, TypeArgInfo.OBJECT);
    }

    /**
     * @param declaredType 如果传入的声明类型与对象的实际类型一致，则可能会省去编码结果中的类型信息
     *                     eg.{@code write(value, value.getClass())}将总是不写入类型信息。
     */
    @Nonnull
    default T write(Object value, @Nonnull Class<?> declaredType) {
        return write(value, TypeArgInfo.of(declaredType));
    }

    default <U> U read(@Nonnull T source, Class<U> declaredType) {
        return read(source, TypeArgInfo.of(declaredType));
    }

    /**
     * 写一个对象，但不写入对象自身的类型信息
     * 嵌套对象的信息会写入
     */
    @Nonnull
    default T writeNoTypeKey(@Nonnull Object value) {
        return write(value, TypeArgInfo.of(value.getClass()));
    }

    /**
     * 克隆一个对象。
     * 注意：返回值的类型不一定和原始对象相同，这通常发生在集合对象上。
     *
     * @param typeArgInfo 用于确定返回结果类型
     */
    default <U> U cloneObject(Object value, TypeArgInfo<U> typeArgInfo) {
        if (value == null) {
            return null;
        }
        final T out = write(value, typeArgInfo);
        return read(out, typeArgInfo);
    }

}