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

package cn.wjybxx.bigcat.pb.codec;

import cn.wjybxx.bigcat.common.codec.TypeArgInfo;
import cn.wjybxx.bigcat.common.codec.binary.BinaryPojoCodecImpl;
import cn.wjybxx.bigcat.common.codec.binary.BinaryReader;
import cn.wjybxx.bigcat.common.codec.binary.BinaryWriter;
import cn.wjybxx.bigcat.common.codec.binary.TypeId;
import cn.wjybxx.bigcat.common.pb.codec.ProtobufConverter;
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
public class PbCodeTest {

    private MyStruct myStruct;
    private TypeId typeId;

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
        typeId = new TypeId(1, 1);
    }

    /** 基础读写测试 */
    @Test
    void testStructCodec() {
        ProtobufConverter converter = ProtobufConverter.newInstance(Set.of(),
                List.of(new MyStructCodec()),
                Map.of(MyStruct.class, typeId));

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
        public void writeObject(MyStruct instance, BinaryWriter writer, TypeArgInfo<?> typeArgInfo) {
            NestStruct nestStruct = instance.nestStruct;
            writer.writeStartObject(nestStruct, TypeArgInfo.of(NestStruct.class));
            {
                writer.writeInt(nestStruct.intVal);
                writer.writeLong(nestStruct.longVal);
                writer.writeFloat(nestStruct.floatVal);
                writer.writeDouble(nestStruct.doubleVal);
            }
            writer.writeEndObject();

            writer.writeInt(instance.intVal);
            writer.writeLong(instance.longVal);
            writer.writeFloat(instance.floatVal);
            writer.writeDouble(instance.doubleVal);
            writer.writeBoolean(instance.boolVal);
            writer.writeString(instance.strVal);
            writer.writeBytes(instance.bytes);
            writer.writeMap(instance.map, TypeArgInfo.STRING_LINKEDHASHMAP);
            writer.writeCollection(instance.list, TypeArgInfo.ARRAYLIST);
        }

        @Override
        public MyStruct readObject(BinaryReader reader, TypeArgInfo<?> typeArgInfo) {
            Class<?> nestTypeArg = reader.readStartObject(TypeArgInfo.of(NestStruct.class));
            NestStruct nestStruct = new NestStruct(
                    reader.readInt(),
                    reader.readLong(),
                    reader.readFloat(),
                    reader.readDouble());
            reader.readEndObject();

            return new MyStruct(
                    reader.readInt(),
                    reader.readLong(),
                    reader.readFloat(),
                    reader.readDouble(),
                    reader.readBoolean(),
                    reader.readString(),
                    reader.readBytes(),
                    reader.readMap(TypeArgInfo.STRING_LINKEDHASHMAP),
                    reader.readCollection(TypeArgInfo.ARRAYLIST), nestStruct);
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