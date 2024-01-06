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

import cn.wjybxx.dson.WireType;
import cn.wjybxx.dson.codec.DuplexCodec;
import cn.wjybxx.dson.codec.TypeArgInfo;
import cn.wjybxx.dson.codec.codecs.EnumLiteCodec;
import cn.wjybxx.dson.codec.dson.DsonCodecScanIgnore;
import cn.wjybxx.dson.codec.dson.DsonObjectReader;
import cn.wjybxx.dson.codec.dson.DsonObjectWriter;
import cn.wjybxx.dson.codec.dsonlite.DsonLiteCodecScanIgnore;
import cn.wjybxx.dson.codec.dsonlite.DsonLiteObjectReader;
import cn.wjybxx.dson.codec.dsonlite.DsonLiteObjectWriter;
import cn.wjybxx.dson.text.NumberStyle;
import cn.wjybxx.dson.text.ObjectStyle;
import com.google.protobuf.Internal;
import com.google.protobuf.ProtocolMessageEnum;

import javax.annotation.Nonnull;
import java.util.Objects;

/**
 * MessageEnum会写为具有int32字段的Object
 * (和{@link EnumLiteCodec}相同)
 *
 * @author wjybxx
 * date 2023/4/2
 */
@DsonLiteCodecScanIgnore
@DsonCodecScanIgnore
public class MessageEnumCodec<T extends ProtocolMessageEnum> implements DuplexCodec<T> {

    private final Class<T> clazz;
    private final Internal.EnumLiteMap<T> enumLiteMap;

    public MessageEnumCodec(Class<T> clazz, Internal.EnumLiteMap<T> enumLiteMap) {
        this.clazz = clazz;
        this.enumLiteMap = Objects.requireNonNull(enumLiteMap, "enumLiteMap");
    }

    @Nonnull
    @Override
    public Class<T> getEncoderClass() {
        return clazz;
    }

    @Override
    public void writeObject(DsonLiteObjectWriter writer, T instance, TypeArgInfo<?> typeArgInfo) {
        writer.writeInt(0, instance.getNumber(), WireType.UINT);
    }

    @Override
    public T readObject(DsonLiteObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        int number = reader.readInt(0);
        return enumLiteMap.findValueByNumber(number); // TODO 是否要让Null非法？
    }

    @Override
    public void writeObject(DsonObjectWriter writer, T instance, TypeArgInfo<?> typeArgInfo, ObjectStyle style) {
        writer.writeInt("number", instance.getNumber(), WireType.UINT, NumberStyle.SIMPLE);
    }

    @Override
    public T readObject(DsonObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        int number = reader.readInt("number");
        return enumLiteMap.findValueByNumber(number); // TODO 是否要让Null非法？
    }
}