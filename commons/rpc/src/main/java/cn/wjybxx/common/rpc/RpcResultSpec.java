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
import org.apache.commons.lang3.exception.ExceptionUtils;

import java.util.List;

/**
 * rpc执行结果描述
 * 1.该对象为临时对象，不序列化
 *
 * @author wjybxx
 * date - 2023/10/28
 */
public final class RpcResultSpec {

    private int errorCode;
    private List<Object> results;

    /** 结果对象是否可共享 */
    private transient boolean sharable;

    public RpcResultSpec() {
    }

    public RpcResultSpec(int errorCode, List<Object> results) {
        this(errorCode, results, false);
    }

    public RpcResultSpec(int errorCode, List<Object> results, boolean sharable) {
        this.errorCode = errorCode;
        this.results = results;
        this.sharable = sharable;
    }

    // region 业务方法

    /** 没有返回值 */
    public void succeeded() {
        errorCode = 0;
        results = List.of();
    }

    public void succeeded(Object result) {
        errorCode = 0;
        results = List.of(result);
    }

    public void failed(int errorCode, String msg) {
        if (errorCode == 0) throw new IllegalArgumentException("invalid errorCode " + errorCode);
        results = List.of(msg);
    }

    public void failed(Throwable e) {
        e = FutureUtils.unwrapCompletionException(e);
        if (e instanceof ErrorCodeException codeException) {
            failed(codeException.getErrorCode(), codeException.getMessage());
        } else if (e instanceof RpcException rpcException) {
            failed(rpcException.getErrorCode(), rpcException.getMessage());
        } else {
            failed(RpcErrorCodes.SERVER_EXCEPTION, ExceptionUtils.getMessage(e));
        }
    }

    public static RpcResultSpec newSucceedResult(Object result) {
        RpcResultSpec resultSpec = new RpcResultSpec();
        resultSpec.succeeded(result);
        return resultSpec;
    }

    public static RpcResultSpec newFailedResult(int errorCode, String msg) {
        RpcResultSpec resultSpec = new RpcResultSpec();
        resultSpec.failed(errorCode, msg);
        return resultSpec;
    }

    // endregion

    public int getErrorCode() {
        return errorCode;
    }

    public RpcResultSpec setErrorCode(int errorCode) {
        this.errorCode = errorCode;
        return this;
    }

    public List<Object> getResults() {
        return results;
    }

    public RpcResultSpec setResults(List<Object> results) {
        this.results = results;
        return this;
    }

    public boolean isSharable() {
        return sharable;
    }

    public RpcResultSpec setSharable(boolean sharable) {
        this.sharable = sharable;
        return this;
    }

    @Override
    public String toString() {
        return "RpcResultSpec{" +
                "errorCode=" + errorCode +
                ", results=" + results +
                ", sharable=" + sharable +
                '}';
    }
}
