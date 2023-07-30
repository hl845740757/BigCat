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

package cn.wjybxx.common.codec.document.codecs;

import cn.wjybxx.common.codec.TypeArgInfo;
import cn.wjybxx.dson.DsonType;
import cn.wjybxx.common.codec.document.DocumentObjectReader;
import cn.wjybxx.common.codec.document.DocumentObjectWriter;
import cn.wjybxx.common.codec.document.DocumentPojoCodecImpl;
import cn.wjybxx.common.codec.document.DocumentPojoCodecScanIgnore;
import cn.wjybxx.dson.text.NumberStyle;
import it.unimi.dsi.fastutil.doubles.DoubleArrayList;

import javax.annotation.Nonnull;

/**
 * @author wjybxx
 * date 2023/4/4
 */
@DocumentPojoCodecScanIgnore
public class DoubleArrayCodec implements DocumentPojoCodecImpl<double[]> {

    @Nonnull
    @Override
    public Class<double[]> getEncoderClass() {
        return double[].class;
    }

    @Override
    public void writeObject(double[] instance, DocumentObjectWriter writer, TypeArgInfo<?> typeArgInfo) {
        for (double e : instance) {
            writer.writeDouble(null, e, NumberStyle.SIMPLE);
        }
    }

    @Override
    public double[] readObject(DocumentObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        DoubleArrayList result = new DoubleArrayList();
        while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
            result.add(reader.readDouble(null));
        }
        return result.toDoubleArray();
    }
}