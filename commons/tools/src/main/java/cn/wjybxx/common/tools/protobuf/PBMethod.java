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

package cn.wjybxx.common.tools.protobuf;

import javax.annotation.Nonnull;

/**
 * protobuf中的rpc方法
 *
 * @author wjybxx
 * date - 2023/9/27
 */
public class PBMethod extends PBElement {

    /** 普通模式 */
    public static final int MODE_NORMAL = 0;
    /** future异步模式 -- 返回的返回值修正为{@code CompletionStage<V>} */
    public static final int MODE_FUTURE = 1;
    /** context异步模式 -- 在方法参数中添加{@code RpcContext<V>} */
    public static final int MODE_CONTEXT = 2;

    /** 方法参数的类型 */
    private String argType;
    /** 方法参数的名字 -- 默认值由parser赋值 */
    private String argName;
    /** 方法返回值的类型 */
    private String resultType;

    /** 方法id -- 从注解中获得的缓存值 */
    private int methodId;
    /** 方法的执行模式，如果文件中未定义，则使用默认的模式（解析器中配置） */
    private int mode;
    /** 非context模式下，是否在方法参数中追加{@code RpcGenericContext}参数 */
    private boolean ctx = false;

    //

    @Nonnull
    @Override
    public PBElementKind getKind() {
        return PBElementKind.METHOD;
    }

    public boolean isFutureMode() {
        return mode == MODE_FUTURE;
    }

    public boolean isContextMode() {
        return mode == MODE_CONTEXT;
    }

    //

    public String getArgType() {
        return argType;
    }

    public PBMethod setArgType(String argType) {
        this.argType = argType;
        return this;
    }

    public String getResultType() {
        return resultType;
    }

    public PBMethod setResultType(String resultType) {
        this.resultType = resultType;
        return this;
    }

    public String getArgName() {
        return argName;
    }

    public PBMethod setArgName(String argName) {
        this.argName = argName;
        return this;
    }

    public int getMethodId() {
        return methodId;
    }

    public PBMethod setMethodId(int methodId) {
        this.methodId = methodId;
        return this;
    }

    public int getMode() {
        return mode;
    }

    public PBMethod setMode(int mode) {
        this.mode = mode;
        return this;
    }

    public boolean isCtx() {
        return ctx;
    }

    public PBMethod setCtx(boolean ctx) {
        this.ctx = ctx;
        return this;
    }

    @Override
    protected void toString(StringBuilder sb) {
        sb.append(", argType='").append(argType).append('\'')
                .append(", argName='").append(argName).append('\'')
                .append(", resultType='").append(resultType).append('\'')
                .append(", methodId=").append(methodId)
                .append(", mode=").append(mode)
                .append(", ctx=").append(ctx);

    }
}