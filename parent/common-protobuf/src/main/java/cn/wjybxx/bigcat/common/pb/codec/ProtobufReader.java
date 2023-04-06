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

import cn.wjybxx.bigcat.common.codec.EntityConverterUtils;
import cn.wjybxx.bigcat.common.codec.TypeArgInfo;
import cn.wjybxx.bigcat.common.codec.binary.BinaryConverterUtils;
import cn.wjybxx.bigcat.common.codec.binary.BinaryPojoCodec;
import cn.wjybxx.bigcat.common.codec.binary.BinaryReader;
import cn.wjybxx.bigcat.common.codec.binary.TypeId;
import com.google.protobuf.Parser;
import org.apache.commons.lang3.exception.ExceptionUtils;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.util.Objects;

/**
 * @author wjybxx
 * date 2023/4/2
 */
public class ProtobufReader implements BinaryReader {

    private final ProtobufConverter converter;
    private final CodedDataInputStream inputStream;

    private Context context;
    private Context pooledContext; // 一个额外的缓存，用于写集合等减少上下文创建

    public ProtobufReader(ProtobufConverter converter, CodedDataInputStream inputStream) {
        this.converter = converter;
        this.inputStream = inputStream;
    }

    <T> T readTopLevelObject(TypeArgInfo<T> typeArgInfo) {
        try {
            final BinaryValueType currentValueType = readTag(inputStream);
            if (currentValueType != BinaryValueType.OBJECT) {
                throw new ProtobufCodecException("top level must be object");
            }

            T result = readObjectWait(typeArgInfo);
            pooledContext = null; // help gc
            return result;
        } catch (Exception e) {
            return ExceptionUtils.rethrow(ProtobufCodecException.wrap(e));
        }
    }

    // region 基础api

    @Override
    public int readInt() {
        try {
            return readIntValue(readTag(inputStream));
        } catch (Exception e) {
            return ExceptionUtils.rethrow(e);
        }
    }

    @Override
    public long readLong() {
        try {
            return readLongValue(readTag(inputStream));
        } catch (Exception e) {
            return ExceptionUtils.rethrow(e);
        }
    }

    @Override
    public float readFloat() {
        try {
            return readFloatValue(readTag(inputStream));
        } catch (Exception e) {
            return ExceptionUtils.rethrow(e);
        }
    }

    @Override
    public double readDouble() {
        try {
            return readDoubleValue(readTag(inputStream));
        } catch (Exception e) {
            return ExceptionUtils.rethrow(e);
        }
    }

    @Override
    public boolean readBoolean() {
        try {
            return readBoolValue(readTag(inputStream));
        } catch (Exception e) {
            return ExceptionUtils.rethrow(e);
        }
    }

    @Override
    public String readString() {
        try {
            BinaryValueType valueType = readTag(inputStream);
            return readStringValue(valueType);
        } catch (Exception e) {
            return ExceptionUtils.rethrow(e);
        }
    }

    @Override
    public byte[] readBytes() {
        try {
            BinaryValueType valueType = readTag(inputStream);
            return readBinaryValue(valueType);
        } catch (Exception e) {
            return ExceptionUtils.rethrow(e);
        }
    }

    @Override
    public int readBytes(byte[] out, int offset) {
        try {
            BinaryValueType valueType = readTag(inputStream);
            return readBinaryValue(valueType, out, offset);
        } catch (Exception e) {
            return ExceptionUtils.rethrow(e);
        }
    }

    public <T> T readMessage(@Nonnull Parser<T> parser) {
        try {
            return inputStream.readMessageNoSize(parser);
        } catch (Exception e) {
            return ExceptionUtils.rethrow(e);
        }
    }

    @Override
    public void close() {

    }

    static BinaryValueType readTag(CodedDataInputStream codedDataInputStream) throws IOException {
        final byte number = codedDataInputStream.readRawByte();
        return BinaryValueType.forNumber(number);
    }

    static byte readNameSpace(CodedDataInputStream codedDataInputStream) throws IOException {
        return codedDataInputStream.readRawByte();
    }

    static int readClassId(CodedDataInputStream codedDataInputStream) throws IOException {
        return codedDataInputStream.readFixed32();
    }

    // endregion

    // region 值解析

    private int readIntValue(BinaryValueType type) throws IOException {
        switch (type) {
            case INT -> {
                return inputStream.readInt32();
            }
            case LONG -> {
                return (int) inputStream.readInt64();
            }
            case FLOAT -> {
                return (int) inputStream.readFloat();
            }
            case DOUBLE -> {
                return (int) inputStream.readDouble();
            }
            case NULL -> {
                return 0;
            }
        }
        throw ProtobufCodecException.incompatible(Integer.class, type);
    }

    private long readLongValue(BinaryValueType type) throws IOException {
        switch (type) {
            case INT -> {
                return inputStream.readInt32();
            }
            case LONG -> {
                return inputStream.readInt64();
            }
            case FLOAT -> {
                return (long) inputStream.readFloat();
            }
            case DOUBLE -> {
                return (long) inputStream.readDouble();
            }
            case NULL -> {
                return 0;
            }
        }
        throw ProtobufCodecException.incompatible(Long.class, type);
    }

    private float readFloatValue(BinaryValueType type) throws IOException {
        switch (type) {
            case INT -> {
                return inputStream.readInt32();
            }
            case LONG -> {
                return inputStream.readInt64();
            }
            case FLOAT -> {
                return inputStream.readFloat();
            }
            case DOUBLE -> {
                return (float) inputStream.readDouble();
            }
            case NULL -> {
                return 0;
            }
        }
        throw ProtobufCodecException.incompatible(Float.class, type);
    }

    private double readDoubleValue(BinaryValueType type) throws IOException {
        switch (type) {
            case INT -> {
                return inputStream.readInt32();
            }
            case LONG -> {
                return inputStream.readInt64();
            }
            case FLOAT -> {
                return inputStream.readFloat();
            }
            case DOUBLE -> {
                return inputStream.readDouble();
            }
            case NULL -> {
                return 0;
            }
        }
        throw ProtobufCodecException.incompatible(Double.class, type);
    }

    private boolean readBoolValue(BinaryValueType type) throws IOException {
        switch (type) {
            case INT -> {
                return inputStream.readInt32() != 0;
            }
            case LONG -> {
                return inputStream.readInt64() != 0;
            }
            case BOOLEAN -> {
                return inputStream.readBool();
            }
            case NULL -> {
                return false;
            }
        }
        throw ProtobufCodecException.incompatible(Boolean.class, type);
    }

    private String readStringValue(BinaryValueType valueType) throws IOException {
        switch (valueType) {
            case STRING -> {
                return inputStream.readString();
            }
            case BINARY, OBJECT -> {
                int length = inputStream.readFixed32();
                byte[] bytes = inputStream.readRawBytes(length);
                return new String(bytes, StandardCharsets.UTF_8);
            }
            case NULL -> {
                return null;
            }
        }
        throw ProtobufCodecException.incompatible(String.class, valueType);
    }

    private byte[] readBinaryValue(BinaryValueType valueType) throws IOException {
        switch (valueType) {
            case BINARY, OBJECT -> {
                int length = inputStream.readFixed32();
                return inputStream.readRawBytes(length);
            }
            case NULL -> {
                return null;
            }
        }
        throw ProtobufCodecException.incompatible(byte[].class, valueType);
    }

    private int readBinaryValue(BinaryValueType valueType, byte[] out, int offset) throws IOException {
        switch (valueType) {
            case BINARY, OBJECT -> {
                int size = inputStream.readFixed32();
                inputStream.readRawBytes(size, out, offset);
                return size;
            }
            case NULL -> {
                return 0;
            }
        }
        throw ProtobufCodecException.incompatible(byte[].class, valueType);
    }

    // endregion

    @SuppressWarnings("unchecked")
    @Nullable
    @Override
    public <T> T readObject(TypeArgInfo<T> typeArgInfo) {
        Class<T> declaredType = typeArgInfo.declaredType;
        try {
            BinaryValueType valueType = readTag(inputStream);
            if (valueType == BinaryValueType.NULL) { // 基础类型不能返回null
                return (T) EntityConverterUtils.getDefaultValue(declaredType);
            }
            // 基础类型--概率最高
            if (declaredType.isPrimitive()) {
                return (T) readPrimitive(valueType, declaredType);
            }

            // 特殊类型测试 - String和byte[]都可以接收Object
            if (declaredType == String.class) {
                return (T) readStringValue(valueType);
            }
            if (declaredType == byte[].class) {
                return (T) readBinaryValue(valueType);
            }
            // 其它情况下，如果写入的是Object，则必须按照Object解析，且codec必须存在
            if (valueType == BinaryValueType.OBJECT) {
                return readObjectWait(typeArgInfo);
            }

            // 考虑包装类型
            Class<?> unwrappedType = EntityConverterUtils.unboxIfWrapperType(declaredType);
            if (unwrappedType.isPrimitive()) {
                return (T) readPrimitive(valueType, unwrappedType);
            }
            // 读取之后转换
            return (T) readByValueType(valueType, typeArgInfo);
        } catch (Exception e) {
            return ExceptionUtils.rethrow(e);
        }
    }

    private Object readByValueType(BinaryValueType valueType, TypeArgInfo<?> typeArgInfo) throws IOException {
        Class<?> declaredType = typeArgInfo.declaredType;
        switch (valueType) {
            case INT -> {
                return declaredType.cast(inputStream.readInt32());
            }
            case LONG -> {
                return declaredType.cast(inputStream.readInt64());
            }
            case FLOAT -> {
                return declaredType.cast(inputStream.readFloat());
            }
            case DOUBLE -> {
                return declaredType.cast(inputStream.readDouble());
            }
            case BOOLEAN -> {
                return declaredType.cast(inputStream.readBool());
            }
            case STRING -> {
                return declaredType.cast(inputStream.readString());
            }
            case BINARY -> {
                return declaredType.cast(readBinaryValue(valueType));
            }
            case NULL, OBJECT -> {
                throw new AssertionError(); // 在前面进行了测试
            }
        }
        throw ProtobufCodecException.invalidTag(valueType);
    }

    private Object readPrimitive(BinaryValueType valueType, Class<?> targetType) throws IOException {
        if (targetType == int.class) {
            return readIntValue(valueType);
        }
        if (targetType == long.class) {
            return readLongValue(valueType);
        }
        if (targetType == float.class) {
            return readFloatValue(valueType);
        }
        if (targetType == double.class) {
            return readDoubleValue(valueType);
        }
        if (targetType == boolean.class) {
            return readBoolValue(valueType);
        }
        if (targetType == short.class) {
            return (short) readIntValue(valueType);
        }
        if (targetType == char.class) {
            return (char) readIntValue(valueType);
        }
        if (targetType == byte.class) {
            return (byte) readIntValue(valueType);
        }
        throw new AssertionError();
    }

    @Override
    public boolean isAtEndOfObject() {
        try {
            return inputStream.isAtEnd();
        } catch (Exception e) {
            return ExceptionUtils.rethrow(e);
        }
    }

    @Override
    public byte[] remainObjectBytes() {
        try {
            int n = inputStream.getBytesUntilLimit();
            return inputStream.readRawBytes(n);
        } catch (Exception e) {
            return ExceptionUtils.rethrow(e);
        }
    }

    // region 读对象核心代码

    /**
     * 已经确定接下来的Value是一个Object，进行读Object前的准备工作，包括：
     * 查找codec，等待codec调用{@link BinaryReader#readStartObject(TypeArgInfo)}
     */
    private <T> T readObjectWait(TypeArgInfo<T> typeArgInfo) throws IOException {
        if (typeArgInfo.declaredType.isPrimitive()) {
            throw ProtobufCodecException.incompatible(typeArgInfo.declaredType, BinaryValueType.OBJECT);
        }

        Context context = newContext();
        BinaryPojoCodec<? extends T> codec = readObjectDecoder(context, typeArgInfo);
        if (codec == null) {
            throw ProtobufCodecException.incompatible(typeArgInfo.declaredType, context.typeId);
        }

        context.state = State.WAIT_START;
        context.waitDecodeTypeArg = typeArgInfo;
        context.codec = codec;
        this.context = context;

        return codec.readObject(this, typeArgInfo);
    }

    @Nullable
    private <T> BinaryPojoCodec<? extends T> readObjectDecoder(Context context, TypeArgInfo<T> typeArgInfo) throws IOException {
        final CodedDataInputStream inputStream = this.inputStream;

        // 读取length
        final int size = inputStream.readFixed32();
        context.oldLimit = inputStream.pushLimit(size);

        // 读取typeId
        byte nameSpace = readNameSpace(inputStream);
        long typeId;
        if (nameSpace != TypeId.INVALID_NAMESPACE) {
            int classId = readClassId(inputStream);
            typeId = TypeId.toGuid(nameSpace, classId);
        } else {
            typeId = 0;
        }
        context.typeId = typeId;
        return findObjectDecoder(typeArgInfo, typeId);
    }

    private <T> BinaryPojoCodec<? extends T> findObjectDecoder(TypeArgInfo<T> typeArgInfo, long typeId) {
        final Class<T> declaredType = typeArgInfo.declaredType;
        final Class<?> encodedType = findEncodedType(typeId);

        // 尝试按真实类型读
        if (encodedType != null && declaredType.isAssignableFrom(encodedType)) {
            @SuppressWarnings("unchecked") BinaryPojoCodec<? extends T> codec = (BinaryPojoCodec<? extends T>) converter.codecRegistry.checkedGet(encodedType);
            return codec;
        }
        // 尝试按照声明类型读 - 读的时候两者可能是无继承关系的
        BinaryPojoCodec<T> codec = converter.codecRegistry.get(declaredType);
        if (codec != null) {
            return codec;
        }
        // 查找默认解码器 -- 读的时候声明类型重要
        return BinaryConverterUtils.getDefaultDecoder(declaredType);
    }

    private Class<?> findEncodedType(long typeId) {
        if (typeId > 0) {
            return TypeId.isDefaultNameSpaceTypeId(typeId) ?
                    BinaryConverterUtils.classOfId(typeId) : converter.typeIdRegistry.ofId(typeId);
        }
        return null;
    }

    @Override
    public Class<?> readStartObject(@Nonnull TypeArgInfo<?> typeArgInfo) {
        Objects.requireNonNull(typeArgInfo);
        // 上下文分两种：
        // 1.读当前对象，外部读取到接下来是一个Object，codec调用readStartObject走到这里 -- 此时context状态为waitStart，参数typeArg为当前对象的类型信息
        // 2.读嵌套对象，codec在读值的过程中直接调用readStartObject，通常是用户自行调用 -- 此时context的状态为reading，参数typeArg为新对象的类型信息
        Context context = this.context;
        if (context.state == State.READING) {
            // 用户直接调用readStartObject
            try {
                final BinaryValueType valueType = readTag(inputStream);
                if (valueType.getNumber() < BinaryValueType.OBJECT.getNumber()) {
                    return valueType.getJavaType(); // 写入的不是Object容器
                }
                if (valueType != BinaryValueType.OBJECT) { // 该上下文只支持Object
                    throw ProtobufCodecException.incompatible(BinaryValueType.OBJECT, valueType);
                }
                if (context.recursionDepth >= converter.recursionLimit) {
                    throw ProtobufCodecException.recursionLimitExceeded();
                }

                Context newContext = newContext();
                newContext.state = State.WAIT_START;
                newContext.codec = readObjectDecoder(newContext, typeArgInfo); // 允许codec不存在
                this.context = newContext;
                context = newContext;
            } catch (Exception e) {
                return ExceptionUtils.rethrow(e);
            }
        } else if (context.state == State.WAIT_START) {
            // 通过外层codec走到这 -- 判断用户是否发起了读替换
            if (context.waitDecodeTypeArg != typeArgInfo) {
                context.codec = findObjectDecoder(typeArgInfo, 0);
            }
            context.waitDecodeTypeArg = null;
        } else {
            throw ProtobufCodecException.contextError();
        }

        context.state = State.READING;
        if (context.codec != null) {
            return context.codec.getEncoderClass();
        }
        return typeArgInfo.declaredType;
    }

    @Override
    public void readEndObject() {
        Context context = this.context;
        assert context.state == State.READING;
        try {
            // 用户既然调用了readEnd，则剩余部分对于用户无用
            if (!inputStream.isAtEnd()) {
                inputStream.skipRawBytes(inputStream.getBytesUntilLimit());
            }
            context.state = State.END;

            // 恢复到之前的上下文和限制
            inputStream.popLimit(context.oldLimit);
            this.context = context.parent;
            poolContext(context);
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    private Context newContext() {
        Context context;
        if (this.pooledContext != null) {
            context = this.pooledContext;
            this.pooledContext = null;
        } else {
            context = new Context();
        }
        context.setParent(this.context);
        return context;
    }

    private void poolContext(Context context) {
        context.reset();
        this.pooledContext = context;
    }

    private static class Context {

        Context parent;
        int recursionDepth;
        TypeArgInfo<?> waitDecodeTypeArg;
        BinaryPojoCodec<?> codec;
        /** 对方写入的类型id，0表示未写入 -- 可用于抛出异常打印详细信息 */
        long typeId = 0;

        /** 父节点的Limit */
        int oldLimit = -1;
        /** 当前状态 */
        State state = State.NEW;

        public Context() {
        }

        void setParent(Context parent) {
            this.parent = parent;
            this.recursionDepth = parent == null ? 0 : parent.recursionDepth + 1;
        }

        void reset() {
            parent = null;
            recursionDepth = 0;
            waitDecodeTypeArg = null;
            codec = null;

            oldLimit = -1;
            typeId = 0;

            state = State.NEW;
        }
    }

    private enum State {

        /** 初始状态，逻辑中不应该出现 */
        NEW,

        /** 已确定是对象和其typeId，等待codec调用{@link BinaryReader#readStartObject(TypeArgInfo)}方法 */
        WAIT_START,

        /** 读的过程中 */
        READING,

        /** 读完毕 */
        END,

    }

}