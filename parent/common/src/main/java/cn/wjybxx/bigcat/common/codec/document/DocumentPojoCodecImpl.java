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

import cn.wjybxx.bigcat.common.codec.TypeArgInfo;
import cn.wjybxx.bigcat.common.codec.binary.BinaryPojoCodec;

import javax.annotation.Nonnull;

/**
 * @author wjybxx
 * date 2023/4/3
 */
public interface DocumentPojoCodecImpl<T> {

    /**
     * 编解码对象的类型名
     * 如果是数组对象，可以返回{@link #getEncoderClass()}的类型名
     */
    @Nonnull
    String getTypeName();

    /**
     * 获取负责编解码的类对象
     */
    @Nonnull
    Class<T> getEncoderClass();

    /**
     * 从输入流中解析指定对象。
     * 它应该创建对象，并反序列化该类及其所有超类定义的所有要序列化的字段。
     */
    T readObject(DocumentReader reader, TypeArgInfo<?> typeArgInfo);

    /**
     * 将对象写入输出流。
     * 将对象及其所有超类定义的所有要序列化的字段写入输出流。
     */
    void writeObject(T instance, DocumentWriter writer, TypeArgInfo<?> typeArgInfo);

    /**
     * 该方法用于告知{@link BinaryPojoCodec}是否自动调用以下方法
     * {@link DocumentWriter#writeStartObject(Object, TypeArgInfo)} ()}
     * {@link DocumentWriter#writeEndObject()}
     * {@link DocumentReader#readStartObject(String, TypeArgInfo)}
     * {@link DocumentReader#readEndObject()}
     */
    default boolean autoStartEnd() {
        return true;
    }
}