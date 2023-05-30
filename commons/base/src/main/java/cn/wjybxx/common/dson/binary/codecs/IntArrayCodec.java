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

package cn.wjybxx.common.dson.binary.codecs;

import cn.wjybxx.common.dson.TypeArgInfo;
import cn.wjybxx.common.dson.binary.BinaryObjectReader;
import cn.wjybxx.common.dson.binary.BinaryObjectWriter;
import cn.wjybxx.common.dson.binary.BinaryPojoCodecImpl;
import cn.wjybxx.common.dson.binary.BinaryPojoCodecScanIgnore;
import it.unimi.dsi.fastutil.ints.IntArrayList;

import javax.annotation.Nonnull;

/**
 * @author wjybxx
 * date 2023/4/4
 */
@BinaryPojoCodecScanIgnore
public class IntArrayCodec implements BinaryPojoCodecImpl<int[]> {

    @Nonnull
    @Override
    public Class<int[]> getEncoderClass() {
        return int[].class;
    }

    @Override
    public void writeObject(int[] instance, BinaryObjectWriter writer, TypeArgInfo<?> typeArgInfo) {
        for (int e : instance) {
            writer.writeInt(0, e);
        }
    }

    @Override
    public int[] readObject(BinaryObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        IntArrayList result = new IntArrayList();
        while (!reader.isAtEndOfObject()) {
            result.add(reader.readInt(0));
        }
        return result.toIntArray();
    }
}