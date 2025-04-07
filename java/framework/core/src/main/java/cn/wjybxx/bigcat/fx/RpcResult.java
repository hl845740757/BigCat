/*
 * Copyright 2023-2025 wjybxx(845740757@qq.com)
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

/**
 * 该接口用于解除同步调用对{@link RpcResponse}的依赖
 *
 * @author wjybxx
 * date - 2025/4/6
 */
public final class RpcResult {

    private final int errorCode;
    private final Object data;

    /**
     * @param errorCode 错误码，0表示成功
     * @param data      方法结果，错误码非0时为字符串类型
     */
    public RpcResult(int errorCode, Object data) {
        this.errorCode = errorCode;
        this.data = data;
    }

    /** 结果转String，只有失败的情况下可调用 */
    public String getErrorMsg() {
        if (errorCode == 0) {
            throw new IllegalStateException("errorCode == 0");
        }
        return (String) data;
    }

    /** 是否成功 */
    public boolean isSucceeded() {
        return errorCode == 0;
    }

    /** 是否失败 */
    public boolean isFailed() {
        return errorCode != 0;
    }

    // region getter/setter

    public int getErrorCode() {
        return errorCode;
    }

    public Object getData() {
        return data;
    }

    // endregion
}