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
 * 将json模拟为dson输入流
 *
 * @author wjybxx
 * date - 2023/6/5
 */
public class JsonBuffer implements DsonBuffer {

    private final CharSequence buffer;

    protected final List<LineInfo> lines = new ArrayList<>();
    protected LineInfo curLine;
    private boolean eof = false;
    private int position = -1;
    private boolean startLine = false;

    public JsonBuffer(CharSequence buffer) {
        this.buffer = buffer;
    }

    protected void setCurLine(LineInfo curLine) {
        this.curLine = curLine;
    }

    @Override
    public int readSlowly() {
        if (eof) {
            throw new DsonParseException("Trying to read past eof");
        }
        LineInfo curLine = this.curLine;
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
            if (curLine.startPos == curLine.endPos) { // 无可读内容
                position = curLine.endPos;
                return onReadEndOfLine();
            }
            if (!startLine) { // 初始读
                startLine = true;
            } else if (position + 1 == curLine.endPos) { // 读完
                position = curLine.endPos;
                return onReadEndOfLine();
            } else {
                position++;
            }
            return buffer.charAt(position);
        }
    }

    private void onReadNextLine(LineInfo curLine) {
        setCurLine(curLine);
        position = curLine.startPos;
        startLine = false;
    }

    private int onReadEndOfLine() {
        // 由于存在unread，因此要检查，避免过早解析
        LineInfo curLine = this.curLine;
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
        LineInfo curLine = this.curLine;
        if (eof) {
            eof = false;
        } else if (position > curLine.startPos) {
            position--;
        } else if (startLine) {
            startLine = false;
        } else {
            assert position == curLine.startPos;
            // 回退到上一行
            if (curLine.index > 0) {
                curLine = lines.get(curLine.index - 1);
                setCurLine(curLine);
                position = curLine.endPos;
                startLine = true;
            } else {
                setCurLine(null);
                position = -1;
                startLine = false;
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

    @Override
    public void close() {

    }

    //
    private void scanNextLine() {
        CharSequence buffer = this.buffer;
        List<LineInfo> lines = this.lines;
        int bufferLength = buffer.length();

        int startPos = DsonStringBuffer.indexNextLineStartPos(buffer, lines, bufferLength);
        int ln = DsonStringBuffer.getNextLn(lines);
        int endPos = startPos;
        while (endPos < bufferLength) {
            char c = buffer.charAt(endPos);
            if (c == '\n' || c == '\r' || endPos == bufferLength - 1) {
                DsonStringBuffer.checkLRLF(buffer, bufferLength, endPos, c);
                if (startPos == endPos) { // 空行
                    endPos += DsonStringBuffer.lengthLRLF(c);
                    continue;
                }
                if (endPos == bufferLength - 1) { // eof - parse需要扫描到该位置
                    endPos = bufferLength;
                }
                if (DsonStringBuffer.isCommentLine(buffer, startPos, endPos)) { // 注释行
                    endPos += DsonStringBuffer.lengthLRLF(c);
                    continue;
                }
                lines.add(new LineInfo(startPos, endPos, startPos,
                        LheadType.APPEND_LINE, ln, lines.size()));
                break;
            }
            endPos++;
        }
    }

}