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

import javax.annotation.Nonnull;
import java.util.ArrayList;
import java.util.Collection;

/**
 * @author wjybxx
 * date 2023/4/4
 */
@SuppressWarnings("rawtypes")
@DocumentPojoCodecScanIgnore
public class CollectionCodec implements DocumentPojoCodecImpl<Collection> {

    @Nonnull
    @Override
    public String getTypeName() {
        return "Collection";
    }

    @Nonnull
    @Override
    public Class<Collection> getEncoderClass() {
        return Collection.class;
    }

    @Override
    public void writeObject(Collection instance, DocumentObjectWriter writer, TypeArgInfo<?> typeArgInfo) {
        TypeArgInfo<?> componentArgInfo = TypeArgInfo.of(typeArgInfo.typeArg1);
        for (Object e : instance) {
            writer.writeObject(null, e, componentArgInfo);
        }
    }

    @Override
    public Collection<?> readObject(DocumentObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        Collection<Object> result;
        if (typeArgInfo.factory != null) {
            result = (Collection<Object>) typeArgInfo.factory.get();
        } else {
            result = new ArrayList<>();
        }

        TypeArgInfo<?> componentArgInfo = TypeArgInfo.of(typeArgInfo.typeArg1);
        while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
            result.add(reader.readObject(null, componentArgInfo));
        }
        return result;
    }
}