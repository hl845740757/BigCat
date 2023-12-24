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

package cn.wjybxx.bigcat.rpc;

/**
 * rpc异常的超类
 *
 * @author wjybxx
 * date 2023/4/1
 */
public abstract class RpcException extends RuntimeException {

    private final int errorCode;

    public RpcException(int errorCode) {
        this.errorCode = errorCode;
    }

    public RpcException(int errorCode, String message) {
        super(message);
        this.errorCode = errorCode;
    }

    public RpcException(int errorCode, String message, Throwable cause) {
        super(message, cause);
        this.errorCode = errorCode;
    }

    public RpcException(int errorCode, Throwable cause) {
        super(cause);
        this.errorCode = errorCode;
    }

    public RpcException(int errorCode, String message, Throwable cause, boolean enableSuppression, boolean writableStackTrace) {
        super(message, cause, enableSuppression, writableStackTrace);
        this.errorCode = errorCode;
    }

    /**
     * @return 返回异常对应的错误码
     */
    public final int getErrorCode() {
        return errorCode;
    }

    public final boolean isClientException() {
        return this instanceof RpcClientException;
    }

    public final boolean isServerException() {
        return this instanceof RpcServerException;
    }

}