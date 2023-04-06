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
import cn.wjybxx.bigcat.common.codec.binary.BinaryWriter;
import cn.wjybxx.bigcat.common.codec.binary.TypeId;
import com.google.protobuf.MessageLite;
import org.apache.commons.lang3.exception.ExceptionUtils;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.io.IOException;
import java.util.Objects;

/**
 * @author wjybxx
 * date 2023/4/2
 */
public class ProtobufWriter implements BinaryWriter {

    private final ProtobufConverter converter;
    private final CodedDataOutputStream outputStream;

    private Context context;
    private Context pooledContext; // 一个额外的缓存，用于写集合等减少上下文创建

    public ProtobufWriter(ProtobufConverter converter, CodedDataOutputStream outputStream) {
        this.converter = converter;
        this.outputStream = outputStream;
    }

    public void writeTopLevelObject(Object value, TypeArgInfo<?> typeArgInfo) {
        Objects.requireNonNull(value);
        BinaryPojoCodec<?> codec = findObjectEncoder(value, typeArgInfo);
        if (codec == null) {
            throw new ProtobufCodecException("unsupported top level object, type " + value.getClass());
        }
        try {
            writeObjectWait(value, typeArgInfo, codec);
            pooledContext = null; // help gc
        } catch (Exception e) {
            throw ProtobufCodecException.wrap(e);
        }
    }

    // region 基础api

    @Override
    public void writeInt(int value) {
        try {
            writeTag(outputStream, BinaryValueType.INT);
            outputStream.writeInt32(value);
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    @Override
    public void writeLong(long value) {
        try {
            writeTag(outputStream, BinaryValueType.LONG);
            outputStream.writeInt64(value);
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    @Override
    public void writeFloat(float value) {
        try {
            writeTag(outputStream, BinaryValueType.FLOAT);
            outputStream.writeFloat(value);
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    @Override
    public void writeDouble(double value) {
        try {
            writeTag(outputStream, BinaryValueType.DOUBLE);
            outputStream.writeDouble(value);
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    @Override
    public void writeBoolean(boolean value) {
        try {
            writeTag(outputStream, BinaryValueType.BOOLEAN);
            outputStream.writeBool(value);
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    @Override
    public void writeString(@Nullable String value) {
        if (value == null) {
            writeNull();
            return;
        }
        try {
            writeTag(outputStream, BinaryValueType.STRING);
            outputStream.writeString(value);
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    @Override
    public void writeBytes(@Nullable byte[] value) {
        if (value == null) {
            writeNull();
            return;
        }
        writeBytes(value, 0, value.length);
    }

    @Override
    public void writeBytes(@Nonnull byte[] bytes, int offset, int length) {
        try {
            CodedDataOutputStream outputStream = this.outputStream;
            writeTag(outputStream, BinaryValueType.BINARY);
            outputStream.writeFixed32(length);
            outputStream.writeRawBytes(bytes, offset, length);
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    public void writeMessage(MessageLite message) {
        try {
            outputStream.writeMessageNoSize(message);
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    public void writeNull() {
        try {
            writeTag(outputStream, BinaryValueType.NULL);
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    @Override
    public void flush() {
        try {
            outputStream.flush();
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    @Override
    public void close() {

    }

    public static void writeTag(CodedDataOutputStream codedDataOutputStream, BinaryValueType valueType) throws IOException {
        codedDataOutputStream.writeRawByte((byte) valueType.getNumber());
    }

    public static void writeNameSpace(CodedDataOutputStream codedDataOutputStream, byte nameSpace) throws IOException {
        codedDataOutputStream.writeRawByte(nameSpace);
    }

    public static void writeTypeId(CodedDataOutputStream codedDataOutputStream, long guid) throws IOException {
        codedDataOutputStream.writeRawByte(TypeId.parseNamespace(guid));
        codedDataOutputStream.writeFixed32(TypeId.parseClassId(guid));
    }

    // endregion

    // region object

    @Override
    public void writeObject(Object value, @Nonnull TypeArgInfo<?> typeArgInfo) {
        if (value == null) {
            writeNull();
            return;
        }

        // 第一梯队(基本类型会装箱，因此无需处理基本类型) -- unbox测试是否是基本类型的效率不会比这里的几次==测试快
        Class<?> type = value.getClass();
        if (type == Integer.class) {
            writeInt((Integer) value);
            return;
        }
        if (type == Long.class) {
            writeLong((Long) value);
            return;
        }
        if (type == Float.class) {
            writeFloat((Float) value);
            return;
        }
        if (type == Double.class) {
            writeDouble((Double) value);
            return;
        }
        if (type == Boolean.class) {
            writeBoolean((Boolean) value);
            return;
        }
        // 特殊类型
        if (type == String.class) {
            writeString((String) value);
            return;
        }
        if (type == byte[].class) {
            writeBytes((byte[]) value);
        }
        // 自定义编解码和集合等
        BinaryPojoCodec<?> codec = findObjectEncoder(value, typeArgInfo);
        if (codec != null) {
            writeObjectWait(value, typeArgInfo, codec);
            return;
        }

        // 第三梯队
        if (type == Short.class) {
            writeShort((Short) value);
            return;
        }
        if (type == Byte.class) {
            writeByte((Byte) value);
            return;
        }
        if (type == Character.class) {
            writeChar((Character) value);
            return;
        }
        throw ProtobufCodecException.unsupportedType(type);
    }

    @Override
    public void writeObjectBytes(@Nonnull byte[] objectBytes) {
        try {
            outputStream.writeRawBytes(objectBytes);
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }
    // endregion

    //region 写对象核心逻辑

    /**
     * 写对象的基本流程是：通过实例查找Codec，waitStart, writeStart
     * 读对象的基本流程是：通过input查找codec，waitStart, writeStart
     */
    private <T> void writeObjectWait(T value, TypeArgInfo<?> typeArgInfo, BinaryPojoCodec<?> codec) {
        @SuppressWarnings("unchecked") BinaryPojoCodec<? super T> castEncoder = (BinaryPojoCodec<? super T>) codec;

        Context context = newContext();
        context.state = State.WAIT_START;
        context.waitEncodeTypeArg = typeArgInfo;
        context.codec = codec;
        this.context = context;

        castEncoder.writeObject(value, this, typeArgInfo);
    }

    @SuppressWarnings("unchecked")
    private <T> BinaryPojoCodec<? super T> findObjectEncoder(Object value, TypeArgInfo<T> typeArgInfo) {
        Class<T> declaredType = typeArgInfo.declaredType;
        Class<?> encodeClass = EntityConverterUtils.getEncodeClass(value); // 枚举这个例外...

        // 按运行时类型写
        @SuppressWarnings("unchecked") BinaryPojoCodec<? super T> codec = (BinaryPojoCodec<? super T>) converter.codecRegistry.get(encodeClass);
        if (codec != null) {
            return codec;
        }
        // 按声明类型写 -- 传入的声明类型也可能是无关的，要进行测试
        if (declaredType.isAssignableFrom(encodeClass) && (codec = converter.codecRegistry.get(declaredType)) != null) {
            return codec;
        }
        // 查找默认编码器 -- 写的时候运行时类型重要
        codec = (BinaryPojoCodec<? super T>) BinaryConverterUtils.getDefaultEncoder(encodeClass);
        return codec;
    }

    @Override
    public void writeStartObject(@Nullable Object value, @Nonnull TypeArgInfo<?> typeArgInfo) {
        Objects.requireNonNull(typeArgInfo);
        // 上下文分两种：
        // 1.写当前对象，外部确定了其codec，codec调用writeStartObject走到这里 -- 此时context状态为waitStart，参数typeArg为当前对象的类型信息
        // 2.写嵌套对象，codec在读值的过程中直接调用writeStartObject，通常是用户自行调用 -- 此时context的状态为writing，参数typeArg为新对象的类型信息
        Context context = this.context;
        if (context.state == State.WRITING) {
            // 用户直接调用writeStartObject
            if (value == null) {
                writeNull();
                return;
            }
            if (context.recursionDepth >= converter.recursionLimit) {
                throw ProtobufCodecException.recursionLimitExceeded();
            }
            Context newContext = newContext();
            newContext.state = State.WAIT_START;
            newContext.codec = findObjectEncoder(value, typeArgInfo); // 允许codec不存在
            this.context = newContext;
            context = newContext;
        } else if (context.state == State.WAIT_START) {
            // 通过外层codec走到这 -- 判断用户是否发起了写替换（记录对象的引用固然可以更精确判断是否对象发生了变化，但这里不使用引用会更好）
            if (context.waitEncodeTypeArg != typeArgInfo) {
                context.codec = findObjectEncoder(value, typeArgInfo);
            }
            context.waitEncodeTypeArg = null;
        } else {
            throw ProtobufCodecException.contextError();
        }

        try {
            // tag + length(预填充)
            CodedDataOutputStream outputStream = this.outputStream;
            writeTag(outputStream, BinaryValueType.OBJECT);
            outputStream.writeFixed32(0);
            context.preWritten = outputStream.getTotalBytesWritten();

            final Class<?> type = EntityConverterUtils.getEncodeClass(value); // 小心枚举
            // 1.protobuf的message强制写入typeId
            // 2.codec为null表示用户知道具体类型
            final BinaryPojoCodec<?> codec = context.codec;
            if (!(value instanceof MessageLite) && (type == typeArgInfo.declaredType || codec == null)) {
                writeNameSpace(outputStream, TypeId.INVALID_NAMESPACE);
            } else {
                // 类型不同要写入typeId
                writeTypeId(outputStream, codec.getTypeId());
            }

            context.state = State.WRITING;
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    @Override
    public void writeEndObject() {
        Context context = this.context;
        try {
            // 回写size
            final int size = outputStream.getTotalBytesWritten() - context.preWritten;
            outputStream.setFixedInt32(context.preWritten - 4, size);
            context.state = State.END;

            this.context = context.parent;
            poolContext(context);
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    //endregion

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
        TypeArgInfo<?> waitEncodeTypeArg;
        BinaryPojoCodec<?> codec;

        int preWritten = -1;
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
            waitEncodeTypeArg = null;
            codec = null;

            preWritten = -1;
            state = State.NEW;
        }
    }

    private enum State {

        /** 初始状态，逻辑中不应该出现 */
        NEW,

        /** 已确定是对象和其typeId，等待codec调用{@link BinaryWriter#writeStartObject(Object, TypeArgInfo)}方法 */
        WAIT_START,

        /** 读的过程中 */
        WRITING,

        /** 读完毕 */
        END,

    }
}