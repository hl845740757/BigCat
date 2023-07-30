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

package cn.wjybxx.common.log;

import org.slf4j.event.Level;

/**
 * debug日志级别（没有使用而枚举是故意的）。
 * 注意：通常我们开启这里的日志，表示我们期望一定输出日志。
 * 因此调用{@link org.slf4j.Logger}的方法时并不是它的{@link Level#DEBUG}，而是{@link Level#INFO}.
 *
 * @author wjybxx
 * date 2023/4/1
 */
public class DebugLogLevel {

    /** 不打印日志 */
    public static final int NONE = 0;
    /** 打印简单日志 */
    public static final int SIMPLE = 1;
    /** 打印详细日志 */
    public static final int DETAIL = 2;

    public static int checkedLevel(int level) {
        if (level < NONE) {
            return NONE;
        }
        if (level > DETAIL) {
            return DETAIL;
        }
        return level;
    }
}
