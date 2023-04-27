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

import cn.wjybxx.common.dson.*;
import cn.wjybxx.common.dson.io.BinaryUtils;
import cn.wjybxx.common.dson.io.DsonInput;
import com.google.protobuf.Parser;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.util.List;
import java.util.Objects;

/**
 * 与{@link cn.wjybxx.common.dson.binary.DefaultDsonBinReader}的主要区别：
 * 1.name和classId由Int变为String
 * 2.没有className时，写入的是空字符串(会写入长度，但不会被编码)
 * 3.skipName不是通过读取name跳过，而是真实跳过
 *
 * <h3>字符串池化</h3>
 * {@link Dsons#enableFieldIntern}{@link Dsons#enableClassIntern}
 *
 * @author wjybxx
 * date - 2023/4/22
 */
public class DefaultDsonDocReader implements DsonDocReader {

    private final DsonInput input;
    private final int recursionLimit;

    private Context context;
    private Context pooledContext; // 一个额外的缓存，用于写集合等减少上下文创建

    // 这些值放外面，不需要上下文隔离
    private int recursionDepth;
    private DsonType currentDsonType;
    private WireType currentWireType;
    private String currentName = null;

    public DefaultDsonDocReader(DsonInput input, int recursionLimit) {
        this.input = input;
        this.recursionLimit = recursionLimit;

        this.context = new Context(null, DsonContextType.TOP_LEVEL);
    }

    @Override
    public void close() {
        input.close();
    }

    // region state

    @Nonnull
    @Override
    public DsonType getCurrentDsonType() {
        if (currentDsonType == null) {
            assert context.contextType == DsonContextType.TOP_LEVEL;
            throw invalidState(List.of(DsonReaderState.NAME, DsonReaderState.VALUE));
        }
        return currentDsonType;
    }

    @Override
    public String getCurrentName() {
        if (context.state != DsonReaderState.VALUE) {
            throw invalidState(List.of(DsonReaderState.VALUE));
        }
        return currentName;
    }

    @Nonnull
    @Override
    public DocClassId getCurrentClassId() {
        DocClassId classId = context.classId;
        if (classId == null) {
            throw DsonCodecException.contextErrorTopLevel();
        }
        return classId;
    }

    @Override
    public boolean isAtEndOfObject() {
        return input.isAtEnd();
    }

    @Override
    public boolean isAtType() {
        if (context.state == DsonReaderState.TYPE) {
            return true;
        }
        return context.contextType == DsonContextType.TOP_LEVEL
                && context.state != DsonReaderState.VALUE; // INIT or DONE
    }

    @Override
    public DsonType readDsonType() {
        Context context = this.context;
        if (context.contextType == DsonContextType.TOP_LEVEL) {
            if (context.state != DsonReaderState.INITIAL && context.state != DsonReaderState.DONE) {
                throw invalidState(List.of(DsonReaderState.INITIAL, DsonReaderState.DONE));
            }
        } else if (context.state != DsonReaderState.TYPE) {
            throw invalidState(List.of(DsonReaderState.TYPE));
        }

        final int fullType = input.isAtEnd() ? 0 : BinaryUtils.toUint8(input.readRawByte());
        DsonType dsonType = DsonType.forNumber(Dsons.dsonTypeOfFullType(fullType));
        WireType wireType = WireType.forNumber(Dsons.wireTypeOfFullType(fullType));
        this.currentDsonType = dsonType;
        this.currentWireType = wireType;
        this.currentName = null;

        // topLevel只可是容器对象
        if (context.contextType == DsonContextType.TOP_LEVEL && !dsonType.isContainer()) {
            throw DsonCodecException.invalidDsonType(context.contextType, dsonType);
        }

        if (dsonType == DsonType.END_OF_OBJECT) {
            context.setState(DsonReaderState.WAIT_END_OBJECT);
        } else {
            // name/name总是和type同时解析
            if (context.contextType == DsonContextType.OBJECT) {
                currentName = Dsons.internField(input.readString());
                context.setState(DsonReaderState.NAME);
            } else {
                context.setState(DsonReaderState.VALUE);
            }
        }
        return dsonType;
    }

    @Override
    public String readName() {
        Context context = this.context;
        if (context.state != DsonReaderState.NAME) {
            throw invalidState(List.of(DsonReaderState.NAME));
        }
        context.setState(DsonReaderState.VALUE);
        return currentName;
    }

    @Override
    public void readName(String expected) {
        String name = readName();
        if (!Objects.equals(name, expected)) {
            throw DsonCodecException.unexpectedName(expected, name);
        }
    }

    /** 前进到读值状态 */
    private void advanceToValueState(String name, @Nullable DsonType requiredType) {
        Context context = this.context;
        if (context.state == DsonReaderState.TYPE) {
            readDsonType();
        }
        if (context.state == DsonReaderState.NAME) {
            readName(name);
        }
        if (context.state != DsonReaderState.VALUE) {
            throw invalidState(List.of(DsonReaderState.VALUE));
        }
        if (requiredType != null && currentDsonType != requiredType) {
            throw DsonCodecException.dsonTypeMismatch(requiredType, currentDsonType);
        }
    }

    private void ensureValueState(Context context, DsonType requiredType) {
        if (context.state != DsonReaderState.VALUE) {
            throw invalidState(List.of(DsonReaderState.VALUE));
        }
        if (currentDsonType != requiredType) {
            throw DsonCodecException.dsonTypeMismatch(requiredType, currentDsonType);
        }
    }

    private void setNextState() {
        if (context.contextType == DsonContextType.TOP_LEVEL) {
            context.setState(DsonReaderState.DONE);
        } else {
            context.setState(DsonReaderState.TYPE);
        }
    }

    private DsonCodecException invalidState(List<DsonReaderState> expected) {
        return DsonCodecException.invalidState(context.contextType, expected, context.state);
    }
    // endregion

    // region 简单值
    @Override
    public int readInt32(String name) {
        advanceToValueState(name, DsonType.INT32);
        int value = currentWireType.readInt32(input);
        setNextState();
        return value;
    }

    @Override
    public long readInt64(String name) {
        advanceToValueState(name, DsonType.INT64);
        long value = currentWireType.readInt64(input);
        setNextState();
        return value;
    }

    @Override
    public float readFloat(String name) {
        advanceToValueState(name, DsonType.FLOAT);
        float value = input.readFloat();
        setNextState();
        return value;
    }

    @Override
    public double readDouble(String name) {
        advanceToValueState(name, DsonType.DOUBLE);
        double value = input.readDouble();
        setNextState();
        return value;
    }

    @Override
    public boolean readBoolean(String name) {
        advanceToValueState(name, DsonType.BOOLEAN);
        boolean value = input.readBool();
        setNextState();
        return value;
    }

    @Override
    public String readString(String name) {
        advanceToValueState(name, DsonType.STRING);
        String value = input.readString();
        setNextState();
        return value;
    }

    @Override
    public void readNull(String name) {
        advanceToValueState(name, DsonType.NULL);
        setNextState();
    }

    @Override
    public DsonBinary readBinary(String name) {
        advanceToValueState(name, DsonType.BINARY);
        int size = input.readFixed32();
        DsonBinary value = new DsonBinary(
                input.readRawByte(),
                input.readRawBytes(size - 1));
        setNextState();
        return value;
    }

    @Override
    public DsonExtString readExtString(String name) {
        advanceToValueState(name, DsonType.EXT_STRING);
        DsonExtString value = new DsonExtString(
                input.readRawByte(),
                input.readString());
        setNextState();
        return value;
    }

    @Override
    public DsonExtInt32 readExtInt32(String name) {
        advanceToValueState(name, DsonType.EXT_INT32);
        DsonExtInt32 value = new DsonExtInt32(
                input.readRawByte(),
                currentWireType.readInt32(input));
        setNextState();
        return value;
    }

    @Override
    public DsonExtInt64 readExtInt64(String name) {
        advanceToValueState(name, DsonType.EXT_INT64);
        DsonExtInt64 value = new DsonExtInt64(
                input.readRawByte(),
                currentWireType.readInt64(input));
        setNextState();
        return value;
    }

    // endregion

    // region 容器
    @Nonnull
    @Override
    public DocClassId readStartArray() {
        Context context = this.context;
        if (context.state == DsonReaderState.WAIT_START_OBJECT) {
            setNextState();
            return context.classId;
        }
        if (recursionDepth >= recursionLimit) {
            throw DsonCodecException.recursionLimitExceeded();
        }
        autoStartTopLevel(context);
        ensureValueState(context, DsonType.ARRAY);
        return doReadStartContainer(context, DsonContextType.ARRAY);
    }

    @Override
    public void readEndArray() {
        Context context = this.context;
        if (context.contextType != DsonContextType.ARRAY) {
            throw DsonCodecException.contextError(DsonContextType.ARRAY, context.contextType);
        }
        if (context.state != DsonReaderState.WAIT_END_OBJECT) {
            throw invalidState(List.of(DsonReaderState.WAIT_END_OBJECT));
        }
        doReadEndContainer(context);
    }

    @Nonnull
    @Override
    public DocClassId readStartObject() {
        Context context = this.context;
        if (context.state == DsonReaderState.WAIT_START_OBJECT) {
            setNextState();
            return context.classId;
        }
        if (recursionDepth >= recursionLimit) {
            throw DsonCodecException.recursionLimitExceeded();
        }
        autoStartTopLevel(context);
        ensureValueState(context, DsonType.OBJECT);
        return doReadStartContainer(context, DsonContextType.OBJECT);
    }

    @Override
    public void readEndObject() {
        Context context = this.context;
        if (context.contextType != DsonContextType.OBJECT) {
            throw DsonCodecException.contextError(DsonContextType.OBJECT, context.contextType);
        }
        if (context.state != DsonReaderState.WAIT_END_OBJECT) {
            throw invalidState(List.of(DsonReaderState.WAIT_END_OBJECT));
        }
        doReadEndContainer(context);
    }

    @Override
    public DocClassId prestartArray() {
        DocClassId classId = readStartArray();
        context.setState(DsonReaderState.WAIT_START_OBJECT);
        return classId;
    }

    @Override
    public DocClassId prestartObject() {
        DocClassId classId = readStartObject();
        context.setState(DsonReaderState.WAIT_START_OBJECT);
        return classId;
    }

    private void autoStartTopLevel(Context context) {
        if (context.contextType == DsonContextType.TOP_LEVEL
                && (context.state == DsonReaderState.INITIAL || context.state == DsonReaderState.DONE)) {
            readDsonType();
        }
    }

    private DocClassId doReadStartContainer(Context context, DsonContextType contextType) {
        Context newContext = newContext(context, contextType);
        int length = input.readFixed32();
        newContext.oldLimit = input.pushLimit(length); // length包含classId
        newContext.classId = readClassId();
        newContext.name = currentName;

        this.context = newContext;
        this.recursionDepth++;
        setNextState(); // 设置初始状态
        return newContext.classId;
    }

    private DocClassId readClassId() {
        String classId = Dsons.internClass(input.readString());
        return DocClassId.of(classId);
    }

    private void doReadEndContainer(Context context) {
        if (!input.isAtEnd()) {
            throw DsonCodecException.bytesRemain(input.getBytesUntilLimit());
        }
        input.popLimit(context.oldLimit);

        this.context = context.parent;
        this.recursionDepth--;
        // 恢复上下文
        this.currentDsonType = context.contextType == DsonContextType.ARRAY ? DsonType.ARRAY : DsonType.OBJECT;
        this.currentWireType = WireType.VARINT;
        this.currentName = context.name;
        setNextState(); // parent前进一个状态

        poolContext(context);
    }
    // endregion

    // region sp

    @Override
    public void skipName() {
        if (context.state == DsonReaderState.VALUE) {
            return;
        }
        if (context.state != DsonReaderState.NAME) {
            throw invalidState(List.of(DsonReaderState.VALUE, DsonReaderState.NAME));
        }
        // 避免构建字符串
        int size = input.readUint32();
        if (size > 0) {
            input.skipRawBytes(size);
        }
        context.setState(DsonReaderState.VALUE);
    }

    @Override
    public void skipValue() {
        if (context.state != DsonReaderState.VALUE) {
            throw invalidState(List.of(DsonReaderState.VALUE));
        }
        doSkipValue();
        setNextState();
    }

    private void doSkipValue() {
        int skip;
        switch (currentDsonType) {
            case FLOAT -> skip = 4;
            case DOUBLE -> skip = 8;
            case BOOLEAN -> skip = 1;
            case NULL -> skip = 0;

            case INT32 -> {
                currentWireType.readInt32(input);
                return;
            }
            case INT64 -> {
                currentWireType.readInt64(input);
                return;
            }
            case STRING -> {
                skip = input.readUint32();
            }
            case EXT_INT32 -> {
                input.readRawByte();
                currentWireType.readInt32(input);
                return;
            }
            case EXT_INT64 -> {
                input.readRawByte();
                currentWireType.readInt64(input);
                return;
            }
            case EXT_STRING -> {
                input.readRawByte();
                skip = input.readUint32();
            }

            case BINARY, ARRAY, OBJECT -> {
                skip = input.readFixed32();
            }
            default -> throw DsonCodecException.invalidDsonType(context.contextType, currentDsonType);
        }
        if (skip > 0) {
            input.skipRawBytes(skip);
        }
    }

    @Override
    public void skipToEndOfObject() {
        if (context.contextType == DsonContextType.TOP_LEVEL) {
            throw DsonCodecException.contextErrorTopLevel();
        }
        if (context.state == DsonReaderState.WAIT_START_OBJECT) {
            throw invalidState(List.of(DsonReaderState.TYPE, DsonReaderState.NAME, DsonReaderState.VALUE));
        }
        if (currentDsonType == DsonType.END_OF_OBJECT) {
            assert context.state == DsonReaderState.WAIT_END_OBJECT;
            return;
        }
        int size = input.getBytesUntilLimit();
        if (size > 0) {
            input.skipRawBytes(size);
        }
        setNextState();
        readDsonType(); // end of object
    }

    @Override
    public DsonValueSummary peekValueSummary() {
        if (context.state != DsonReaderState.VALUE) {
            throw invalidState(List.of(DsonReaderState.VALUE));
        }
        int prePosition = input.position();
        DsonType dsonType = currentDsonType;
        DsonValueSummary summary;
        switch (dsonType) {
            case STRING -> {
                summary = new DsonValueSummary(dsonType, input.readUint32(), (byte) 0, null);
            }
            case EXT_INT32, EXT_INT64 -> {
                byte subType = input.readRawByte();
                summary = new DsonValueSummary(dsonType, 0, subType, null);
            }
            case EXT_STRING -> {
                byte subType = input.readRawByte();
                summary = new DsonValueSummary(dsonType, input.readUint32(), subType, null);
            }
            case BINARY -> {
                int length = input.readFixed32();
                byte subType = input.readRawByte();
                summary = new DsonValueSummary(dsonType, length, subType, null);
            }
            case ARRAY, OBJECT -> {
                int length = input.readFixed32();
                DocClassId classId = readClassId();
                summary = new DsonValueSummary(dsonType, length, (byte) 0, classId);
            }
            default -> {
                summary = new DsonValueSummary(dsonType, 0, (byte) 0, null);
            }
        }
        input.setPosition(prePosition);
        return summary;
    }

    @Override
    public <T> T readMessage(String name, @Nonnull Parser<T> parser) {
        Objects.requireNonNull(parser, "parser");
        advanceToValueState(name, DsonType.BINARY);
        T value = doReadMessage(parser);
        setNextState();
        return value;
    }

    private <T> T doReadMessage(Parser<T> parser) {
        DsonInput input = this.input;
        int size = input.readFixed32();
        int oldLimit = input.pushLimit(size);
        byte subType = input.readRawByte();
        checkSubType(DsonBinaryType.PROTOBUF_MESSAGE.getValue(), subType);
        T value = input.readMessageNoSize(parser);
        input.popLimit(oldLimit);
        return value;
    }

    private static void checkSubType(byte expected, byte subType) {
        if (subType != expected) {
            throw DsonCodecException.unexpectedSubType(expected, subType);
        }
    }

    @Override
    public byte[] readValueAsBytes(String name) {
        advanceToValueState(name, null);
        if (!Dsons.VALUE_BYTES_TYPES.contains(currentDsonType)) {
            throw DsonCodecException.invalidDsonType(Dsons.VALUE_BYTES_TYPES, currentDsonType);
        }
        byte[] data = doReadValueAsBytes();
        setNextState();
        return data;
    }

    private byte[] doReadValueAsBytes() {
        int size;
        if (currentDsonType == DsonType.STRING) {
            size = input.readUint32();
        } else {
            size = input.readFixed32();
        }
        return input.readRawBytes(size);
    }

    @Override
    public void attachContext(Object value) {
        context.attach = value;
    }

    @Override
    public Object attachContext() {
        return context.attach;
    }

    @Override
    public boolean isArrayContext() {
        return context.contextType == DsonContextType.ARRAY;
    }

    @Override
    public boolean isObjectContext() {
        return context.contextType == DsonContextType.OBJECT;
    }

    @Override
    public DsonReaderGuide whatShouldIDo() {
        Context context = this.context;
        if (context.contextType == DsonContextType.TOP_LEVEL) {
            if (input.isAtEnd()) {
                return DsonReaderGuide.CLOSE;
            }
            if (context.state == DsonReaderState.VALUE) {
                return DsonReaderGuide.READ_VALUE;
            }
            return DsonReaderGuide.READ_TYPE;
        } else {
            return switch (context.state) {
                case TYPE -> DsonReaderGuide.READ_TYPE;
                case VALUE -> DsonReaderGuide.READ_VALUE;
                case NAME -> DsonReaderGuide.READ_NAME;
                case WAIT_START_OBJECT ->
                        context.contextType == DsonContextType.ARRAY ? DsonReaderGuide.START_ARRAY : DsonReaderGuide.START_OBJECT;
                case WAIT_END_OBJECT ->
                        context.contextType == DsonContextType.ARRAY ? DsonReaderGuide.END_ARRAY : DsonReaderGuide.END_OBJECT;
                case INITIAL, DONE -> throw new AssertionError("invalid state " + context.state);
            };
        }
    }

    // endregion

    // region context

    private Context newContext(Context parent, DsonContextType contextType) {
        Context context;
        if (this.pooledContext != null) {
            context = this.pooledContext;
            this.pooledContext = null;
        } else {
            context = new Context();
        }
        context.init(parent, contextType);
        return context;
    }

    private void poolContext(Context context) {
        context.reset();
        this.pooledContext = context;
    }

    private static class Context {

        Context parent;
        DsonContextType contextType;
        DsonReaderState state = DsonReaderState.INITIAL;
        Object attach;

        int oldLimit = -1;
        String name;
        DocClassId classId;

        public Context() {
        }

        public Context(Context parent, DsonContextType contextType) {
            this.parent = parent;
            this.contextType = contextType;
        }

        void init(Context parent, DsonContextType contextType) {
            this.parent = parent;
            this.contextType = contextType;
        }

        void setState(DsonReaderState state) {
            this.state = state;
        }

        void reset() {
            parent = null;
            contextType = null;
            state = DsonReaderState.INITIAL;
            attach = null;

            oldLimit = -1;
            name = null;
            classId = null;
        }
    }

    // endregion

}