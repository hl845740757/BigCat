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

package cn.wjybxx.common.codec.codecs;

import cn.wjybxx.common.codec.PojoCodecImpl;
import cn.wjybxx.common.codec.TypeArgInfo;
import cn.wjybxx.common.codec.binary.BinaryObjectReader;
import cn.wjybxx.common.codec.binary.BinaryObjectWriter;
import cn.wjybxx.common.codec.binary.BinaryPojoCodecScanIgnore;
import cn.wjybxx.common.codec.document.DocumentObjectReader;
import cn.wjybxx.common.codec.document.DocumentObjectWriter;
import cn.wjybxx.common.codec.document.DocumentPojoCodecScanIgnore;
import cn.wjybxx.dson.DsonType;
import cn.wjybxx.dson.text.NumberStyle;
import cn.wjybxx.dson.text.ObjectStyle;
import it.unimi.dsi.fastutil.floats.FloatArrayList;

import javax.annotation.Nonnull;

/**
 * @author wjybxx
 * date 2023/4/4
 */
@BinaryPojoCodecScanIgnore
@DocumentPojoCodecScanIgnore
public class FloatArrayCodec implements PojoCodecImpl<float[]> {

    @Nonnull
    @Override
    public Class<float[]> getEncoderClass() {
        return float[].class;
    }

    @Override
    public void writeObject(BinaryObjectWriter writer, float[] instance, TypeArgInfo<?> typeArgInfo) {
        for (float e : instance) {
            writer.writeFloat(0, e);
        }
    }

    @Override
    public float[] readObject(BinaryObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        FloatArrayList result = new FloatArrayList();
        while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
            result.add(reader.readFloat(0));
        }
        return result.toFloatArray();
    }

    @Override
    public void writeObject(DocumentObjectWriter writer, float[] instance, TypeArgInfo<?> typeArgInfo, ObjectStyle style) {
        for (float e : instance) {
            writer.writeFloat(null, e, NumberStyle.SIMPLE);
        }
    }

    @Override
    public float[] readObject(DocumentObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        FloatArrayList result = new FloatArrayList();
        while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
            result.add(reader.readFloat(null));
        }
        return result.toFloatArray();
    }
}