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

package cn.wjybxx.common.rpc;

import cn.wjybxx.common.ex.ErrorCodeException;

/**
 * 表示服务器的异常
 *
 * @author wjybxx
 * date 2023/4/1
 */
public class RpcServerException extends RpcException {

    public RpcServerException(int errorCode, String message) {
        super(errorCode, message, null, true, false);
    }

    @Override
    public synchronized Throwable fillInStackTrace() {
        // 不填充堆栈，没有意义（因为错误信息是远端的）
        return this;
    }

    public static RuntimeException newServerException(RpcResponse response) {
        int errorCode = response.getErrorCode();
        if (RpcErrorCodes.isUserCode(errorCode)) {
            return new ErrorCodeException(errorCode, response.getErrorMsg());
        } else {
            return new RpcServerException(errorCode, response.getErrorMsg());
        }
    }
}