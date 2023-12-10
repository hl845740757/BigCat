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

import cn.wjybxx.common.OptionalBool;
import cn.wjybxx.common.codec.binary.*;
import cn.wjybxx.common.codec.document.*;
import cn.wjybxx.dson.DsonLites;
import cn.wjybxx.dson.WireType;
import cn.wjybxx.dson.text.NumberStyle;
import cn.wjybxx.dson.text.ObjectStyle;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

import javax.annotation.Nonnull;
import java.util.List;

/**
 * 测试不写入默认值情况下的解码测试。
 *
 * @author wjybxx
 * date - 2023/9/17
 */
public class RandomReadTest {

    @Test
    void docTest() {
        ConvertOptions options = ConvertOptions.newBuilder()
                .setEncodeMapAsObject(OptionalBool.TRUE)
                .setAppendDef(OptionalBool.FALSE)
                .build();

        DocumentConverter converter = DefaultDocumentConverter.newInstance(
                List.of(new BeanDocCodec()),
                TypeMetaRegistries.fromMetas(TypeMeta.of(Bean.class, ObjectStyle.INDENT, "Bean")),
                options);

        Bean bean = new Bean();
        bean.iv1 = 1;
        bean.lv2 = 3;
        bean.fv2 = 5;
        bean.dv1 = 7;
        bean.bv1 = true;

        String dson = converter.writeAsDson(bean);
//        System.out.println(dson);

        Bean bean2 = converter.readFromDson(dson, TypeArgInfo.of(Bean.class));
        Assertions.assertEquals(bean, bean2);
    }

    @Test
    void binTest() {
        ConvertOptions options = ConvertOptions.newBuilder()
                .setEncodeMapAsObject(OptionalBool.TRUE)
                .setAppendDef(OptionalBool.FALSE)
                .build();

        BinaryConverter converter = DefaultBinaryConverter.newInstance(
                List.of(new BeanBinCodec()),
                TypeMetaRegistries.fromMetas(TypeMeta.of(Bean.class, new ClassId(1, 1))),
                options);

        Bean bean = new Bean();
        bean.iv1 = 1;
        bean.lv2 = 3;
        bean.fv1 = 5;
        bean.dv2 = 7;
        bean.bv1 = true;

        Assertions.assertEquals(bean, converter.cloneObject(bean, TypeArgInfo.OBJECT));
    }

    public static class Bean {
        public int iv1;
        public int iv2;
        public long lv1;
        public long lv2;
        public float fv1;
        public float fv2;
        public double dv1;
        public double dv2;
        public boolean bv1;
        public boolean bv2;

        @Override
        public boolean equals(Object o) {
            if (this == o) return true;
            if (o == null || getClass() != o.getClass()) return false;

            Bean bean = (Bean) o;

            if (iv1 != bean.iv1) return false;
            if (iv2 != bean.iv2) return false;
            if (lv1 != bean.lv1) return false;
            if (lv2 != bean.lv2) return false;
            if (Float.compare(bean.fv1, fv1) != 0) return false;
            if (Float.compare(bean.fv2, fv2) != 0) return false;
            if (Double.compare(bean.dv1, dv1) != 0) return false;
            if (Double.compare(bean.dv2, dv2) != 0) return false;
            if (bv1 != bean.bv1) return false;
            return bv2 == bean.bv2;
        }

        @Override
        public int hashCode() {
            int result;
            long temp;
            result = iv1;
            result = 31 * result + iv2;
            result = 31 * result + (int) (lv1 ^ (lv1 >>> 32));
            result = 31 * result + (int) (lv2 ^ (lv2 >>> 32));
            result = 31 * result + (fv1 != +0.0f ? Float.floatToIntBits(fv1) : 0);
            result = 31 * result + (fv2 != +0.0f ? Float.floatToIntBits(fv2) : 0);
            temp = Double.doubleToLongBits(dv1);
            result = 31 * result + (int) (temp ^ (temp >>> 32));
            temp = Double.doubleToLongBits(dv2);
            result = 31 * result + (int) (temp ^ (temp >>> 32));
            result = 31 * result + (bv1 ? 1 : 0);
            result = 31 * result + (bv2 ? 1 : 0);
            return result;
        }
    }


    private static class BeanDocCodec extends AbstractDocumentPojoCodecImpl<Bean> {

        @Override
        @Nonnull
        public Class<RandomReadTest.Bean> getEncoderClass() {
            return RandomReadTest.Bean.class;
        }

        @Override
        protected RandomReadTest.Bean newInstance(DocumentObjectReader reader,
                                                  TypeArgInfo<?> typeArgInfo) {
            return new RandomReadTest.Bean();
        }

        @Override
        public void readFields(DocumentObjectReader reader, RandomReadTest.Bean instance,
                               TypeArgInfo<?> typeArgInfo) {
            instance.iv1 = reader.readInt("iv1");
            instance.iv2 = reader.readInt("iv2");
            instance.lv1 = reader.readLong("lv1");
            instance.lv2 = reader.readLong("lv2");
            instance.fv1 = reader.readFloat("fv1");
            instance.fv2 = reader.readFloat("fv2");
            instance.dv1 = reader.readDouble("dv1");
            instance.dv2 = reader.readDouble("dv2");
            instance.bv1 = reader.readBoolean("bv1");
            instance.bv2 = reader.readBoolean("bv2");
        }

        @Override
        public void writeObject(DocumentObjectWriter writer, Bean instance,
                                TypeArgInfo<?> typeArgInfo, ObjectStyle style) {
            writer.writeInt("iv1", instance.iv1, WireType.VARINT, NumberStyle.SIMPLE);
            writer.writeInt("iv2", instance.iv2, WireType.VARINT, NumberStyle.SIMPLE);
            writer.writeLong("lv1", instance.lv1, WireType.VARINT, NumberStyle.SIMPLE);
            writer.writeLong("lv2", instance.lv2, WireType.VARINT, NumberStyle.SIMPLE);
            writer.writeFloat("fv1", instance.fv1, NumberStyle.SIMPLE);
            writer.writeFloat("fv2", instance.fv2, NumberStyle.SIMPLE);
            writer.writeDouble("dv1", instance.dv1, NumberStyle.SIMPLE);
            writer.writeDouble("dv2", instance.dv2, NumberStyle.SIMPLE);
            writer.writeBoolean("bv1", instance.bv1);
            writer.writeBoolean("bv2", instance.bv2);
        }

    }

    private static class BeanBinCodec extends AbstractBinaryPojoCodecImpl<Bean> {

        @Override
        @Nonnull
        public Class<RandomReadTest.Bean> getEncoderClass() {
            return RandomReadTest.Bean.class;
        }

        @Override
        protected RandomReadTest.Bean newInstance(BinaryObjectReader reader,
                                                  TypeArgInfo<?> typeArgInfo) {
            return new RandomReadTest.Bean();
        }

        @Override
        public void readFields(BinaryObjectReader reader, Bean instance,
                               TypeArgInfo<?> typeArgInfo) {
            instance.iv1 = reader.readInt(DsonLites.makeFullNumber(0, 0));
            instance.iv2 = reader.readInt(DsonLites.makeFullNumber(0, 1));
            instance.lv1 = reader.readLong(DsonLites.makeFullNumber(0, 2));
            instance.lv2 = reader.readLong(DsonLites.makeFullNumber(0, 3));
            instance.fv1 = reader.readFloat(DsonLites.makeFullNumber(0, 4));
            instance.fv2 = reader.readFloat(DsonLites.makeFullNumber(0, 5));
            instance.dv1 = reader.readDouble(DsonLites.makeFullNumber(0, 6));
            instance.dv2 = reader.readDouble(DsonLites.makeFullNumber(0, 7));
            instance.bv1 = reader.readBoolean(DsonLites.makeFullNumber(0, 8));
            instance.bv2 = reader.readBoolean(DsonLites.makeFullNumber(0, 9));
        }

        @Override
        public void writeObject(BinaryObjectWriter writer, Bean instance, TypeArgInfo<?> typeArgInfo) {
            writer.writeInt(DsonLites.makeFullNumber(0, 0), instance.iv1, WireType.VARINT, NumberStyle.SIMPLE);
            writer.writeInt(DsonLites.makeFullNumber(0, 1), instance.iv2, WireType.VARINT, NumberStyle.SIMPLE);
            writer.writeLong(DsonLites.makeFullNumber(0, 2), instance.lv1, WireType.VARINT, NumberStyle.SIMPLE);
            writer.writeLong(DsonLites.makeFullNumber(0, 3), instance.lv2, WireType.VARINT, NumberStyle.SIMPLE);
            writer.writeFloat(DsonLites.makeFullNumber(0, 4), instance.fv1, NumberStyle.SIMPLE);
            writer.writeFloat(DsonLites.makeFullNumber(0, 5), instance.fv2, NumberStyle.SIMPLE);
            writer.writeDouble(DsonLites.makeFullNumber(0, 6), instance.dv1, NumberStyle.SIMPLE);
            writer.writeDouble(DsonLites.makeFullNumber(0, 7), instance.dv2, NumberStyle.SIMPLE);
            writer.writeBoolean(DsonLites.makeFullNumber(0, 8), instance.bv1);
            writer.writeBoolean(DsonLites.makeFullNumber(0, 9), instance.bv2);
        }
    }
}