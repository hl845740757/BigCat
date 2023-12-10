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

import cn.wjybxx.common.codec.TypeArgInfo;
import cn.wjybxx.common.codec.codecs.EnumLiteCodec;
import cn.wjybxx.dson.text.ObjectStyle;

import javax.annotation.Nonnull;
import java.util.Objects;

/**
 * @author wjybxx
 * date 2023/4/3
 */
public class DocumentPojoCodec<T> {

    private final DocumentPojoCodecImpl<T> codecImpl;
    private final boolean isArray;

    public DocumentPojoCodec(DocumentPojoCodecImpl<T> codecImpl) {
        Objects.requireNonNull(codecImpl.getEncoderClass());
        this.codecImpl = codecImpl;
        this.isArray = codecImpl.isWriteAsArray();
    }

    @Nonnull
    public Class<T> getEncoderClass() {
        return codecImpl.getEncoderClass();
    }

    /**
     * 从输入流中解析指定对象。
     * 它应该创建对象，并反序列化该类及其所有超类定义的所有要序列化的字段。
     */
    public T readObject(DocumentObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        if (codecImpl.autoStartEnd()) {
            T result;
            if (isArray) {
                reader.readStartArray(typeArgInfo);
                result = codecImpl.readObject(reader, typeArgInfo);
                reader.readEndArray();
            } else {
                reader.readStartObject(typeArgInfo);
                result = codecImpl.readObject(reader, typeArgInfo);
                reader.readEndObject();
            }
            return result;
        } else {
            return codecImpl.readObject(reader, typeArgInfo);
        }
    }

    /**
     * 将对象写入输出流。
     * 将对象及其所有超类定义的所有要序列化的字段写入输出流。
     */
    public void writeObject(DocumentObjectWriter writer, T instance, TypeArgInfo<?> typeArgInfo, ObjectStyle style) {
        if (codecImpl.autoStartEnd()) {
            if (isArray) {
                writer.writeStartArray(instance, typeArgInfo, style);
                codecImpl.writeObject(writer, instance, typeArgInfo, style);
                writer.writeEndArray();
            } else {
                writer.writeStartObject(instance, typeArgInfo, style);
                codecImpl.writeObject(writer, instance, typeArgInfo, style);
                writer.writeEndObject();
            }
        } else {
            codecImpl.writeObject(writer, instance, typeArgInfo, style);
        }
    }

    public boolean isEnumLiteCodec() {
        return codecImpl instanceof EnumLiteCodec;
    }

    @SuppressWarnings("unchecked")
    public T forNumber(int number) {
        if (codecImpl instanceof EnumLiteCodec<?> enumLiteCodec) {
            return (T) enumLiteCodec.forNumber(number);
        }
        throw new UnsupportedOperationException("unexpected forNumber method call");
    }

}