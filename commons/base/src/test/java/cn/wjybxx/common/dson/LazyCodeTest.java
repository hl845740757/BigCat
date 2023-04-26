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

package cn.wjybxx.common.dson;

import cn.wjybxx.common.dson.binary.BinaryObjectReader;
import cn.wjybxx.common.dson.binary.BinaryObjectWriter;
import cn.wjybxx.common.dson.binary.BinaryPojoCodecImpl;
import cn.wjybxx.common.dson.binary.DefaultBinaryConverter;
import org.apache.commons.lang3.RandomStringUtils;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

import javax.annotation.Nonnull;
import java.util.List;
import java.util.Map;
import java.util.Random;
import java.util.Set;

/**
 * 测试中间路由节点不解码，直到目的地后解码是否正确
 *
 * @author wjybxx
 * date 2023/4/4
 */
public class LazyCodeTest {

    @Test
    void testLazyCodec() {
        BinClassId binClassId = new BinClassId(1, 1);

        Random random = new Random();
        NestStruct nestStruct = new NestStruct(random.nextInt(), random.nextLong(),
                random.nextFloat() * 100, random.nextDouble() * 100);
        MyStruct myStruct = new MyStruct(RandomStringUtils.random(10), nestStruct);

        // 源端
        final byte[] bytesSource;
        {
            DefaultBinaryConverter converter = DefaultBinaryConverter.newInstance(Set.of(),
                    List.of(new MyStructCodec(Role.SOURCE)),
                    Map.of(MyStruct.class, binClassId), 32);
            bytesSource = converter.write(myStruct);
        }

        final byte[] routerBytes;
        // 模拟转发 -- 读进来再写
        {
            DefaultBinaryConverter converter = DefaultBinaryConverter.newInstance(Set.of(),
                    List.of(new MyStructCodec(Role.ROUTER)),
                    Map.of(MyStruct.class, binClassId), 32);
            routerBytes = converter.write(converter.read(bytesSource));
        }

        // 终端
        MyStruct destStruct;
        {
            DefaultBinaryConverter converter = DefaultBinaryConverter.newInstance(Set.of(),
                    List.of(new MyStructCodec(Role.DESTINATION)),
                    Map.of(MyStruct.class, binClassId), 32);
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
        public void writeObject(MyStruct instance, BinaryObjectWriter writer, TypeArgInfo<?> typeArgInfo) {
            writer.writeString(Dsons.makeFullNumber(0, 0), instance.strVal);
            if (role == Role.ROUTER) {
                writer.writeValueBytes(Dsons.makeFullNumber(0, 1), DsonType.OBJECT, (byte[]) instance.nestStruct);
            } else {
                // 不在编码器里，定制写
                NestStruct nestStruct = (NestStruct) instance.nestStruct;
                writer.writeStartObject(Dsons.makeFullNumber(0, 1), nestStruct, TypeArgInfo.of(NestStruct.class));
                {
                    writer.writeInt(Dsons.makeFullNumber(0, 0), nestStruct.intVal);
                    writer.writeLong(Dsons.makeFullNumber(0, 1), nestStruct.longVal);
                    writer.writeFloat(Dsons.makeFullNumber(0, 2), nestStruct.floatVal);
                    writer.writeDouble(Dsons.makeFullNumber(0, 3), nestStruct.doubleVal);
                }
                writer.writeEndObject();
            }
        }

        @Override
        public MyStruct readObject(BinaryObjectReader reader, TypeArgInfo<?> typeArgInfo) {
            String strVal = reader.readString(Dsons.makeFullNumber(0, 0));
            Object nestStruct;
            if (role == Role.ROUTER) {
                nestStruct = reader.readValueAsBytes(Dsons.makeFullNumber(0, 1));
            } else {
                reader.readStartObject(Dsons.makeFullNumber(0, 1), TypeArgInfo.of(NestStruct.class));
                nestStruct = new NestStruct(
                        reader.readInt(Dsons.makeFullNumber(0, 0)),
                        reader.readLong(Dsons.makeFullNumber(0, 1)),
                        reader.readFloat(Dsons.makeFullNumber(0, 2)),
                        reader.readDouble(Dsons.makeFullNumber(0, 3)));
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