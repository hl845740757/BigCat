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

package cn.wjybxx.common.codec.binary;

import cn.wjybxx.common.codec.ConvertOptions;
import cn.wjybxx.common.codec.TypeArgInfo;
import cn.wjybxx.common.codec.TypeMetaRegistries;
import cn.wjybxx.common.codec.TypeMetaRegistry;
import cn.wjybxx.dson.DsonBinaryLiteReader;
import cn.wjybxx.dson.DsonBinaryLiteWriter;
import cn.wjybxx.dson.io.*;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.util.ArrayList;
import java.util.List;
import java.util.Objects;

/**
 * @author wjybxx
 * date 2023/4/2
 */
public class DefaultBinaryConverter implements BinaryConverter {

    final TypeMetaRegistry typeMetaRegistry;
    final BinaryCodecRegistry codecRegistry;
    final ConvertOptions options;

    private DefaultBinaryConverter(TypeMetaRegistry typeMetaRegistry,
                                   BinaryCodecRegistry codecRegistry,
                                   ConvertOptions options) {
        this.codecRegistry = codecRegistry;
        this.typeMetaRegistry = typeMetaRegistry;
        this.options = options;
    }

    @Override
    public BinaryCodecRegistry codecRegistry() {
        return codecRegistry;
    }

    @Override
    public TypeMetaRegistry typeMetaRegistry() {
        return typeMetaRegistry;
    }

    @Override
    public ConvertOptions options() {
        return options;
    }

    @Override
    public void write(Object value, DsonChunk chunk, TypeArgInfo<?> typeArgInfo) {
        Objects.requireNonNull(value);
        final DsonOutput outputStream = DsonOutputs.newInstance(chunk.getBuffer(), chunk.getOffset(), chunk.getLength());
        encodeObject(outputStream, value, typeArgInfo);
        chunk.setUsed(outputStream.getPosition());
    }

    @Override
    public <U> U read(DsonChunk chunk, TypeArgInfo<U> typeArgInfo) {
        final DsonInput inputStream = DsonInputs.newInstance(chunk.getBuffer(), chunk.getOffset(), chunk.getLength());
        return decodeObject(inputStream, typeArgInfo);
    }

    @Nonnull
    @Override
    public byte[] write(Object value, @Nonnull TypeArgInfo<?> typeArgInfo) {
        Objects.requireNonNull(value);
        final byte[] localBuffer = options.bufferPool.alloc();
        try {
            final DsonChunk chunk = new DsonChunk(localBuffer);
            write(value, chunk, typeArgInfo);

            final byte[] result = new byte[chunk.getUsed()];
            System.arraycopy(localBuffer, 0, result, 0, result.length);
            return result;
        } finally {
            options.bufferPool.release(localBuffer);
        }
    }

    @Override
    public <U> U cloneObject(Object value, TypeArgInfo<U> typeArgInfo) {
        Objects.requireNonNull(value);
        final byte[] localBuffer = options.bufferPool.alloc();
        try {
            final DsonOutput outputStream = DsonOutputs.newInstance(localBuffer);
            encodeObject(outputStream, value, typeArgInfo);

            final DsonInput inputStream = DsonInputs.newInstance(localBuffer, 0, outputStream.getPosition());
            return decodeObject(inputStream, typeArgInfo);
        } finally {
            options.bufferPool.release(localBuffer);
        }
    }

    private void encodeObject(DsonOutput outputStream, @Nullable Object value, TypeArgInfo<?> typeArgInfo) {
        try (BinaryObjectWriter wrapper = new DefaultBinaryObjectWriter(this,
                new DsonBinaryLiteWriter(options.binWriterSettings, outputStream))) {
            wrapper.writeObject(value, typeArgInfo);
            wrapper.flush();
        }
    }

    private <U> U decodeObject(DsonInput inputStream, TypeArgInfo<U> typeArgInfo) {
        try (BinaryObjectReader wrapper = new DefaultBinaryObjectReader(this,
                new DsonBinaryLiteReader(options.binReaderSettings, inputStream))) {
            return wrapper.readObject(typeArgInfo);
        }
    }

    // ------------------------------------------------- 工厂方法 ------------------------------------------------------

    /**
     * @param pojoCodecImplList 所有的普通对象编解码器，外部传入，因此用户可以处理冲突后传入
     * @param typeMetaRegistry  所有的类型id信息，包括protobuf的类
     * @param options           一些可选项
     */
    public static DefaultBinaryConverter newInstance(final List<? extends BinaryPojoCodecImpl<?>> pojoCodecImplList,
                                                     final TypeMetaRegistry typeMetaRegistry,
                                                     final ConvertOptions options) {
        Objects.requireNonNull(options, "options");
        // 检查classId是否存在，以及命名空间是否非法
        for (BinaryPojoCodecImpl<?> codecImpl : pojoCodecImplList) {
            typeMetaRegistry.checkedOfType(codecImpl.getEncoderClass());
        }

        // 转换codecImpl
        List<BinaryPojoCodec<?>> allPojoCodecList = new ArrayList<>(pojoCodecImplList.size());
        for (BinaryPojoCodecImpl<?> codecImpl : pojoCodecImplList) {
            allPojoCodecList.add(new BinaryPojoCodec<>(codecImpl));
        }

        return new DefaultBinaryConverter(
                TypeMetaRegistries.fromRegistries(
                        typeMetaRegistry,
                        BinaryConverterUtils.getDefaultTypeMetaRegistry()),
                BinaryCodecRegistries.fromRegistries(
                        BinaryCodecRegistries.fromPojoCodecs(allPojoCodecList),
                        BinaryConverterUtils.getDefaultCodecRegistry()),
                options);
    }

    /**
     * @param registryList     可以包含一些特殊的可以包含一些特殊的registry
     * @param typeMetaRegistry 所有的类型id信息，包括protobuf的类
     * @param options          一些可选项
     */
    public static DefaultBinaryConverter newInstance2(final List<BinaryCodecRegistry> registryList,
                                                      final TypeMetaRegistry typeMetaRegistry,
                                                      final ConvertOptions options) {
        ArrayList<BinaryCodecRegistry> copied = new ArrayList<>(registryList);
        copied.add(BinaryConverterUtils.getDefaultCodecRegistry());

        return new DefaultBinaryConverter(
                TypeMetaRegistries.fromRegistries(
                        typeMetaRegistry,
                        BinaryConverterUtils.getDefaultTypeMetaRegistry()),
                BinaryCodecRegistries.fromRegistries(
                        copied.toArray(BinaryCodecRegistry[]::new)),
                options);

    }
}