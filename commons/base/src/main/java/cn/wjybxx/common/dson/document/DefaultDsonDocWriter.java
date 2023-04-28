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

/**
 * @author wjybxx
 * date - 2023/4/21
 */
public class DefaultDsonDocWriter extends AbstractDsonDocWriter {

    private final DsonOutput output;

    public DefaultDsonDocWriter(DsonOutput output, int recursionLimit) {
        super(recursionLimit);
        this.output = output;
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
        output.flush();
    }

    @Override
    public void close() {
        output.close();
    }

    // region state

    private void writeFullTypeAndCurrentName(DsonOutput output, DsonType dsonType, @Nullable WireType wireType) {
        if (wireType == null) {
            output.writeRawByte((byte) Dsons.makeFullType(dsonType.getNumber(), 0));
        } else {
            output.writeRawByte((byte) Dsons.makeFullType(dsonType.getNumber(), wireType.getNumber()));
        }
        Context context = getContext();
        if (context.contextType == DsonContextType.OBJECT) {
            output.writeString(context.name);
        }
    }
    // endregion

    // region 简单值

    @Override
    protected void doWriteInt32(int value, WireType wireType) {
        DsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.INT32, wireType);
        wireType.writeInt32(output, value);
    }

    @Override
    protected void doWriteInt64(long value, WireType wireType) {
        DsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.INT64, wireType);
        wireType.writeInt64(output, value);
    }

    @Override
    protected void doWriteFloat(float value) {
        DsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.FLOAT, null);
        output.writeFloat(value);
    }

    @Override
    protected void doWriteDouble(double value) {
        DsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.DOUBLE, null);
        output.writeDouble(value);
    }

    @Override
    protected void doWriteBool(boolean value) {
        DsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.BOOLEAN, null);
        output.writeBool(value);
    }

    @Override
    protected void doWriteString(String value) {
        DsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.STRING, null);
        output.writeString(value);
    }

    @Override
    protected void doWriteNull() {
        DsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.NULL, null);
    }

    @Override
    protected void doWriteBinary(byte type, byte[] data) {
        DsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.BINARY, null);
        {
            output.writeFixed32(1 + data.length);
            output.writeRawByte(type);
            output.writeRawBytes(data);
        }
    }

    @Override
    protected void doWriteBinary(byte type, Chunk chunk) {
        DsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.BINARY, null);
        {
            output.writeFixed32(1 + chunk.getLength());
            output.writeRawByte(type);
            output.writeRawBytes(chunk.getBuffer(), chunk.getOffset(), chunk.getLength());
        }
    }

    @Override
    protected void doWriteExtString(byte type, String value) {
        DsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.EXT_STRING, null);
        output.writeRawByte(type);
        output.writeString(value);
    }

    @Override
    protected void doWriteExtInt32(byte type, int value, WireType wireType) {
        DsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.EXT_INT32, wireType);
        output.writeRawByte(type);
        wireType.writeInt32(output, value);
    }

    @Override
    protected void doWriteExtInt64(byte type, long value, WireType wireType) {
        DsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.EXT_INT64, wireType);
        output.writeRawByte(type);
        wireType.writeInt64(output, value);
    }

    // endregion

    // region 容器

    @Override
    protected void doWriteStartContainer(DsonContextType contextType, DocClassId classId) {
        DsonOutput output = this.output;
        DsonType dsonType = contextType == DsonContextType.ARRAY ? DsonType.ARRAY : DsonType.OBJECT;
        writeFullTypeAndCurrentName(output, dsonType, null);

        Context newContext = newContext(getContext(), contextType);
        newContext.preWritten = output.position();
        output.writeFixed32(0);
        writeClassId(classId);

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

    private void writeClassId(@Nullable DocClassId classId) {
        if (classId == null || classId.isObjectClassId()) {
            output.writeString("");
        } else {
            output.writeString(classId.getValue());
        }
    }
    // endregion

    // region 特殊接口

    @Override
    protected void doWriteMessage(MessageLite messageLite) {
        DsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.BINARY, null);
        {
            int preWritten = output.position();
            output.writeFixed32(0);
            output.writeRawByte(DsonBinaryType.PROTOBUF_MESSAGE.getValue());
            output.writeMessageNoSize(messageLite);
            output.setFixedInt32(preWritten, output.position() - preWritten - 4);
        }
    }

    @Override
    protected void doWriteValueBytes(DsonType type, byte[] data) {
        DsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, type, null);
        {
            if (type == DsonType.STRING) {
                output.writeUint32(data.length);
            } else {
                output.writeFixed32(data.length);
            }
            output.writeRawBytes(data);
        }
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

        public Context(AbstractDsonDocWriter.Context parent, DsonContextType contextType) {
            super(parent, contextType);
        }

        void reset() {
            super.reset();
            preWritten = 0;
        }
    }

    // endregion

}