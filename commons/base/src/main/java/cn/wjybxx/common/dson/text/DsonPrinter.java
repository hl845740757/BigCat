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

import org.apache.commons.lang3.exception.ExceptionUtils;

import java.io.Writer;
import java.util.Arrays;

/**
 * 该接口与{@link DsonScanner}对应
 * 总指导：
 * 1. token字符尽量不换行，eg：'{'、'['、'@'
 * 2. token字符和内容的空格缩进尽量在行尾
 *
 * @author wjybxx
 * date - 2023/6/5
 */
public class DsonPrinter {

    private final Writer writer;
    private final DsonTextWriterSettings settings;

    private char[] indentionArray = new char[0];
    private int indent = 0;
    private int column;

    public DsonPrinter(Writer writer, DsonTextWriterSettings settings) {
        this.writer = writer;
        this.settings = settings;
    }

    // region

    public void writeInt32(int value) {
        try {
            writer.write("@i ");
            writer.write(Integer.toString(value));
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    public void writeInt64(long value) {
        try {
            writer.write("@L ");
            writer.write(Long.toString(value));
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    public void writeFloat(float value) {
        try {
            writer.write("@f ");
            writer.write(Float.toString(value));
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    public void writeDouble(double value) {
        try {
            writer.write("@d ");
            writer.write(Double.toString(value));
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    public void writeBoolean(boolean value) {
        try {
            writer.write("@b ");
            writer.write(value ? "true" : "false");
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    public void writeString(String value, StringStyle mode) {
        try {
            // TODO 处理与Mode的冲突
            writer.write("@s ");
            writer.write(value);
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    public void writeNull() {
        try {
            writer.write("@N null");
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    public void writeStartArray() {
        try {
            writer.write('[');
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    public void writeEndArray() {
        try {
            writer.write(']');
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    public void writeStartObject() {
        try {
            writer.write('{');
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    public void writeEndObject() {
        try {
            writer.write('}');
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    public void writeStartHeader() {
        try {
            writer.write("@{");
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    public void writeEndHeader() {
        try {
            writer.write('}');
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    public void writeColon() {
        try {
            writer.write(':');
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    public void writeComma() {
        try {
            writer.write(',');
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }
    // endregion

    public void writeType(String value) {
        try {
            writer.write(value);
            writer.write(' ');
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    public void writeInt32NoType(int value) {
        try {
            writer.write(Integer.toString(value));
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    public void writeInt64NoType(long value) {
        try {
            writer.write(Long.toString(value));
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    public void writeFloatNoType(float value) {
        try {
            writer.write(Float.toString(value));
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    public void writeDoubleNoType(double value) {
        try {
            writer.write(Double.toString(value));
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    public void writeBooleanNoType(boolean value) {
        try {
            writer.write(value ? "true" : "false");
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    public void writeStringNoType(String value, StringStyle mode) {
        try {
            // todo 处理mode的冲突
            writer.write(value);
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    public void writeNullNoType() {
        try {
            writer.write("null");
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    public void flush() {

    }

    public void close() {

    }

    //

    private void indent() {
        indent += 2;
        updateIndent();
    }

    private void retract() {
        indent -= 2;
        updateIndent();
    }

    private void printIndent() {
        try {
            writer.write(indentionArray, 0, indent);
            column += indent;
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    private void println() {
        try {
            writer.append(settings.lineSeparator);
            column = 0;
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    private void updateIndent() {
        if (indent > indentionArray.length) {
            indentionArray = new char[indent];
            Arrays.fill(indentionArray, ' ');
        }
    }
}