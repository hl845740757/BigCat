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

/**
 * @author wjybxx
 * date - 2023/6/3
 */
public class DsonStringBuffer extends AbstractDsonBuffer<LineInfo> {

    private final String buffer;

    public DsonStringBuffer(String buffer) {
        this.buffer = Objects.requireNonNull(buffer);
    }

    @Override
    protected int charAt(LineInfo curLine, int position) {
        return buffer.charAt(position);
    }

    @Override
    protected void scanNextLine() {
        String buffer = this.buffer;
        List<LineInfo> lines = this.lines;
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
            // 现在操作系统的换行符只有: \r\n (windows) 和 \n (unix, mac)
            // 检查中途是否出现单独的 \r
            if (c == '\n' || c == '\r' || endPos == bufferLength - 1) {
                if (c == '\r') {
                    if (endPos + 1 != bufferLength && buffer.charAt(endPos + 1) != '\n') {
                        throw new DsonParseException("invalid input. A separate \\r, \\r\\n or \\n is require, Position: " + endPos);
                    }
                }
                if (endPos == bufferLength - 1) { // eof
                    endPos = bufferLength;
                }
                LheadType lheadType = parseLhead(buffer, startPos, endPos, ln);
                if (lheadType == LheadType.COMMENT) {
                    if (endPos == bufferLength) { // eof
                        break;
                    }
                    if (c == '\r') { // \r\n
                        endPos += 2;
                    } else {
                        endPos += 1;
                    }
                    continue;
                }
                int contentStartPos = indexContentStart(buffer, startPos, endPos);
                lines.add(new LineInfo(startPos, endPos, contentStartPos,
                        lheadType, ln, lines.size()));
                break;
            }
            endPos++;
        }
    }

    @Override
    public void close() {

    }
}