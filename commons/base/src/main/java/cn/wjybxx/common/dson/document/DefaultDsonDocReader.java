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

package cn.wjybxx.common.dson.document;

import cn.wjybxx.common.dson.*;
import cn.wjybxx.common.dson.io.BinaryUtils;
import cn.wjybxx.common.dson.io.DsonInput;
import com.google.protobuf.Parser;

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
public class DefaultDsonDocReader extends AbstractDsonDocReader {

    private final DsonInput input;

    public DefaultDsonDocReader(DsonInput input, int recursionLimit) {
        super(recursionLimit);
        this.input = input;
        setContext(new Context(null, DsonContextType.TOP_LEVEL));
    }

    @Override
    public void close() {
        input.close();
    }

    @Override
    public Context getContext() {
        return (Context) super.getContext();
    }

    @Override
    public Context getPooledContext() {
        return (Context) super.getPooledContext();
    }

    // region state

    @Override
    public boolean isAtEndOfObject() {
        return input.isAtEnd();
    }

    @Override
    public DsonType readDsonType() {
        Context context = this.getContext();
        checkReadDsonTypeState(context);

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

    // endregion

    // region 简单值

    @Override
    protected int doReadInt32() {
        return currentWireType.readInt32(input);
    }

    @Override
    protected long doReadInt64() {
        return currentWireType.readInt64(input);
    }

    @Override
    protected float doReadFloat() {
        return input.readFloat();
    }

    @Override
    protected double doReadDouble() {
        return input.readDouble();
    }

    @Override
    protected boolean doReadBool() {
        return input.readBool();
    }

    @Override
    protected String doReadString() {
        return input.readString();
    }

    @Override
    protected void doReadNull() {

    }

    @Override
    protected DsonBinary doReadBinary() {
        int size = input.readFixed32();
        return new DsonBinary(
                input.readRawByte(),
                input.readRawBytes(size - 1));
    }

    @Override
    protected DsonExtString doReadExtString() {
        return new DsonExtString(
                input.readRawByte(),
                input.readString());
    }

    @Override
    protected DsonExtInt32 doReadExtInt32() {
        return new DsonExtInt32(
                input.readRawByte(),
                currentWireType.readInt32(input));
    }

    @Override
    protected DsonExtInt64 doReadExtInt64() {
        return new DsonExtInt64(
                input.readRawByte(),
                currentWireType.readInt64(input));
    }

    // endregion

    // region 容器

    @Override
    protected void doReadStartContainer(DsonContextType contextType) {
        Context newContext = newContext(getContext(), contextType);
        int length = input.readFixed32();
        newContext.oldLimit = input.pushLimit(length); // length包含classId
        newContext.classId = readClassId();
        newContext.name = currentName;

        this.recursionDepth++;
        setContext(newContext);
    }

    private DocClassId readClassId() {
        String classId = Dsons.internClass(input.readString());
        return DocClassId.of(classId);
    }

    @Override
    protected void doReadEndContainer() {
        if (!input.isAtEnd()) {
            throw DsonCodecException.bytesRemain(input.getBytesUntilLimit());
        }
        Context context = getContext();
        input.popLimit(context.oldLimit);

        // 恢复上下文
        recoverDsonType(context);
        this.recursionDepth--;
        setContext(context.parent);
        poolContext(context);
    }

    // endregion

    // region 特殊接口

    @Override
    protected void doSkipName() {
        // 避免构建字符串
        int size = input.readUint32();
        if (size > 0) {
            input.skipRawBytes(size);
        }
    }

    @Override
    protected void doSkipValue() {
        DsonInput input = this.input;
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
            default -> throw DsonCodecException.invalidDsonType(getContext().contextType, currentDsonType);
        }
        if (skip > 0) {
            input.skipRawBytes(skip);
        }
    }

    @Override
    protected DsonValueSummary doPeekValueSummary() {
        DsonInput input = this.input;
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
    protected void doSkipToEndOfObject() {
        int size = input.getBytesUntilLimit();
        if (size > 0) {
            input.skipRawBytes(size);
        }
    }

    @Override
    protected <T> T doReadMessage(Parser<T> parser) {
        DsonInput input = this.input;
        int size = input.readFixed32();
        int oldLimit = input.pushLimit(size);
        byte subType = input.readRawByte();
        checkSubType(DsonBinaryType.PROTOBUF_MESSAGE.getValue(), subType);
        T value = input.readMessageNoSize(parser);
        input.popLimit(oldLimit);
        return value;
    }

    @Override
    protected byte[] doReadValueAsBytes() {
        int size;
        if (currentDsonType == DsonType.STRING) {
            size = input.readUint32();
        } else {
            size = input.readFixed32();
        }
        return input.readRawBytes(size);
    }

    // endregion

    // region context

    private Context newContext(Context parent, DsonContextType contextType) {
        Context context = getPooledContext();
        if (context != null) {
            setPooledContext(null);
        } else {
            context = new Context();
        }
        context.init(parent, contextType);
        return context;
    }

    private void poolContext(Context context) {
        context.reset();
        setPooledContext(context);
    }

    private static class Context extends AbstractDsonDocReader.Context {

        int oldLimit = -1;

        public Context() {
        }

        public Context(Context parent, DsonContextType contextType) {
            super(parent, contextType);
        }

        public void reset() {
            super.reset();
            oldLimit = -1;
        }
    }

    // endregion

}