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

/**
 * @author wjybxx
 * date - 2023/6/2
 */
public class DsonLineBuffer implements DsonBuffer {

    private final List<LineInfo> lines;

    private LineInfo curLine;
    private int position = -1;
    private boolean eof = false;

    public DsonLineBuffer(List<String> lines) {
        this.lines = preprocess(lines);
    }

    @Override
    public int readSlowly() {
        if (eof) {
            throw new DsonParseException("Trying to read past eof");
        }
        if (position == -1) {
            position = 0;
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
                return readSlowly();
            }
            if (position < curLine.contentStartPos) {
                position = curLine.contentStartPos;
            } else {
                position++;
            }
            int offset = position - curLine.startPos;
            return curLine.line.charAt(offset);
        }
    }

    private int onReadEndOfLine() {
        if (curLine.index + 1 < lines.size()) {
            curLine = lines.get(curLine.index + 1);
            position = curLine.startPos;
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

    private static class LineInfo {

        final String line;
        final int startPos; // 行起始位置，包含行首，与上一行只差包含换行符
        final int endPos; // 行结束位置，不包含换行符
        final int contentStartPos; // 内容起始位置，可能 -1
        final DsonLheadType lheadType;

        final int ln;
        final int index;

        public LineInfo(String line, int startPos, int endPos, int contentStartPos, DsonLheadType lheadType,
                        int ln, int index) {
            this.line = line;
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
                    "line='" + line + '\'' +
                    ", startPos=" + startPos +
                    ", endPos=" + endPos +
                    ", contentStartPos=" + contentStartPos +
                    ", lheadType=" + lheadType +
                    ", ln=" + ln +
                    ", index=" + index +
                    '}';
        }
    }

    private static List<LineInfo> preprocess(List<String> lines) {
        List<LineInfo> contentLines = new ArrayList<>(lines.size());
        int ln = 0;
        int lineIndex = -1;
        int startPos = 0;
        int endPos = -2;
        for (final String line : lines) {
            ln++;
            startPos = endPos + 2; // 换行符号也算算一个字符
            endPos = startPos + line.length() - 1;
            if (line.isEmpty()) {
                continue;
            }

            lineIndex++;
            String lhead = parseLhead(line);
            DsonLheadType lheadType = DsonLheadType.ofLabel(lhead);
            if (lheadType == DsonLheadType.COMMENT) {
                continue;
            }
            int contentStartPos;
            int contentStartIndex = indexContentStart(line);
            if (contentStartIndex == -1) {
                contentStartPos = -1;
            } else {
                contentStartPos = startPos + contentStartIndex;
            }
            contentLines.add(new LineInfo(line, startPos, endPos, contentStartPos, lheadType, ln, lineIndex));
        }
        return contentLines;
    }

    /**
     * 解析行首
     * 1. 空白行 和 #开头的行 都认为是注释行，返回 #
     * 2. 如果是约定的内容行行首，则返回行首标识
     * 3. 其它情况下返回null
     */
    private static String parseLhead(final String line) {
        if (line.isBlank() || line.startsWith("#")) {
            return "#"; // 空白行也当做注释行
        }
        // 减少不必要的字符串切割
        int startIndex = 0;
        while (startIndex < line.length() && Character.isWhitespace(line.charAt(startIndex))) {
            startIndex++;
        }
        if (line.charAt(startIndex) == '#') {
            return "#";
        }

        int endIndex = startIndex;
        while (endIndex < line.length() && !Character.isWhitespace(line.charAt(endIndex))) {
            endIndex++;
        }
        // 检查第一个缩进字符必须是空格
        if (endIndex < line.length() && line.charAt(endIndex) != ' ') {
            throw new DsonParseException(String.format("The first indent char must be a space, char: '%c' ", line.charAt(endIndex)));
        }
        String lhead = line.substring(startIndex, endIndex);
        if (DsonTexts.CONTENT_LHEAD_SET.contains(lhead)) {
            return lhead;
        }
        throw new DsonParseException("Unknown head " + lhead);
    }

    private static int indexContentStart(String line) {
        // 我们的内容label都是两个字符，都是 - 开头，且和内容之间一个空白字符
        final int startIndex = line.indexOf('-');
        final int targetIndex = startIndex + DsonTexts.CONTENT_LHEAD_LENGTH + 1;
        if (targetIndex < line.length()) {
            return targetIndex;
        }
        return -1;
    }

}