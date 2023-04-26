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
import cn.wjybxx.common.dson.io.Chunk;
import cn.wjybxx.common.dson.io.DsonOutput;
import com.google.protobuf.MessageLite;

import javax.annotation.Nullable;
import java.util.List;

/**
 * @author wjybxx
 * date - 2023/4/21
 */
public class DefaultDsonDocWriter implements DsonDocWriter {

    private final DsonOutput output;
    private final int recursionLimit;

    private Context context;
    private Context pooledContext; // 一个额外的缓存，用于写集合等减少上下文创建

    private int recursionDepth;

    public DefaultDsonDocWriter(DsonOutput output, int recursionLimit) {
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

    private void writeFullType(DsonType dsonType, @Nullable WireType wireType) {
        if (wireType == null) {
            output.writeRawByte((byte) Dsons.makeFullType(dsonType.getNumber(), 0));
        } else {
            output.writeRawByte((byte) Dsons.makeFullType(dsonType.getNumber(), wireType.getNumber()));
        }
    }

    private void writeClassId(@Nullable DocClassId classId) {
        if (classId == null || classId.isObjectClassId()) {
            output.writeString("");
        } else {
            output.writeString(classId.getValue());
        }
    }

    // region state
    private void writeCurrentNumber(Context context) {
        if (context.contextType == DsonContextType.OBJECT) {
            output.writeString(context.name);
        }
    }

    @Override
    public void writeName(String name) {
        Context context = this.context;
        if (context.state != DsonWriterState.NAME) {
            throw invalidState(List.of(DsonWriterState.NAME), context.state);
        }
        context.name = name;
        context.state = DsonWriterState.VALUE;
    }

    private void advanceToValueState(String name, DsonType dsonType, WireType wireType) {
        Context context = this.context;
        if (context.state == DsonWriterState.NAME) {
            writeName(name);
        }
        if (context.state != DsonWriterState.VALUE) {
            throw invalidState(List.of(DsonWriterState.VALUE), context.state);
        }
        writeFullType(dsonType, wireType);
        writeCurrentNumber(context);
    }

    private void ensureValueState(Context context, DsonType dsonType) {
        if (context.state != DsonWriterState.VALUE) {
            throw invalidState(List.of(DsonWriterState.VALUE), context.state);
        }
        writeFullType(dsonType, null);
        writeCurrentNumber(context);
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
    public void writeInt32(String name, int value, WireType wireType) {
        advanceToValueState(name, DsonType.INT32, wireType);
        wireType.writeInt32(output, value);
        setNextState();
    }

    @Override
    public void writeInt64(String name, long value, WireType wireType) {
        advanceToValueState(name, DsonType.INT64, wireType);
        wireType.writeInt64(output, value);
        setNextState();
    }

    @Override
    public void writeFloat(String name, float value) {
        advanceToValueState(name, DsonType.FLOAT, null);
        output.writeFloat(value);
        setNextState();
    }

    @Override
    public void writeDouble(String name, double value) {
        advanceToValueState(name, DsonType.DOUBLE, null);
        output.writeDouble(value);
        setNextState();
    }

    @Override
    public void writeBoolean(String name, boolean value) {
        advanceToValueState(name, DsonType.BOOLEAN, null);
        output.writeBool(value);
        setNextState();
    }

    @Override
    public void writeString(String name, String value) {
        advanceToValueState(name, DsonType.STRING, null);
        output.writeString(value);
        setNextState();
    }

    @Override
    public void writeNull(String name) {
        advanceToValueState(name, DsonType.NULL, null);
        setNextState();
    }

    @Override
    public void writeBinary(String name, DsonBinary dsonBinary) {
        writeBinary(name, dsonBinary.getType(), dsonBinary.getData());
    }

    @Override
    public void writeBinary(String name, byte type, byte[] data) {
        advanceToValueState(name, DsonType.BINARY, null);
        {
            output.writeFixed32(1 + data.length);
            output.writeRawByte(type);
            output.writeRawBytes(data);
        }
        setNextState();
    }

    @Override
    public void writeBinary(String name, byte type, Chunk chunk) {
        advanceToValueState(name, DsonType.BINARY, null);
        {
            output.writeFixed32(1 + chunk.getLength());
            output.writeRawByte(type);
            output.writeRawBytes(chunk.getBuffer(), chunk.getOffset(), chunk.getLength());
        }
        setNextState();
    }

    @Override
    public void writeExtString(String name, DsonExtString value) {
        writeExtString(name, value.getType(), value.getValue());
    }

    @Override
    public void writeExtString(String name, byte type, String value) {
        advanceToValueState(name, DsonType.EXT_STRING, null);
        output.writeRawByte(type);
        output.writeString(value);
        setNextState();
    }

    @Override
    public void writeExtInt32(String name, DsonExtInt32 value, WireType wireType) {
        writeExtInt32(name, value.getType(), value.getValue(), wireType);
    }

    @Override
    public void writeExtInt32(String name, byte type, int value, WireType wireType) {
        advanceToValueState(name, DsonType.EXT_INT32, wireType);
        output.writeRawByte(type);
        wireType.writeInt32(output, value);
        setNextState();
    }

    @Override
    public void writeExtInt64(String name, DsonExtInt64 value, WireType wireType) {
        writeExtInt64(name, value.getType(), value.getValue(), wireType);
    }

    @Override
    public void writeExtInt64(String name, byte type, long value, WireType wireType) {
        advanceToValueState(name, DsonType.EXT_INT64, wireType);
        output.writeRawByte(type);
        wireType.writeInt64(output, value);
        setNextState();
    }

    // endregion

    // region 容器
    @Override
    public void writeStartArray(DocClassId classId) {
        if (recursionDepth >= recursionLimit) {
            throw DsonCodecException.recursionLimitExceeded();
        }
        Context context = this.context;
        autoStartTopLevel(context);
        ensureValueState(context, DsonType.ARRAY);
        doWriteStartContainer(context, DsonContextType.ARRAY, classId);
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
    }

    @Override
    public void writeStartObject(DocClassId classId) {
        if (recursionDepth >= recursionLimit) {
            throw DsonCodecException.recursionLimitExceeded();
        }
        Context context = this.context;
        autoStartTopLevel(context);
        ensureValueState(context, DsonType.OBJECT);
        doWriteStartContainer(context, DsonContextType.OBJECT, classId);
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
    }

    private void autoStartTopLevel(Context context) {
        if (context.contextType == DsonContextType.TOP_LEVEL
                && (context.state == DsonWriterState.INITIAL || context.state == DsonWriterState.DONE)) {
            context.setState(DsonWriterState.VALUE);
        }
    }

    private void doWriteStartContainer(Context parent, DsonContextType contextType, DocClassId classId) {
        Context newContext = newContext(parent, contextType);
        newContext.preWritten = output.position();
        this.context = newContext;
        this.recursionDepth++;

        output.writeFixed32(0);
        writeClassId(classId);
        setNextState(); // 设置初始状态
    }

    private void doWriteEndContainer(Context context) {
        // 记录preWritten在写length之前，最后的size要减4
        int preWritten = context.preWritten;
        output.setFixedInt32(preWritten, output.position() - preWritten - 4);

        this.recursionDepth--;
        this.context = context.parent;
        poolContext(context);
        setNextState(); // parent前进一个状态
    }
    // endregion

    // region sp

    @Override
    public void writeMessage(String name, MessageLite messageLite) {
        advanceToValueState(name, DsonType.BINARY, null);
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
    public void writeValueBytes(String name, DsonType type, byte[] data) {
        if (!Dsons.VALUE_BYTES_TYPES.contains(type)) {
            throw DsonCodecException.invalidDsonType(Dsons.VALUE_BYTES_TYPES, type);
        }
        advanceToValueState(name, type, null);
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
        DsonWriterState state = DsonWriterState.INITIAL;
        Object attach;

        int preWritten = 0;
        /** 需要先缓存下来，因为name和value可能分开写入，而name必须写在type后面 */
        String name;

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
            name = null;
        }
    }

    // endregion

}