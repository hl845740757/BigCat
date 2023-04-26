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

import cn.wjybxx.common.dson.TypeArgInfo;
import cn.wjybxx.common.dson.WireType;
import cn.wjybxx.common.dson.document.DocumentObjectReader;
import cn.wjybxx.common.dson.document.DocumentObjectWriter;
import cn.wjybxx.common.dson.document.DocumentPojoCodecImpl;
import com.google.protobuf.Internal;
import com.google.protobuf.ProtocolMessageEnum;

import javax.annotation.Nonnull;

/**
 * MessageEnum会写为具有int32字段的Object
 *
 * @author wjybxx
 * date 2023/4/2
 */
public class MessageEnumCodec<T extends ProtocolMessageEnum> implements DocumentPojoCodecImpl<T> {

    private final Class<T> clazz;
    private final Internal.EnumLiteMap<T> enumLiteMap;
    private final String typeName;

    public MessageEnumCodec(Class<T> clazz, Internal.EnumLiteMap<T> enumLiteMap) {
        this.clazz = clazz;
        this.enumLiteMap = enumLiteMap;
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
        writer.writeInt("number", instance.getNumber(), WireType.UINT);
    }

    @Override
    public T readObject(DocumentObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        int number = reader.readInt("number");
        return enumLiteMap.findValueByNumber(number); // TODO 是否要让Null非法？
    }
}