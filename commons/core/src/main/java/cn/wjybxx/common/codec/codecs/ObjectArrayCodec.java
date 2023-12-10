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
public class ObjectArrayCodec implements PojoCodecImpl<Object[]> {

    @Nonnull
    @Override
    public Class<Object[]> getEncoderClass() {
        return Object[].class;
    }

    @Override
    public void writeObject(BinaryObjectWriter writer, Object[] instance, TypeArgInfo<?> typeArgInfo) {
        TypeArgInfo<?> componentArgInfo = ConverterUtils.findComponentTypeArg(typeArgInfo.declaredType);
        for (Object e : instance) {
            writer.writeObject(0, e, componentArgInfo);
        }
    }

    @Override
    public Object[] readObject(BinaryObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        ArrayList<Object> result = new ArrayList<>();
        TypeArgInfo<?> componentArgInfo = ConverterUtils.findComponentTypeArg(typeArgInfo.declaredType);
        while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
            result.add(reader.readObject(0, componentArgInfo));
        }
        // 一定不是基础类型数组
        return (Object[]) ConverterUtils.convertList2Array(result, typeArgInfo.declaredType);
    }

    @Override
    public void writeObject(DocumentObjectWriter writer, Object[] instance, TypeArgInfo<?> typeArgInfo, ObjectStyle style) {
        TypeArgInfo<?> componentArgInfo = ConverterUtils.findComponentTypeArg(typeArgInfo.declaredType);
        for (Object e : instance) {
            writer.writeObject(null, e, componentArgInfo, null);
        }
    }

    @Override
    public Object[] readObject(DocumentObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        ArrayList<Object> result = new ArrayList<>();
        TypeArgInfo<?> componentArgInfo = ConverterUtils.findComponentTypeArg(typeArgInfo.declaredType);
        while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
            result.add(reader.readObject(null, componentArgInfo));
        }
        // 一定不是基础类型数组
        return (Object[]) ConverterUtils.convertList2Array(result, typeArgInfo.declaredType);
    }
}