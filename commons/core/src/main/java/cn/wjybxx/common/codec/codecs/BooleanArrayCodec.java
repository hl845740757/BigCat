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

import cn.wjybxx.common.codec.ConverterUtils;
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

import javax.annotation.Nonnull;
import java.util.ArrayList;

/**
 * @author wjybxx
 * date 2023/4/4
 */
@BinaryPojoCodecScanIgnore
@DocumentPojoCodecScanIgnore
public class BooleanArrayCodec implements PojoCodecImpl<boolean[]> {

    @Nonnull
    @Override
    public Class<boolean[]> getEncoderClass() {
        return boolean[].class;
    }

    @Override
    public void writeObject(BinaryObjectWriter writer, boolean[] instance, TypeArgInfo<?> typeArgInfo) {
        for (boolean e : instance) {
            writer.writeBoolean(0, e);
        }
    }

    @Override
    public boolean[] readObject(BinaryObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        ArrayList<Boolean> result = new ArrayList<>();
        while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
            result.add(reader.readBoolean(0));
        }
        return ConverterUtils.convertList2Array(result, boolean[].class);
    }

    @Override
    public void writeObject(DocumentObjectWriter writer, boolean[] instance, TypeArgInfo<?> typeArgInfo, ObjectStyle style) {
        for (boolean e : instance) {
            writer.writeBoolean(null, e);
        }
    }

    @Override
    public boolean[] readObject(DocumentObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        ArrayList<Boolean> result = new ArrayList<>();
        while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
            result.add(reader.readBoolean(null));
        }
        return ConverterUtils.convertList2Array(result, boolean[].class);
    }
}