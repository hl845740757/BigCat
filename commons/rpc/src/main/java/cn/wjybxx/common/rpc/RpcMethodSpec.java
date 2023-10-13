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


import cn.wjybxx.common.codec.AutoSchema;
import cn.wjybxx.common.codec.binary.BinarySerializable;
import cn.wjybxx.common.log.DebugLogFriendlyObject;

import javax.annotation.Nonnull;
import java.util.List;
import java.util.Objects;

/**
 * rpc方法描述信息
 * 1. 使用int类型的serviceId和methodId，可以大大减少数据传输量，而且定位方法更快
 * 2. 不记录方法参数类型，没有太大的意义
 *
 * @param <V> 用于捕获返回值类型
 * @author wjybxx
 * date 2023/4/1
 */
@SuppressWarnings("unused")
@AutoSchema
@BinarySerializable
public final class RpcMethodSpec<V> implements DebugLogFriendlyObject {

    private int serviceId;
    private int methodId;
    private List<Object> parameters; // 要省开销的化，可以是Object类型，当方法参数大于1才扩展为List
    private transient boolean sharable;

    public RpcMethodSpec() {
        // 用于可能的序列化支持
    }

    public RpcMethodSpec(int serviceId, int methodId, List<Object> parameters) {
        this.serviceId = serviceId;
        this.methodId = methodId;
        this.parameters = Objects.requireNonNull(parameters);
    }

    public RpcMethodSpec(int serviceId, int methodId, List<Object> parameters, boolean sharable) {
        this.serviceId = serviceId;
        this.methodId = methodId;
        this.parameters = parameters;
        this.sharable = sharable;
    }

    public int getServiceId() {
        return serviceId;
    }

    public void setServiceId(int serviceId) {
        this.serviceId = serviceId;
    }

    public int getMethodId() {
        return methodId;
    }

    public void setMethodId(int methodId) {
        this.methodId = methodId;
    }

    public List<Object> getParameters() {
        return parameters;
    }

    public void setParameters(List<Object> parameters) {
        this.parameters = parameters;
    }

    public boolean isSharable() {
        return sharable;
    }

    public RpcMethodSpec<V> setSharable(boolean sharable) {
        this.sharable = sharable;
        return this;
    }

    // region 简化生成代码

    public int getInt(int index) {
        Number number = (Number) parameters.get(index);
        return number.intValue();
    }

    public long getLong(int index) {
        Number number = (Number) parameters.get(index);
        return number.longValue();
    }

    public float getFloat(int index) {
        Number number = (Number) parameters.get(index);
        return number.floatValue();
    }

    public double getDouble(int index) {
        Number number = (Number) parameters.get(index);
        return number.doubleValue();
    }

    public boolean getBoolean(int index) {
        return (Boolean) parameters.get(index);
    }

    public String getString(int index) {
        return (String) parameters.get(index);
    }

    public Object getObject(int index) {
        return parameters.get(index);
    }

    public void setObject(int index, Object arg) {
        parameters.set(index, arg);
    }

    // endregion

    @Nonnull
    @Override
    public String toSimpleLog() {
        return '{' +
                "serviceId=" + serviceId +
                ", methodId=" + methodId +
                ", paramCount=" + parameters.size() +
                "}";
    }

    @Nonnull
    @Override
    public String toDetailLog() {
        return '{' + "serviceId=" + serviceId +
                ", methodId=" + methodId +
                ", paramCount=" + parameters.size() +
                ", params=" + parameters +
                "}";
    }

    @Override
    public String toString() {
        return "DefaultRpcMethodSpec{"
                + "serviceId=" + serviceId
                + ", methodId=" + methodId
                + ", methodParams=" + parameters
                + '}';
    }

}