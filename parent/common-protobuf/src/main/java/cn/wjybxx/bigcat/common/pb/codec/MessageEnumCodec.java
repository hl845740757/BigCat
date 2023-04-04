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

package cn.wjybxx.bigcat.common.pb.codec;

import cn.wjybxx.bigcat.common.codec.TypeArgInfo;
import cn.wjybxx.bigcat.common.codec.binary.BinaryPojoCodecImpl;
import cn.wjybxx.bigcat.common.codec.binary.BinaryReader;
import cn.wjybxx.bigcat.common.codec.binary.BinaryWriter;
import com.google.protobuf.Internal;
import com.google.protobuf.ProtocolMessageEnum;

import javax.annotation.Nonnull;

/**
 * @author wjybxx
 * date 2023/4/2
 */
public class MessageEnumCodec<T extends ProtocolMessageEnum> implements BinaryPojoCodecImpl<T> {

    private final Class<T> clazz;
    private final Internal.EnumLiteMap<T> enumLiteMap;

    MessageEnumCodec(Class<T> clazz, Internal.EnumLiteMap<T> enumLiteMap) {
        this.clazz = clazz;
        this.enumLiteMap = enumLiteMap;
    }

    @Nonnull
    @Override
    public Class<T> getEncoderClass() {
        return clazz;
    }

    @Override
    public void writeObject(T instance, BinaryWriter writer, TypeArgInfo<?> typeArgInfo) {
        writer.writeInt(instance.getNumber());
    }

    @Override
    public T readObject(BinaryReader reader, TypeArgInfo<?> typeArgInfo) {
        int number = reader.readInt();
        return enumLiteMap.findValueByNumber(number); // TODO 是否要让Null非法？
    }
}