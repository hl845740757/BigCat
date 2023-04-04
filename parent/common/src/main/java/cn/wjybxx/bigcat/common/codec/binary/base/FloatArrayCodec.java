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

package cn.wjybxx.bigcat.common.codec.binary.base;

import cn.wjybxx.bigcat.common.codec.TypeArgInfo;
import cn.wjybxx.bigcat.common.codec.binary.BinaryPojoCodecImpl;
import cn.wjybxx.bigcat.common.codec.binary.BinaryPojoCodecScanIgnore;
import cn.wjybxx.bigcat.common.codec.binary.BinaryReader;
import cn.wjybxx.bigcat.common.codec.binary.BinaryWriter;
import it.unimi.dsi.fastutil.floats.FloatArrayList;

import javax.annotation.Nonnull;

/**
 * @author wjybxx
 * date 2023/4/4
 */
@BinaryPojoCodecScanIgnore
public class FloatArrayCodec implements BinaryPojoCodecImpl<float[]> {

    @Nonnull
    @Override
    public Class<float[]> getEncoderClass() {
        return float[].class;
    }

    @Override
    public void writeObject(float[] instance, BinaryWriter writer, TypeArgInfo<?> typeArgInfo) {
        for (float e : instance) {
            writer.writeFloat(e);
        }
    }

    @Override
    public float[] readObject(BinaryReader reader, TypeArgInfo<?> typeArgInfo) {
        FloatArrayList result = new FloatArrayList();
        while (!reader.isAtEndOfObject()) {
            result.add(reader.readFloat());
        }
        return result.toFloatArray();
    }
}