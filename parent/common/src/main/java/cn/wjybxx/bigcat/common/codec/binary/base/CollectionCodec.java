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

package cn.wjybxx.bigcat.common.codec.binary.base;

import cn.wjybxx.bigcat.common.codec.TypeArgInfo;
import cn.wjybxx.bigcat.common.codec.binary.BinaryPojoCodecImpl;
import cn.wjybxx.bigcat.common.codec.binary.BinaryPojoCodecScanIgnore;
import cn.wjybxx.bigcat.common.codec.binary.BinaryReader;
import cn.wjybxx.bigcat.common.codec.binary.BinaryWriter;

import javax.annotation.Nonnull;
import java.util.ArrayList;
import java.util.Collection;

/**
 * @author wjybxx
 * date 2023/4/4
 */
@SuppressWarnings("rawtypes")
@BinaryPojoCodecScanIgnore
public class CollectionCodec implements BinaryPojoCodecImpl<Collection> {

    @Nonnull
    @Override
    public Class<Collection> getEncoderClass() {
        return Collection.class;
    }

    @Override
    public void writeObject(Collection instance, BinaryWriter writer, TypeArgInfo<?> typeArgInfo) {
        TypeArgInfo<?> componentArgInfo = TypeArgInfo.of(typeArgInfo.typeArg1);
        for (Object e : instance) {
            writer.writeObject(e, componentArgInfo);
        }
    }

    @Override
    public Collection<?> readObject(BinaryReader reader, TypeArgInfo<?> typeArgInfo) {
        Collection<Object> result;
        if (typeArgInfo.factory != null) {
            result = (Collection<Object>) typeArgInfo.factory.get();
        } else {
            result = new ArrayList<>();
        }

        TypeArgInfo<?> componentArgInfo = TypeArgInfo.of(typeArgInfo.typeArg1);
        while (!reader.isAtEndOfObject()) {
            result.add(reader.readObject(componentArgInfo));
        }
        return result;
    }
}