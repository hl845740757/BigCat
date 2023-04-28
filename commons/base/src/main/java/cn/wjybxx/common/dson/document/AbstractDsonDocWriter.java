package cn.wjybxx.common.dson.document;

import cn.wjybxx.common.dson.*;
import cn.wjybxx.common.dson.io.Chunk;
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

    // region state

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
    public void writeBinary(String name, byte type, byte[] data) {
        advanceToValueState(name);
        doWriteBinary(type, data);
        setNextState();
    }

    @Override
    public void writeBinary(String name, byte type, Chunk chunk) {
        advanceToValueState(name);
        doWriteBinary(type, chunk);
        setNextState();
    }

    @Override
    public void writeExtString(String name, DsonExtString value) {
        writeExtString(name, value.getType(), value.getValue());
    }

    @Override
    public void writeExtString(String name, byte type, String value) {
        advanceToValueState(name);
        doWriteExtString(type, value);
        setNextState();
    }

    @Override
    public void writeExtInt32(String name, DsonExtInt32 value, WireType wireType) {
        writeExtInt32(name, value.getType(), value.getValue(), wireType);
    }

    @Override
    public void writeExtInt32(String name, byte type, int value, WireType wireType) {
        advanceToValueState(name);
        doWriteExtInt32(type, value, wireType);
        setNextState();
    }

    @Override
    public void writeExtInt64(String name, DsonExtInt64 value, WireType wireType) {
        writeExtInt64(name, value.getType(), value.getValue(), wireType);
    }

    @Override
    public void writeExtInt64(String name, byte type, long value, WireType wireType) {
        advanceToValueState(name);
        doWriteExtInt64(type, value, wireType);
        setNextState();
    }

    protected abstract void doWriteInt32(int value, WireType wireType);

    protected abstract void doWriteInt64(long value, WireType wireType);

    protected abstract void doWriteFloat(float value);

    protected abstract void doWriteDouble(double value);

    protected abstract void doWriteBool(boolean value);

    protected abstract void doWriteString(String value);

    protected abstract void doWriteNull();

    protected abstract void doWriteBinary(byte type, byte[] data);

    protected abstract void doWriteBinary(byte type, Chunk chunk);

    protected abstract void doWriteExtString(byte type, String value);

    protected abstract void doWriteExtInt32(byte type, int value, WireType wireType);

    protected abstract void doWriteExtInt64(byte type, long value, WireType wireType);

    // endregion

    // region 容器
    @Override
    public void writeStartArray(DocClassId classId) {
        if (recursionDepth >= recursionLimit) {
            throw DsonCodecException.recursionLimitExceeded();
        }
        Context context = this.context;
        autoStartTopLevel(context);
        ensureValueState(context);
        doWriteStartContainer(DsonContextType.ARRAY, classId);
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
        doWriteEndContainer();
        setNextState(); // parent前进一个状态
    }

    @Override
    public void writeStartObject(DocClassId classId) {
        if (recursionDepth >= recursionLimit) {
            throw DsonCodecException.recursionLimitExceeded();
        }
        Context context = this.context;
        autoStartTopLevel(context);
        ensureValueState(context);
        doWriteStartContainer(DsonContextType.OBJECT, classId);
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
        doWriteEndContainer();
        setNextState(); // parent前进一个状态
    }

    private void autoStartTopLevel(Context context) {
        if (context.contextType == DsonContextType.TOP_LEVEL
                && (context.state == DsonWriterState.INITIAL || context.state == DsonWriterState.DONE)) {
            context.setState(DsonWriterState.VALUE);
        }
    }

    /** 写入类型信息，创建新上下文，压入上下文 */
    protected abstract void doWriteStartContainer(DsonContextType contextType, DocClassId classId);

    /** 弹出上下文 */
    protected abstract void doWriteEndContainer();

    // endregion

    // region sp

    @Override
    public void writeMessage(String name, MessageLite messageLite) {
        advanceToValueState(name);
        doWriteMessage(messageLite);
        setNextState();
    }

    @Override
    public void writeValueBytes(String name, DsonType type, byte[] data) {
        if (!Dsons.VALUE_BYTES_TYPES.contains(type)) {
            throw DsonCodecException.invalidDsonType(Dsons.VALUE_BYTES_TYPES, type);
        }
        advanceToValueState(name);
        doWriteValueBytes(type, data);
        setNextState();
    }

    protected abstract void doWriteMessage(MessageLite messageLite);

    protected abstract void doWriteValueBytes(DsonType type, byte[] data);

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

    protected static class Context {

        Context parent;
        DsonContextType contextType;
        DsonWriterState state = DsonWriterState.INITIAL;
        Object attach;

        /** 需要先缓存下来，因为name和value可能分开写入，而name必须写在type后面 */
        String name;

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
            attach = null;

            name = null;
        }
    }

}