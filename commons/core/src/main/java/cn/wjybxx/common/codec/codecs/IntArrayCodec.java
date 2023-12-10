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
import cn.wjybxx.dson.text.ObjectStyle;
import it.unimi.dsi.fastutil.ints.IntArrayList;

import javax.annotation.Nonnull;

/**
 * @author wjybxx
 * date 2023/4/4
 */
@BinaryPojoCodecScanIgnore
@DocumentPojoCodecScanIgnore
public class IntArrayCodec implements PojoCodecImpl<int[]> {

    @Nonnull
    @Override
    public Class<int[]> getEncoderClass() {
        return int[].class;
    }

    @Override
    public void writeObject(BinaryObjectWriter writer, int[] instance, TypeArgInfo<?> typeArgInfo) {
        for (int e : instance) {
            writer.writeInt(0, e);
        }
    }

    @Override
    public int[] readObject(BinaryObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        IntArrayList result = new IntArrayList();
        while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
            result.add(reader.readInt(0));
        }
        return result.toIntArray();
    }

    @Override
    public void writeObject(DocumentObjectWriter writer, int[] instance, TypeArgInfo<?> typeArgInfo, ObjectStyle style) {
        for (int e : instance) {
            writer.writeInt(null, e);
        }
    }

    @Override
    public int[] readObject(DocumentObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        IntArrayList result = new IntArrayList();
        while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
            result.add(reader.readInt(null));
        }
        return result.toIntArray();
    }
}