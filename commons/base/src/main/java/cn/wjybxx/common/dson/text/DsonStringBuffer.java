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

import java.util.ArrayList;
import java.util.List;
import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/6/3
 */
public class DsonStringBuffer implements DsonBuffer {

    private final String buffer;
    private final List<LineInfo> lines = new ArrayList<>();

    private LineInfo curLine;
    private boolean eof = false;
    private int position = -1;

    public DsonStringBuffer(String buffer) {
        this.buffer = Objects.requireNonNull(buffer);
    }

    @Override
    public int readSlowly() {
        if (eof) {
            throw new DsonParseException("Trying to read past eof");
        }
        if (position == -1) {
            position = 0;
            // 由于存在unread，因此要检查
            if (lines.isEmpty()) {
                scanNextLine();
            }
            if (lines.isEmpty()) {
                eof = true;
                return -1;
            } else {
                curLine = lines.get(0);
                return -2;
            }
        } else if (position >= curLine.endPos) {
            return onReadEndOfLine();
        } else {
            // 只读取内容部分
            if (curLine.contentStartPos < 0) {
                position = curLine.endPos; // 跳到行尾
                return onReadEndOfLine();
            }
            if (position < curLine.contentStartPos) {
                position = curLine.contentStartPos;
            } else {
                position++;
            }
            // 全局位置
            return buffer.charAt(position);
        }
    }

    private int onReadEndOfLine() {
        // 由于存在unread，因此要检查，避免过早解析
        if (curLine.index + 1 == lines.size()) {
            scanNextLine();
        }
        if (curLine.index + 1 < lines.size()) {
            curLine = lines.get(curLine.index + 1);
            position = curLine.startPos;
            return -2;
        } else {
            eof = true;
            return -1;
        }
    }

    private void scanNextLine() {
        int bufferLength = buffer.length();
        int startPos = 0;
        int ln = 1;
        if (lines.size() > 0) {
            LineInfo lastLine = lines.get(lines.size() - 1);
            startPos = lastLine.endPos;
            // 先跳过当前行换行符，如果不是最后一行
            while (startPos < bufferLength && buffer.charAt(startPos) != '\n') {
                startPos++;
            }
            startPos++;
            ln = lastLine.ln + 1;
        }

        int endPos = startPos;
        while (endPos < bufferLength) {
            char c = buffer.charAt(endPos);
            // 检查中途是否出现单独的 \r
            if (c == '\n' || c == '\r' || endPos == bufferLength -1) {
                if (c == '\r') {
                    if (endPos + 1 != bufferLength && buffer.charAt(endPos + 1) != '\n') {
                        throw new DsonParseException("invalid input. A separate \\r, \\r\\n or \\n is require, Position: " + endPos);
                    }
                }
                if (endPos != bufferLength - 1) {
                    endPos = endPos - 1; // endPos不包含换行符
                }
                String lhead = parseLhead(buffer, startPos, endPos, ln);
                DsonLheadType lheadType = DsonLheadType.ofLabel(lhead);
                if (lheadType == DsonLheadType.COMMENT) {
                    if (endPos == bufferLength -1) { // eof
                        break;
                    }
                    if (c == '\r') { // 前面-1了
                        endPos += 3;
                    } else {
                        endPos += 2;
                    }
                    continue;
                }
                int contentStartPos;
                int contentStartIndex = indexContentStart(buffer, startPos, endPos);
                if (contentStartIndex == -1) {
                    contentStartPos = -1;
                } else {
                    contentStartPos = contentStartIndex;
                }
                lines.add(new LineInfo(startPos, endPos, contentStartPos, lheadType, ln, lines.size()));
                break;
            }
            endPos++;
        }
    }

    @Override
    public void unread() {
        if (position == -1) {
            throw new IllegalStateException("read must be called before unread.");
        }
        if (eof) {
            eof = false;
        } else if (position > curLine.contentStartPos) {
            position--;
        } else if (position == curLine.contentStartPos) {
            position = curLine.startPos;
        } else if (position == curLine.startPos) {
            if (curLine.index > 0) {
                curLine = lines.get(curLine.index - 1);
                position = curLine.endPos;
            } else {
                curLine = null;
                position = -1;
            }
        } else {
            throw new IllegalStateException("");
        }
    }

    @Override
    public DsonLheadType lhead() {
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

    /**
     * 解析行首
     * 1. 空白行 和 #开头的行 都认为是注释行，返回 #
     * 2. 如果是约定的内容行行首，则返回行首标识
     * 3. 其它情况下返回null
     *
     * @param startPos inclusive 0-based
     * @param endPos   inclusive 0-based
     */
    private static String parseLhead(final String line, int startPos, int endPos, int ln) {
        // 减少不必要的字符串切割
        int startIndex = startPos;
        while (startIndex <= endPos && Character.isWhitespace(line.charAt(startIndex))) {
            startIndex++;
        }
        if (startIndex > endPos || line.charAt(startIndex) == '#') {
            return "#"; // 空白行或注释行都看做注释行
        }

        int endIndex = startIndex;
        while (endIndex <= endPos && !Character.isWhitespace(line.charAt(endIndex))) {
            endIndex++;
        }
        // 检查第一个缩进字符必须是空格
        if (endIndex <= endPos && line.charAt(endIndex) != ' ') {
            throw new DsonParseException(String.format("The first indent char must be a space, ln: %d, char: '%c' ", ln, line.charAt(endIndex)));
        }
        String lhead = line.substring(startIndex, endIndex);
        if (DsonTexts.CONTENT_LHEAD_SET.contains(lhead)) {
            return lhead;
        }
        throw new DsonParseException("Unknown head " + lhead);
    }

    /**
     * @param startPos inclusive 0-based
     * @param endPos   inclusive 0-based
     */
    private static int indexContentStart(String line, int startPos, int endPos) {
        // 我们的内容label都是两个字符，都是 - 开头，且和内容之间一个空白字符
        final int startIndex = line.indexOf('-', startPos);
        final int targetIndex = startIndex + DsonTexts.CONTENT_LHEAD_LENGTH + 1;
        if (targetIndex <= endPos) {
            return targetIndex;
        }
        return -1;
    }

    private static class LineInfo {

        final int startPos; // 行起始位置，包含行首，与上一行只差包含换行符
        final int endPos; // 行结束位置，不包含换行符
        final int contentStartPos; // 内容起始位置，可能 -1
        final DsonLheadType lheadType;

        final int ln;
        final int index;

        public LineInfo(int startPos, int endPos, int contentStartPos, DsonLheadType lheadType,
                        int ln, int index) {
            this.startPos = startPos;
            this.endPos = endPos;
            this.contentStartPos = contentStartPos;
            this.lheadType = lheadType;
            this.ln = ln;
            this.index = index;
        }

        @Override
        public String toString() {
            return "LineInfo{" +
                    " startPos=" + startPos +
                    ", endPos=" + endPos +
                    ", contentStartPos=" + contentStartPos +
                    ", lheadType=" + lheadType +
                    ", ln=" + ln +
                    '}';
        }
    }
}