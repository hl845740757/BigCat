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

package cn.wjybxx.common.dson.binary;

import cn.wjybxx.common.Preconditions;
import cn.wjybxx.common.dson.*;
import cn.wjybxx.common.dson.io.Chunk;
import cn.wjybxx.common.dson.io.DsonOutput;
import com.google.protobuf.MessageLite;

import javax.annotation.Nullable;
import java.util.List;

/**
 * @author wjybxx
 * date - 2023/4/21
 */
public class DefaultDsonBinWriter implements DsonBinWriter {

    private final DsonOutput output;
    private final int recursionLimit;

    private Context context;
    private Context pooledContext; // 一个额外的缓存，用于写集合等减少上下文创建

    private int recursionDepth;

    public DefaultDsonBinWriter(DsonOutput output, int recursionLimit) {
        this.output = output;
        this.recursionLimit = recursionLimit;
        this.context = new Context(null, DsonContextType.TOP_LEVEL);
    }

    @Override
    public void flush() {
        output.flush();
    }

    @Override
    public void close() {
        output.close();
    }

    // region state

    @Override
    public boolean isAtName() {
        return context.state == DsonWriterState.NAME;
    }

    @Override
    public void writeName(int name) {
        Preconditions.checkNonNegative(name, "name");
        Context context = this.context;
        if (context.state != DsonWriterState.NAME) {
            throw invalidState(List.of(DsonWriterState.NAME), context.state);
        }
        context.name = name;
        context.state = DsonWriterState.VALUE;
    }

    private void advanceToValueState(int name) {
        Context context = this.context;
        if (context.state == DsonWriterState.NAME) {
            writeName(name);
        }
        if (context.state != DsonWriterState.VALUE) {
            throw invalidState(List.of(DsonWriterState.VALUE), context.state);
        }
    }

    private void writeFullTypeAndCurrentName(DsonOutput output, DsonType dsonType, WireType wireType) {
        if (wireType == null) {
            output.writeRawByte((byte) Dsons.makeFullType(dsonType.getNumber(), 0));
        } else {
            output.writeRawByte((byte) Dsons.makeFullType(dsonType.getNumber(), wireType.getNumber()));
        }
        if (context.contextType == DsonContextType.OBJECT) {
            output.writeUint32(context.name);
        }
    }

    private void ensureValueState(Context context) {
        if (context.state != DsonWriterState.VALUE) {
            throw invalidState(List.of(DsonWriterState.VALUE), context.state);
        }
    }

    private void setNextState() {
        switch (context.contextType) {
            case TOP_LEVEL -> context.setState(DsonWriterState.DONE);
            case OBJECT -> context.setState(DsonWriterState.NAME);
            case ARRAY -> context.setState(DsonWriterState.VALUE);
        }
    }

    private DsonCodecException invalidState(List<DsonWriterState> expected, DsonWriterState state) {
        return DsonCodecException.invalidState(context.contextType, expected, state);
    }
    // endregion

    // region 简单值
    @Override
    public void writeInt32(int name, int value, WireType wireType) {
        DsonOutput output = this.output;
        advanceToValueState(name);
        writeFullTypeAndCurrentName(output, DsonType.INT32, wireType);
        wireType.writeInt32(output, value);
        setNextState();
    }

    @Override
    public void writeInt64(int name, long value, WireType wireType) {
        DsonOutput output = this.output;
        advanceToValueState(name);
        writeFullTypeAndCurrentName(output, DsonType.INT64, wireType);
        wireType.writeInt64(output, value);
        setNextState();
    }

    @Override
    public void writeFloat(int name, float value) {
        DsonOutput output = this.output;
        advanceToValueState(name);
        writeFullTypeAndCurrentName(output, DsonType.FLOAT, null);
        output.writeFloat(value);
        setNextState();
    }

    @Override
    public void writeDouble(int name, double value) {
        DsonOutput output = this.output;
        advanceToValueState(name);
        writeFullTypeAndCurrentName(output, DsonType.DOUBLE, null);
        output.writeDouble(value);
        setNextState();
    }

    @Override
    public void writeBoolean(int name, boolean value) {
        DsonOutput output = this.output;
        advanceToValueState(name);
        writeFullTypeAndCurrentName(output, DsonType.BOOLEAN, null);
        output.writeBool(value);
        setNextState();
    }

    @Override
    public void writeString(int name, String value) {
        DsonOutput output = this.output;
        advanceToValueState(name);
        writeFullTypeAndCurrentName(output, DsonType.STRING, null);
        output.writeString(value);
        setNextState();
    }

    @Override
    public void writeNull(int name) {
        DsonOutput output = this.output;
        advanceToValueState(name);
        writeFullTypeAndCurrentName(output, DsonType.NULL, null);
        setNextState();
    }

    @Override
    public void writeBinary(int name, DsonBinary dsonBinary) {
        writeBinary(name, dsonBinary.getType(), dsonBinary.getData());
    }

    @Override
    public void writeBinary(int name, byte type, byte[] data) {
        DsonOutput output = this.output;
        advanceToValueState(name);
        writeFullTypeAndCurrentName(output, DsonType.BINARY, null);
        {
            output.writeFixed32(1 + data.length);
            output.writeRawByte(type);
            output.writeRawBytes(data);
        }
        setNextState();
    }

    @Override
    public void writeBinary(int name, byte type, Chunk chunk) {
        DsonOutput output = this.output;
        advanceToValueState(name);
        writeFullTypeAndCurrentName(output, DsonType.BINARY, null);
        {
            output.writeFixed32(1 + chunk.getLength());
            output.writeRawByte(type);
            output.writeRawBytes(chunk.getBuffer(), chunk.getOffset(), chunk.getLength());
        }
        setNextState();
    }

    @Override
    public void writeExtString(int name, DsonExtString value) {
        writeExtString(name, value.getType(), value.getValue());
    }

    @Override
    public void writeExtString(int name, byte type, String value) {
        DsonOutput output = this.output;
        advanceToValueState(name);
        writeFullTypeAndCurrentName(output, DsonType.EXT_STRING, null);
        output.writeRawByte(type);
        output.writeString(value);
        setNextState();
    }

    @Override
    public void writeExtInt32(int name, DsonExtInt32 value, WireType wireType) {
        writeExtInt32(name, value.getType(), value.getValue(), wireType);
    }

    @Override
    public void writeExtInt32(int name, byte type, int value, WireType wireType) {
        DsonOutput output = this.output;
        advanceToValueState(name);
        writeFullTypeAndCurrentName(output, DsonType.EXT_INT32, wireType);
        output.writeRawByte(type);
        wireType.writeInt32(output, value);
        setNextState();
    }

    @Override
    public void writeExtInt64(int name, DsonExtInt64 value, WireType wireType) {
        writeExtInt64(name, value.getType(), value.getValue(), wireType);
    }

    @Override
    public void writeExtInt64(int name, byte type, long value, WireType wireType) {
        DsonOutput output = this.output;
        advanceToValueState(name);
        writeFullTypeAndCurrentName(output, DsonType.EXT_INT64, wireType);
        output.writeRawByte(type);
        wireType.writeInt64(output, value);
        setNextState();
    }

    // endregion

    // region 容器
    @Override
    public void writeStartArray(BinClassId classId) {
        if (recursionDepth >= recursionLimit) {
            throw DsonCodecException.recursionLimitExceeded();
        }
        Context context = this.context;
        autoStartTopLevel(context);
        ensureValueState(context);
        doWriteStartContainer(context, DsonContextType.ARRAY, classId);
        setNextState(); // 设置新上下文状态
    }

    @Override
    public void writeEndArray() {
        Context context = this.context;
        if (context.contextType != DsonContextType.ARRAY) {
            throw DsonCodecException.contextError(DsonContextType.ARRAY, context.contextType);
        }
        if (context.state != DsonWriterState.VALUE) {
            throw invalidState(List.of(DsonWriterState.VALUE), context.state);
        }
        doWriteEndContainer(context);
        setNextState(); // parent前进一个状态
    }

    @Override
    public void writeStartObject(BinClassId classId) {
        if (recursionDepth >= recursionLimit) {
            throw DsonCodecException.recursionLimitExceeded();
        }
        Context context = this.context;
        autoStartTopLevel(context);
        ensureValueState(context);
        doWriteStartContainer(context, DsonContextType.OBJECT, classId);
        setNextState(); // 设置新上下文状态
    }

    @Override
    public void writeEndObject() {
        Context context = this.context;
        if (context.contextType != DsonContextType.OBJECT) {
            throw DsonCodecException.contextError(DsonContextType.OBJECT, context.contextType);
        }
        if (context.state != DsonWriterState.NAME) { // 下一个循环的开始
            throw invalidState(List.of(DsonWriterState.NAME), context.state);
        }
        doWriteEndContainer(context);
        setNextState(); // parent前进一个状态
    }

    private void autoStartTopLevel(Context context) {
        if (context.contextType == DsonContextType.TOP_LEVEL
                && (context.state == DsonWriterState.INITIAL || context.state == DsonWriterState.DONE)) {
            context.setState(DsonWriterState.VALUE);
        }
    }

    private void doWriteStartContainer(Context parent, DsonContextType contextType, BinClassId classId) {
        DsonOutput output = this.output;
        DsonType dsonType = contextType == DsonContextType.ARRAY ? DsonType.ARRAY : DsonType.OBJECT;
        writeFullTypeAndCurrentName(output, dsonType, null);

        Context newContext = newContext(parent, contextType);
        newContext.preWritten = output.position();
        output.writeFixed32(0);
        writeClassId(classId);

        this.context = newContext;
        this.recursionDepth++;
    }

    private void doWriteEndContainer(Context context) {
        // 记录preWritten在写length之前，最后的size要减4
        int preWritten = context.preWritten;
        output.setFixedInt32(preWritten, output.position() - preWritten - 4);

        this.recursionDepth--;
        this.context = context.parent;
        poolContext(context);
    }

    private void writeClassId(@Nullable BinClassId classId) {
        if (classId == null || classId.isObjectClassId()) {
            output.writeRawByte(BinClassId.INVALID_NAMESPACE);
        } else {
            output.writeRawByte(classId.getNamespace());
            output.writeFixed32(classId.getLclassId());
        }
    }
    // endregion

    // region sp

    @Override
    public void writeMessage(int name, MessageLite messageLite) {
        advanceToValueState(name);
        writeFullTypeAndCurrentName(output, DsonType.BINARY, null);
        {
            DsonOutput output = this.output;
            int preWritten = output.position();
            output.writeFixed32(0);
            output.writeRawByte(DsonBinaryType.PROTOBUF_MESSAGE.getValue());
            output.writeMessageNoSize(messageLite);
            output.setFixedInt32(preWritten, output.position() - preWritten - 4);
        }
        setNextState();
    }

    @Override
    public void writeValueBytes(int name, DsonType type, byte[] data) {
        if (!Dsons.VALUE_BYTES_TYPES.contains(type)) {
            throw DsonCodecException.invalidDsonType(Dsons.VALUE_BYTES_TYPES, type);
        }
        advanceToValueState(name);
        writeFullTypeAndCurrentName(output, type, null);
        {
            if (type == DsonType.STRING) {
                output.writeUint32(data.length);
            } else {
                output.writeFixed32(data.length);
            }
            output.writeRawBytes(data);
        }
        setNextState();
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

    // endregion

    // region context

    private Context newContext(Context parent, DsonContextType contextType) {
        Context context = this.pooledContext;
        if (context != null) {
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
        DsonWriterState state = DsonWriterState.INITIAL;
        Object attach;

        int preWritten = 0;
        /** 需要先缓存下来，因为name和value可能分开写入，而name必须写在type后面 */
        int name;

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

        void setState(DsonWriterState state) {
            this.state = state;
        }

        void reset() {
            parent = null;
            contextType = null;
            state = DsonWriterState.INITIAL;
            attach = null;

            preWritten = 0;
            name = 0;
        }
    }

    // endregion

}