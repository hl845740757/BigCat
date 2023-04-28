package cn.wjybxx.common.dson;

import cn.wjybxx.common.dson.binary.BinaryObjectReader;
import cn.wjybxx.common.dson.binary.BinaryObjectWriter;
import cn.wjybxx.common.dson.binary.BinaryPojoCodecImpl;
import cn.wjybxx.common.dson.document.DocumentObjectReader;
import cn.wjybxx.common.dson.document.DocumentObjectWriter;
import cn.wjybxx.common.dson.document.DocumentPojoCodecImpl;

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
    @AutoFields
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
    @AutoFields
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

        @Nonnull
        @Override
        public String getTypeName() {
            return "MyStruct";
        }

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

        @Override
        public void writeObject(MyStruct instance, DocumentObjectWriter writer, TypeArgInfo<?> typeArgInfo) {
            NestStruct nestStruct = instance.nestStruct;
            writer.writeStartObject(CodecStructs_MyStructFields.nestStruct, nestStruct, TypeArgInfo.of(NestStruct.class));
            {
                writer.writeInt(CodecStructs_NestStructFields.intVal, nestStruct.intVal);
                writer.writeLong(CodecStructs_NestStructFields.longVal, nestStruct.longVal);
                writer.writeFloat(CodecStructs_NestStructFields.floatVal, nestStruct.floatVal);
                writer.writeDouble(CodecStructs_NestStructFields.doubleVal, nestStruct.doubleVal);
            }
            writer.writeEndObject();

            writer.writeInt(CodecStructs_MyStructFields.intVal, instance.intVal);
            writer.writeLong(CodecStructs_MyStructFields.longVal, instance.longVal);
            writer.writeFloat(CodecStructs_MyStructFields.floatVal, instance.floatVal);
            writer.writeDouble(CodecStructs_MyStructFields.doubleVal, instance.doubleVal);
            writer.writeBoolean(CodecStructs_MyStructFields.boolVal, instance.boolVal);
            writer.writeString(CodecStructs_MyStructFields.strVal, instance.strVal);
            writer.writeBytes(CodecStructs_MyStructFields.bytes, instance.bytes);
            writer.writeObject(CodecStructs_MyStructFields.map, instance.map, TypeArgInfo.STRING_LINKEDHASHMAP);
            writer.writeObject(CodecStructs_MyStructFields.list, instance.list, TypeArgInfo.ARRAYLIST);
        }

        @SuppressWarnings("unchecked")
        @Override
        public MyStruct readObject(DocumentObjectReader reader, TypeArgInfo<?> typeArgInfo) {
            reader.readStartObject(CodecStructs_MyStructFields.nestStruct, TypeArgInfo.of(NestStruct.class));
            NestStruct nestStruct = new NestStruct(
                    reader.readInt(CodecStructs_NestStructFields.intVal),
                    reader.readLong(CodecStructs_NestStructFields.longVal),
                    reader.readFloat(CodecStructs_NestStructFields.floatVal),
                    reader.readDouble(CodecStructs_NestStructFields.doubleVal));
            reader.readEndObject();

            return new MyStruct(
                    reader.readInt(CodecStructs_MyStructFields.intVal),
                    reader.readLong(CodecStructs_MyStructFields.longVal),
                    reader.readFloat(CodecStructs_MyStructFields.floatVal),
                    reader.readDouble(CodecStructs_MyStructFields.doubleVal),
                    reader.readBoolean(CodecStructs_MyStructFields.boolVal),
                    reader.readString(CodecStructs_MyStructFields.strVal),
                    reader.readBytes(CodecStructs_MyStructFields.bytes),
                    reader.readObject(CodecStructs_MyStructFields.map, TypeArgInfo.STRING_LINKEDHASHMAP),
                    reader.readObject(CodecStructs_MyStructFields.list, TypeArgInfo.ARRAYLIST),
                    nestStruct);
        }
    }
}