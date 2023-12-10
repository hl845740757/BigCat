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

package cn.wjybxx.common.codec.codecs;

import cn.wjybxx.common.codec.PojoCodecImpl;
import cn.wjybxx.common.codec.TypeArgInfo;
import cn.wjybxx.common.codec.binary.BinaryObjectReader;
import cn.wjybxx.common.codec.binary.BinaryObjectWriter;
import cn.wjybxx.common.codec.binary.BinaryPojoCodecScanIgnore;
import cn.wjybxx.common.codec.document.DocumentObjectReader;
import cn.wjybxx.common.codec.document.DocumentObjectWriter;
import cn.wjybxx.common.codec.document.DocumentPojoCodecScanIgnore;
import cn.wjybxx.dson.WireType;
import cn.wjybxx.dson.text.NumberStyle;
import cn.wjybxx.dson.text.ObjectStyle;
import com.google.protobuf.Internal;
import com.google.protobuf.ProtocolMessageEnum;

import javax.annotation.Nonnull;

/**
 * MessageEnum会写为具有int32字段的Object
 *
 * @author wjybxx
 * date 2023/4/2
 */
@BinaryPojoCodecScanIgnore
@DocumentPojoCodecScanIgnore
public class MessageEnumCodec<T extends ProtocolMessageEnum> implements PojoCodecImpl<T> {

    private final Class<T> clazz;
    private final Internal.EnumLiteMap<T> enumLiteMap;

    public MessageEnumCodec(Class<T> clazz, Internal.EnumLiteMap<T> enumLiteMap) {
        this.clazz = clazz;
        this.enumLiteMap = enumLiteMap;
    }

    @Nonnull
    @Override
    public Class<T> getEncoderClass() {
        return clazz;
    }

    @Override
    public void writeObject(BinaryObjectWriter writer, T instance, TypeArgInfo<?> typeArgInfo) {
        writer.writeInt(0, instance.getNumber(), WireType.UINT);
    }

    @Override
    public T readObject(BinaryObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        int number = reader.readInt(0);
        return enumLiteMap.findValueByNumber(number); // TODO 是否要让Null非法？
    }

    @Override
    public void writeObject(DocumentObjectWriter writer, T instance, TypeArgInfo<?> typeArgInfo, ObjectStyle style) {
        writer.writeInt("number", instance.getNumber(), WireType.UINT, NumberStyle.SIMPLE);
    }

    @Override
    public T readObject(DocumentObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        int number = reader.readInt("number");
        return enumLiteMap.findValueByNumber(number); // TODO 是否要让Null非法？
    }
}