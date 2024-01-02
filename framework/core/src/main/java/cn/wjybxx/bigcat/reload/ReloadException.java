/*
 * Copyright 2023-2024 wjybxx(845740757@qq.com)
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

package cn.wjybxx.bigcat.reload;

/**
 * @author wjybxx
 * date - 2023/5/21
 */
public class ReloadException extends RuntimeException {

    public ReloadException() {
    }

    public ReloadException(String message) {
        super(message);
    }

    public ReloadException(String message, Throwable cause) {
        super(message, cause);
    }

    public ReloadException(Throwable cause) {
        super(cause);
    }

    public ReloadException(String message, Throwable cause, boolean enableSuppression, boolean writableStackTrace) {
        super(message, cause, enableSuppression, writableStackTrace);
    }

    public static ReloadException wrap(Throwable e) {
        if (e instanceof ReloadException reloadException) {
            return reloadException;
        }
        return new ReloadException(e);
    }

}