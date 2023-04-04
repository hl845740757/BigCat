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

package cn.wjybxx.bigcat.common.codec.binary;

import cn.wjybxx.bigcat.common.codec.TypeArgInfo;

import java.util.Objects;

/**
 * @author wjybxx
 * date 2023/3/31
 */
public class BinaryPojoCodec<T> {

    private final BinaryPojoCodecImpl<T> codecImpl;
    private final long typeId;

    public BinaryPojoCodec(BinaryPojoCodecImpl<T> codecImpl, long typeId) {
        Objects.requireNonNull(codecImpl.getEncoderClass(), "codecImpl.encoderClass");
        this.codecImpl = codecImpl;
        this.typeId = typeId;
    }

    /** 主要用于阅读 */
    public TypeId getWrapedTypeId() {
        return TypeId.ofGuid(typeId);
    }

    /** 获取类型的类型id */
    public long getTypeId() {
        return typeId;
    }

    /** 获取负责编解码的类对象 */
    public Class<T> getEncoderClass() {
        return codecImpl.getEncoderClass();
    }

    /**
     * 从输入流中解析指定对象。
     * 它应该创建对象，并反序列化该类及其所有超类定义的所有要序列化的字段。
     */
    public T readObject(BinaryReader reader, TypeArgInfo<?> typeArgInfo) {
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
    public void writeObject(T instance, BinaryWriter writer, TypeArgInfo<?> typeArgInfo) {
        if (codecImpl.autoStartEnd()) {
            writer.writeStartObject(instance, typeArgInfo);
            codecImpl.writeObject(instance, writer, typeArgInfo);
            writer.writeEndObject();
        } else {
            codecImpl.writeObject(instance, writer, typeArgInfo);
        }
    }

}