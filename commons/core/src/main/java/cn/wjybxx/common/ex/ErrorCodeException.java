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

package cn.wjybxx.common.ex;

/**
 * 用于返回一个错误码结果，Rpc底层会对此做特殊支持
 *
 * @author wjybxx
 * date - 2023/9/12
 */
public final class ErrorCodeException extends RuntimeException implements NoLogRequiredException {

    private final int errorCode;

    public ErrorCodeException(int errorCode, String message) {
        super(message, null, false, false); // 不写堆栈
        this.errorCode = errorCode;
    }

    public int getErrorCode() {
        return errorCode;
    }

}