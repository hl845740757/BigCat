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

package cn.wjybxx.common.tools.util;

import cn.wjybxx.common.ObjectUtils;
import it.unimi.dsi.fastutil.chars.CharPredicate;

import java.io.BufferedReader;
import java.io.File;
import java.io.IOException;
import java.io.InputStreamReader;

/**
 * @author wjybxx
 * date - 2023/10/9
 */
public class Utils extends ObjectUtils {

    // region 空白字符

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

    // region 字符查找

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

    public static int indexOf(CharSequence cs, CharPredicate predicate) {
        return indexOf(cs, predicate, 0);
    }

    public static int indexOf(CharSequence cs, CharPredicate predicate, final int startIndex) {
        if (startIndex < 0) {
            throw new IllegalArgumentException("startIndex " + startIndex);
        }

        int length = length(cs);
        if (length == 0) {
            return -1;
        }

        for (int i = startIndex; i < length; i++) {
            if (predicate.test(cs.charAt(i))) {
                return i;
            }
        }
        return -1;
    }

    public static int lastIndexOf(CharSequence cs, CharPredicate predicate) {
        return lastIndexOf(cs, predicate, -1);
    }

    /**
     * @param startIndex 开始下标，-1表示从最后一个字符开始
     * @return -1表示查找失败
     */
    public static int lastIndexOf(CharSequence cs, CharPredicate predicate, int startIndex) {
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
            if (predicate.test(cs.charAt(i))) {
                return i;
            }
        }
        return -1;
    }

    // endregion

    /** 去除字符串的双引号 */
    public static String unquote(String str) {
        int length = ObjectUtils.length(str);
        if (length == 0) {
            return str;
        }
        char firstChar = str.charAt(0);
        char lastChar = str.charAt(str.length() - 1);
        if (firstChar == '"' && lastChar == '"') {
            return str.substring(1, str.length() - 1);
        }
        return str;
    }

    /** 给字符串添加双引号，若之前无双引号 */
    public static String quote(String str) {
        if (str == null) {
            return null;
        }
        if (str.isEmpty()) {
            return "\"\"";
        }
        char firstChar = str.charAt(0);
        char lastChar = str.charAt(str.length() - 1);
        if (firstChar == '"' && lastChar == '"') {
            return str;
        }
        return '"' + str + '"';
    }

    /** 读取另一个进程的输出 */
    public static StringBuilder readProcessOutput(Process process) throws IOException {
        // 另一个进程的输出，对于当前进程而言就是可读取的（InputStream）
        // 另一个进程的输入，对于当前进程而言就是可写入的（OutputStream）
        StringBuilder sb = new StringBuilder(1024);
        try (BufferedReader reader = new BufferedReader(new InputStreamReader(process.getInputStream()))) {
            String line = reader.readLine();
            if (line != null) {
                if (sb.length() > 0) {
                    sb.append('\n');
                }
                sb.append(line);
            }
        }
        return sb;
    }
    // region file

    /** 获取当前工作目录 */
    public static File getUserWorkerDir() {
        return new File(System.getProperty("user.dir"));
    }

    /**
     * 查找项目的根目录
     * 1.可避免WorkDir不同，导致的资源定位问题，尤其是单元测试（Main方法和Junit就存在不同）
     * 2.可以统一API
     */
    public static File findProjectDir(String projectName) {
        File currentDir = getUserWorkerDir();
        do {
            if (currentDir.getName().equalsIgnoreCase(projectName)) {
                return currentDir;
            }
        } while ((currentDir = currentDir.getParentFile()) != null);

        throw new IllegalArgumentException("invalid projectName: " + projectName);
    }

    // endregion

}