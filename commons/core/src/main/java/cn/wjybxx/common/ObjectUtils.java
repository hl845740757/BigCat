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

import it.unimi.dsi.fastutil.chars.CharPredicate;
import org.apache.commons.lang3.StringUtils;

import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;
import java.util.Objects;
import java.util.stream.Collectors;

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

    public static <T extends CharSequence> T emptyToDef(T str, T def) {
        return isEmpty(str) ? def : str;
    }

    public static <T extends CharSequence> T blankToDef(T str, T def) {
        return isBlank(str) ? def : str;
    }

    //
    public static char firstChar(CharSequence value) {
        return value.charAt(0);
    }

    public static char lastChar(CharSequence value) {
        return value.charAt(value.length() - 1);
    }

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

    public static int indexOfNonWhitespace(CharSequence cs) {
        int length = length(cs);
        if (length == 0) {
            return -1;
        }
        for (int i = 0; i < length; i++) {
            if (!Character.isWhitespace(cs.charAt(i))) {
                return i;
            }
        }
        return -1;
    }

    public static int lastIndexOfNonWhitespace(CharSequence cs) {
        int length = length(cs);
        if (length == 0) {
            return -1;
        }
        for (int i = length - 1; i >= 0; i--) {
            if (!Character.isWhitespace(cs.charAt(i))) {
                return i;
            }
        }
        return -1;
    }

    public static int indexOf(CharSequence cs, CharPredicate predicate) {
        int length = length(cs);
        if (length == 0) {
            return -1;
        }
        for (int i = 0; i < length; i++) {
            if (predicate.test(cs.charAt(i))) {
                return i;
            }
        }
        return -1;
    }

    public static int lastIndexOf(CharSequence cs, CharPredicate predicate) {
        int length = length(cs);
        if (length == 0) {
            return -1;
        }
        for (int i = length - 1; i >= 0; i--) {
            if (predicate.test(cs.charAt(i))) {
                return i;
            }
        }
        return -1;
    }

    /** 查找第一个非给定char的元素的位置 */
    public static int indexOfNot(CharSequence cs, char c) {
        int length = length(cs);
        if (length == 0) {
            return -1;
        }
        for (int i = 0; i < length; i++) {
            if (cs.charAt(i) != c) {
                return i;
            }
        }
        return -1;
    }

    /** 查找最后一个非给定char的元素位置 */
    public static int lastIndexOfNot(CharSequence cs, char c) {
        int length = length(cs);
        if (length == 0) {
            return -1;
        }
        for (int i = length - 1; i >= 0; i--) {
            if (cs.charAt(i) != c) {
                return i;
            }
        }
        return -1;
    }

    public static List<String> toLines(String str) {
        if (isEmpty(str)) {
            return new ArrayList<>();
        }
        return str.lines()
                .collect(Collectors.toList());
    }

    // endregion

}