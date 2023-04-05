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

package cn.wjybxx.bigcat.common.pb.codec;

import cn.wjybxx.bigcat.common.CollectionUtils;
import cn.wjybxx.bigcat.common.codec.TypeArgInfo;
import cn.wjybxx.bigcat.common.codec.binary.*;
import com.google.protobuf.Message;
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
public class ProtobufConverter implements BinaryConverter {

    final TypeIdRegistry typeIdRegistry;
    final BinaryCodecRegistry codecRegistry;
    final int recursionLimit;

    private ProtobufConverter(TypeIdRegistry typeIdRegistry, BinaryCodecRegistry codecRegistry, int recursionLimit) {
        this.codecRegistry = codecRegistry;
        this.typeIdRegistry = typeIdRegistry;
        this.recursionLimit = recursionLimit;
    }

    @Override
    public BinaryCodecRegistry codecRegistry() {
        return codecRegistry;
    }

    @Override
    public TypeIdRegistry typeIdRegistry() {
        return typeIdRegistry;
    }

    @Override
    public void write(@Nullable Object value, Chunk chunk, TypeArgInfo<?> typeArgInfo) {
        final CodedDataOutputStream outputStream = CodedDataOutputStream.newInstance(chunk.getBuffer(), chunk.getOffset(), chunk.getLength());
        encodeObject(outputStream, value, typeArgInfo);
        chunk.setUsed(outputStream.getTotalBytesWritten());
    }

    @Override
    public <U> U read(Chunk chunk, TypeArgInfo<U> typeArgInfo) {
        final CodedDataInputStream inputStream = CodedDataInputStream.newInstance(chunk.getBuffer(), chunk.getOffset(), chunk.getLength());
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

    @SuppressWarnings("unchecked")
    @Override
    public <U> U cloneObject(Object value, TypeArgInfo<U> typeArgInfo) {
        Objects.requireNonNull(value);
        final byte[] localBuffer = BufferPool.allocateBuffer();
        try {
            // 写入
            final CodedDataOutputStream outputStream = CodedDataOutputStream.newInstance(localBuffer);
            encodeObject(outputStream, value, typeArgInfo);

            // 读出
            final CodedDataInputStream inputStream = CodedDataInputStream.newInstance(localBuffer, 0, outputStream.getTotalBytesWritten());
            return decodeObject(inputStream, typeArgInfo);
        } finally {
            BufferPool.releaseBuffer(localBuffer);
        }
    }

    private void encodeObject(CodedDataOutputStream outputStream, @Nullable Object value, TypeArgInfo<?> typeArgInfo) {
        try (ProtobufWriter writer = new ProtobufWriter(this, outputStream)) {
            writer.writeTopLevelObject(value, typeArgInfo);
            writer.flush();
        }
    }

    private <U> U decodeObject(CodedDataInputStream inputStream, TypeArgInfo<U> typeArgInfo) {
        try (ProtobufReader reader = new ProtobufReader(this, inputStream)) {
            return reader.readTopLevelObject(typeArgInfo);
        }
    }

    // ------------------------------------------------- 工厂方法 ------------------------------------------------------

    /**
     * 指定所有的pb类和pojo类所在目录，通过Mapper创建对象
     * 该方法无法处理codec之间的冲突
     *
     * @param protoBufPackage protoBuf协议所在的包
     * @param pojoPackages    自定义对象所在的包
     * @param typeIdMapper    类型id映射策略
     */
    @SuppressWarnings("unchecked")
    public static ProtobufConverter newInstance(final String protoBufPackage,
                                                final Set<String> pojoPackages,
                                                final TypeIdMapper typeIdMapper,
                                                final int recursionLimit) {
        final Set<Class<?>> allProtoBufClasses = ProtoUtils.scan(Collections.singleton(protoBufPackage));

        final HashSet<Class<?>> allClasses = new HashSet<>(allProtoBufClasses);
        final List<? extends BinaryPojoCodecImpl<?>> pojoCodecImplList = BinaryPojoCodecScanner.scan(pojoPackages);
        pojoCodecImplList.forEach(e -> allClasses.add(e.getEncoderClass()));

        final Map<Class<?>, TypeId> typeIdMap = allClasses.stream()
                .collect(Collectors.toMap(e -> e, typeIdMapper::map));

        return newInstance(allProtoBufClasses, pojoCodecImplList, typeIdMap, recursionLimit);
    }

    /**
     * @param allProtoBufClasses 所有的protobuf类
     * @param pojoCodecImplList  所有的普通对象编解码器，外部传入，因此用户可以处理冲突后传入
     * @param typeIdMap          所有的类型id信息，包括protobuf的类
     * @param recursionLimit     递归深度限制
     */
    public static ProtobufConverter newInstance(final Set<Class<?>> allProtoBufClasses,
                                                final List<? extends BinaryPojoCodecImpl<?>> pojoCodecImplList,
                                                final Map<Class<?>, TypeId> typeIdMap, int recursionLimit) {
        if (recursionLimit < 1) {
            throw new IllegalArgumentException("recursionLimit " + recursionLimit);
        }
        // 检查typeId是否存在，以及命名空间是否非法
        for (Class<?> clazz : allProtoBufClasses) {
            TypeId typeId = CollectionUtils.checkGet(typeIdMap, clazz, "class");
            if (typeId.getNamespace() <= 0) {
                throw new IllegalArgumentException("bad namespace " + typeId.getNamespace());
            }
        }
        for (BinaryPojoCodecImpl<?> codecImpl : pojoCodecImplList) {
            TypeId typeId = CollectionUtils.checkGet(typeIdMap, codecImpl.getEncoderClass(), "class");
            if (typeId.getNamespace() <= 0) {
                throw new IllegalArgumentException("bad namespace " + typeId.getNamespace());
            }
        }

        final TypeIdRegistry typeIdRegistry = TypeIdRegistries.fromTypeIdMap(typeIdMap);
        final List<BinaryPojoCodec<?>> allPojoCodecList = new ArrayList<>(allProtoBufClasses.size() + pojoCodecImplList.size());
        // 解析parser
        for (Class<?> messageClazz : allProtoBufClasses) {
            // protoBuf消息
            if (Message.class.isAssignableFrom(messageClazz)) {
                @SuppressWarnings("unchecked") MessageCodec<?> messageCodec = parseMessageCodec((Class<? extends MessageLite>) messageClazz);
                allPojoCodecList.add(new BinaryPojoCodec<>(messageCodec, typeIdRegistry.checkedOfType(messageClazz)));
                continue;
            }
            // protoBuf枚举
            if (ProtocolMessageEnum.class.isAssignableFrom(messageClazz)) {
                @SuppressWarnings("unchecked") MessageEnumCodec<?> enumCodec = parseMessageEnumCodec((Class<? extends ProtocolMessageEnum>) messageClazz);
                allPojoCodecList.add(new BinaryPojoCodec<>(enumCodec, typeIdRegistry.checkedOfType(messageClazz)));
                continue;
            }
            throw new IllegalArgumentException("Unsupported class " + messageClazz.getName());
        }
        // 转换codecImpl
        for (BinaryPojoCodecImpl<?> codecImpl : pojoCodecImplList) {
            allPojoCodecList.add(new BinaryPojoCodec<>(codecImpl, typeIdRegistry.checkedOfType(codecImpl.getEncoderClass())));
        }

        final BinaryCodecRegistry codecRegistry = BinaryCodecRegistries.fromPojoCodecs(allPojoCodecList);
        return new ProtobufConverter(typeIdRegistry, codecRegistry, recursionLimit);
    }

    private static <T extends MessageLite> MessageCodec<T> parseMessageCodec(Class<T> messageClazz) {
        final var enumLiteMap = ProtoUtils.findParser(messageClazz);
        return new MessageCodec<>(messageClazz, enumLiteMap);
    }

    private static <T extends ProtocolMessageEnum> MessageEnumCodec<T> parseMessageEnumCodec(Class<T> messageClazz) {
        final var enumLiteMap = ProtoUtils.findMapper(messageClazz);
        return new MessageEnumCodec<>(messageClazz, enumLiteMap);
    }

}