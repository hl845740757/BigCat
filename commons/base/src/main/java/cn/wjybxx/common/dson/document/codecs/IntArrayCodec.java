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

package cn.wjybxx.common.dson.document.codecs;

import cn.wjybxx.common.dson.TypeArgInfo;
import cn.wjybxx.common.dson.document.DocumentObjectReader;
import cn.wjybxx.common.dson.document.DocumentObjectWriter;
import cn.wjybxx.common.dson.document.DocumentPojoCodecImpl;
import cn.wjybxx.common.dson.document.DocumentPojoCodecScanIgnore;
import it.unimi.dsi.fastutil.ints.IntArrayList;

import javax.annotation.Nonnull;

/**
 * @author wjybxx
 * date 2023/4/4
 */
@DocumentPojoCodecScanIgnore
public class IntArrayCodec implements DocumentPojoCodecImpl<int[]> {

    @Nonnull
    @Override
    public String getTypeName() {
        return "int[]";
    }

    @Nonnull
    @Override
    public Class<int[]> getEncoderClass() {
        return int[].class;
    }

    @Override
    public void writeObject(int[] instance, DocumentObjectWriter writer, TypeArgInfo<?> typeArgInfo) {
        for (int e : instance) {
            writer.writeInt(null, e);
        }
    }

    @Override
    public int[] readObject(DocumentObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        IntArrayList result = new IntArrayList();
        while (!reader.isAtEndOfObject()) {
            result.add(reader.readInt(null));
        }
        return result.toIntArray();
    }
}