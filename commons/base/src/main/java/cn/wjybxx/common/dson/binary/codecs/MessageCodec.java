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

package cn.wjybxx.common.dson.binary.codecs;

import cn.wjybxx.common.dson.DsonBinary;
import cn.wjybxx.common.dson.TypeArgInfo;
import cn.wjybxx.common.dson.binary.BinaryObjectReader;
import cn.wjybxx.common.dson.binary.BinaryObjectWriter;
import cn.wjybxx.common.dson.binary.BinaryPojoCodecImpl;
import com.google.protobuf.MessageLite;
import com.google.protobuf.Parser;

import javax.annotation.Nonnull;

/**
 * message会写为具有一个{@link DsonBinary}字段的Object
 *
 * @author wjybxx
 * date 2023/4/2
 */
public class MessageCodec<T extends MessageLite> implements BinaryPojoCodecImpl<T> {

    private final Class<T> clazz;
    private final Parser<T> parser;

    public MessageCodec(Class<T> clazz, Parser<T> parser) {
        this.clazz = clazz;
        this.parser = parser;
    }

    @Nonnull
    @Override
    public Class<T> getEncoderClass() {
        return clazz;
    }

    @Override
    public void writeObject(T instance, BinaryObjectWriter writer, TypeArgInfo<?> typeArgInfo) {
        writer.writeMessage(0, instance);
    }

    @Override
    public T readObject(BinaryObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        return reader.readMessage(0, parser);
    }

}