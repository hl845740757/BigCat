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

package cn.wjybxx.common;

import java.util.Arrays;
import java.util.Objects;

/**
 * 一些基础的扩展
 *
 * @author wjybxx
 * date - 2023/4/17
 */
public class ObjectUtils {

    /**
     * 如果给定参数为null，则返回给定的默认值，否则返回值本身
     * {@link Objects#requireNonNullElse(Object, Object)}不允许def为null
     */
    public static <V> V nullToDef(V obj, V def) {
        return obj == null ? def : obj;
    }

    // region equals/hash/toString

    public static int hashCode(Object first) {
        return Objects.hashCode(first);
    }

    public static int hashCode(Object first, Object second) {
        int result = Objects.hashCode(first);
        result = 31 * result + Objects.hashCode(second);
        return result;
    }

    public static int hashCode(Object first, Object second, Object third) {
        int result = Objects.hashCode(first);
        result = 31 * result + Objects.hashCode(second);
        result = 31 * result + Objects.hashCode(third);
        return result;
    }

    public static int hashCode(Object... args) {
        return Arrays.hashCode(args);
    }

    public static String toString(Object object, String nullDef) {
        return object == null ? nullDef : object.toString();
    }

    public static String toStringIfNotNull(Object object) {
        return object == null ? null : object.toString();
    }

    // endregion

    // region string
    // 没打算造一个StringUtils

    public static int length(CharSequence cs) {
        return cs == null ? 0 : cs.length();
    }

    public static boolean isEmpty(CharSequence cs) {
        return cs == null || cs.isEmpty();
    }

    public static boolean isBlank(CharSequence cs) {
        final int strLen = length(cs);
        if (strLen == 0) {
            return true;
        }
        for (int i = 0; i < strLen; i++) {
            if (!Character.isWhitespace(cs.charAt(i))) {
                return false;
            }
        }
        return true;
    }

    public static <T extends CharSequence> T emptyToDef(T str, T def) {
        return isEmpty(str) ? def : str;
    }

    public static <T extends CharSequence> T blankToDef(T str, T def) {
        return isBlank(str) ? def : str;
    }

    public static char lastChar(CharSequence value) {
        return value.charAt(value.length() - 1);
    }

    /** 首字母大写 */
    public static String firstCharToUpperCase(String str) {
        int length = length(str);
        if (length == 0) {
            return str;
        }
        char firstChar = str.charAt(0);
        if (Character.isLowerCase(firstChar)) { // 可拦截非英文字符
            StringBuilder sb = new StringBuilder(str);
            sb.setCharAt(0, Character.toUpperCase(firstChar));
            return sb.toString();
        }
        return str;
    }

    /** 首字母小写 */
    public static String firstCharToLowerCase(String str) {
        int length = length(str);
        if (length == 0) {
            return str;
        }
        char firstChar = str.charAt(0);
        if (Character.isUpperCase(firstChar)) { // 可拦截非英文字符
            StringBuilder sb = new StringBuilder(str);
            sb.setCharAt(0, Character.toLowerCase(firstChar));
            return sb.toString();
        }
        return str;
    }

    // region 空白字符

    /** 是否包含不可见字符 */
    public static boolean containsWhitespace(final CharSequence cs) {
        final int strLen = length(cs);
        if (strLen == 0) {
            return false;
        }
        for (int i = 0; i < strLen; i++) {
            if (Character.isWhitespace(cs.charAt(i))) {
                return true;
            }
        }
        return false;
    }

    /**
     * 查找首个非空白字符
     *
     * @return 如果不存在则返回-1
     */
    public static int firstCharNonWhitespace(CharSequence cs) {
        int length = length(cs);
        if (length == 0) {
            return -1;
        }
        for (int i = 0; i < length; i++) {
            char c = cs.charAt(i);
            if (!Character.isWhitespace(c)) {
                return c;
            }
        }
        return -1;
    }

    /**
     * 查找最后一个非空白字符
     *
     * @return 如果不存在则返回-1
     */
    public static int lastCharNonWhitespace(CharSequence cs) {
        int length = length(cs);
        if (length == 0) {
            return -1;
        }
        for (int i = length - 1; i >= 0; i--) {
            char c = cs.charAt(i);
            if (!Character.isWhitespace(c)) {
                return c;
            }
        }
        return -1;
    }

    /** 索引首个非空白字符的 */
    public static int indexOfNonWhitespace(CharSequence cs) {
        return indexOfNonWhitespace(cs, 0);
    }

    /** 索引首个非空白字符 */
    public static int indexOfNonWhitespace(CharSequence cs, final int startIndex) {
        if (startIndex < 0) {
            throw new IllegalArgumentException("startIndex " + startIndex);
        }

        int length = length(cs);
        if (length == 0) {
            return -1;
        }
        for (int i = startIndex; i < length; i++) {
            if (!Character.isWhitespace(cs.charAt(i))) {
                return i;
            }
        }
        return -1;
    }

    /** 逆向索引首个非空白字符 */
    public static int lastIndexOfNonWhitespace(CharSequence cs) {
        return lastIndexOfNonWhitespace(cs, -1);
    }

    /**
     * 逆向索引首个非空白字符
     *
     * @param startIndex 开始下标，-1表示从最后一个字符开始
     * @return -1表示查找失败
     */
    public static int lastIndexOfNonWhitespace(CharSequence cs, int startIndex) {
        if (startIndex < -1) {
            throw new IllegalArgumentException("startIndex " + startIndex);
        }

        int length = length(cs);
        if (length == 0) {
            return -1;
        }

        if (startIndex == -1) {
            startIndex = length - 1;
        } else if (startIndex >= length) {
            startIndex = length - 1;
        }

        for (int i = startIndex; i >= 0; i--) {
            if (!Character.isWhitespace(cs.charAt(i))) {
                return i;
            }
        }
        return -1;
    }
    // endregion

    // endregion

}