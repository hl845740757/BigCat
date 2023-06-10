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
     * {@link java.util.Objects#requireNonNullElse(Object, Object)}不允许def为null
     */
    public static <V> V nullToDef(V obj, V def) {
        return obj == null ? def : obj;
    }

    // region equals/hash/toString

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

    public static String toStringIfNotNull(Object message) {
        return message == null ? null : String.valueOf(message);
    }

    // endregion

    // region string
    // 没打算造一个StringUtils

    public static String nullToEmpty(String str) {
        return str == null ? "" : str;
    }

    public static String emptyToNull(String str) {
        return (str == null || str.isEmpty()) ? null : str;
    }

    public static boolean isEmpty(String value) {
        return value == null || value.isEmpty();
    }

    public static boolean isBlank(String value) {
        return value == null || value.isBlank();
    }

    public static char firstChar(String value) {
        return value.charAt(0);
    }

    public static char lastChar(String value) {
        return value.charAt(value.length() - 1);
    }

    // endregion

}