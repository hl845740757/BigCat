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

import it.unimi.dsi.fastutil.ints.IntArrayList;
import it.unimi.dsi.fastutil.ints.IntSet;
import org.apache.commons.codec.binary.Hex;
import org.apache.commons.lang3.exception.ExceptionUtils;
import org.apache.commons.lang3.math.NumberUtils;

import java.util.Set;

/**
 * Dson的文本表示法
 * 类似json但不是json
 *
 * @author wjybxx
 * date - 2023/6/2
 */
public class DsonTexts {

    // 类型标签
    public static final String LABEL_INT32 = "i";
    public static final String LABEL_INT64 = "L";
    public static final String LABEL_FLOAT = "f";
    public static final String LABEL_DOUBLE = "d";
    public static final String LABEL_BOOL = "b";
    public static final String LABEL_STRING = "s";
    public static final String LABEL_NULL = "N";

    public static final String LABEL_BINARY = "bin";
    public static final String LABEL_EXTINT32 = "ei";
    public static final String LABEL_EXTINT64 = "eL";
    public static final String LABEL_EXTSTRING = "es";
    public static final String LABEL_REFERENCE = "ref";

    /** 长文本，字符串不需要加引号，不对内容进行转义，可直接换行 */
    public static final String LABEL_TEXT = "ss";
    public static final String LABEL_ARRAY = "[";
    public static final String LABEL_OBJECT = "{";

    public static final Set<String> LABEL_SET = Set.of(
            LABEL_INT32, LABEL_INT64, LABEL_FLOAT, LABEL_DOUBLE,
            LABEL_BOOL, LABEL_NULL, LABEL_BINARY,
            LABEL_EXTINT32, LABEL_EXTINT64, LABEL_EXTSTRING, LABEL_REFERENCE,
            LABEL_TEXT, LABEL_ARRAY, LABEL_OBJECT);

    // 行首标签
    public static final String LHEAD_COMMENT = "#";
    public static final String LHEAD_APPEND_LINE = "--";
    public static final String LHEAD_APPEND = "-|";
    public static final String LHEAD_TEXT_APPEND_LINE = "->";
    public static final int CONTENT_LHEAD_LENGTH = 2;

    public static final Set<String> ALL_LHEAD_SET = Set.of(LHEAD_COMMENT, LHEAD_APPEND_LINE, LHEAD_APPEND, LHEAD_TEXT_APPEND_LINE);
    public static final Set<String> CONTENT_LHEAD_SET = Set.of(LHEAD_APPEND_LINE, LHEAD_APPEND, LHEAD_TEXT_APPEND_LINE);

    // 默认header名字
    public static final String CLASS_NAME = "clsName";
    public static final String CLASS_ID = "clsId";
    public static final String COMP_CLASS_NAME = "compClsName";
    public static final String COMP_CLASS_ID = "compClsId";
    public static final String IS_COMP_TEXT = "isCompText";
    public static final String LOCAL_ID = "localId";
    public static final String GUID = "guid";
    public static final String TAGS = "tags";

    /** 有特殊含义的字符串 */
    private static final Set<String> PARSABLE_STRINGS = Set.of("true", "false",
            "null", "undefine",
            "NaN", "Infinity", "-Infinity");

    /** 规定哪些不安全较为容易，规定哪些安全反而不容易 */
    private static final IntSet unsafeCharSet;

    static {
        char[] tokenCharArray = "{}[],:\"@\\".toCharArray();
        // 圆括号、单引号
        char[] reservedCharArray = "()'".toCharArray();
        IntArrayList intChars = new IntArrayList();
        addAll(intChars, tokenCharArray);
        addAll(intChars, reservedCharArray);
        unsafeCharSet = IntSet.of(intChars.toIntArray());
    }

    private static void addAll(IntArrayList src, char[] target) {
        for (char c : target) {
            src.add(c);
        }
    }

    /** 是否是不安全的字符，不能省略引号的字符 */
    public static boolean isUnsafeStringChar(int c) {
        return Character.isWhitespace(c) || unsafeCharSet.contains(c);
    }

    /**
     * 是否是安全字符，可以省略引号的字符
     * 注意：safeChar也可能组合出不安全的无引号字符串，比如：123, 0.5, null,true,false，
     * 因此不能因为每个字符安全，就认为整个字符串安全
     */
    public static boolean isSafeStringChar(int c) {
        return !Character.isWhitespace(c) && !unsafeCharSet.contains(c);
    }

    /**
     * 是否可省略字符串的引号
     * 其实并不建议底层默认判断是否可以不加引号，用户可以根据自己的数据决定是否加引号，比如；guid可能就是可以不加引号的
     * 这里的计算是保守的，保守一些不容易出错，因为情况太多，既难以保证正确性，性能也差
     */
    public static boolean canUnquoteString(String value) {
        if (value.isEmpty() || value.length() > 32) { // 长字符串都加引号，避免不必要的计算
            return false;
        }
        if (PARSABLE_STRINGS.contains(value)) { // 特殊字符串值
            return false;
        }
        // 这遍历的不是unicode码点，但不影响
        for (int i = 0; i < value.length(); i++) {
            char c = value.charAt(i);
            if (isUnsafeStringChar(c)) {
                return false;
            }
        }
        if (NumberUtils.isCreatable(value)) { // 可解析的数字类型，这个开销大放最后检测
            return false;
        }
        return true;
    }

    /** 是否是ASCII码中的可打印字符构成的文本 */
    public static boolean isASCIIText(String text) {
        for (int i = 0, len = text.length(); i < len; i++) {
            if (text.charAt(i) < 32 || text.charAt(i) > 126) {
                return false;
            }
        }
        return true;
    }

    /**
     * 现在操作系统的换行符只有: \r\n (windows) 和 \n (unix, mac)
     * 检查中途是否出现单独的 \r
     */
    public static void checkLRLF(CharSequence buffer, int bufferLength, int pos, char c) {
        if (c == '\r') {
            if (pos + 1 == bufferLength || buffer.charAt(pos + 1) != '\n') {
                throw new DsonParseException("invalid input. A separate \\r, \\r\\n or \\n is require, Position: " + pos);
            }
        }
    }

    // 封装一下，方便以后切换实现

    public static char[] encodeHex(byte[] data) {
        return Hex.encodeHex(data, false);
    }

    public static byte[] decodeHex(char[] data) {
        try {
            return Hex.decodeHex(data);
        } catch (Exception e) {
            return ExceptionUtils.rethrow(e);
        }
    }

    static Object checkNullString(String str) {
        if ("null".equals(str)) return null;
        throw new IllegalArgumentException("invalid null str: " + str);
    }

    static Object parseBool(String str) {
        if ("true".equals(str)) return Boolean.TRUE;
        if ("false".equals(str)) return Boolean.FALSE;
        throw new IllegalArgumentException("invalid bool str: " + str);
    }

    static int parseInt(String str) {
        if (str.startsWith("0x")) {
            return Integer.parseInt(str, 2, str.length(), 16);
        }
        if (str.startsWith("-0x")) {
            return Integer.parseInt(str, 3, str.length(), 16);
        }
        return Integer.parseInt(str);
    }

    static long parseLong(String str) {
        if (str.startsWith("0x")) {
            return Long.parseLong(str, 2, str.length(), 16);
        }
        if (str.startsWith("-0x")) {
            return Long.parseLong(str, 3, str.length(), 16);
        }
        return Long.parseLong(str);
    }

    static float parseFloat(String str) {
        return Float.parseFloat(str);
    }

    static double parseDouble(String str) {
        return Double.parseDouble(str);
    }

}