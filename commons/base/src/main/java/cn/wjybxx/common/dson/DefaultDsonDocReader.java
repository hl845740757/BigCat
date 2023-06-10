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

import cn.wjybxx.common.dson.io.BinaryUtils;
import cn.wjybxx.common.dson.io.DsonInput;
import cn.wjybxx.common.dson.types.ObjectRef;
import com.google.protobuf.Parser;

/**
 * 与{@link DefaultDsonBinReader}的主要区别：
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

    private DsonInput input;

    public DefaultDsonDocReader(int recursionLimit, DsonInput input) {
        super(recursionLimit);
        this.input = input;
        setContext(new Context(null, DsonContextType.TOP_LEVEL));
    }

    @Override
    public void close() {
        if (input != null) {
            input.close();
            input = null;
        }
        super.close();
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
    public DsonType readDsonType() {
        Context context = this.getContext();
        checkReadDsonTypeState(context);

        final int fullType = input.isAtEnd() ? 0 : BinaryUtils.toUint8(input.readRawByte());
        DsonType dsonType = DsonType.forNumber(Dsons.dsonTypeOfFullType(fullType));
        WireType wireType = WireType.forNumber(Dsons.wireTypeOfFullType(fullType));
        this.currentDsonType = dsonType;
        this.currentWireType = wireType;
        this.currentName = INVALID_NAME;

        if (dsonType == DsonType.END_OF_OBJECT) {
            // readEndXXX都是子上下文中执行的，因此正常情况下topLevel不会读取到 endOfObject 标记
            // 顶层读取到 END_OF_OBJECT 表示到达文件尾
            if (context.contextType == DsonContextType.TOP_LEVEL) {
                context.setState(DsonReaderState.END_OF_FILE);
            } else {
                context.setState(DsonReaderState.WAIT_END_OBJECT);
            }
        } else {
            // topLevel只可是容器对象
            if (context.contextType == DsonContextType.TOP_LEVEL && !dsonType.isContainer()) {
                throw DsonCodecException.invalidDsonType(context.contextType, dsonType);
            }
            if (context.contextType == DsonContextType.OBJECT) {
                // 如果是header则直接进入VALUE状态 - header是匿名属性
                if (dsonType == DsonType.HEADER) {
                    context.setState(DsonReaderState.VALUE);
                } else {
                    context.setState(DsonReaderState.NAME);
                }
            } else if (context.contextType == DsonContextType.HEADER) {
                context.setState(DsonReaderState.NAME);
            } else {
                context.setState(DsonReaderState.VALUE);
            }
        }
        return dsonType;
    }

    @Override
    protected void doReadName() {
        currentName = Dsons.internField(input.readString());
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
        return DsonReaderUtils.readDsonBinary(input);
    }

    @Override
    protected DsonExtString doReadExtString() {
        return DsonReaderUtils.readDsonExtString(input);
    }

    @Override
    protected DsonExtInt32 doReadExtInt32() {
        return DsonReaderUtils.readDsonExtInt32(input, currentWireType);
    }

    @Override
    protected DsonExtInt64 doReadExtInt64() {
        return DsonReaderUtils.readDsonExtInt64(input, currentWireType);
    }

    @Override
    protected ObjectRef doReadRef() {
        return DsonReaderUtils.readRef(input);
    }

    // endregion

    // region 容器

    @Override
    protected void doReadStartContainer(DsonContextType contextType) {
        Context newContext = newContext(getContext(), contextType);
        int length = input.readFixed32();
        newContext.oldLimit = input.pushLimit(length);
        newContext.name = currentName;

        this.recursionDepth++;
        setContext(newContext);
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
        DsonReaderUtils.skipValue(input, getContextType(), currentDsonType, currentWireType);
    }

    @Override
    protected void doSkipToEndOfObject() {
        DsonReaderUtils.skipToEndOfObject(input);
    }

    @Override
    protected <T> T doReadMessage(int binaryType, Parser<T> parser) {
        return DsonReaderUtils.readMessage(input, binaryType, parser);
    }

    @Override
    protected byte[] doReadValueAsBytes() {
        return DsonReaderUtils.readValueAsBytes(input, currentDsonType);
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