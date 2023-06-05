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

import java.util.ArrayList;
import java.util.List;
import java.util.Objects;

/**
 * 抽象实现，提供基于Dson语法的字符串解析目标
 *
 * @author wjybxx
 * date - 2023/6/3
 */
@Internal
public abstract class AbstractDsonBuffer<T extends LineInfo> implements DsonBuffer {

    protected final List<T> lines = new ArrayList<>();
    protected T curLine;
    private boolean eof = false;
    private int position = -1;

    protected void setCurLine(T curLine) {
        this.curLine = curLine;
    }

    public void addLine(T newLine) {
        Objects.requireNonNull(newLine);
        lines.add(newLine);
    }

    @Override
    public int readSlowly() {
        if (eof) {
            throw new DsonParseException("Trying to read past eof");
        }
        T curLine = this.curLine;
        if (position == -1) {
            position = 0;
            // 由于存在unread，因此要检查，避免重复解析
            if (lines.isEmpty()) {
                scanNextLine();
            }
            if (lines.isEmpty()) {
                eof = true;
                return -1;
            } else {
                curLine = lines.get(0);
                onReadNextLine(curLine);
                return -2;
            }
        } else if (position == curLine.endPos) {
            return onReadEndOfLine();
        } else {
            if (curLine.contentStartPos < 0) { // 无可读内容
                position = curLine.endPos;
                return onReadEndOfLine();
            }
            if (position < curLine.contentStartPos) { // 初始读
                position = curLine.contentStartPos;
            } else if (position + 1 == curLine.endPos) { // 读完
                position = curLine.endPos;
                return onReadEndOfLine();
            } else {
                position++;
            }
            return charAt(curLine, position);
        }
    }

    protected abstract int charAt(T curLine, int position);

    protected abstract void scanNextLine();

    private void onReadNextLine(T curLine) {
        setCurLine(curLine);
        position = curLine.startPos;
    }

    private int onReadEndOfLine() {
        // 由于存在unread，因此要检查，避免过早解析
        T curLine = this.curLine;
        if (curLine.index + 1 == lines.size()) {
            scanNextLine();
        }
        if (curLine.index + 1 < lines.size()) {
            curLine = lines.get(curLine.index + 1);
            onReadNextLine(curLine);
            return -2;
        } else {
            eof = true;
            return -1;
        }
    }

    @Override
    public void unread() {
        if (position == -1) {
            throw new IllegalStateException("read must be called before unread.");
        }
        T curLine = this.curLine;
        if (eof) {
            eof = false;
        } else if (position > curLine.contentStartPos) {
            position--;
        } else if (position == curLine.contentStartPos) {
            position = curLine.startPos;
        } else {
            assert position == curLine.startPos;
            // 回退上一行
            if (curLine.index > 0) {
                curLine = lines.get(curLine.index - 1);
                setCurLine(curLine);
                position = curLine.endPos;
            } else {
                setCurLine(null);
                position = -1;
            }
        }
    }

    @Override
    public LheadType lhead() {
        if (curLine == null) throw new IllegalStateException("read must be called before lhead");
        return curLine.lheadType;
    }

    @Override
    public int getPosition() {
        return position;
    }

    @Override
    public int getLn() {
        return curLine == null ? 0 : curLine.ln;
    }

    @Override
    public int getCol() {
        return curLine == null ? 0 : (position - curLine.startPos + 1);
    }

    //

    /**
     * 解析行首
     * 1. 空白行 和 #开头的行 都认为是注释行，返回 #
     * 2. 如果是约定的内容行行首，则返回行首标识
     * 3. 其它情况下返回null
     *
     * @param startPos inclusive 0-based
     * @param endPos   exclusive 0-based
     */
    static LheadType parseLhead(final CharSequence line, int startPos, int endPos, int ln) {
        // 减少不必要的字符串切割
        int startIndex = startPos;
        while (startIndex < endPos && Character.isWhitespace(line.charAt(startIndex))) {
            startIndex++;
        }
        if (startIndex >= endPos || line.charAt(startIndex) == '#') {
            return LheadType.COMMENT; // 空白行或注释行都看做注释行
        }

        int endIndex = startIndex;
        while (endIndex < endPos && !Character.isWhitespace(line.charAt(endIndex))) {
            endIndex++;
        }
        // 检查第一个缩进字符必须是空格
        if (endIndex < endPos && line.charAt(endIndex) != ' ') {
            throw new DsonParseException(String.format("The first indent char must be a space, ln: %d, char: '%c' ", ln, line.charAt(endIndex)));
        }
        String lhead = line.subSequence(startIndex, endIndex).toString();
        if (DsonTexts.CONTENT_LHEAD_SET.contains(lhead)) {
            return LheadType.ofLabel(lhead);
        }
        throw new DsonParseException("Unknown head " + lhead);
    }

    /**
     * @param startPos inclusive 0-based
     * @param endPos   exclusive 0-based
     */
    static int indexContentStart(CharSequence line, int startPos, int endPos) {
        // 我们的内容label都是两个字符，都是 - 开头，且和内容之间一个空白字符
        int startIndex = startPos;
        while (line.charAt(startIndex) != '-') {
            startIndex++;
        }
        final int targetIndex = startIndex + DsonTexts.CONTENT_LHEAD_LENGTH + 1;
        if (targetIndex < endPos) {
            return targetIndex;
        }
        return -1;
    }

    /**
     * @param startPos inclusive 0-based
     * @param endPos   exclusive 0-based
     */
    static boolean isCommentLine(CharSequence line, int startPos, int endPos) {
        while (startPos < endPos && Character.isWhitespace(line.charAt(startPos))) {
            startPos++;
        }
        return startPos >= endPos || line.charAt(startPos) == '#';
    }

}