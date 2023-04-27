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

package cn.wjybxx.common.dson.binary.codecs;

import cn.wjybxx.common.dson.TypeArgInfo;
import cn.wjybxx.common.dson.binary.BinaryObjectReader;
import cn.wjybxx.common.dson.binary.BinaryObjectWriter;
import cn.wjybxx.common.dson.binary.BinaryPojoCodecImpl;
import cn.wjybxx.common.dson.binary.BinaryPojoCodecScanIgnore;
import org.apache.commons.lang3.ArrayUtils;

import javax.annotation.Nonnull;
import java.util.ArrayList;

/**
 * @author wjybxx
 * date 2023/4/4
 */
@BinaryPojoCodecScanIgnore
public class StringArrayCodec implements BinaryPojoCodecImpl<String[]> {

    @Nonnull
    @Override
    public Class<String[]> getEncoderClass() {
        return String[].class;
    }

    @Override
    public void writeObject(String[] instance, BinaryObjectWriter writer, TypeArgInfo<?> typeArgInfo) {
        for (String e : instance) {
            writer.writeString(0, e);
        }
    }

    @Override
    public String[] readObject(BinaryObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        ArrayList<String> result = new ArrayList<>();
        while (!reader.isAtEndOfObject()) {
            result.add(reader.readString(0));
        }
        return result.toArray(ArrayUtils.EMPTY_STRING_ARRAY);
    }
}