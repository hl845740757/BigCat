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

package cn.wjybxx.bigcat.common.codec.document.base;

import cn.wjybxx.bigcat.common.codec.TypeArgInfo;
import cn.wjybxx.bigcat.common.codec.document.DocumentPojoCodecImpl;
import cn.wjybxx.bigcat.common.codec.document.DocumentPojoCodecScanIgnore;
import cn.wjybxx.bigcat.common.codec.document.DocumentReader;
import cn.wjybxx.bigcat.common.codec.document.DocumentWriter;
import it.unimi.dsi.fastutil.longs.LongArrayList;

import javax.annotation.Nonnull;

/**
 * @author wjybxx
 * date 2023/4/4
 */
@DocumentPojoCodecScanIgnore
public class LongArrayCodec implements DocumentPojoCodecImpl<long[]> {

    @Nonnull
    @Override
    public String getTypeName() {
        return "long[]";
    }

    @Nonnull
    @Override
    public Class<long[]> getEncoderClass() {
        return long[].class;
    }

    @Override
    public void writeObject(long[] instance, DocumentWriter writer, TypeArgInfo<?> typeArgInfo) {
        for (long e : instance) {
            writer.writeLong(writer.nextElementName(), e);
        }
    }

    @Override
    public long[] readObject(DocumentReader reader, TypeArgInfo<?> typeArgInfo) {
        LongArrayList result = new LongArrayList();
        while (!reader.isAtEndOfObject()) {
            result.add(reader.readLong(reader.nextElementName()));
        }
        return result.toLongArray();
    }
}