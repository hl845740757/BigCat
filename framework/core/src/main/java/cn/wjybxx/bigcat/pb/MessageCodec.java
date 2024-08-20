/*
 * Copyright 2023-2024 wjybxx(845740757@qq.com)
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

package cn.wjybxx.bigcat.pb;

import cn.wjybxx.base.ObjectUtils;
import cn.wjybxx.dson.text.ObjectStyle;
import cn.wjybxx.dsoncodec.DsonCodec;
import cn.wjybxx.dsoncodec.DsonObjectReader;
import cn.wjybxx.dsoncodec.DsonObjectWriter;
import cn.wjybxx.dsoncodec.TypeInfo;
import cn.wjybxx.dsoncodec.annotations.DsonCodecScanIgnore;
import com.google.protobuf.InvalidProtocolBufferException;
import com.google.protobuf.MessageLite;
import com.google.protobuf.Parser;

import javax.annotation.Nonnull;
import java.util.Objects;
import java.util.function.Supplier;

/**
 * Message会序列化为字节数组，因此不可以作为顶层对象。
 *
 * @author wjybxx
 * date 2023/4/2
 */
@DsonCodecScanIgnore
public class MessageCodec<T extends MessageLite> implements DsonCodec<T> {

    private final Class<T> clazz;
    private final Parser<T> parser;

    public MessageCodec(Class<T> clazz, Parser<T> parser) {
        this.clazz = clazz;
        this.parser = Objects.requireNonNull(parser, "parser");
    }

    @Nonnull
    @Override
    public Class<T> getEncoderClass() {
        return clazz;
    }

    @Override
    public void writeObject(DsonObjectWriter writer, T instance, TypeInfo<?> typeArgInfo, ObjectStyle style) {
        writer.writeBytes(null, instance.toByteArray());
    }

    @Override
    public T readObject(DsonObjectReader reader, TypeInfo<?> typeArgInfo, Supplier<? extends T> factory) {
        byte[] bytes = reader.readBytes(reader.getCurrentName());
        if (bytes == null) {
            return null;
        }
        try {
            return parser.parseFrom(bytes);
        } catch (InvalidProtocolBufferException e) {
            return ObjectUtils.rethrow(e);
        }
    }

}