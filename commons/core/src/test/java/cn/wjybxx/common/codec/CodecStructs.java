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
import cn.wjybxx.common.codec.document.DocumentObjectReader;
import cn.wjybxx.common.codec.document.DocumentObjectWriter;
import cn.wjybxx.common.codec.document.DocumentPojoCodecImpl;
import cn.wjybxx.dson.DsonLites;
import cn.wjybxx.dson.text.NumberStyle;
import cn.wjybxx.dson.text.ObjectStyle;
import cn.wjybxx.dson.text.StringStyle;

import javax.annotation.Nonnull;
import java.util.Arrays;
import java.util.List;
import java.util.Map;
import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/4/28
 */
class CodecStructs {

    /** 该类不添加到类型仓库，也不提供codec -- 直接外部读写 */
    static final class NestStruct {

        public final int intVal;
        public final long longVal;
        public final float floatVal;
        public final double doubleVal;

        NestStruct(int intVal, long longVal, float floatVal, double doubleVal) {
            this.intVal = intVal;
            this.longVal = longVal;
            this.floatVal = floatVal;
            this.doubleVal = doubleVal;
        }

        @Override
        public boolean equals(Object obj) {
            if (obj == this) return true;
            if (obj == null || obj.getClass() != this.getClass()) return false;
            var that = (NestStruct) obj;
            return this.intVal == that.intVal &&
                    this.longVal == that.longVal &&
                    Float.floatToIntBits(this.floatVal) == Float.floatToIntBits(that.floatVal) &&
                    Double.doubleToLongBits(this.doubleVal) == Double.doubleToLongBits(that.doubleVal);
        }

        @Override
        public int hashCode() {
            return Objects.hash(intVal, longVal, floatVal, doubleVal);
        }

        @Override
        public String toString() {
            return "NestStruct[" +
                    "intVal=" + intVal + ", " +
                    "longVal=" + longVal + ", " +
                    "floatVal=" + floatVal + ", " +
                    "doubleVal=" + doubleVal + ']';
        }

    }

    /**
     * Java出的新Record特性有点问题啊。。。
     * 比较字节数组的时候用的不是{@link Arrays#equals(byte[], byte[])}，而是{@link Objects#equals(Object, Object)}...
     * 只能先用record定义，定义完一键转Class，再修改hashCode和equals方法
     */
    static final class MyStruct {
        public final int intVal;
        public final long longVal;
        public final float floatVal;
        public final double doubleVal;
        public final boolean boolVal;
        public final String strVal;
        public final byte[] bytes;
        public final Map<String, Object> map;
        public final List<String> list;
        public final NestStruct nestStruct;

        public MyStruct(int intVal, long longVal, float floatVal, double doubleVal, boolean boolVal, String strVal,
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

    static class MyStructCodec implements BinaryPojoCodecImpl<MyStruct>, DocumentPojoCodecImpl<MyStruct> {

        @Override
        public boolean isWriteAsArray() {
            return false;
        }

        @Override
        public boolean autoStartEnd() {
            return true;
        }

        @Nonnull
        @Override
        public Class<MyStruct> getEncoderClass() {
            return MyStruct.class;
        }

        @Override
        public void writeObject(BinaryObjectWriter writer, MyStruct instance, TypeArgInfo<?> typeArgInfo) {
            NestStruct nestStruct = instance.nestStruct;
            writer.writeStartObject(DsonLites.makeFullNumber(0, 0), nestStruct, TypeArgInfo.of(NestStruct.class));
            {
                writer.writeInt(DsonLites.makeFullNumber(0, 0), nestStruct.intVal);
                writer.writeLong(DsonLites.makeFullNumber(0, 1), nestStruct.longVal);
                writer.writeFloat(DsonLites.makeFullNumber(0, 2), nestStruct.floatVal);
                writer.writeDouble(DsonLites.makeFullNumber(0, 3), nestStruct.doubleVal);
            }
            writer.writeEndObject();

            writer.writeInt(DsonLites.makeFullNumber(0, 1), instance.intVal);
            writer.writeLong(DsonLites.makeFullNumber(0, 2), instance.longVal);
            writer.writeFloat(DsonLites.makeFullNumber(0, 3), instance.floatVal);
            writer.writeDouble(DsonLites.makeFullNumber(0, 4), instance.doubleVal);
            writer.writeBoolean(DsonLites.makeFullNumber(0, 5), instance.boolVal);
            writer.writeString(DsonLites.makeFullNumber(0, 6), instance.strVal);
            writer.writeBytes(DsonLites.makeFullNumber(0, 7), instance.bytes);
            writer.writeObject(DsonLites.makeFullNumber(0, 8), instance.map, TypeArgInfo.STRING_LINKED_HASHMAP);
            writer.writeObject(DsonLites.makeFullNumber(0, 9), instance.list, TypeArgInfo.ARRAYLIST);
        }

        @SuppressWarnings("unchecked")
        @Override
        public MyStruct readObject(BinaryObjectReader reader, TypeArgInfo<?> typeArgInfo) {
            reader.readStartObject(DsonLites.makeFullNumber(0, 0), TypeArgInfo.of(NestStruct.class));
            NestStruct nestStruct = new NestStruct(
                    reader.readInt(DsonLites.makeFullNumber(0, 0)),
                    reader.readLong(DsonLites.makeFullNumber(0, 1)),
                    reader.readFloat(DsonLites.makeFullNumber(0, 2)),
                    reader.readDouble(DsonLites.makeFullNumber(0, 3)));
            reader.readEndObject();

            return new MyStruct(
                    reader.readInt(DsonLites.makeFullNumber(0, 1)),
                    reader.readLong(DsonLites.makeFullNumber(0, 2)),
                    reader.readFloat(DsonLites.makeFullNumber(0, 3)),
                    reader.readDouble(DsonLites.makeFullNumber(0, 4)),
                    reader.readBoolean(DsonLites.makeFullNumber(0, 5)),
                    reader.readString(DsonLites.makeFullNumber(0, 6)),
                    reader.readBytes(DsonLites.makeFullNumber(0, 7)),
                    reader.readObject(DsonLites.makeFullNumber(0, 8), TypeArgInfo.STRING_LINKED_HASHMAP),
                    reader.readObject(DsonLites.makeFullNumber(0, 9), TypeArgInfo.ARRAYLIST),
                    nestStruct);
        }

        @Override
        public void writeObject(DocumentObjectWriter writer, MyStruct instance, TypeArgInfo<?> typeArgInfo, ObjectStyle style) {
            NestStruct nestStruct = instance.nestStruct;
            writer.writeStartObject("nestStruct", nestStruct, TypeArgInfo.of(NestStruct.class));
            {
                writer.writeInt("intVal", nestStruct.intVal);
                writer.writeLong("longVal", nestStruct.longVal);
                writer.writeFloat("floatVal", nestStruct.floatVal, NumberStyle.SIMPLE);
                writer.writeDouble("doubleVal", nestStruct.doubleVal, NumberStyle.SIMPLE);
            }
            writer.writeEndObject();

            writer.writeInt("intVal", instance.intVal);
            writer.writeLong("longVal", instance.longVal);
            writer.writeFloat("floatVal", instance.floatVal, NumberStyle.SIMPLE);
            writer.writeDouble("doubleVal", instance.doubleVal, NumberStyle.SIMPLE);
            writer.writeBoolean("boolVal", instance.boolVal);
            writer.writeString("strVal", instance.strVal, StringStyle.AUTO);
            writer.writeBytes("bytes", 0, instance.bytes);
            writer.writeObject("map", instance.map, TypeArgInfo.STRING_LINKED_HASHMAP, null);
            writer.writeObject("list", instance.list, TypeArgInfo.ARRAYLIST, null);
        }

        @SuppressWarnings("unchecked")
        @Override
        public MyStruct readObject(DocumentObjectReader reader, TypeArgInfo<?> typeArgInfo) {
            reader.readStartObject("nestStruct", TypeArgInfo.of(NestStruct.class));
            NestStruct nestStruct = new NestStruct(
                    reader.readInt("intVal"),
                    reader.readLong("longVal"),
                    reader.readFloat("floatVal"),
                    reader.readDouble("doubleVal"));
            reader.readEndObject();

            return new MyStruct(
                    reader.readInt("intVal"),
                    reader.readLong("longVal"),
                    reader.readFloat("floatVal"),
                    reader.readDouble("doubleVal"),
                    reader.readBoolean("boolVal"),
                    reader.readString("strVal"),
                    reader.readBytes("bytes"),
                    reader.readObject("map", TypeArgInfo.STRING_LINKED_HASHMAP),
                    reader.readObject("list", TypeArgInfo.ARRAYLIST),
                    nestStruct);
        }
    }
}