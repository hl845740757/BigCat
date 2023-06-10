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

package cn.wjybxx.common.dson;

import cn.wjybxx.common.Preconditions;
import cn.wjybxx.common.dson.io.Chunk;
import cn.wjybxx.common.dson.text.ObjectStyle;
import cn.wjybxx.common.dson.text.StringStyle;
import cn.wjybxx.common.dson.types.ObjectRef;
import com.google.protobuf.MessageLite;

import java.util.List;
import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/4/28
 */
public abstract class AbstractDsonBinWriter implements DsonBinWriter {

    protected final int recursionLimit;
    private Context context;
    private Context pooledContext; // 一个额外的缓存，用于写集合等减少上下文创建

    protected int recursionDepth;

    public AbstractDsonBinWriter(int recursionLimit) {
        this.recursionLimit = recursionLimit;
    }

    public Context getContext() {
        return context;
    }

    public void setContext(Context context) {
        this.context = context;
    }

    public Context getPooledContext() {
        return pooledContext;
    }

    public void setPooledContext(Context pooledContext) {
        this.pooledContext = pooledContext;
    }

    @Override
    public void close() {
        context = null;
        pooledContext = null;
    }

    // region state

    @Override
    public DsonContextType getContextType() {
        return context.contextType;
    }

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
        doWriteName(name);
    }

    /** 执行{@link #writeName(int)}时调用 */
    protected void doWriteName(int name) {

    }

    protected void advanceToValueState(int name) {
        Context context = this.context;
        if (context.state == DsonWriterState.NAME) {
            writeName(name);
        }
        if (context.state != DsonWriterState.VALUE) {
            throw invalidState(List.of(DsonWriterState.VALUE), context.state);
        }
    }

    protected void ensureValueState(Context context) {
        if (context.state != DsonWriterState.VALUE) {
            throw invalidState(List.of(DsonWriterState.VALUE), context.state);
        }
    }

    protected void setNextState() {
        switch (context.contextType) {
            case OBJECT, HEADER -> context.setState(DsonWriterState.NAME);
            case TOP_LEVEL, ARRAY -> context.setState(DsonWriterState.VALUE);
        }
    }

    private DsonCodecException invalidState(List<DsonWriterState> expected, DsonWriterState state) {
        return DsonCodecException.invalidState(context.contextType, expected, state);
    }
    // endregion

    // region 简单值
    @Override
    public void writeInt32(int name, int value, WireType wireType, boolean stronglyTyped) {
        advanceToValueState(name);
        doWriteInt32(value, wireType, stronglyTyped);
        setNextState();
    }

    @Override
    public void writeInt64(int name, long value, WireType wireType, boolean stronglyTyped) {
        advanceToValueState(name);
        doWriteInt64(value, wireType, stronglyTyped);
        setNextState();
    }

    @Override
    public void writeFloat(int name, float value, boolean stronglyTyped) {
        advanceToValueState(name);
        doWriteFloat(value, stronglyTyped);
        setNextState();
    }

    @Override
    public void writeDouble(int name, double value) {
        advanceToValueState(name);
        doWriteDouble(value);
        setNextState();
    }

    @Override
    public void writeBoolean(int name, boolean value) {
        advanceToValueState(name);
        doWriteBool(value);
        setNextState();
    }

    @Override
    public void writeString(int name, String value, StringStyle style) {
        Objects.requireNonNull(value);
        advanceToValueState(name);
        doWriteString(value, style);
        setNextState();
    }

    @Override
    public void writeNull(int name) {
        advanceToValueState(name);
        doWriteNull();
        setNextState();
    }

    @Override
    public void writeBinary(int name, DsonBinary dsonBinary) {
        Objects.requireNonNull(dsonBinary);
        advanceToValueState(name);
        doWriteBinary(dsonBinary);
        setNextState();
    }

    @Override
    public void writeBinary(int name, int type, Chunk chunk) {
        Objects.requireNonNull(chunk);
        advanceToValueState(name);
        doWriteBinary(type, chunk);
        setNextState();
    }

    @Override
    public void writeExtInt32(int name, DsonExtInt32 value, WireType wireType) {
        Objects.requireNonNull(value);
        advanceToValueState(name);
        doWriteExtInt32(value, wireType);
        setNextState();
    }

    @Override
    public void writeExtInt64(int name, DsonExtInt64 value, WireType wireType) {
        Objects.requireNonNull(value);
        advanceToValueState(name);
        doWriteExtInt64(value, wireType);
        setNextState();
    }

    @Override
    public void writeExtString(int name, DsonExtString value, StringStyle style) {
        Objects.requireNonNull(value);
        advanceToValueState(name);
        doWriteExtString(value, style);
        setNextState();
    }

    @Override
    public void writeRef(int name, ObjectRef objectRef) {
        Objects.requireNonNull(objectRef);
        advanceToValueState(name);
        doWriteRef(objectRef);
        setNextState();
    }

    protected abstract void doWriteInt32(int value, WireType wireType, boolean stronglyTyped);

    protected abstract void doWriteInt64(long value, WireType wireType, boolean stronglyTyped);

    protected abstract void doWriteFloat(float value, boolean stronglyTyped);

    protected abstract void doWriteDouble(double value);

    protected abstract void doWriteBool(boolean value);

    protected abstract void doWriteString(String value, StringStyle style);

    protected abstract void doWriteNull();

    protected abstract void doWriteBinary(DsonBinary binary);

    protected abstract void doWriteBinary(int type, Chunk chunk);

    protected abstract void doWriteExtInt32(DsonExtInt32 value, WireType wireType);

    protected abstract void doWriteExtInt64(DsonExtInt64 value, WireType wireType);

    protected abstract void doWriteExtString(DsonExtString value, StringStyle style);

    protected abstract void doWriteRef(ObjectRef objectRef);

    // endregion

    // region 容器
    @Override
    public void writeStartArray(ObjectStyle style) {
        writeStartContainer(DsonContextType.ARRAY, style);
    }

    @Override
    public void writeEndArray() {
        writeEndContainer(DsonContextType.ARRAY, DsonWriterState.VALUE);
    }

    @Override
    public void writeStartObject(ObjectStyle style) {
        writeStartContainer(DsonContextType.OBJECT, style);
    }

    @Override
    public void writeEndObject() {
        writeEndContainer(DsonContextType.OBJECT, DsonWriterState.NAME);
    }

    @Override
    public void writeStartHeader(ObjectStyle style) {
        writeStartContainer(DsonContextType.HEADER, style);
    }

    @Override
    public void writeEndHeader() {
        writeEndContainer(DsonContextType.HEADER, DsonWriterState.NAME);
    }

    private void writeStartContainer(DsonContextType contextType, ObjectStyle style) {
        if (recursionDepth >= recursionLimit) {
            throw DsonCodecException.recursionLimitExceeded();
        }
        Context context = this.context;
        autoStartTopLevel(context);
        ensureValueState(context);
        doWriteStartContainer(contextType, style);
        setNextState(); // 设置新上下文状态
    }

    private void writeEndContainer(DsonContextType contextType, DsonWriterState expectedState) {
        Context context = this.context;
        checkEndContext(context, contextType, expectedState);
        doWriteEndContainer();
        setNextState(); // parent前进一个状态
    }

    private void autoStartTopLevel(Context context) {
        if (context.contextType == DsonContextType.TOP_LEVEL
                && context.state == DsonWriterState.INITIAL) {
            context.setState(DsonWriterState.VALUE);
        }
    }

    private void checkEndContext(Context context, DsonContextType contextType, DsonWriterState state) {
        if (context.contextType != contextType) {
            throw DsonCodecException.contextError(contextType, context.contextType);
        }
        if (context.state != state) {
            throw invalidState(List.of(state), context.state);
        }
    }

    /** 写入类型信息，创建新上下文，压入上下文 */
    protected abstract void doWriteStartContainer(DsonContextType contextType, ObjectStyle style);

    /** 弹出上下文 */
    protected abstract void doWriteEndContainer();

    // endregion
    // region sp

    @Override
    public void writeMessage(int name, int binaryType, MessageLite messageLite) {
        DsonBinary.checksSubType(binaryType);
        advanceToValueState(name);
        doWriteMessage(binaryType, messageLite);
        setNextState();
    }

    @Override
    public void writeValueBytes(int name, DsonType type, byte[] data) {
        DsonReaderUtils.checkWriteValueAsBytes(type);
        advanceToValueState(name);
        doWriteValueBytes(type, data);
        setNextState();
    }

    protected abstract void doWriteMessage(int binaryType, MessageLite messageLite);

    protected abstract void doWriteValueBytes(DsonType type, byte[] data);

    // endregion

    // region context

    protected static class Context {

        public Context parent;
        public DsonContextType contextType;
        public DsonWriterState state = DsonWriterState.INITIAL;
        public int name;

        public Context() {
        }

        public Context(Context parent, DsonContextType contextType) {
            this.parent = parent;
            this.contextType = contextType;
        }

        public void init(Context parent, DsonContextType contextType) {
            this.parent = parent;
            this.contextType = contextType;
        }

        public void reset() {
            parent = null;
            contextType = null;
            state = DsonWriterState.INITIAL;
            name = 0;
        }

        /** 方便查看赋值的调用 */
        public void setState(DsonWriterState state) {
            this.state = state;
        }

        public Context getParent() {
            return parent;
        }
    }

}