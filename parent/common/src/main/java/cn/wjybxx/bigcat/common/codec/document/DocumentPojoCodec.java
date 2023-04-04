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

import javax.annotation.Nonnull;

/**
 * @author wjybxx
 * date 2023/4/3
 */
public class DocumentPojoCodec<T> {

    private final DocumentPojoCodecImpl<T> codecImpl;

    public DocumentPojoCodec(DocumentPojoCodecImpl<T> codecImpl) {
        this.codecImpl = codecImpl;
    }

    @Nonnull
    public String getTypeName() {
        return codecImpl.getTypeName();
    }

    @Nonnull
    public Class<T> getEncoderClass() {
        return codecImpl.getEncoderClass();
    }

    /**
     * 从输入流中解析指定对象。
     * 它应该创建对象，并反序列化该类及其所有超类定义的所有要序列化的字段。
     */
    public T readObject(DocumentReader reader, TypeArgInfo<?> typeArgInfo) {
        if (codecImpl.autoStartEnd()) {
            reader.readStartObject(typeArgInfo);
            T result = codecImpl.readObject(reader, typeArgInfo);
            reader.readEndObject();
            return result;
        } else {
            return codecImpl.readObject(reader, typeArgInfo);
        }
    }

    /**
     * 将对象写入输出流。
     * 将对象及其所有超类定义的所有要序列化的字段写入输出流。
     */
    public void writeObject(T instance, DocumentWriter writer, TypeArgInfo<?> typeArgInfo) {
        if (codecImpl.autoStartEnd()) {
            writer.writeStartObject(instance, typeArgInfo);
            codecImpl.writeObject(instance, writer, typeArgInfo);
            writer.writeEndObject();
        } else {
            codecImpl.writeObject(instance, writer, typeArgInfo);
        }
    }

}