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

import cn.wjybxx.dson.DsonBinary;
import cn.wjybxx.dson.codec.PojoCodecImpl;
import cn.wjybxx.dson.codec.TypeArgInfo;
import cn.wjybxx.dson.codec.binary.BinaryObjectReader;
import cn.wjybxx.dson.codec.binary.BinaryObjectWriter;
import cn.wjybxx.dson.codec.binary.BinaryPojoCodecScanIgnore;
import cn.wjybxx.dson.codec.document.DocumentObjectReader;
import cn.wjybxx.dson.codec.document.DocumentObjectWriter;
import cn.wjybxx.dson.codec.document.DocumentPojoCodecScanIgnore;
import cn.wjybxx.dson.text.ObjectStyle;
import com.google.protobuf.InvalidProtocolBufferException;
import com.google.protobuf.MessageLite;
import com.google.protobuf.Parser;

import javax.annotation.Nonnull;
import java.util.Objects;

/**
 * message会写为具有一个{@link DsonBinary}字段的Object
 * (也可写为具有一个元素的数组，可以不写name)
 *
 * @author wjybxx
 * date 2023/4/2
 */
@BinaryPojoCodecScanIgnore
@DocumentPojoCodecScanIgnore
public class MessageCodec<T extends MessageLite> implements PojoCodecImpl<T> {

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
    public void writeObject(BinaryObjectWriter writer, T instance, TypeArgInfo<?> typeArgInfo) {
        writer.writeBytes(0, writer.options().pbBinaryType, instance.toByteArray());
    }

    @Override
    public T readObject(BinaryObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        byte[] bytes = reader.readBytes(0);
        try {
            return parser.parseFrom(bytes);
        } catch (InvalidProtocolBufferException e) {
            throw new RuntimeException(e);
        }
    }

    @Override
    public void writeObject(DocumentObjectWriter writer, T instance, TypeArgInfo<?> typeArgInfo, ObjectStyle style) {
        writer.writeBytes("data", writer.options().pbBinaryType, instance.toByteArray());
    }

    @Override
    public T readObject(DocumentObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        byte[] bytes = reader.readBytes("data");
        try {
            return parser.parseFrom(bytes);
        } catch (InvalidProtocolBufferException e) {
            throw new RuntimeException(e);
        }
    }

}