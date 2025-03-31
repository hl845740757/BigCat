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

package cn.wjybxx.bigcattools.protobuf;

import cn.wjybxx.base.ObjectUtils;

import javax.annotation.Nonnull;

/**
 * protobuf中的rpc方法
 *
 * @author wjybxx
 * date - 2023/9/27
 */
public class PBMethod extends PBElement {

    /** 方法参数的类型 */
    private String parameterType;
    /** 方法参数的名字 -- 默认值由parser赋值 */
    private String parameterName;
    /** 方法返回值的类型 */
    private String resultType;

    /** 方法id -- 从注解中获得的缓存值 */
    private int methodId;
    /** 服务器是否是异步方法 -- 默认值由parser赋值 */
    private boolean async;
    /** 是否在方法参数中追加{@code RpcContext}参数 */
    private boolean appendCtx = false;
    /** 是否手动返回结果 */
    private boolean manual = false;
    /** 是否启用建造者模式 -- 默认值由解析器配置 */
    private boolean builderPattern;
    //

    @Nonnull
    @Override
    public PBElementKind getKind() {
        return PBElementKind.METHOD;
    }

    public boolean hasParameter() {
        return !ObjectUtils.isEmpty(parameterType);
    }

    public boolean hasResult() {
        return !ObjectUtils.isEmpty(resultType);
    }

    //

    public String getParameterType() {
        return parameterType;
    }

    public PBMethod setParameterType(String parameterType) {
        this.parameterType = parameterType;
        return this;
    }

    public String getResultType() {
        return resultType;
    }

    public PBMethod setResultType(String resultType) {
        this.resultType = resultType;
        return this;
    }

    public String getParameterName() {
        return parameterName;
    }

    public PBMethod setParameterName(String parameterName) {
        this.parameterName = parameterName;
        return this;
    }

    public int getMethodId() {
        return methodId;
    }

    public PBMethod setMethodId(int methodId) {
        this.methodId = methodId;
        return this;
    }

    public boolean isAsync() {
        return async;
    }

    public PBMethod setAsync(boolean async) {
        this.async = async;
        return this;
    }

    public boolean isAppendCtx() {
        return appendCtx;
    }

    public PBMethod setAppendCtx(boolean appendCtx) {
        this.appendCtx = appendCtx;
        return this;
    }

    public boolean isManual() {
        return manual;
    }

    public PBMethod setManual(boolean manual) {
        this.manual = manual;
        return this;
    }

    public boolean isBuilderPattern() {
        return builderPattern;
    }

    public PBMethod setBuilderPattern(boolean builderPattern) {
        this.builderPattern = builderPattern;
        return this;
    }

    @Override
    protected void toString(StringBuilder sb) {
        sb.append(", parameterType='").append(parameterType).append('\'')
                .append(", parameterName='").append(parameterName).append('\'')
                .append(", resultType='").append(resultType).append('\'')
                .append(", methodId=").append(methodId)
                .append(", mode=").append(async)
                .append(", appendCtx=").append(appendCtx)
                .append(", manual=").append(manual)
                .append(", builderPattern=").append(builderPattern);
    }
}