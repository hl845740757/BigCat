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
import cn.wjybxx.common.dson.codec.ConvertOptions;
import org.apache.commons.lang3.RandomStringUtils;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

import javax.annotation.Nonnull;
import java.util.*;

/**
 * @author wjybxx
 * date 2023/4/3
 */
public class CodecTest {

    private MyStruct myStruct;
    private BinClassId binClassId;

    @BeforeEach
    void setUp() {
        Random random = new Random();
        NestStruct nestStruct = new NestStruct(random.nextInt(), random.nextLong(),
                random.nextFloat() * 100, random.nextDouble() * 100);

        MyStruct myStruct = new MyStruct(random.nextInt(), random.nextLong(),
                random.nextFloat() * 100, random.nextDouble() * 100,
                random.nextBoolean(),
                RandomStringUtils.random(10),
                new byte[5],
                new HashMap<>(),
                new ArrayList<>(),
                nestStruct);

        random.nextBytes(myStruct.bytes);

        myStruct.list.add(RandomStringUtils.random(5));
        myStruct.list.add(RandomStringUtils.random(7));

        myStruct.map.put(String.valueOf(myStruct.intVal), random.nextFloat() * 100);
        myStruct.map.put(String.valueOf(myStruct.longVal), random.nextDouble() * 100);

        this.myStruct = myStruct;
        binClassId = new BinClassId(1, 1);
    }

    /** 基础读写测试 */
    @Test
    void testStructCodec() {
        DefaultBinaryConverter converter = DefaultBinaryConverter.newInstance(Set.of(),
                List.of(new MyStructCodec()),
                Map.of(MyStruct.class, binClassId),
                ConvertOptions.DEFAULT);

        MyStruct clonedObject = converter.cloneObject(myStruct, TypeArgInfo.of(MyStruct.class));
        Assertions.assertEquals(myStruct, clonedObject);
    }

    private static class MyStructCodec implements BinaryPojoCodecImpl<MyStruct> {

        @Nonnull
        @Override
        public Class<MyStruct> getEncoderClass() {
            return MyStruct.class;
        }

        @Override
        public void writeObject(MyStruct instance, BinaryObjectWriter writer, TypeArgInfo<?> typeArgInfo) {
            NestStruct nestStruct = instance.nestStruct;
            writer.writeStartObject(Dsons.makeFullNumber(0, 0), nestStruct, TypeArgInfo.of(NestStruct.class));
            {
                writer.writeInt(Dsons.makeFullNumber(0, 0), nestStruct.intVal);
                writer.writeLong(Dsons.makeFullNumber(0, 1), nestStruct.longVal);
                writer.writeFloat(Dsons.makeFullNumber(0, 2), nestStruct.floatVal);
                writer.writeDouble(Dsons.makeFullNumber(0, 3), nestStruct.doubleVal);
            }
            writer.writeEndObject();

            writer.writeInt(Dsons.makeFullNumber(0, 1), instance.intVal);
            writer.writeLong(Dsons.makeFullNumber(0, 2), instance.longVal);
            writer.writeFloat(Dsons.makeFullNumber(0, 3), instance.floatVal);
            writer.writeDouble(Dsons.makeFullNumber(0, 4), instance.doubleVal);
            writer.writeBoolean(Dsons.makeFullNumber(0, 5), instance.boolVal);
            writer.writeString(Dsons.makeFullNumber(0, 6), instance.strVal);
            writer.writeBytes(Dsons.makeFullNumber(0, 7), instance.bytes);
            writer.writeObject(Dsons.makeFullNumber(0, 8), instance.map, TypeArgInfo.STRING_LINKEDHASHMAP);
            writer.writeObject(Dsons.makeFullNumber(0, 9), instance.list, TypeArgInfo.ARRAYLIST);
        }

        @SuppressWarnings("unchecked")
        @Override
        public MyStruct readObject(BinaryObjectReader reader, TypeArgInfo<?> typeArgInfo) {
            reader.readStartObject(Dsons.makeFullNumber(0, 0), TypeArgInfo.of(NestStruct.class));
            NestStruct nestStruct = new NestStruct(
                    reader.readInt(Dsons.makeFullNumber(0, 0)),
                    reader.readLong(Dsons.makeFullNumber(0, 1)),
                    reader.readFloat(Dsons.makeFullNumber(0, 2)),
                    reader.readDouble(Dsons.makeFullNumber(0, 3)));
            reader.readEndObject();

            return new MyStruct(
                    reader.readInt(Dsons.makeFullNumber(0, 1)),
                    reader.readLong(Dsons.makeFullNumber(0, 2)),
                    reader.readFloat(Dsons.makeFullNumber(0, 3)),
                    reader.readDouble(Dsons.makeFullNumber(0, 4)),
                    reader.readBoolean(Dsons.makeFullNumber(0, 5)),
                    reader.readString(Dsons.makeFullNumber(0, 6)),
                    reader.readBytes(Dsons.makeFullNumber(0, 7)),
                    reader.readObject(Dsons.makeFullNumber(0, 8), TypeArgInfo.STRING_LINKEDHASHMAP),
                    reader.readObject(Dsons.makeFullNumber(0, 9), TypeArgInfo.ARRAYLIST),
                    nestStruct);
        }
    }

    /** 该类不添加到类型仓库，也不提供codec -- 直接外部读写 */
    private record NestStruct(int intVal, long longVal, float floatVal, double doubleVal) {

    }

    /**
     * Java出的新Record特性有点问题啊。。。
     * 比较字节数组的时候用的不是{@link Arrays#equals(byte[], byte[])}，而是{@link Objects#equals(Object, Object)}...
     * 只能先用record定义，定义完一键转Class，再修改hashCode和equals方法
     */
    private static final class MyStruct {
        private final int intVal;
        private final long longVal;
        private final float floatVal;
        private final double doubleVal;
        private final boolean boolVal;
        private final String strVal;
        private final byte[] bytes;
        private final Map<String, Object> map;
        private final List<String> list;
        private final NestStruct nestStruct;

        private MyStruct(int intVal, long longVal, float floatVal, double doubleVal, boolean boolVal, String strVal,
                         byte[] bytes, Map<String, Object> map, List<String> list, NestStruct nestStruct) {
            this.intVal = intVal;
            this.longVal = longVal;
            this.floatVal = floatVal;
            this.doubleVal = doubleVal;
            this.boolVal = boolVal;
            this.strVal = strVal;
            this.bytes = bytes;
            this.map = map;
            this.list = list;
            this.nestStruct = nestStruct;
        }

        public int intVal() {
            return intVal;
        }

        public long longVal() {
            return longVal;
        }

        public float floatVal() {
            return floatVal;
        }

        public double doubleVal() {
            return doubleVal;
        }

        public boolean boolVal() {
            return boolVal;
        }

        public String strVal() {
            return strVal;
        }

        public byte[] bytes() {
            return bytes;
        }

        public Map<String, Object> map() {
            return map;
        }

        public List<String> list() {
            return list;
        }

        public NestStruct nestStruct() {
            return nestStruct;
        }

        @Override
        public boolean equals(Object obj) {
            if (obj == this) return true;
            if (obj == null || obj.getClass() != this.getClass()) return false;
            var that = (MyStruct) obj;
            return this.intVal == that.intVal &&
                    this.longVal == that.longVal &&
                    Float.floatToIntBits(this.floatVal) == Float.floatToIntBits(that.floatVal) &&
                    Double.doubleToLongBits(this.doubleVal) == Double.doubleToLongBits(that.doubleVal) &&
                    this.boolVal == that.boolVal &&
                    Objects.equals(this.strVal, that.strVal) &&
                    Arrays.equals(this.bytes, that.bytes) &&
                    Objects.equals(this.map, that.map) &&
                    Objects.equals(this.list, that.list) &&
                    Objects.equals(this.nestStruct, that.nestStruct);
        }

        @Override
        public int hashCode() {
            return Objects.hash(intVal, longVal, floatVal, doubleVal, boolVal, strVal, Arrays.hashCode(bytes), map, list, nestStruct);
        }

        @Override
        public String toString() {
            return "MyStruct[" +
                    "intVal=" + intVal + ", " +
                    "longVal=" + longVal + ", " +
                    "floatVal=" + floatVal + ", " +
                    "doubleVal=" + doubleVal + ", " +
                    "boolVal=" + boolVal + ", " +
                    "strVal=" + strVal + ", " +
                    "bytes=" + Arrays.toString(bytes) + ", " +
                    "map=" + map + ", " +
                    "list=" + list + ", " +
                    "nestStruct=" + nestStruct + ']';
        }


    }
}