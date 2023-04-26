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

package cn.wjybxx.common.dson.document;

import cn.wjybxx.common.CollectionUtils;
import cn.wjybxx.common.dson.DocClassId;
import cn.wjybxx.common.dson.TypeArgInfo;
import cn.wjybxx.common.dson.codec.ClassIdMapper;
import cn.wjybxx.common.dson.codec.ClassIdRegistries;
import cn.wjybxx.common.dson.codec.ClassIdRegistry;
import cn.wjybxx.common.dson.codec.ProtobufUtils;
import cn.wjybxx.common.dson.document.codecs.MessageCodec;
import cn.wjybxx.common.dson.document.codecs.MessageEnumCodec;
import cn.wjybxx.common.dson.io.*;
import com.google.protobuf.MessageLite;
import com.google.protobuf.ProtocolMessageEnum;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.util.*;
import java.util.stream.Collectors;

/**
 * @author wjybxx
 * date 2023/4/2
 */
public class DefaultDocumentConverter implements DocumentConverter {

    final ClassIdRegistry<DocClassId> typeIdRegistry;
    final DocumentCodecRegistry codecRegistry;
    final int recursionLimit;

    private DefaultDocumentConverter(ClassIdRegistry<DocClassId> typeIdRegistry, DocumentCodecRegistry codecRegistry, int recursionLimit) {
        this.codecRegistry = codecRegistry;
        this.typeIdRegistry = typeIdRegistry;
        this.recursionLimit = recursionLimit;
    }

    @Override
    public DocumentCodecRegistry codecRegistry() {
        return codecRegistry;
    }

    @Override
    public ClassIdRegistry<DocClassId> classIdRegistry() {
        return typeIdRegistry;
    }

    @Override
    public void write(Object value, Chunk chunk, TypeArgInfo<?> typeArgInfo) {
        Objects.requireNonNull(value);
        final DsonOutput outputStream = DsonOutputs.newInstance(chunk.getBuffer(), chunk.getOffset(), chunk.getLength());
        encodeObject(outputStream, value, typeArgInfo);
        chunk.setUsed(outputStream.position());
    }

    @Override
    public <U> U read(Chunk chunk, TypeArgInfo<U> typeArgInfo) {
        final DsonInput inputStream = DsonInputs.newInstance(chunk.getBuffer(), chunk.getOffset(), chunk.getLength());
        return decodeObject(inputStream, typeArgInfo);
    }

    @Nonnull
    @Override
    public byte[] write(Object value, @Nonnull TypeArgInfo<?> typeArgInfo) {
        Objects.requireNonNull(value);
        final byte[] localBuffer = BufferPool.allocateBuffer();
        try {
            final Chunk chunk = new Chunk(localBuffer);
            write(value, chunk, typeArgInfo);

            final byte[] result = new byte[chunk.getUsed()];
            System.arraycopy(localBuffer, 0, result, 0, result.length);
            return result;
        } finally {
            BufferPool.releaseBuffer(localBuffer);
        }
    }

    @Override
    public <U> U cloneObject(Object value, TypeArgInfo<U> typeArgInfo) {
        Objects.requireNonNull(value);
        final byte[] localBuffer = BufferPool.allocateBuffer();
        try {
            final DsonOutput outputStream = DsonOutputs.newInstance(localBuffer);
            encodeObject(outputStream, value, typeArgInfo);

            final DsonInput inputStream = DsonInputs.newInstance(localBuffer, 0, outputStream.position());
            return decodeObject(inputStream, typeArgInfo);
        } finally {
            BufferPool.releaseBuffer(localBuffer);
        }
    }

    private void encodeObject(DsonOutput outputStream, @Nullable Object value, TypeArgInfo<?> typeArgInfo) {
        try (DocumentObjectWriter wrapper = new DefaultDocumentObjectWriter(this,
                new DefaultDsonDocWriter(outputStream, recursionLimit))) {
            wrapper.writeObject(value, typeArgInfo);
            wrapper.flush();
        }
    }

    private <U> U decodeObject(DsonInput inputStream, TypeArgInfo<U> typeArgInfo) {
        try (DocumentObjectReader wrapper = new DefaultDocumentObjectReader(this,
                new DefaultDsonDocReader(inputStream, recursionLimit))) {
            return wrapper.readObject(typeArgInfo);
        }
    }

    // ------------------------------------------------- 工厂方法 ------------------------------------------------------

    /**
     * 指定所有的pb类和pojo类所在目录，通过Mapper创建对象
     * 该方法无法处理codec之间的冲突
     *
     * @param protoBufPackages protoBuf协议所在的包
     * @param pojoPackages     自定义对象所在的包
     * @param classIdMapper    类型id映射策略
     */
    public static DefaultDocumentConverter newInstance(final Set<String> protoBufPackages,
                                                       final Set<String> pojoPackages,
                                                       final ClassIdMapper<DocClassId> classIdMapper,
                                                       final int recursionLimit) {
        final Set<Class<?>> allProtoBufClasses = ProtobufUtils.scan(protoBufPackages);

        final HashSet<Class<?>> allClasses = new HashSet<>(allProtoBufClasses);
        final List<? extends DocumentPojoCodecImpl<?>> pojoCodecImplList = DocumentPojoCodecScanner.scan(pojoPackages);
        pojoCodecImplList.forEach(e -> allClasses.add(e.getEncoderClass()));

        final Map<Class<?>, DocClassId> classIdMap = allClasses.stream()
                .collect(Collectors.toMap(e -> e, classIdMapper::map));

        return newInstance(allProtoBufClasses, pojoCodecImplList, classIdMap, recursionLimit);
    }

    /**
     * @param allProtoBufClasses 所有的protobuf类
     * @param pojoCodecImplList  所有的普通对象编解码器，外部传入，因此用户可以处理冲突后传入
     * @param typeIdMap          所有的类型id信息，包括protobuf的类
     * @param recursionLimit     递归深度限制
     */
    @SuppressWarnings("unchecked")
    public static DefaultDocumentConverter newInstance(final Set<Class<?>> allProtoBufClasses,
                                                       final List<? extends DocumentPojoCodecImpl<?>> pojoCodecImplList,
                                                       final Map<Class<?>, DocClassId> typeIdMap, int recursionLimit) {
        if (recursionLimit < 1) {
            throw new IllegalArgumentException("recursionLimit " + recursionLimit);
        }
        // 检查typeId是否存在，以及命名空间是否非法
        for (Class<?> clazz : allProtoBufClasses) {
            DocClassId classId = CollectionUtils.checkGet(typeIdMap, clazz, "class");
            if (classId.isObjectClassId()) {
                throw new IllegalArgumentException("bad classId " + classId + ", class " + clazz);
            }
        }
        for (DocumentPojoCodecImpl<?> codecImpl : pojoCodecImplList) {
            DocClassId classId = CollectionUtils.checkGet(typeIdMap, codecImpl.getEncoderClass(), "class");
            if (classId.isObjectClassId()) {
                throw new IllegalArgumentException("bad classId " + classId + ", class " + codecImpl.getEncoderClass());
            }
        }

        final List<DocumentPojoCodec<?>> allPojoCodecList = new ArrayList<>(allProtoBufClasses.size() + pojoCodecImplList.size());
        // 解析parser
        for (Class<?> messageClazz : allProtoBufClasses) {
            // protoBuf消息
            if (MessageLite.class.isAssignableFrom(messageClazz)) {
                MessageCodec<?> messageCodec = parseMessageCodec((Class<? extends MessageLite>) messageClazz);
                allPojoCodecList.add(new DocumentPojoCodec<>(messageCodec));
                continue;
            }
            // protoBuf枚举
            if (ProtocolMessageEnum.class.isAssignableFrom(messageClazz)) {
                MessageEnumCodec<?> enumCodec = parseMessageEnumCodec((Class<? extends ProtocolMessageEnum>) messageClazz);
                allPojoCodecList.add(new DocumentPojoCodec<>(enumCodec));
                continue;
            }
            throw new IllegalArgumentException("Unsupported class " + messageClazz);
        }
        // 转换codecImpl
        for (DocumentPojoCodecImpl<?> codecImpl : pojoCodecImplList) {
            allPojoCodecList.add(new DocumentPojoCodec<>(codecImpl));
        }

        ClassIdRegistry<DocClassId> classIdRegistry = ClassIdRegistries.fromRegistries(ClassIdRegistries.fromClassIdMap(typeIdMap),
                DocumentConverterUtils.getDefaultTypeIdRegistry());

        final DocumentCodecRegistry codecRegistry = DocumentCodecRegistries.fromRegistries(DocumentCodecRegistries.fromPojoCodecs(allPojoCodecList),
                DocumentConverterUtils.getDefaultCodecRegistry());

        return new DefaultDocumentConverter(classIdRegistry, codecRegistry, recursionLimit);
    }

    private static <T extends MessageLite> MessageCodec<T> parseMessageCodec(Class<T> messageClazz) {
        final var enumLiteMap = ProtobufUtils.findParser(messageClazz);
        return new MessageCodec<>(messageClazz, enumLiteMap);
    }

    private static <T extends ProtocolMessageEnum> MessageEnumCodec<T> parseMessageEnumCodec(Class<T> messageClazz) {
        final var enumLiteMap = ProtobufUtils.findMapper(messageClazz);
        return new MessageEnumCodec<>(messageClazz, enumLiteMap);
    }

}