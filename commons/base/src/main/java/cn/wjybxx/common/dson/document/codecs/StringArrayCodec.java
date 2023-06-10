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

import cn.wjybxx.common.dson.DsonType;
import cn.wjybxx.common.dson.TypeArgInfo;
import cn.wjybxx.common.dson.document.DocumentObjectReader;
import cn.wjybxx.common.dson.document.DocumentObjectWriter;
import cn.wjybxx.common.dson.document.DocumentPojoCodecImpl;
import cn.wjybxx.common.dson.document.DocumentPojoCodecScanIgnore;
import org.apache.commons.lang3.ArrayUtils;

import javax.annotation.Nonnull;
import java.util.ArrayList;

/**
 * @author wjybxx
 * date 2023/4/4
 */
@DocumentPojoCodecScanIgnore
public class StringArrayCodec implements DocumentPojoCodecImpl<String[]> {

    @Nonnull
    @Override
    public String getTypeName() {
        return "String[]";
    }

    @Nonnull
    @Override
    public Class<String[]> getEncoderClass() {
        return String[].class;
    }

    @Override
    public void writeObject(String[] instance, DocumentObjectWriter writer, TypeArgInfo<?> typeArgInfo) {
        for (String e : instance) {
            writer.writeString(null, e);
        }
    }

    @Override
    public String[] readObject(DocumentObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        ArrayList<String> result = new ArrayList<>();
        while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
            result.add(reader.readString(null));
        }
        return result.toArray(ArrayUtils.EMPTY_STRING_ARRAY);
    }
}