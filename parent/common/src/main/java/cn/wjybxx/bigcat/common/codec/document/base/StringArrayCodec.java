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
    public void writeObject(String[] instance, DocumentWriter writer, TypeArgInfo<?> typeArgInfo) {
        for (String e : instance) {
            writer.writeString(writer.nextElementName(), e);
        }
    }

    @Override
    public String[] readObject(DocumentReader reader, TypeArgInfo<?> typeArgInfo) {
        ArrayList<String> result = new ArrayList<>();
        while (!reader.isAtEndOfObject()) {
            result.add(reader.readString(reader.nextElementName()));
        }
        return result.toArray(ArrayUtils.EMPTY_STRING_ARRAY);
    }
}