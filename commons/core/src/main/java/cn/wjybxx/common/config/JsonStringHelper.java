/*
 * Copyright 2023 wjybxx
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

package cn.wjybxx.common.config;

import cn.wjybxx.common.annotation.Beta;

/**
 * @author wjybxx
 * date 2023/4/15
 */
@Beta
public class JsonStringHelper {

    /**
     * 取消转义，将特殊符号打印为可见字符，eg： '\n' -> "\n"
     *
     * @param sb        用户需要外部重置
     * @param charArray 转义后的字符串，外部没有引号
     */
    public static void inverseEscape(StringBuilder sb, CharSequence charArray) {
        for (int index = 0; index < charArray.length(); index++) {
            char c = charArray.charAt(index);
            switch (c) {
                case '"' -> sb.append("\\\"");
                case '\\' -> sb.append("\\\\");
                case '\b' -> sb.append("\\b");
                case '\f' -> sb.append("\\f");
                case '\n' -> sb.append("\\n");
                case '\r' -> sb.append("\\r");
                case '\t' -> sb.append("\\t");
                default -> sb.append(c);
            }
        }
    }

    /** 转义无外部引号的字符串 */
    public static String escapeUnquoted(String value) {
        if (value.indexOf('\\') == -1) {
            return value;
        }
        StringBuilder sb = new StringBuilder(value.length());
        escapeImpl(sb, value, '"', -1);
        return sb.toString();
    }

    /**
     * 转义字符串
     *
     * @param sb             用户需要外部重置
     * @param charArray      原始字符串
     * @param quoteCharacter 双引号或单引号
     * @param quoteIndex     引号出现的位置
     * @return 引号结束的位置
     */
    public static int escape(StringBuilder sb, CharSequence charArray, char quoteCharacter, int quoteIndex) {
        return escapeImpl(sb, charArray, quoteCharacter, quoteIndex);
    }

    public static int escapeImpl(StringBuilder sb, CharSequence charArray, char quoteCharacter, final int quoteIndex) {
        int index = quoteIndex;
        while (true) {
            char c = next(charArray, ++index);
            if (c == '\\') {
                c = next(charArray, ++index);
                switch (c) {
                    case '\'' -> sb.append('\'');
                    case '"' -> sb.append('"');
                    case '\\' -> sb.append('\\');
                    case '/' -> sb.append('/');
                    case 'b' -> sb.append('\b');
                    case 'f' -> sb.append('\f');
                    case 'n' -> sb.append('\n');
                    case 'r' -> sb.append('\r');
                    case 't' -> sb.append('\t');
                    case 'u' -> {
                        char u1 = next(charArray, ++index);
                        char u2 = next(charArray, ++index);
                        char u3 = next(charArray, ++index);
                        char u4 = next(charArray, ++index);
                        String hex = new String(new char[]{u1, u2, u3, u4});
                        sb.append((char) Integer.parseInt(hex, 16));
                    }
                    default ->
                            throw new JsonStringParseException(String.format("Invalid escape sequence in JSON charArray '\\%c'.", c));
                }
            } else {
                if (quoteIndex != -1 && c == quoteCharacter) {
                    return index;
                }
                sb.append(c);
                if (quoteIndex == -1 && index == charArray.length() - 1) {
                    return index;
                }
            }
        }
    }

    private static char next(CharSequence charArray, int index) {
        if (index < charArray.length()) {
            return charArray.charAt(index);
        }
        throw new JsonStringParseException("End of file in JSON charArray.");
    }

}