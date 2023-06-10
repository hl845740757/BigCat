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
import org.apache.commons.codec.binary.Hex;
import org.apache.commons.lang3.StringUtils;

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

    private void writeCurrentName(DsonPrinter printer, DsonType dsonType) {
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
        } else if (context.count > 0) {
            printer.print(' ');
        }
        if (context.contextType == DsonContextType.OBJECT ||
                context.contextType == DsonContextType.HEADER) {
            printStringNonSS(printer, context.name);
            printer.print(": ");
        }

        if (dsonType == DsonType.HEADER) {
            context.headerWrited = true;
        } else {
            context.count++;
        }
    }

    /**
     * 不能无引号的情况下只回退为双引号模式；通常用于打印短字符串
     */
    private void printStringNonSS(DsonPrinter printer, String text) {
        if (DsonTexts.canUnquoteString(text) && DsonTexts.isASCIIText(text)) {
            printer.print(text);
        } else {
            printEscaped(text);
        }
    }

    private void printString(DsonPrinter printer, String value, StringStyle style) {
        switch (style) {
            case AUTO -> {
                if (DsonTexts.canUnquoteString(value) && DsonTexts.isASCIIText(value)) {
                    printer.print(value);
                } else if (!settings.enableText || value.length() < settings.softLineLength * 2) {
                    printEscaped(value);
                } else {
                    printText(value);
                }
            }
            case QUOTE -> printEscaped(value);
            case UNQUOTE -> printer.print(value);
            case TEXT -> {
                if (settings.enableText) {
                    printText(value);
                } else {
                    printEscaped(value);
                }
            }
        }
    }

    /** 打印双引号String */
    private void printEscaped(String text) {
        DsonPrinter printer = this.printer;
        boolean unicodeChar = settings.unicodeChar;
        printer.print('"');
        for (int i = 0, length = text.length(); i < length; i++) {
            printEscaped(text.charAt(i), printer, unicodeChar);
            if (printer.getColumn() >= settings.softLineLength) {
                printer.println();
                printer.printLhead(LheadType.APPEND);
            }
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
                if ((c < 32 || c > 126) && unicodeChar) {
                    printer.print("\\u");
                    printer.printSubRange(Integer.toHexString(0x10000 + (int) c), 1, 5);
                    return;
                }
                printer.print(c);
            }
        }
    }

    /** 纯文本模式打印，要执行换行符 */
    private void printText(String text) {
        DsonPrinter printer = this.printer;
        printer.print("@ss "); // 开始符
        for (int i = 0, length = text.length(); i < length; i++) {
            char c = text.charAt(i);
            if (c == '\r') {
                DsonTexts.checkLRLF(text, length, i, c);
                i++;
                c = text.charAt(i);
            }
            if (c == '\n') { // 要执行文本中的换行符
                printer.println();
                printer.printLhead(LheadType.TEXT_APPEND_LINE);
                continue;
            }
            printer.print(c);
            if (printer.getColumn() > settings.softLineLength) {
                printer.println();
                printer.printLhead(LheadType.APPEND);
            }
        }
        printer.println();
        printer.printLhead(LheadType.APPEND_LINE); // 结束符
    }

    private void printBinary(byte[] buffer, int offset, int length) {
        DsonPrinter printer = this.printer;
        // 使用小buffer多次编码代替大的buffer，不过也可能性能会下降
        int segment = 64;
        char[] cBuffer = new char[segment * 2];
        int loop = length / segment;
        for (int i = 0; i < loop; i++) {
            Hex.encodeHex(buffer, offset + i * segment, segment, false, cBuffer, 0);
            printer.print(cBuffer);
        }
        int remain = length - loop * segment;
        if (remain > 0) {
            Hex.encodeHex(buffer, offset + loop * segment, remain, false, cBuffer, 0);
            printer.print(cBuffer, 0, remain * 2);
        }
    }

    private void checkLineLength(LheadType lheadType) {
        DsonPrinter printer = this.printer;
        if (printer.getColumn() >= settings.softLineLength) {
            printer.println();
            printer.printLhead(lheadType);
        }
    }

    // endregion

    // region 简单值

    @Override
    protected void doWriteInt32(int value, WireType wireType, boolean stronglyTyped) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.INT32);
        if (stronglyTyped) {
            printer.print("@i ");
        }
        printer.print(Integer.toString(value));
    }

    @Override
    protected void doWriteInt64(long value, WireType wireType, boolean stronglyTyped) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.INT64);
        if (stronglyTyped) {
            printer.print("@L ");
        }
        printer.print(Long.toString(value));
    }

    @Override
    protected void doWriteFloat(float value, boolean stronglyTyped) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.FLOAT);
        if (stronglyTyped) {
            printer.print("@f ");
        }
        printer.print(Float.toString(value));
    }

    @Override
    protected void doWriteDouble(double value) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.DOUBLE);
        printer.print(Double.toString(value));
    }

    @Override
    protected void doWriteBool(boolean value) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.BOOLEAN);
        printer.print(value ? "true" : "false");
    }

    @Override
    protected void doWriteString(String value, StringStyle style) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.STRING);
        printString(printer, value, style);
    }

    @Override
    protected void doWriteNull() {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.NULL);
        printer.print("null");
    }

    @Override
    protected void doWriteBinary(DsonBinary binary) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.BINARY);
        printer.print("{@bin ");
        printer.print(Integer.toString(binary.getType()));
        printer.print(", ");
        printBinary(binary.getData(), 0, binary.getData().length);
        printer.print('}');
    }

    @Override
    protected void doWriteBinary(int type, Chunk chunk) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.BINARY);
        printer.print("{@bin ");
        printer.print(Integer.toString(type));
        printer.print(", ");
        printBinary(chunk.getBuffer(), chunk.getOffset(), chunk.getLength());
        printer.print('}');
    }

    @Override
    protected void doWriteExtInt32(DsonExtInt32 value, WireType wireType) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.EXT_INT32);
        printer.print("{@ei ");
        printer.print(Integer.toString(value.getType()));
        printer.print(", ");
        printer.print(Integer.toString(value.getValue()));
        printer.print('}');
    }

    @Override
    protected void doWriteExtInt64(DsonExtInt64 value, WireType wireType) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.EXT_INT64);
        printer.print("{@eL ");
        printer.print(Integer.toString(value.getType()));
        printer.print(", ");
        printer.print(Long.toString(value.getValue()));
        printer.print('}');
    }

    @Override
    protected void doWriteExtString(DsonExtString value, StringStyle style) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.EXT_STRING);
        printer.print("{@es ");
        printer.print(Integer.toString(value.getType()));
        printer.print(", ");
        printString(printer, value.getValue(), style);
        printer.print('}');
    }

    @Override
    protected void doWriteRef(ObjectRef objectRef) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.REFERENCE);
        if (StringUtils.isBlank(objectRef.getNamespace())
                && objectRef.getType() == 0 && objectRef.getPolicy() == 0) {
            printer.print("@ref ");
            printStringNonSS(printer, objectRef.getLocalId());
            return;
        }

        printer.print("{@ref ");
        int count = 0;
        if (objectRef.hasNamespace()) {
            count++;
            printer.print(ObjectRef.FIELDS_NAMESPACE);
            printer.print(": ");
            printStringNonSS(printer, objectRef.getNamespace());
        }
        if (objectRef.hasLocalId()) {
            if (count++ > 0) printer.print(", ");
            checkLineLength(LheadType.APPEND_LINE);
            printer.print(ObjectRef.FIELDS_LOCAL_ID);
            printer.print(": ");
            printStringNonSS(printer, objectRef.getLocalId());
        }
        if (objectRef.getType() != 0) {
            if (count++ > 0) printer.print(", ");
            checkLineLength(LheadType.APPEND_LINE);
            printer.print(ObjectRef.FIELDS_TYPE);
            printer.print(": ");
            printer.print(Integer.toString(objectRef.getType()));
        }
        if (objectRef.getPolicy() != 0) {
            if (count > 0) printer.print(", ");
            checkLineLength(LheadType.APPEND_LINE);
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
        writeCurrentName(printer, contextType.dsonType);

        Context newContext = newContext(getContext(), contextType);
        newContext.style = Objects.requireNonNull(style);

        printer.print(contextType.startSymbol);
        if (style == ObjectStyle.INDENT) {
            printer.indent();
        }

        setContext(newContext);
        this.recursionDepth++;
    }

    @Override
    protected void doWriteEndContainer() {
        Context context = getContext();
        DsonPrinter printer = this.printer;

        if (context.style == ObjectStyle.INDENT) {
            printer.retract();
            // 打印了内容的情况下才换行结束
            if (context.headerWrited || context.count > 0) {
                printer.println();
                printer.printLhead(LheadType.APPEND_LINE);
                printer.printIndent();
            }
        }
        printer.print(context.contextType.endSymbol);

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

        @Override
        public Context getParent() {
            return (Context) parent;
        }

    }

    // endregion

}