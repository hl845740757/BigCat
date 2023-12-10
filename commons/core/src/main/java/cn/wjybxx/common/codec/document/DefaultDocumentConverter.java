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

package cn.wjybxx.common.codec.document;

import cn.wjybxx.common.codec.ConvertOptions;
import cn.wjybxx.common.codec.TypeArgInfo;
import cn.wjybxx.common.codec.TypeMetaRegistries;
import cn.wjybxx.common.codec.TypeMetaRegistry;
import cn.wjybxx.dson.*;
import cn.wjybxx.dson.io.*;
import cn.wjybxx.dson.text.*;
import org.apache.commons.io.output.StringBuilderWriter;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.io.Reader;
import java.io.Writer;
import java.util.ArrayList;
import java.util.List;
import java.util.Objects;

/**
 * @author wjybxx
 * date 2023/4/2
 */
public class DefaultDocumentConverter implements DocumentConverter {

    final TypeMetaRegistry typeMetaRegistry;
    final DocumentCodecRegistry codecRegistry;
    final ConvertOptions options;

    private DefaultDocumentConverter(TypeMetaRegistry typeMetaRegistry,
                                     DocumentCodecRegistry codecRegistry,
                                     ConvertOptions options) {
        this.codecRegistry = codecRegistry;
        this.typeMetaRegistry = typeMetaRegistry;
        this.options = options;
    }

    @Override
    public DocumentCodecRegistry codecRegistry() {
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
        try (DocumentObjectWriter wrapper = new DefaultDocumentObjectWriter(this,
                new DsonBinaryWriter(options.binWriterSettings, outputStream))) {
            wrapper.writeObject(value, typeArgInfo, null);
            wrapper.flush();
        }
    }

    private <U> U decodeObject(DsonInput inputStream, TypeArgInfo<U> typeArgInfo) {
        try (DsonReader binaryReader = new DsonBinaryReader(options.binReaderSettings, inputStream)) {
            DocumentObjectReader wrapper;
            if (!options.appendDef || !options.appendNull) { // 二进制数据不全
                wrapper = new DefaultDocumentObjectReader(this, toDsonObjectReader(binaryReader));
            } else {
                wrapper = new SequentialObjectReader(this, binaryReader);
            }
            return wrapper.readObject(typeArgInfo);
        }
    }

    private DsonObjectReader toDsonObjectReader(DsonReader dsonReader) {
        DsonValue dsonValue = Dsons.readTopDsonValue(dsonReader);
        return new DsonObjectReader(options.binReaderSettings, new DsonArray<String>().append(dsonValue));
    }

    @Nonnull
    @Override
    public String writeAsDson(Object value, DsonMode dsonMode, @Nonnull TypeArgInfo<?> typeArgInfo) {
        StringBuilder stringBuilder = options.stringBuilderPool.alloc();
        try {
            writeAsDson(value, dsonMode, typeArgInfo, new StringBuilderWriter(stringBuilder));
            return stringBuilder.toString();
        } finally {
            options.stringBuilderPool.release(stringBuilder);
        }
    }

    @Override
    public <U> U readFromDson(CharSequence source, DsonMode dsonMode, @Nonnull TypeArgInfo<U> typeArgInfo) {
        try (DsonReader textReader = new DsonTextReader(options.textReaderSettings, Dsons.newStringScanner(source, dsonMode));
             DocumentObjectReader wrapper = new DefaultDocumentObjectReader(this, toDsonObjectReader(textReader))) {
            return wrapper.readObject(typeArgInfo);
        }
    }

    @Override
    public void writeAsDson(Object value, DsonMode dsonMode, @Nonnull TypeArgInfo<?> typeArgInfo, Writer writer) {
        Objects.requireNonNull(writer, "writer");
        DsonTextWriterSettings writerSettings = dsonMode == DsonMode.RELAXED ? options.jsonWriterSettings : options.textWriterSettings;
        try (DocumentObjectWriter wrapper = new DefaultDocumentObjectWriter(this,
                new DsonTextWriter(writerSettings, writer))) {
            wrapper.writeObject(value, typeArgInfo, null);
            wrapper.flush();
        }
    }

    @Override
    public <U> U readFromDson(Reader source, DsonMode dsonMode, @Nonnull TypeArgInfo<U> typeArgInfo) {
        try (DsonReader textReader = new DsonTextReader(options.textReaderSettings, Dsons.newStreamScanner(source, dsonMode, 256, true));
             DocumentObjectReader wrapper = new DefaultDocumentObjectReader(this, toDsonObjectReader(textReader))) {
            return wrapper.readObject(typeArgInfo);
        }
    }

    @Override
    public DsonValue writeAsDsonValue(Object value, TypeArgInfo<?> typeArgInfo) {
        Objects.requireNonNull(value);
        DsonArray<String> outList = new DsonArray<>(1);
        try (DsonWriter objectWriter = new DsonObjectWriter(options.binWriterSettings, outList);
             DocumentObjectWriter wrapper = new DefaultDocumentObjectWriter(this, objectWriter)) {
            wrapper.writeObject(value, typeArgInfo, ObjectStyle.INDENT);
            DsonValue dsonValue = outList.get(0);
            if (dsonValue.getDsonType().isContainer()) {
                return dsonValue;
            }
            throw new IllegalArgumentException("value must be container");
        }
    }

    @Override
    public <U> U readFromDsonValue(DsonValue source, @Nonnull TypeArgInfo<U> typeArgInfo) {
        if (!source.getDsonType().isContainer()) {
            throw new IllegalArgumentException("value must be container");
        }
        try (DsonReader objectReader = new DsonObjectReader(options.binReaderSettings, new DsonArray<String>().append(source));
             DocumentObjectReader wrapper = new DefaultDocumentObjectReader(this, toDsonObjectReader(objectReader))) {
            return wrapper.readObject(typeArgInfo);
        }
    }

    // ------------------------------------------------- 工厂方法 ------------------------------------------------------

    /**
     * @param pojoCodecImplList 所有的普通对象编解码器，外部传入，因此用户可以处理冲突后传入
     * @param typeMetaRegistry  所有的类型id信息，包括protobuf的类
     * @param options           一些可选项
     */
    public static DefaultDocumentConverter newInstance(final List<? extends DocumentPojoCodecImpl<?>> pojoCodecImplList,
                                                       final TypeMetaRegistry typeMetaRegistry,
                                                       final ConvertOptions options) {
        Objects.requireNonNull(options, "options");
        // 检查classId是否存在，以及命名是否非法
        for (DocumentPojoCodecImpl<?> codecImpl : pojoCodecImplList) {
            typeMetaRegistry.checkedOfType(codecImpl.getEncoderClass());
        }

        // 转换codecImpl
        final List<DocumentPojoCodec<?>> allPojoCodecList = new ArrayList<>(pojoCodecImplList.size());
        for (DocumentPojoCodecImpl<?> codecImpl : pojoCodecImplList) {
            allPojoCodecList.add(new DocumentPojoCodec<>(codecImpl));
        }
        return new DefaultDocumentConverter(
                TypeMetaRegistries.fromRegistries(
                        typeMetaRegistry,
                        DocumentConverterUtils.getDefaultTypeMetaRegistry()),
                DocumentCodecRegistries.fromRegistries(
                        DocumentCodecRegistries.fromPojoCodecs(allPojoCodecList),
                        DocumentConverterUtils.getDefaultCodecRegistry()),
                options);
    }

    /**
     * @param registryList     可以包含一些特殊的registry
     * @param typeMetaRegistry 所有的类型id信息，包括protobuf的类
     * @param options          一些可选项
     * @return
     */
    public static DefaultDocumentConverter newInstance2(final List<DocumentCodecRegistry> registryList,
                                                        final TypeMetaRegistry typeMetaRegistry,
                                                        final ConvertOptions options) {

        ArrayList<DocumentCodecRegistry> copied = new ArrayList<>(registryList);
        copied.add(DocumentConverterUtils.getDefaultCodecRegistry());

        return new DefaultDocumentConverter(
                TypeMetaRegistries.fromRegistries(
                        typeMetaRegistry,
                        DocumentConverterUtils.getDefaultTypeMetaRegistry()),
                DocumentCodecRegistries.fromRegistries(
                        copied.toArray(DocumentCodecRegistry[]::new)),
                options);
    }
}