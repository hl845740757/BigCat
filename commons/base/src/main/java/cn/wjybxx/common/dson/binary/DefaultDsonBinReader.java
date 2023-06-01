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

import cn.wjybxx.common.dson.*;
import cn.wjybxx.common.dson.io.BinaryUtils;
import cn.wjybxx.common.dson.io.DsonInput;
import cn.wjybxx.common.dson.types.ObjectRef;
import com.google.protobuf.Parser;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.util.List;
import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/4/22
 */
public class DefaultDsonBinReader implements DsonBinReader {

    private final DsonInput input;
    private final int recursionLimit;

    private Context context;
    private Context pooledContext; // 一个额外的缓存，用于写集合等减少上下文创建

    // 这些值放外面，不需要上下文隔离
    private int recursionDepth;
    private DsonType currentDsonType;
    private WireType currentWireType;
    private int currentName = 0;

    public DefaultDsonBinReader(DsonInput input, int recursionLimit) {
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
    public int getCurrentName() {
        if (context.state != DsonReaderState.VALUE) {
            throw invalidState(List.of(DsonReaderState.VALUE));
        }
        return currentName;
    }

    @Override
    public DsonContextType getContextType() {
        return context.contextType;
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
        this.currentName = 0;

        // topLevel只可是容器对象
        if (context.contextType == DsonContextType.TOP_LEVEL && !dsonType.isContainer()) {
            throw DsonCodecException.invalidDsonType(context.contextType, dsonType);
        }

        if (dsonType == DsonType.END_OF_OBJECT) {
            context.setState(DsonReaderState.WAIT_END_OBJECT);
        } else {
            // name/name总是和type同时解析
            if (context.contextType == DsonContextType.OBJECT) {
                // 如果是header则直接进入VALUE状态 - header匿名属性
                if (dsonType == DsonType.HEADER) {
                    context.setState(DsonReaderState.VALUE);
                } else {
                    currentName = input.readUint32();
                    context.setState(DsonReaderState.NAME);
                }
            } else {
                context.setState(DsonReaderState.VALUE);
            }
        }
        return dsonType;
    }

    @Override
    public int readName() {
        if (context.state != DsonReaderState.NAME) {
            throw invalidState(List.of(DsonReaderState.NAME));
        }
        context.setState(DsonReaderState.VALUE);
        return currentName;
    }

    @Override
    public void readName(int expected) {
        int name = readName();
        if (name != expected) {
            throw DsonCodecException.unexpectedName(expected, name);
        }
    }

    /** 前进到读值状态 */
    private void advanceToValueState(int name, @Nullable DsonType requiredType) {
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
    public int readInt32(int name) {
        advanceToValueState(name, DsonType.INT32);
        int value = currentWireType.readInt32(input);
        setNextState();
        return value;
    }

    @Override
    public long readInt64(int name) {
        advanceToValueState(name, DsonType.INT64);
        long value = currentWireType.readInt64(input);
        setNextState();
        return value;
    }

    @Override
    public float readFloat(int name) {
        advanceToValueState(name, DsonType.FLOAT);
        float value = input.readFloat();
        setNextState();
        return value;
    }

    @Override
    public double readDouble(int name) {
        advanceToValueState(name, DsonType.DOUBLE);
        double value = input.readDouble();
        setNextState();
        return value;
    }

    @Override
    public boolean readBoolean(int name) {
        advanceToValueState(name, DsonType.BOOLEAN);
        boolean value = input.readBool();
        setNextState();
        return value;
    }

    @Override
    public String readString(int name) {
        advanceToValueState(name, DsonType.STRING);
        String value = input.readString();
        setNextState();
        return value;
    }

    @Override
    public void readNull(int name) {
        advanceToValueState(name, DsonType.NULL);
        setNextState();
    }

    @Override
    public DsonBinary readBinary(int name) {
        advanceToValueState(name, DsonType.BINARY);
        DsonBinary value = DsonReaderUtils.readDsonBinary(input);
        setNextState();
        return value;
    }

    @Override
    public DsonExtString readExtString(int name) {
        advanceToValueState(name, DsonType.EXT_STRING);
        DsonExtString value = DsonReaderUtils.readDsonExtString(input);
        setNextState();
        return value;
    }

    @Override
    public DsonExtInt32 readExtInt32(int name) {
        advanceToValueState(name, DsonType.EXT_INT32);
        DsonExtInt32 value = DsonReaderUtils.readDsonExtInt32(input, currentWireType);
        setNextState();
        return value;
    }

    @Override
    public DsonExtInt64 readExtInt64(int name) {
        advanceToValueState(name, DsonType.EXT_INT64);
        DsonExtInt64 value = DsonReaderUtils.readDsonExtInt64(input, currentWireType);
        setNextState();
        return value;
    }

    @Override
    public ObjectRef readObjectRef(int name) {
        advanceToValueState(name, DsonType.REFERENCE);
        ObjectRef value = DsonReaderUtils.readObjectRef(input);
        setNextState();
        return value;
    }

    // endregion

    // region 容器
    @Override
    public void readStartArray() {
        Context context = this.context;
        if (context.state == DsonReaderState.WAIT_START_OBJECT) {
            setNextState();
            return;
        }
        if (recursionDepth >= recursionLimit) {
            throw DsonCodecException.recursionLimitExceeded();
        }
        autoStartTopLevel(context);
        ensureValueState(context, DsonType.ARRAY);
        doReadStartContainer(context, DsonContextType.ARRAY);
        setNextState(); // 设置新上下文状态
    }

    @Override
    public void readEndArray() {
        Context context = this.context;
        checkEndContext(context, DsonContextType.ARRAY);
        doReadEndContainer(context);
        setNextState(); // parent前进一个状态
    }

    @Override
    public void readStartObject() {
        Context context = this.context;
        if (context.state == DsonReaderState.WAIT_START_OBJECT) {
            setNextState();
            return;
        }
        if (recursionDepth >= recursionLimit) {
            throw DsonCodecException.recursionLimitExceeded();
        }
        autoStartTopLevel(context);
        ensureValueState(context, DsonType.OBJECT);
        doReadStartContainer(context, DsonContextType.OBJECT);
        setNextState(); // 设置新上下文状态
    }

    @Override
    public void readEndObject() {
        Context context = this.context;
        checkEndContext(context, DsonContextType.OBJECT);
        doReadEndContainer(context);
        setNextState(); // parent前进一个状态
    }

    @Override
    public void readStartHeader() {
        Context context = this.context;
        if (context.state == DsonReaderState.WAIT_START_OBJECT) {
            setNextState();
            return;
        }
        if (recursionDepth >= recursionLimit) {
            throw DsonCodecException.recursionLimitExceeded();
        }
//        autoStartTopLevel(context); // header不能是顶层对象
        ensureValueState(context, DsonType.HEADER);
        doReadStartContainer(context, DsonContextType.HEADER);
        setNextState(); // 设置新上下文状态
    }

    @Override
    public void readEndHeader() {
        Context context = this.context;
        checkEndContext(context, DsonContextType.HEADER);
        doReadEndContainer(context);
        setNextState(); // parent前进一个状态
    }

    @Override
    public void backToWaitStart() {
        Context context = this.context;
        if (context.contextType == DsonContextType.TOP_LEVEL) {
            throw DsonCodecException.contextErrorTopLevel();
        }
        if (context.state != DsonReaderState.TYPE) {
            throw invalidState(List.of(DsonReaderState.TYPE));
        }
        context.setState(DsonReaderState.WAIT_START_OBJECT);
    }

    private void doReadStartContainer(Context context, DsonContextType contextType) {
        Context newContext = newContext(context, contextType);
        int length = input.readFixed32();
        newContext.oldLimit = input.pushLimit(length);
        newContext.name = currentName;

        this.recursionDepth++;
        this.context = newContext;
    }

    private void doReadEndContainer(Context context) {
        if (!input.isAtEnd()) {
            throw DsonCodecException.bytesRemain(input.getBytesUntilLimit());
        }
        input.popLimit(context.oldLimit);

        // 恢复上下文
        recoverDsonType(context);
        this.recursionDepth--;
        this.context = context.parent;
        poolContext(context);
    }

    private void autoStartTopLevel(Context context) {
        if (context.contextType == DsonContextType.TOP_LEVEL
                && (context.state == DsonReaderState.INITIAL || context.state == DsonReaderState.DONE)) {
            readDsonType();
        }
    }

    private void recoverDsonType(Context context) {
        this.currentDsonType = Objects.requireNonNull(context.contextType.dsonType);
        this.currentWireType = WireType.VARINT;
        this.currentName = context.name;
    }

    private void checkEndContext(Context context, DsonContextType contextType) {
        if (context.contextType != contextType) {
            throw DsonCodecException.contextError(DsonContextType.OBJECT, context.contextType);
        }
        if (context.state != DsonReaderState.WAIT_END_OBJECT) {
            throw invalidState(List.of(DsonReaderState.WAIT_END_OBJECT));
        }
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
        readName();
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
        DsonReaderUtils.skipValue(input, getContextType(), currentDsonType, currentWireType);
    }

    @Override
    public void skipToEndOfObject() {
        Context context = this.context;
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
        doSkipToEndOfObject();
        setNextState();
        readDsonType(); // end of object
    }

    private void doSkipToEndOfObject() {
        DsonReaderUtils.skipToEndOfObject(input);
    }

    @Override
    public <T> T readMessage(int name, int binaryType, @Nonnull Parser<T> parser) {
        Objects.requireNonNull(parser, "parser");
        advanceToValueState(name, DsonType.BINARY);
        T value = doReadMessage(binaryType, parser);
        setNextState();
        return value;
    }

    private <T> T doReadMessage(int binaryType, Parser<T> parser) {
        return DsonReaderUtils.readMessage(input, binaryType, parser);
    }

    @Override
    public byte[] readValueAsBytes(int name) {
        advanceToValueState(name, null);
        DsonReaderUtils.checkReadValueAsBytes(currentDsonType);
        byte[] data = doReadValueAsBytes();
        setNextState();
        return data;
    }

    private byte[] doReadValueAsBytes() {
        return DsonReaderUtils.readValueAsBytes(input, currentDsonType);
    }

    @Override
    public DsonReaderGuide whatShouldIDo() {
        return DsonReaderUtils.whatShouldIDo(isAtEndOfObject(), context.contextType, context.state);
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
        DsonReaderState state = DsonReaderState.INITIAL;
        int oldLimit = -1;
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

        void setState(DsonReaderState state) {
            this.state = state;
        }

        void reset() {
            parent = null;
            contextType = null;
            state = DsonReaderState.INITIAL;
            oldLimit = -1;
            name = 0;
        }
    }

    // endregion

}