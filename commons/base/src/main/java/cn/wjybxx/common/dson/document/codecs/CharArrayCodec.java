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

package cn.wjybxx.common.dson.document.codecs;

import cn.wjybxx.common.dson.TypeArgInfo;
import cn.wjybxx.common.dson.document.DocumentObjectReader;
import cn.wjybxx.common.dson.document.DocumentObjectWriter;
import cn.wjybxx.common.dson.document.DocumentPojoCodecImpl;
import cn.wjybxx.common.dson.document.DocumentPojoCodecScanIgnore;
import it.unimi.dsi.fastutil.chars.CharArrayList;

import javax.annotation.Nonnull;

/**
 * @author wjybxx
 * date 2023/4/4
 */
@DocumentPojoCodecScanIgnore
public class CharArrayCodec implements DocumentPojoCodecImpl<char[]> {

    @Nonnull
    @Override
    public String getTypeName() {
        return "char[]";
    }

    @Nonnull
    @Override
    public Class<char[]> getEncoderClass() {
        return char[].class;
    }

    @Override
    public void writeObject(char[] instance, DocumentObjectWriter writer, TypeArgInfo<?> typeArgInfo) {
        for (char e : instance) {
            writer.writeChar(null, e);
        }
    }

    @Override
    public char[] readObject(DocumentObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        CharArrayList result = new CharArrayList();
        while (!reader.isAtEndOfObject()) {
            result.add(reader.readChar(null));
        }
        return result.toCharArray();
    }
}