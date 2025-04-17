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

import cn.wjybxx.bigcat.pb.ProtobufUtils;
import com.google.protobuf.Message;
import com.google.protobuf.MessageLite;
import com.google.protobuf.Parser;

import java.util.Objects;

/**
 * Rpc方法信息
 * (本地用)
 *
 * @param <T> 方法参数类型，{@link Void}表示无
 * @param <R> 方法结果类型，{@link Void}表示无
 * @author wjybxx
 * date - 2023/10/12
 */
public final class RpcMethodInfo<T, R> {

    /** 服务名 -- 本地debug用，不参与equals比较 */
    public final String serviceName;
    /** 方法名 -- 本地debug用 */
    public final String methodName;

    /** 服务id */
    public final int serviceId;
    /** 方法id */
    public final int methodId;
    /** 方法参数类型 -- 无参数时为null */
    public final Class<T> parameterType;
    /** 方法结果类型 -- 无结果时为null */
    public final Class<R> resultType;

    // pb特殊支持
    /** 不为null则表示参数为pb类型，不参与equals比较 */
    public final Parser<T> parameterParser;
    /** 不为null则表示结果为pb类型 */
    public final Parser<R> resultParser;

    public RpcMethodInfo(String serviceName, String methodName,
                         int serviceId, int methodId,
                         Class<T> parameterType,
                         Class<R> resultType) {
        this.serviceName = Objects.requireNonNull(serviceName);
        this.methodName = Objects.requireNonNull(methodName);
        this.serviceId = serviceId;
        this.methodId = methodId;
        this.parameterType = voidToNull(parameterType);
        this.resultType = voidToNull(resultType);

        this.parameterParser = findParser(this.parameterType);
        this.resultParser = findParser(this.resultType);
    }

    /** 方法是否有参数 */
    public boolean hasParameter() {
        return parameterType != null;
    }

    /** 方法是否有结果 */
    public boolean hasResult() {
        return resultType != null;
    }
    // region util

    private static <T> Class<T> voidToNull(Class<T> clazz) {
        if (clazz == null || clazz == Void.class || clazz == void.class) {
            return null;
        }
        return clazz;
    }

    @SuppressWarnings("unchecked")
    private static <T> Parser<T> findParser(Class<T> clazz) {
        if (clazz == null) {
            return null;
        }
        if (!Message.class.isAssignableFrom(clazz)) {
            return null;
        }
        Class<? extends MessageLite> msgClazz = (Class<? extends MessageLite>) clazz;
        return (Parser<T>) ProtobufUtils.findParser(msgClazz);
    }
    // endregion

    // region equals
    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        RpcMethodInfo<?, ?> that = (RpcMethodInfo<?, ?>) o;
        return serviceId == that.serviceId
                && methodId == that.methodId
                && parameterType == that.parameterType
                && resultType == that.resultType;
    }

    @Override
    public int hashCode() {
        int result = serviceId;
        result = 31 * result + methodId;
        result = 31 * result + Objects.hashCode(parameterType);
        result = 31 * result + Objects.hashCode(resultType);
        return result;
    }

    @Override
    public String toString() {
        return "RpcMethodInfo{" +
                "serviceName='" + serviceName + '\'' +
                ", methodName='" + methodName + '\'' +
                ", serviceId=" + serviceId +
                ", methodId=" + methodId +
                ", parameterType=" + parameterType +
                ", resultType=" + resultType +
                '}';
    }
    // endregion


}