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

import java.util.List;
import java.util.Objects;
import java.util.stream.Collectors;

/**
 * @author wjybxx
 * date - 2023/6/2
 */
public class DsonLinesBuffer extends AbstractDsonBuffer<DsonLinesBuffer.LocalLineInfo> {

    private List<String> originLines;

    /**
     * @param originLines 不可以再包含换行符
     */
    public DsonLinesBuffer(List<String> originLines) {
        this.originLines = Objects.requireNonNull(originLines);
    }

    public static DsonLinesBuffer ofJson(String json) {
        List<String> lines = json.lines().map(e -> "-- " + e)
                .collect(Collectors.toList());
        return new DsonLinesBuffer(lines);
    }

    @Override
    protected int charAt(LocalLineInfo curLine, int position) {
        int offset = position - curLine.startPos;
        return curLine.line.charAt(offset);
    }

    @Override
    protected void scanNextLine() {
        if (originLines == null) {
            return;
        }
        preprocess(originLines, lines);
        originLines = null;
    }

    @Override
    public void close() {

    }

    static class LocalLineInfo extends LineInfo {

        final String line;

        public LocalLineInfo(int startPos, int endPos, int contentStartPos, LheadType lheadType,
                             int ln, int index, String line) {
            super(startPos, endPos, contentStartPos, lheadType, ln, index);
            this.line = line;
        }

        @Override
        public String toString() {
            return "LocalLineInfo{" +
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

    private static List<LocalLineInfo> preprocess(List<String> originLines, List<LocalLineInfo> lines) {
        int ln = 0;
        int startPos;
        int endPos = -1;
        for (final String line : originLines) {
            ln++;
            startPos = endPos + 1; // 换行符号也算一个字符
            endPos = startPos + line.length();
            if (line.isEmpty()) {
                continue;
            }
            LheadType lheadType = parseLhead(line, 0, line.length(), ln);
            if (lheadType == LheadType.COMMENT) {
                continue;
            }
            int contentStartPos;
            int contentStartIndex = indexContentStart(line, 0, line.length());
            if (contentStartIndex == -1) {
                contentStartPos = -1;
            } else {
                contentStartPos = startPos + contentStartIndex;
            }
            lines.add(new LocalLineInfo(startPos, endPos, contentStartPos,
                    lheadType, ln, lines.size(),
                    line));
        }
        return lines;
    }


}