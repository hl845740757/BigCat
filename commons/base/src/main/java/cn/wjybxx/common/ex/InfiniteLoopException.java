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

package cn.wjybxx.common.ex;

/**
 * 如果抛出该类型异常，则表示可能产生无限循环（死循环）风险
 *
 * @author wjybxx
 * date 2023/4/1
 */
public class InfiniteLoopException extends RuntimeException {

    public InfiniteLoopException() {
    }

    public InfiniteLoopException(String message) {
        super(message);
    }

    public InfiniteLoopException(String message, Throwable cause) {
        super(message, cause);
    }

    public InfiniteLoopException(Throwable cause) {
        super(cause);
    }

    public InfiniteLoopException(String message, Throwable cause, boolean enableSuppression, boolean writableStackTrace) {
        super(message, cause, enableSuppression, writableStackTrace);
    }
}