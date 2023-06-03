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

import cn.wjybxx.common.dson.io.Chunk;
import cn.wjybxx.common.dson.types.ObjectRef;
import com.google.protobuf.MessageLite;

import java.util.List;
import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/4/28
 */
public abstract class AbstractDsonDocWriter implements DsonDocWriter {

    protected final int recursionLimit;
    private Context context;
    private Context pooledContext; // 一个额外的缓存，用于写集合等减少上下文创建

    protected int recursionDepth;

    public AbstractDsonDocWriter(int recursionLimit) {
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
    public void writeName(String name) {
        Objects.requireNonNull(name, "name");
        Context context = this.context;
        if (context.state != DsonWriterState.NAME) {
            throw invalidState(List.of(DsonWriterState.NAME), context.state);
        }
        context.name = name;
        context.state = DsonWriterState.VALUE;
        doWriteName();
    }

    /** 执行{@link #writeName(String)}时调用 */
    protected void doWriteName() {

    }

    protected void advanceToValueState(String name) {
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
            case TOP_LEVEL -> context.setState(DsonWriterState.DONE);
            case OBJECT, HEADER -> context.setState(DsonWriterState.NAME);
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
        advanceToValueState(name);
        doWriteInt32(value, wireType);
        setNextState();
    }

    @Override
    public void writeInt64(String name, long value, WireType wireType) {
        advanceToValueState(name);
        doWriteInt64(value, wireType);
        setNextState();
    }

    @Override
    public void writeFloat(String name, float value) {
        advanceToValueState(name);
        doWriteFloat(value);
        setNextState();
    }

    @Override
    public void writeDouble(String name, double value) {
        advanceToValueState(name);
        doWriteDouble(value);
        setNextState();
    }

    @Override
    public void writeBoolean(String name, boolean value) {
        advanceToValueState(name);
        doWriteBool(value);
        setNextState();
    }

    @Override
    public void writeString(String name, String value) {
        advanceToValueState(name);
        doWriteString(value);
        setNextState();
    }

    @Override
    public void writeNull(String name) {
        advanceToValueState(name);
        doWriteNull();
        setNextState();
    }

    @Override
    public void writeBinary(String name, DsonBinary dsonBinary) {
        writeBinary(name, dsonBinary.getType(), dsonBinary.getData());
    }

    @Override
    public void writeBinary(String name, int type, byte[] data) {
        advanceToValueState(name);
        doWriteBinary(type, data);
        setNextState();
    }

    @Override
    public void writeBinary(String name, int type, Chunk chunk) {
        advanceToValueState(name);
        doWriteBinary(type, chunk);
        setNextState();
    }

    @Override
    public void writeExtString(String name, DsonExtString value) {
        writeExtString(name, value.getType(), value.getValue());
    }

    @Override
    public void writeExtString(String name, int type, String value) {
        advanceToValueState(name);
        doWriteExtString(type, value);
        setNextState();
    }

    @Override
    public void writeExtInt32(String name, DsonExtInt32 value, WireType wireType) {
        writeExtInt32(name, value.getType(), value.getValue(), wireType);
    }

    @Override
    public void writeExtInt32(String name, int type, int value, WireType wireType) {
        advanceToValueState(name);
        doWriteExtInt32(type, value, wireType);
        setNextState();
    }

    @Override
    public void writeExtInt64(String name, DsonExtInt64 value, WireType wireType) {
        writeExtInt64(name, value.getType(), value.getValue(), wireType);
    }

    @Override
    public void writeExtInt64(String name, int type, long value, WireType wireType) {
        advanceToValueState(name);
        doWriteExtInt64(type, value, wireType);
        setNextState();
    }

    @Override
    public void writeRef(String name, ObjectRef objectRef) {
        advanceToValueState(name);
        doWriteRef(objectRef);
        setNextState();
    }

    protected abstract void doWriteInt32(int value, WireType wireType);

    protected abstract void doWriteInt64(long value, WireType wireType);

    protected abstract void doWriteFloat(float value);

    protected abstract void doWriteDouble(double value);

    protected abstract void doWriteBool(boolean value);

    protected abstract void doWriteString(String value);

    protected abstract void doWriteNull();

    protected abstract void doWriteBinary(int type, byte[] data);

    protected abstract void doWriteBinary(int type, Chunk chunk);

    protected abstract void doWriteExtString(int type, String value);

    protected abstract void doWriteExtInt32(int type, int value, WireType wireType);

    protected abstract void doWriteExtInt64(int type, long value, WireType wireType);

    protected abstract void doWriteRef(ObjectRef objectRef);

    // endregion

    // region 容器
    @Override
    public void writeStartArray() {
        if (recursionDepth >= recursionLimit) {
            throw DsonCodecException.recursionLimitExceeded();
        }
        Context context = this.context;
        autoStartTopLevel(context);
        ensureValueState(context);
        doWriteStartContainer(DsonContextType.ARRAY);
        setNextState(); // 设置新上下文状态
    }

    @Override
    public void writeEndArray() {
        Context context = this.context;
        checkEndContext(context, DsonContextType.ARRAY, DsonWriterState.VALUE);
        doWriteEndContainer();
        setNextState(); // parent前进一个状态
    }

    @Override
    public void writeStartObject() {
        if (recursionDepth >= recursionLimit) {
            throw DsonCodecException.recursionLimitExceeded();
        }
        Context context = this.context;
        autoStartTopLevel(context);
        ensureValueState(context);
        doWriteStartContainer(DsonContextType.OBJECT);
        setNextState(); // 设置新上下文状态
    }

    @Override
    public void writeEndObject() {
        Context context = this.context;
        checkEndContext(context, DsonContextType.OBJECT, DsonWriterState.NAME);
        doWriteEndContainer();
        setNextState(); // parent前进一个状态
    }

    @Override
    public void writeStartHeader() {
        if (recursionDepth >= recursionLimit) {
            throw DsonCodecException.recursionLimitExceeded();
        }
        Context context = this.context;
//        autoStartTopLevel(context); // header不能是顶层对象
        ensureValueState(context);
        doWriteStartContainer(DsonContextType.HEADER);
        setNextState(); // 设置新上下文状态
    }

    @Override
    public void writeEndHeader() {
        Context context = this.context;
        checkEndContext(context, DsonContextType.HEADER, DsonWriterState.NAME);
        doWriteEndContainer();
        setNextState(); // parent前进一个状态
    }

    private void autoStartTopLevel(Context context) {
        if (context.contextType == DsonContextType.TOP_LEVEL
                && (context.state == DsonWriterState.INITIAL || context.state == DsonWriterState.DONE)) {
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
    protected abstract void doWriteStartContainer(DsonContextType contextType);

    /** 弹出上下文 */
    protected abstract void doWriteEndContainer();

    // endregion
    // region sp

    @Override
    public void writeMessage(String name, int binaryType, MessageLite messageLite) {
        advanceToValueState(name);
        doWriteMessage(binaryType, messageLite);
        setNextState();
    }

    @Override
    public void writeValueBytes(String name, DsonType type, byte[] data) {
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
        public String name;

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

        /** 方便查看赋值的调用 */
        public void setState(DsonWriterState state) {
            this.state = state;
        }

        void reset() {
            parent = null;
            contextType = null;
            state = DsonWriterState.INITIAL;
            name = null;
        }
    }

}