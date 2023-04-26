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

package cn.wjybxx.common.dson.document.codecs;

import cn.wjybxx.common.dson.DsonBinary;
import cn.wjybxx.common.dson.TypeArgInfo;
import cn.wjybxx.common.dson.document.DocumentObjectReader;
import cn.wjybxx.common.dson.document.DocumentObjectWriter;
import cn.wjybxx.common.dson.document.DocumentPojoCodecImpl;
import com.google.protobuf.MessageLite;
import com.google.protobuf.Parser;

import javax.annotation.Nonnull;

/**
 * message会写为具有一个{@link DsonBinary}字段的Object
 *
 * @author wjybxx
 * date 2023/4/2
 */
public class MessageCodec<T extends MessageLite> implements DocumentPojoCodecImpl<T> {

    private final Class<T> clazz;
    private final Parser<T> parser;
    private final String typeName;

    public MessageCodec(Class<T> clazz, Parser<T> parser) {
        this.clazz = clazz;
        this.parser = parser;
        this.typeName = "Protobuf." + clazz.getSimpleName();
    }

    @Nonnull
    @Override
    public String getTypeName() {
        return typeName;
    }

    @Nonnull
    @Override
    public Class<T> getEncoderClass() {
        return clazz;
    }

    @Override
    public void writeObject(T instance, DocumentObjectWriter writer, TypeArgInfo<?> typeArgInfo) {
        writer.writeMessage("value", instance);
    }

    @Override
    public T readObject(DocumentObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        return reader.readMessage("value", parser);
    }

}