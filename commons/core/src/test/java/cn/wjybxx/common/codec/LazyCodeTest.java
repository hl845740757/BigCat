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

package cn.wjybxx.common.codec;

import cn.wjybxx.common.codec.binary.BinaryObjectReader;
import cn.wjybxx.common.codec.binary.BinaryObjectWriter;
import cn.wjybxx.common.codec.binary.BinaryPojoCodecImpl;
import cn.wjybxx.common.codec.binary.DefaultBinaryConverter;
import cn.wjybxx.dson.DsonLites;
import cn.wjybxx.dson.DsonType;
import org.apache.commons.lang3.RandomStringUtils;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

import javax.annotation.Nonnull;
import java.util.List;
import java.util.Random;

/**
 * 测试中间路由节点不解码，直到目的地后解码是否正确
 *
 * @author wjybxx
 * date 2023/4/4
 */
public class LazyCodeTest {

    @Test
    void testLazyCodec() {
        TypeMetaRegistry typeMetaRegistry = TypeMetaRegistries.fromMetas(
                TypeMeta.of(MyStruct.class, new ClassId(1, 1))
        );

        Random random = new Random();
        NestStruct nestStruct = new NestStruct(random.nextInt(), random.nextLong(),
                random.nextFloat() * 100, random.nextDouble() * 100);
        MyStruct myStruct = new MyStruct(RandomStringUtils.random(10), nestStruct);

        // 源端
        final byte[] bytesSource;
        {
            DefaultBinaryConverter converter = DefaultBinaryConverter.newInstance(
                    List.of(new MyStructCodec(Role.SOURCE)),
                    typeMetaRegistry,
                    ConvertOptions.DEFAULT);
            bytesSource = converter.write(myStruct);
        }

        final byte[] routerBytes;
        // 模拟转发 -- 读进来再写
        {
            DefaultBinaryConverter converter = DefaultBinaryConverter.newInstance(
                    List.of(new MyStructCodec(Role.ROUTER)),
                    typeMetaRegistry,
                    ConvertOptions.DEFAULT);
            routerBytes = converter.write(converter.read(bytesSource));
        }

        // 终端
        MyStruct destStruct;
        {
            DefaultBinaryConverter converter = DefaultBinaryConverter.newInstance(
                    List.of(new MyStructCodec(Role.DESTINATION)),
                    typeMetaRegistry,
                    ConvertOptions.DEFAULT);
            destStruct = (MyStruct) converter.read(routerBytes);
        }
        Assertions.assertEquals(myStruct, destStruct);
    }

    private enum Role {
        SOURCE,
        ROUTER,
        DESTINATION
    }

    private static class MyStructCodec implements BinaryPojoCodecImpl<MyStruct> {

        private final Role role;

        private MyStructCodec(Role role) {
            this.role = role;
        }

        @Nonnull
        @Override
        public Class<MyStruct> getEncoderClass() {
            return MyStruct.class;
        }

        @Override
        public void writeObject(BinaryObjectWriter writer, MyStruct instance, TypeArgInfo<?> typeArgInfo) {
            writer.writeString(DsonLites.makeFullNumber(0, 0), instance.strVal);
            if (role == Role.ROUTER) {
                writer.writeValueBytes(DsonLites.makeFullNumber(0, 1), DsonType.OBJECT, (byte[]) instance.nestStruct);
            } else {
                // 不在编码器里，定制写
                NestStruct nestStruct = (NestStruct) instance.nestStruct;
                writer.writeStartObject(DsonLites.makeFullNumber(0, 1), nestStruct, TypeArgInfo.of(NestStruct.class));
                {
                    writer.writeInt(DsonLites.makeFullNumber(0, 0), nestStruct.intVal);
                    writer.writeLong(DsonLites.makeFullNumber(0, 1), nestStruct.longVal);
                    writer.writeFloat(DsonLites.makeFullNumber(0, 2), nestStruct.floatVal);
                    writer.writeDouble(DsonLites.makeFullNumber(0, 3), nestStruct.doubleVal);
                }
                writer.writeEndObject();
            }
        }

        @Override
        public MyStruct readObject(BinaryObjectReader reader, TypeArgInfo<?> typeArgInfo) {
            String strVal = reader.readString(DsonLites.makeFullNumber(0, 0));
            Object nestStruct;
            if (role == Role.ROUTER) {
                nestStruct = reader.readValueAsBytes(DsonLites.makeFullNumber(0, 1));
            } else {
                reader.readStartObject(DsonLites.makeFullNumber(0, 1), TypeArgInfo.of(NestStruct.class));
                nestStruct = new NestStruct(
                        reader.readInt(DsonLites.makeFullNumber(0, 0)),
                        reader.readLong(DsonLites.makeFullNumber(0, 1)),
                        reader.readFloat(DsonLites.makeFullNumber(0, 2)),
                        reader.readDouble(DsonLites.makeFullNumber(0, 3)));
                reader.readEndObject();
            }
            return new MyStruct(strVal, nestStruct);
        }
    }

    private record NestStruct(int intVal, long longVal, float floatVal, double doubleVal) {

    }

    private record MyStruct(String strVal, Object nestStruct) {

    }

}