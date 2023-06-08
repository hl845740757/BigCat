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

import cn.wjybxx.common.annotation.Internal;
import org.apache.commons.lang3.exception.ExceptionUtils;

import java.io.Writer;
import java.util.Arrays;
import java.util.Objects;

/**
 * 该接口与{@link DsonScanner}对应
 * 总指导：
 * 1. token字符尽量不换行，eg：'{'、'['、'@'
 * 2. token字符和内容的空格缩进尽量在行尾
 *
 * @author wjybxx
 * date - 2023/6/5
 */
@Internal
public final class DsonPrinter implements AutoCloseable {

    private final Writer writer;
    private final String lineSeparator;
    private char[] indentionArray = new char[0];
    private int indent = 0;
    private int column;

    public DsonPrinter(Writer writer, String lineSeparator) {
        this.writer = Objects.requireNonNull(writer);
        this.lineSeparator = Objects.requireNonNull(lineSeparator);
    }

    public int getColumn() {
        return column;
    }

    /** 换行 */
    public void println() {
        try {
            writer.append(lineSeparator);
            column = 0;
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    /** 打印行首 */
    public void printLhead(LheadType lheadType) {
        try {
            writer.append(lheadType.label);
            writer.append(' ');
            column += lheadType.label.length() + 1;
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    /** 打印缩进 */
    public void printIndent() {
        try {
            writer.write(indentionArray, 0, indent);
            column += indent;
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    public void print(char c) {
        try {
            writer.write(c);
            column += 1;
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    public void print(char[] cBuffer) {
        try {
            writer.write(cBuffer);
            column += 1;
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    public void print(char[] cBuffer, int offset, int len) {
        try {
            writer.write(cBuffer, offset, len);
            column += len;
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    /** @param text 纯文本 */
    public void print(String text) {
        try {
            writer.write(text);
            column += text.length();
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    /**
     * @param offset Offset from which to start writing characters
     * @param length 写入的长度，jdk这个api的风格和其它的不一样啊...
     */
    public void print(String text, int offset, int length) {
        try {
            writer.write(text, offset, length);
            column += length;
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    public void println(String text) {
        print(text);
        println();
    }

    public void flush() {
        try {
            writer.flush();
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    @Override
    public void close() {
        try {
            writer.close();
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    public void indent() {
        indent += 2;
        updateIndent();
    }

    public void retract() {
        if (indent < 2) {
            throw new IllegalArgumentException("indent must be called before retract");
        }
        indent -= 2;
        updateIndent();
    }

    private void updateIndent() {
        if (indent > indentionArray.length) {
            indentionArray = new char[indent];
            Arrays.fill(indentionArray, ' ');
        }
    }
    // endregion
}