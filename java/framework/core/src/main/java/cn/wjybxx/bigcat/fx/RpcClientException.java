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

package cn.wjybxx.bigcat.fx;

import cn.wjybxx.concurrent.ExecutorUtils;

import java.util.concurrent.ExecutionException;

/**
 * 表示本地异常（一般需要填充堆栈）
 * <p>
 * PS：不能在{@link #fillInStackTrace()}中测试错误码，因为方法是在构造函数中调用的。
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

    /** 超时 -- 不需要填充堆栈，意义不大 */
    public static RpcClientException timeout(WorkerAddr destAddr) {
        return new RpcClientException(RpcErrorCodes.LOCAL_TIMEOUT, "destAddr: " + destAddr, null, true, false);
    }

    /** session不存在 -- 不需要填充堆栈，意义不大 */
    public static RpcClientException sessionNotExist(WorkerAddr destAddr) {
        return new RpcClientException(RpcErrorCodes.LOCAL_SESSION_NOT_EXIST, "destAddr: " + destAddr, null, true, false);
    }

    /** session关闭 -- 不需要填充堆栈，意义不大 */
    public static RpcClientException sessionClosed(WorkerAddr destAddr) {
        return new RpcClientException(RpcErrorCodes.LOCAL_SESSION_CLOSED, "destAddr: " + destAddr, null, true, false);
    }

    /** 未知异常 */
    public static RpcClientException unknownException(Throwable ex) {
        if (ex instanceof ExecutionException) {
            ex = ex.getCause();
        } else {
            ex = ExecutorUtils.unwrapCompletionException(ex);
        }
        return new RpcClientException(RpcErrorCodes.LOCAL_UNKNOWN_EXCEPTION, "unknownException", ex);
    }

}