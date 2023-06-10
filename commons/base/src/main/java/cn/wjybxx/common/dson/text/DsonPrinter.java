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

    /** 行缓冲，减少同步写操作 */
    private final StringBuilder builder = new StringBuilder(150);
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
            builder.append(lineSeparator);
            flush();
            column = 0;
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    /** 打印行首 */
    public void printLhead(LheadType lheadType) {
        try {
            builder.append(lheadType.label);
            builder.append(' ');
            column += lheadType.label.length() + 1;
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    /** 打印缩进 */
    public void printIndent() {
        try {
            builder.append(indentionArray, 0, indent);
            column += indent;
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    public void print(char c) {
        try {
            builder.append(c);
            column += 1;
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    public void print(char[] cBuffer) {
        try {
            builder.append(cBuffer);
            column += 1;
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    public void print(char[] cBuffer, int offset, int len) {
        try {
            builder.append(cBuffer, offset, len);
            column += len;
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    /** @param text 纯文本 */
    public void print(String text) {
        try {
            builder.append(text);
            column += text.length();
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    /**
     * @param start the starting index of the subsequence to be appended.
     * @param end   the end index of the subsequence to be appended.
     */
    public void printSubRange(String text, int start, int end) {
        try {
            builder.append(text, start, end);
            column += (end - start);
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
            if (builder.length() > 0) {
                // 显式转cBuffer，避免toString的额外开销
                char[] cBuffer = new char[builder.length()];
                builder.getChars(0, builder.length(), cBuffer, 0);

                writer.write(cBuffer, 0, cBuffer.length);
                builder.setLength(0);
            }
            writer.flush();
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    @Override
    public void close() {
        try {
            flush();
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