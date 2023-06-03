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

import cn.wjybxx.common.dson.DsonBinary;
import cn.wjybxx.common.dson.DsonExtInt32;
import cn.wjybxx.common.dson.DsonExtInt64;
import cn.wjybxx.common.dson.DsonExtString;
import org.apache.commons.lang3.StringUtils;

import java.util.List;

/**
 * @author wjybxx
 * date - 2023/6/2
 */
public class DsonScanner implements AutoCloseable {

    private static final List<TokenType> STRING_TOKEN_LIST = List.of(TokenType.STRING, TokenType.UNQUOTE_STRING);

    private final DsonBuffer buffer;
    private final char[] unicodeCharBuffer = new char[4];
    private final StringBuilder sbBuffer = new StringBuilder(64);

    public DsonScanner(DsonBuffer buffer) {
        this.buffer = buffer;
    }

    @Override
    public void close() {
        buffer.close();
    }

    public DsonToken nextToken() {
        int c = skipWhitespace();
        if (c == -1) {
            return new DsonToken(TokenType.EOF, "<eof>");
        }
        return switch (c) {
            case '{' -> {
                int nextChar = buffer.read();
                buffer.unread();
                if (nextChar == '@') { // 告诉外部，这个@是修饰object/array自身的
                    yield new DsonToken(TokenType.BEGIN_OBJECT, "{@");
                } else {
                    yield new DsonToken(TokenType.BEGIN_OBJECT, "{");
                }
            }
            case '[' -> {
                int nextChar = buffer.read();
                buffer.unread();
                if (nextChar == '@') {
                    yield new DsonToken(TokenType.BEGIN_ARRAY, "[@");
                } else {
                    yield new DsonToken(TokenType.BEGIN_ARRAY, "[");
                }
            }
            case '}' -> new DsonToken(TokenType.END_OBJECT, "}");
            case ']' -> new DsonToken(TokenType.END_ARRAY, "]");
            case ':' -> new DsonToken(TokenType.COLON, ":");
            case ',' -> new DsonToken(TokenType.COMMA, ",");
            case '"' -> new DsonToken(TokenType.STRING, scanString((char) c));
            case '@' -> parseHeaderToken();
            default -> new DsonToken(TokenType.UNQUOTE_STRING, scanUnquotedString((char) c));
        };
    }

    // region common

    /** @return 如果调到文件尾则返回 -1 */
    private int skipWhitespace() {
        int c;
        while ((c = buffer.read()) != -1 && Character.isWhitespace(c)) {

        }
        return c;
    }

    private static void checkEof(int c) {
        if (c == -1) {
            throw new DsonParseException("End of file in Dson string.");
        }
    }

    private static void checkToken(List<TokenType> expected, TokenType tokenType, int position) {
        if (!expected.contains(tokenType)) {
            throw new DsonParseException(String.format("Invalid Dson Token. Position: %d. Expected: %s. Found: '%s'.",
                    position, expected, tokenType));
        }
    }

    private static void checkToken(TokenType expected, TokenType tokenType, int position) {
        if (tokenType != expected) {
            throw invalidTokenType(List.of(expected), tokenType, position);
        }
    }

    private static DsonParseException invalidTokenType(List<TokenType> expected, TokenType tokenType, int position) {
        throw new DsonParseException(String.format("Invalid Dson Token. Position: %d. Expected: %s. Found: '%s'.",
                position, expected, tokenType));
    }

    private static DsonParseException invalidInput(int c, int position) {
        return new DsonParseException(String.format("Invalid Dson input. Position: %d. Character: '%c'.", position, c));
    }

    private static DsonParseException invalidClassName(String c, int position) {
        return new DsonParseException(String.format("Invalid className. Position: %d. ClassName: '%s'.", position, c));
    }

    private DsonParseException invalidEscapeSequence(int c, int position) {
        return new DsonParseException(String.format("Invalid escape sequence. Position: %d. Character: '\\%c'.", position, c));
    }

    private int getPosition() {
        return buffer.getPosition();
    }

    private int getPositionAndUnread() {
        int position = buffer.getPosition();
        buffer.unread();
        return position;
    }
    // endregion

    // region header

    private DsonToken parseHeaderToken() {
        try {
            String className = scanClassName();
            return onReadClassName(className);
        } catch (Exception e) {
            throw DsonParseException.wrap(e);
        }
    }

    private String scanClassName() {
        int firstChar = buffer.read();
        checkEof(firstChar);
        // header是结构体，需要等待上层 beginObject
        if (firstChar == '{' || firstChar == '[') {
            buffer.unread();
            return "{";
        }
        if (Character.isWhitespace(firstChar)) {
            throw invalidInput(firstChar, getPosition());
        }
        String className = firstChar == '"' ? scanString((char) firstChar) : scanUnquotedString((char) firstChar);
        if (StringUtils.isBlank(className)) {
            throw invalidClassName(className, getPosition());
        }
        return className;
    }

    private DsonToken onReadClassName(String className) {
        // className后一定是空格 - 因为scan是扫描到空白字符才停止
        final int position = getPosition();
        switch (className) {
            case DsonTexts.LABEL_INT32 -> {
                DsonToken nextToken = nextToken();
                checkToken(STRING_TOKEN_LIST, nextToken.getType(), position);
                return new DsonToken(TokenType.INT32, Integer.parseInt(nextToken.castAsString()));
            }
            case DsonTexts.LABEL_INT64 -> {
                DsonToken nextToken = nextToken();
                checkToken(STRING_TOKEN_LIST, nextToken.getType(), position);
                return new DsonToken(TokenType.INT64, Long.parseLong(nextToken.castAsString()));
            }
            case DsonTexts.LABEL_FLOAT -> {
                DsonToken nextToken = nextToken();
                checkToken(STRING_TOKEN_LIST, nextToken.getType(), position);
                return new DsonToken(TokenType.FLOAT, Float.parseFloat(nextToken.castAsString()));
            }
            case DsonTexts.LABEL_DOUBLE -> {
                DsonToken nextToken = nextToken();
                checkToken(STRING_TOKEN_LIST, nextToken.getType(), position);
                return new DsonToken(TokenType.DOUBLE, Double.parseDouble(nextToken.castAsString()));
            }
            case DsonTexts.LABEL_STRING -> {
                DsonToken nextToken = nextToken();
                checkToken(STRING_TOKEN_LIST, nextToken.getType(), position);
                return new DsonToken(TokenType.STRING, nextToken.getValue());
            }
            case DsonTexts.LABEL_BOOL -> {
                DsonToken nextToken = nextToken();
                checkToken(STRING_TOKEN_LIST, nextToken.getType(), position);
                return new DsonToken(TokenType.BOOL, parseBool(nextToken.castAsString()));
            }
            case DsonTexts.LABEL_NULL -> {
                DsonToken nextToken = nextToken();
                checkToken(STRING_TOKEN_LIST, nextToken.getType(), position);
                checkNullString(nextToken.castAsString());
                return new DsonToken(TokenType.NULL, null);
            }
            case DsonTexts.LABEL_BINARY -> {
                return scanBinary();
            }
            case DsonTexts.LABEL_EXTINT32 -> {
                return scanExtInt32();
            }
            case DsonTexts.LABEL_EXTINT64 -> {
                return scanExtInt64();
            }
            case DsonTexts.LABEL_EXTSTRING -> {
                return scanExtString();
            }
            case DsonTexts.LABEL_TEXT -> {
                return new DsonToken(TokenType.STRING, scanText());
            }
        }
        return new DsonToken(TokenType.HEADER, className);
    }

    private DsonToken scanExtString() {
        TuplePair tuplePair = scanTupleImpl();
        return new DsonToken(TokenType.EXTSTRING, new DsonExtString(tuplePair.type, tuplePair.value));
    }

    private DsonToken scanExtInt64() {
        TuplePair tuplePair = scanTupleImpl();
        long value = Long.parseLong(tuplePair.value);
        return new DsonToken(TokenType.EXTINT32, new DsonExtInt64(tuplePair.type, value));
    }

    private DsonToken scanExtInt32() {
        TuplePair tuplePair = scanTupleImpl();
        int value = Integer.parseInt(tuplePair.value);
        return new DsonToken(TokenType.EXTINT32, new DsonExtInt32(tuplePair.type, value));
    }

    private DsonToken scanBinary() {
        TuplePair tuplePair = scanTupleImpl();
        byte[] data = DsonTexts.decodeHex(tuplePair.value.toCharArray());
        return new DsonToken(TokenType.BINARY, new DsonBinary(tuplePair.type, data));
    }

    private TuplePair scanTupleImpl() {
        DsonToken nextToken = nextToken();
        checkToken(TokenType.BEGIN_OBJECT, nextToken.getType(), getPosition());

        nextToken = nextToken();
        checkToken(TokenType.UNQUOTE_STRING, nextToken.getType(), getPosition());
        int type = Integer.parseInt(nextToken.castAsString());

        nextToken = nextToken();
        checkToken(TokenType.COMMA, nextToken.getType(), getPosition());

        nextToken = nextToken();
        checkToken(STRING_TOKEN_LIST, nextToken.getType(), getPosition());
        String value = nextToken.castAsString();

        nextToken = nextToken();
        checkToken(TokenType.END_OBJECT, nextToken.getType(), getPosition());
        return new TuplePair(type, value);
    }

    private static Object parseBool(String str) {
        if ("true".equals(str)) return true;
        if ("false".equals(str)) return false;
        throw new IllegalArgumentException("invalid bool str: " + str);
    }

    private static Object checkNullString(String str) {
        if ("null".equals(str)) return null;
        throw new IllegalArgumentException("invalid null str: " + str);
    }

    // endregion

    // region 字符串

    /**
     * 扫描双引号字符串
     *
     * @param quoteChar 引号字符
     */
    private String scanString(char quoteChar) {
        StringBuilder sb = alloStringBuilder();
        while (true) {
            int c = buffer.read();
            if (c == '\\') { // 处理转义字符
                c = buffer.read();
                switch (c) {
                    case '"' -> sb.append('"'); // 双引号字符串下，双引号需要转义
                    case '\\' -> sb.append('\\');
                    case 'b' -> sb.append('\b');
                    case 'f' -> sb.append('\f');
                    case 'n' -> sb.append('\n');
                    case 'r' -> sb.append('\r');
                    case 't' -> sb.append('\t');
                    case 'u' -> {
                        // unicode码元，char是2字节，固定编码为4个16进制数，从高到底
                        int u1 = buffer.read();
                        int u2 = buffer.read();
                        int u3 = buffer.read();
                        int u4 = buffer.read();
                        if (u4 != -1) {
                            char[] charBuffer = this.unicodeCharBuffer;
                            charBuffer[0] = (char) u1;
                            charBuffer[1] = (char) u2;
                            charBuffer[2] = (char) u3;
                            charBuffer[3] = (char) u4;
                            String hex = new String(charBuffer);
                            sb.append((char) Integer.parseInt(hex, 16));
                        }
                    }
                    default -> throw invalidEscapeSequence(c, getPositionAndUnread());

                }
            } else {
                if (c == quoteChar) {
                    return sb.toString();
                }
                if (c != -1) {
                    sb.append((char) c);
                }
            }
            checkEof(c);
        }
    }

    /**
     * 扫描无引号字符串，无引号字符串不支持切换到独立行
     *
     * @param firstChar 第一个非空白字符
     */
    private String scanUnquotedString(final char firstChar) {
        StringBuilder sb = alloStringBuilder();
        sb.append((char) firstChar);
        int c;
        while ((c = buffer.readSlowly()) != -1) {
            if (c == -2) { // 产生换行，无引号字符串不可以切换到独立行
                if (buffer.lhead() != LheadType.APPEND) {
                    break;
                }
                continue;
            }
            if (!DsonTexts.isSafeStringChar((char) c)) {
                break;
            }
            sb.append((char) c);
        }
        buffer.unread();
        return sb.toString();
    }

    private StringBuilder alloStringBuilder() {
        sbBuffer.setLength(0);
        return sbBuffer;
    }

    /** 扫描文本段 */
    private String scanText() {
        buffer.read(); // 第一个字符为缩进，且一定是空白字符

        StringBuilder sb = alloStringBuilder();
        int c;
        while ((c = buffer.readSlowly()) != -1) {
            if (c == -2) { // 产生换行
                if (buffer.lhead() == LheadType.APPEND_LINE) { // 读取结束
                    break;
                }
                if (buffer.lhead() == LheadType.TEXT_APPEND_LINE) { // 开启新行
                    sb.append('\n');
                }// else 行合并
            } else {
                sb.append((char) c);
            }
        }
        return sb.toString();
    }

    private static class TuplePair {

        final int type;
        final String value;

        private TuplePair(int type, String value) {
            this.type = type;
            this.value = value;
        }
    }

    // endregion

    private static String unicodeChar(char c) {
        StringBuilder sb = new StringBuilder(6);
        // 确保编码为4个字符
        sb.append('\\').append('u');
        sb.append(Integer.toHexString((c & 0xf000) >> 12));
        sb.append(Integer.toHexString((c & 0x0f00) >> 8));
        sb.append(Integer.toHexString((c & 0x00f0) >> 4));
        sb.append(Integer.toHexString((c & 0x000f)));
        return sb.toString();
    }

    public static void main(String[] args) {

    }
}