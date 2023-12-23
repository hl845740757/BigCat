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

import cn.wjybxx.common.concurrent.FutureUtils;
import cn.wjybxx.common.ex.ErrorCodeException;

import java.util.concurrent.ExecutionException;
import java.util.concurrent.TimeoutException;

/**
 * 表示本地异常（一般需要填充堆栈）
 *
 * @author wjybxx
 * date 2023/4/1
 */
public class RpcClientException extends RpcException {

    public RpcClientException(int errorCode) {
        super(errorCode, "rpc client exception, code " + errorCode);
    }

    public RpcClientException(int errorCode, String message) {
        super(errorCode, message);
    }

    public RpcClientException(int errorCode, String message, Throwable cause) {
        super(errorCode, message, cause);
    }

    public RpcClientException(int errorCode, Throwable cause) {
        super(errorCode, cause);
    }

    public RpcClientException(int errorCode, String message, Throwable cause, boolean enableSuppression, boolean writableStackTrace) {
        super(errorCode, message, cause, enableSuppression, writableStackTrace);
    }

    // 静态工厂方法

    /** 异步调用的情况下，超时堆栈毫无益处，因此可共享该对象 */
    private static final RpcClientException TIMEOUT = new RpcClientException(RpcErrorCodes.LOCAL_TIMEOUT, "timeout", null, false, false);

    public static RpcClientException sendFailed(RpcAddr target) {
        return new RpcClientException(RpcErrorCodes.LOCAL_ROUTER_EXCEPTION, target + " unreachable", null, true, true);
    }

    public static RpcClientException timeout() {
        return TIMEOUT;
    }

    public static RpcClientException blockingTimeout(TimeoutException e) {
        return new RpcClientException(RpcErrorCodes.LOCAL_TIMEOUT, "blockingTimeout", e, true, true);
    }

    public static RpcClientException interrupted(InterruptedException e) {
        return new RpcClientException(RpcErrorCodes.LOCAL_INTERRUPTED, "interrupted", e, true, true);
    }

    public static RpcClientException unknownException(Throwable e) {
        return new RpcClientException(RpcErrorCodes.LOCAL_UNKNOWN_EXCEPTION, "unknownException", e, true, true);
    }

    public static RuntimeException wrapOrRethrow(Throwable e) {
        if (e instanceof ErrorCodeException || e instanceof RpcException) {
            return (RuntimeException) e;
        }
        if (e instanceof TimeoutException timeoutException) {
            return blockingTimeout(timeoutException);
        }
        if (e instanceof InterruptedException ie) {
            return interrupted(ie);
        }
        if (e instanceof ExecutionException) {
            return RpcClientException.unknownException(e.getCause());
        }
        e = FutureUtils.unwrapCompletionException(e);
        return unknownException(e);
    }

}