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

package cn.wjybxx.common.dson.text;

import cn.wjybxx.common.dson.*;
import cn.wjybxx.common.dson.io.Chunk;
import cn.wjybxx.common.dson.io.DsonOutput;
import cn.wjybxx.common.dson.types.ObjectRef;
import com.google.protobuf.MessageLite;

import javax.annotation.Nullable;
import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/4/21
 */
public class DsonTextWriter extends AbstractDsonDocWriter {

    private DsonPrinter printer;

    public DsonTextWriter(int recursionLimit, DsonPrinter printer) {
        super(recursionLimit);
        this.printer = printer;
        setContext(new Context(null, DsonContextType.TOP_LEVEL));
    }

    @Override
    public Context getContext() {
        return (Context) super.getContext();
    }

    @Override
    public Context getPooledContext() {
        return (Context) super.getPooledContext();
    }

    //
    @Override
    public void flush() {
        printer.flush();
    }

    @Override
    public void close() {
        if (printer != null) {
            printer.close();
            printer = null;
        }
        super.close();
    }

    // region state

    private void writeFullTypeAndCurrentName(DsonPrinter output, DsonType dsonType, @Nullable WireType wireType) {
        if (wireType == null) {
            output.writeRawByte((byte) Dsons.makeFullType(dsonType.getNumber(), 0));
        } else {
            output.writeRawByte((byte) Dsons.makeFullType(dsonType.getNumber(), wireType.getNumber()));
        }
        Context context = getContext();
        if (context.contextType == DsonContextType.OBJECT ||
                context.contextType == DsonContextType.HEADER) {
            output.writeString(context.name, );
        }
    }

    @Override
    protected void doWriteName(String name) {
        printer.writeString(name, StringMode.AUTO);
    }

    // endregion

    // region 简单值

    @Override
    protected void doWriteInt32(int value, WireType wireType) {
        printer.writeInt32(value, );
    }

    @Override
    protected void doWriteInt64(long value, WireType wireType) {
        printer.writeInt64(value, );
    }

    @Override
    protected void doWriteFloat(float value) {
        printer.writeFloat(value, );
    }

    @Override
    protected void doWriteDouble(double value) {
        printer.writeDouble(value, );
    }

    @Override
    protected void doWriteBool(boolean value) {
        printer.writeBoolean(value, );
    }

    @Override
    protected void doWriteString(String value) {
        printer.writeString(value, StringMode.AUTO);
    }

    @Override
    protected void doWriteNull() {
        printer.writeNull();
    }

    @Override
    protected void doWriteBinary(DsonBinary binary) {
        printer.writeStartObject();
        printer.writeType(DsonTexts.LABEL_BINARY);
        printer.writeInt32();

        DsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.BINARY, null);
        DsonReaderUtils.writeBinary(output, binary);
    }

    @Override
    protected void doWriteBinary(int type, Chunk chunk) {
        DsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.BINARY, null);
        DsonReaderUtils.writeBinary(output, type, chunk);
    }

    @Override
    protected void doWriteExtInt32(DsonExtInt32 value, WireType wireType) {
        DsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.EXT_INT32, wireType);
        DsonReaderUtils.writeExtInt32(output, value, wireType);
    }

    @Override
    protected void doWriteExtInt64(DsonExtInt64 value, WireType wireType) {
        DsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.EXT_INT64, wireType);
        DsonReaderUtils.writeExtInt64(output, value, wireType);
    }

    @Override
    protected void doWriteExtString(DsonExtString value) {
        DsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.EXT_STRING, null);
        DsonReaderUtils.writeExtString(output, value);
    }

    @Override
    protected void doWriteRef(ObjectRef objectRef) {
        DsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.REFERENCE, null);
        DsonReaderUtils.writeRef(output, objectRef);
    }

    // endregion

    // region 容器

    @Override
    protected void doWriteStartContainer(DsonContextType contextType) {
        DsonOutput output = this.output;
        DsonType dsonType = Objects.requireNonNull(contextType.dsonType);
        writeFullTypeAndCurrentName(output, dsonType, null);

        Context newContext = newContext(getContext(), contextType);
        newContext.preWritten = output.position();
        output.writeFixed32(0);

        setContext(newContext);
        this.recursionDepth++;
    }

    @Override
    protected void doWriteEndContainer() {
        // 记录preWritten在写length之前，最后的size要减4
        Context context = getContext();
        int preWritten = context.preWritten;
        output.setFixedInt32(preWritten, output.position() - preWritten - 4);

        this.recursionDepth--;
        setContext(context.parent);
        poolContext(context);
    }

    // endregion

    // region 特殊接口

    @Override
    protected void doWriteMessage(int binaryType, MessageLite messageLite) {
        DsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.BINARY, null);
        DsonReaderUtils.writeMessage(output, binaryType, messageLite);
    }

    @Override
    protected void doWriteValueBytes(DsonType type, byte[] data) {
        DsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, type, null);
        DsonReaderUtils.writeValueBytes(output, type, data);
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

    private static class Context extends AbstractDsonDocWriter.Context {

        int preWritten = 0;

        public Context() {
        }

        public Context(Context parent, DsonContextType contextType) {
            super(parent, contextType);
        }

        public void reset() {
            super.reset();
            preWritten = 0;
        }
    }

    // endregion

}