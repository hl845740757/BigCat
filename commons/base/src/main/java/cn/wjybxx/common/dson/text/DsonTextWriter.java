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
import cn.wjybxx.common.dson.types.ObjectRef;
import com.google.protobuf.MessageLite;

import java.io.Writer;
import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/4/21
 */
public class DsonTextWriter extends AbstractDsonDocWriter {

    private DsonPrinter printer;
    private final DsonTextWriterSettings settings;

    public DsonTextWriter(int recursionLimit, Writer writer, DsonTextWriterSettings settings) {
        super(recursionLimit);
        this.settings = settings;
        this.printer = new DsonPrinter(writer, settings.lineSeparator);
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

    private void writeCurrentName(DsonPrinter printer) {
        Context context = getContext();
        if (context.count > 0) {
            printer.print(",");
        }
        if (context.style == ObjectStyle.INDENT || printer.getColumn() >= settings.softLineLength) {
            printer.println();
            printer.printLhead(LheadType.APPEND_LINE);
            if (context.style == ObjectStyle.INDENT) {
                printer.printIndent();
            }
        } else {
            printer.print(' ');
        }
        if (context.contextType == DsonContextType.OBJECT ||
                context.contextType == DsonContextType.HEADER) {
            printStringNonSS(printer, context.name);
            printer.print(": ");
        }
    }

    /**
     * 不能无引号的情况下只回退为双引号模式；通常用于打印短字符串
     */
    private void printStringNonSS(DsonPrinter printer, String text) {
        if (DsonTexts.canUnquoteString(text)) {
            printer.print(text);
        } else {
            printEscaped(text);
        }
    }

    private void printString(DsonPrinter printer, String value, StringStyle style) {

    }

    /** 打印双引号String */
    private void printEscaped(String text) {
        DsonPrinter printer = this.printer;
        boolean unicodeChar = settings.unicodeChar;
        printer.print('"');
        for (int i = 0, length = text.length(); i < length; i++) {
            printEscaped(text.charAt(i), printer, unicodeChar);
        }
        printer.print('"');
    }

    private static void printEscaped(char c, DsonPrinter printer, boolean unicodeChar) {
        switch (c) {
            case '\"' -> printer.print("\\\"");
            case '\\' -> printer.print("\\\\");
            case '\b' -> printer.print("\\b");
            case '\f' -> printer.print("\\f");
            case '\n' -> printer.print("\\n");
            case '\r' -> printer.print("\\r");
            case '\t' -> printer.print("\\t");
            default -> {
                if (c < 32 || (c > 0x7F && unicodeChar)) {
                    printer.print("\\u");
                    printer.print(Integer.toHexString(0x10000 + (int) c), 1, 4);
                    return;
                }
                printer.print(c);
            }
        }
    }

    private void printBinary(byte[] buffer, int offset, int length) {

    }

    // endregion

    // region 简单值

    @Override
    protected void doWriteInt32(int value, WireType wireType, boolean stronglyTyped) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer);
        if (stronglyTyped) {
            printer.print("@i ");
        }
        printer.print(Integer.toString(value));
    }

    @Override
    protected void doWriteInt64(long value, WireType wireType, boolean stronglyTyped) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer);
        if (stronglyTyped) {
            printer.print("@L ");
        }
        printer.print(Long.toString(value));
    }

    @Override
    protected void doWriteFloat(float value, boolean stronglyTyped) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer);
        if (stronglyTyped) {
            printer.print("@f ");
        }
        printer.print(Float.toString(value));
    }

    @Override
    protected void doWriteDouble(double value) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer);
        printer.print(Double.toString(value));
    }

    @Override
    protected void doWriteBool(boolean value) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer);
        printer.print(value ? "true" : "false");
    }

    @Override
    protected void doWriteString(String value, StringStyle style) {
        printString(printer, value, style);
    }

    @Override
    protected void doWriteNull() {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer);
        printer.print("null");
    }

    @Override
    protected void doWriteBinary(DsonBinary binary) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer);
        printer.print("{@bin ");
        printer.print(Integer.toString(binary.getType()));
        printer.print(", ");
        // 打印16进制字符串...
        printBinary(binary.getData(), 0, binary.getData().length);
        printer.print('}');
    }

    @Override
    protected void doWriteBinary(int type, Chunk chunk) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer);
        printer.print("{@bin ");
        printer.print(Integer.toString(type));
        printer.print(", ");
        printBinary(chunk.getBuffer(), chunk.getOffset(), chunk.getLength());
        printer.print('}');
    }

    @Override
    protected void doWriteExtInt32(DsonExtInt32 value, WireType wireType) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer);
        printer.print("{@ei ");
        printer.print(Integer.toString(value.getType()));
        printer.print(", ");
        printer.print(Integer.toString(value.getValue()));
        printer.print('}');
    }

    @Override
    protected void doWriteExtInt64(DsonExtInt64 value, WireType wireType) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer);
        printer.print("{@eL ");
        printer.print(Integer.toString(value.getType()));
        printer.print(", ");
        printer.print(Long.toString(value.getValue()));
        printer.print('}');
    }

    @Override
    protected void doWriteExtString(DsonExtString value, StringStyle style) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer);
        printer.print("{@es ");
        printer.print(Integer.toString(value.getType()));
        printer.print(", ");
        printString(printer, value.getValue(), style);
        printer.print('}');
    }

    @Override
    protected void doWriteRef(ObjectRef objectRef) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer);
        printer.print("{@ref ");
        int count = 0;
        if (objectRef.getGuid() != null) {
            count++;
            printer.print(ObjectRef.FIELDS_GUID);
            printer.print(": ");
            printStringNonSS(printer, objectRef.getGuid());
        }
        if (objectRef.getLocalId() != null) {
            if (count++ > 0) printer.print(", ");
            printer.print(ObjectRef.FIELDS_LOCAL_ID);
            printer.print(": ");
            printStringNonSS(printer, objectRef.getLocalId());
        }
        if (objectRef.getType() != 0) {
            if (count++ > 0) printer.print(", ");
            printer.print(ObjectRef.FIELDS_TYPE);
            printer.print(": ");
            printer.print(Integer.toString(objectRef.getType()));
        }
        if (objectRef.getPolicy() != 0) {
            if (count > 0) printer.print(", ");
            printer.print(ObjectRef.FIELDS_POLICY);
            printer.print(": ");
            printer.print(Integer.toString(objectRef.getPolicy()));
        }
        printer.print('}');
    }

    // endregion

    // region 容器

    @Override
    protected void doWriteStartContainer(DsonContextType contextType, ObjectStyle style) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer);
        // todo
        printer.print('{');

        Context newContext = newContext(getContext(), contextType);
        newContext.style = Objects.requireNonNull(style);
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
        doWriteBinary(new DsonBinary(binaryType, messageLite.toByteArray()));
    }

    @Override
    protected void doWriteValueBytes(DsonType type, byte[] data) {
        throw new UnsupportedOperationException();
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

        ObjectStyle style = ObjectStyle.INDENT;
        boolean headerWrited = false;
        int count;

        public Context() {
        }

        public Context(Context parent, DsonContextType contextType) {
            super(parent, contextType);
        }

        public void reset() {
            super.reset();
            style = ObjectStyle.INDENT;
            headerWrited = false;
        }
    }

    // endregion

}