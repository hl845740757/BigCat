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

package cn.wjybxx.common.concurrent;

import cn.wjybxx.common.ex.NoLogRequiredException;

/**
 * 该异常表示{@link FutureCombiner}监听的任务数不足以到达成功条件
 *
 * @author wjybxx
 * date 2023/4/12
 */
public class TaskInsufficientException extends RuntimeException implements NoLogRequiredException {

    public TaskInsufficientException() {
    }

    public TaskInsufficientException(String message) {
        super(message);
    }

    public final Throwable fillInStackTrace() {
        return this;
    }

    public static TaskInsufficientException create(int futureCount, int doneCount, int succeedCount, int successRequire) {
        final String msg = String.format("futureCount :%d, doneCount %d, succeedCount: %d, successRequire :%d",
                futureCount, doneCount, succeedCount, successRequire);
        return new TaskInsufficientException(msg);
    }

}