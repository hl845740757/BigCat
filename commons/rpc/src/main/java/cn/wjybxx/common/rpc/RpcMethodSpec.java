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


import cn.wjybxx.common.Bits;

import java.util.List;
import java.util.Objects;

/**
 * rpc方法描述信息
 * 1.不记录方法参数类型，没有太大的意义
 * 2.该对象为临时对象，不序列化
 *
 * @param <V> 用于捕获返回值类型
 * @author wjybxx
 * date 2023/4/1
 */
@SuppressWarnings("unused")
public final class RpcMethodSpec<V> {

    private transient int serviceId;
    private transient int methodId;
    private List<Object> parameters;

    /** 临时控制标记 */
    private transient int ctl;

    public RpcMethodSpec(int serviceId, int methodId, List<Object> parameters) {
        this(serviceId, methodId, parameters, false);
    }

    public RpcMethodSpec(int serviceId, int methodId, List<Object> parameters, boolean sharable) {
        this.serviceId = serviceId;
        this.methodId = methodId;
        this.parameters = Objects.requireNonNull(parameters);
        if (sharable) {
            ctl |= RpcProtocol.MASK_SHARABLE;
        }
    }

    /** 方法参数是否可共享 */
    public boolean isSharable() {
        return (ctl & RpcProtocol.MASK_SHARABLE) != 0;
    }

    public RpcMethodSpec<V> setSharable(boolean value) {
        ctl = Bits.set(ctl, RpcProtocol.MASK_SHARABLE, value);
        return this;
    }

    // region getter

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
    // endregion

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

    @Override
    public String toString() {
        return "DefaultRpcMethodSpec{"
                + "serviceId=" + serviceId
                + ", methodId=" + methodId
                + ", methodParams=" + parameters
                + '}';
    }

}