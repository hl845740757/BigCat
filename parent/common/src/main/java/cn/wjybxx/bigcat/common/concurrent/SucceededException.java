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

package cn.wjybxx.bigcat.common.concurrent;

/**
 * 该异常用于周期性任务主动退出
 * 这有利于我们封装周期性任务实现一些有用的功能
 *
 * @author wjybxx
 * date 2023/4/9
 */
@SuppressWarnings("unused")
public final class SucceededException extends RuntimeException implements NoLogRequiredException {

    private static final SucceededException INSTANCE = new SucceededException(null);

    private final Object result;

    public SucceededException() {
        this.result = null;
    }

    private SucceededException(Object result) {
        this.result = result;
    }

    public final Throwable fillInStackTrace() {
        return this;
    }

    public Object getResult() {
        return result;
    }
}