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
import cn.wjybxx.common.dson.binary.BinaryPojoCodecScanIgnore;
import cn.wjybxx.common.dson.codec.ConverterUtils;
import cn.wjybxx.common.dson.document.DocumentObjectReader;
import cn.wjybxx.common.dson.document.DocumentObjectWriter;
import cn.wjybxx.common.dson.document.DocumentPojoCodecImpl;

import javax.annotation.Nonnull;
import java.util.ArrayList;

/**
 * @author wjybxx
 * date 2023/4/4
 */
@BinaryPojoCodecScanIgnore
public class BooleanArrayCodec implements DocumentPojoCodecImpl<boolean[]> {

    @Nonnull
    @Override
    public String getTypeName() {
        return "boolean[]";
    }

    @Nonnull
    @Override
    public Class<boolean[]> getEncoderClass() {
        return boolean[].class;
    }

    @Override
    public void writeObject(boolean[] instance, DocumentObjectWriter writer, TypeArgInfo<?> typeArgInfo) {
        for (boolean e : instance) {
            writer.writeBoolean(null, e);
        }
    }

    @Override
    public boolean[] readObject(DocumentObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        ArrayList<Boolean> result = new ArrayList<>();
        while (!reader.isAtEndOfObject()) {
            result.add(reader.readBoolean(null));
        }
        return ConverterUtils.convertList2Array(result, boolean[].class);
    }
}